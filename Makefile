.PHONY: up down

# Local Postgres only. Run the app natively alongside it:
#   dotnet run --project backend/WebApi   (API on http://localhost:5182)
#   cd frontend && npm start              (SPA on http://localhost:4200)
up:
	docker compose up -d

down:
	docker compose down
