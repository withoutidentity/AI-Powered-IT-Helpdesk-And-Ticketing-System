# Project Plan: AI-Powered IT Helpdesk & Ticketing System

**Status:** Draft (personal template — not tied to any employer's assignment)
**Owner:** Muhqi
**Last updated:** 2026-07-30

---

## 1. Project Overview

A web application that lets employees chat with an AI assistant about IT problems
(e.g. "How do I connect to the office Wi-Fi?", "I forgot my email password").
The assistant answers using **Retrieval-Augmented Generation (RAG)** over an internal
IT knowledge base. When a request needs human intervention, the system classifies the
message's **intent**, automatically opens a **ticket**, and notifies the IT team on
**Discord**.

This document is a reusable planning template for building the project end-to-end with
production-grade practices, using:

- **Frontend:** Angular
- **Backend:** .NET Core (ASP.NET Core Web API)
- **Database:** PostgreSQL + `pgvector`
- **AI Inference:** Groq API
- **Notifications/Automation:** Discord Webhooks

---

## 2. Goals & Objectives

| Goal | Why it matters |
|---|---|
| Ship a working chat → RAG → ticket pipeline | This is the core value proposition and the hardest technical piece |
| Demonstrate clean architecture on both FE and BE | Portfolio/learning value, maintainability |
| Demonstrate authorization boundaries (staff vs employee) | Realistic enterprise requirement |
| Full test coverage on business logic | Confidence + demonstrates engineering discipline |
| Document AI-assisted development decisions | Traceability when AI coding agents contribute code |

### Out of scope (v1)
- Multi-tenant / multi-organization support
- Native mobile app
- Real human-agent live chat handoff (tickets are async, not a live chat queue)
- Multi-language knowledge base (start with one language, design for i18n later)

---

## 3. User Roles & Personas

| Role | Description | Key permissions |
|---|---|---|
| **Employee** | Any staff member asking IT questions | Chat with AI, view **own** conversations & tickets |
| **IT Agent** | IT support staff | View/manage tickets assigned to them, view all tickets in their queue |
| **IT Admin** | Manages the knowledge base and users | Upload/reindex KB documents, manage users, view all tickets & dashboard stats |

---

## 4. Functional Requirements

### 4.1 Chat
- FR-1: Employee can start a new conversation and send messages.
- FR-2: Assistant responds with **streamed** tokens (perceived latency matters).
- FR-3: Each incoming message is classified into an intent: `Greeting`, `Question`, `Action`.
- FR-4: `Question` intent triggers RAG lookup against the knowledge base before answering.
- FR-5: `Action` intent (e.g. "printer is broken, send someone") creates a ticket automatically and replies confirming the ticket number.
- FR-6: Conversation history persists and is retrievable per user.

### 4.2 Knowledge Base (RAG)
- FR-7: IT manual documents are ingested **automatically** via an n8n workflow that watches a designated Google Drive folder — no manual admin upload UI in v1 (see §20.2).
- FR-8: New or updated Drive files trigger n8n to call a backend ingestion endpoint, which chunks, embeds, and stores the content as vectors in PostgreSQL (`pgvector`).
- FR-9: Re-ingesting a previously seen Drive file (matched by Drive file ID) **updates** its existing chunks rather than duplicating them.
- FR-10: Admin can view ingested documents, trigger re-indexing, and delete documents via the dashboard/API — management stays a backend responsibility even though ingestion is automated.
- FR-11: RAG retrieval returns top-k relevant chunks used as context for the LLM answer, and the API response includes which source document(s) were used (for traceability).

### 4.3 Ticketing
- FR-12: Tickets have a status lifecycle: `Open → InProgress → Resolved → Closed`.
- FR-13: A ticket is linked to the originating conversation/message for context.
- FR-14: IT Agents can view, comment on, and update the status of tickets.
- FR-15: On ticket creation, the **backend calls the Discord webhook directly** (not via n8n) with a summary + link, keeping this real-time path independent of n8n.
- FR-16: If a ticket remains `Open` longer than a configurable SLA threshold (default: 8 business hours), an n8n workflow escalates it to a separate team-lead Discord channel. Each ticket escalates **at most once per threshold crossing** (see §20.1).

### 4.4 Dashboard
- FR-17: IT staff can see a ticket list with filters (status, priority, date, assignee).
- FR-18: IT staff can see basic stats (open tickets, avg. resolution time, tickets by category).

### 4.5 Auth & Authorization
- FR-19: JWT-based login/registration.
- FR-20: Row-level authorization: employees only ever see their own conversations/tickets; IT staff see tickets per their scope; admins see everything.

---

## 5. Non-Functional Requirements

| Category | Requirement |
|---|---|
| **Performance** | Chat responses begin streaming in < 1.5s (excluding LLM cold-start); RAG query < 500ms for retrieval step |
| **Security** | JWT auth, hashed passwords (BCrypt/Argon2), input validation on every endpoint, rate limiting on chat + auth endpoints, secrets never committed to source control |
| **Scalability** | Stateless API (horizontally scalable), DB connection pooling, background jobs for embedding (don't block request thread) |
| **Reliability** | Discord webhook failures must not fail ticket creation (retry + dead-letter, not a hard dependency). n8n workflow failures (ingestion or SLA escalation) must not fail silently — configure an n8n error workflow that posts failures to a dedicated ops channel |
| **Maintainability** | Clean/Onion architecture, SOLID, ≥ 80% unit test coverage on Application/Domain layers |
| **Observability** | Structured logging (Serilog), correlation IDs per request, health check endpoints |
| **Portability** | Fully containerized via Docker Compose; runs identically on any dev machine or server |

---

## 6. Tech Stack & Rationale

| Layer | Choice | Why |
|---|---|---|
| Frontend | **Angular** (standalone components) | Strong typing, opinionated structure, good fit for enterprise-style dashboards + reactive chat UI |
| Frontend state | Angular Signals + RxJS | Signals for local/component state, RxJS for async streams (SSE, HTTP) |
| Frontend styling | Angular Material or Tailwind (pick one, don't mix design systems) | Consistent UI without hand-rolling components |
| Backend | **ASP.NET Core Web API (.NET 10 LTS)** | Mature, fast, first-class DI, great tooling for Clean Architecture; chosen over .NET 8 because it gives the project a longer LTS support window for new development |
| Backend pattern | Clean Architecture + CQRS (MediatR) | Separates business logic from framework/infra concerns; testable |
| ORM | Entity Framework Core | Migrations, LINQ, good Postgres support via Npgsql |
| Database | PostgreSQL + `pgvector` extension | One database for both relational data and vector search — avoids running a separate vector DB |
| AI inference | **Groq API** (OpenAI-compatible `/openai/v1` endpoint) | Very low-latency inference, good for a responsive chat UX; supports streaming and tool/function calling |
| Embeddings | Google AI Studio `gemini-embedding-001` via Gemini `embedContent`, shortened to 768 dimensions | Groq free-plan model access does not currently include embedding models for this project, and OpenAI API requires paid quota. Google AI Studio gives a practical free-tier path for development while keeping chat inference on Groq. The backend enforces a local embedding request limit of 2 requests/minute and 10 requests/day. |
| Notifications | Discord Webhooks | Simple, no bot hosting required, matches team's existing channel |
| Automation | **n8n** (self-hosted, Docker) | Owns two workflows where visual, decoupled automation genuinely earns its place: **SLA escalation** (schedule-based) and **knowledge-base ingestion from Google Drive** (event-based). See §20 for full detail and rationale. |
| Reverse proxy | Nginx | TLS termination, routing FE/BE, gzip |
| Containerization | Docker + Docker Compose | Reproducible local/prod parity |
| CI/CD | GitHub Actions | Free for public/private repos at this scale, good Docker + .NET + Angular support |
| Backend tests | xUnit + Moq + FluentAssertions | Standard .NET testing stack |
| Frontend tests | Jest (or Karma/Jasmine) + Angular Testing Library | Fast unit tests, component behavior testing |

> **Note on the original reference doc:** the sample "How to Build" notes used Supabase + n8n
> + Next.js. This plan substitutes Supabase → self-hosted Postgres/pgvector throughout.
> n8n went through one round-trip in this plan's history: it was initially dropped in
> favor of a direct backend → Discord webhook call for real-time ticket notifications
> (a single HTTP call didn't justify an extra orchestration service), then **reintroduced**
> once there were two concrete workflows — SLA escalation and Drive-based KB ingestion —
> where n8n's scheduling, execution history, and event-watching genuinely save custom
> code rather than just wrapping one webhook. Real-time ticket-creation notifications
> **still** go straight from the backend to Discord, not through n8n, to keep that
> critical path fast and independent of n8n's uptime. Full reasoning in §20.

---

## 7. System Architecture

```
                ┌────────────────────┐
                │   Angular SPA      │
                │  (Chat + Dashboard)│
                └─────────┬──────────┘
                          │ HTTPS / SSE
                ┌─────────▼──────────┐
                │       Nginx        │
                └─────────┬──────────┘
                          │
                ┌─────────▼──────────┐
                │  ASP.NET Core API  │
                │ (Clean Architecture│
                │   + CQRS/MediatR)  │
                └──┬───────┬─────────┘
                   │       │
      ┌────────────▼─┐   ┌─▼─────────────────┐
      │ PostgreSQL   │   │  Groq API (LLM + │
      │ + pgvector   │   │   Embeddings)    │
      └──────────────┘   └──────────────────┘
                   │
                   ▼
           ┌────────────────┐
           │ Discord Webhook│  ◄── direct, real-time ticket-created notification
           └────────────────┘

                (separately, not in the request path above)

  ┌──────────────┐  watches   ┌─────────────┐  calls ingest endpoint   ┌─────────────┐
  │ Google Drive │ ─────────► │     n8n     │ ───────────────────────► │ ASP.NET API │
  │ (IT manuals) │            │ (Docker)    │                          └──────┬──────┘
  └──────────────┘            │             │                                 │
                              │  + schedule │  polls SLA-breach endpoint      ▼
                              │    trigger  │ ◄─────────────────────  PostgreSQL/pgvector
                              │             │
                              │             │  posts escalation
                              └─────────────┴────────────► Discord (team-lead channel)
```

### Request flow: employee asks a question
1. Angular sends `POST /api/chat/conversations/{id}/messages` (or SSE stream request).
2. API persists the user message, calls Groq for **intent classification** (cheap, low-token prompt).
3. If `Question`: embed the query → vector similarity search in `document_chunks` → build context → call Groq chat completion with RAG context → **stream** tokens back to the client → persist assistant message.
4. If `Action`: create a `Ticket` record → fire-and-retry a Discord webhook call (background/queued, not inline with the response) → return a confirmation message referencing the ticket number.
5. If `Greeting`: short-circuit with a canned/LLM-generated greeting, no RAG needed.

### Request flow: SLA escalation (n8n, scheduled)
1. n8n's Schedule Trigger node runs periodically (e.g. every 15 minutes).
2. n8n calls `GET /api/v1/tickets/sla-breaches?thresholdHours=8` (service-authenticated).
3. For each ticket returned, n8n posts an escalation message to the team-lead Discord channel, then calls `POST /api/v1/tickets/{id}/escalate` to record `escalatedAt` on the ticket so it isn't re-escalated on the next run.
4. Details: §20.1.

### Request flow: knowledge-base ingestion (n8n, event-driven)
1. n8n's Google Drive Trigger node watches the configured IT-manuals folder for new/updated files.
2. On a match, n8n downloads the file and calls `POST /api/v1/kb/documents/ingest` (service-authenticated) with the file content and Drive file ID.
3. The backend chunks, embeds through the configured embedding provider, and upserts rows into `document_chunks`, keyed on Drive file ID so re-ingestion updates rather than duplicates.
4. Details: §20.2.

---

## 8. Backend Architecture (Clean Architecture)

```
backend/src/
├── Api/                          # Presentation layer
│   ├── Controllers/
│   ├── Middleware/               # Exception handling, correlation ID, request logging
│   ├── Filters/
│   ├── Extensions/               # DI setup, Swagger config, auth config
│   └── Program.cs
│
├── Application/                  # Use cases (framework-agnostic)
│   ├── Common/
│   │   ├── Interfaces/           # IChatAiService, IEmbeddingService, ITicketNotifier, IUnitOfWork
│   │   ├── Behaviors/            # MediatR pipeline: ValidationBehavior, LoggingBehavior
│   │   └── Models/                # Result<T>, PaginatedList<T>
│   ├── Chat/
│   │   ├── Commands/SendMessage/
│   │   └── Queries/GetConversation/
│   ├── Tickets/
│   │   ├── Commands/CreateTicket, UpdateTicketStatus/
│   │   └── Queries/GetTickets/
│   ├── KnowledgeBase/
│   │   ├── Commands/UploadDocument, ReindexDocument/
│   └── Auth/
│       ├── Commands/Register, Login/
│
├── Domain/                       # Enterprise business rules
│   ├── Entities/                 # User, Conversation, Message, Ticket, KnowledgeDocument, DocumentChunk
│   ├── Enums/                    # TicketStatus, MessageIntent, UserRole
│   ├── Exceptions/               # DomainException, NotFoundException
│   └── Events/                   # TicketCreatedEvent
│
├── Infrastructure/                # External concerns
│   ├── Persistence/
│   │   ├── AppDbContext.cs
│   │   ├── Configurations/       # EF Core Fluent API entity configs
│   │   ├── Repositories/
│   │   └── Migrations/
│   ├── Ai/
│   │   ├── GroqChatService.cs        # implements IChatAiService
│   │   ├── GoogleEmbeddingService.cs    # default IEmbeddingService
│   │   ├── OpenAiEmbeddingService.cs    # optional fallback provider
│   │   └── GroqEmbeddingService.cs      # optional fallback provider
│   ├── Notifications/
│   │   └── DiscordWebhookNotifier.cs # implements ITicketNotifier
│   └── Identity/
│       └── JwtTokenService.cs
│
tests/
├── Application.UnitTests/
├── Domain.UnitTests/
└── Api.IntegrationTests/         # WebApplicationFactory + Testcontainers (Postgres)
```

**Dependency rule:** `Api → Application → Domain`, and `Infrastructure → Application` (implements
its interfaces). Domain has zero dependencies on anything else. This is what makes the
Application/Domain layers unit-testable without a database or HTTP server.

---

## 9. Frontend Architecture (Angular)

```
frontend/src/app/
├── core/                         # Singleton, app-wide concerns
│   ├── auth/                     # AuthService, auth guard, JWT interceptor
│   ├── http/                     # error-handling interceptor, base API config
│   └── layout/                   # AppShell, Navbar, Sidebar
│
├── shared/                       # Reusable, presentational (dumb) components
│   ├── components/                # ButtonComponent, BadgeComponent, LoadingSpinner
│   ├── pipes/
│   └── directives/
│
├── features/
│   ├── chat/
│   │   ├── data-access/          # ChatService, ChatStore (signals-based state)
│   │   ├── feature/               # ChatPageComponent (smart)
│   │   └── ui/                    # MessageBubbleComponent, ChatInputComponent (dumb)
│   │
│   ├── tickets/
│   │   ├── data-access/
│   │   ├── feature/                # TicketListPageComponent, TicketDetailPageComponent
│   │   └── ui/
│   │
│   ├── dashboard/
│   │   ├── data-access/
│   │   └── feature/
│   │
│   └── auth/
│       ├── data-access/
│       └── feature/                # LoginPageComponent, RegisterPageComponent
│
├── app.routes.ts                  # Lazy-loaded routes per feature
├── app.config.ts
└── environments/
```

**Conventions:**
- **Smart vs. dumb components:** `feature/` components fetch data and hold state; `ui/`
  components are pure, `@Input`/`@Output` only, no service injection.
- **Data access layer per feature:** a thin service wrapping `HttpClient`, plus a signal-based
  store for local UI state. Keeps components free of business/data logic.
- **Standalone components** everywhere (no NgModules) — Angular's current recommended
  approach, smaller bundles via better tree-shaking.
- **Strict TypeScript** (`strict: true` in `tsconfig.json`) — no implicit `any`.

---

## 10. Data & AI Pipeline Design

### 10.1 Ingestion (offline / admin-triggered)
1. Admin uploads a document via `/api/kb/documents`.
2. Backend extracts text, splits into chunks (~500–800 tokens, with overlap).
3. Each chunk is sent through the configured embedding provider (`IEmbeddingService`; Google `gemini-embedding-001` by default); the resulting vector is stored in
   `document_chunks.embedding` (`vector` column via `pgvector`).
4. An index (`ivfflat` or `hnsw`, depending on pgvector version) is built on the embedding
   column for fast approximate nearest-neighbor search.

### 10.2 Query time
1. User message → intent classification (single fast LLM call, low max-tokens, JSON-mode
   response: `{"intent": "Question"}`).
2. If `Question`: embed the user's message → `SELECT ... ORDER BY embedding <=> $1 LIMIT k`
   (cosine distance) → top-k chunks become the context block in the prompt.
3. Compose a system prompt that instructs the model to answer **only** from the provided
   context, and to say it doesn't know rather than hallucinate.
4. Call Groq chat completion with `stream: true`; forward chunks to the client via
   **Server-Sent Events (SSE)**.

Current implementation note: `GET /api/v1/kb/search` performs the query-embedding and pgvector retrieval step for ITAgent/ITAdmin users. Chat send-message now reuses the same search service after explicit approval to send employee chat text to the configured embedding provider. The current chat response is non-streaming and Groq-generated from retrieved KB context, with a retrieval-only fallback when the chat provider fails. Persisted source citations and streaming remain later slices.

### 10.3 Why not a separate vector DB
Keeping vectors inside Postgres avoids operating a second stateful service, keeps
transactions consistent (a ticket + its embeddings context live in the same DB), and is
enough for the expected data volume of an internal IT knowledge base. If retrieval volume/
scale later demands it, this can be swapped for a dedicated vector store behind the same
`IEmbeddingService`/repository interfaces without touching Application-layer code.

### 10.4 Duplicate / overlapping KB documents (planned)

The system can ingest multiple KB documents that discuss the same issue, and this is normal
for an internal knowledge base. Duplicate or overlapping Wi-Fi, printer, VPN, or password
reset guides should not break RAG, but they can waste context tokens or confuse the LLM if
old and new instructions disagree.

Planned follow-up for retrieval quality:
- Add KB document metadata such as `category`, `source`, `version`, `effective_from`,
  `is_active`, and `priority`.
- Search only active documents by default, so old policies can be retained for audit/debugging
  without being used as answer context.
- Prefer authoritative/high-priority/newer documents during retrieval or reranking.
- Add duplicate control at search time, for example limiting chunks per document and removing
  near-identical chunks from the final top-k context.
- Use existing `content_hash` for exact duplicate detection, and vector similarity/reranking
  later for near-duplicate content that is phrased differently.

This is intentionally deferred until after the first semantic search/RAG slice, because the
project first needs measurable retrieval results before tuning duplicate handling.

---
## 11. Coding Standards & Best Practices

### Backend (.NET)
- Follow **SOLID**; one reason to change per class.
- **CQRS via MediatR**: every use case is a `Command`/`Query` + `Handler`. Controllers stay thin — they only map HTTP ↔ MediatR requests.
- **FluentValidation** for all command/query input validation, run automatically via a MediatR `ValidationBehavior`.
- **Result pattern** (`Result<T>` / `Result`) for expected failures (e.g. "ticket not found") instead of throwing exceptions for control flow. Reserve exceptions for truly exceptional cases.
- **Global exception-handling middleware** maps unhandled exceptions to a consistent `application/problem+json` response (RFC 7807).
- **DTOs at the API boundary** — never expose EF Core entities directly in responses; map with a lightweight mapper (Mapster/AutoMapper or manual extension methods).
- **Async all the way** — no `.Result`/`.Wait()` blocking calls.
- **Nullable reference types enabled** (`<Nullable>enable</Nullable>`).
- Configuration via `IOptions<T>` pattern, strongly typed settings classes, secrets from environment variables / user-secrets / a secret manager — never hard-coded.
- **Structured logging** with Serilog; log with semantic properties, not string concatenation. Include a correlation/request ID on every log line.
- API versioning from day one (`/api/v1/...`), even with a single version, to avoid breaking clients later.

### Frontend (Angular)
- **Strict typing everywhere**; avoid `any`. Define interfaces for every API DTO shape (generate from the backend's OpenAPI spec where possible to avoid drift).
- **Reactive forms** over template-driven forms for anything beyond a single field.
- **`async` pipe** in templates rather than manual `.subscribe()` + manual unsubscribe wherever practical; use `takeUntilDestroyed()` for the rest.
- **HTTP interceptors** for: attaching the JWT, centralized error handling/toasts, and loading-state tracking.
- **OnPush change detection** on presentational components.
- **Lazy-loaded routes** per feature to keep the initial bundle small.
- ESLint + Prettier enforced via a pre-commit hook (see §12).

### Cross-cutting
- **Conventional Commits** (`feat:`, `fix:`, `chore:`, `refactor:`, `test:`, `docs:`) for a readable history and to enable auto-changelog generation later.
- **Trunk-based / short-lived feature branches**, PRs required even for a solo project (self-review habit), PR template with a checklist (tests added, docs updated, lint passes).
- No secrets in git — `.env` files gitignored, `.env.example` committed with placeholder values.

---

## 12. Testing Strategy

| Layer | Tool | What's covered |
|---|---|---|
| Domain | xUnit | Entity invariants, value objects |
| Application | xUnit + Moq/NSubstitute + FluentAssertions | Command/query handlers, validators — mock all `Application.Common.Interfaces` |
| Infrastructure/API (integration) | xUnit + `WebApplicationFactory` + Testcontainers (real Postgres in Docker) | End-to-end request → DB round trip, auth flows, RLS-equivalent authorization checks |
| Frontend unit | Jest + Angular Testing Library | Components (dumb + smart), services, pipes |
| Frontend E2E (optional, later) | Playwright | Critical user journeys: login → ask question → get answer, staff → resolve ticket |

**Conventions:**
- AAA pattern (Arrange/Act/Assert) in every test.
- One behavior per test; descriptive test names: `MethodName_StateUnderTest_ExpectedBehavior`.
- Cover both positive and negative scenarios for every command/query (mirrors the evaluation criteria from typical assignment rubrics — build the habit regardless of context).
- Target ≥ 80% line coverage on `Application` + `Domain`; coverage on `Infrastructure`/`Api` is a secondary signal, not a target to chase for its own sake.
- CI fails the build if coverage drops below the configured threshold.

---

## 13. Security Considerations

- Passwords hashed with BCrypt/Argon2, never logged.
- JWT: short-lived access token + refresh token rotation; tokens signed with a secret from environment configuration (or an asymmetric key pair if issuing tokens other services must verify).
- Authorization enforced **server-side** on every query (never trust a client-supplied `userId`/`hospitalId`/`departmentId` — always derive scoping from the authenticated principal).
- Rate limiting on `/api/auth/*` and `/api/chat/*` (ASP.NET Core built-in rate limiting middleware).
- Input validation on every endpoint (FluentValidation) — reject before it reaches business logic.
- CORS locked down to the known frontend origin(s).
- Discord webhook URL treated as a secret (not a normal config value) — rotate if ever leaked.
- Dependency scanning (Dependabot / `dotnet list package --vulnerable`, `npm audit`) in CI.

---

## 14. Observability

- **Serilog** with structured JSON output (console sink locally, file/aggregator sink in prod).
- Correlation ID middleware — generate/propagate `X-Correlation-Id` and include it in every log line for a request.
- **Health checks**: `/health/live` (process is up) and `/health/ready` (DB + Groq API reachability) — used by Docker/orchestrator.
- Basic metrics worth tracking even in v1: chat response latency, RAG retrieval latency, ticket-creation success rate, Discord webhook failure rate.

---

## 15. CI/CD (GitHub Actions)

Suggested pipeline stages, split into two workflows (backend, frontend), both required to pass before merge:

**Backend workflow**
1. Restore & build (`dotnet build backend/HelpdeskTicketingSystem.slnx`)
2. Lint/format check (`dotnet format --verify-no-changes`)
3. Unit + integration tests (`dotnet test backend/HelpdeskTicketingSystem.slnx`, integration tests spin up Postgres via Testcontainers or a service container)
4. Build & push Docker image (on merge to `main`, tagged with git SHA)

**Frontend workflow**
1. Install deps (`npm ci`)
2. Lint (`ng lint`)
3. Unit tests (`ng test --watch=false --browsers=ChromeHeadless`)
4. Build (`ng build --configuration production`)
5. Build & push Docker image (on merge to `main`)

**Deploy stage (optional v1.1):** pull latest images on the target host and run
`docker compose up -d` — keep this manual/scripted at first; add a proper CD trigger once
the app is stable.

---

## 16. Docker & Local Development

`docker-compose.yml` services:

| Service | Purpose |
|---|---|
| `db` | `pgvector/pgvector:pg16` image - Postgres with the extension preinstalled, published to host port `5433` for local tools |
| `pgadmin` | Browser-based PostgreSQL administration UI for local inspection at `http://localhost:5050` |
| `api` | ASP.NET Core backend |
| `web` | Angular app (built static files served by its own lightweight Nginx, or served via the main `nginx` service) |
| `nginx` | Reverse proxy: routes `/api/*` to `api`, everything else to `web`; TLS termination in prod |

For local database inspection, start `db` and `pgadmin`, then sign in to pgAdmin with
the `PGADMIN_DEFAULT_EMAIL` / `PGADMIN_DEFAULT_PASSWORD` values from `.env`. Register a
server using host `db`, port `5432`, database `helpdesk`, username `helpdesk`, and the
configured `POSTGRES_PASSWORD`. Desktop database clients running on the host machine should
use host `localhost` and port `5433` to avoid collisions with any local PostgreSQL service.

See `docs/PROJECT_PLAN.md` section 6 for why n8n was intentionally left out of this compose
stack for v1.

---

## 17. Environment Configuration

Real `.env` files are gitignored and must never be committed. Commit only `.env.example`
files with non-secret placeholders.

Root `.env.example` is for Docker Compose:

```
POSTGRES_USER=helpdesk
POSTGRES_PASSWORD=<generate-a-local-postgres-password>
POSTGRES_DB=helpdesk
PGADMIN_DEFAULT_EMAIL=<your-local-pgadmin-email@example.com>
PGADMIN_DEFAULT_PASSWORD=<generate-a-local-pgadmin-password>
```

`backend/.env.example` is loaded by the API at startup during local development:

```
ConnectionStrings__Default=Host=localhost;Port=5433;Database=helpdesk;Username=helpdesk;Password=<same-as-root-POSTGRES_PASSWORD>
Cors__AllowedOrigins__0=http://localhost:4200
Jwt__Secret=<generate-a-long-random-secret-at-least-32-characters>
Jwt__AccessTokenMinutes=15
Jwt__RefreshTokenDays=7
Groq__ApiKey=<your-groq-api-key>
Groq__ChatModel=llama-3.3-70b-versatile
GoogleAI__ApiKey=<your-google-ai-studio-api-key>
Embedding__Provider=Google
Embedding__Model=gemini-embedding-001
Embedding__Dimensions=768
Embedding__RateLimit__RequestsPerMinute=2
Embedding__RateLimit__RequestsPerDay=10
Discord__WebhookUrl=<your-discord-webhook-url-or-leave-unset>
```

`frontend/.env.example` is only for public browser config. Do not put API keys, JWT
secrets, database passwords, or webhook URLs in frontend env files because Angular bundles
frontend config into JavaScript delivered to the browser.

```
NG_APP_API_BASE_URL=http://localhost:5175/api/v1
```

Production secrets are injected via the hosting platform's secret manager, not committed
anywhere. Docker Compose should require root `.env` values instead of falling back to
public default passwords.

---
## 18. Roadmap / Milestones

| Phase | Scope | Rough effort |
|---|---|---|
| **0. Setup** | Repo scaffolding, Docker Compose skeleton, CI skeleton, empty Clean Architecture solution + Angular workspace | 0.5–1 day |
| **1. Auth & Authorization** | Register/login, JWT, role-based guards (FE + BE) | 1–2 days |
| **2. Core data model** | EF Core entities/migrations for Users, Conversations, Messages, Tickets, KB docs/chunks | 1 day |
| **3. Chat happy path (no AI yet)** | Send/receive messages, persistence, named conversations, basic UI, no intent/RAG - proves the plumbing | 1-2 days |
| **4. AI integration** | Groq chat completion + streaming, intent classification | 1–2 days |
| **5. RAG pipeline** | Document upload, chunking, embeddings, vector search, grounded answers | 2–3 days |
| **6. Ticketing + Discord** | Auto ticket creation, ticket list/detail UI, status lifecycle, Discord webhook, dashboard | 1-2 days |
| **7. Hardening** | Tests to target coverage, rate limiting, logging/health checks, docs pass | 2–3 days |
| **8. Polish** | UI/UX pass, loading/error states, responsive layout | 1–2 days |

This is intentionally generous compared to a 3-day assignment timeline — it targets a
genuinely production-quality personal project rather than a minimum-viable demo.

---

## 19. AI-Assisted Development Policy

This project expects AI coding agents (Claude Code, Cursor, Copilot, etc.) to contribute
code. To keep that traceable and reviewable, every AI-assisted change must be logged. The
rules for agents live in **`AGENTS.md`** at the repo root, and the log itself lives in
**`docs/ai-changelog/AI_CHANGELOG.md`**. See both files for the exact process.

