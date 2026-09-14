# SimBank — a microservices banking demo on Kubernetes

A simulated retail-banking application — **log in, view accounts, transfer funds** — built as
independent C# microservices, running on a local Kubernetes cluster, with **Kong** as the API
gateway enforcing JWT authentication and rate limiting.

---

## What this project demonstrates

| Skill | Where it shows up |
|---|---|
| **Microservice decomposition** | Four services (`auth`, `account`, `transaction`, `frontend`), each owning one responsibility and its own data store |
| **Inter-service communication** | `transaction-service` calls `account-service` over in-cluster DNS to move funds; failure handling with a compensating action |
| **Container orchestration** | Deployments, Services, ReplicaSets, rolling updates, a StatefulSet-style Postgres with a PersistentVolumeClaim — all on local Kubernetes (k3d) |
| **API gateway pattern** | Kong as the single north-south entry point: path-based routing, JWT validation, and rate limiting applied at the edge so the services carry **zero auth code** |
| **Stateless JWT authentication** | `auth-service` issues HS256 tokens; Kong verifies them by matching the `iss` claim to a credential; the trust chain works across services that never issued the token |
| **Persistence & migrations** | EF Core + PostgreSQL, schema managed by code-first migrations applied automatically on pod startup |
| **Secrets management** | Every password and signing key lives in a Kubernetes Secret, injected as an environment variable — nothing sensitive is committed to Git |
| **Infrastructure as code** | The entire stack comes up from `./scripts/deploy.sh` on a clean checkout |

---

## Architecture

```
                          ┌───────────────────────────────┐
        Browser  ───────► │   Angular SPA  (nginx pod)     │
                          └───────────────┬───────────────┘
                                          │  HTTP  (same origin — no CORS)
                                          ▼
                          ┌───────────────────────────────┐
                          │        Kong API Gateway        │   ◄── north-south traffic:
                          │  routing · JWT auth · rate      │       one entry point for
                          │  limiting  (DB-less mode)       │       everything external
                          └───┬───────────┬───────────┬────┘
                    /auth     │  /accounts │  /transfers│      (/ → SPA)
              ┌───────────────▼┐  ┌────────▼───────┐  ┌─▼──────────────────┐
              │  auth-service  │  │ account-service│  │ transaction-service │
              │  register/login│  │ balances,      │  │ performs transfers, │
              │  issues JWT    │  │ debit / credit │◄─┤ records history      │
              └───────┬────────┘  └───────┬────────┘  └──────────┬──────────┘
                      │                   │   ▲                  │      east-west traffic:
                      │                   │   └──────────────────┘      transaction-service
                      ▼                   ▼                             calls account-service
              ┌──────────────┐   ┌──────────────┐   ┌──────────────┐
              │  users  DB   │   │ accounts  DB │   │ transactions │
              └──────────────┘   └──────────────┘   └──────────────┘
                        one PostgreSQL pod  +  PersistentVolumeClaim

          Everything above runs inside a local Kubernetes cluster (k3d).
```

### The transfer flow, step by step

A `POST /transfers { fromAccountId, toAccountId, amount }` is not one operation — it spans two
services and at least four calls:

1. **Browser → Kong.** Kong checks the `Authorization: Bearer <jwt>` header: verifies the HS256
   signature and the `exp` claim. No token, or an invalid one → `401`, the request never reaches a
   service. Too many requests in a minute → `429`.
2. **Kong → `transaction-service`.** It validates the request (positive amount, distinct accounts).
3. **`transaction-service` → `account-service`** (`GET /accounts/{from}` and `GET /accounts/{to}`)
   to confirm both accounts exist, then checks the source balance is sufficient.
4. **`transaction-service` → `account-service`** (`POST /accounts/{from}/debit`), then
   (`POST /accounts/{to}/credit`). The balance arithmetic happens *inside* `account-service` — the
   one place that owns account state.
5. If the **credit fails after the debit succeeded**, `transaction-service` issues a compensating
   `credit` back to the source account and returns an error.
6. On success, `transaction-service` writes a row to its own `transactions` database and returns.

---

## Tech stack, and why

| Layer | Choice | Why |
|---|---|---|
| Language / runtime | **C# on .NET 10** | Lean footprint, first-class async, excellent tooling |
| Service style | **ASP.NET Core Minimal APIs** | Minimal ceremony — endpoints as expressions, ideal for small services |
| Data access | **EF Core + Npgsql** | Standard .NET ORM; teaches migrations and change tracking |
| Database | **PostgreSQL 17** | Free, ubiquitous, runs anywhere |
| Password hashing | **bcrypt** (`BCrypt.Net-Next`) | Adaptive work factor and per-user salt — never store or fast-hash a password |
| Tokens | **JWT (HS256)** issued by `auth-service` | Stateless auth — no server-side sessions; any holder of the secret can verify |
| Containers | **Docker**, multi-stage builds | SDK image to build, slim runtime image to run — small final images, no toolchain shipped |
| Local cluster | **k3d** (k3s in Docker) | Lightweight, fast to create/destroy, has a built-in load balancer |
| Package manager (k8s) | **Helm** | How Kong (and real-world charts) are installed |
| API gateway | **Kong Ingress Controller**, DB-less | The gateway skill this project exists to learn; DB-less keeps it stateless and config-as-code |
| Frontend | **Angular 22** (standalone, zoneless, signals) | Enterprise-standard SPA framework; served as static files behind nginx |

---

## Repository layout

```
.
├── src/
│   ├── account-service/       # accounts + balances; debit / credit endpoints
│   ├── transaction-service/   # transfer orchestration; transaction history
│   ├── auth-service/          # register / login; JWT issuance
│   └── frontend/              # Angular SPA (login, accounts, transfer)
├── k8s/                       # all Kubernetes manifests (Deployments, Services, Ingress, Kong CRDs)
├── scripts/
│   ├── deploy.sh              # one-shot: cluster + Kong + secrets + build + deploy
│   └── teardown.sh            # delete the cluster
├── PROGRESS.md                # build log (kept during development)
└── README.md
```

Each service follows the same internal structure: `Program.cs` (startup + endpoints),
`Models/` (EF entities), `Dtos/` (request/response records), `Data/AppDbContext.cs`,
`Migrations/`, a multi-stage `Dockerfile`.

---

## Run it yourself

### Prerequisites

| Tool | Version used | Install |
|---|---|---|
| Docker | 29.x | Docker Desktop |
| k3d | 5.9 | `brew install k3d` |
| kubectl | 1.36 | `brew install kubectl` |
| Helm | 4.x | `brew install helm` |
| .NET SDK | 10.0 | <https://dotnet.microsoft.com> |
| Node.js | 22+ | `brew install node` |

Docker must be running, and Docker Desktop should have **≥ 6 GB** of memory allocated (Kong +
4 services × 2 replicas + Postgres).

### Quick start

```bash
git clone <this-repo> && cd k8microservice
./scripts/deploy.sh
```

The script (idempotent — safe to re-run):

1. creates a k3d cluster `bankdev` with Traefik disabled and host port `8000` → cluster port `80`,
2. installs the Kong ingress controller via Helm (DB-less mode),
3. creates the six Kubernetes Secrets (DB password, three connection strings, the JWT signing key,
   and Kong's JWT credential) — generated locally, never committed,
4. builds the four container images and imports them into the cluster,
5. applies every manifest in `k8s/` and waits for the rollouts.

When it finishes, open **<http://localhost:8000>**.

> To use your own credentials: `JWT_SIGNING_KEY=… DB_PASSWORD=… ./scripts/deploy.sh`

### Verify the cluster is real

```bash
kubectl get pods                       # 2 replicas each of account/transaction/auth/frontend, 1 postgres
kubectl get ingress                    # bank-ingress-public and bank-ingress-protected, class "kong"
kubectl get kongplugin                 # jwt-auth, rate-limit

# unauthenticated request is rejected at the gateway
curl -i localhost:8000/accounts        # 401, Server: kong/...

# authenticate, then call through Kong
curl -s -X POST localhost:8000/auth/register -H 'Content-Type: application/json' \
  -d '{"username":"me","password":"pw123456"}'
TOKEN=$(curl -s -X POST localhost:8000/auth/login -H 'Content-Type: application/json' \
  -d '{"username":"me","password":"pw123456"}' | sed 's/.*"token":"\([^"]*\)".*/\1/')
curl -s localhost:8000/accounts -H "Authorization: Bearer $TOKEN"

# watch the databases while you transfer through the UI
kubectl exec deploy/postgres -- psql -U postgres -d accounts     -c 'SELECT * FROM "Accounts";'
kubectl exec deploy/postgres -- psql -U postgres -d transactions -c 'SELECT * FROM "Transactions";'
```

### Tear down

```bash
./scripts/teardown.sh        # deletes the cluster and the Postgres volume
```

### Local development (without rebuilding images)

Run a service directly against a throwaway Postgres:

```bash
docker run --name bank-pg -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres:17
cd src/account-service && dotnet run          # falls back to Host=localhost when the env var is absent
```

For the frontend, `cd src/frontend && npm start` serves on `:4200` and proxies `/auth`, `/accounts`,
`/transfers` to `localhost:8000` (start a Kong port-forward first if the cluster isn't mapping the
host port).

---

## How authentication works

`account-service` and `transaction-service` contain **no login code and no JWT libraries**. The
trust chain:

1. `auth-service` signs a token with a shared secret (`Jwt__Secret`), setting `iss: "auth-service"`.
2. That same secret is stored a second time as a **Kong JWT credential** (`bank-jwt-credential`),
   keyed by `auth-service`.
3. A request arrives at Kong with `Authorization: Bearer <token>`. Kong reads the `iss` claim
   (`auth-service`), looks up the credential with that key, and verifies the signature with its
   secret. Expired or tampered → `401`, and the request never reaches a service.
4. The `jwt-auth` and `rate-limit` Kong plugins are attached to `/accounts` and `/transfers` via an
   annotation on `bank-ingress-protected`. `/auth` and `/` are on `bank-ingress-public` (rate-limit
   only) — you can't require a token in order to *get* a token.

This is **symmetric** signing (HMAC) for simplicity. Production would use **asymmetric** (RS256):
`auth-service` signs with a private key that never leaves it, and everyone verifies with the public
key.

---

## What I'd do next

- **Link users to accounts.** Right now `auth-service` users and `account-service` accounts are
  unrelated — every logged-in user sees the same seeded accounts. `account-service` should create an
  account per user, keyed by the JWT `sub` claim.
- **Run it on OpenShift (OCP).** The same manifests on an enterprise Kubernetes distribution —
  Routes instead of raw Ingress, built-in image builds and registry, and the stricter security
  context constraints that a real platform team would enforce.
- **Istio for east-west traffic.** Today only north-south calls are authenticated, at Kong; a
  service mesh would add mTLS and per-service authorization policies between the services
  themselves, so `account-service` can verify *which* service is calling it.
- **Managed Postgres** (RDS / Cloud SQL / Supabase) instead of an in-cluster pod, and `policy: redis`
  for Kong's rate limiter so the count is shared across gateway replicas.
- **Observability** — OpenTelemetry tracing to see a transfer span across services, and structured
  logs shipped to a collector.
- **CI** — GitHub Actions to build, lint, run the transfer unit test, and push images to GHCR.
