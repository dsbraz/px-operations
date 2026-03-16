# px-operations

## Solution Layout

- `src/Server` contains the API host and backend layers.
- `src/Client` contains the Blazor WebAssembly client.
- `tests/Server` contains architecture, domain and API integration tests.
- `tests/Client` contains Blazor component tests.
- `specs/openapi` is reserved for exported OpenAPI snapshots.

## Development with Docker Desktop

1. Copy `.env.example` to `.env`.
2. Optionally set `OTEL_EXPORTER_OTLP_ENDPOINT` if you want to export traces and metrics to a collector.
3. Run `docker compose up`.
4. Open `http://localhost:8080` for the Blazor client.
5. Open `http://localhost:8081/swagger` for Swagger UI.
6. Open `http://localhost:8081/health/live` for liveness.
7. Open `http://localhost:8081/health/ready` for readiness.

The development environment uses `dotnet watch` inside the `client` and `server` containers, so changes under `src/Client/` and `src/Server/` trigger rebuilds and browser refresh when supported.

## Deploy to Google Cloud Run

The production deployment publishes the API and the Blazor WebAssembly client as separate Cloud Run services, with a dedicated Cloud Run Job for EF Core migrations.

1. Copy `.env.gcp.example` to `.env.gcp` and adjust the values if needed.
2. Run `./scripts/gcp-bootstrap.sh` to provision Artifact Registry, service account, Cloud SQL, database user, and the database connection secret.
3. Run `./deploy.sh` or `./scripts/gcp-deploy.sh`.

The deploy flow uses:

- Cloud Build to generate the container images remotely
- Cloud Run Job to execute EF Core migrations before the service rollout
- Secret Manager to inject `ConnectionStrings__Default`

Defaults:

- `PROJECT_ID=px-operations`
- `REGION=us-central1`
- `API_SERVICE_NAME=px-operations-api`
- `WEB_SERVICE_NAME=px-operations-web`
- `SQL_INSTANCE=px-operations-pg-us`
