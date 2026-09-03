# PROGRESS.md

Session log for the banking-microservices learning project. Read this at the start of every session.

---

- **Phase 4 COMPLETE** (checkpoint skipped by learner). transaction-service + account-service deployed
  to k3d; in-cluster transfer works via DNS name `http://account-service` (env `AccountService__BaseUrl`).
- **auth-service deferred to Phase 7** (learner's choice).

## Current position

- **Phase 10 IN PROGRESS.** `README.md` written (full §10 structure), `scripts/deploy.sh` +
  `scripts/teardown.sh` created (idempotent one-shot deploy: cluster w/ traefik disabled + port
  8000 mapped, helm Kong, 6 secrets, build+import 4 images, apply k8s/, wait rollouts).
- **Remaining for Phase 10:**
  1. Learner fills in the "What I learned" section of the README (left as a prompt).
  2. Test `./scripts/teardown.sh && ./scripts/deploy.sh` on a clean cluster (§10 requires it).
  3. Record 2-4 min demo video, add link to README.
  4. Optional: unit test on transfer logic (§7).
- Phase 9 (OpenTelemetry) optional. Phase 11 (GH Actions CI) optional stretch.

### Phase 8 outcome

- **Angular 22 SPA** at `src/frontend/` (standalone, ZONELESS — must use signals for async-updated
  state, plain props only for ngModel fields). Files: `auth.ts` (token in signal + localStorage),
  `api.ts`, `auth-interceptor.ts` (Bearer + 401->logout, skips /auth/*), `auth-guard.ts`,
  `login.ts` / `accounts.ts` / `transfer.ts` (transfer uses account dropdowns from GET /accounts +
  refreshes after). All API calls use RELATIVE urls (same-origin via Kong).
- `proxy.config.json` + start script `ng serve --proxy-config proxy.config.json` for local dev
  (proxies /auth /accounts /transfers -> localhost:8000).
- **Container:** `src/frontend/Dockerfile` multi-stage node:22-alpine build -> nginx:1.27-alpine.
  `src/frontend/nginx.conf` has SPA history fallback `try_files $uri $uri/ /index.html`.
  Build output is `dist/frontend/browser/`. Image `frontend:0.1`, 2 replicas.
- **k8s:** `k8s/frontend-deployment.yaml` + `frontend-service.yaml` (port 80->80).
- **Kong:** added `path: /` -> frontend:80 to `k8s/bank-ingress-public.yaml` (longest-prefix means
  /auth, /accounts, /transfers still win). Whole app served at http://localhost:8000/ via Kong.

### Known simplifications (README "what I'd do next")

- Users (auth-service) and Accounts (account-service) are NOT linked — every logged-in user sees the
  same 3 seeded accounts. Would key accounts by the JWT `sub` claim.
- Token in localStorage (XSS-exposed); httpOnly cookie is better.

### Phase 7 outcome

- **auth-service** (`src/auth-service/`, image `auth-service:0.1`, 2 replicas): `User` entity
  (Id/Username/PasswordHash), `AppDbContext` (DbSet<User>), own `users` database (auto-created by
  startup Migrate()). `POST /auth/register` (bcrypt hash via `BCrypt.Net-Next`, 409 if taken),
  `POST /auth/login` (bcrypt verify, 401 on fail, issues HS256 JWT via `System.IdentityModel.Tokens.Jwt`
  with claims sub/unique_name, iss=auth-service, aud=bank-api, 1h exp).
- Secrets: `auth-db-secret` (connection string), `jwt-secret` (`Jwt__Secret` signing key).
- **Kong JWT plugin** (`k8s/kong-jwt.yaml`): KongPlugin `jwt-auth` + KongConsumer `bank-api-consumer`
  + imperative labeled secret `bank-jwt-credential` (kongCredType=jwt, key=auth-service, algorithm=HS256,
  secret=<same as jwt-secret>). Kong matches token `iss` -> credential `key` -> verifies signature.
- **Kong rate-limiting plugin** (`k8s/kong-rate-limit.yaml`): `rate-limit`, 10/min, policy=local.
- **Ingress split**: `bank-ingress-public.yaml` (/auth, plugins: rate-limit),
  `bank-ingress-protected.yaml` (/accounts + /transfers, plugins: jwt-auth,rate-limit).
  Old combined `k8s/bank-ingress.yaml` deleted.
- Verified: no token -> 401, valid -> 200, bad sig -> 401, /auth public, 11th req/min -> 429.

### Run-book additions (README, Phase 10) — NOT in repo

- `kubectl create secret generic auth-db-secret --from-literal=ConnectionStrings__Default='Host=postgres;Port=5432;Database=users;Username=postgres;Password=...'`
- `kubectl create secret generic jwt-secret --from-literal=Jwt__Secret='<32+ byte key>'`
- `kubectl create secret generic bank-jwt-credential --from-literal=kongCredType=jwt --from-literal=key=auth-service --from-literal=algorithm=HS256 --from-literal=secret='<same key as jwt-secret>'` then `kubectl label secret bank-jwt-credential konghq.com/credential=jwt`
- JWT signing is symmetric (HMAC) for simplicity; production would use asymmetric (RS256) so the
  private key never leaves auth-service.

### Phase 6 outcome

- Traefik (k3d default ingress) disabled in-place: `kubectl -n kube-system delete helmchart traefik
  traefik-crd` (k3s ran helm-delete jobs that removed the deployment/CRDs). No cluster recreate.
- Kong installed via Helm: `helm repo add kong https://charts.konghq.com`; `helm install kong
  kong/kong -n kong --create-namespace --set ingressController.enabled=true --set env.database=off`
  (DB-less). Kong 3.9.3. Pod `kong-kong-*` 2/2 (proxy + controller). `kong-kong-proxy` is
  LoadBalancer :80/:443 — EXTERNAL-IP is k3d node IPs (not host-reachable), so reach Kong via
  `kubectl port-forward -n kong service/kong-kong-proxy 8000:80`.
- `k8s/bank-ingress.yaml`: Ingress `bank-ingress`, `ingressClassName: kong`,
  annotation `konghq.com/strip-path: "false"` (services expect full path), Prefix rules
  `/accounts` -> account-service:80, `/transfers` -> transaction-service:80.
- Verified: `curl localhost:8000/accounts`, `/accounts/1`, `POST /transfers`, `GET /transfers` all
  work through Kong; `/nonsense` returns Kong's `no Route matched` 404 with `Server: kong/3.9.3`.

### Run-book items (for README, Phase 10) — NOT in the repo

- `helm repo add kong https://charts.konghq.com && helm install kong kong/kong -n kong
  --create-namespace --set ingressController.enabled=true --set env.database=off`
- `kubectl -n kube-system delete helmchart traefik traefik-crd` (disable default ingress)
- `kubectl create secret generic postgres-secret --from-literal=POSTGRES_PASSWORD=...`
- `kubectl create secret generic account-db-secret --from-literal=ConnectionStrings__Default='Host=postgres;Port=5432;Database=accounts;Username=postgres;Password=...'`
- `kubectl create secret generic transaction-db-secret --from-literal=ConnectionStrings__Default='Host=postgres;Port=5432;Database=transactions;Username=postgres;Password=...'`
- Ideal fresh setup: `k3d cluster create bankdev --agents 1 --k3s-arg "--disable=traefik@server:*" -p "8000:80@loadbalancer"` (avoids the in-place Traefik removal and the port-forward).

### Phase 5 outcome

- **Postgres in k3d:** `k8s/postgres-{pvc,deployment,service}.yaml`. PVC `postgres-pvc` (1Gi,
  local-path), Deployment `postgres` (postgres:17, 1 replica, mounts PVC at
  /var/lib/postgresql/data), Service `postgres:5432`. Password via `postgres-secret`.
- **account-service:** `Account` is an EF entity (class, Models/); `Data/AppDbContext.cs`
  (DbSet<Account>); all 4 endpoints async EF Core; startup `Migrate()` + seed-if-empty.
  Image `account-service:0.3`, **2 replicas**. Connection string via Secret `account-db-secret`
  (env `ConnectionStrings__Default`, `Host=postgres;Database=accounts`). Migration
  `20260903144027_InitialCreate` in `src/account-service/Migrations/`.
- **transaction-service:** `Transaction` entity (Id, FromAccountId, ToAccountId, decimal Amount,
  Status, CreatedAt); `Data/AppDbContext.cs` (DbSet<Transaction>); `/transfers` now records a
  row on success; new `GET /transfers` returns history desc. Startup `Migrate()` (auto-creates
  the `transactions` DB). Image `transaction-service:0.2`, 2 replicas. Secret `transaction-db-secret`
  (`Host=postgres;Database=transactions`). Migration in `src/transaction-service/Migrations/`.
- **Local dev:** Docker container `bank-postgres` (postgres:17, port 5432) — keep running for
  local `dotnet run`. Services fall back to `Host=localhost;...` when the env var is absent.

### Known gotchas / polish items (for README "what I'd do next")

- Every replica runs `db.Database.Migrate()` on startup — EF's table lock makes it safe but the
  correct pattern is a migration Job / initContainer.
- `libgssapi_krb5.so.2` warning in account-service logs — Npgsql optional Kerberos libs missing
  from slim aspnet image; non-fatal (password auth). One-line apt install in Dockerfile silences it.
- `kubectl exec -it ...` swallows output when stdout isn't a TTY — drop `-it` for scripted queries.
- Postgres `Balance`/`Amount` columns are unbounded `numeric` — prod would use `numeric(18,2)`.
- README must document the `kubectl create secret ...` commands (secrets aren't in the repo).

## Platform decision (Phase 3)

- Learner asked about using Red Hat OpenShift (OCP) instead of k3d, since banks run OpenShift.
- **Decision: stay on k3d.** Cost adjudication: k3d is $0 with full cluster-admin; OpenShift Local (CRC)
  is also $0 but needs 16 GB+ RAM (32 GB comfortable) and is heavy for fast iteration; the Developer
  Sandbox has no cluster-admin (can't install Kong CRDs) and expires in 30 days; the cloud "60-day
  trial" / ROSA is NOT free — the subscription is free but AWS/Azure worker nodes bill ~$10-25/day.
- The Kubernetes concepts (pods, deployments, services, ingress, Helm, Kong, JWT) transfer ~1:1 to
  OpenShift. OpenShift-specific layer (`oc`, Routes vs Ingress, SecurityContextConstraints, Projects,
  BuildConfigs/ImageStreams) is a nameable add-on to learn later, and belongs in the README's
  "what I'd do next" section as a portability note.

## Learner profile (from Phase 0 assessment)

- **C# / .NET:** New to it — slow down on language concepts (records, async/await, DI, LINQ) as they arise.
- **Docker:** New to it — teach images, containers, Dockerfiles from scratch.
- **Kubernetes:** New to it — this is the primary learning goal; go deep on pods, deployments, services, manifests.
- **Toolchain:** Was mostly already installed; k3d and helm added via Homebrew.

## Environment facts

- macOS 26.6.2 on Apple Silicon (arm64).
- Docker Desktop Linux VM: 7.7 GiB RAM, 10 CPUs. May need a memory bump around Phase 6 (Kong + 3 services + Postgres).
- Versions: git 2.50.1, docker 29.7.2, dotnet 10.0.400, kubectl v1.36.1, k3d v5.9.0, helm v4.2.4.

## Completed this session

- Read `CLAUDE.md`, confirmed mentor role.
- Phase 0 assessment done (see learner profile above).
- Taught the four core concepts: container vs VM, Kubernetes / desired-state, microservices, API gateway (north-south vs east-west).
- Verified all six tools; installed k3d + helm.
- Wrote `.gitignore`. First commit made (`65da0d0`), then git identity configured and commit amended.
- Phase 0 checkpoint passed.
- **Phase 1 done:** `dotnet new web` → `src/account-service/` (project `AccountService`, net10.0).
  Learner wrote `Program.cs`: `Account` record (Id/Owner/decimal Balance), in-memory `List<Account>`,
  `GET /accounts` and `GET /accounts/{id}` (404 via `Results.NotFound`). Tested with curl, works.
- Taught: SDK vs runtime, `.csproj` anatomy, `Nullable`/`ImplicitUsings`, `launchSettings.json` (dev-only),
  builder/Build/Run two-phase, Minimal API `MapGet` + lambdas, `record` vs `class`, `decimal` vs `double`
  (+ `m` suffix), C# casing (PascalCase members / camelCase locals), `Results.Ok`/`NotFound`, LINQ `FirstOrDefault`.

- **Phase 2 done:** Learner wrote `src/account-service/Dockerfile` (multi-stage: `sdk:10.0 AS build`
  → `aspnet:10.0` runtime, csproj-first restore for layer caching, `EXPOSE 8080`) and `.dockerignore`.
  Built `account-service:0.1`, ran with `docker run --rm -p 8080:8080`, curl verified.
  Taught: image vs container, Dockerfile instructions, layer caching + instruction ordering, multi-stage.
  Hiccup: Docker Desktop was stopped at session start of day 2 — `open -a Docker` fixed it.

- **Phase 3 done:** k3d cluster `bankdev` (1 server + 1 agent, survives reboots). Learner wrote
  `k8s/account-service-deployment.yaml` (Deployment, apps/v1, 2 replicas, label wiring app=account-service,
  imagePullPolicy: IfNotPresent, containerPort 8080) and `k8s/account-service-service.yaml`
  (Service, v1, ClusterIP, port 80 -> targetPort 8080). `k3d image import account-service:0.1 -c bankdev`,
  applied both, verified self-healing (deleted a pod, ReplicaSet recreated it), reached it via
  `kubectl port-forward service/account-service 8080:80` + curl.
  Taught: manifest structure (apiVersion/kind/metadata/spec), Deployment->ReplicaSet->Pod, labels+selectors,
  ClusterIP vs LoadBalancer, port vs targetPort, in-cluster DNS name, k3d image import, port-forward as dev-only.
  Note: traefik (k3d built-in ingress) still running in kube-system — must disable in Phase 6 for Kong.
  Repo hygiene fixed: `publish/` removed from tracking, added to `.gitignore` + `.dockerignore`.

- **Phase 4 progress:**
  - account-service `:0.2` deployed to k3d, scaled to **1 replica** (in-memory drift sidestep;
    deployment manifest also set to replicas: 1). Restore to 2 after Phase 5 (DB).
  - account-service Program.cs: `Account` is now a **class** in `Models/`, `AmountRequest` record in
    `Dtos/`. Debit/credit endpoints written by learner.
  - transaction-service scaffolded at `src/transaction-service/`. Has: `Clients/AccountClient.cs`
    (typed HttpClient: GetAccountAsync/DebitAsync/CreditAsync), `Dtos/AccountDto.cs`,
    `Dtos/TransferRequest.cs`, `Program.cs` with `AddHttpClient<AccountClient>` + `POST /transfers`
    orchestration (validate → fetch both → check funds → debit → credit → compensating credit-back).
  - Taught: DI, typed HttpClient + IHttpClientFactory (socket exhaustion), async/await, config via
    env var, "each service owns its data; others call it over HTTP" (learner initially tried to put an
    accounts list in transaction-service — corrected).

  - transaction-service containerized (`transaction-service:0.1`, Dockerfile adapted from account-service),
    imported to k3d, deployed via `k8s/transaction-service-deployment.yaml` (2 replicas — stateless, so
    scaling is safe, unlike account-service) + `k8s/transaction-service-service.yaml` (ClusterIP 80->8080).
  - In-cluster transfer verified: `curl POST localhost:8090/transfers` (port-forward) -> status completed.

## Half-done / exact next action

- **Commit pending:** `src/frontend/` (34 files), `k8s/frontend-deployment.yaml`,
  `k8s/frontend-service.yaml`, `k8s/bank-ingress-public.yaml`, `PROGRESS.md`.
- **Phase 10 (priority):** write the full `README.md` per CLAUDE.md §10 (pitch + disclaimer,
  architecture diagram, tech stack + why, what it demonstrates, run-it-yourself commands tested on
  a clean checkout, demo video link, "what I learned / what I'd do next"). Consolidate the run-book
  items scattered through this file (helm install, all `kubectl create secret` commands, the ideal
  `k3d cluster create` flags). Then the learner records a 2-4 min demo showing the app + the cluster
  underneath (`kubectl get pods`, a request through Kong, a transfer succeeding).
- Phase 9 (OpenTelemetry tracing) is optional — offer, don't push.
- Phase 11 (GitHub Actions CI + GHCR) is optional stretch.

## The whole system now (for quick recall)

Browser -> Kong (:8000 via port-forward) -> {
  `/`         -> frontend  (nginx serving Angular SPA)
  `/auth`     -> auth-service   -> `users` DB          [rate-limited]
  `/accounts` -> account-service -> `accounts` DB      [JWT required, rate-limited]
  `/transfers`-> transaction-service -> `transactions` DB, calls account-service  [JWT required, rate-limited]
}
All in k3d cluster `bankdev`. Postgres 1 pod + PVC. Each app service 2 replicas.
Images: account-service:0.3, transaction-service:0.2, auth-service:0.1, frontend:0.1.

## Recurring issues with this learner (keep applying)

- Runs `git add .` + commit without reviewing the diff; has committed bad content 3x (publish/ dir,
  reverted Transaction.cs, ...). Always nudge `git diff --cached` before commits.
- New to C#: recurring friction with which file/project code goes in, namespace = folder path,
  `if` needs parens, variable name consistency. Editor frequently reverts/duplicates on paste —
  small support files (entities, DTOs, Service YAMLs) keep landing empty or with wrong names;
  fastest to just write those and have the learner own the endpoint/business logic.
- Frequently skips checkpoint questions and asks for files to be written. Hold the line on
  business logic; be generous with boilerplate.
- **Phase 7 — Auth & JWT:** build `auth-service` (register/login, password hashing, JWT issuance);
  secure account-service + transaction-service to require a valid JWT; enable Kong's JWT plugin
  and rate-limiting plugin; add `/auth` route to `bank-ingress.yaml`; test the full authenticated flow.

## Open questions / things to revisit

- Phase 1 checkpoint was **skipped** by learner's choice. Revisit `record` vs `class` and `decimal` vs
  `double` reasoning later — both are common interview questions.
- Phase 2 checkpoint (explain each Dockerfile stage) also **skipped** by learner's choice.
- Learner tends to re-paste code rather than answer conceptual questions, and asks for files to be
  written for them. Hold the line on the "learner writes the important code" rule — offer
  fill-in-the-blank skeletons rather than finished files, and keep nudging for explanations.
- Learner is genuinely new to C# — recurring friction with: which file/project code belongs in,
  namespace = folder path, `if` needs parentheses, `req.Amount` vs bare `amount`. Slow, concrete,
  one-file-at-a-time works best. Comment-skeleton with numbered TODO steps worked well for the
  transfer endpoint. Phase 4 checkpoint (trace a transfer + name failure points) still pending.
