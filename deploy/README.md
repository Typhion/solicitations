# Deployment

Push to `main` → GitHub Actions (`.github/workflows/deploy.yml`):

1. **CI**: backend build + unit/integration tests, frontend build + tests, Docker image builds
   (`ci.yml`, the same workflow that runs on every other branch and PR).
2. **Build & push**: `ghcr.io/typhion/sollicitations-{api,web}:<sha>` (+ `:latest`).
3. **Deploy over SSH**: the steps are listed below.
4. **Smoke check**: the job polls `https://api.admin.typhion.com/health/ready` and
   `https://admin.typhion.com`. If they aren't healthy within about 2 minutes, the job fails
   and prints the API logs.

The deploy step:
- copies `deploy/docker-compose.yml` to `/opt/sollicitations`
- dumps the DB to `backups/`
- rewrites only the `API_IMAGE`/`WEB_IMAGE` lines in `.env`
- runs `docker compose pull && up -d`

The server is shared with **euro-millions**, whose stack in `/opt/euromillions` runs
Traefik. Traefik owns the `traefik-web` network, ports 80/443, HTTP→HTTPS redirects and the
Let's Encrypt resolver `le`. This stack only *joins* that network. It never touches Traefik,
binds no host ports, and caps its own memory and log size, so euro-millions is unaffected.
Don't run `docker compose down` in `/opt/euromillions`: that would take Traefik down, and
both sites with it.

## One-time setup

### 1. DNS
Add A records (and AAAA if the server has IPv6) for `admin.typhion.com` and
`api.admin.typhion.com` pointing to the server IP. Traefik requests the certificates on the
first request.

### 2. Server
```bash
ssh root@<server>
docker network ls | grep traefik-web           # must exist (euro-millions stack)
mkdir -p /opt/sollicitations/backups
chown -R deploy:deploy /opt/sollicitations
chmod 700 /opt/sollicitations
```
Then, as `deploy`, create `/opt/sollicitations/.env` from `deploy/.env.example`:
```bash
openssl rand -hex 32   # run once for POSTGRES_PASSWORD and once for JWT_KEY (hex: no special chars)
chmod 600 /opt/sollicitations/.env
```
`SEED_ADMIN_PASSWORD` creates the first admin account. It needs at least 12 characters with
upper, lower, digit and symbol. Change the password after your first login.

### 3. Deploy SSH key
Use a separate key for this repo, so it can be revoked without affecting euro-millions:
```bash
ssh-keygen -t ed25519 -N '' -C 'gha-sollicitations' -f ./sol_deploy
ssh-copy-id -i ./sol_deploy.pub deploy@<server>     # or append to ~deploy/.ssh/authorized_keys
ssh-keyscan -t ed25519 <server>                     # output → SSH_KNOWN_HOSTS
```
Check the `ssh-keyscan` fingerprint against the one the server reports
(`ssh-keygen -lf /etc/ssh/ssh_host_ed25519_key.pub` on the box). Delete the local key files
once they're stored in GitHub.

> The `deploy` user is in the `docker` group, which is effectively root on the host.
> Treat `SSH_PRIVATE_KEY` like a root credential.

### 4. GitHub
- **Settings → Environments → New environment `production`**:
  - Deployment branches: *Selected branches* → `main` only.
  - Add these **environment** secrets (not repo secrets, so only the deploy job on `main`
    can read them):

    | Secret | Value |
    |--------|-------|
    | `SSH_HOST` | server IP / hostname |
    | `SSH_USER` | `deploy` |
    | `SSH_PRIVATE_KEY` | contents of `sol_deploy` |
    | `SSH_KNOWN_HOSTS` | the `ssh-keyscan` output line(s) |

- **Settings → Branches**: protect `main` and require the CI checks (`backend`, `frontend`,
  `docker-build`) to pass before merging.
- **Settings → Code security**: enable Dependabot alerts + security updates.

The GHCR packages can stay private. The deploy job logs the server in with its own
short-lived `GITHUB_TOKEN`, stores the credential in `/opt/sollicitations/.docker` (kept
apart from euro-millions'), and logs out again at the end. No long-lived registry
credentials live on the box.

## Operations

All commands run as `deploy` in `/opt/sollicitations`.

- **Logs**: `docker compose logs -f api`
- **Rollback**: set `API_IMAGE`/`WEB_IMAGE` in `.env` to an earlier `:<sha>`, then run
  `docker compose up -d`. A schema migration is not rolled back by this; restore the
  matching backup if needed.
- **Restore a backup** (the dumps are plain SQL):
  ```bash
  docker compose stop api
  docker compose exec -T db sh -c 'dropdb -U "$POSTGRES_USER" "$POSTGRES_DB" && createdb -U "$POSTGRES_USER" "$POSTGRES_DB"'
  gunzip -c backups/pre-<sha>.sql.gz | docker compose exec -T db sh -c 'psql -U "$POSTGRES_USER" -d "$POSTGRES_DB"'
  docker compose start api
  ```
- **Off-box backups**: `backups/` only protects against bad migrations, not against losing
  the server. Enable Hetzner server backups, or copy `backups/` somewhere else on a schedule.
