.PHONY: up down dev

up:
	docker compose up -d

down:
	docker compose down

# Inner dev loop: Postgres + hot-reloading API (dotnet watch on bind-mounted source).
# Run the Angular dev server natively alongside it: cd frontend && npm start
dev:
	docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d db api
