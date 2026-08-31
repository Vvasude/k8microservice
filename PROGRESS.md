# PROGRESS.md

Session log for the banking-microservices learning project. Read this at the start of every session.

---

## Current position

- **Phase:** 0 — Orientation & environment
- **Step:** Toolchain verified; `.gitignore` written; about to make the first commit and run the Phase 0 checkpoint.

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
- Wrote `.gitignore` (covers .NET build output, local secrets, env files, IDE cruft, OS junk, k3d kubeconfig).

## Half-done / exact next action

- Make the first commit (`.gitignore`, `CLAUDE.md`, `PROGRESS.md`).
- Run the Phase 0 checkpoint questions (container vs pod; what Kong does for us).
- Then start Phase 1 — `account-service` (first C# Minimal API, in-memory accounts).

## Open questions / things to revisit

- None yet.
