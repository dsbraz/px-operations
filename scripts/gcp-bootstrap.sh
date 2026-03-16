#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<USAGE
Uso:
  ./scripts/gcp-bootstrap.sh [opcoes]

Opcoes:
  --env-file <path>             Arquivo de variaveis (default: .env.gcp, se existir)
  --project-id <id>             PROJECT_ID
  --region <region>             REGION
  --repo-name <nome>            REPO_NAME
  --sql-instance <nome>         SQL_INSTANCE
  --db-name <nome>              DB_NAME
  --db-user <nome>              DB_USER
  --db-pass <senha>             DB_PASS
  --service-account <nome>      SERVICE_ACCOUNT
  --service-name <nome>         Alias legado para API_SERVICE_NAME
  --api-service-name <nome>     API_SERVICE_NAME
  --web-service-name <nome>     WEB_SERVICE_NAME
  --secret-db-connection <n>    SECRET_DB_CONNECTION
  --db-tier <tier>              DB_TIER
  --db-edition <ed>             DB_EDITION
  --billing-account-id <id>     BILLING_ACCOUNT_ID (opcional)
USAGE
}

require_cmd() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Erro: comando '$1' nao encontrado." >&2
    exit 1
  fi
}

wait_for_service_account() {
  local email="$1"
  local attempts="${2:-12}"
  local delay_seconds="${3:-5}"

  for ((attempt = 1; attempt <= attempts; attempt++)); do
    if gcloud iam service-accounts describe "$email" >/dev/null 2>&1; then
      return 0
    fi

    if (( attempt < attempts )); then
      echo "Aguardando propagacao da service account (${attempt}/${attempts})..."
      sleep "$delay_seconds"
    fi
  done

  echo "Erro: service account '$email' nao ficou disponivel a tempo." >&2
  exit 1
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

escape_env_value() {
  local value="$1"
  value="${value//\\/\\\\}"
  value="${value//\"/\\\"}"
  printf '%s' "$value"
}

upsert_env_var() {
  local file="$1"
  local key="$2"
  local value="$3"
  local escaped
  escaped="$(escape_env_value "$value")"

  if [[ ! -f "$file" ]]; then
    printf '%s="%s"\n' "$key" "$escaped" >"$file"
    return
  fi

  local tmp
  tmp="$(mktemp)"
  awk -v key="$key" -v value="$escaped" '
    BEGIN { found = 0 }
    $0 ~ "^" key "=" {
      print key "=\"" value "\""
      found = 1
      next
    }
    { print }
    END {
      if (!found) {
        print key "=\"" value "\""
      }
    }
  ' "$file" >"$tmp"
  mv "$tmp" "$file"
}

persist_effective_env() {
  local file="$1"
  upsert_env_var "$file" "PROJECT_ID" "$PROJECT_ID"
  upsert_env_var "$file" "REGION" "$REGION"
  upsert_env_var "$file" "REPO_NAME" "$REPO_NAME"
  upsert_env_var "$file" "SQL_INSTANCE" "$SQL_INSTANCE"
  upsert_env_var "$file" "DB_NAME" "$DB_NAME"
  upsert_env_var "$file" "DB_USER" "$DB_USER"
  upsert_env_var "$file" "DB_PASS" "$DB_PASS"
  upsert_env_var "$file" "SERVICE_ACCOUNT" "$SERVICE_ACCOUNT"
  upsert_env_var "$file" "API_SERVICE_NAME" "$API_SERVICE_NAME"
  upsert_env_var "$file" "WEB_SERVICE_NAME" "$WEB_SERVICE_NAME"
  upsert_env_var "$file" "SECRET_DB_CONNECTION" "$SECRET_DB_CONNECTION"
  upsert_env_var "$file" "DB_TIER" "$DB_TIER"
  upsert_env_var "$file" "DB_EDITION" "$DB_EDITION"
  if [[ -n "${BILLING_ACCOUNT_ID:-}" ]]; then
    upsert_env_var "$file" "BILLING_ACCOUNT_ID" "$BILLING_ACCOUNT_ID"
  fi
}

generate_secure_password() {
  if command -v openssl >/dev/null 2>&1; then
    openssl rand -base64 32 | tr -d '\n'
    return
  fi

  LC_ALL=C tr -dc 'A-Za-z0-9' </dev/urandom | head -c 48
}

normalize_secret_name() {
  local value="$1"
  value="$(printf '%s' "$value" | tr '[:upper:]' '[:lower:]')"
  value="$(printf '%s' "$value" | sed -E 's/[^a-z0-9-]+/-/g; s/^-+//; s/-+$//; s/-{2,}/-/g')"
  printf '%s' "$value"
}

generate_secret_name() {
  local suffix="$1"
  local base
  base="$(normalize_secret_name "$PROJECT_ID")"
  if [[ -z "$base" ]]; then
    base="px-operations"
  fi
  printf '%s-%s' "$base" "$suffix"
}

ensure_project_exists() {
  if gcloud projects describe "$PROJECT_ID" >/dev/null 2>&1; then
    return
  fi

  echo "Erro: projeto '$PROJECT_ID' nao encontrado." >&2
  exit 1
}

ensure_billing_linked() {
  local billing_enabled
  billing_enabled="$(gcloud beta billing projects describe "$PROJECT_ID" --format='value(billingEnabled)' 2>/dev/null || true)"

  if [[ "$billing_enabled" == "True" ]]; then
    return
  fi

  if [[ -n "${BILLING_ACCOUNT_ID:-}" ]]; then
    gcloud beta billing projects link "$PROJECT_ID" --billing-account="$BILLING_ACCOUNT_ID"
    return
  fi

  echo "Aviso: billing nao vinculado ao projeto '$PROJECT_ID'." >&2
}

secret_exists() {
  local name="$1"
  gcloud secrets describe "$name" --project "$PROJECT_ID" >/dev/null 2>&1
}

ensure_secret() {
  local name="$1"
  if secret_exists "$name"; then
    echo "Secret ja existe: $name"
  else
    gcloud secrets create "$name" --replication-policy="automatic" --project "$PROJECT_ID"
    echo "Secret criado: $name"
  fi
}

ensure_secret_value() {
  local name="$1"
  local value="$2"
  printf '%s' "$value" | gcloud secrets versions add "$name" --data-file=- --project "$PROJECT_ID" >/dev/null
  echo "Nova versao publicada para secret: $name"
}

require_cmd gcloud

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
    --sql-instance) SQL_INSTANCE="$2"; shift 2 ;;
    --db-name) DB_NAME="$2"; shift 2 ;;
    --db-user) DB_USER="$2"; shift 2 ;;
    --db-pass) DB_PASS="$2"; shift 2 ;;
    --service-account) SERVICE_ACCOUNT="$2"; shift 2 ;;
    --service-name) API_SERVICE_NAME="$2"; shift 2 ;;
    --api-service-name) API_SERVICE_NAME="$2"; shift 2 ;;
    --web-service-name) WEB_SERVICE_NAME="$2"; shift 2 ;;
    --secret-db-connection) SECRET_DB_CONNECTION="$2"; shift 2 ;;
    --db-tier) DB_TIER="$2"; shift 2 ;;
    --db-edition) DB_EDITION="$2"; shift 2 ;;
    --billing-account-id) BILLING_ACCOUNT_ID="$2"; shift 2 ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Parametro invalido: $1" >&2; usage; exit 1 ;;
  esac
done

PROJECT_ID="${PROJECT_ID:-px-operations}"
REGION="${REGION:-us-central1}"
REPO_NAME="${REPO_NAME:-px-operations}"
SQL_INSTANCE="${SQL_INSTANCE:-px-operations-pg-us}"
DB_NAME="${DB_NAME:-pxoperations}"
DB_USER="${DB_USER:-pxoperations_app}"
SERVICE_ACCOUNT="${SERVICE_ACCOUNT:-px-operations-runner}"
API_SERVICE_NAME="${API_SERVICE_NAME:-px-operations-api}"
WEB_SERVICE_NAME="${WEB_SERVICE_NAME:-px-operations-web}"
DB_TIER="${DB_TIER:-db-custom-1-3840}"
DB_EDITION="${DB_EDITION:-ENTERPRISE}"
SECRET_DB_CONNECTION="${SECRET_DB_CONNECTION:-$(generate_secret_name "db-connection")}"
DB_PASS="${DB_PASS:-$(generate_secure_password)}"

persist_effective_env "$ENV_FILE"
echo "Variaveis efetivas gravadas em '$ENV_FILE'."

INSTANCE_CONNECTION_NAME="${PROJECT_ID}:${REGION}:${SQL_INSTANCE}"
SERVICE_ACCOUNT_EMAIL="${SERVICE_ACCOUNT}@${PROJECT_ID}.iam.gserviceaccount.com"

ensure_project_exists

echo "==> Configurando projeto"
gcloud config set project "$PROJECT_ID" >/dev/null

ensure_billing_linked

echo "==> Habilitando APIs"
gcloud services enable \
  artifactregistry.googleapis.com \
  cloudbuild.googleapis.com \
  run.googleapis.com \
  sqladmin.googleapis.com \
  secretmanager.googleapis.com \
  iam.googleapis.com \
  cloudtrace.googleapis.com \
  monitoring.googleapis.com

echo "==> Garantindo Artifact Registry"
if gcloud artifacts repositories describe "$REPO_NAME" --location "$REGION" >/dev/null 2>&1; then
  echo "Repositorio ja existe: $REPO_NAME"
else
  gcloud artifacts repositories create "$REPO_NAME" \
    --repository-format=docker \
    --location="$REGION" \
    --description="Docker repository for Px Operations"
fi

echo "==> Garantindo service account"
if gcloud iam service-accounts describe "$SERVICE_ACCOUNT_EMAIL" >/dev/null 2>&1; then
  echo "Service account ja existe: $SERVICE_ACCOUNT_EMAIL"
else
  gcloud iam service-accounts create "$SERVICE_ACCOUNT" \
    --display-name="Px Operations Cloud Run runtime"
fi

wait_for_service_account "$SERVICE_ACCOUNT_EMAIL"

echo "==> Aplicando papeis na service account (runtime)"
gcloud projects add-iam-policy-binding "$PROJECT_ID" \
  --member="serviceAccount:${SERVICE_ACCOUNT_EMAIL}" \
  --role="roles/cloudsql.client" >/dev/null
gcloud projects add-iam-policy-binding "$PROJECT_ID" \
  --member="serviceAccount:${SERVICE_ACCOUNT_EMAIL}" \
  --role="roles/secretmanager.secretAccessor" >/dev/null
gcloud projects add-iam-policy-binding "$PROJECT_ID" \
  --member="serviceAccount:${SERVICE_ACCOUNT_EMAIL}" \
  --role="roles/cloudtrace.agent" >/dev/null
gcloud projects add-iam-policy-binding "$PROJECT_ID" \
  --member="serviceAccount:${SERVICE_ACCOUNT_EMAIL}" \
  --role="roles/monitoring.metricWriter" >/dev/null

echo "==> Aplicando papeis na service account padrao do Cloud Build"
PROJECT_NUMBER="$(gcloud projects describe "$PROJECT_ID" --format='value(projectNumber)')"
CLOUDBUILD_SA="${PROJECT_NUMBER}-compute@developer.gserviceaccount.com"

gcloud projects add-iam-policy-binding "$PROJECT_ID" \
  --member="serviceAccount:${CLOUDBUILD_SA}" \
  --role="roles/storage.admin" >/dev/null
gcloud projects add-iam-policy-binding "$PROJECT_ID" \
  --member="serviceAccount:${CLOUDBUILD_SA}" \
  --role="roles/artifactregistry.writer" >/dev/null
gcloud projects add-iam-policy-binding "$PROJECT_ID" \
  --member="serviceAccount:${CLOUDBUILD_SA}" \
  --role="roles/logging.logWriter" >/dev/null

echo "==> Garantindo Cloud SQL"
if gcloud sql instances describe "$SQL_INSTANCE" >/dev/null 2>&1; then
  echo "Cloud SQL instance ja existe: $SQL_INSTANCE"
else
  gcloud sql instances create "$SQL_INSTANCE" \
    --database-version=POSTGRES_17 \
    --tier="$DB_TIER" \
    --edition="$DB_EDITION" \
    --region="$REGION" \
    --storage-type=SSD \
    --storage-size=20GB \
    --backup-start-time=03:00
fi

echo "==> Garantindo database"
if gcloud sql databases describe "$DB_NAME" --instance "$SQL_INSTANCE" >/dev/null 2>&1; then
  echo "Database ja existe: $DB_NAME"
else
  gcloud sql databases create "$DB_NAME" --instance "$SQL_INSTANCE"
fi

echo "==> Garantindo usuario de banco"
if gcloud sql users list --instance "$SQL_INSTANCE" --format='value(name)' | grep -Fxq "$DB_USER"; then
  echo "Usuario de banco ja existe: $DB_USER"
else
  gcloud sql users create "$DB_USER" --instance "$SQL_INSTANCE" --password "$DB_PASS"
fi

echo "==> Garantindo secret de conexao"
ensure_secret "$SECRET_DB_CONNECTION"

DB_CONNECTION="Host=/cloudsql/${INSTANCE_CONNECTION_NAME};Port=5432;Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASS};SSL Mode=Disable"
ensure_secret_value "$SECRET_DB_CONNECTION" "$DB_CONNECTION"

echo "==> Bootstrap concluido"
echo "PROJECT_ID=$PROJECT_ID"
echo "REGION=$REGION"
echo "REPO_NAME=$REPO_NAME"
echo "API_SERVICE_NAME=$API_SERVICE_NAME"
echo "WEB_SERVICE_NAME=$WEB_SERVICE_NAME"
echo "SQL_INSTANCE=$SQL_INSTANCE"
echo "INSTANCE_CONNECTION_NAME=$INSTANCE_CONNECTION_NAME"
echo "SERVICE_ACCOUNT_EMAIL=$SERVICE_ACCOUNT_EMAIL"
echo "SECRET_DB_CONNECTION=$SECRET_DB_CONNECTION"
