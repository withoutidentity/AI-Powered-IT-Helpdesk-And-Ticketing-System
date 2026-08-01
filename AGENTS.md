# AGENTS.md — Instructions for AI Coding Assistants

This file is read by AI coding agents (Claude Code, Cursor, GitHub Copilot, Windsurf,
etc.) working in this repository. It is the entry point — read it before touching any
code, and follow it alongside `docs/PROJECT_PLAN.md`, `docs/API_SPEC.md`, and
`docs/ER_DIAGRAM.md`.

---

## 1. Project context (short version)

AI-Powered IT Helpdesk & Ticketing system. Angular frontend, ASP.NET Core (.NET 10 LTS)
backend using Clean Architecture + CQRS/MediatR, PostgreSQL + pgvector, Groq API for
chat/embeddings, Discord webhooks for ticket notifications. Full details in
`docs/PROJECT_PLAN.md`.

---

## 2. Mandatory: log every AI-assisted change

**Before considering any task complete, append an entry to
`docs/ai-changelog/AI_CHANGELOG.md`** describing what was done and, importantly, *why
that approach was chosen over alternatives*. This is a hard requirement, not optional
documentation — it's how a human reviewer (or a future agent) understands the reasoning
behind a change without re-deriving it from the diff alone.

Use this template for each entry (see the changelog file itself for the exact format
and a worked example):

```
## [YYYY-MM-DD] <short task title>

**Prompt/task summary:** what was asked
**Files changed:** list of files touched
**What changed:** plain description of the change
**Why this approach:** the reasoning — trade-offs considered, why this option over
  alternatives, any constraints from docs/PROJECT_PLAN.md that drove the decision
**Alternatives considered:** (if any) briefly, and why they were rejected
**Follow-ups / risks:** anything a human should double-check
**Reviewed by human:** ☐ (leave unchecked — a human checks this box after review)
```

Keep entries scoped to one coherent unit of work per entry — don't batch unrelated
changes into a single log entry.

---

## 3. Ground rules for agents working in this repo

- **Stay inside Clean Architecture boundaries.** Domain has no dependencies. Application
  depends only on Domain + its own interfaces. Infrastructure implements Application
  interfaces. Api depends on Application, not Infrastructure directly (resolved via DI).
  If a change seems to require breaking this, stop and flag it in the changelog entry
  instead of quietly violating it.
- **Don't invent business rules.** If a requirement is ambiguous (e.g. an unspecified
  ticket priority default, an unclear status transition), pick the most reasonable
  option, state the assumption explicitly in the changelog entry, and proceed — don't
  block on it, but don't hide the assumption either.
- **Never remove or weaken a test to make it pass.** If a test seems wrong, fix the
  test's expectation deliberately and say so in the changelog entry — don't delete
  coverage silently.
- **Run tests before declaring a task done.** `dotnet test
  backend/HelpdeskTicketingSystem.slnx` for backend changes, `ng test` from
  `frontend/` for frontend changes. A task isn't finished if the suite is red.
- **Never commit secrets.** No API keys, connection strings, or webhook URLs in code —
  use configuration (`appsettings.*.json` placeholders + environment variables), and
  make sure anything sensitive is covered by `.gitignore`.
- **Match existing conventions** described in `docs/PROJECT_PLAN.md` §11 (Coding
  Standards & Best Practices) rather than introducing a new pattern for a single file.
- **Ask before large architectural changes** (new external dependency, new service in
  `docker-compose.yml`, changing the auth model, swapping the vector store) — these
  should be a deliberate human decision, not something an agent decides unilaterally
  mid-task. Small, locally-scoped implementation choices don't need to be escalated —
  just log the reasoning.
- **Keep changes scoped** to what was asked. Don't opportunistically refactor unrelated
  code in the same change — note it as a follow-up in the changelog instead.

---

## 3.1 RAG / KB teaching preference

When work touches the RAG pipeline, knowledge-base documents/chunks, embeddings, vector
columns/indexes, pgvector search, or retrieval quality, explain the concept and the code
step by step in Thai. The user explicitly wants to learn this area deeply, so do not only
implement the change: teach the why, the data flow, the schema choices, the trade-offs,
and how to test/debug it manually.
---

## 4. Where things live (quick reference)

| Need to... | Look at / put it in |
|---|---|
| Add a new use case (backend) | `backend/src/Application/<Feature>/Commands|Queries/` + handler + validator |
| Add a new entity | `backend/src/Domain/Entities/` + EF Core config in `backend/src/Infrastructure/Persistence/Configurations/` + migration |
| Call the AI provider | `backend/src/Infrastructure/Ai/` behind `IChatAiService` / `IEmbeddingService` — never call Groq directly from Application or Api |
| Add a new API endpoint | Thin controller action in `backend/src/Api/Controllers/` that dispatches a MediatR command/query |
| Add a new Angular feature | `frontend/src/app/features/<name>/{data-access,feature,ui}` |
| Update requirements/architecture | `docs/PROJECT_PLAN.md` |
| Update the API contract | `docs/API_SPEC.md` (and keep Swagger annotations in sync) |
| Update the schema | `docs/ER_DIAGRAM.md` + an EF Core migration, together, in the same change |
