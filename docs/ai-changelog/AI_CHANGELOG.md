# AI Changelog

A running log of every AI-assisted/AI-agentic change made in this repository: what was
done, and — more importantly — **why that approach was chosen**. This exists so that any
change made by an AI coding agent is traceable and reviewable, the same way a thoughtful
PR description would be for a human contributor.

**Every AI agent must append an entry here (following the template below) before
considering a task complete.** See `AGENTS.md` for the full policy.

Newest entries at the top.

---

## [2026-07-30] Move backend into dedicated folder

**Prompt/task summary:** Clarify whether Docker must be run now and whether backend API
files such as `src` can live under a dedicated `backend` folder.

**Files changed:**
- `AGENTS.md`
- `docs/PROJECT_PLAN.md`
- `docs/ai-changelog/AI_CHANGELOG.md`
- `backend/Directory.Build.props`
- `backend/HelpdeskTicketingSystem.slnx`
- `backend/src/Api/Api.csproj`
- `backend/src/Api/Program.cs`
- `backend/src/Api/appsettings.json`
- `backend/src/Api/appsettings.Development.json`
- `backend/src/Api/Properties/launchSettings.json`
- `backend/src/Api/Controllers/HealthController.cs`
- `backend/src/Application/Application.csproj`
- `backend/src/Application/AssemblyReference.cs`
- `backend/src/Application/Common/Interfaces/IApplicationDbContext.cs`
- `backend/src/Application/Common/Interfaces/IChatAiService.cs`
- `backend/src/Application/Common/Interfaces/IEmbeddingService.cs`
- `backend/src/Application/Common/Interfaces/ITicketNotifier.cs`
- `backend/src/Domain/Domain.csproj`
- `backend/src/Domain/AssemblyReference.cs`
- `backend/src/Infrastructure/Infrastructure.csproj`
- `backend/src/Infrastructure/AssemblyReference.cs`

**What changed:** Moved the .NET solution, shared build props, and Clean Architecture
projects from the repository root into `backend/`. Updated project guidance in
`AGENTS.md` and backend/frontend architecture paths in `docs/PROJECT_PLAN.md` so future
work uses `backend/src/...` for .NET code and `frontend/src/app/...` for Angular code.

**Why this approach:** Keeping .NET code under `backend/` makes the repository layout
clearer now that an Angular workspace also exists at `frontend/`. Moving the solution
along with `src/` lets the solution file keep simple relative paths (`src/...`) and
avoids brittle references back to the repository root.

**Alternatives considered:** Leaving backend code at root `src/` would also work, but it
would make `src/` ambiguous in a full-stack repository because Angular has its own
`frontend/src`. Moving only `src/` while leaving the solution at the root was rejected
because it would require noisier `backend/src/...` project paths inside the solution.

**Follow-ups / risks:** Commands and documentation that mention backend paths should use
`backend/HelpdeskTicketingSystem.slnx`. Docker still only starts the pgvector database;
API/web containers are a later compose expansion.

**Reviewed by human:** ☐

---

## [2026-07-30] Scaffold phase 0 project structure

**Prompt/task summary:** Read the repository instructions, follow them strictly, and
start implementing the project from the documented plan.

**Files changed:**
- `Directory.Build.props`
- `.gitignore`
- `.env.example`
- `docker-compose.yml`
- `HelpdeskTicketingSystem.slnx`
- `src/Api/Api.csproj`
- `src/Api/Program.cs`
- `src/Api/appsettings.json`
- `src/Api/Controllers/HealthController.cs`
- `src/Application/Application.csproj`
- `src/Application/AssemblyReference.cs`
- `src/Application/Common/Interfaces/IApplicationDbContext.cs`
- `src/Application/Common/Interfaces/IChatAiService.cs`
- `src/Application/Common/Interfaces/IEmbeddingService.cs`
- `src/Application/Common/Interfaces/ITicketNotifier.cs`
- `src/Domain/Domain.csproj`
- `src/Domain/AssemblyReference.cs`
- `src/Infrastructure/Infrastructure.csproj`
- `src/Infrastructure/AssemblyReference.cs`
- `frontend/.editorconfig`
- `frontend/.gitignore`
- `frontend/.prettierrc`
- `frontend/.vscode/extensions.json`
- `frontend/.vscode/launch.json`
- `frontend/.vscode/tasks.json`
- `frontend/angular.json`
- `frontend/package.json`
- `frontend/package-lock.json`
- `frontend/README.md`
- `frontend/tsconfig.json`
- `frontend/tsconfig.app.json`
- `frontend/tsconfig.spec.json`
- `frontend/public/favicon.ico`
- `frontend/src/index.html`
- `frontend/src/main.ts`
- `frontend/src/styles.scss`
- `frontend/src/app/app.config.ts`
- `frontend/src/app/app.html`
- `frontend/src/app/app.scss`
- `frontend/src/app/app.spec.ts`
- `frontend/src/app/app.ts`
- `frontend/src/app/app.routes.ts`

**What changed:** Added the initial backend solution and projects for the documented
Clean Architecture layers, wired project references according to the dependency rule,
removed the default WeatherForecast sample, and added basic health endpoints. Added
configuration placeholders, a gitignore, an environment example, and a PostgreSQL
pgvector-only docker compose skeleton. Created a strict standalone Angular workspace
with an initial helpdesk shell and matching component test.

**Why this approach:** The repository only contained planning documents, so the lowest
risk first implementation step was Phase 0 scaffolding from `docs/PROJECT_PLAN.md`
rather than jumping into business behavior. The backend targets `net8.0` centrally via
`Directory.Build.props` to match the plan and keep future projects consistent. The API
references `Application` but not `Infrastructure`, preserving the stricter boundary
stated in `AGENTS.md`; Infrastructure can later be composed deliberately once the DI
shape is agreed. The frontend uses Angular CLI-generated strict standalone defaults and
then replaces the placeholder UI with a domain-specific shell.

**Alternatives considered:** Adding MediatR, EF Core, Serilog, Swagger, and xUnit
projects immediately was deferred because it would broaden the first change and require
more external package restore decisions before any domain behavior exists. Creating a
full `api`, `web`, and `nginx` compose stack was also deferred; `docs/PROJECT_PLAN.md`
lists those services, but starting with the stateful pgvector database keeps local
infrastructure useful without inventing incomplete container builds.

**Follow-ups / risks:** `npm install` reported 3 moderate vulnerabilities and blocked
install scripts for several packages; review with `npm audit` and script approvals
before treating the frontend toolchain as production-ready. The backend currently has
no test projects because no business behavior has been implemented yet. The repository
git root is `D:/project`, so status output includes unrelated sibling projects unless
commands are scoped to this directory.

**Reviewed by human:** ☐

---

## [YYYY-MM-DD] <short task title>

**Prompt/task summary:** what was asked

**Files changed:**
- `path/to/file1`
- `path/to/file2`

**What changed:** plain description of the change

**Why this approach:** the reasoning behind it — trade-offs considered, why this option
over alternatives, any constraints from `docs/PROJECT_PLAN.md` that drove the decision

**Alternatives considered:** (if any) briefly, and why they were rejected

**Follow-ups / risks:** anything a human should double-check

**Reviewed by human:** ☐

---

## Example entry (for reference — delete once the first real entry is added)

## [2026-07-29] Scaffold ticket status transition validation

**Prompt/task summary:** "Add validation so a ticket can't move directly from `Open` to
`Closed`, skipping `InProgress`/`Resolved`."

**Files changed:**
- `src/Domain/Entities/Ticket.cs`
- `src/Domain/Exceptions/InvalidTicketTransitionException.cs`
- `src/Application/Tickets/Commands/UpdateTicketStatus/UpdateTicketStatusCommandHandler.cs`
- `tests/Domain.UnitTests/TicketTests.cs`

**What changed:** Added a `CanTransitionTo(TicketStatus target)` method on the `Ticket`
domain entity that encodes the allowed status graph
(`Open → InProgress → Resolved → Closed`, plus `Resolved → InProgress` for reopening).
`UpdateTicketStatusCommandHandler` now calls this before persisting and returns a `409
Conflict`-mapped domain exception on an invalid transition.

**Why this approach:** Put the rule on the `Ticket` entity itself (not just in the
handler) so the invariant holds no matter which code path mutates a ticket in the
future — this matches the Clean Architecture principle in `docs/PROJECT_PLAN.md` §8 of
keeping business rules in the Domain layer rather than scattered across handlers.

**Alternatives considered:** A database `CHECK` constraint enforcing transitions was
considered, but Postgres check constraints can't easily express "depends on the
*current* row value being updated" without a trigger, which would move business logic
out of C# and out of unit-test coverage. Rejected in favor of the domain-entity
approach, which is both testable and framework-agnostic.

**Follow-ups / risks:** None currently. If a "bulk reopen" admin feature is added later,
revisit whether the transition graph needs an `Admin` override path.

**Reviewed by human:** ☐
