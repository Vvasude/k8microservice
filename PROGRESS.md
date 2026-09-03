# PROGRESS.md

Session log for the banking-microservices learning project. Read this at the start of every session.

---

- **Phase 4 COMPLETE** (checkpoint skipped by learner). transaction-service + account-service deployed
  to k3d; in-cluster transfer works via DNS name `http://account-service` (env `AccountService__BaseUrl`).
- **auth-service deferred to Phase 7** (learner's choice).

## Current position

- **Phase:** 5 — Persistence. account-service now persists to PostgreSQL via EF Core, verified LOCALLY.
- Considered Supabase (hosted DB); learner agreed to stick with in-cluster Postgres for the k8s
  learning (PVC/Secret). README "what I'd do next" should note: swap for managed DB in production.
- **Done (local):** EF Core + Npgsql packages; `Data/AppDbContext.cs` (DbSet<Account>); DbContext
  registered with `UseNpgsql` + connection-string fallback; local Postgres in Docker
  (`bank-postgres`, postgres:17, db `accounts`, pw postgres, port 5432); `InitialCreate` migration
  applied; startup seed block (`db.Database.Migrate()` + seed if empty); all 4 endpoints rewritten
  to async EF Core (`FirstOrDefaultAsync` / `SaveChangesAsync`). Persistence verified via restart +
  direct psql query.
- **Next (cluster):**
  1. Add `.OrderBy(a => a.Id)` to GET /accounts (SQL has no inherent order), rebuild, commit.
  2. Deploy Postgres to k3d: Deployment + Service + **PersistentVolumeClaim** (teach PVC).
  3. Create k8s **Secret** for DB password; wire connection string into account-service via env.
  4. Build account-service:0.3, redeploy (startup Migrate() runs migration against cluster PG).
  5. Scale account-service back to **2 replicas** (safe now — shared DB).
  6. Give transaction-service its own DB + `transactions` table (records each transfer).

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

- See "Next (cluster)" steps under Current position.
- Commit pending: `src/account-service` (EF Core changes + `Migrations/` folder — that IS source, commit it).

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
