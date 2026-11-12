# px-operations

## Solution Layout

- `src/Server` contains the API host and backend layers.
- `src/Client` contains the Blazor WebAssembly client.
- `tests/Server` contains architecture, domain and API integration tests.
- `tests/Client` contains Blazor component tests.
- `specs/openapi` is reserved for exported OpenAPI snapshots.

## Development with Docker Desktop

1. Copy `.env.example` to `.env` and fill in `ENTRA_TENANT_ID` and `ENTRA_CLIENT_ID` (see Authentication below).
2. Optionally set `OTEL_EXPORTER_OTLP_ENDPOINT` if you want to export traces and metrics to a collector.
3. Run `docker compose up`.
4. Open `http://localhost:8080` for the Blazor client.
5. Open `http://localhost:8081/swagger` for Swagger UI.
6. Open `http://localhost:8081/health/live` for liveness.
7. Open `http://localhost:8081/health/ready` for readiness.

The development environment uses `dotnet watch` inside the `client` and `server` containers, so changes under `src/Client/` and `src/Server/` trigger rebuilds and browser refresh when supported.

## Authentication (Microsoft 365 / Entra ID)

Sign-in is restricted to **members** of the BRQ Entra ID tenant; guest (B2B) accounts get 403. Every `/api` route requires a token except `/health/*` and the public NPS survey (`/api/nps/public/*`). The API refuses to start without `Authentication:TenantId` and `Authentication:ClientId`.

One app registration (single tenant) serves every environment:

1. **Platform: Single-page application**, with redirect URIs `<origin>/authentication/login-callback` and `<origin>/authentication/logout-callback` for `http://localhost:8080` and each deployed origin.
2. **Expose an API**: Application ID URI `api://<client-id>` and scope `access_as_user`.
3. **Manifest**: `"accessTokenAcceptedVersion": 2`.
4. **Token configuration**: add the optional claim `acct` to the **access token**. Without it, every user gets 403.

Then set the two public identifiers in one place, outside git: `.env` for local development and `.env.gcp` for deploys.

```
ENTRA_TENANT_ID=<tenant-id>
ENTRA_CLIENT_ID=<client-id>
```

Everything else is derived from them:

- **API**: `compose.yaml`, and `scripts/cloudrun-api.yaml.template` in deploys, pass them as `Authentication__TenantId` and `Authentication__ClientId`.
- **Client in Cloud Run**: nginx renders `/appsettings.json` from the container variables, which `gcp-deploy.sh` sets. Without them nginx refuses to start.
- **Client in `docker compose`**: the client container writes `wwwroot/appsettings.Development.json` (git-ignored) from `.env` on startup.

Running the client with `dotnet run` outside Docker requires creating that file by hand:

```json
{
  "Api": { "BaseUrl": "http://localhost:8081/", "Scope": "api://<client-id>/access_as_user" },
  "AzureAd": { "Authority": "https://login.microsoftonline.com/<tenant-id>", "ClientId": "<client-id>", "ValidateAuthority": true }
}
```

Before relying on it, decode a real access token and check `ver` = `2.0`, `iss` = `https://login.microsoftonline.com/<tenant-id>/v2.0`, `aud` = `<client-id>`, and that `acct` is present.

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
