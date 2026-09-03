#!/usr/bin/env bash
#
# One-shot local deploy: creates the k3d cluster, installs Kong, creates the
# secrets, builds and imports the service images, and applies all manifests.
#
# Re-runnable. Override the demo credentials with env vars if you like:
#   JWT_SIGNING_KEY=... DB_PASSWORD=... ./scripts/deploy.sh
#
set -euo pipefail
cd "$(dirname "$0")/.."

CLUSTER="${CLUSTER:-bankdev}"
JWT_SIGNING_KEY="${JWT_SIGNING_KEY:-demo-signing-key-please-change-min-32-bytes!!}"
DB_PASSWORD="${DB_PASSWORD:-supersecret123}"

# Image tags — pinned (never :latest) so Kubernetes always knows what it runs.
ACCOUNT_TAG="account-service:0.3"
TRANSACTION_TAG="transaction-service:0.2"
AUTH_TAG="auth-service:0.1"
FRONTEND_TAG="frontend:0.1"

echo "==> 1/6  k3d cluster '${CLUSTER}'"
if ! k3d cluster list 2>/dev/null | grep -qE "^${CLUSTER}\s"; then
  # Traefik disabled so Kong is the only ingress controller.
  # Host port 8000 is mapped to the cluster load balancer's port 80.
  k3d cluster create "${CLUSTER}" \
    --agents 1 \
    --k3s-arg "--disable=traefik@server:*" \
    -p "8000:80@loadbalancer"
else
  echo "    already exists"
fi

echo "==> 2/6  Kong ingress controller (DB-less) via Helm"
helm repo add kong https://charts.konghq.com >/dev/null 2>&1 || true
helm repo update >/dev/null
helm upgrade --install kong kong/kong \
  --namespace kong --create-namespace \
  --set ingressController.enabled=true \
  --set env.database=off \
  --wait

echo "==> 3/6  Secrets (generated here, never committed)"
apply_secret() { kubectl create secret generic "$@" --dry-run=client -o yaml | kubectl apply -f - ; }

apply_secret postgres-secret \
  --from-literal=POSTGRES_PASSWORD="${DB_PASSWORD}"
apply_secret account-db-secret \
  --from-literal=ConnectionStrings__Default="Host=postgres;Port=5432;Database=accounts;Username=postgres;Password=${DB_PASSWORD}"
apply_secret transaction-db-secret \
  --from-literal=ConnectionStrings__Default="Host=postgres;Port=5432;Database=transactions;Username=postgres;Password=${DB_PASSWORD}"
apply_secret auth-db-secret \
  --from-literal=ConnectionStrings__Default="Host=postgres;Port=5432;Database=users;Username=postgres;Password=${DB_PASSWORD}"
apply_secret jwt-secret \
  --from-literal=Jwt__Secret="${JWT_SIGNING_KEY}"
apply_secret bank-jwt-credential \
  --from-literal=kongCredType=jwt \
  --from-literal=key=auth-service \
  --from-literal=algorithm=HS256 \
  --from-literal=secret="${JWT_SIGNING_KEY}"
kubectl label secret bank-jwt-credential konghq.com/credential=jwt --overwrite >/dev/null

echo "==> 4/6  Build images"
docker build -t "${ACCOUNT_TAG}"     src/account-service
docker build -t "${TRANSACTION_TAG}" src/transaction-service
docker build -t "${AUTH_TAG}"        src/auth-service
docker build -t "${FRONTEND_TAG}"    src/frontend

echo "==> 5/6  Import images into the cluster"
k3d image import -c "${CLUSTER}" \
  "${ACCOUNT_TAG}" "${TRANSACTION_TAG}" "${AUTH_TAG}" "${FRONTEND_TAG}"

echo "==> 6/6  Apply manifests"
kubectl apply -f k8s/
for d in postgres account-service transaction-service auth-service frontend; do
  kubectl rollout status "deployment/${d}" --timeout=120s
done

cat <<EOF

Done. The app is at  http://localhost:8000

  Register:  curl -X POST localhost:8000/auth/register -H 'Content-Type: application/json' -d '{"username":"me","password":"pw123456"}'
  Log in:    curl -X POST localhost:8000/auth/login    -H 'Content-Type: application/json' -d '{"username":"me","password":"pw123456"}'

Tear down with:  ./scripts/teardown.sh
EOF
