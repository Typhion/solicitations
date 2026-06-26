# Backend dev tasks — run from backend/.
# `make help` lists everything. Parameterised targets show their usage when run without args.

SLN     := backend.sln
API     := WebApi
INFRA   := Infrastructure
COMPOSE := ../docker-compose.yml

.PHONY: help restore build run test test-one migration migration-remove migrations-list secret secrets db-up db-down clean

help: ## List available targets
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) \
		| awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-16s\033[0m %s\n", $$1, $$2}'

restore: ## Restore NuGet packages
	dotnet restore $(SLN)

build: ## Build the solution
	dotnet build $(SLN)

run: ## Run the API (http://localhost:5182)
	dotnet run --project $(API)

test: ## Run all tests (integration tests need Docker)
	dotnet test $(SLN)

test-one: ## Run tests matching FILTER, e.g. make test-one FILTER=LoginResult
	@test -n "$(FILTER)" || { echo "Usage: make test-one FILTER=<name>"; exit 1; }
	dotnet test $(SLN) --filter "FullyQualifiedName~$(FILTER)"

migration: ## Add an EF migration, e.g. make migration NAME=AddFoo
	@test -n "$(NAME)" || { echo "Usage: make migration NAME=<name>"; exit 1; }
	dotnet ef migrations add $(NAME) --project $(INFRA) --startup-project $(API)

migration-remove: ## Remove the last (not-yet-applied) migration
	dotnet ef migrations remove --project $(INFRA) --startup-project $(API)

migrations-list: ## List migrations and their applied state
	dotnet ef migrations list --project $(INFRA) --startup-project $(API)

secret: ## Set a user-secret, e.g. make secret KEY=Jwt:Key VALUE='...' (single-quote values with $ or !)
	@test -n "$(KEY)" || { echo "Usage: make secret KEY=<key> VALUE=<value>"; exit 1; }
	dotnet user-secrets set "$(KEY)" "$(VALUE)" --project $(API)

secrets: ## List the configured user-secrets
	dotnet user-secrets list --project $(API)

db-up: ## Start the local Postgres (repo-root compose)
	docker compose -f $(COMPOSE) up -d

db-down: ## Stop the local Postgres
	docker compose -f $(COMPOSE) down

clean: ## Clean build outputs
	dotnet clean $(SLN)
