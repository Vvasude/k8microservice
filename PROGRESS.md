# PROGRESS.md

Session log for the banking-microservices learning project. Read this at the start of every session.

---

## Current position

- **Phase:** 3 — Local Kubernetes with k3d
- **Step:** Phase 2 complete and committed (`e86ba12`, `da794d7`). Next: teach
  cluster/node/pod/deployment/service, then learner creates a k3d cluster, writes Deployment + Service
  manifests, imports the image, and applies them.

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

## Half-done / exact next action

- **Repo hygiene:** `src/account-service/publish/` build artifacts were committed in `da794d7`
  (compiled `AccountService` binary, `web.config`, etc.). `.gitignore` covers `bin/`+`obj/` but not
  `publish/`. Add `publish/` to `.gitignore` and `git rm --cached` those files.
- Phase 3: teach k8s objects, then learner runs `k3d cluster create`, writes Deployment + Service
  manifests, `k3d image import account-service:0.1`, `kubectl apply`, inspects with get/describe/logs.

## Open questions / things to revisit

- Phase 1 checkpoint was **skipped** by learner's choice. Revisit `record` vs `class` and `decimal` vs
  `double` reasoning later — both are common interview questions.
- Phase 2 checkpoint (explain each Dockerfile stage) also **skipped** by learner's choice.
- Learner tends to re-paste code rather than answer conceptual questions, and asks for files to be
  written for them (Dockerfile). Hold the line on the "learner writes the important code" rule — offer
  fill-in-the-blank skeletons rather than finished files, and keep nudging for explanations.
