#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<USAGE
Uso:
  ./scripts/gcp-deploy.sh [opcoes]

Opcoes:
  --env-file <path>             Arquivo de variaveis (default: .env.gcp, se existir)
  --project-id <id>             PROJECT_ID
  --region <region>             REGION
  --repo-name <nome>            REPO_NAME
  --service-name <nome>         Alias legado para API_SERVICE_NAME
  --api-service-name <nome>     API_SERVICE_NAME
  --web-service-name <nome>     WEB_SERVICE_NAME
  --sql-instance <nome>         SQL_INSTANCE
  --service-account <nome>      SERVICE_ACCOUNT
  --secret-db-connection <n>    SECRET_DB_CONNECTION
  --migration-job-name <nome>   MIGRATION_JOB_NAME
  --image-tag <tag>             IMAGE_TAG
  --skip-migration              Pular etapa de migracao
USAGE
}

require_cmd() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Erro: comando '$1' nao encontrado." >&2
    exit 1
  fi
}

require_env() {
  local name="$1"
  if [[ -z "${!name:-}" ]]; then
    echo "Erro: variavel obrigatoria nao definida: $name" >&2
    exit 1
  fi
}

load_env_file() {
  local env_file="$1"
  if [[ -f "$env_file" ]]; then
    set -a
    # shellcheck disable=SC1090
    source "$env_file"
    set +a
  fi
}

job_exists() {
  local name="$1"
  gcloud run jobs describe "$name" --region "$REGION" --project "$PROJECT_ID" >/dev/null 2>&1
}

secret_has_versions() {
  local name="$1"
  [[ -n "$(gcloud secrets versions list "$name" --project "$PROJECT_ID" --limit=1 --format='value(name)' 2>/dev/null)" ]]
}

require_cmd gcloud
require_cmd curl

authenticated_curl() {
  local url="$1"
  local token
  token="$(gcloud auth print-identity-token)"
  curl --fail --silent --show-error -H "Authorization: Bearer ${token}" "$url" >/dev/null
}

ENV_FILE=".env.gcp"
args=("$@")
for ((i = 0; i < ${#args[@]}; i++)); do
  if [[ "${args[$i]}" == "--env-file" ]]; then
    if (( i + 1 >= ${#args[@]} )); then
      echo "Erro: --env-file requer um valor." >&2
      exit 1
    fi
    ENV_FILE="${args[$((i + 1))]}"
  fi
done

load_env_file "$ENV_FILE"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --env-file) ENV_FILE="$2"; shift 2 ;;
    --project-id) PROJECT_ID="$2"; shift 2 ;;
    --region) REGION="$2"; shift 2 ;;
    --repo-name) REPO_NAME="$2"; shift 2 ;;
    --service-name) API_SERVICE_NAME="$2"; shift 2 ;;
    --api-service-name) API_SERVICE_NAME="$2"; shift 2 ;;
    --web-service-name) WEB_SERVICE_NAME="$2"; shift 2 ;;
    --sql-instance) SQL_INSTANCE="$2"; shift 2 ;;
    --service-account) SERVICE_ACCOUNT="$2"; shift 2 ;;
    --secret-db-connection) SECRET_DB_CONNECTION="$2"; shift 2 ;;
    --migration-job-name) MIGRATION_JOB_NAME="$2"; shift 2 ;;
    --image-tag) IMAGE_TAG="$2"; shift 2 ;;
    --skip-migration) SKIP_MIGRATION="true"; shift ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Parametro invalido: $1" >&2; usage; exit 1 ;;
  esac
done

require_env PROJECT_ID
require_env REGION

REPO_NAME="${REPO_NAME:-px-operations}"
API_SERVICE_NAME="${API_SERVICE_NAME:-px-operations-api}"
WEB_SERVICE_NAME="${WEB_SERVICE_NAME:-px-operations-web}"
SQL_INSTANCE="${SQL_INSTANCE:-px-operations-pg-us}"
SERVICE_ACCOUNT="${SERVICE_ACCOUNT:-px-operations-runner}"
SECRET_DB_CONNECTION="${SECRET_DB_CONNECTION:-px-operations-db-connection}"
MIGRATION_JOB_NAME="${MIGRATION_JOB_NAME:-${API_SERVICE_NAME}-migrate}"
SKIP_MIGRATION="${SKIP_MIGRATION:-false}"
IMAGE_TAG="${IMAGE_TAG:-r$(date +%y%m%d%H%M%S)}"

INSTANCE_CONNECTION_NAME="${PROJECT_ID}:${REGION}:${SQL_INSTANCE}"
SERVICE_ACCOUNT_EMAIL="${SERVICE_ACCOUNT}@${PROJECT_ID}.iam.gserviceaccount.com"
API_IMAGE_URI="${REGION}-docker.pkg.dev/${PROJECT_ID}/${REPO_NAME}/${API_SERVICE_NAME}:${IMAGE_TAG}"
WEB_IMAGE_URI="${REGION}-docker.pkg.dev/${PROJECT_ID}/${REPO_NAME}/${WEB_SERVICE_NAME}:${IMAGE_TAG}"
MIGRATE_IMAGE_URI="${REGION}-docker.pkg.dev/${PROJECT_ID}/${REPO_NAME}/${API_SERVICE_NAME}-migrate:${IMAGE_TAG}"

echo "==> Configurando projeto"
gcloud config set project "$PROJECT_ID" >/dev/null

echo "==> Validando secret obrigatorio"
if ! secret_has_versions "$SECRET_DB_CONNECTION"; then
  echo "Erro: secret '$SECRET_DB_CONNECTION' nao possui versao." >&2
  echo "Execute ./scripts/gcp-bootstrap.sh antes do deploy." >&2
  exit 1
fi

if [[ "$SKIP_MIGRATION" == "true" ]]; then
  echo "==> Buildando imagem da API (${API_IMAGE_URI})"
  gcloud builds submit \
    --project "$PROJECT_ID" \
    --config "scripts/cloudbuild-image.yaml" \
    --substitutions "_IMAGE_URI=${API_IMAGE_URI},_DOCKER_TARGET=web" \
    .

  echo "==> Buildando imagem do frontend (${WEB_IMAGE_URI})"
  gcloud builds submit \
    --project "$PROJECT_ID" \
    --config "scripts/cloudbuild-image.yaml" \
    --substitutions "_IMAGE_URI=${WEB_IMAGE_URI},_DOCKER_TARGET=frontend" \
    .
  echo "==> Migracao pulada (--skip-migration)"
else
  echo "==> Buildando imagem de migracao (${MIGRATE_IMAGE_URI})"
  gcloud builds submit \
    --project "$PROJECT_ID" \
    --config "scripts/cloudbuild-image.yaml" \
    --substitutions "_IMAGE_URI=${MIGRATE_IMAGE_URI},_DOCKER_TARGET=migrate" \
    .

  echo "==> Buildando imagem da API (${API_IMAGE_URI})"
  gcloud builds submit \
    --project "$PROJECT_ID" \
    --config "scripts/cloudbuild-image.yaml" \
    --substitutions "_IMAGE_URI=${API_IMAGE_URI},_DOCKER_TARGET=web" \
    .

  echo "==> Buildando imagem do frontend (${WEB_IMAGE_URI})"
  gcloud builds submit \
    --project "$PROJECT_ID" \
    --config "scripts/cloudbuild-image.yaml" \
    --substitutions "_IMAGE_URI=${WEB_IMAGE_URI},_DOCKER_TARGET=frontend" \
    .

  echo "==> Garantindo Cloud Run Job de migracao"
  if job_exists "$MIGRATION_JOB_NAME"; then
    gcloud run jobs update "$MIGRATION_JOB_NAME" \
      --project "$PROJECT_ID" \
      --region "$REGION" \
      --image "$MIGRATE_IMAGE_URI" \
      --service-account "$SERVICE_ACCOUNT_EMAIL" \
      --set-cloudsql-instances "$INSTANCE_CONNECTION_NAME" \
      --set-secrets "ConnectionStrings__Default=${SECRET_DB_CONNECTION}:latest" \
      --set-env-vars "DOTNET_ENVIRONMENT=Production,ASPNETCORE_ENVIRONMENT=Production" \
      --max-retries 1
  else
    gcloud run jobs create "$MIGRATION_JOB_NAME" \
      --project "$PROJECT_ID" \
      --region "$REGION" \
      --image "$MIGRATE_IMAGE_URI" \
      --service-account "$SERVICE_ACCOUNT_EMAIL" \
      --set-cloudsql-instances "$INSTANCE_CONNECTION_NAME" \
      --set-secrets "ConnectionStrings__Default=${SECRET_DB_CONNECTION}:latest" \
      --set-env-vars "DOTNET_ENVIRONMENT=Production,ASPNETCORE_ENVIRONMENT=Production" \
      --max-retries 1
  fi

  echo "==> Executando migracao"
  gcloud run jobs execute "$MIGRATION_JOB_NAME" \
    --project "$PROJECT_ID" \
    --region "$REGION" \
    --wait
fi

echo "==> Deployando API no Cloud Run"
gcloud run deploy "$API_SERVICE_NAME" \
  --project "$PROJECT_ID" \
  --region "$REGION" \
  --platform managed \
  --image "$API_IMAGE_URI" \
  --service-account "$SERVICE_ACCOUNT_EMAIL" \
  --port 8080 \
  --allow-unauthenticated \
  --set-cloudsql-instances "$INSTANCE_CONNECTION_NAME" \
  --set-secrets "ConnectionStrings__Default=${SECRET_DB_CONNECTION}:latest" \
  --set-env-vars "^|^ASPNETCORE_ENVIRONMENT=Production|DOTNET_ENVIRONMENT=Production|ASPNETCORE_URLS=http://0.0.0.0:8080|ASPNETCORE_FORWARDEDHEADERS_ENABLED=true|OpenTelemetry__ServiceName=PxOperations.Api|OpenTelemetry__Otlp__Endpoint=https://telemetry.googleapis.com"

echo "==> Validando health checks da API"
API_URL="$(gcloud run services describe "$API_SERVICE_NAME" --region "$REGION" --project "$PROJECT_ID" --format='value(status.url)')"

authenticated_curl "${API_URL}/health/live"
authenticated_curl "${API_URL}/health/ready"

echo "==> Deployando frontend no Cloud Run"
gcloud run deploy "$WEB_SERVICE_NAME" \
  --project "$PROJECT_ID" \
  --region "$REGION" \
  --platform managed \
  --image "$WEB_IMAGE_URI" \
  --port 8080 \
  --allow-unauthenticated \
  --set-env-vars "PX_OPERATIONS_API_BASE_URL=${API_URL}"

echo "==> Validando frontend"
FRONTEND_URL="$(gcloud run services describe "$WEB_SERVICE_NAME" --region "$REGION" --project "$PROJECT_ID" --format='value(status.url)')"

authenticated_curl "${FRONTEND_URL}/"
authenticated_curl "${FRONTEND_URL}/_framework/blazor.webassembly.js"

echo "==> Deploy concluido com sucesso"
echo "API_URL=${API_URL}"
echo "FRONTEND_URL=${FRONTEND_URL}"
