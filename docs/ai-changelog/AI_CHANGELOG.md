## [2026-08-02] Add server-side knowledge document chunking

**Prompt/task summary:** Explain why KB content uses content hashes, then add a backend chunking service so admins can paste a full document and the server splits it into chunks automatically.

**Files changed:**
- `backend/src/Application/Common/Interfaces/IKnowledgeDocumentChunker.cs`
- `backend/src/Application/Common/Models/KnowledgeDocumentChunk.cs`
- `backend/src/Application/KnowledgeBase/Services/MarkdownKnowledgeDocumentChunker.cs`
- `backend/src/Application/KnowledgeBase/Commands/CreateKnowledgeDocument/CreateKnowledgeDocumentCommandHandler.cs`
- `backend/src/Application/DependencyInjection.cs`
- `backend/tests/Application.UnitTests/KnowledgeBase/KnowledgeDocumentHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ER_DIAGRAM.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added an application-level chunking abstraction and a Markdown-aware implementation. KB document creation now normalizes pasted content, computes a document hash, splits the content into ordered chunks, stores a chunk hash for each chunk, and returns all stored chunks in the create response.

**Why this approach:** Chunking belongs on the server so web UI paste, future file upload, future reindex jobs, and future admin tools all use the same policy. Markdown headings keep topic boundaries intact for RAG, while long-section windows with overlap reduce context loss near chunk boundaries.

**Alternatives considered:** Client-side chunking was rejected because each ingestion source would need to duplicate policy. Keeping one giant chunk was rejected because retrieval quality gets worse and embeddings become expensive/noisy. Exact model-token counting was deferred until the embedding model is finalized.

**Follow-ups / risks:** Add upload extraction for PDF/DOCX/Markdown files, tune chunk size and overlap with real Thai/English KB documents, use tokenizer-aware counts, and add reindex support for old one-chunk documents.

**Reviewed by human:** ?

## [2026-08-02] Add frontend knowledge base page

**Prompt/task summary:** Add a frontend Knowledge Base page where IT admins can paste text to create KB documents and IT agents/admins can view document lists and chunk details.

**Files changed:**
- `frontend/src/app/app.routes.ts`
- `frontend/src/app/features/knowledge-base/data-access/knowledge-base.models.ts`
- `frontend/src/app/features/knowledge-base/data-access/knowledge-base.service.ts`
- `frontend/src/app/features/knowledge-base/feature/knowledge-base-page.component.ts`
- `frontend/src/app/features/knowledge-base/feature/knowledge-base-page.component.html`
- `frontend/src/app/features/knowledge-base/feature/knowledge-base-page.component.scss`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the Knowledge Base placeholder route with a real page. The page lists KB documents, loads document detail with stored chunks, allows IT admins to create a document from pasted text, and blocks employee UI access with a restricted state.

**Why this approach:** This matches the current backend no-embed API and keeps the UI focused on validating document storage before adding upload parsing, chunk splitting, embeddings, and vector search. The create form stays Admin-only while IT agents can inspect documents for support context.

**Alternatives considered:** Adding file upload was deferred because the backend currently accepts JSON text only. Automatic client-side chunking was also deferred so chunking policy stays server-side and consistent for future ingestion sources.

**Follow-ups / risks:** The backend currently stores pasted content as a single chunk. Add a server-side chunking service next so long documents are split consistently before embedding.

**Reviewed by human:** ?
## [2026-08-02] Add knowledge base document APIs

**Prompt/task summary:** Add KB document APIs without embedding: list documents, get document detail, and create a document from text with Admin-only create permissions and IT staff read permissions.

**Files changed:**
- `backend/src/Api/Controllers/KnowledgeBaseController.cs`
- `backend/src/Application/Common/Interfaces/IKnowledgeDocumentRepository.cs`
- `backend/src/Application/Common/Interfaces/IDocumentChunkRepository.cs`
- `backend/src/Application/KnowledgeBase/Models/KnowledgeDocumentSummaryDto.cs`
- `backend/src/Application/KnowledgeBase/Models/KnowledgeDocumentDetailDto.cs`
- `backend/src/Application/KnowledgeBase/Models/DocumentChunkDto.cs`
- `backend/src/Application/KnowledgeBase/Queries/GetKnowledgeDocuments/*`
- `backend/src/Application/KnowledgeBase/Queries/GetKnowledgeDocumentDetail/*`
- `backend/src/Application/KnowledgeBase/Commands/CreateKnowledgeDocument/*`
- `backend/src/Infrastructure/Persistence/Repositories/KnowledgeDocumentRepository.cs`
- `backend/src/Infrastructure/Persistence/Repositories/DocumentChunkRepository.cs`
- `backend/src/CompositionRoot/DependencyInjection.cs`
- `backend/tests/Application.UnitTests/KnowledgeBase/KnowledgeDocumentHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `/api/v1/kb/documents` list/detail/create endpoints. Create accepts JSON text, stores a `Ready` document, and creates one initial chunk without embedding. IT admins can create documents; IT admins and IT agents can read documents; employees are denied by Application-layer authorization.

**Why this approach:** A JSON text endpoint lets the project validate the KB admin flow before file upload, PDF extraction, chunk splitting, and embeddings add more moving parts. Keeping authorization in Application handlers matches the existing Clean Architecture pattern and keeps permissions unit-testable without an HTTP server.

**Alternatives considered:** Multipart upload was deferred because this slice intentionally avoids extraction/parsing. Creating many chunks immediately was deferred until a real chunking policy is implemented and can be tested independently.

**Follow-ups / risks:** Add update/delete/reindex later, then add a dedicated chunking service before embeddings/vector search. The current `tokenCount` is a rough word-count estimate, not model-token accurate.

**Reviewed by human:** ?
## [2026-08-02] Add knowledge base data model

**Prompt/task summary:** Add the backend Knowledge Base foundation with `knowledge_documents` and `document_chunks`, EF Core migration, tests, and docs.

**Files changed:**
- `backend/src/Domain/Enums/KnowledgeDocumentStatus.cs`
- `backend/src/Domain/Entities/KnowledgeDocument.cs`
- `backend/src/Domain/Entities/DocumentChunk.cs`
- `backend/src/Infrastructure/Persistence/AppDbContext.cs`
- `backend/src/Infrastructure/Persistence/Configurations/KnowledgeDocumentConfiguration.cs`
- `backend/src/Infrastructure/Persistence/Configurations/DocumentChunkConfiguration.cs`
- `backend/src/Infrastructure/Persistence/Migrations/20260802080646_AddKnowledgeBaseModel.cs`
- `backend/src/Infrastructure/Persistence/Migrations/20260802080646_AddKnowledgeBaseModel.Designer.cs`
- `backend/src/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `backend/tests/Application.UnitTests/Domain/CoreDataModelTests.cs`
- `docs/API_SPEC.md`
- `docs/ER_DIAGRAM.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added domain entities and EF mappings for knowledge-base documents and ordered document chunks. Documents track source metadata, content hash, processing status, failure reason, and timestamps. Chunks track ordering, content, content hash, token count, and nullable embedding metadata. Added the EF migration and domain tests for document/chunk creation and status transitions.

**Why this approach:** This keeps the slice focused on the relational foundation needed before ingestion/RAG. The actual pgvector column is deferred because the project has not added pgvector EF mapping support yet and the embedding dimension should be verified against the chosen model before committing `vector(n)` to the schema.

**Alternatives considered:** Adding a `vector(768)` column immediately was considered, but rejected for this slice because the current backend package set does not include explicit pgvector mapping and a guessed dimension would create avoidable re-embedding risk later.

**Follow-ups / risks:** Next KB slices should add CRUD/ingestion APIs, text extraction/chunking, then pgvector package/migration plus vector search once the embedding model dimension is verified.

**Reviewed by human:** ?
## [2026-08-02] Polish ticket comments and permissions

**Prompt/task summary:** Strengthen ticket comment permission coverage and polish ticket detail comments/activity UX after the workflow slice.

**Files changed:**
- `backend/tests/Application.UnitTests/Tickets/TicketCommentCommandHandlerTests.cs`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added unit tests for employee, IT agent, and IT admin comment permissions. The ticket detail page now keeps comment/activity loading and mutation errors separate from the main ticket error, and it scrolls the comments list to the newest comment after posting.

**Why this approach:** The backend authorization rule already existed in the comment command handler, so adding focused unit coverage was lower risk than changing production policy. On the frontend, thread-specific errors avoid replacing the whole ticket detail state when only comments/activity fail.

**Alternatives considered:** Logging every comment as a ticket activity was considered, but left out for now so the activity feed stays focused on workflow events: ticket creation, assignment, and status changes.

**Follow-ups / risks:** If comment volume grows, the comments endpoint should add pagination rather than loading the full thread.

**Reviewed by human:** ?
## [2026-08-02] Fix ticket thread route wiring

**Prompt/task summary:** Fix 404s from ticket comments/activity requests and remove the Angular reactive-form disabled warning on the ticket assignment dropdown.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added the missing controller actions for listing ticket comments, creating a comment, and listing ticket activities. Updated the assignment dropdown so disabled/enabled state is managed through the reactive form control API instead of the template `disabled` attribute.

**Verification:** `dotnet test backend/HelpdeskTicketingSystem.slnx` passed 44 tests. `dotnet build backend/src/Api/Api.csproj` passed. `npm test -- --watch=false` passed 2 tests. `npm run build` passed. Local unauthenticated checks to `/api/v1/tickets/{id}/comments` and `/activities` now return 401, confirming the routes are registered instead of 404.
# AI Changelog

A running log of every AI-assisted/AI-agentic change made in this repository: what was
done, and - more importantly - **why that approach was chosen**. This exists so that any
change made by an AI coding agent is traceable and reviewable, the same way a thoughtful
PR description would be for a human contributor.

**Every AI agent must append an entry here (following the template below) before
considering a task complete.** See `AGENTS.md` for the full policy.

Newest entries at the top.

---

## [2026-08-02] Add ticket comments and activity log

**Prompt/task summary:** Add ticket comments, ticket detail activity logs, and tighten IT agent status updates so only the assigned agent can change status.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Application/Chat/Commands/CreateTicketFromMessage/CreateTicketFromMessageCommandHandler.cs`
- `backend/src/Application/Common/Interfaces/ITicketActivityRepository.cs`
- `backend/src/Application/Common/Interfaces/ITicketCommentRepository.cs`
- `backend/src/Application/Tickets/Commands/CreateTicketComment/*`
- `backend/src/Application/Tickets/Commands/UpdateTicketAssignment/UpdateTicketAssignmentCommandHandler.cs`
- `backend/src/Application/Tickets/Commands/UpdateTicketStatus/UpdateTicketStatusCommandHandler.cs`
- `backend/src/Application/Tickets/Models/TicketActivityDto.cs`
- `backend/src/Application/Tickets/Models/TicketCommentDto.cs`
- `backend/src/Application/Tickets/Queries/GetTicketActivities/*`
- `backend/src/Application/Tickets/Queries/GetTicketComments/*`
- `backend/src/CompositionRoot/DependencyInjection.cs`
- `backend/src/Domain/Entities/TicketActivity.cs`
- `backend/src/Infrastructure/Persistence/AppDbContext.cs`
- `backend/src/Infrastructure/Persistence/Configurations/TicketActivityConfiguration.cs`
- `backend/src/Infrastructure/Persistence/Migrations/*AddTicketActivities*`
- `backend/src/Infrastructure/Persistence/Repositories/TicketActivityRepository.cs`
- `backend/src/Infrastructure/Persistence/Repositories/TicketCommentRepository.cs`
- `backend/tests/Application.UnitTests/Chat/ChatCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketAssignmentCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketStatusCommandHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ER_DIAGRAM.md`
- `docs/ai-changelog/AI_CHANGELOG.md`
- `frontend/angular.json`
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`

**What changed:** Added ticket comments APIs/UI and ticket activity APIs/UI. Ticket creation, assignment changes, and status changes now write activity rows. The frontend loads comments and activity when a ticket is selected and refreshes activity after assignment/status mutations. IT agents can now update status only when the ticket is assigned to them; unassigned tickets can still be claimed first through assignment.

**Why this approach:** Comments and activity are separate records because both are append-only ticket history, not properties of the ticket itself. Activity is written in the same Application command that mutates ticket state, keeping audit logging close to the business action and in the same unit of work.

**Alternatives considered:** Storing activity as JSON on `tickets` was rejected because it would make ordering, filtering, and future pagination harder. Allowing IT agents to update unassigned ticket status was removed because assignment should define who owns the work.

**Follow-ups / risks:** Add dedicated tests for comment/activity query handlers if this area grows. Add activity pagination if ticket history becomes long.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add ticket assignment workflow

**Prompt/task summary:** Add ticket assignment so IT agents can assign tickets only to themselves, while IT admins can assign tickets to other agents through a dropdown.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Api/Controllers/UsersController.cs`
- `backend/src/Application/Common/Interfaces/IUserRepository.cs`
- `backend/src/Application/Tickets/Commands/UpdateTicketAssignment/*`
- `backend/src/Application/Users/Models/AgentDto.cs`
- `backend/src/Application/Users/Queries/ListAgents/*`
- `backend/src/Infrastructure/Persistence/Repositories/UserRepository.cs`
- `backend/tests/Application.UnitTests/Auth/AuthCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketAssignmentCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketQueryHandlerTests.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketStatusCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Users/ListAgentsQueryHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`

**What changed:** Added `PATCH /api/v1/tickets/{ticketId}/assignment` and `GET /api/v1/users/agents`. Assignment rules are enforced in the Application layer: employees cannot assign, IT agents can assign only themselves and only from unassigned/self-assigned tickets, and IT admins can assign tickets to IT agents. The Tickets page now shows `Assign to me` for eligible agents and an admin-only agent dropdown.

**Why this approach:** Assignment is a ticket use case, so the mutation lives under `Application/Tickets/Commands`, while the dropdown source is a user query. The backend returns the updated ticket detail so the frontend can update the selected detail and list row without a separate refetch.

**Alternatives considered:** Allowing unassign/reassign controls for everyone was deferred because the requested rule was intentionally narrower. Returning all users to the dropdown was rejected because only IT agents are valid assignees.

**Follow-ups / risks:** Add component tests for role-specific assignment controls once the Tickets page has dedicated frontend test coverage. Consider an admin unassign action later if queue management needs it.

**Reviewed by human:** [ ]

---
## [2026-08-01] Restrict ticket conversation deep links to owners

**Prompt/task summary:** Prevent ITAgent and ITAdmin users from clicking through to a ticket's linked conversation when they are not the owner of that conversation.

**Files changed:**
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The ticket detail Conversation field now renders as a router link only when the current user is the ticket creator/conversation owner. Other users who can view the ticket still see the conversation ID for context, but it is read-only and cannot navigate into that chat.

**Why this approach:** The backend remains the source of truth for conversation access, while the frontend no longer advertises an action that ITAgent/Admin users should not use when the chat belongs to another employee.

**Alternatives considered:** Hiding the conversation ID entirely was not chosen because agents and admins may still need traceability while reviewing a ticket.

**Follow-ups / risks:** If backend ticket detail later returns a dedicated `canOpenConversation` flag, the frontend should use that instead of deriving ownership from `createdBy.id`.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add frontend ticket status controls

**Prompt/task summary:** Continue the status workflow by wiring the frontend Tickets detail page to the backend status update endpoint.

**Files changed:**
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added a `PATCH /tickets/{id}/status` client method and a next-status action in Ticket detail. The UI shows the action only for `ITAgent` and `ITAdmin`, advances `Open -> InProgress -> Resolved -> Closed`, and synchronizes the selected detail and list item after a successful update.

**Why this approach:** A next-status button keeps the first workflow simple and prevents users from jumping to invalid states. Backend authorization and transition checks remain the source of truth; frontend role checks only keep the UI clean.

**Alternatives considered:** A free-form status dropdown was rejected for this slice because it exposes invalid choices that the backend would reject. Employee close/confirm-resolved controls remain deferred to a dedicated workflow slice.

**Follow-ups / risks:** Add assignment controls before agent-owned queues become more complex. Add frontend tests around role-specific rendering once the Tickets page has component-level test coverage.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket status workflow

**Prompt/task summary:** Implement the backend ticket status workflow slice before adding frontend status controls.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Application/Tickets/Commands/UpdateTicketStatus/*`
- `backend/src/Domain/Entities/Ticket.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketStatusCommandHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `PATCH /api/v1/tickets/{ticketId}/status` and an Application command that updates ticket status through the v1 lifecycle: `Open -> InProgress -> Resolved -> Closed`. IT admins can update any ticket; IT agents can update tickets assigned to them or unassigned queue tickets; employees cannot update status in this slice.

**Why this approach:** Status transition logic stays on the `Ticket` domain entity, while role/scope authorization stays in the Application command handler. This keeps business rules testable without HTTP or database dependencies and matches the existing Clean Architecture boundaries.

**Alternatives considered:** Letting employees close tickets immediately was deferred to a separate confirm-resolved/close-own-ticket slice because it changes the support workflow semantics. Allowing backward transitions or reopen was also deferred to keep the first lifecycle strict and predictable.

**Follow-ups / risks:** Frontend controls still need to call this endpoint. Reopen/escalation rules are future slices.

**Reviewed by human:** [ ]

---
## [2026-08-01] Link ticket detail back to chat conversation

**Prompt/task summary:** Make the Conversation field in ticket detail clickable so it opens the linked chat conversation.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The ticket detail Conversation context now renders as a router link to the Chat page with `conversationId` in the query string. The Chat page reads that query param after loading conversations and selects the matching conversation automatically.

**Why this approach:** Query params keep the existing Chat route simple while still making ticket-to-conversation navigation explicit and shareable. It avoids adding a new route shape before conversation detail routing is needed elsewhere.

**Alternatives considered:** Adding `/chat/conversations/:id` was deferred because the app currently uses the root Chat workspace route and only needs deep selection behavior for now.

**Follow-ups / risks:** If Chat later gets a dedicated conversation route, this link should move from query params to route params.

**Reviewed by human:** [ ]

---
## [2026-08-01] Fix chat ticket title and new conversation form layout

**Prompt/task summary:** Make tickets created from chat use the conversation title, and fix the New conversation form UI that expanded incorrectly in the conversation sidebar.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The chat-to-ticket handoff now sends the active conversation title as the ticket title instead of deriving it from the latest message. The conversation sidebar layout now uses flex column sizing so the New conversation form stays compact and the conversation list is the only section that grows and scrolls.

**Why this approach:** The conversation title is the user-defined issue name, so using it for the ticket title keeps chat and ticket records aligned. Flex column sizing is more robust than a fixed two-row grid when optional panels such as the creation form appear conditionally.

**Alternatives considered:** Keeping message-derived ticket titles was rejected because it can diverge from the user's explicit conversation name. Adding more grid rows was rejected because future conditional sidebar controls would recreate the same layout fragility.

**Follow-ups / risks:** Ticket details still show basic chat context only; richer context can be added when the backend exposes conversation title/message snippets directly.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add frontend tickets page and named conversation creation

**Prompt/task summary:** Continue after backend ticket list/detail APIs by adding the frontend Tickets page, and change Chat `New` conversation creation so the user can choose a conversation name.

**Files changed:**
- `frontend/src/app/app.routes.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/PROJECT_PLAN.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `/tickets` placeholder route with a real Tickets page that lists tickets, supports status/priority filters, opens detail in the same workspace, and displays title/status/priority/created metadata plus basic chat context via conversation/message IDs. The Chat page `New` action now opens a small title form before creating the conversation.

**Why this approach:** A split list/detail view matches the existing support-workspace layout and keeps repeated ticket triage efficient. For chat context, the frontend displays the IDs currently available from the backend detail contract instead of inventing another backend call in this slice.

**Alternatives considered:** A separate `/tickets/:id` page was deferred because the current app shell benefits from selecting tickets without route churn. Showing full originating message content was deferred until the backend exposes richer ticket context or a dedicated conversation summary endpoint.

**Follow-ups / risks:** Frontend status updates/comments are still future slices. Ticket detail can become richer once backend returns the originating message snippet or conversation title.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket list and detail APIs

**Prompt/task summary:** Implement the next backend slice for ticket list/detail APIs with status/priority filters and role-scoped authorization.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Api/Services/CurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ICurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ITicketRepository.cs`
- `backend/src/Application/Common/Interfaces/IUserRepository.cs`
- `backend/src/Application/Common/Models/PaginatedList.cs`
- `backend/src/Application/Common/Models/TicketListCriteria.cs`
- `backend/src/Application/Tickets/Models/TicketDetailDto.cs`
- `backend/src/Application/Tickets/Models/TicketSummaryDto.cs`
- `backend/src/Application/Tickets/Models/UserRefDto.cs`
- `backend/src/Application/Tickets/Queries/GetTicketDetail/*`
- `backend/src/Application/Tickets/Queries/GetTickets/*`
- `backend/src/Infrastructure/Persistence/Repositories/TicketRepository.cs`
- `backend/src/Infrastructure/Persistence/Repositories/UserRepository.cs`
- `backend/tests/Application.UnitTests/Auth/AuthCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Chat/ChatCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketQueryHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `GET /api/v1/tickets` and `GET /api/v1/tickets/{ticketId}`. Ticket list supports `status`, `priority`, `page`, and `pageSize`. The query handlers enforce role scope: employees see created tickets, IT agents see assigned plus unassigned queue tickets, and IT admins see all tickets. Responses include lightweight user references for creator and assignee.

**Why this approach:** Authorization stays in Application query handlers, matching the documented Clean Architecture boundary and keeping role behavior unit-testable without a database. The repository receives already-scoped criteria instead of reading HTTP claims or deciding product permissions itself.

**Alternatives considered:** Putting role checks in controllers was rejected because it would split business authorization across the API layer. Returning raw ticket entities was rejected because API DTOs keep response contracts explicit and prevent accidental field exposure.

**Follow-ups / risks:** Status update, comments, and frontend Tickets page are still separate slices. IT agent queue scope currently follows the documented v1 rule of assigned-to-me or unassigned tickets; if queues become department-based later, the criteria object can be extended.

**Reviewed by human:** [ ]

---
## [2026-08-01] Simplify chat ticket button template

**Prompt/task summary:** Check and fix the Chat page ticket button template after the user reported an error around the disabled/click bindings and conditional label.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `@if` block inside the ticket action button with a single interpolation expression that chooses between `Ticket created`, `Creating...`, and `Create ticket`.

**Why this approach:** The Angular compiler accepted the original template, but a simple interpolation is enough for this button label and is less likely to trigger editor/template tooling issues.

**Alternatives considered:** Keeping the `@if` block was valid Angular syntax, but it was more structure than this label needed.

**Follow-ups / risks:** None.

**Reviewed by human:** [ ]

---
## [2026-08-01] Expose conversation ticket state

**Prompt/task summary:** Fix the chat-to-ticket UX so refresh/selecting a conversation immediately shows that its ticket already exists, instead of requiring the user to click and receive a conflict.

**Files changed:**
- `backend/src/Application/Chat/Models/ConversationDto.cs`
- `backend/src/Application/Chat/Queries/GetConversations/GetConversationsQueryHandler.cs`
- `backend/src/Application/Common/Interfaces/ITicketRepository.cs`
- `backend/src/Infrastructure/Persistence/Repositories/TicketRepository.cs`
- `backend/tests/Application.UnitTests/Chat/ChatCommandHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`
- `frontend/src/app/features/chat/data-access/chat.models.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.ts`

**What changed:** Added `hasTicket` to conversation list responses and populated it from tickets linked by `conversation_id`. The Chat page now initializes and refreshes its header button state from `conversation.hasTicket`, and still marks the active conversation locally after successful ticket creation.

**Why this approach:** The UI should not rely on a failed create request to discover existing state. Returning ticket state with conversations keeps the conversation-level invariant visible to clients without requiring an extra request per selected conversation.

**Alternatives considered:** Calling the create endpoint and handling `409` as normal control flow was rejected because it creates a poor user experience and unnecessary failed writes. Adding a separate ticket-status endpoint was deferred because the conversation list already has the relevant scope.

**Follow-ups / risks:** When ticket detail/list endpoints exist, `hasTicket` can be expanded to include a `ticketId` so the header can link directly to the created ticket.

**Reviewed by human:** [ ]

---
## [2026-08-01] Make chat ticket handoff conversation-scoped

**Prompt/task summary:** Change ticket creation so each conversation can create only one ticket, and move the frontend `Create ticket` action from individual message bubbles to the chat panel header.

**Files changed:**
- `backend/src/Application/Chat/Commands/CreateTicketFromMessage/CreateTicketFromMessageCommandHandler.cs`
- `backend/src/Application/Common/Interfaces/ITicketRepository.cs`
- `backend/src/Infrastructure/Persistence/Repositories/TicketRepository.cs`
- `backend/tests/Application.UnitTests/Chat/ChatCommandHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ER_DIAGRAM.md`
- `docs/ai-changelog/AI_CHANGELOG.md`
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `frontend/src/app/features/chat/feature/chat-page.component.ts`

**What changed:** The backend duplicate check now uses `conversation_id` instead of `message_id`, enforcing one ticket per conversation. The frontend now renders a single conversation-level `Create ticket` button in the message panel header and creates the ticket from the latest user message in the active conversation.

**Why this approach:** A conversation-level handoff better matches the product workflow: a support conversation represents one issue context, while messages are just the supporting evidence. Keeping `message_id` still preserves traceability to the specific user message used as the trigger.

**Alternatives considered:** Keeping per-message ticket creation was rejected because one conversation could generate multiple duplicate tickets. Adding a separate conversation-only backend endpoint was deferred because the current endpoint already links both conversation and originating message and can enforce the conversation-scoped invariant.

**Follow-ups / risks:** Persisted UI awareness of existing tickets still needs a conversation/ticket summary endpoint so the header button can show `Ticket created` immediately after refresh instead of learning it from a `409` response.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add frontend chat-to-ticket handoff

**Prompt/task summary:** Continue the recommended Phase 3 flow by wiring the frontend Chat page to the backend create-ticket-from-message endpoint.

**Files changed:**
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`
- `frontend/src/app/features/chat/data-access/chat.models.ts`
- `frontend/src/app/features/chat/data-access/chat.service.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `frontend/src/app/features/chat/feature/chat-page.component.ts`

**What changed:** Added frontend ticket DTO/request types, a `ChatService.createTicketFromMessage` method, and a `Create ticket` action on user message bubbles. The component now handles per-message creating state, success feedback, duplicate-ticket conflict feedback, and hides the action for assistant messages.

**Why this approach:** This completes the manual chat-to-ticket handoff before AI intent classification. Keeping the action attached to the originating user message preserves context and maps directly to the backend endpoint that links tickets to `conversationId` and `messageId`.

**Alternatives considered:** Adding a full modal for title/priority editing was deferred because the current slice is about wiring the flow end to end. The default title/description from message content keeps the UI small while still exercising the real backend path.

**Follow-ups / risks:** Add ticket list/detail UI next so users can navigate to the created ticket. Later, AI intent classification can call the same backend flow automatically for `Action` messages.

**Reviewed by human:** [ ]

---
## [2026-08-01] Create tickets from chat messages

**Prompt/task summary:** Continue Phase 3 by adding backend ticket creation from a persisted chat message before AI intent/RAG automation.

**Files changed:**
- `backend/src/Api/Controllers/ChatController.cs`
- `backend/src/Application/Chat/Commands/CreateTicketFromMessage/CreateTicketFromMessageCommand.cs`
- `backend/src/Application/Chat/Commands/CreateTicketFromMessage/CreateTicketFromMessageCommandHandler.cs`
- `backend/src/Application/Chat/Commands/CreateTicketFromMessage/CreateTicketFromMessageCommandValidator.cs`
- `backend/src/Application/Chat/Models/TicketDto.cs`
- `backend/src/Application/Common/Interfaces/IMessageRepository.cs`
- `backend/src/Application/Common/Interfaces/ITicketRepository.cs`
- `backend/src/CompositionRoot/DependencyInjection.cs`
- `backend/src/Infrastructure/Persistence/Repositories/MessageRepository.cs`
- `backend/src/Infrastructure/Persistence/Repositories/TicketRepository.cs`
- `backend/tests/Application.UnitTests/Chat/ChatCommandHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added a manual handoff endpoint that creates an `Open` ticket from an existing user message, links it to the originating conversation and message, prevents duplicate tickets for the same message, and rejects assistant messages. Added repository support and unit tests for success, forbidden access, invalid assistant-message creation, and duplicate-ticket conflict.

**Why this approach:** This keeps Phase 3 focused on proving the chat-to-ticket plumbing without introducing AI intent classification yet. The endpoint models the same behavior that the future `Action` intent path will call automatically, while keeping the Application layer testable and the controller thin.

**Alternatives considered:** Creating tickets directly inside `SendMessageCommandHandler` was deferred because without AI classification every user message would risk creating a ticket. A separate manual endpoint keeps behavior explicit until the AI slice can decide intent.

**Follow-ups / risks:** Add ticket list/detail/status endpoints and frontend ticket handoff UI next. Discord notification should be introduced later with a notifier abstraction and should not block ticket creation.

**Reviewed by human:** [ ]

---
## [2026-08-01] Load local configuration from env files

**Prompt/task summary:** Move local backend/frontend configuration to folder-specific `.env` files and clarify how PostgreSQL password rotation works.

**Files changed:**
- `.env.example`
- `backend/.env.example`
- `backend/src/Api/Configuration/DotEnvLoader.cs`
- `backend/src/Api/Program.cs`
- `docs/PROJECT_PLAN.md`
- `docs/ai-changelog/AI_CHANGELOG.md`
- `frontend/.env.example`
- `frontend/package.json`
- `frontend/scripts/write-environment.mjs`
- `frontend/src/app/environments/environment.ts`

**What changed:** Added a small backend dotenv loader that reads `backend/.env` before the API builds configuration, without overwriting real process environment variables. Added backend and frontend `.env.example` files, generated local gitignored env files, and added a frontend pre-script that generates Angular's public `environment.ts` from `frontend/.env`. Root `.env.example` is now scoped to Docker Compose only.

**Why this approach:** Backend secrets must stay server-side, so the API can safely load them from `backend/.env` in local development and from real environment variables or secret managers in production. Frontend values are not secret once bundled into browser JavaScript, so the frontend env file is intentionally limited to public config such as the API base URL.

**Alternatives considered:** Adding a third-party dotenv package was avoided because the required parser behavior is small and local-only. Putting secrets in Angular env files was rejected because frontend bundles are visible to users.

**Follow-ups / risks:** Existing PostgreSQL volumes keep their original database password until changed inside Postgres with `ALTER USER` or the volume is recreated. Do not commit any real `.env` file.

**Reviewed by human:** [ ]

---
## [2026-07-31] Replace chat panel row grid with flexbox

**Prompt/task summary:** Record the final chat layout fix provided by the user after another AI-assisted pass: replace the message panel row grid with flexbox so the composer stays visible and the message list owns scrolling.

**Files changed:**
- `docs/ai-changelog/AI_CHANGELOG.md`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`

**What changed:** The chat message panel now uses `display: flex` with `flex-direction: column` instead of `display: grid` with fixed row indexes. The panel heading, error message, and chat input are fixed-size flex children, while `.message-list` uses `flex: 1 1 auto`, `min-height: 0`, and `overflow-y: auto` so it is the only region that grows, shrinks, and scrolls. The mobile layout also removes the hard-coded `min-height: 720px`, adds `grid-template-rows: auto minmax(0, 1fr)`, and lowers the conversation list max height to 200px.

**Why this approach:** The grid-row approach was brittle because conditional children such as `error-message` changed the panel's effective row structure and could push or clip the composer. Flexbox matches the desired layout better: fixed header/error/composer areas stay visible, and the message list absorbs the remaining space and scrolls independently.

**Alternatives considered:** Keeping the panel as CSS grid with explicit rows was tried first but caused the input to be clipped or hidden under certain viewport/message states. Absolute positioning was also avoided because it would remove the composer from normal layout flow and risk overlap with messages.

**Follow-ups / risks:** The earlier changelog entries about the failed grid/padding attempts are intentionally left as history, but this entry supersedes them as the final working layout decision.

**Reviewed by human:** [ ]

---

## [2026-08-01] Add frontend ticket status controls

**Prompt/task summary:** Continue the status workflow by wiring the frontend Tickets detail page to the backend status update endpoint.

**Files changed:**
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added a `PATCH /tickets/{id}/status` client method and a next-status action in Ticket detail. The UI shows the action only for `ITAgent` and `ITAdmin`, advances `Open -> InProgress -> Resolved -> Closed`, and synchronizes the selected detail and list item after a successful update.

**Why this approach:** A next-status button keeps the first workflow simple and prevents users from jumping to invalid states. Backend authorization and transition checks remain the source of truth; frontend role checks only keep the UI clean.

**Alternatives considered:** A free-form status dropdown was rejected for this slice because it exposes invalid choices that the backend would reject. Employee close/confirm-resolved controls remain deferred to a dedicated workflow slice.

**Follow-ups / risks:** Add assignment controls before agent-owned queues become more complex. Add frontend tests around role-specific rendering once the Tickets page has component-level test coverage.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket status workflow

**Prompt/task summary:** Implement the backend ticket status workflow slice before adding frontend status controls.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Application/Tickets/Commands/UpdateTicketStatus/*`
- `backend/src/Domain/Entities/Ticket.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketStatusCommandHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `PATCH /api/v1/tickets/{ticketId}/status` and an Application command that updates ticket status through the v1 lifecycle: `Open -> InProgress -> Resolved -> Closed`. IT admins can update any ticket; IT agents can update tickets assigned to them or unassigned queue tickets; employees cannot update status in this slice.

**Why this approach:** Status transition logic stays on the `Ticket` domain entity, while role/scope authorization stays in the Application command handler. This keeps business rules testable without HTTP or database dependencies and matches the existing Clean Architecture boundaries.

**Alternatives considered:** Letting employees close tickets immediately was deferred to a separate confirm-resolved/close-own-ticket slice because it changes the support workflow semantics. Allowing backward transitions or reopen was also deferred to keep the first lifecycle strict and predictable.

**Follow-ups / risks:** Frontend controls still need to call this endpoint. Reopen/escalation rules are future slices.

**Reviewed by human:** [ ]

---
## [2026-08-01] Link ticket detail back to chat conversation

**Prompt/task summary:** Make the Conversation field in ticket detail clickable so it opens the linked chat conversation.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The ticket detail Conversation context now renders as a router link to the Chat page with `conversationId` in the query string. The Chat page reads that query param after loading conversations and selects the matching conversation automatically.

**Why this approach:** Query params keep the existing Chat route simple while still making ticket-to-conversation navigation explicit and shareable. It avoids adding a new route shape before conversation detail routing is needed elsewhere.

**Alternatives considered:** Adding `/chat/conversations/:id` was deferred because the app currently uses the root Chat workspace route and only needs deep selection behavior for now.

**Follow-ups / risks:** If Chat later gets a dedicated conversation route, this link should move from query params to route params.

**Reviewed by human:** [ ]

---
## [2026-08-01] Fix chat ticket title and new conversation form layout

**Prompt/task summary:** Make tickets created from chat use the conversation title, and fix the New conversation form UI that expanded incorrectly in the conversation sidebar.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The chat-to-ticket handoff now sends the active conversation title as the ticket title instead of deriving it from the latest message. The conversation sidebar layout now uses flex column sizing so the New conversation form stays compact and the conversation list is the only section that grows and scrolls.

**Why this approach:** The conversation title is the user-defined issue name, so using it for the ticket title keeps chat and ticket records aligned. Flex column sizing is more robust than a fixed two-row grid when optional panels such as the creation form appear conditionally.

**Alternatives considered:** Keeping message-derived ticket titles was rejected because it can diverge from the user's explicit conversation name. Adding more grid rows was rejected because future conditional sidebar controls would recreate the same layout fragility.

**Follow-ups / risks:** Ticket details still show basic chat context only; richer context can be added when the backend exposes conversation title/message snippets directly.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add frontend tickets page and named conversation creation

**Prompt/task summary:** Continue after backend ticket list/detail APIs by adding the frontend Tickets page, and change Chat `New` conversation creation so the user can choose a conversation name.

**Files changed:**
- `frontend/src/app/app.routes.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/PROJECT_PLAN.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `/tickets` placeholder route with a real Tickets page that lists tickets, supports status/priority filters, opens detail in the same workspace, and displays title/status/priority/created metadata plus basic chat context via conversation/message IDs. The Chat page `New` action now opens a small title form before creating the conversation.

**Why this approach:** A split list/detail view matches the existing support-workspace layout and keeps repeated ticket triage efficient. For chat context, the frontend displays the IDs currently available from the backend detail contract instead of inventing another backend call in this slice.

**Alternatives considered:** A separate `/tickets/:id` page was deferred because the current app shell benefits from selecting tickets without route churn. Showing full originating message content was deferred until the backend exposes richer ticket context or a dedicated conversation summary endpoint.

**Follow-ups / risks:** Frontend status updates/comments are still future slices. Ticket detail can become richer once backend returns the originating message snippet or conversation title.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket list and detail APIs

**Prompt/task summary:** Implement the next backend slice for ticket list/detail APIs with status/priority filters and role-scoped authorization.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Api/Services/CurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ICurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ITicketRepository.cs`
- `backend/src/Application/Common/Interfaces/IUserRepository.cs`
- `backend/src/Application/Common/Models/PaginatedList.cs`
- `backend/src/Application/Common/Models/TicketListCriteria.cs`
- `backend/src/Application/Tickets/Models/TicketDetailDto.cs`
- `backend/src/Application/Tickets/Models/TicketSummaryDto.cs`
- `backend/src/Application/Tickets/Models/UserRefDto.cs`
- `backend/src/Application/Tickets/Queries/GetTicketDetail/*`
- `backend/src/Application/Tickets/Queries/GetTickets/*`
- `backend/src/Infrastructure/Persistence/Repositories/TicketRepository.cs`
- `backend/src/Infrastructure/Persistence/Repositories/UserRepository.cs`
- `backend/tests/Application.UnitTests/Auth/AuthCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Chat/ChatCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketQueryHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `GET /api/v1/tickets` and `GET /api/v1/tickets/{ticketId}`. Ticket list supports `status`, `priority`, `page`, and `pageSize`. The query handlers enforce role scope: employees see created tickets, IT agents see assigned plus unassigned queue tickets, and IT admins see all tickets. Responses include lightweight user references for creator and assignee.

**Why this approach:** Authorization stays in Application query handlers, matching the documented Clean Architecture boundary and keeping role behavior unit-testable without a database. The repository receives already-scoped criteria instead of reading HTTP claims or deciding product permissions itself.

**Alternatives considered:** Putting role checks in controllers was rejected because it would split business authorization across the API layer. Returning raw ticket entities was rejected because API DTOs keep response contracts explicit and prevent accidental field exposure.

**Follow-ups / risks:** Status update, comments, and frontend Tickets page are still separate slices. IT agent queue scope currently follows the documented v1 rule of assigned-to-me or unassigned tickets; if queues become department-based later, the criteria object can be extended.

**Reviewed by human:** [ ]

---
## [2026-08-01] Simplify chat ticket button template

**Prompt/task summary:** Check and fix the Chat page ticket button template after the user reported an error around the disabled/click bindings and conditional label.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `@if` block inside the ticket action button with a single interpolation expression that chooses between `Ticket created`, `Creating...`, and `Create ticket`.

**Why this approach:** The Angular compiler accepted the original template, but a simple interpolation is enough for this button label and is less likely to trigger editor/template tooling issues.

**Alternatives considered:** Keeping the `@if` block was valid Angular syntax, but it was more structure than this label needed.

**Follow-ups / risks:** None.

**Reviewed by human:** [ ]

---
## [2026-07-31] Make chat composer visible

**Prompt/task summary:** Fix the composer tray after the previous spacing change hid the message input.

**Files changed:**
- `docs/ai-changelog/AI_CHANGELOG.md`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`

**What changed:** Replaced padding-based composer positioning with explicit grid rows and fixed input/button heights so the textarea is always visible at the bottom of the tray.

**Why this approach:** The previous fix relied on padding inside an auto-sized grid row, which could clip the control. Explicit rows give the composer stable dimensions and match the requested lower input placement without hiding the textarea.

**Alternatives considered:** Using absolute positioning was avoided because the form is already a dedicated grid row in the message panel and should remain in normal document flow.

**Follow-ups / risks:** The send button can be changed to an icon button later if the UI moves closer to a Codex-style composer.

**Reviewed by human:** [ ]

---

## [2026-08-01] Add frontend ticket status controls

**Prompt/task summary:** Continue the status workflow by wiring the frontend Tickets detail page to the backend status update endpoint.

**Files changed:**
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added a `PATCH /tickets/{id}/status` client method and a next-status action in Ticket detail. The UI shows the action only for `ITAgent` and `ITAdmin`, advances `Open -> InProgress -> Resolved -> Closed`, and synchronizes the selected detail and list item after a successful update.

**Why this approach:** A next-status button keeps the first workflow simple and prevents users from jumping to invalid states. Backend authorization and transition checks remain the source of truth; frontend role checks only keep the UI clean.

**Alternatives considered:** A free-form status dropdown was rejected for this slice because it exposes invalid choices that the backend would reject. Employee close/confirm-resolved controls remain deferred to a dedicated workflow slice.

**Follow-ups / risks:** Add assignment controls before agent-owned queues become more complex. Add frontend tests around role-specific rendering once the Tickets page has component-level test coverage.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket status workflow

**Prompt/task summary:** Implement the backend ticket status workflow slice before adding frontend status controls.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Application/Tickets/Commands/UpdateTicketStatus/*`
- `backend/src/Domain/Entities/Ticket.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketStatusCommandHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `PATCH /api/v1/tickets/{ticketId}/status` and an Application command that updates ticket status through the v1 lifecycle: `Open -> InProgress -> Resolved -> Closed`. IT admins can update any ticket; IT agents can update tickets assigned to them or unassigned queue tickets; employees cannot update status in this slice.

**Why this approach:** Status transition logic stays on the `Ticket` domain entity, while role/scope authorization stays in the Application command handler. This keeps business rules testable without HTTP or database dependencies and matches the existing Clean Architecture boundaries.

**Alternatives considered:** Letting employees close tickets immediately was deferred to a separate confirm-resolved/close-own-ticket slice because it changes the support workflow semantics. Allowing backward transitions or reopen was also deferred to keep the first lifecycle strict and predictable.

**Follow-ups / risks:** Frontend controls still need to call this endpoint. Reopen/escalation rules are future slices.

**Reviewed by human:** [ ]

---
## [2026-08-01] Link ticket detail back to chat conversation

**Prompt/task summary:** Make the Conversation field in ticket detail clickable so it opens the linked chat conversation.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The ticket detail Conversation context now renders as a router link to the Chat page with `conversationId` in the query string. The Chat page reads that query param after loading conversations and selects the matching conversation automatically.

**Why this approach:** Query params keep the existing Chat route simple while still making ticket-to-conversation navigation explicit and shareable. It avoids adding a new route shape before conversation detail routing is needed elsewhere.

**Alternatives considered:** Adding `/chat/conversations/:id` was deferred because the app currently uses the root Chat workspace route and only needs deep selection behavior for now.

**Follow-ups / risks:** If Chat later gets a dedicated conversation route, this link should move from query params to route params.

**Reviewed by human:** [ ]

---
## [2026-08-01] Fix chat ticket title and new conversation form layout

**Prompt/task summary:** Make tickets created from chat use the conversation title, and fix the New conversation form UI that expanded incorrectly in the conversation sidebar.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The chat-to-ticket handoff now sends the active conversation title as the ticket title instead of deriving it from the latest message. The conversation sidebar layout now uses flex column sizing so the New conversation form stays compact and the conversation list is the only section that grows and scrolls.

**Why this approach:** The conversation title is the user-defined issue name, so using it for the ticket title keeps chat and ticket records aligned. Flex column sizing is more robust than a fixed two-row grid when optional panels such as the creation form appear conditionally.

**Alternatives considered:** Keeping message-derived ticket titles was rejected because it can diverge from the user's explicit conversation name. Adding more grid rows was rejected because future conditional sidebar controls would recreate the same layout fragility.

**Follow-ups / risks:** Ticket details still show basic chat context only; richer context can be added when the backend exposes conversation title/message snippets directly.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add frontend tickets page and named conversation creation

**Prompt/task summary:** Continue after backend ticket list/detail APIs by adding the frontend Tickets page, and change Chat `New` conversation creation so the user can choose a conversation name.

**Files changed:**
- `frontend/src/app/app.routes.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/PROJECT_PLAN.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `/tickets` placeholder route with a real Tickets page that lists tickets, supports status/priority filters, opens detail in the same workspace, and displays title/status/priority/created metadata plus basic chat context via conversation/message IDs. The Chat page `New` action now opens a small title form before creating the conversation.

**Why this approach:** A split list/detail view matches the existing support-workspace layout and keeps repeated ticket triage efficient. For chat context, the frontend displays the IDs currently available from the backend detail contract instead of inventing another backend call in this slice.

**Alternatives considered:** A separate `/tickets/:id` page was deferred because the current app shell benefits from selecting tickets without route churn. Showing full originating message content was deferred until the backend exposes richer ticket context or a dedicated conversation summary endpoint.

**Follow-ups / risks:** Frontend status updates/comments are still future slices. Ticket detail can become richer once backend returns the originating message snippet or conversation title.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket list and detail APIs

**Prompt/task summary:** Implement the next backend slice for ticket list/detail APIs with status/priority filters and role-scoped authorization.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Api/Services/CurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ICurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ITicketRepository.cs`
- `backend/src/Application/Common/Interfaces/IUserRepository.cs`
- `backend/src/Application/Common/Models/PaginatedList.cs`
- `backend/src/Application/Common/Models/TicketListCriteria.cs`
- `backend/src/Application/Tickets/Models/TicketDetailDto.cs`
- `backend/src/Application/Tickets/Models/TicketSummaryDto.cs`
- `backend/src/Application/Tickets/Models/UserRefDto.cs`
- `backend/src/Application/Tickets/Queries/GetTicketDetail/*`
- `backend/src/Application/Tickets/Queries/GetTickets/*`
- `backend/src/Infrastructure/Persistence/Repositories/TicketRepository.cs`
- `backend/src/Infrastructure/Persistence/Repositories/UserRepository.cs`
- `backend/tests/Application.UnitTests/Auth/AuthCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Chat/ChatCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketQueryHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `GET /api/v1/tickets` and `GET /api/v1/tickets/{ticketId}`. Ticket list supports `status`, `priority`, `page`, and `pageSize`. The query handlers enforce role scope: employees see created tickets, IT agents see assigned plus unassigned queue tickets, and IT admins see all tickets. Responses include lightweight user references for creator and assignee.

**Why this approach:** Authorization stays in Application query handlers, matching the documented Clean Architecture boundary and keeping role behavior unit-testable without a database. The repository receives already-scoped criteria instead of reading HTTP claims or deciding product permissions itself.

**Alternatives considered:** Putting role checks in controllers was rejected because it would split business authorization across the API layer. Returning raw ticket entities was rejected because API DTOs keep response contracts explicit and prevent accidental field exposure.

**Follow-ups / risks:** Status update, comments, and frontend Tickets page are still separate slices. IT agent queue scope currently follows the documented v1 rule of assigned-to-me or unassigned tickets; if queues become department-based later, the criteria object can be extended.

**Reviewed by human:** [ ]

---
## [2026-08-01] Simplify chat ticket button template

**Prompt/task summary:** Check and fix the Chat page ticket button template after the user reported an error around the disabled/click bindings and conditional label.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `@if` block inside the ticket action button with a single interpolation expression that chooses between `Ticket created`, `Creating...`, and `Create ticket`.

**Why this approach:** The Angular compiler accepted the original template, but a simple interpolation is enough for this button label and is less likely to trigger editor/template tooling issues.

**Alternatives considered:** Keeping the `@if` block was valid Angular syntax, but it was more structure than this label needed.

**Follow-ups / risks:** None.

**Reviewed by human:** [ ]

---
## [2026-07-31] Adjust chat composer spacing

**Prompt/task summary:** Match the requested chat composer layout by adding the larger bottom tray spacing shown in the reference image.

**Files changed:**
- `docs/ai-changelog/AI_CHANGELOG.md`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`

**What changed:** Increased the composer tray top padding and send button height so the textarea sits lower inside a dedicated bottom area instead of touching the message list.

**Why this approach:** The requested screenshot shows a persistent composer tray with vertical breathing room above the input. Adjusting the tray spacing is the smallest targeted fix and avoids changing the already-correct message scroll behavior.

**Alternatives considered:** Moving the form outside the message panel was avoided because it would reintroduce viewport and overlay problems in the app shell.

**Follow-ups / risks:** If the user wants the composer visually closer to Codex exactly, the next refinement should tune width, border radius, and icon-style send affordance separately.

**Reviewed by human:** [ ]

---

## [2026-08-01] Add frontend ticket status controls

**Prompt/task summary:** Continue the status workflow by wiring the frontend Tickets detail page to the backend status update endpoint.

**Files changed:**
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added a `PATCH /tickets/{id}/status` client method and a next-status action in Ticket detail. The UI shows the action only for `ITAgent` and `ITAdmin`, advances `Open -> InProgress -> Resolved -> Closed`, and synchronizes the selected detail and list item after a successful update.

**Why this approach:** A next-status button keeps the first workflow simple and prevents users from jumping to invalid states. Backend authorization and transition checks remain the source of truth; frontend role checks only keep the UI clean.

**Alternatives considered:** A free-form status dropdown was rejected for this slice because it exposes invalid choices that the backend would reject. Employee close/confirm-resolved controls remain deferred to a dedicated workflow slice.

**Follow-ups / risks:** Add assignment controls before agent-owned queues become more complex. Add frontend tests around role-specific rendering once the Tickets page has component-level test coverage.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket status workflow

**Prompt/task summary:** Implement the backend ticket status workflow slice before adding frontend status controls.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Application/Tickets/Commands/UpdateTicketStatus/*`
- `backend/src/Domain/Entities/Ticket.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketStatusCommandHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `PATCH /api/v1/tickets/{ticketId}/status` and an Application command that updates ticket status through the v1 lifecycle: `Open -> InProgress -> Resolved -> Closed`. IT admins can update any ticket; IT agents can update tickets assigned to them or unassigned queue tickets; employees cannot update status in this slice.

**Why this approach:** Status transition logic stays on the `Ticket` domain entity, while role/scope authorization stays in the Application command handler. This keeps business rules testable without HTTP or database dependencies and matches the existing Clean Architecture boundaries.

**Alternatives considered:** Letting employees close tickets immediately was deferred to a separate confirm-resolved/close-own-ticket slice because it changes the support workflow semantics. Allowing backward transitions or reopen was also deferred to keep the first lifecycle strict and predictable.

**Follow-ups / risks:** Frontend controls still need to call this endpoint. Reopen/escalation rules are future slices.

**Reviewed by human:** [ ]

---
## [2026-08-01] Link ticket detail back to chat conversation

**Prompt/task summary:** Make the Conversation field in ticket detail clickable so it opens the linked chat conversation.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The ticket detail Conversation context now renders as a router link to the Chat page with `conversationId` in the query string. The Chat page reads that query param after loading conversations and selects the matching conversation automatically.

**Why this approach:** Query params keep the existing Chat route simple while still making ticket-to-conversation navigation explicit and shareable. It avoids adding a new route shape before conversation detail routing is needed elsewhere.

**Alternatives considered:** Adding `/chat/conversations/:id` was deferred because the app currently uses the root Chat workspace route and only needs deep selection behavior for now.

**Follow-ups / risks:** If Chat later gets a dedicated conversation route, this link should move from query params to route params.

**Reviewed by human:** [ ]

---
## [2026-08-01] Fix chat ticket title and new conversation form layout

**Prompt/task summary:** Make tickets created from chat use the conversation title, and fix the New conversation form UI that expanded incorrectly in the conversation sidebar.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The chat-to-ticket handoff now sends the active conversation title as the ticket title instead of deriving it from the latest message. The conversation sidebar layout now uses flex column sizing so the New conversation form stays compact and the conversation list is the only section that grows and scrolls.

**Why this approach:** The conversation title is the user-defined issue name, so using it for the ticket title keeps chat and ticket records aligned. Flex column sizing is more robust than a fixed two-row grid when optional panels such as the creation form appear conditionally.

**Alternatives considered:** Keeping message-derived ticket titles was rejected because it can diverge from the user's explicit conversation name. Adding more grid rows was rejected because future conditional sidebar controls would recreate the same layout fragility.

**Follow-ups / risks:** Ticket details still show basic chat context only; richer context can be added when the backend exposes conversation title/message snippets directly.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add frontend tickets page and named conversation creation

**Prompt/task summary:** Continue after backend ticket list/detail APIs by adding the frontend Tickets page, and change Chat `New` conversation creation so the user can choose a conversation name.

**Files changed:**
- `frontend/src/app/app.routes.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/PROJECT_PLAN.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `/tickets` placeholder route with a real Tickets page that lists tickets, supports status/priority filters, opens detail in the same workspace, and displays title/status/priority/created metadata plus basic chat context via conversation/message IDs. The Chat page `New` action now opens a small title form before creating the conversation.

**Why this approach:** A split list/detail view matches the existing support-workspace layout and keeps repeated ticket triage efficient. For chat context, the frontend displays the IDs currently available from the backend detail contract instead of inventing another backend call in this slice.

**Alternatives considered:** A separate `/tickets/:id` page was deferred because the current app shell benefits from selecting tickets without route churn. Showing full originating message content was deferred until the backend exposes richer ticket context or a dedicated conversation summary endpoint.

**Follow-ups / risks:** Frontend status updates/comments are still future slices. Ticket detail can become richer once backend returns the originating message snippet or conversation title.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket list and detail APIs

**Prompt/task summary:** Implement the next backend slice for ticket list/detail APIs with status/priority filters and role-scoped authorization.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Api/Services/CurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ICurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ITicketRepository.cs`
- `backend/src/Application/Common/Interfaces/IUserRepository.cs`
- `backend/src/Application/Common/Models/PaginatedList.cs`
- `backend/src/Application/Common/Models/TicketListCriteria.cs`
- `backend/src/Application/Tickets/Models/TicketDetailDto.cs`
- `backend/src/Application/Tickets/Models/TicketSummaryDto.cs`
- `backend/src/Application/Tickets/Models/UserRefDto.cs`
- `backend/src/Application/Tickets/Queries/GetTicketDetail/*`
- `backend/src/Application/Tickets/Queries/GetTickets/*`
- `backend/src/Infrastructure/Persistence/Repositories/TicketRepository.cs`
- `backend/src/Infrastructure/Persistence/Repositories/UserRepository.cs`
- `backend/tests/Application.UnitTests/Auth/AuthCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Chat/ChatCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketQueryHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `GET /api/v1/tickets` and `GET /api/v1/tickets/{ticketId}`. Ticket list supports `status`, `priority`, `page`, and `pageSize`. The query handlers enforce role scope: employees see created tickets, IT agents see assigned plus unassigned queue tickets, and IT admins see all tickets. Responses include lightweight user references for creator and assignee.

**Why this approach:** Authorization stays in Application query handlers, matching the documented Clean Architecture boundary and keeping role behavior unit-testable without a database. The repository receives already-scoped criteria instead of reading HTTP claims or deciding product permissions itself.

**Alternatives considered:** Putting role checks in controllers was rejected because it would split business authorization across the API layer. Returning raw ticket entities was rejected because API DTOs keep response contracts explicit and prevent accidental field exposure.

**Follow-ups / risks:** Status update, comments, and frontend Tickets page are still separate slices. IT agent queue scope currently follows the documented v1 rule of assigned-to-me or unassigned tickets; if queues become department-based later, the criteria object can be extended.

**Reviewed by human:** [ ]

---
## [2026-08-01] Simplify chat ticket button template

**Prompt/task summary:** Check and fix the Chat page ticket button template after the user reported an error around the disabled/click bindings and conditional label.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `@if` block inside the ticket action button with a single interpolation expression that chooses between `Ticket created`, `Creating...`, and `Create ticket`.

**Why this approach:** The Angular compiler accepted the original template, but a simple interpolation is enough for this button label and is less likely to trigger editor/template tooling issues.

**Alternatives considered:** Keeping the `@if` block was valid Angular syntax, but it was more structure than this label needed.

**Follow-ups / risks:** None.

**Reviewed by human:** [ ]

---
## [2026-07-30] Pin chat composer to bottom

**Prompt/task summary:** Adjust the chat UI so the message composer sits at the bottom like the assistant UI, while older messages are reviewed by scrolling upward.

**Files changed:**
- `docs/ai-changelog/AI_CHANGELOG.md`
- `frontend/src/app/app.scss`
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`

**What changed:** Changed the message input to a textarea composer, constrained the authenticated app shell to the viewport, made the message list the scrollable area, anchored short threads to the bottom, and added auto-scroll to the latest message after loading or sending.

**Why this approach:** A chat screen should reserve the bottom action area for composing messages and keep history in a dedicated scroll container. This keeps the input predictable and makes old messages accessible by scrolling up.

**Alternatives considered:** Using a fixed-position composer was avoided because the chat is inside an authenticated app shell with a sidebar and header; keeping the composer inside the message panel avoids overlay and responsive layout issues.

**Follow-ups / risks:** If the authenticated shell gets more fixed header/tool areas later, keep those areas as flex children instead of returning to viewport-height guesses.

**Reviewed by human:** [ ]

---

## [2026-08-01] Add frontend ticket status controls

**Prompt/task summary:** Continue the status workflow by wiring the frontend Tickets detail page to the backend status update endpoint.

**Files changed:**
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added a `PATCH /tickets/{id}/status` client method and a next-status action in Ticket detail. The UI shows the action only for `ITAgent` and `ITAdmin`, advances `Open -> InProgress -> Resolved -> Closed`, and synchronizes the selected detail and list item after a successful update.

**Why this approach:** A next-status button keeps the first workflow simple and prevents users from jumping to invalid states. Backend authorization and transition checks remain the source of truth; frontend role checks only keep the UI clean.

**Alternatives considered:** A free-form status dropdown was rejected for this slice because it exposes invalid choices that the backend would reject. Employee close/confirm-resolved controls remain deferred to a dedicated workflow slice.

**Follow-ups / risks:** Add assignment controls before agent-owned queues become more complex. Add frontend tests around role-specific rendering once the Tickets page has component-level test coverage.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket status workflow

**Prompt/task summary:** Implement the backend ticket status workflow slice before adding frontend status controls.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Application/Tickets/Commands/UpdateTicketStatus/*`
- `backend/src/Domain/Entities/Ticket.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketStatusCommandHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `PATCH /api/v1/tickets/{ticketId}/status` and an Application command that updates ticket status through the v1 lifecycle: `Open -> InProgress -> Resolved -> Closed`. IT admins can update any ticket; IT agents can update tickets assigned to them or unassigned queue tickets; employees cannot update status in this slice.

**Why this approach:** Status transition logic stays on the `Ticket` domain entity, while role/scope authorization stays in the Application command handler. This keeps business rules testable without HTTP or database dependencies and matches the existing Clean Architecture boundaries.

**Alternatives considered:** Letting employees close tickets immediately was deferred to a separate confirm-resolved/close-own-ticket slice because it changes the support workflow semantics. Allowing backward transitions or reopen was also deferred to keep the first lifecycle strict and predictable.

**Follow-ups / risks:** Frontend controls still need to call this endpoint. Reopen/escalation rules are future slices.

**Reviewed by human:** [ ]

---
## [2026-08-01] Link ticket detail back to chat conversation

**Prompt/task summary:** Make the Conversation field in ticket detail clickable so it opens the linked chat conversation.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The ticket detail Conversation context now renders as a router link to the Chat page with `conversationId` in the query string. The Chat page reads that query param after loading conversations and selects the matching conversation automatically.

**Why this approach:** Query params keep the existing Chat route simple while still making ticket-to-conversation navigation explicit and shareable. It avoids adding a new route shape before conversation detail routing is needed elsewhere.

**Alternatives considered:** Adding `/chat/conversations/:id` was deferred because the app currently uses the root Chat workspace route and only needs deep selection behavior for now.

**Follow-ups / risks:** If Chat later gets a dedicated conversation route, this link should move from query params to route params.

**Reviewed by human:** [ ]

---
## [2026-08-01] Fix chat ticket title and new conversation form layout

**Prompt/task summary:** Make tickets created from chat use the conversation title, and fix the New conversation form UI that expanded incorrectly in the conversation sidebar.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The chat-to-ticket handoff now sends the active conversation title as the ticket title instead of deriving it from the latest message. The conversation sidebar layout now uses flex column sizing so the New conversation form stays compact and the conversation list is the only section that grows and scrolls.

**Why this approach:** The conversation title is the user-defined issue name, so using it for the ticket title keeps chat and ticket records aligned. Flex column sizing is more robust than a fixed two-row grid when optional panels such as the creation form appear conditionally.

**Alternatives considered:** Keeping message-derived ticket titles was rejected because it can diverge from the user's explicit conversation name. Adding more grid rows was rejected because future conditional sidebar controls would recreate the same layout fragility.

**Follow-ups / risks:** Ticket details still show basic chat context only; richer context can be added when the backend exposes conversation title/message snippets directly.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add frontend tickets page and named conversation creation

**Prompt/task summary:** Continue after backend ticket list/detail APIs by adding the frontend Tickets page, and change Chat `New` conversation creation so the user can choose a conversation name.

**Files changed:**
- `frontend/src/app/app.routes.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/PROJECT_PLAN.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `/tickets` placeholder route with a real Tickets page that lists tickets, supports status/priority filters, opens detail in the same workspace, and displays title/status/priority/created metadata plus basic chat context via conversation/message IDs. The Chat page `New` action now opens a small title form before creating the conversation.

**Why this approach:** A split list/detail view matches the existing support-workspace layout and keeps repeated ticket triage efficient. For chat context, the frontend displays the IDs currently available from the backend detail contract instead of inventing another backend call in this slice.

**Alternatives considered:** A separate `/tickets/:id` page was deferred because the current app shell benefits from selecting tickets without route churn. Showing full originating message content was deferred until the backend exposes richer ticket context or a dedicated conversation summary endpoint.

**Follow-ups / risks:** Frontend status updates/comments are still future slices. Ticket detail can become richer once backend returns the originating message snippet or conversation title.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket list and detail APIs

**Prompt/task summary:** Implement the next backend slice for ticket list/detail APIs with status/priority filters and role-scoped authorization.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Api/Services/CurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ICurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ITicketRepository.cs`
- `backend/src/Application/Common/Interfaces/IUserRepository.cs`
- `backend/src/Application/Common/Models/PaginatedList.cs`
- `backend/src/Application/Common/Models/TicketListCriteria.cs`
- `backend/src/Application/Tickets/Models/TicketDetailDto.cs`
- `backend/src/Application/Tickets/Models/TicketSummaryDto.cs`
- `backend/src/Application/Tickets/Models/UserRefDto.cs`
- `backend/src/Application/Tickets/Queries/GetTicketDetail/*`
- `backend/src/Application/Tickets/Queries/GetTickets/*`
- `backend/src/Infrastructure/Persistence/Repositories/TicketRepository.cs`
- `backend/src/Infrastructure/Persistence/Repositories/UserRepository.cs`
- `backend/tests/Application.UnitTests/Auth/AuthCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Chat/ChatCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketQueryHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `GET /api/v1/tickets` and `GET /api/v1/tickets/{ticketId}`. Ticket list supports `status`, `priority`, `page`, and `pageSize`. The query handlers enforce role scope: employees see created tickets, IT agents see assigned plus unassigned queue tickets, and IT admins see all tickets. Responses include lightweight user references for creator and assignee.

**Why this approach:** Authorization stays in Application query handlers, matching the documented Clean Architecture boundary and keeping role behavior unit-testable without a database. The repository receives already-scoped criteria instead of reading HTTP claims or deciding product permissions itself.

**Alternatives considered:** Putting role checks in controllers was rejected because it would split business authorization across the API layer. Returning raw ticket entities was rejected because API DTOs keep response contracts explicit and prevent accidental field exposure.

**Follow-ups / risks:** Status update, comments, and frontend Tickets page are still separate slices. IT agent queue scope currently follows the documented v1 rule of assigned-to-me or unassigned tickets; if queues become department-based later, the criteria object can be extended.

**Reviewed by human:** [ ]

---
## [2026-08-01] Simplify chat ticket button template

**Prompt/task summary:** Check and fix the Chat page ticket button template after the user reported an error around the disabled/click bindings and conditional label.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `@if` block inside the ticket action button with a single interpolation expression that chooses between `Ticket created`, `Creating...`, and `Create ticket`.

**Why this approach:** The Angular compiler accepted the original template, but a simple interpolation is enough for this button label and is less likely to trigger editor/template tooling issues.

**Alternatives considered:** Keeping the `@if` block was valid Angular syntax, but it was more structure than this label needed.

**Follow-ups / risks:** None.

**Reviewed by human:** [ ]

---
## [2026-07-30] Connect frontend chat to backend API

**Prompt/task summary:** Continue in the recommended order by wiring the frontend chat page to the existing backend chat happy-path API.

**Files changed:**
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`
- `frontend/src/app/app.routes.ts`
- `frontend/src/app/features/chat/data-access/chat.models.ts`
- `frontend/src/app/features/chat/data-access/chat.service.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`

**What changed:** Replaced the protected Chat placeholder route with a real standalone `ChatPageComponent`. Added typed chat DTOs and `ChatService` calls for listing conversations, starting a conversation, loading messages, and sending messages. The UI now shows a conversation list, active message thread, empty/loading/error states, and a message composer that persists user + canned assistant messages through the backend API.

**Why this approach:** This completes the next practical Phase 3 step after backend chat was verified manually. Keeping HTTP code in `features/chat/data-access` matches the project frontend architecture, while the feature component owns only UI state and user interactions. The UI uses the current no-AI JSON response shape so it can be exercised before streaming/RAG is introduced.

**Alternatives considered:** Building a full chat store abstraction was deferred because the feature still has one page and a small state surface. Implementing streaming UI now was rejected because the backend intentionally returns JSON until the AI integration slice.

**Follow-ups / risks:** Add component/service tests around chat interactions when the frontend test harness expands beyond the root app smoke tests. Next backend slice should add ticket creation from messages before AI intent classification.

**Reviewed by human:** [ ]

---

## [2026-08-01] Add frontend ticket status controls

**Prompt/task summary:** Continue the status workflow by wiring the frontend Tickets detail page to the backend status update endpoint.

**Files changed:**
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added a `PATCH /tickets/{id}/status` client method and a next-status action in Ticket detail. The UI shows the action only for `ITAgent` and `ITAdmin`, advances `Open -> InProgress -> Resolved -> Closed`, and synchronizes the selected detail and list item after a successful update.

**Why this approach:** A next-status button keeps the first workflow simple and prevents users from jumping to invalid states. Backend authorization and transition checks remain the source of truth; frontend role checks only keep the UI clean.

**Alternatives considered:** A free-form status dropdown was rejected for this slice because it exposes invalid choices that the backend would reject. Employee close/confirm-resolved controls remain deferred to a dedicated workflow slice.

**Follow-ups / risks:** Add assignment controls before agent-owned queues become more complex. Add frontend tests around role-specific rendering once the Tickets page has component-level test coverage.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket status workflow

**Prompt/task summary:** Implement the backend ticket status workflow slice before adding frontend status controls.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Application/Tickets/Commands/UpdateTicketStatus/*`
- `backend/src/Domain/Entities/Ticket.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketStatusCommandHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `PATCH /api/v1/tickets/{ticketId}/status` and an Application command that updates ticket status through the v1 lifecycle: `Open -> InProgress -> Resolved -> Closed`. IT admins can update any ticket; IT agents can update tickets assigned to them or unassigned queue tickets; employees cannot update status in this slice.

**Why this approach:** Status transition logic stays on the `Ticket` domain entity, while role/scope authorization stays in the Application command handler. This keeps business rules testable without HTTP or database dependencies and matches the existing Clean Architecture boundaries.

**Alternatives considered:** Letting employees close tickets immediately was deferred to a separate confirm-resolved/close-own-ticket slice because it changes the support workflow semantics. Allowing backward transitions or reopen was also deferred to keep the first lifecycle strict and predictable.

**Follow-ups / risks:** Frontend controls still need to call this endpoint. Reopen/escalation rules are future slices.

**Reviewed by human:** [ ]

---
## [2026-08-01] Link ticket detail back to chat conversation

**Prompt/task summary:** Make the Conversation field in ticket detail clickable so it opens the linked chat conversation.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The ticket detail Conversation context now renders as a router link to the Chat page with `conversationId` in the query string. The Chat page reads that query param after loading conversations and selects the matching conversation automatically.

**Why this approach:** Query params keep the existing Chat route simple while still making ticket-to-conversation navigation explicit and shareable. It avoids adding a new route shape before conversation detail routing is needed elsewhere.

**Alternatives considered:** Adding `/chat/conversations/:id` was deferred because the app currently uses the root Chat workspace route and only needs deep selection behavior for now.

**Follow-ups / risks:** If Chat later gets a dedicated conversation route, this link should move from query params to route params.

**Reviewed by human:** [ ]

---
## [2026-08-01] Fix chat ticket title and new conversation form layout

**Prompt/task summary:** Make tickets created from chat use the conversation title, and fix the New conversation form UI that expanded incorrectly in the conversation sidebar.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The chat-to-ticket handoff now sends the active conversation title as the ticket title instead of deriving it from the latest message. The conversation sidebar layout now uses flex column sizing so the New conversation form stays compact and the conversation list is the only section that grows and scrolls.

**Why this approach:** The conversation title is the user-defined issue name, so using it for the ticket title keeps chat and ticket records aligned. Flex column sizing is more robust than a fixed two-row grid when optional panels such as the creation form appear conditionally.

**Alternatives considered:** Keeping message-derived ticket titles was rejected because it can diverge from the user's explicit conversation name. Adding more grid rows was rejected because future conditional sidebar controls would recreate the same layout fragility.

**Follow-ups / risks:** Ticket details still show basic chat context only; richer context can be added when the backend exposes conversation title/message snippets directly.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add frontend tickets page and named conversation creation

**Prompt/task summary:** Continue after backend ticket list/detail APIs by adding the frontend Tickets page, and change Chat `New` conversation creation so the user can choose a conversation name.

**Files changed:**
- `frontend/src/app/app.routes.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/PROJECT_PLAN.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `/tickets` placeholder route with a real Tickets page that lists tickets, supports status/priority filters, opens detail in the same workspace, and displays title/status/priority/created metadata plus basic chat context via conversation/message IDs. The Chat page `New` action now opens a small title form before creating the conversation.

**Why this approach:** A split list/detail view matches the existing support-workspace layout and keeps repeated ticket triage efficient. For chat context, the frontend displays the IDs currently available from the backend detail contract instead of inventing another backend call in this slice.

**Alternatives considered:** A separate `/tickets/:id` page was deferred because the current app shell benefits from selecting tickets without route churn. Showing full originating message content was deferred until the backend exposes richer ticket context or a dedicated conversation summary endpoint.

**Follow-ups / risks:** Frontend status updates/comments are still future slices. Ticket detail can become richer once backend returns the originating message snippet or conversation title.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket list and detail APIs

**Prompt/task summary:** Implement the next backend slice for ticket list/detail APIs with status/priority filters and role-scoped authorization.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Api/Services/CurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ICurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ITicketRepository.cs`
- `backend/src/Application/Common/Interfaces/IUserRepository.cs`
- `backend/src/Application/Common/Models/PaginatedList.cs`
- `backend/src/Application/Common/Models/TicketListCriteria.cs`
- `backend/src/Application/Tickets/Models/TicketDetailDto.cs`
- `backend/src/Application/Tickets/Models/TicketSummaryDto.cs`
- `backend/src/Application/Tickets/Models/UserRefDto.cs`
- `backend/src/Application/Tickets/Queries/GetTicketDetail/*`
- `backend/src/Application/Tickets/Queries/GetTickets/*`
- `backend/src/Infrastructure/Persistence/Repositories/TicketRepository.cs`
- `backend/src/Infrastructure/Persistence/Repositories/UserRepository.cs`
- `backend/tests/Application.UnitTests/Auth/AuthCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Chat/ChatCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketQueryHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `GET /api/v1/tickets` and `GET /api/v1/tickets/{ticketId}`. Ticket list supports `status`, `priority`, `page`, and `pageSize`. The query handlers enforce role scope: employees see created tickets, IT agents see assigned plus unassigned queue tickets, and IT admins see all tickets. Responses include lightweight user references for creator and assignee.

**Why this approach:** Authorization stays in Application query handlers, matching the documented Clean Architecture boundary and keeping role behavior unit-testable without a database. The repository receives already-scoped criteria instead of reading HTTP claims or deciding product permissions itself.

**Alternatives considered:** Putting role checks in controllers was rejected because it would split business authorization across the API layer. Returning raw ticket entities was rejected because API DTOs keep response contracts explicit and prevent accidental field exposure.

**Follow-ups / risks:** Status update, comments, and frontend Tickets page are still separate slices. IT agent queue scope currently follows the documented v1 rule of assigned-to-me or unassigned tickets; if queues become department-based later, the criteria object can be extended.

**Reviewed by human:** [ ]

---
## [2026-08-01] Simplify chat ticket button template

**Prompt/task summary:** Check and fix the Chat page ticket button template after the user reported an error around the disabled/click bindings and conditional label.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `@if` block inside the ticket action button with a single interpolation expression that chooses between `Ticket created`, `Creating...`, and `Create ticket`.

**Why this approach:** The Angular compiler accepted the original template, but a simple interpolation is enough for this button label and is less likely to trigger editor/template tooling issues.

**Alternatives considered:** Keeping the `@if` block was valid Angular syntax, but it was more structure than this label needed.

**Follow-ups / risks:** None.

**Reviewed by human:** [ ]

---
## [2026-07-30] Record RAG teaching preference

**Prompt/task summary:** The user asked to remember that any future RAG pipeline, KB docs/chunks, or vector work must include detailed step-by-step teaching.

**Files changed:**
- `AGENTS.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added a dedicated AGENTS.md instruction requiring future agents to explain RAG/KB/vector concepts, data flow, schema choices, trade-offs, and manual testing/debugging in Thai whenever that area is touched.

**Why this approach:** `AGENTS.md` is the repo-level instruction file every AI coding agent must read before touching code, so storing the preference there makes it durable for future sessions and future agents.

**Alternatives considered:** Keeping the preference only in conversation memory was rejected because it would not survive context changes reliably. Putting it only in the changelog was rejected because changelog entries are historical, while `AGENTS.md` is active instruction.

**Follow-ups / risks:** Future agents still need to read and obey `AGENTS.md` before starting RAG-related work.

**Reviewed by human:** [ ]

---

## [2026-08-01] Add frontend ticket status controls

**Prompt/task summary:** Continue the status workflow by wiring the frontend Tickets detail page to the backend status update endpoint.

**Files changed:**
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added a `PATCH /tickets/{id}/status` client method and a next-status action in Ticket detail. The UI shows the action only for `ITAgent` and `ITAdmin`, advances `Open -> InProgress -> Resolved -> Closed`, and synchronizes the selected detail and list item after a successful update.

**Why this approach:** A next-status button keeps the first workflow simple and prevents users from jumping to invalid states. Backend authorization and transition checks remain the source of truth; frontend role checks only keep the UI clean.

**Alternatives considered:** A free-form status dropdown was rejected for this slice because it exposes invalid choices that the backend would reject. Employee close/confirm-resolved controls remain deferred to a dedicated workflow slice.

**Follow-ups / risks:** Add assignment controls before agent-owned queues become more complex. Add frontend tests around role-specific rendering once the Tickets page has component-level test coverage.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket status workflow

**Prompt/task summary:** Implement the backend ticket status workflow slice before adding frontend status controls.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Application/Tickets/Commands/UpdateTicketStatus/*`
- `backend/src/Domain/Entities/Ticket.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketStatusCommandHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `PATCH /api/v1/tickets/{ticketId}/status` and an Application command that updates ticket status through the v1 lifecycle: `Open -> InProgress -> Resolved -> Closed`. IT admins can update any ticket; IT agents can update tickets assigned to them or unassigned queue tickets; employees cannot update status in this slice.

**Why this approach:** Status transition logic stays on the `Ticket` domain entity, while role/scope authorization stays in the Application command handler. This keeps business rules testable without HTTP or database dependencies and matches the existing Clean Architecture boundaries.

**Alternatives considered:** Letting employees close tickets immediately was deferred to a separate confirm-resolved/close-own-ticket slice because it changes the support workflow semantics. Allowing backward transitions or reopen was also deferred to keep the first lifecycle strict and predictable.

**Follow-ups / risks:** Frontend controls still need to call this endpoint. Reopen/escalation rules are future slices.

**Reviewed by human:** [ ]

---
## [2026-08-01] Link ticket detail back to chat conversation

**Prompt/task summary:** Make the Conversation field in ticket detail clickable so it opens the linked chat conversation.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The ticket detail Conversation context now renders as a router link to the Chat page with `conversationId` in the query string. The Chat page reads that query param after loading conversations and selects the matching conversation automatically.

**Why this approach:** Query params keep the existing Chat route simple while still making ticket-to-conversation navigation explicit and shareable. It avoids adding a new route shape before conversation detail routing is needed elsewhere.

**Alternatives considered:** Adding `/chat/conversations/:id` was deferred because the app currently uses the root Chat workspace route and only needs deep selection behavior for now.

**Follow-ups / risks:** If Chat later gets a dedicated conversation route, this link should move from query params to route params.

**Reviewed by human:** [ ]

---
## [2026-08-01] Fix chat ticket title and new conversation form layout

**Prompt/task summary:** Make tickets created from chat use the conversation title, and fix the New conversation form UI that expanded incorrectly in the conversation sidebar.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The chat-to-ticket handoff now sends the active conversation title as the ticket title instead of deriving it from the latest message. The conversation sidebar layout now uses flex column sizing so the New conversation form stays compact and the conversation list is the only section that grows and scrolls.

**Why this approach:** The conversation title is the user-defined issue name, so using it for the ticket title keeps chat and ticket records aligned. Flex column sizing is more robust than a fixed two-row grid when optional panels such as the creation form appear conditionally.

**Alternatives considered:** Keeping message-derived ticket titles was rejected because it can diverge from the user's explicit conversation name. Adding more grid rows was rejected because future conditional sidebar controls would recreate the same layout fragility.

**Follow-ups / risks:** Ticket details still show basic chat context only; richer context can be added when the backend exposes conversation title/message snippets directly.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add frontend tickets page and named conversation creation

**Prompt/task summary:** Continue after backend ticket list/detail APIs by adding the frontend Tickets page, and change Chat `New` conversation creation so the user can choose a conversation name.

**Files changed:**
- `frontend/src/app/app.routes.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/PROJECT_PLAN.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `/tickets` placeholder route with a real Tickets page that lists tickets, supports status/priority filters, opens detail in the same workspace, and displays title/status/priority/created metadata plus basic chat context via conversation/message IDs. The Chat page `New` action now opens a small title form before creating the conversation.

**Why this approach:** A split list/detail view matches the existing support-workspace layout and keeps repeated ticket triage efficient. For chat context, the frontend displays the IDs currently available from the backend detail contract instead of inventing another backend call in this slice.

**Alternatives considered:** A separate `/tickets/:id` page was deferred because the current app shell benefits from selecting tickets without route churn. Showing full originating message content was deferred until the backend exposes richer ticket context or a dedicated conversation summary endpoint.

**Follow-ups / risks:** Frontend status updates/comments are still future slices. Ticket detail can become richer once backend returns the originating message snippet or conversation title.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket list and detail APIs

**Prompt/task summary:** Implement the next backend slice for ticket list/detail APIs with status/priority filters and role-scoped authorization.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Api/Services/CurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ICurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ITicketRepository.cs`
- `backend/src/Application/Common/Interfaces/IUserRepository.cs`
- `backend/src/Application/Common/Models/PaginatedList.cs`
- `backend/src/Application/Common/Models/TicketListCriteria.cs`
- `backend/src/Application/Tickets/Models/TicketDetailDto.cs`
- `backend/src/Application/Tickets/Models/TicketSummaryDto.cs`
- `backend/src/Application/Tickets/Models/UserRefDto.cs`
- `backend/src/Application/Tickets/Queries/GetTicketDetail/*`
- `backend/src/Application/Tickets/Queries/GetTickets/*`
- `backend/src/Infrastructure/Persistence/Repositories/TicketRepository.cs`
- `backend/src/Infrastructure/Persistence/Repositories/UserRepository.cs`
- `backend/tests/Application.UnitTests/Auth/AuthCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Chat/ChatCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketQueryHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `GET /api/v1/tickets` and `GET /api/v1/tickets/{ticketId}`. Ticket list supports `status`, `priority`, `page`, and `pageSize`. The query handlers enforce role scope: employees see created tickets, IT agents see assigned plus unassigned queue tickets, and IT admins see all tickets. Responses include lightweight user references for creator and assignee.

**Why this approach:** Authorization stays in Application query handlers, matching the documented Clean Architecture boundary and keeping role behavior unit-testable without a database. The repository receives already-scoped criteria instead of reading HTTP claims or deciding product permissions itself.

**Alternatives considered:** Putting role checks in controllers was rejected because it would split business authorization across the API layer. Returning raw ticket entities was rejected because API DTOs keep response contracts explicit and prevent accidental field exposure.

**Follow-ups / risks:** Status update, comments, and frontend Tickets page are still separate slices. IT agent queue scope currently follows the documented v1 rule of assigned-to-me or unassigned tickets; if queues become department-based later, the criteria object can be extended.

**Reviewed by human:** [ ]

---
## [2026-08-01] Simplify chat ticket button template

**Prompt/task summary:** Check and fix the Chat page ticket button template after the user reported an error around the disabled/click bindings and conditional label.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `@if` block inside the ticket action button with a single interpolation expression that chooses between `Ticket created`, `Creating...`, and `Create ticket`.

**Why this approach:** The Angular compiler accepted the original template, but a simple interpolation is enough for this button label and is less likely to trigger editor/template tooling issues.

**Alternatives considered:** Keeping the `@if` block was valid Angular syntax, but it was more structure than this label needed.

**Follow-ups / risks:** None.

**Reviewed by human:** [ ]

---
## [2026-07-30] Add chat happy path and ticket attachment metadata

**Prompt/task summary:** Explain when KB docs/chunks/vector should be implemented, add ticket support for attachments, and start Phase 3 backend chat happy path without AI.

**Files changed:**
- `docs/API_SPEC.md`
- `docs/ER_DIAGRAM.md`
- `docs/ai-changelog/AI_CHANGELOG.md`
- `backend/src/Api/Controllers/ChatController.cs`
- `backend/src/Api/Program.cs`
- `backend/src/Api/Services/CurrentUserService.cs`
- `backend/src/Application/Chat/**`
- `backend/src/Application/Common/Interfaces/IConversationRepository.cs`
- `backend/src/Application/Common/Interfaces/ICurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/IMessageRepository.cs`
- `backend/src/CompositionRoot/DependencyInjection.cs`
- `backend/src/Domain/Entities/Ticket.cs`
- `backend/src/Infrastructure/Persistence/Configurations/TicketConfiguration.cs`
- `backend/src/Infrastructure/Persistence/Migrations/20260730131340_AddTicketAttachmentsAndChatHappyPath*`
- `backend/src/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `backend/src/Infrastructure/Persistence/Repositories/ConversationRepository.cs`
- `backend/src/Infrastructure/Persistence/Repositories/MessageRepository.cs`
- `backend/tests/Application.UnitTests/Chat/ChatCommandHandlerTests.cs`

**What changed:** Added `tickets.attachments` as a non-null `jsonb` metadata array with default `[]`. Added current-user access, conversation/message repositories, chat Application handlers, and authenticated chat endpoints for creating/listing conversations, listing messages, and sending a message. The current send-message flow persists both the user message and a canned assistant response without AI/RAG. Added tests for current-user scoping and chat message persistence. Applied the migration to local Docker PostgreSQL and smoke-tested register, login, create conversation, send message, and list messages against the running API, then removed the temporary smoke-test user.

**Why this approach:** KB docs/chunks/vector were deferred to the RAG phase because vector dimensions and indexes depend on the chosen embedding model. Ticket attachments are represented as metadata JSON rather than binary data because the database row should reference uploaded files, not store file contents. The Phase 3 chat path proves authenticated persistence and ownership boundaries before adding Groq, streaming, intent classification, or ticket automation.

**Alternatives considered:** A separate `ticket_attachments` table was considered and is still a good option if attachment querying/auditing becomes complex; a `jsonb` column was chosen now because the user specifically asked for a ticket column and this slice only needs future upload metadata support. Implementing SSE immediately was rejected because this is the no-AI happy path and streaming belongs with the AI integration slice.

**Follow-ups / risks:** Add real upload storage and validation before exposing attachment upload UI. Replace the canned assistant response with intent classification/RAG streaming in the AI slices. Add integration tests around JWT-protected chat endpoints when the API test project exists.

**Reviewed by human:** [ ]

---

## [2026-08-01] Add frontend ticket status controls

**Prompt/task summary:** Continue the status workflow by wiring the frontend Tickets detail page to the backend status update endpoint.

**Files changed:**
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added a `PATCH /tickets/{id}/status` client method and a next-status action in Ticket detail. The UI shows the action only for `ITAgent` and `ITAdmin`, advances `Open -> InProgress -> Resolved -> Closed`, and synchronizes the selected detail and list item after a successful update.

**Why this approach:** A next-status button keeps the first workflow simple and prevents users from jumping to invalid states. Backend authorization and transition checks remain the source of truth; frontend role checks only keep the UI clean.

**Alternatives considered:** A free-form status dropdown was rejected for this slice because it exposes invalid choices that the backend would reject. Employee close/confirm-resolved controls remain deferred to a dedicated workflow slice.

**Follow-ups / risks:** Add assignment controls before agent-owned queues become more complex. Add frontend tests around role-specific rendering once the Tickets page has component-level test coverage.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket status workflow

**Prompt/task summary:** Implement the backend ticket status workflow slice before adding frontend status controls.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Application/Tickets/Commands/UpdateTicketStatus/*`
- `backend/src/Domain/Entities/Ticket.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketStatusCommandHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `PATCH /api/v1/tickets/{ticketId}/status` and an Application command that updates ticket status through the v1 lifecycle: `Open -> InProgress -> Resolved -> Closed`. IT admins can update any ticket; IT agents can update tickets assigned to them or unassigned queue tickets; employees cannot update status in this slice.

**Why this approach:** Status transition logic stays on the `Ticket` domain entity, while role/scope authorization stays in the Application command handler. This keeps business rules testable without HTTP or database dependencies and matches the existing Clean Architecture boundaries.

**Alternatives considered:** Letting employees close tickets immediately was deferred to a separate confirm-resolved/close-own-ticket slice because it changes the support workflow semantics. Allowing backward transitions or reopen was also deferred to keep the first lifecycle strict and predictable.

**Follow-ups / risks:** Frontend controls still need to call this endpoint. Reopen/escalation rules are future slices.

**Reviewed by human:** [ ]

---
## [2026-08-01] Link ticket detail back to chat conversation

**Prompt/task summary:** Make the Conversation field in ticket detail clickable so it opens the linked chat conversation.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The ticket detail Conversation context now renders as a router link to the Chat page with `conversationId` in the query string. The Chat page reads that query param after loading conversations and selects the matching conversation automatically.

**Why this approach:** Query params keep the existing Chat route simple while still making ticket-to-conversation navigation explicit and shareable. It avoids adding a new route shape before conversation detail routing is needed elsewhere.

**Alternatives considered:** Adding `/chat/conversations/:id` was deferred because the app currently uses the root Chat workspace route and only needs deep selection behavior for now.

**Follow-ups / risks:** If Chat later gets a dedicated conversation route, this link should move from query params to route params.

**Reviewed by human:** [ ]

---
## [2026-08-01] Fix chat ticket title and new conversation form layout

**Prompt/task summary:** Make tickets created from chat use the conversation title, and fix the New conversation form UI that expanded incorrectly in the conversation sidebar.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The chat-to-ticket handoff now sends the active conversation title as the ticket title instead of deriving it from the latest message. The conversation sidebar layout now uses flex column sizing so the New conversation form stays compact and the conversation list is the only section that grows and scrolls.

**Why this approach:** The conversation title is the user-defined issue name, so using it for the ticket title keeps chat and ticket records aligned. Flex column sizing is more robust than a fixed two-row grid when optional panels such as the creation form appear conditionally.

**Alternatives considered:** Keeping message-derived ticket titles was rejected because it can diverge from the user's explicit conversation name. Adding more grid rows was rejected because future conditional sidebar controls would recreate the same layout fragility.

**Follow-ups / risks:** Ticket details still show basic chat context only; richer context can be added when the backend exposes conversation title/message snippets directly.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add frontend tickets page and named conversation creation

**Prompt/task summary:** Continue after backend ticket list/detail APIs by adding the frontend Tickets page, and change Chat `New` conversation creation so the user can choose a conversation name.

**Files changed:**
- `frontend/src/app/app.routes.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/PROJECT_PLAN.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `/tickets` placeholder route with a real Tickets page that lists tickets, supports status/priority filters, opens detail in the same workspace, and displays title/status/priority/created metadata plus basic chat context via conversation/message IDs. The Chat page `New` action now opens a small title form before creating the conversation.

**Why this approach:** A split list/detail view matches the existing support-workspace layout and keeps repeated ticket triage efficient. For chat context, the frontend displays the IDs currently available from the backend detail contract instead of inventing another backend call in this slice.

**Alternatives considered:** A separate `/tickets/:id` page was deferred because the current app shell benefits from selecting tickets without route churn. Showing full originating message content was deferred until the backend exposes richer ticket context or a dedicated conversation summary endpoint.

**Follow-ups / risks:** Frontend status updates/comments are still future slices. Ticket detail can become richer once backend returns the originating message snippet or conversation title.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket list and detail APIs

**Prompt/task summary:** Implement the next backend slice for ticket list/detail APIs with status/priority filters and role-scoped authorization.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Api/Services/CurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ICurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ITicketRepository.cs`
- `backend/src/Application/Common/Interfaces/IUserRepository.cs`
- `backend/src/Application/Common/Models/PaginatedList.cs`
- `backend/src/Application/Common/Models/TicketListCriteria.cs`
- `backend/src/Application/Tickets/Models/TicketDetailDto.cs`
- `backend/src/Application/Tickets/Models/TicketSummaryDto.cs`
- `backend/src/Application/Tickets/Models/UserRefDto.cs`
- `backend/src/Application/Tickets/Queries/GetTicketDetail/*`
- `backend/src/Application/Tickets/Queries/GetTickets/*`
- `backend/src/Infrastructure/Persistence/Repositories/TicketRepository.cs`
- `backend/src/Infrastructure/Persistence/Repositories/UserRepository.cs`
- `backend/tests/Application.UnitTests/Auth/AuthCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Chat/ChatCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketQueryHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `GET /api/v1/tickets` and `GET /api/v1/tickets/{ticketId}`. Ticket list supports `status`, `priority`, `page`, and `pageSize`. The query handlers enforce role scope: employees see created tickets, IT agents see assigned plus unassigned queue tickets, and IT admins see all tickets. Responses include lightweight user references for creator and assignee.

**Why this approach:** Authorization stays in Application query handlers, matching the documented Clean Architecture boundary and keeping role behavior unit-testable without a database. The repository receives already-scoped criteria instead of reading HTTP claims or deciding product permissions itself.

**Alternatives considered:** Putting role checks in controllers was rejected because it would split business authorization across the API layer. Returning raw ticket entities was rejected because API DTOs keep response contracts explicit and prevent accidental field exposure.

**Follow-ups / risks:** Status update, comments, and frontend Tickets page are still separate slices. IT agent queue scope currently follows the documented v1 rule of assigned-to-me or unassigned tickets; if queues become department-based later, the criteria object can be extended.

**Reviewed by human:** [ ]

---
## [2026-08-01] Simplify chat ticket button template

**Prompt/task summary:** Check and fix the Chat page ticket button template after the user reported an error around the disabled/click bindings and conditional label.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `@if` block inside the ticket action button with a single interpolation expression that chooses between `Ticket created`, `Creating...`, and `Create ticket`.

**Why this approach:** The Angular compiler accepted the original template, but a simple interpolation is enough for this button label and is less likely to trigger editor/template tooling issues.

**Alternatives considered:** Keeping the `@if` block was valid Angular syntax, but it was more structure than this label needed.

**Follow-ups / risks:** None.

**Reviewed by human:** [ ]

---
## [2026-07-30] Add Phase 2 core ticketing data model

**Prompt/task summary:** Start Phase 2 by adding the core backend data model for conversations, messages, tickets, and ticket comments in one focused slice.

**Files changed:**
- `docs/ER_DIAGRAM.md`
- `docs/ai-changelog/AI_CHANGELOG.md`
- `backend/src/Domain/Entities/Conversation.cs`
- `backend/src/Domain/Entities/Message.cs`
- `backend/src/Domain/Entities/Ticket.cs`
- `backend/src/Domain/Entities/TicketComment.cs`
- `backend/src/Domain/Enums/MessageIntent.cs`
- `backend/src/Domain/Enums/MessageSender.cs`
- `backend/src/Domain/Enums/TicketPriority.cs`
- `backend/src/Domain/Enums/TicketStatus.cs`
- `backend/src/Infrastructure/Persistence/AppDbContext.cs`
- `backend/src/Infrastructure/Persistence/Configurations/ConversationConfiguration.cs`
- `backend/src/Infrastructure/Persistence/Configurations/MessageConfiguration.cs`
- `backend/src/Infrastructure/Persistence/Configurations/TicketConfiguration.cs`
- `backend/src/Infrastructure/Persistence/Configurations/TicketCommentConfiguration.cs`
- `backend/src/Infrastructure/Persistence/Migrations/20260730124936_AddCoreTicketingModel*`
- `backend/src/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `backend/tests/Application.UnitTests/Domain/CoreDataModelTests.cs`

**What changed:** Added domain entities and enum-backed state for conversations, messages, tickets, and ticket comments. Added EF Core mappings, DbSets, indexes, foreign keys, and the `AddCoreTicketingModel` migration for `conversations`, `messages`, `tickets`, and `ticket_comments`. Added unit tests for conversation creation/message timestamps, ticket defaults, ticket lifecycle transitions, assignment, and comment content normalization. Updated the ER documentation to match the implemented auth/ticketing schema notes.

**Why this approach:** This keeps Phase 2 focused on the relational foundation needed by the next chat happy-path slice without mixing in AI/RAG behavior. Ticket lifecycle rules live in the Domain entity so later Application handlers can call one policy instead of duplicating transition checks. `TicketComment` was included because it is already part of the documented ticket schema and does not require additional product decisions.

**Alternatives considered:** Implementing all Phase 2 tables including knowledge-base documents/chunks was deferred because vector dimensions and indexing depend on the selected embedding model, which should be confirmed during the RAG slice. Adding API endpoints in the same change was rejected to keep schema/domain risk separate from HTTP authorization behavior.

**Follow-ups / risks:** Apply the migration to any non-local database before using upcoming chat/ticket endpoints. The next slice should add Application repositories/use cases and API endpoints for the chat happy path.

**Reviewed by human:** [ ]

---

## [2026-08-01] Add frontend ticket status controls

**Prompt/task summary:** Continue the status workflow by wiring the frontend Tickets detail page to the backend status update endpoint.

**Files changed:**
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added a `PATCH /tickets/{id}/status` client method and a next-status action in Ticket detail. The UI shows the action only for `ITAgent` and `ITAdmin`, advances `Open -> InProgress -> Resolved -> Closed`, and synchronizes the selected detail and list item after a successful update.

**Why this approach:** A next-status button keeps the first workflow simple and prevents users from jumping to invalid states. Backend authorization and transition checks remain the source of truth; frontend role checks only keep the UI clean.

**Alternatives considered:** A free-form status dropdown was rejected for this slice because it exposes invalid choices that the backend would reject. Employee close/confirm-resolved controls remain deferred to a dedicated workflow slice.

**Follow-ups / risks:** Add assignment controls before agent-owned queues become more complex. Add frontend tests around role-specific rendering once the Tickets page has component-level test coverage.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket status workflow

**Prompt/task summary:** Implement the backend ticket status workflow slice before adding frontend status controls.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Application/Tickets/Commands/UpdateTicketStatus/*`
- `backend/src/Domain/Entities/Ticket.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketStatusCommandHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `PATCH /api/v1/tickets/{ticketId}/status` and an Application command that updates ticket status through the v1 lifecycle: `Open -> InProgress -> Resolved -> Closed`. IT admins can update any ticket; IT agents can update tickets assigned to them or unassigned queue tickets; employees cannot update status in this slice.

**Why this approach:** Status transition logic stays on the `Ticket` domain entity, while role/scope authorization stays in the Application command handler. This keeps business rules testable without HTTP or database dependencies and matches the existing Clean Architecture boundaries.

**Alternatives considered:** Letting employees close tickets immediately was deferred to a separate confirm-resolved/close-own-ticket slice because it changes the support workflow semantics. Allowing backward transitions or reopen was also deferred to keep the first lifecycle strict and predictable.

**Follow-ups / risks:** Frontend controls still need to call this endpoint. Reopen/escalation rules are future slices.

**Reviewed by human:** [ ]

---
## [2026-08-01] Link ticket detail back to chat conversation

**Prompt/task summary:** Make the Conversation field in ticket detail clickable so it opens the linked chat conversation.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The ticket detail Conversation context now renders as a router link to the Chat page with `conversationId` in the query string. The Chat page reads that query param after loading conversations and selects the matching conversation automatically.

**Why this approach:** Query params keep the existing Chat route simple while still making ticket-to-conversation navigation explicit and shareable. It avoids adding a new route shape before conversation detail routing is needed elsewhere.

**Alternatives considered:** Adding `/chat/conversations/:id` was deferred because the app currently uses the root Chat workspace route and only needs deep selection behavior for now.

**Follow-ups / risks:** If Chat later gets a dedicated conversation route, this link should move from query params to route params.

**Reviewed by human:** [ ]

---
## [2026-08-01] Fix chat ticket title and new conversation form layout

**Prompt/task summary:** Make tickets created from chat use the conversation title, and fix the New conversation form UI that expanded incorrectly in the conversation sidebar.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The chat-to-ticket handoff now sends the active conversation title as the ticket title instead of deriving it from the latest message. The conversation sidebar layout now uses flex column sizing so the New conversation form stays compact and the conversation list is the only section that grows and scrolls.

**Why this approach:** The conversation title is the user-defined issue name, so using it for the ticket title keeps chat and ticket records aligned. Flex column sizing is more robust than a fixed two-row grid when optional panels such as the creation form appear conditionally.

**Alternatives considered:** Keeping message-derived ticket titles was rejected because it can diverge from the user's explicit conversation name. Adding more grid rows was rejected because future conditional sidebar controls would recreate the same layout fragility.

**Follow-ups / risks:** Ticket details still show basic chat context only; richer context can be added when the backend exposes conversation title/message snippets directly.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add frontend tickets page and named conversation creation

**Prompt/task summary:** Continue after backend ticket list/detail APIs by adding the frontend Tickets page, and change Chat `New` conversation creation so the user can choose a conversation name.

**Files changed:**
- `frontend/src/app/app.routes.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/PROJECT_PLAN.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `/tickets` placeholder route with a real Tickets page that lists tickets, supports status/priority filters, opens detail in the same workspace, and displays title/status/priority/created metadata plus basic chat context via conversation/message IDs. The Chat page `New` action now opens a small title form before creating the conversation.

**Why this approach:** A split list/detail view matches the existing support-workspace layout and keeps repeated ticket triage efficient. For chat context, the frontend displays the IDs currently available from the backend detail contract instead of inventing another backend call in this slice.

**Alternatives considered:** A separate `/tickets/:id` page was deferred because the current app shell benefits from selecting tickets without route churn. Showing full originating message content was deferred until the backend exposes richer ticket context or a dedicated conversation summary endpoint.

**Follow-ups / risks:** Frontend status updates/comments are still future slices. Ticket detail can become richer once backend returns the originating message snippet or conversation title.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket list and detail APIs

**Prompt/task summary:** Implement the next backend slice for ticket list/detail APIs with status/priority filters and role-scoped authorization.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Api/Services/CurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ICurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ITicketRepository.cs`
- `backend/src/Application/Common/Interfaces/IUserRepository.cs`
- `backend/src/Application/Common/Models/PaginatedList.cs`
- `backend/src/Application/Common/Models/TicketListCriteria.cs`
- `backend/src/Application/Tickets/Models/TicketDetailDto.cs`
- `backend/src/Application/Tickets/Models/TicketSummaryDto.cs`
- `backend/src/Application/Tickets/Models/UserRefDto.cs`
- `backend/src/Application/Tickets/Queries/GetTicketDetail/*`
- `backend/src/Application/Tickets/Queries/GetTickets/*`
- `backend/src/Infrastructure/Persistence/Repositories/TicketRepository.cs`
- `backend/src/Infrastructure/Persistence/Repositories/UserRepository.cs`
- `backend/tests/Application.UnitTests/Auth/AuthCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Chat/ChatCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketQueryHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `GET /api/v1/tickets` and `GET /api/v1/tickets/{ticketId}`. Ticket list supports `status`, `priority`, `page`, and `pageSize`. The query handlers enforce role scope: employees see created tickets, IT agents see assigned plus unassigned queue tickets, and IT admins see all tickets. Responses include lightweight user references for creator and assignee.

**Why this approach:** Authorization stays in Application query handlers, matching the documented Clean Architecture boundary and keeping role behavior unit-testable without a database. The repository receives already-scoped criteria instead of reading HTTP claims or deciding product permissions itself.

**Alternatives considered:** Putting role checks in controllers was rejected because it would split business authorization across the API layer. Returning raw ticket entities was rejected because API DTOs keep response contracts explicit and prevent accidental field exposure.

**Follow-ups / risks:** Status update, comments, and frontend Tickets page are still separate slices. IT agent queue scope currently follows the documented v1 rule of assigned-to-me or unassigned tickets; if queues become department-based later, the criteria object can be extended.

**Reviewed by human:** [ ]

---
## [2026-08-01] Simplify chat ticket button template

**Prompt/task summary:** Check and fix the Chat page ticket button template after the user reported an error around the disabled/click bindings and conditional label.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `@if` block inside the ticket action button with a single interpolation expression that chooses between `Ticket created`, `Creating...`, and `Create ticket`.

**Why this approach:** The Angular compiler accepted the original template, but a simple interpolation is enough for this button label and is less likely to trigger editor/template tooling issues.

**Alternatives considered:** Keeping the `@if` block was valid Angular syntax, but it was more structure than this label needed.

**Follow-ups / risks:** None.

**Reviewed by human:** [ ]

---
## [2026-07-30] Remove browser default page margin

**Prompt/task summary:** The white border still appeared around the whole authenticated app after removing the placeholder card frame.

**Files changed:**
- `docs/ai-changelog/AI_CHANGELOG.md`
- `frontend/src/styles.scss`

**What changed:** Added global `html`/`body` styles to remove the browser default body margin, set the app background, and use border-box sizing globally.

**Why this approach:** The remaining white frame was around the entire viewport, which points to browser default body margin rather than component-level card styling. Fixing it in the global stylesheet makes the Angular app fill the viewport consistently across all routes.

**Alternatives considered:** Adding negative margins or expanding `.app-shell` was rejected because that would hide the symptom in one shell while leaving the global document margin intact.

**Follow-ups / risks:** None for this layout fix.

**Reviewed by human:** [ ]

---

## [2026-08-01] Add frontend ticket status controls

**Prompt/task summary:** Continue the status workflow by wiring the frontend Tickets detail page to the backend status update endpoint.

**Files changed:**
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added a `PATCH /tickets/{id}/status` client method and a next-status action in Ticket detail. The UI shows the action only for `ITAgent` and `ITAdmin`, advances `Open -> InProgress -> Resolved -> Closed`, and synchronizes the selected detail and list item after a successful update.

**Why this approach:** A next-status button keeps the first workflow simple and prevents users from jumping to invalid states. Backend authorization and transition checks remain the source of truth; frontend role checks only keep the UI clean.

**Alternatives considered:** A free-form status dropdown was rejected for this slice because it exposes invalid choices that the backend would reject. Employee close/confirm-resolved controls remain deferred to a dedicated workflow slice.

**Follow-ups / risks:** Add assignment controls before agent-owned queues become more complex. Add frontend tests around role-specific rendering once the Tickets page has component-level test coverage.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket status workflow

**Prompt/task summary:** Implement the backend ticket status workflow slice before adding frontend status controls.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Application/Tickets/Commands/UpdateTicketStatus/*`
- `backend/src/Domain/Entities/Ticket.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketStatusCommandHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `PATCH /api/v1/tickets/{ticketId}/status` and an Application command that updates ticket status through the v1 lifecycle: `Open -> InProgress -> Resolved -> Closed`. IT admins can update any ticket; IT agents can update tickets assigned to them or unassigned queue tickets; employees cannot update status in this slice.

**Why this approach:** Status transition logic stays on the `Ticket` domain entity, while role/scope authorization stays in the Application command handler. This keeps business rules testable without HTTP or database dependencies and matches the existing Clean Architecture boundaries.

**Alternatives considered:** Letting employees close tickets immediately was deferred to a separate confirm-resolved/close-own-ticket slice because it changes the support workflow semantics. Allowing backward transitions or reopen was also deferred to keep the first lifecycle strict and predictable.

**Follow-ups / risks:** Frontend controls still need to call this endpoint. Reopen/escalation rules are future slices.

**Reviewed by human:** [ ]

---
## [2026-08-01] Link ticket detail back to chat conversation

**Prompt/task summary:** Make the Conversation field in ticket detail clickable so it opens the linked chat conversation.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The ticket detail Conversation context now renders as a router link to the Chat page with `conversationId` in the query string. The Chat page reads that query param after loading conversations and selects the matching conversation automatically.

**Why this approach:** Query params keep the existing Chat route simple while still making ticket-to-conversation navigation explicit and shareable. It avoids adding a new route shape before conversation detail routing is needed elsewhere.

**Alternatives considered:** Adding `/chat/conversations/:id` was deferred because the app currently uses the root Chat workspace route and only needs deep selection behavior for now.

**Follow-ups / risks:** If Chat later gets a dedicated conversation route, this link should move from query params to route params.

**Reviewed by human:** [ ]

---
## [2026-08-01] Fix chat ticket title and new conversation form layout

**Prompt/task summary:** Make tickets created from chat use the conversation title, and fix the New conversation form UI that expanded incorrectly in the conversation sidebar.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The chat-to-ticket handoff now sends the active conversation title as the ticket title instead of deriving it from the latest message. The conversation sidebar layout now uses flex column sizing so the New conversation form stays compact and the conversation list is the only section that grows and scrolls.

**Why this approach:** The conversation title is the user-defined issue name, so using it for the ticket title keeps chat and ticket records aligned. Flex column sizing is more robust than a fixed two-row grid when optional panels such as the creation form appear conditionally.

**Alternatives considered:** Keeping message-derived ticket titles was rejected because it can diverge from the user's explicit conversation name. Adding more grid rows was rejected because future conditional sidebar controls would recreate the same layout fragility.

**Follow-ups / risks:** Ticket details still show basic chat context only; richer context can be added when the backend exposes conversation title/message snippets directly.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add frontend tickets page and named conversation creation

**Prompt/task summary:** Continue after backend ticket list/detail APIs by adding the frontend Tickets page, and change Chat `New` conversation creation so the user can choose a conversation name.

**Files changed:**
- `frontend/src/app/app.routes.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/PROJECT_PLAN.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `/tickets` placeholder route with a real Tickets page that lists tickets, supports status/priority filters, opens detail in the same workspace, and displays title/status/priority/created metadata plus basic chat context via conversation/message IDs. The Chat page `New` action now opens a small title form before creating the conversation.

**Why this approach:** A split list/detail view matches the existing support-workspace layout and keeps repeated ticket triage efficient. For chat context, the frontend displays the IDs currently available from the backend detail contract instead of inventing another backend call in this slice.

**Alternatives considered:** A separate `/tickets/:id` page was deferred because the current app shell benefits from selecting tickets without route churn. Showing full originating message content was deferred until the backend exposes richer ticket context or a dedicated conversation summary endpoint.

**Follow-ups / risks:** Frontend status updates/comments are still future slices. Ticket detail can become richer once backend returns the originating message snippet or conversation title.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket list and detail APIs

**Prompt/task summary:** Implement the next backend slice for ticket list/detail APIs with status/priority filters and role-scoped authorization.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Api/Services/CurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ICurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ITicketRepository.cs`
- `backend/src/Application/Common/Interfaces/IUserRepository.cs`
- `backend/src/Application/Common/Models/PaginatedList.cs`
- `backend/src/Application/Common/Models/TicketListCriteria.cs`
- `backend/src/Application/Tickets/Models/TicketDetailDto.cs`
- `backend/src/Application/Tickets/Models/TicketSummaryDto.cs`
- `backend/src/Application/Tickets/Models/UserRefDto.cs`
- `backend/src/Application/Tickets/Queries/GetTicketDetail/*`
- `backend/src/Application/Tickets/Queries/GetTickets/*`
- `backend/src/Infrastructure/Persistence/Repositories/TicketRepository.cs`
- `backend/src/Infrastructure/Persistence/Repositories/UserRepository.cs`
- `backend/tests/Application.UnitTests/Auth/AuthCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Chat/ChatCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketQueryHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `GET /api/v1/tickets` and `GET /api/v1/tickets/{ticketId}`. Ticket list supports `status`, `priority`, `page`, and `pageSize`. The query handlers enforce role scope: employees see created tickets, IT agents see assigned plus unassigned queue tickets, and IT admins see all tickets. Responses include lightweight user references for creator and assignee.

**Why this approach:** Authorization stays in Application query handlers, matching the documented Clean Architecture boundary and keeping role behavior unit-testable without a database. The repository receives already-scoped criteria instead of reading HTTP claims or deciding product permissions itself.

**Alternatives considered:** Putting role checks in controllers was rejected because it would split business authorization across the API layer. Returning raw ticket entities was rejected because API DTOs keep response contracts explicit and prevent accidental field exposure.

**Follow-ups / risks:** Status update, comments, and frontend Tickets page are still separate slices. IT agent queue scope currently follows the documented v1 rule of assigned-to-me or unassigned tickets; if queues become department-based later, the criteria object can be extended.

**Reviewed by human:** [ ]

---
## [2026-08-01] Simplify chat ticket button template

**Prompt/task summary:** Check and fix the Chat page ticket button template after the user reported an error around the disabled/click bindings and conditional label.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `@if` block inside the ticket action button with a single interpolation expression that chooses between `Ticket created`, `Creating...`, and `Create ticket`.

**Why this approach:** The Angular compiler accepted the original template, but a simple interpolation is enough for this button label and is less likely to trigger editor/template tooling issues.

**Alternatives considered:** Keeping the `@if` block was valid Angular syntax, but it was more structure than this label needed.

**Follow-ups / risks:** None.

**Reviewed by human:** [ ]

---
## [2026-07-30] Remove placeholder page frame

**Prompt/task summary:** Remove the white framed placeholder panel shown after login so the protected workspace content does not look like a full-page card.

**Files changed:**
- `docs/ai-changelog/AI_CHANGELOG.md`
- `frontend/src/app/features/dashboard/feature/placeholder-page.component.ts`

**What changed:** Removed the placeholder page border, white background, radius, and card padding, leaving the route content as unframed text on the workspace surface.

**Why this approach:** The white frame was coming from the temporary protected-route placeholder component, not the app shell. Removing the card styling keeps the authenticated shell cleaner and follows the frontend guidance to avoid unnecessary page-section cards.

**Alternatives considered:** Changing the whole workspace background was rejected because the screenshot issue was localized to the placeholder component and broader layout changes would affect auth/dashboard surfaces unnecessarily.

**Follow-ups / risks:** Replace this temporary placeholder with the real chat workspace in the next slice.

**Reviewed by human:** [ ]

---

## [2026-08-01] Add frontend ticket status controls

**Prompt/task summary:** Continue the status workflow by wiring the frontend Tickets detail page to the backend status update endpoint.

**Files changed:**
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added a `PATCH /tickets/{id}/status` client method and a next-status action in Ticket detail. The UI shows the action only for `ITAgent` and `ITAdmin`, advances `Open -> InProgress -> Resolved -> Closed`, and synchronizes the selected detail and list item after a successful update.

**Why this approach:** A next-status button keeps the first workflow simple and prevents users from jumping to invalid states. Backend authorization and transition checks remain the source of truth; frontend role checks only keep the UI clean.

**Alternatives considered:** A free-form status dropdown was rejected for this slice because it exposes invalid choices that the backend would reject. Employee close/confirm-resolved controls remain deferred to a dedicated workflow slice.

**Follow-ups / risks:** Add assignment controls before agent-owned queues become more complex. Add frontend tests around role-specific rendering once the Tickets page has component-level test coverage.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket status workflow

**Prompt/task summary:** Implement the backend ticket status workflow slice before adding frontend status controls.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Application/Tickets/Commands/UpdateTicketStatus/*`
- `backend/src/Domain/Entities/Ticket.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketStatusCommandHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `PATCH /api/v1/tickets/{ticketId}/status` and an Application command that updates ticket status through the v1 lifecycle: `Open -> InProgress -> Resolved -> Closed`. IT admins can update any ticket; IT agents can update tickets assigned to them or unassigned queue tickets; employees cannot update status in this slice.

**Why this approach:** Status transition logic stays on the `Ticket` domain entity, while role/scope authorization stays in the Application command handler. This keeps business rules testable without HTTP or database dependencies and matches the existing Clean Architecture boundaries.

**Alternatives considered:** Letting employees close tickets immediately was deferred to a separate confirm-resolved/close-own-ticket slice because it changes the support workflow semantics. Allowing backward transitions or reopen was also deferred to keep the first lifecycle strict and predictable.

**Follow-ups / risks:** Frontend controls still need to call this endpoint. Reopen/escalation rules are future slices.

**Reviewed by human:** [ ]

---
## [2026-08-01] Link ticket detail back to chat conversation

**Prompt/task summary:** Make the Conversation field in ticket detail clickable so it opens the linked chat conversation.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The ticket detail Conversation context now renders as a router link to the Chat page with `conversationId` in the query string. The Chat page reads that query param after loading conversations and selects the matching conversation automatically.

**Why this approach:** Query params keep the existing Chat route simple while still making ticket-to-conversation navigation explicit and shareable. It avoids adding a new route shape before conversation detail routing is needed elsewhere.

**Alternatives considered:** Adding `/chat/conversations/:id` was deferred because the app currently uses the root Chat workspace route and only needs deep selection behavior for now.

**Follow-ups / risks:** If Chat later gets a dedicated conversation route, this link should move from query params to route params.

**Reviewed by human:** [ ]

---
## [2026-08-01] Fix chat ticket title and new conversation form layout

**Prompt/task summary:** Make tickets created from chat use the conversation title, and fix the New conversation form UI that expanded incorrectly in the conversation sidebar.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The chat-to-ticket handoff now sends the active conversation title as the ticket title instead of deriving it from the latest message. The conversation sidebar layout now uses flex column sizing so the New conversation form stays compact and the conversation list is the only section that grows and scrolls.

**Why this approach:** The conversation title is the user-defined issue name, so using it for the ticket title keeps chat and ticket records aligned. Flex column sizing is more robust than a fixed two-row grid when optional panels such as the creation form appear conditionally.

**Alternatives considered:** Keeping message-derived ticket titles was rejected because it can diverge from the user's explicit conversation name. Adding more grid rows was rejected because future conditional sidebar controls would recreate the same layout fragility.

**Follow-ups / risks:** Ticket details still show basic chat context only; richer context can be added when the backend exposes conversation title/message snippets directly.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add frontend tickets page and named conversation creation

**Prompt/task summary:** Continue after backend ticket list/detail APIs by adding the frontend Tickets page, and change Chat `New` conversation creation so the user can choose a conversation name.

**Files changed:**
- `frontend/src/app/app.routes.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/PROJECT_PLAN.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `/tickets` placeholder route with a real Tickets page that lists tickets, supports status/priority filters, opens detail in the same workspace, and displays title/status/priority/created metadata plus basic chat context via conversation/message IDs. The Chat page `New` action now opens a small title form before creating the conversation.

**Why this approach:** A split list/detail view matches the existing support-workspace layout and keeps repeated ticket triage efficient. For chat context, the frontend displays the IDs currently available from the backend detail contract instead of inventing another backend call in this slice.

**Alternatives considered:** A separate `/tickets/:id` page was deferred because the current app shell benefits from selecting tickets without route churn. Showing full originating message content was deferred until the backend exposes richer ticket context or a dedicated conversation summary endpoint.

**Follow-ups / risks:** Frontend status updates/comments are still future slices. Ticket detail can become richer once backend returns the originating message snippet or conversation title.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket list and detail APIs

**Prompt/task summary:** Implement the next backend slice for ticket list/detail APIs with status/priority filters and role-scoped authorization.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Api/Services/CurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ICurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ITicketRepository.cs`
- `backend/src/Application/Common/Interfaces/IUserRepository.cs`
- `backend/src/Application/Common/Models/PaginatedList.cs`
- `backend/src/Application/Common/Models/TicketListCriteria.cs`
- `backend/src/Application/Tickets/Models/TicketDetailDto.cs`
- `backend/src/Application/Tickets/Models/TicketSummaryDto.cs`
- `backend/src/Application/Tickets/Models/UserRefDto.cs`
- `backend/src/Application/Tickets/Queries/GetTicketDetail/*`
- `backend/src/Application/Tickets/Queries/GetTickets/*`
- `backend/src/Infrastructure/Persistence/Repositories/TicketRepository.cs`
- `backend/src/Infrastructure/Persistence/Repositories/UserRepository.cs`
- `backend/tests/Application.UnitTests/Auth/AuthCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Chat/ChatCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketQueryHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `GET /api/v1/tickets` and `GET /api/v1/tickets/{ticketId}`. Ticket list supports `status`, `priority`, `page`, and `pageSize`. The query handlers enforce role scope: employees see created tickets, IT agents see assigned plus unassigned queue tickets, and IT admins see all tickets. Responses include lightweight user references for creator and assignee.

**Why this approach:** Authorization stays in Application query handlers, matching the documented Clean Architecture boundary and keeping role behavior unit-testable without a database. The repository receives already-scoped criteria instead of reading HTTP claims or deciding product permissions itself.

**Alternatives considered:** Putting role checks in controllers was rejected because it would split business authorization across the API layer. Returning raw ticket entities was rejected because API DTOs keep response contracts explicit and prevent accidental field exposure.

**Follow-ups / risks:** Status update, comments, and frontend Tickets page are still separate slices. IT agent queue scope currently follows the documented v1 rule of assigned-to-me or unassigned tickets; if queues become department-based later, the criteria object can be extended.

**Reviewed by human:** [ ]

---
## [2026-08-01] Simplify chat ticket button template

**Prompt/task summary:** Check and fix the Chat page ticket button template after the user reported an error around the disabled/click bindings and conditional label.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `@if` block inside the ticket action button with a single interpolation expression that chooses between `Ticket created`, `Creating...`, and `Create ticket`.

**Why this approach:** The Angular compiler accepted the original template, but a simple interpolation is enough for this button label and is less likely to trigger editor/template tooling issues.

**Alternatives considered:** Keeping the `@if` block was valid Angular syntax, but it was more structure than this label needed.

**Follow-ups / risks:** None.

**Reviewed by human:** [ ]

---
## [2026-07-30] Fix frontend auth CORS and register feedback

**Prompt/task summary:** Login failed in the browser with a CORS preflight error, while Postman worked; the register button also appeared to do nothing.

**Files changed:**
- `.env.example`
- `docs/ai-changelog/AI_CHANGELOG.md`
- `backend/src/Api/Program.cs`
- `backend/src/Api/appsettings.json`
- `frontend/src/app/features/auth/feature/register-page.component.ts`
- `frontend/src/app/features/auth/feature/register-page.component.html`
- `frontend/src/app/features/auth/feature/auth-page.component.scss`

**What changed:** Added `Cors:AllowedOrigins` for `http://localhost:4200`, moved CORS middleware before HTTPS redirection/auth in the API pipeline, and verified the browser preflight path returns `204` with `Access-Control-Allow-Origin: http://localhost:4200`. Added visible register-form validation and backend error feedback so invalid form submission no longer feels like a dead button.

**Why this approach:** Postman bypasses browser CORS, so the failing path was the API preflight response rather than the auth command itself. CORS must be configured with the actual Angular origin and run early enough in the middleware pipeline to handle `OPTIONS` before auth or redirects interfere. Register feedback belongs in the component because the form was already correctly blocking invalid submissions, but the UI did not explain why.

**Alternatives considered:** Disabling CORS globally or allowing every origin was rejected because the project docs call for CORS to be locked down to known frontend origins. Making the register button always disabled until valid was deferred because explicit field-level feedback is clearer while testing.

**Follow-ups / risks:** Add environment-specific CORS values when deployment origins are known. The frontend still uses localStorage token storage for the dev slice and should be revisited before production.

**Reviewed by human:** [ ]

---

## [2026-08-01] Add frontend ticket status controls

**Prompt/task summary:** Continue the status workflow by wiring the frontend Tickets detail page to the backend status update endpoint.

**Files changed:**
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added a `PATCH /tickets/{id}/status` client method and a next-status action in Ticket detail. The UI shows the action only for `ITAgent` and `ITAdmin`, advances `Open -> InProgress -> Resolved -> Closed`, and synchronizes the selected detail and list item after a successful update.

**Why this approach:** A next-status button keeps the first workflow simple and prevents users from jumping to invalid states. Backend authorization and transition checks remain the source of truth; frontend role checks only keep the UI clean.

**Alternatives considered:** A free-form status dropdown was rejected for this slice because it exposes invalid choices that the backend would reject. Employee close/confirm-resolved controls remain deferred to a dedicated workflow slice.

**Follow-ups / risks:** Add assignment controls before agent-owned queues become more complex. Add frontend tests around role-specific rendering once the Tickets page has component-level test coverage.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket status workflow

**Prompt/task summary:** Implement the backend ticket status workflow slice before adding frontend status controls.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Application/Tickets/Commands/UpdateTicketStatus/*`
- `backend/src/Domain/Entities/Ticket.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketStatusCommandHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `PATCH /api/v1/tickets/{ticketId}/status` and an Application command that updates ticket status through the v1 lifecycle: `Open -> InProgress -> Resolved -> Closed`. IT admins can update any ticket; IT agents can update tickets assigned to them or unassigned queue tickets; employees cannot update status in this slice.

**Why this approach:** Status transition logic stays on the `Ticket` domain entity, while role/scope authorization stays in the Application command handler. This keeps business rules testable without HTTP or database dependencies and matches the existing Clean Architecture boundaries.

**Alternatives considered:** Letting employees close tickets immediately was deferred to a separate confirm-resolved/close-own-ticket slice because it changes the support workflow semantics. Allowing backward transitions or reopen was also deferred to keep the first lifecycle strict and predictable.

**Follow-ups / risks:** Frontend controls still need to call this endpoint. Reopen/escalation rules are future slices.

**Reviewed by human:** [ ]

---
## [2026-08-01] Link ticket detail back to chat conversation

**Prompt/task summary:** Make the Conversation field in ticket detail clickable so it opens the linked chat conversation.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The ticket detail Conversation context now renders as a router link to the Chat page with `conversationId` in the query string. The Chat page reads that query param after loading conversations and selects the matching conversation automatically.

**Why this approach:** Query params keep the existing Chat route simple while still making ticket-to-conversation navigation explicit and shareable. It avoids adding a new route shape before conversation detail routing is needed elsewhere.

**Alternatives considered:** Adding `/chat/conversations/:id` was deferred because the app currently uses the root Chat workspace route and only needs deep selection behavior for now.

**Follow-ups / risks:** If Chat later gets a dedicated conversation route, this link should move from query params to route params.

**Reviewed by human:** [ ]

---
## [2026-08-01] Fix chat ticket title and new conversation form layout

**Prompt/task summary:** Make tickets created from chat use the conversation title, and fix the New conversation form UI that expanded incorrectly in the conversation sidebar.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The chat-to-ticket handoff now sends the active conversation title as the ticket title instead of deriving it from the latest message. The conversation sidebar layout now uses flex column sizing so the New conversation form stays compact and the conversation list is the only section that grows and scrolls.

**Why this approach:** The conversation title is the user-defined issue name, so using it for the ticket title keeps chat and ticket records aligned. Flex column sizing is more robust than a fixed two-row grid when optional panels such as the creation form appear conditionally.

**Alternatives considered:** Keeping message-derived ticket titles was rejected because it can diverge from the user's explicit conversation name. Adding more grid rows was rejected because future conditional sidebar controls would recreate the same layout fragility.

**Follow-ups / risks:** Ticket details still show basic chat context only; richer context can be added when the backend exposes conversation title/message snippets directly.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add frontend tickets page and named conversation creation

**Prompt/task summary:** Continue after backend ticket list/detail APIs by adding the frontend Tickets page, and change Chat `New` conversation creation so the user can choose a conversation name.

**Files changed:**
- `frontend/src/app/app.routes.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/PROJECT_PLAN.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `/tickets` placeholder route with a real Tickets page that lists tickets, supports status/priority filters, opens detail in the same workspace, and displays title/status/priority/created metadata plus basic chat context via conversation/message IDs. The Chat page `New` action now opens a small title form before creating the conversation.

**Why this approach:** A split list/detail view matches the existing support-workspace layout and keeps repeated ticket triage efficient. For chat context, the frontend displays the IDs currently available from the backend detail contract instead of inventing another backend call in this slice.

**Alternatives considered:** A separate `/tickets/:id` page was deferred because the current app shell benefits from selecting tickets without route churn. Showing full originating message content was deferred until the backend exposes richer ticket context or a dedicated conversation summary endpoint.

**Follow-ups / risks:** Frontend status updates/comments are still future slices. Ticket detail can become richer once backend returns the originating message snippet or conversation title.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket list and detail APIs

**Prompt/task summary:** Implement the next backend slice for ticket list/detail APIs with status/priority filters and role-scoped authorization.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Api/Services/CurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ICurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ITicketRepository.cs`
- `backend/src/Application/Common/Interfaces/IUserRepository.cs`
- `backend/src/Application/Common/Models/PaginatedList.cs`
- `backend/src/Application/Common/Models/TicketListCriteria.cs`
- `backend/src/Application/Tickets/Models/TicketDetailDto.cs`
- `backend/src/Application/Tickets/Models/TicketSummaryDto.cs`
- `backend/src/Application/Tickets/Models/UserRefDto.cs`
- `backend/src/Application/Tickets/Queries/GetTicketDetail/*`
- `backend/src/Application/Tickets/Queries/GetTickets/*`
- `backend/src/Infrastructure/Persistence/Repositories/TicketRepository.cs`
- `backend/src/Infrastructure/Persistence/Repositories/UserRepository.cs`
- `backend/tests/Application.UnitTests/Auth/AuthCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Chat/ChatCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketQueryHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `GET /api/v1/tickets` and `GET /api/v1/tickets/{ticketId}`. Ticket list supports `status`, `priority`, `page`, and `pageSize`. The query handlers enforce role scope: employees see created tickets, IT agents see assigned plus unassigned queue tickets, and IT admins see all tickets. Responses include lightweight user references for creator and assignee.

**Why this approach:** Authorization stays in Application query handlers, matching the documented Clean Architecture boundary and keeping role behavior unit-testable without a database. The repository receives already-scoped criteria instead of reading HTTP claims or deciding product permissions itself.

**Alternatives considered:** Putting role checks in controllers was rejected because it would split business authorization across the API layer. Returning raw ticket entities was rejected because API DTOs keep response contracts explicit and prevent accidental field exposure.

**Follow-ups / risks:** Status update, comments, and frontend Tickets page are still separate slices. IT agent queue scope currently follows the documented v1 rule of assigned-to-me or unassigned tickets; if queues become department-based later, the criteria object can be extended.

**Reviewed by human:** [ ]

---
## [2026-08-01] Simplify chat ticket button template

**Prompt/task summary:** Check and fix the Chat page ticket button template after the user reported an error around the disabled/click bindings and conditional label.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `@if` block inside the ticket action button with a single interpolation expression that chooses between `Ticket created`, `Creating...`, and `Create ticket`.

**Why this approach:** The Angular compiler accepted the original template, but a simple interpolation is enough for this button label and is less likely to trigger editor/template tooling issues.

**Alternatives considered:** Keeping the `@if` block was valid Angular syntax, but it was more structure than this label needed.

**Follow-ups / risks:** None.

**Reviewed by human:** [ ]

---
## [2026-07-30] Add frontend auth flow

**Prompt/task summary:** Continue Phase 1 by adding frontend login/register pages, auth service, token storage, HTTP interceptor, and route guard.

**Files changed:**
- `docs/ai-changelog/AI_CHANGELOG.md`
- `frontend/src/app/app.config.ts`
- `frontend/src/app/app.html`
- `frontend/src/app/app.routes.ts`
- `frontend/src/app/app.scss`
- `frontend/src/app/app.spec.ts`
- `frontend/src/app/core/auth/**`
- `frontend/src/app/environments/environment.ts`
- `frontend/src/app/features/auth/feature/**`
- `frontend/src/app/features/dashboard/feature/placeholder-page.component.ts`

**What changed:** Added typed auth DTOs, `AuthService` with local development token storage, a Bearer-token HTTP interceptor, and an `authGuard` that redirects unauthenticated users to `/login`. Added standalone login/register pages using reactive forms, updated routes for protected app pages, and adjusted the app shell to show authenticated navigation plus logout. Added protected placeholder pages so the guarded navigation can be tested before chat/ticket features are implemented. Added a dev CORS policy so the Angular app at `http://localhost:4200` can call the API at `http://localhost:5175`. Ignored local Angular/API dev-server log files used for background testing.

**Why this approach:** This follows the frontend structure in `docs/PROJECT_PLAN.md` by keeping app-wide auth concerns under `core/auth` and feature pages under `features/auth/feature`. Local storage is acceptable for this dev slice because the backend currently issues JSON tokens and has no cookie/session policy; keeping all storage behind `AuthService` leaves room to swap the strategy later. The interceptor stays narrow and only attaches the access token so refresh/retry behavior can be added deliberately after logout/revoke semantics exist.

**Alternatives considered:** Adding Angular Material was rejected for now because it would be a new design-system dependency and the current shell uses plain SCSS. Automatically refreshing on every `401` was deferred because the backend does not yet expose logout/revoke or session cleanup behavior, so silent retry policy should be designed with those endpoints together.

**Follow-ups / risks:** Token storage is developer-oriented and should be revisited before production. Add logout/revoke support on the backend, then extend the interceptor to handle one-shot refresh on `401` responses.

**Reviewed by human:** [ ]

---

## [2026-08-01] Add frontend ticket status controls

**Prompt/task summary:** Continue the status workflow by wiring the frontend Tickets detail page to the backend status update endpoint.

**Files changed:**
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added a `PATCH /tickets/{id}/status` client method and a next-status action in Ticket detail. The UI shows the action only for `ITAgent` and `ITAdmin`, advances `Open -> InProgress -> Resolved -> Closed`, and synchronizes the selected detail and list item after a successful update.

**Why this approach:** A next-status button keeps the first workflow simple and prevents users from jumping to invalid states. Backend authorization and transition checks remain the source of truth; frontend role checks only keep the UI clean.

**Alternatives considered:** A free-form status dropdown was rejected for this slice because it exposes invalid choices that the backend would reject. Employee close/confirm-resolved controls remain deferred to a dedicated workflow slice.

**Follow-ups / risks:** Add assignment controls before agent-owned queues become more complex. Add frontend tests around role-specific rendering once the Tickets page has component-level test coverage.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket status workflow

**Prompt/task summary:** Implement the backend ticket status workflow slice before adding frontend status controls.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Application/Tickets/Commands/UpdateTicketStatus/*`
- `backend/src/Domain/Entities/Ticket.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketStatusCommandHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `PATCH /api/v1/tickets/{ticketId}/status` and an Application command that updates ticket status through the v1 lifecycle: `Open -> InProgress -> Resolved -> Closed`. IT admins can update any ticket; IT agents can update tickets assigned to them or unassigned queue tickets; employees cannot update status in this slice.

**Why this approach:** Status transition logic stays on the `Ticket` domain entity, while role/scope authorization stays in the Application command handler. This keeps business rules testable without HTTP or database dependencies and matches the existing Clean Architecture boundaries.

**Alternatives considered:** Letting employees close tickets immediately was deferred to a separate confirm-resolved/close-own-ticket slice because it changes the support workflow semantics. Allowing backward transitions or reopen was also deferred to keep the first lifecycle strict and predictable.

**Follow-ups / risks:** Frontend controls still need to call this endpoint. Reopen/escalation rules are future slices.

**Reviewed by human:** [ ]

---
## [2026-08-01] Link ticket detail back to chat conversation

**Prompt/task summary:** Make the Conversation field in ticket detail clickable so it opens the linked chat conversation.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The ticket detail Conversation context now renders as a router link to the Chat page with `conversationId` in the query string. The Chat page reads that query param after loading conversations and selects the matching conversation automatically.

**Why this approach:** Query params keep the existing Chat route simple while still making ticket-to-conversation navigation explicit and shareable. It avoids adding a new route shape before conversation detail routing is needed elsewhere.

**Alternatives considered:** Adding `/chat/conversations/:id` was deferred because the app currently uses the root Chat workspace route and only needs deep selection behavior for now.

**Follow-ups / risks:** If Chat later gets a dedicated conversation route, this link should move from query params to route params.

**Reviewed by human:** [ ]

---
## [2026-08-01] Fix chat ticket title and new conversation form layout

**Prompt/task summary:** Make tickets created from chat use the conversation title, and fix the New conversation form UI that expanded incorrectly in the conversation sidebar.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The chat-to-ticket handoff now sends the active conversation title as the ticket title instead of deriving it from the latest message. The conversation sidebar layout now uses flex column sizing so the New conversation form stays compact and the conversation list is the only section that grows and scrolls.

**Why this approach:** The conversation title is the user-defined issue name, so using it for the ticket title keeps chat and ticket records aligned. Flex column sizing is more robust than a fixed two-row grid when optional panels such as the creation form appear conditionally.

**Alternatives considered:** Keeping message-derived ticket titles was rejected because it can diverge from the user's explicit conversation name. Adding more grid rows was rejected because future conditional sidebar controls would recreate the same layout fragility.

**Follow-ups / risks:** Ticket details still show basic chat context only; richer context can be added when the backend exposes conversation title/message snippets directly.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add frontend tickets page and named conversation creation

**Prompt/task summary:** Continue after backend ticket list/detail APIs by adding the frontend Tickets page, and change Chat `New` conversation creation so the user can choose a conversation name.

**Files changed:**
- `frontend/src/app/app.routes.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/PROJECT_PLAN.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `/tickets` placeholder route with a real Tickets page that lists tickets, supports status/priority filters, opens detail in the same workspace, and displays title/status/priority/created metadata plus basic chat context via conversation/message IDs. The Chat page `New` action now opens a small title form before creating the conversation.

**Why this approach:** A split list/detail view matches the existing support-workspace layout and keeps repeated ticket triage efficient. For chat context, the frontend displays the IDs currently available from the backend detail contract instead of inventing another backend call in this slice.

**Alternatives considered:** A separate `/tickets/:id` page was deferred because the current app shell benefits from selecting tickets without route churn. Showing full originating message content was deferred until the backend exposes richer ticket context or a dedicated conversation summary endpoint.

**Follow-ups / risks:** Frontend status updates/comments are still future slices. Ticket detail can become richer once backend returns the originating message snippet or conversation title.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket list and detail APIs

**Prompt/task summary:** Implement the next backend slice for ticket list/detail APIs with status/priority filters and role-scoped authorization.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Api/Services/CurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ICurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ITicketRepository.cs`
- `backend/src/Application/Common/Interfaces/IUserRepository.cs`
- `backend/src/Application/Common/Models/PaginatedList.cs`
- `backend/src/Application/Common/Models/TicketListCriteria.cs`
- `backend/src/Application/Tickets/Models/TicketDetailDto.cs`
- `backend/src/Application/Tickets/Models/TicketSummaryDto.cs`
- `backend/src/Application/Tickets/Models/UserRefDto.cs`
- `backend/src/Application/Tickets/Queries/GetTicketDetail/*`
- `backend/src/Application/Tickets/Queries/GetTickets/*`
- `backend/src/Infrastructure/Persistence/Repositories/TicketRepository.cs`
- `backend/src/Infrastructure/Persistence/Repositories/UserRepository.cs`
- `backend/tests/Application.UnitTests/Auth/AuthCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Chat/ChatCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketQueryHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `GET /api/v1/tickets` and `GET /api/v1/tickets/{ticketId}`. Ticket list supports `status`, `priority`, `page`, and `pageSize`. The query handlers enforce role scope: employees see created tickets, IT agents see assigned plus unassigned queue tickets, and IT admins see all tickets. Responses include lightweight user references for creator and assignee.

**Why this approach:** Authorization stays in Application query handlers, matching the documented Clean Architecture boundary and keeping role behavior unit-testable without a database. The repository receives already-scoped criteria instead of reading HTTP claims or deciding product permissions itself.

**Alternatives considered:** Putting role checks in controllers was rejected because it would split business authorization across the API layer. Returning raw ticket entities was rejected because API DTOs keep response contracts explicit and prevent accidental field exposure.

**Follow-ups / risks:** Status update, comments, and frontend Tickets page are still separate slices. IT agent queue scope currently follows the documented v1 rule of assigned-to-me or unassigned tickets; if queues become department-based later, the criteria object can be extended.

**Reviewed by human:** [ ]

---
## [2026-08-01] Simplify chat ticket button template

**Prompt/task summary:** Check and fix the Chat page ticket button template after the user reported an error around the disabled/click bindings and conditional label.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `@if` block inside the ticket action button with a single interpolation expression that chooses between `Ticket created`, `Creating...`, and `Create ticket`.

**Why this approach:** The Angular compiler accepted the original template, but a simple interpolation is enough for this button label and is less likely to trigger editor/template tooling issues.

**Alternatives considered:** Keeping the `@if` block was valid Angular syntax, but it was more structure than this label needed.

**Follow-ups / risks:** None.

**Reviewed by human:** [ ]

---
## [2026-07-30] Persist and rotate refresh tokens

**Prompt/task summary:** Continue Phase 1 after the .NET 10 migration by implementing the next auth slice: persisted refresh tokens and the `/api/v1/auth/refresh` endpoint.

**Files changed:**
- `docs/API_SPEC.md`
- `docs/ER_DIAGRAM.md`
- `docs/ai-changelog/AI_CHANGELOG.md`
- `backend/src/Api/Controllers/AuthController.cs`
- `backend/src/Application/Auth/Commands/Login/LoginCommandHandler.cs`
- `backend/src/Application/Auth/Commands/Refresh/**`
- `backend/src/Application/Auth/Models/TokenResult.cs`
- `backend/src/Application/Common/Interfaces/IJwtTokenService.cs`
- `backend/src/Application/Common/Interfaces/IRefreshTokenRepository.cs`
- `backend/src/Application/Common/Interfaces/IUserRepository.cs`
- `backend/src/CompositionRoot/DependencyInjection.cs`
- `backend/src/Domain/Entities/RefreshToken.cs`
- `backend/src/Infrastructure/Identity/JwtTokenService.cs`
- `backend/src/Infrastructure/Persistence/AppDbContext.cs`
- `backend/src/Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs`
- `backend/src/Infrastructure/Persistence/Migrations/20260730110653_AddRefreshTokens*`
- `backend/src/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `backend/src/Infrastructure/Persistence/Repositories/RefreshTokenRepository.cs`
- `backend/src/Infrastructure/Persistence/Repositories/UserRepository.cs`
- `backend/tests/Application.UnitTests/Auth/AuthCommandHandlerTests.cs`

**What changed:** Added a `RefreshToken` domain entity, repository interface/implementation, EF configuration, migration, and `refresh_tokens` table. Login now hashes and persists the refresh token before returning it. Added `RefreshCommand` handling for `/api/v1/auth/refresh`, which validates an active token by hash, issues a new access/refresh token pair, revokes the old token, and stores the replacement hash. Updated auth tests from four to six scenarios. Applied the migration to the local Docker PostgreSQL database and smoke-tested register, login, and refresh against the running API, then removed the temporary smoke-test user.

**Why this approach:** Persisting only token hashes reduces blast radius if the database is inspected or leaked, while token rotation closes the gap where the previous login response returned refresh tokens that could not be validated or revoked. Keeping the flow in Application handlers preserves the existing MediatR/Clean Architecture pattern; hashing/generation stays behind `IJwtTokenService` in Infrastructure.

**Alternatives considered:** Storing raw refresh tokens was rejected because it weakens secret handling. Reusing the `users` table for a single refresh token was rejected because it would make multi-device sessions and audit/revocation history harder. A full session-management feature was deferred to keep this slice focused on the API contract already documented in `API_SPEC.md`.

**Follow-ups / risks:** There is no logout/revoke endpoint yet, and there is no cleanup job for expired refresh-token rows. Add those before treating auth as production-complete.

**Reviewed by human:** [ ]

---

## [2026-08-01] Add frontend ticket status controls

**Prompt/task summary:** Continue the status workflow by wiring the frontend Tickets detail page to the backend status update endpoint.

**Files changed:**
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added a `PATCH /tickets/{id}/status` client method and a next-status action in Ticket detail. The UI shows the action only for `ITAgent` and `ITAdmin`, advances `Open -> InProgress -> Resolved -> Closed`, and synchronizes the selected detail and list item after a successful update.

**Why this approach:** A next-status button keeps the first workflow simple and prevents users from jumping to invalid states. Backend authorization and transition checks remain the source of truth; frontend role checks only keep the UI clean.

**Alternatives considered:** A free-form status dropdown was rejected for this slice because it exposes invalid choices that the backend would reject. Employee close/confirm-resolved controls remain deferred to a dedicated workflow slice.

**Follow-ups / risks:** Add assignment controls before agent-owned queues become more complex. Add frontend tests around role-specific rendering once the Tickets page has component-level test coverage.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket status workflow

**Prompt/task summary:** Implement the backend ticket status workflow slice before adding frontend status controls.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Application/Tickets/Commands/UpdateTicketStatus/*`
- `backend/src/Domain/Entities/Ticket.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketStatusCommandHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `PATCH /api/v1/tickets/{ticketId}/status` and an Application command that updates ticket status through the v1 lifecycle: `Open -> InProgress -> Resolved -> Closed`. IT admins can update any ticket; IT agents can update tickets assigned to them or unassigned queue tickets; employees cannot update status in this slice.

**Why this approach:** Status transition logic stays on the `Ticket` domain entity, while role/scope authorization stays in the Application command handler. This keeps business rules testable without HTTP or database dependencies and matches the existing Clean Architecture boundaries.

**Alternatives considered:** Letting employees close tickets immediately was deferred to a separate confirm-resolved/close-own-ticket slice because it changes the support workflow semantics. Allowing backward transitions or reopen was also deferred to keep the first lifecycle strict and predictable.

**Follow-ups / risks:** Frontend controls still need to call this endpoint. Reopen/escalation rules are future slices.

**Reviewed by human:** [ ]

---
## [2026-08-01] Link ticket detail back to chat conversation

**Prompt/task summary:** Make the Conversation field in ticket detail clickable so it opens the linked chat conversation.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The ticket detail Conversation context now renders as a router link to the Chat page with `conversationId` in the query string. The Chat page reads that query param after loading conversations and selects the matching conversation automatically.

**Why this approach:** Query params keep the existing Chat route simple while still making ticket-to-conversation navigation explicit and shareable. It avoids adding a new route shape before conversation detail routing is needed elsewhere.

**Alternatives considered:** Adding `/chat/conversations/:id` was deferred because the app currently uses the root Chat workspace route and only needs deep selection behavior for now.

**Follow-ups / risks:** If Chat later gets a dedicated conversation route, this link should move from query params to route params.

**Reviewed by human:** [ ]

---
## [2026-08-01] Fix chat ticket title and new conversation form layout

**Prompt/task summary:** Make tickets created from chat use the conversation title, and fix the New conversation form UI that expanded incorrectly in the conversation sidebar.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The chat-to-ticket handoff now sends the active conversation title as the ticket title instead of deriving it from the latest message. The conversation sidebar layout now uses flex column sizing so the New conversation form stays compact and the conversation list is the only section that grows and scrolls.

**Why this approach:** The conversation title is the user-defined issue name, so using it for the ticket title keeps chat and ticket records aligned. Flex column sizing is more robust than a fixed two-row grid when optional panels such as the creation form appear conditionally.

**Alternatives considered:** Keeping message-derived ticket titles was rejected because it can diverge from the user's explicit conversation name. Adding more grid rows was rejected because future conditional sidebar controls would recreate the same layout fragility.

**Follow-ups / risks:** Ticket details still show basic chat context only; richer context can be added when the backend exposes conversation title/message snippets directly.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add frontend tickets page and named conversation creation

**Prompt/task summary:** Continue after backend ticket list/detail APIs by adding the frontend Tickets page, and change Chat `New` conversation creation so the user can choose a conversation name.

**Files changed:**
- `frontend/src/app/app.routes.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/PROJECT_PLAN.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `/tickets` placeholder route with a real Tickets page that lists tickets, supports status/priority filters, opens detail in the same workspace, and displays title/status/priority/created metadata plus basic chat context via conversation/message IDs. The Chat page `New` action now opens a small title form before creating the conversation.

**Why this approach:** A split list/detail view matches the existing support-workspace layout and keeps repeated ticket triage efficient. For chat context, the frontend displays the IDs currently available from the backend detail contract instead of inventing another backend call in this slice.

**Alternatives considered:** A separate `/tickets/:id` page was deferred because the current app shell benefits from selecting tickets without route churn. Showing full originating message content was deferred until the backend exposes richer ticket context or a dedicated conversation summary endpoint.

**Follow-ups / risks:** Frontend status updates/comments are still future slices. Ticket detail can become richer once backend returns the originating message snippet or conversation title.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket list and detail APIs

**Prompt/task summary:** Implement the next backend slice for ticket list/detail APIs with status/priority filters and role-scoped authorization.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Api/Services/CurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ICurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ITicketRepository.cs`
- `backend/src/Application/Common/Interfaces/IUserRepository.cs`
- `backend/src/Application/Common/Models/PaginatedList.cs`
- `backend/src/Application/Common/Models/TicketListCriteria.cs`
- `backend/src/Application/Tickets/Models/TicketDetailDto.cs`
- `backend/src/Application/Tickets/Models/TicketSummaryDto.cs`
- `backend/src/Application/Tickets/Models/UserRefDto.cs`
- `backend/src/Application/Tickets/Queries/GetTicketDetail/*`
- `backend/src/Application/Tickets/Queries/GetTickets/*`
- `backend/src/Infrastructure/Persistence/Repositories/TicketRepository.cs`
- `backend/src/Infrastructure/Persistence/Repositories/UserRepository.cs`
- `backend/tests/Application.UnitTests/Auth/AuthCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Chat/ChatCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketQueryHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `GET /api/v1/tickets` and `GET /api/v1/tickets/{ticketId}`. Ticket list supports `status`, `priority`, `page`, and `pageSize`. The query handlers enforce role scope: employees see created tickets, IT agents see assigned plus unassigned queue tickets, and IT admins see all tickets. Responses include lightweight user references for creator and assignee.

**Why this approach:** Authorization stays in Application query handlers, matching the documented Clean Architecture boundary and keeping role behavior unit-testable without a database. The repository receives already-scoped criteria instead of reading HTTP claims or deciding product permissions itself.

**Alternatives considered:** Putting role checks in controllers was rejected because it would split business authorization across the API layer. Returning raw ticket entities was rejected because API DTOs keep response contracts explicit and prevent accidental field exposure.

**Follow-ups / risks:** Status update, comments, and frontend Tickets page are still separate slices. IT agent queue scope currently follows the documented v1 rule of assigned-to-me or unassigned tickets; if queues become department-based later, the criteria object can be extended.

**Reviewed by human:** [ ]

---
## [2026-08-01] Simplify chat ticket button template

**Prompt/task summary:** Check and fix the Chat page ticket button template after the user reported an error around the disabled/click bindings and conditional label.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `@if` block inside the ticket action button with a single interpolation expression that chooses between `Ticket created`, `Creating...`, and `Create ticket`.

**Why this approach:** The Angular compiler accepted the original template, but a simple interpolation is enough for this button label and is less likely to trigger editor/template tooling issues.

**Alternatives considered:** Keeping the `@if` block was valid Angular syntax, but it was more structure than this label needed.

**Follow-ups / risks:** None.

**Reviewed by human:** [ ]

---
## [2026-07-30] Migrate backend to .NET 10

**Prompt/task summary:** Change the backend from .NET 8 to .NET 10 before continuing Phase 1, and update the project documents.

**Files changed:**
- `AGENTS.md`
- `docs/PROJECT_PLAN.md`
- `docs/ai-changelog/AI_CHANGELOG.md`
- `dotnet-tools.json`
- `backend/Directory.Build.props`
- `backend/src/Api/Api.csproj`
- `backend/src/CompositionRoot/CompositionRoot.csproj`
- `backend/src/Infrastructure/Infrastructure.csproj`

**What changed:** Retargeted the backend projects from `net8.0` to `net10.0`, upgraded ASP.NET authentication, EF Core, Npgsql, Microsoft.Extensions, JWT, and local `dotnet-ef` tooling to .NET 10-compatible versions, and pinned package versions instead of leaving wildcard ranges. Updated the project context and tech-stack documentation to name .NET 10 LTS.

**Why this approach:** The user approved the framework migration before the next Phase 1 slice. Moving now avoids carrying a local `DOTNET_ROLL_FORWARD=Major` workaround and aligns the repo with the SDK/runtime installed on the development machine. EF Core was pinned to `10.0.4` to match the latest compatible Npgsql EF provider graph and remove the EF Relational version conflict seen during the first build.

**Alternatives considered:** Staying on `net8.0` plus installing the .NET 8 runtime would reduce package churn, but it keeps the project closer to the end of .NET 8 support and does not match the current machine. Leaving package versions as `10.*` was rejected because restores should be reproducible across machines.

**Follow-ups / risks:** Existing EF migration files still show their original generator `ProductVersion` metadata from EF 8; that is historical migration metadata and should update naturally on the next generated migration. Re-run API smoke tests after the next auth slice if refresh-token persistence changes runtime behavior.

**Reviewed by human:** [ ]

---

## [2026-08-01] Add frontend ticket status controls

**Prompt/task summary:** Continue the status workflow by wiring the frontend Tickets detail page to the backend status update endpoint.

**Files changed:**
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added a `PATCH /tickets/{id}/status` client method and a next-status action in Ticket detail. The UI shows the action only for `ITAgent` and `ITAdmin`, advances `Open -> InProgress -> Resolved -> Closed`, and synchronizes the selected detail and list item after a successful update.

**Why this approach:** A next-status button keeps the first workflow simple and prevents users from jumping to invalid states. Backend authorization and transition checks remain the source of truth; frontend role checks only keep the UI clean.

**Alternatives considered:** A free-form status dropdown was rejected for this slice because it exposes invalid choices that the backend would reject. Employee close/confirm-resolved controls remain deferred to a dedicated workflow slice.

**Follow-ups / risks:** Add assignment controls before agent-owned queues become more complex. Add frontend tests around role-specific rendering once the Tickets page has component-level test coverage.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket status workflow

**Prompt/task summary:** Implement the backend ticket status workflow slice before adding frontend status controls.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Application/Tickets/Commands/UpdateTicketStatus/*`
- `backend/src/Domain/Entities/Ticket.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketStatusCommandHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `PATCH /api/v1/tickets/{ticketId}/status` and an Application command that updates ticket status through the v1 lifecycle: `Open -> InProgress -> Resolved -> Closed`. IT admins can update any ticket; IT agents can update tickets assigned to them or unassigned queue tickets; employees cannot update status in this slice.

**Why this approach:** Status transition logic stays on the `Ticket` domain entity, while role/scope authorization stays in the Application command handler. This keeps business rules testable without HTTP or database dependencies and matches the existing Clean Architecture boundaries.

**Alternatives considered:** Letting employees close tickets immediately was deferred to a separate confirm-resolved/close-own-ticket slice because it changes the support workflow semantics. Allowing backward transitions or reopen was also deferred to keep the first lifecycle strict and predictable.

**Follow-ups / risks:** Frontend controls still need to call this endpoint. Reopen/escalation rules are future slices.

**Reviewed by human:** [ ]

---
## [2026-08-01] Link ticket detail back to chat conversation

**Prompt/task summary:** Make the Conversation field in ticket detail clickable so it opens the linked chat conversation.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The ticket detail Conversation context now renders as a router link to the Chat page with `conversationId` in the query string. The Chat page reads that query param after loading conversations and selects the matching conversation automatically.

**Why this approach:** Query params keep the existing Chat route simple while still making ticket-to-conversation navigation explicit and shareable. It avoids adding a new route shape before conversation detail routing is needed elsewhere.

**Alternatives considered:** Adding `/chat/conversations/:id` was deferred because the app currently uses the root Chat workspace route and only needs deep selection behavior for now.

**Follow-ups / risks:** If Chat later gets a dedicated conversation route, this link should move from query params to route params.

**Reviewed by human:** [ ]

---
## [2026-08-01] Fix chat ticket title and new conversation form layout

**Prompt/task summary:** Make tickets created from chat use the conversation title, and fix the New conversation form UI that expanded incorrectly in the conversation sidebar.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The chat-to-ticket handoff now sends the active conversation title as the ticket title instead of deriving it from the latest message. The conversation sidebar layout now uses flex column sizing so the New conversation form stays compact and the conversation list is the only section that grows and scrolls.

**Why this approach:** The conversation title is the user-defined issue name, so using it for the ticket title keeps chat and ticket records aligned. Flex column sizing is more robust than a fixed two-row grid when optional panels such as the creation form appear conditionally.

**Alternatives considered:** Keeping message-derived ticket titles was rejected because it can diverge from the user's explicit conversation name. Adding more grid rows was rejected because future conditional sidebar controls would recreate the same layout fragility.

**Follow-ups / risks:** Ticket details still show basic chat context only; richer context can be added when the backend exposes conversation title/message snippets directly.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add frontend tickets page and named conversation creation

**Prompt/task summary:** Continue after backend ticket list/detail APIs by adding the frontend Tickets page, and change Chat `New` conversation creation so the user can choose a conversation name.

**Files changed:**
- `frontend/src/app/app.routes.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/PROJECT_PLAN.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `/tickets` placeholder route with a real Tickets page that lists tickets, supports status/priority filters, opens detail in the same workspace, and displays title/status/priority/created metadata plus basic chat context via conversation/message IDs. The Chat page `New` action now opens a small title form before creating the conversation.

**Why this approach:** A split list/detail view matches the existing support-workspace layout and keeps repeated ticket triage efficient. For chat context, the frontend displays the IDs currently available from the backend detail contract instead of inventing another backend call in this slice.

**Alternatives considered:** A separate `/tickets/:id` page was deferred because the current app shell benefits from selecting tickets without route churn. Showing full originating message content was deferred until the backend exposes richer ticket context or a dedicated conversation summary endpoint.

**Follow-ups / risks:** Frontend status updates/comments are still future slices. Ticket detail can become richer once backend returns the originating message snippet or conversation title.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket list and detail APIs

**Prompt/task summary:** Implement the next backend slice for ticket list/detail APIs with status/priority filters and role-scoped authorization.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Api/Services/CurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ICurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ITicketRepository.cs`
- `backend/src/Application/Common/Interfaces/IUserRepository.cs`
- `backend/src/Application/Common/Models/PaginatedList.cs`
- `backend/src/Application/Common/Models/TicketListCriteria.cs`
- `backend/src/Application/Tickets/Models/TicketDetailDto.cs`
- `backend/src/Application/Tickets/Models/TicketSummaryDto.cs`
- `backend/src/Application/Tickets/Models/UserRefDto.cs`
- `backend/src/Application/Tickets/Queries/GetTicketDetail/*`
- `backend/src/Application/Tickets/Queries/GetTickets/*`
- `backend/src/Infrastructure/Persistence/Repositories/TicketRepository.cs`
- `backend/src/Infrastructure/Persistence/Repositories/UserRepository.cs`
- `backend/tests/Application.UnitTests/Auth/AuthCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Chat/ChatCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketQueryHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `GET /api/v1/tickets` and `GET /api/v1/tickets/{ticketId}`. Ticket list supports `status`, `priority`, `page`, and `pageSize`. The query handlers enforce role scope: employees see created tickets, IT agents see assigned plus unassigned queue tickets, and IT admins see all tickets. Responses include lightweight user references for creator and assignee.

**Why this approach:** Authorization stays in Application query handlers, matching the documented Clean Architecture boundary and keeping role behavior unit-testable without a database. The repository receives already-scoped criteria instead of reading HTTP claims or deciding product permissions itself.

**Alternatives considered:** Putting role checks in controllers was rejected because it would split business authorization across the API layer. Returning raw ticket entities was rejected because API DTOs keep response contracts explicit and prevent accidental field exposure.

**Follow-ups / risks:** Status update, comments, and frontend Tickets page are still separate slices. IT agent queue scope currently follows the documented v1 rule of assigned-to-me or unassigned tickets; if queues become department-based later, the criteria object can be extended.

**Reviewed by human:** [ ]

---
## [2026-08-01] Simplify chat ticket button template

**Prompt/task summary:** Check and fix the Chat page ticket button template after the user reported an error around the disabled/click bindings and conditional label.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `@if` block inside the ticket action button with a single interpolation expression that chooses between `Ticket created`, `Creating...`, and `Create ticket`.

**Why this approach:** The Angular compiler accepted the original template, but a simple interpolation is enough for this button label and is less likely to trigger editor/template tooling issues.

**Alternatives considered:** Keeping the `@if` block was valid Angular syntax, but it was more structure than this label needed.

**Follow-ups / risks:** None.

**Reviewed by human:** [ ]

---
## [2026-07-30] Implement backend auth foundation

**Prompt/task summary:** Start Phase 1 slice one by adding backend dependencies, register/login endpoints, and the initial `users` table.

**Files changed:**
- `dotnet-tools.json`
- `.env.example`
- `.gitignore`
- `docker-compose.yml`
- `docs/PROJECT_PLAN.md`
- `docs/ai-changelog/AI_CHANGELOG.md`
- `backend/HelpdeskTicketingSystem.slnx`
- `backend/src/Api/Api.csproj`
- `backend/src/Api/Program.cs`
- `backend/src/Api/appsettings.json`
- `backend/src/Api/Controllers/AuthController.cs`
- `backend/src/Application/Application.csproj`
- `backend/src/Application/DependencyInjection.cs`
- `backend/src/Application/Auth/**`
- `backend/src/Application/Common/Behaviors/ValidationBehavior.cs`
- `backend/src/Application/Common/Interfaces/IJwtTokenService.cs`
- `backend/src/Application/Common/Interfaces/IPasswordHasher.cs`
- `backend/src/Application/Common/Interfaces/IUnitOfWork.cs`
- `backend/src/Application/Common/Interfaces/IUserRepository.cs`
- `backend/src/Application/Common/Models/Result.cs`
- `backend/src/CompositionRoot/**`
- `backend/src/Domain/Entities/User.cs`
- `backend/src/Domain/Enums/UserRole.cs`
- `backend/src/Infrastructure/Infrastructure.csproj`
- `backend/src/Infrastructure/Identity/**`
- `backend/src/Infrastructure/Persistence/**`
- `backend/tests/Application.UnitTests/**`

**What changed:** Added the `User` domain entity and `UserRole` enum, EF Core `AppDbContext`, user repository, user configuration, and the first EF migration for the `users` table. Added MediatR-based register/login commands, validators, handlers, result models, password hashing, JWT token generation, auth API endpoints under `/api/v1/auth`, and a small `CompositionRoot` project for DI wiring. Added focused Application unit tests for register/login behavior. PostgreSQL is now published on host port `5433` to avoid the local port/auth collision seen on `5432`; Docker-internal access remains `db:5432` for pgAdmin.

**Why this approach:** The slice starts with backend auth because later chat/ticket authorization depends on a stable authenticated user model. Register/login behavior lives in Application handlers behind interfaces so it remains unit-testable without EF or HTTP. EF Core and BCrypt/JWT implementations stay in Infrastructure. A separate CompositionRoot project keeps `Api` from directly referencing `Infrastructure`, preserving the dependency boundary in `AGENTS.md` while still allowing runtime DI composition.

**Alternatives considered:** Wiring `Api` directly to `Infrastructure` would be simpler but was rejected because the repository instructions explicitly avoid a direct API-to-Infrastructure dependency. Resetting the PostgreSQL volume to fix host password failures was also rejected after the migration could be applied safely without data loss; changing the host-published port to `5433` was less destructive and made host EF tooling work.

**Follow-ups / risks:** Refresh tokens are generated in the login response but are not yet persisted or rotatable; implement refresh-token storage before enabling `POST /auth/refresh`. This local runtime mismatch was later superseded by the .NET 10 backend migration entry above. The smoke test created and then removed a temporary `smoke.*` user from the dev database.

**Reviewed by human:** [ ]

---

## [2026-08-01] Add frontend ticket status controls

**Prompt/task summary:** Continue the status workflow by wiring the frontend Tickets detail page to the backend status update endpoint.

**Files changed:**
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added a `PATCH /tickets/{id}/status` client method and a next-status action in Ticket detail. The UI shows the action only for `ITAgent` and `ITAdmin`, advances `Open -> InProgress -> Resolved -> Closed`, and synchronizes the selected detail and list item after a successful update.

**Why this approach:** A next-status button keeps the first workflow simple and prevents users from jumping to invalid states. Backend authorization and transition checks remain the source of truth; frontend role checks only keep the UI clean.

**Alternatives considered:** A free-form status dropdown was rejected for this slice because it exposes invalid choices that the backend would reject. Employee close/confirm-resolved controls remain deferred to a dedicated workflow slice.

**Follow-ups / risks:** Add assignment controls before agent-owned queues become more complex. Add frontend tests around role-specific rendering once the Tickets page has component-level test coverage.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket status workflow

**Prompt/task summary:** Implement the backend ticket status workflow slice before adding frontend status controls.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Application/Tickets/Commands/UpdateTicketStatus/*`
- `backend/src/Domain/Entities/Ticket.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketStatusCommandHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `PATCH /api/v1/tickets/{ticketId}/status` and an Application command that updates ticket status through the v1 lifecycle: `Open -> InProgress -> Resolved -> Closed`. IT admins can update any ticket; IT agents can update tickets assigned to them or unassigned queue tickets; employees cannot update status in this slice.

**Why this approach:** Status transition logic stays on the `Ticket` domain entity, while role/scope authorization stays in the Application command handler. This keeps business rules testable without HTTP or database dependencies and matches the existing Clean Architecture boundaries.

**Alternatives considered:** Letting employees close tickets immediately was deferred to a separate confirm-resolved/close-own-ticket slice because it changes the support workflow semantics. Allowing backward transitions or reopen was also deferred to keep the first lifecycle strict and predictable.

**Follow-ups / risks:** Frontend controls still need to call this endpoint. Reopen/escalation rules are future slices.

**Reviewed by human:** [ ]

---
## [2026-08-01] Link ticket detail back to chat conversation

**Prompt/task summary:** Make the Conversation field in ticket detail clickable so it opens the linked chat conversation.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The ticket detail Conversation context now renders as a router link to the Chat page with `conversationId` in the query string. The Chat page reads that query param after loading conversations and selects the matching conversation automatically.

**Why this approach:** Query params keep the existing Chat route simple while still making ticket-to-conversation navigation explicit and shareable. It avoids adding a new route shape before conversation detail routing is needed elsewhere.

**Alternatives considered:** Adding `/chat/conversations/:id` was deferred because the app currently uses the root Chat workspace route and only needs deep selection behavior for now.

**Follow-ups / risks:** If Chat later gets a dedicated conversation route, this link should move from query params to route params.

**Reviewed by human:** [ ]

---
## [2026-08-01] Fix chat ticket title and new conversation form layout

**Prompt/task summary:** Make tickets created from chat use the conversation title, and fix the New conversation form UI that expanded incorrectly in the conversation sidebar.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The chat-to-ticket handoff now sends the active conversation title as the ticket title instead of deriving it from the latest message. The conversation sidebar layout now uses flex column sizing so the New conversation form stays compact and the conversation list is the only section that grows and scrolls.

**Why this approach:** The conversation title is the user-defined issue name, so using it for the ticket title keeps chat and ticket records aligned. Flex column sizing is more robust than a fixed two-row grid when optional panels such as the creation form appear conditionally.

**Alternatives considered:** Keeping message-derived ticket titles was rejected because it can diverge from the user's explicit conversation name. Adding more grid rows was rejected because future conditional sidebar controls would recreate the same layout fragility.

**Follow-ups / risks:** Ticket details still show basic chat context only; richer context can be added when the backend exposes conversation title/message snippets directly.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add frontend tickets page and named conversation creation

**Prompt/task summary:** Continue after backend ticket list/detail APIs by adding the frontend Tickets page, and change Chat `New` conversation creation so the user can choose a conversation name.

**Files changed:**
- `frontend/src/app/app.routes.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/PROJECT_PLAN.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `/tickets` placeholder route with a real Tickets page that lists tickets, supports status/priority filters, opens detail in the same workspace, and displays title/status/priority/created metadata plus basic chat context via conversation/message IDs. The Chat page `New` action now opens a small title form before creating the conversation.

**Why this approach:** A split list/detail view matches the existing support-workspace layout and keeps repeated ticket triage efficient. For chat context, the frontend displays the IDs currently available from the backend detail contract instead of inventing another backend call in this slice.

**Alternatives considered:** A separate `/tickets/:id` page was deferred because the current app shell benefits from selecting tickets without route churn. Showing full originating message content was deferred until the backend exposes richer ticket context or a dedicated conversation summary endpoint.

**Follow-ups / risks:** Frontend status updates/comments are still future slices. Ticket detail can become richer once backend returns the originating message snippet or conversation title.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket list and detail APIs

**Prompt/task summary:** Implement the next backend slice for ticket list/detail APIs with status/priority filters and role-scoped authorization.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Api/Services/CurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ICurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ITicketRepository.cs`
- `backend/src/Application/Common/Interfaces/IUserRepository.cs`
- `backend/src/Application/Common/Models/PaginatedList.cs`
- `backend/src/Application/Common/Models/TicketListCriteria.cs`
- `backend/src/Application/Tickets/Models/TicketDetailDto.cs`
- `backend/src/Application/Tickets/Models/TicketSummaryDto.cs`
- `backend/src/Application/Tickets/Models/UserRefDto.cs`
- `backend/src/Application/Tickets/Queries/GetTicketDetail/*`
- `backend/src/Application/Tickets/Queries/GetTickets/*`
- `backend/src/Infrastructure/Persistence/Repositories/TicketRepository.cs`
- `backend/src/Infrastructure/Persistence/Repositories/UserRepository.cs`
- `backend/tests/Application.UnitTests/Auth/AuthCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Chat/ChatCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketQueryHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `GET /api/v1/tickets` and `GET /api/v1/tickets/{ticketId}`. Ticket list supports `status`, `priority`, `page`, and `pageSize`. The query handlers enforce role scope: employees see created tickets, IT agents see assigned plus unassigned queue tickets, and IT admins see all tickets. Responses include lightweight user references for creator and assignee.

**Why this approach:** Authorization stays in Application query handlers, matching the documented Clean Architecture boundary and keeping role behavior unit-testable without a database. The repository receives already-scoped criteria instead of reading HTTP claims or deciding product permissions itself.

**Alternatives considered:** Putting role checks in controllers was rejected because it would split business authorization across the API layer. Returning raw ticket entities was rejected because API DTOs keep response contracts explicit and prevent accidental field exposure.

**Follow-ups / risks:** Status update, comments, and frontend Tickets page are still separate slices. IT agent queue scope currently follows the documented v1 rule of assigned-to-me or unassigned tickets; if queues become department-based later, the criteria object can be extended.

**Reviewed by human:** [ ]

---
## [2026-08-01] Simplify chat ticket button template

**Prompt/task summary:** Check and fix the Chat page ticket button template after the user reported an error around the disabled/click bindings and conditional label.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `@if` block inside the ticket action button with a single interpolation expression that chooses between `Ticket created`, `Creating...`, and `Create ticket`.

**Why this approach:** The Angular compiler accepted the original template, but a simple interpolation is enough for this button label and is less likely to trigger editor/template tooling issues.

**Alternatives considered:** Keeping the `@if` block was valid Angular syntax, but it was more structure than this label needed.

**Follow-ups / risks:** None.

**Reviewed by human:** [ ]

---
## [2026-07-30] Fix pgAdmin default email placeholder

**Prompt/task summary:** pgAdmin failed to start because `admin@helpdesk.local` was rejected as an invalid default email, and PostgreSQL client connection testing showed password authentication failures.

**Files changed:**
- `docker-compose.yml`
- `.env.example`
- `docs/PROJECT_PLAN.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the pgAdmin default email placeholder from `admin@helpdesk.local` to `admin@helpdesk.dev`, which pgAdmin accepts. Verified Docker Compose configuration, recreated the pgAdmin container so it picked up the corrected email, and tested PostgreSQL authentication inside the `db` container with `<redacted-dev-db-credentials>`.

**Why this approach:** pgAdmin validates default email addresses on startup and rejects `.local` as a reserved/special-use domain. Changing only the placeholder keeps the Docker setup intact while preserving the intent of having development-only credentials. The PostgreSQL password was verified directly against the running container before recommending destructive volume reset, avoiding unnecessary data loss.

**Alternatives considered:** Disabling pgAdmin email validation was not chosen because using a syntactically accepted development email is simpler and closer to pgAdmin defaults. Resetting the PostgreSQL volume was considered for the password failure, but rejected after container-side authentication with `<redacted-dev-db-credentials>` succeeded.

**Follow-ups / risks:** If a desktop database client still fails authentication, clear any saved password for that connection and recreate it with host `localhost`, port `5432`, database `helpdesk`, username `helpdesk`, and password `<redacted-dev-db-password>`.

**Reviewed by human:** [ ]

---

## [2026-08-01] Add frontend ticket status controls

**Prompt/task summary:** Continue the status workflow by wiring the frontend Tickets detail page to the backend status update endpoint.

**Files changed:**
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added a `PATCH /tickets/{id}/status` client method and a next-status action in Ticket detail. The UI shows the action only for `ITAgent` and `ITAdmin`, advances `Open -> InProgress -> Resolved -> Closed`, and synchronizes the selected detail and list item after a successful update.

**Why this approach:** A next-status button keeps the first workflow simple and prevents users from jumping to invalid states. Backend authorization and transition checks remain the source of truth; frontend role checks only keep the UI clean.

**Alternatives considered:** A free-form status dropdown was rejected for this slice because it exposes invalid choices that the backend would reject. Employee close/confirm-resolved controls remain deferred to a dedicated workflow slice.

**Follow-ups / risks:** Add assignment controls before agent-owned queues become more complex. Add frontend tests around role-specific rendering once the Tickets page has component-level test coverage.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket status workflow

**Prompt/task summary:** Implement the backend ticket status workflow slice before adding frontend status controls.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Application/Tickets/Commands/UpdateTicketStatus/*`
- `backend/src/Domain/Entities/Ticket.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketStatusCommandHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `PATCH /api/v1/tickets/{ticketId}/status` and an Application command that updates ticket status through the v1 lifecycle: `Open -> InProgress -> Resolved -> Closed`. IT admins can update any ticket; IT agents can update tickets assigned to them or unassigned queue tickets; employees cannot update status in this slice.

**Why this approach:** Status transition logic stays on the `Ticket` domain entity, while role/scope authorization stays in the Application command handler. This keeps business rules testable without HTTP or database dependencies and matches the existing Clean Architecture boundaries.

**Alternatives considered:** Letting employees close tickets immediately was deferred to a separate confirm-resolved/close-own-ticket slice because it changes the support workflow semantics. Allowing backward transitions or reopen was also deferred to keep the first lifecycle strict and predictable.

**Follow-ups / risks:** Frontend controls still need to call this endpoint. Reopen/escalation rules are future slices.

**Reviewed by human:** [ ]

---
## [2026-08-01] Link ticket detail back to chat conversation

**Prompt/task summary:** Make the Conversation field in ticket detail clickable so it opens the linked chat conversation.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The ticket detail Conversation context now renders as a router link to the Chat page with `conversationId` in the query string. The Chat page reads that query param after loading conversations and selects the matching conversation automatically.

**Why this approach:** Query params keep the existing Chat route simple while still making ticket-to-conversation navigation explicit and shareable. It avoids adding a new route shape before conversation detail routing is needed elsewhere.

**Alternatives considered:** Adding `/chat/conversations/:id` was deferred because the app currently uses the root Chat workspace route and only needs deep selection behavior for now.

**Follow-ups / risks:** If Chat later gets a dedicated conversation route, this link should move from query params to route params.

**Reviewed by human:** [ ]

---
## [2026-08-01] Fix chat ticket title and new conversation form layout

**Prompt/task summary:** Make tickets created from chat use the conversation title, and fix the New conversation form UI that expanded incorrectly in the conversation sidebar.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The chat-to-ticket handoff now sends the active conversation title as the ticket title instead of deriving it from the latest message. The conversation sidebar layout now uses flex column sizing so the New conversation form stays compact and the conversation list is the only section that grows and scrolls.

**Why this approach:** The conversation title is the user-defined issue name, so using it for the ticket title keeps chat and ticket records aligned. Flex column sizing is more robust than a fixed two-row grid when optional panels such as the creation form appear conditionally.

**Alternatives considered:** Keeping message-derived ticket titles was rejected because it can diverge from the user's explicit conversation name. Adding more grid rows was rejected because future conditional sidebar controls would recreate the same layout fragility.

**Follow-ups / risks:** Ticket details still show basic chat context only; richer context can be added when the backend exposes conversation title/message snippets directly.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add frontend tickets page and named conversation creation

**Prompt/task summary:** Continue after backend ticket list/detail APIs by adding the frontend Tickets page, and change Chat `New` conversation creation so the user can choose a conversation name.

**Files changed:**
- `frontend/src/app/app.routes.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/PROJECT_PLAN.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `/tickets` placeholder route with a real Tickets page that lists tickets, supports status/priority filters, opens detail in the same workspace, and displays title/status/priority/created metadata plus basic chat context via conversation/message IDs. The Chat page `New` action now opens a small title form before creating the conversation.

**Why this approach:** A split list/detail view matches the existing support-workspace layout and keeps repeated ticket triage efficient. For chat context, the frontend displays the IDs currently available from the backend detail contract instead of inventing another backend call in this slice.

**Alternatives considered:** A separate `/tickets/:id` page was deferred because the current app shell benefits from selecting tickets without route churn. Showing full originating message content was deferred until the backend exposes richer ticket context or a dedicated conversation summary endpoint.

**Follow-ups / risks:** Frontend status updates/comments are still future slices. Ticket detail can become richer once backend returns the originating message snippet or conversation title.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket list and detail APIs

**Prompt/task summary:** Implement the next backend slice for ticket list/detail APIs with status/priority filters and role-scoped authorization.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Api/Services/CurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ICurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ITicketRepository.cs`
- `backend/src/Application/Common/Interfaces/IUserRepository.cs`
- `backend/src/Application/Common/Models/PaginatedList.cs`
- `backend/src/Application/Common/Models/TicketListCriteria.cs`
- `backend/src/Application/Tickets/Models/TicketDetailDto.cs`
- `backend/src/Application/Tickets/Models/TicketSummaryDto.cs`
- `backend/src/Application/Tickets/Models/UserRefDto.cs`
- `backend/src/Application/Tickets/Queries/GetTicketDetail/*`
- `backend/src/Application/Tickets/Queries/GetTickets/*`
- `backend/src/Infrastructure/Persistence/Repositories/TicketRepository.cs`
- `backend/src/Infrastructure/Persistence/Repositories/UserRepository.cs`
- `backend/tests/Application.UnitTests/Auth/AuthCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Chat/ChatCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketQueryHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `GET /api/v1/tickets` and `GET /api/v1/tickets/{ticketId}`. Ticket list supports `status`, `priority`, `page`, and `pageSize`. The query handlers enforce role scope: employees see created tickets, IT agents see assigned plus unassigned queue tickets, and IT admins see all tickets. Responses include lightweight user references for creator and assignee.

**Why this approach:** Authorization stays in Application query handlers, matching the documented Clean Architecture boundary and keeping role behavior unit-testable without a database. The repository receives already-scoped criteria instead of reading HTTP claims or deciding product permissions itself.

**Alternatives considered:** Putting role checks in controllers was rejected because it would split business authorization across the API layer. Returning raw ticket entities was rejected because API DTOs keep response contracts explicit and prevent accidental field exposure.

**Follow-ups / risks:** Status update, comments, and frontend Tickets page are still separate slices. IT agent queue scope currently follows the documented v1 rule of assigned-to-me or unassigned tickets; if queues become department-based later, the criteria object can be extended.

**Reviewed by human:** [ ]

---
## [2026-08-01] Simplify chat ticket button template

**Prompt/task summary:** Check and fix the Chat page ticket button template after the user reported an error around the disabled/click bindings and conditional label.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `@if` block inside the ticket action button with a single interpolation expression that chooses between `Ticket created`, `Creating...`, and `Create ticket`.

**Why this approach:** The Angular compiler accepted the original template, but a simple interpolation is enough for this button label and is less likely to trigger editor/template tooling issues.

**Alternatives considered:** Keeping the `@if` block was valid Angular syntax, but it was more structure than this label needed.

**Follow-ups / risks:** None.

**Reviewed by human:** [ ]

---
## [2026-07-30] Add pgAdmin for local database inspection

**Prompt/task summary:** Add pgAdmin4 to Docker Compose so PostgreSQL data can be inspected through a browser UI instead of using CLI commands.

**Files changed:**
- `docker-compose.yml`
- `.env.example`
- `docs/PROJECT_PLAN.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added a `pgadmin` service using the official `dpage/pgadmin4` image, exposed it on `http://localhost:5050`, persisted its state in a `pgadmin-data` volume, and configured it to wait for the `db` service health check. Added pgAdmin development credentials to `.env.example` and documented how to register the Docker-networked PostgreSQL server in pgAdmin.

**Why this approach:** pgAdmin directly addresses the need to inspect tables and records without dropping into PostgreSQL CLI tooling. Keeping it in Docker Compose makes local setup repeatable and keeps it isolated from production secrets. The service uses host `db` for database registration because Docker Compose service names resolve on the internal network, which is more reliable than connecting from pgAdmin to `localhost` inside its own container.

**Alternatives considered:** Installing pgAdmin on the host machine was rejected because it adds manual machine-specific setup outside the project. Adminer was considered because it is lighter, but pgAdmin is the more familiar PostgreSQL-focused UI and better matches the user's request for pgAdmin4 specifically.

**Follow-ups / risks:** The default pgAdmin and database passwords are development placeholders only; replace them in a real `.env` before exposing the compose stack beyond local development. The API/web/nginx services are still documented as planned compose services but not yet implemented in `docker-compose.yml`.

**Reviewed by human:** [ ]

---

## [2026-08-01] Add frontend ticket status controls

**Prompt/task summary:** Continue the status workflow by wiring the frontend Tickets detail page to the backend status update endpoint.

**Files changed:**
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added a `PATCH /tickets/{id}/status` client method and a next-status action in Ticket detail. The UI shows the action only for `ITAgent` and `ITAdmin`, advances `Open -> InProgress -> Resolved -> Closed`, and synchronizes the selected detail and list item after a successful update.

**Why this approach:** A next-status button keeps the first workflow simple and prevents users from jumping to invalid states. Backend authorization and transition checks remain the source of truth; frontend role checks only keep the UI clean.

**Alternatives considered:** A free-form status dropdown was rejected for this slice because it exposes invalid choices that the backend would reject. Employee close/confirm-resolved controls remain deferred to a dedicated workflow slice.

**Follow-ups / risks:** Add assignment controls before agent-owned queues become more complex. Add frontend tests around role-specific rendering once the Tickets page has component-level test coverage.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket status workflow

**Prompt/task summary:** Implement the backend ticket status workflow slice before adding frontend status controls.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Application/Tickets/Commands/UpdateTicketStatus/*`
- `backend/src/Domain/Entities/Ticket.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketStatusCommandHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `PATCH /api/v1/tickets/{ticketId}/status` and an Application command that updates ticket status through the v1 lifecycle: `Open -> InProgress -> Resolved -> Closed`. IT admins can update any ticket; IT agents can update tickets assigned to them or unassigned queue tickets; employees cannot update status in this slice.

**Why this approach:** Status transition logic stays on the `Ticket` domain entity, while role/scope authorization stays in the Application command handler. This keeps business rules testable without HTTP or database dependencies and matches the existing Clean Architecture boundaries.

**Alternatives considered:** Letting employees close tickets immediately was deferred to a separate confirm-resolved/close-own-ticket slice because it changes the support workflow semantics. Allowing backward transitions or reopen was also deferred to keep the first lifecycle strict and predictable.

**Follow-ups / risks:** Frontend controls still need to call this endpoint. Reopen/escalation rules are future slices.

**Reviewed by human:** [ ]

---
## [2026-08-01] Link ticket detail back to chat conversation

**Prompt/task summary:** Make the Conversation field in ticket detail clickable so it opens the linked chat conversation.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The ticket detail Conversation context now renders as a router link to the Chat page with `conversationId` in the query string. The Chat page reads that query param after loading conversations and selects the matching conversation automatically.

**Why this approach:** Query params keep the existing Chat route simple while still making ticket-to-conversation navigation explicit and shareable. It avoids adding a new route shape before conversation detail routing is needed elsewhere.

**Alternatives considered:** Adding `/chat/conversations/:id` was deferred because the app currently uses the root Chat workspace route and only needs deep selection behavior for now.

**Follow-ups / risks:** If Chat later gets a dedicated conversation route, this link should move from query params to route params.

**Reviewed by human:** [ ]

---
## [2026-08-01] Fix chat ticket title and new conversation form layout

**Prompt/task summary:** Make tickets created from chat use the conversation title, and fix the New conversation form UI that expanded incorrectly in the conversation sidebar.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The chat-to-ticket handoff now sends the active conversation title as the ticket title instead of deriving it from the latest message. The conversation sidebar layout now uses flex column sizing so the New conversation form stays compact and the conversation list is the only section that grows and scrolls.

**Why this approach:** The conversation title is the user-defined issue name, so using it for the ticket title keeps chat and ticket records aligned. Flex column sizing is more robust than a fixed two-row grid when optional panels such as the creation form appear conditionally.

**Alternatives considered:** Keeping message-derived ticket titles was rejected because it can diverge from the user's explicit conversation name. Adding more grid rows was rejected because future conditional sidebar controls would recreate the same layout fragility.

**Follow-ups / risks:** Ticket details still show basic chat context only; richer context can be added when the backend exposes conversation title/message snippets directly.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add frontend tickets page and named conversation creation

**Prompt/task summary:** Continue after backend ticket list/detail APIs by adding the frontend Tickets page, and change Chat `New` conversation creation so the user can choose a conversation name.

**Files changed:**
- `frontend/src/app/app.routes.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/PROJECT_PLAN.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `/tickets` placeholder route with a real Tickets page that lists tickets, supports status/priority filters, opens detail in the same workspace, and displays title/status/priority/created metadata plus basic chat context via conversation/message IDs. The Chat page `New` action now opens a small title form before creating the conversation.

**Why this approach:** A split list/detail view matches the existing support-workspace layout and keeps repeated ticket triage efficient. For chat context, the frontend displays the IDs currently available from the backend detail contract instead of inventing another backend call in this slice.

**Alternatives considered:** A separate `/tickets/:id` page was deferred because the current app shell benefits from selecting tickets without route churn. Showing full originating message content was deferred until the backend exposes richer ticket context or a dedicated conversation summary endpoint.

**Follow-ups / risks:** Frontend status updates/comments are still future slices. Ticket detail can become richer once backend returns the originating message snippet or conversation title.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket list and detail APIs

**Prompt/task summary:** Implement the next backend slice for ticket list/detail APIs with status/priority filters and role-scoped authorization.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Api/Services/CurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ICurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ITicketRepository.cs`
- `backend/src/Application/Common/Interfaces/IUserRepository.cs`
- `backend/src/Application/Common/Models/PaginatedList.cs`
- `backend/src/Application/Common/Models/TicketListCriteria.cs`
- `backend/src/Application/Tickets/Models/TicketDetailDto.cs`
- `backend/src/Application/Tickets/Models/TicketSummaryDto.cs`
- `backend/src/Application/Tickets/Models/UserRefDto.cs`
- `backend/src/Application/Tickets/Queries/GetTicketDetail/*`
- `backend/src/Application/Tickets/Queries/GetTickets/*`
- `backend/src/Infrastructure/Persistence/Repositories/TicketRepository.cs`
- `backend/src/Infrastructure/Persistence/Repositories/UserRepository.cs`
- `backend/tests/Application.UnitTests/Auth/AuthCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Chat/ChatCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketQueryHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `GET /api/v1/tickets` and `GET /api/v1/tickets/{ticketId}`. Ticket list supports `status`, `priority`, `page`, and `pageSize`. The query handlers enforce role scope: employees see created tickets, IT agents see assigned plus unassigned queue tickets, and IT admins see all tickets. Responses include lightweight user references for creator and assignee.

**Why this approach:** Authorization stays in Application query handlers, matching the documented Clean Architecture boundary and keeping role behavior unit-testable without a database. The repository receives already-scoped criteria instead of reading HTTP claims or deciding product permissions itself.

**Alternatives considered:** Putting role checks in controllers was rejected because it would split business authorization across the API layer. Returning raw ticket entities was rejected because API DTOs keep response contracts explicit and prevent accidental field exposure.

**Follow-ups / risks:** Status update, comments, and frontend Tickets page are still separate slices. IT agent queue scope currently follows the documented v1 rule of assigned-to-me or unassigned tickets; if queues become department-based later, the criteria object can be extended.

**Reviewed by human:** [ ]

---
## [2026-08-01] Simplify chat ticket button template

**Prompt/task summary:** Check and fix the Chat page ticket button template after the user reported an error around the disabled/click bindings and conditional label.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `@if` block inside the ticket action button with a single interpolation expression that chooses between `Ticket created`, `Creating...`, and `Create ticket`.

**Why this approach:** The Angular compiler accepted the original template, but a simple interpolation is enough for this button label and is less likely to trigger editor/template tooling issues.

**Alternatives considered:** Keeping the `@if` block was valid Angular syntax, but it was more structure than this label needed.

**Follow-ups / risks:** None.

**Reviewed by human:** [ ]

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

**Reviewed by human:** [ ]

---

## [2026-08-01] Add frontend ticket status controls

**Prompt/task summary:** Continue the status workflow by wiring the frontend Tickets detail page to the backend status update endpoint.

**Files changed:**
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added a `PATCH /tickets/{id}/status` client method and a next-status action in Ticket detail. The UI shows the action only for `ITAgent` and `ITAdmin`, advances `Open -> InProgress -> Resolved -> Closed`, and synchronizes the selected detail and list item after a successful update.

**Why this approach:** A next-status button keeps the first workflow simple and prevents users from jumping to invalid states. Backend authorization and transition checks remain the source of truth; frontend role checks only keep the UI clean.

**Alternatives considered:** A free-form status dropdown was rejected for this slice because it exposes invalid choices that the backend would reject. Employee close/confirm-resolved controls remain deferred to a dedicated workflow slice.

**Follow-ups / risks:** Add assignment controls before agent-owned queues become more complex. Add frontend tests around role-specific rendering once the Tickets page has component-level test coverage.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket status workflow

**Prompt/task summary:** Implement the backend ticket status workflow slice before adding frontend status controls.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Application/Tickets/Commands/UpdateTicketStatus/*`
- `backend/src/Domain/Entities/Ticket.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketStatusCommandHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `PATCH /api/v1/tickets/{ticketId}/status` and an Application command that updates ticket status through the v1 lifecycle: `Open -> InProgress -> Resolved -> Closed`. IT admins can update any ticket; IT agents can update tickets assigned to them or unassigned queue tickets; employees cannot update status in this slice.

**Why this approach:** Status transition logic stays on the `Ticket` domain entity, while role/scope authorization stays in the Application command handler. This keeps business rules testable without HTTP or database dependencies and matches the existing Clean Architecture boundaries.

**Alternatives considered:** Letting employees close tickets immediately was deferred to a separate confirm-resolved/close-own-ticket slice because it changes the support workflow semantics. Allowing backward transitions or reopen was also deferred to keep the first lifecycle strict and predictable.

**Follow-ups / risks:** Frontend controls still need to call this endpoint. Reopen/escalation rules are future slices.

**Reviewed by human:** [ ]

---
## [2026-08-01] Link ticket detail back to chat conversation

**Prompt/task summary:** Make the Conversation field in ticket detail clickable so it opens the linked chat conversation.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The ticket detail Conversation context now renders as a router link to the Chat page with `conversationId` in the query string. The Chat page reads that query param after loading conversations and selects the matching conversation automatically.

**Why this approach:** Query params keep the existing Chat route simple while still making ticket-to-conversation navigation explicit and shareable. It avoids adding a new route shape before conversation detail routing is needed elsewhere.

**Alternatives considered:** Adding `/chat/conversations/:id` was deferred because the app currently uses the root Chat workspace route and only needs deep selection behavior for now.

**Follow-ups / risks:** If Chat later gets a dedicated conversation route, this link should move from query params to route params.

**Reviewed by human:** [ ]

---
## [2026-08-01] Fix chat ticket title and new conversation form layout

**Prompt/task summary:** Make tickets created from chat use the conversation title, and fix the New conversation form UI that expanded incorrectly in the conversation sidebar.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The chat-to-ticket handoff now sends the active conversation title as the ticket title instead of deriving it from the latest message. The conversation sidebar layout now uses flex column sizing so the New conversation form stays compact and the conversation list is the only section that grows and scrolls.

**Why this approach:** The conversation title is the user-defined issue name, so using it for the ticket title keeps chat and ticket records aligned. Flex column sizing is more robust than a fixed two-row grid when optional panels such as the creation form appear conditionally.

**Alternatives considered:** Keeping message-derived ticket titles was rejected because it can diverge from the user's explicit conversation name. Adding more grid rows was rejected because future conditional sidebar controls would recreate the same layout fragility.

**Follow-ups / risks:** Ticket details still show basic chat context only; richer context can be added when the backend exposes conversation title/message snippets directly.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add frontend tickets page and named conversation creation

**Prompt/task summary:** Continue after backend ticket list/detail APIs by adding the frontend Tickets page, and change Chat `New` conversation creation so the user can choose a conversation name.

**Files changed:**
- `frontend/src/app/app.routes.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/PROJECT_PLAN.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `/tickets` placeholder route with a real Tickets page that lists tickets, supports status/priority filters, opens detail in the same workspace, and displays title/status/priority/created metadata plus basic chat context via conversation/message IDs. The Chat page `New` action now opens a small title form before creating the conversation.

**Why this approach:** A split list/detail view matches the existing support-workspace layout and keeps repeated ticket triage efficient. For chat context, the frontend displays the IDs currently available from the backend detail contract instead of inventing another backend call in this slice.

**Alternatives considered:** A separate `/tickets/:id` page was deferred because the current app shell benefits from selecting tickets without route churn. Showing full originating message content was deferred until the backend exposes richer ticket context or a dedicated conversation summary endpoint.

**Follow-ups / risks:** Frontend status updates/comments are still future slices. Ticket detail can become richer once backend returns the originating message snippet or conversation title.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket list and detail APIs

**Prompt/task summary:** Implement the next backend slice for ticket list/detail APIs with status/priority filters and role-scoped authorization.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Api/Services/CurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ICurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ITicketRepository.cs`
- `backend/src/Application/Common/Interfaces/IUserRepository.cs`
- `backend/src/Application/Common/Models/PaginatedList.cs`
- `backend/src/Application/Common/Models/TicketListCriteria.cs`
- `backend/src/Application/Tickets/Models/TicketDetailDto.cs`
- `backend/src/Application/Tickets/Models/TicketSummaryDto.cs`
- `backend/src/Application/Tickets/Models/UserRefDto.cs`
- `backend/src/Application/Tickets/Queries/GetTicketDetail/*`
- `backend/src/Application/Tickets/Queries/GetTickets/*`
- `backend/src/Infrastructure/Persistence/Repositories/TicketRepository.cs`
- `backend/src/Infrastructure/Persistence/Repositories/UserRepository.cs`
- `backend/tests/Application.UnitTests/Auth/AuthCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Chat/ChatCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketQueryHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `GET /api/v1/tickets` and `GET /api/v1/tickets/{ticketId}`. Ticket list supports `status`, `priority`, `page`, and `pageSize`. The query handlers enforce role scope: employees see created tickets, IT agents see assigned plus unassigned queue tickets, and IT admins see all tickets. Responses include lightweight user references for creator and assignee.

**Why this approach:** Authorization stays in Application query handlers, matching the documented Clean Architecture boundary and keeping role behavior unit-testable without a database. The repository receives already-scoped criteria instead of reading HTTP claims or deciding product permissions itself.

**Alternatives considered:** Putting role checks in controllers was rejected because it would split business authorization across the API layer. Returning raw ticket entities was rejected because API DTOs keep response contracts explicit and prevent accidental field exposure.

**Follow-ups / risks:** Status update, comments, and frontend Tickets page are still separate slices. IT agent queue scope currently follows the documented v1 rule of assigned-to-me or unassigned tickets; if queues become department-based later, the criteria object can be extended.

**Reviewed by human:** [ ]

---
## [2026-08-01] Simplify chat ticket button template

**Prompt/task summary:** Check and fix the Chat page ticket button template after the user reported an error around the disabled/click bindings and conditional label.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `@if` block inside the ticket action button with a single interpolation expression that chooses between `Ticket created`, `Creating...`, and `Create ticket`.

**Why this approach:** The Angular compiler accepted the original template, but a simple interpolation is enough for this button label and is less likely to trigger editor/template tooling issues.

**Alternatives considered:** Keeping the `@if` block was valid Angular syntax, but it was more structure than this label needed.

**Follow-ups / risks:** None.

**Reviewed by human:** [ ]

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

**Reviewed by human:** [ ]

---

## [2026-08-01] Add frontend ticket status controls

**Prompt/task summary:** Continue the status workflow by wiring the frontend Tickets detail page to the backend status update endpoint.

**Files changed:**
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added a `PATCH /tickets/{id}/status` client method and a next-status action in Ticket detail. The UI shows the action only for `ITAgent` and `ITAdmin`, advances `Open -> InProgress -> Resolved -> Closed`, and synchronizes the selected detail and list item after a successful update.

**Why this approach:** A next-status button keeps the first workflow simple and prevents users from jumping to invalid states. Backend authorization and transition checks remain the source of truth; frontend role checks only keep the UI clean.

**Alternatives considered:** A free-form status dropdown was rejected for this slice because it exposes invalid choices that the backend would reject. Employee close/confirm-resolved controls remain deferred to a dedicated workflow slice.

**Follow-ups / risks:** Add assignment controls before agent-owned queues become more complex. Add frontend tests around role-specific rendering once the Tickets page has component-level test coverage.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket status workflow

**Prompt/task summary:** Implement the backend ticket status workflow slice before adding frontend status controls.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Application/Tickets/Commands/UpdateTicketStatus/*`
- `backend/src/Domain/Entities/Ticket.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketStatusCommandHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `PATCH /api/v1/tickets/{ticketId}/status` and an Application command that updates ticket status through the v1 lifecycle: `Open -> InProgress -> Resolved -> Closed`. IT admins can update any ticket; IT agents can update tickets assigned to them or unassigned queue tickets; employees cannot update status in this slice.

**Why this approach:** Status transition logic stays on the `Ticket` domain entity, while role/scope authorization stays in the Application command handler. This keeps business rules testable without HTTP or database dependencies and matches the existing Clean Architecture boundaries.

**Alternatives considered:** Letting employees close tickets immediately was deferred to a separate confirm-resolved/close-own-ticket slice because it changes the support workflow semantics. Allowing backward transitions or reopen was also deferred to keep the first lifecycle strict and predictable.

**Follow-ups / risks:** Frontend controls still need to call this endpoint. Reopen/escalation rules are future slices.

**Reviewed by human:** [ ]

---
## [2026-08-01] Link ticket detail back to chat conversation

**Prompt/task summary:** Make the Conversation field in ticket detail clickable so it opens the linked chat conversation.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The ticket detail Conversation context now renders as a router link to the Chat page with `conversationId` in the query string. The Chat page reads that query param after loading conversations and selects the matching conversation automatically.

**Why this approach:** Query params keep the existing Chat route simple while still making ticket-to-conversation navigation explicit and shareable. It avoids adding a new route shape before conversation detail routing is needed elsewhere.

**Alternatives considered:** Adding `/chat/conversations/:id` was deferred because the app currently uses the root Chat workspace route and only needs deep selection behavior for now.

**Follow-ups / risks:** If Chat later gets a dedicated conversation route, this link should move from query params to route params.

**Reviewed by human:** [ ]

---
## [2026-08-01] Fix chat ticket title and new conversation form layout

**Prompt/task summary:** Make tickets created from chat use the conversation title, and fix the New conversation form UI that expanded incorrectly in the conversation sidebar.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The chat-to-ticket handoff now sends the active conversation title as the ticket title instead of deriving it from the latest message. The conversation sidebar layout now uses flex column sizing so the New conversation form stays compact and the conversation list is the only section that grows and scrolls.

**Why this approach:** The conversation title is the user-defined issue name, so using it for the ticket title keeps chat and ticket records aligned. Flex column sizing is more robust than a fixed two-row grid when optional panels such as the creation form appear conditionally.

**Alternatives considered:** Keeping message-derived ticket titles was rejected because it can diverge from the user's explicit conversation name. Adding more grid rows was rejected because future conditional sidebar controls would recreate the same layout fragility.

**Follow-ups / risks:** Ticket details still show basic chat context only; richer context can be added when the backend exposes conversation title/message snippets directly.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add frontend tickets page and named conversation creation

**Prompt/task summary:** Continue after backend ticket list/detail APIs by adding the frontend Tickets page, and change Chat `New` conversation creation so the user can choose a conversation name.

**Files changed:**
- `frontend/src/app/app.routes.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/PROJECT_PLAN.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `/tickets` placeholder route with a real Tickets page that lists tickets, supports status/priority filters, opens detail in the same workspace, and displays title/status/priority/created metadata plus basic chat context via conversation/message IDs. The Chat page `New` action now opens a small title form before creating the conversation.

**Why this approach:** A split list/detail view matches the existing support-workspace layout and keeps repeated ticket triage efficient. For chat context, the frontend displays the IDs currently available from the backend detail contract instead of inventing another backend call in this slice.

**Alternatives considered:** A separate `/tickets/:id` page was deferred because the current app shell benefits from selecting tickets without route churn. Showing full originating message content was deferred until the backend exposes richer ticket context or a dedicated conversation summary endpoint.

**Follow-ups / risks:** Frontend status updates/comments are still future slices. Ticket detail can become richer once backend returns the originating message snippet or conversation title.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket list and detail APIs

**Prompt/task summary:** Implement the next backend slice for ticket list/detail APIs with status/priority filters and role-scoped authorization.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Api/Services/CurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ICurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ITicketRepository.cs`
- `backend/src/Application/Common/Interfaces/IUserRepository.cs`
- `backend/src/Application/Common/Models/PaginatedList.cs`
- `backend/src/Application/Common/Models/TicketListCriteria.cs`
- `backend/src/Application/Tickets/Models/TicketDetailDto.cs`
- `backend/src/Application/Tickets/Models/TicketSummaryDto.cs`
- `backend/src/Application/Tickets/Models/UserRefDto.cs`
- `backend/src/Application/Tickets/Queries/GetTicketDetail/*`
- `backend/src/Application/Tickets/Queries/GetTickets/*`
- `backend/src/Infrastructure/Persistence/Repositories/TicketRepository.cs`
- `backend/src/Infrastructure/Persistence/Repositories/UserRepository.cs`
- `backend/tests/Application.UnitTests/Auth/AuthCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Chat/ChatCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketQueryHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `GET /api/v1/tickets` and `GET /api/v1/tickets/{ticketId}`. Ticket list supports `status`, `priority`, `page`, and `pageSize`. The query handlers enforce role scope: employees see created tickets, IT agents see assigned plus unassigned queue tickets, and IT admins see all tickets. Responses include lightweight user references for creator and assignee.

**Why this approach:** Authorization stays in Application query handlers, matching the documented Clean Architecture boundary and keeping role behavior unit-testable without a database. The repository receives already-scoped criteria instead of reading HTTP claims or deciding product permissions itself.

**Alternatives considered:** Putting role checks in controllers was rejected because it would split business authorization across the API layer. Returning raw ticket entities was rejected because API DTOs keep response contracts explicit and prevent accidental field exposure.

**Follow-ups / risks:** Status update, comments, and frontend Tickets page are still separate slices. IT agent queue scope currently follows the documented v1 rule of assigned-to-me or unassigned tickets; if queues become department-based later, the criteria object can be extended.

**Reviewed by human:** [ ]

---
## [2026-08-01] Simplify chat ticket button template

**Prompt/task summary:** Check and fix the Chat page ticket button template after the user reported an error around the disabled/click bindings and conditional label.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `@if` block inside the ticket action button with a single interpolation expression that chooses between `Ticket created`, `Creating...`, and `Create ticket`.

**Why this approach:** The Angular compiler accepted the original template, but a simple interpolation is enough for this button label and is less likely to trigger editor/template tooling issues.

**Alternatives considered:** Keeping the `@if` block was valid Angular syntax, but it was more structure than this label needed.

**Follow-ups / risks:** None.

**Reviewed by human:** [ ]

---
## [YYYY-MM-DD] <short task title>

**Prompt/task summary:** what was asked

**Files changed:**
- `path/to/file1`
- `path/to/file2`

**What changed:** plain description of the change

**Why this approach:** the reasoning behind it - trade-offs considered, why this option
over alternatives, any constraints from `docs/PROJECT_PLAN.md` that drove the decision

**Alternatives considered:** (if any) briefly, and why they were rejected

**Follow-ups / risks:** anything a human should double-check

**Reviewed by human:** [ ]

---

## [2026-08-01] Add frontend ticket status controls

**Prompt/task summary:** Continue the status workflow by wiring the frontend Tickets detail page to the backend status update endpoint.

**Files changed:**
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added a `PATCH /tickets/{id}/status` client method and a next-status action in Ticket detail. The UI shows the action only for `ITAgent` and `ITAdmin`, advances `Open -> InProgress -> Resolved -> Closed`, and synchronizes the selected detail and list item after a successful update.

**Why this approach:** A next-status button keeps the first workflow simple and prevents users from jumping to invalid states. Backend authorization and transition checks remain the source of truth; frontend role checks only keep the UI clean.

**Alternatives considered:** A free-form status dropdown was rejected for this slice because it exposes invalid choices that the backend would reject. Employee close/confirm-resolved controls remain deferred to a dedicated workflow slice.

**Follow-ups / risks:** Add assignment controls before agent-owned queues become more complex. Add frontend tests around role-specific rendering once the Tickets page has component-level test coverage.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket status workflow

**Prompt/task summary:** Implement the backend ticket status workflow slice before adding frontend status controls.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Application/Tickets/Commands/UpdateTicketStatus/*`
- `backend/src/Domain/Entities/Ticket.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketStatusCommandHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `PATCH /api/v1/tickets/{ticketId}/status` and an Application command that updates ticket status through the v1 lifecycle: `Open -> InProgress -> Resolved -> Closed`. IT admins can update any ticket; IT agents can update tickets assigned to them or unassigned queue tickets; employees cannot update status in this slice.

**Why this approach:** Status transition logic stays on the `Ticket` domain entity, while role/scope authorization stays in the Application command handler. This keeps business rules testable without HTTP or database dependencies and matches the existing Clean Architecture boundaries.

**Alternatives considered:** Letting employees close tickets immediately was deferred to a separate confirm-resolved/close-own-ticket slice because it changes the support workflow semantics. Allowing backward transitions or reopen was also deferred to keep the first lifecycle strict and predictable.

**Follow-ups / risks:** Frontend controls still need to call this endpoint. Reopen/escalation rules are future slices.

**Reviewed by human:** [ ]

---
## [2026-08-01] Link ticket detail back to chat conversation

**Prompt/task summary:** Make the Conversation field in ticket detail clickable so it opens the linked chat conversation.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The ticket detail Conversation context now renders as a router link to the Chat page with `conversationId` in the query string. The Chat page reads that query param after loading conversations and selects the matching conversation automatically.

**Why this approach:** Query params keep the existing Chat route simple while still making ticket-to-conversation navigation explicit and shareable. It avoids adding a new route shape before conversation detail routing is needed elsewhere.

**Alternatives considered:** Adding `/chat/conversations/:id` was deferred because the app currently uses the root Chat workspace route and only needs deep selection behavior for now.

**Follow-ups / risks:** If Chat later gets a dedicated conversation route, this link should move from query params to route params.

**Reviewed by human:** [ ]

---
## [2026-08-01] Fix chat ticket title and new conversation form layout

**Prompt/task summary:** Make tickets created from chat use the conversation title, and fix the New conversation form UI that expanded incorrectly in the conversation sidebar.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** The chat-to-ticket handoff now sends the active conversation title as the ticket title instead of deriving it from the latest message. The conversation sidebar layout now uses flex column sizing so the New conversation form stays compact and the conversation list is the only section that grows and scrolls.

**Why this approach:** The conversation title is the user-defined issue name, so using it for the ticket title keeps chat and ticket records aligned. Flex column sizing is more robust than a fixed two-row grid when optional panels such as the creation form appear conditionally.

**Alternatives considered:** Keeping message-derived ticket titles was rejected because it can diverge from the user's explicit conversation name. Adding more grid rows was rejected because future conditional sidebar controls would recreate the same layout fragility.

**Follow-ups / risks:** Ticket details still show basic chat context only; richer context can be added when the backend exposes conversation title/message snippets directly.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add frontend tickets page and named conversation creation

**Prompt/task summary:** Continue after backend ticket list/detail APIs by adding the frontend Tickets page, and change Chat `New` conversation creation so the user can choose a conversation name.

**Files changed:**
- `frontend/src/app/app.routes.ts`
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `frontend/src/app/features/chat/feature/chat-page.component.scss`
- `frontend/src/app/features/chat/feature/chat-page.component.ts`
- `frontend/src/app/features/tickets/data-access/ticket.models.ts`
- `frontend/src/app/features/tickets/data-access/ticket.service.ts`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.html`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.scss`
- `frontend/src/app/features/tickets/feature/ticket-list-page.component.ts`
- `docs/API_SPEC.md`
- `docs/PROJECT_PLAN.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `/tickets` placeholder route with a real Tickets page that lists tickets, supports status/priority filters, opens detail in the same workspace, and displays title/status/priority/created metadata plus basic chat context via conversation/message IDs. The Chat page `New` action now opens a small title form before creating the conversation.

**Why this approach:** A split list/detail view matches the existing support-workspace layout and keeps repeated ticket triage efficient. For chat context, the frontend displays the IDs currently available from the backend detail contract instead of inventing another backend call in this slice.

**Alternatives considered:** A separate `/tickets/:id` page was deferred because the current app shell benefits from selecting tickets without route churn. Showing full originating message content was deferred until the backend exposes richer ticket context or a dedicated conversation summary endpoint.

**Follow-ups / risks:** Frontend status updates/comments are still future slices. Ticket detail can become richer once backend returns the originating message snippet or conversation title.

**Reviewed by human:** [ ]

---
## [2026-08-01] Add backend ticket list and detail APIs

**Prompt/task summary:** Implement the next backend slice for ticket list/detail APIs with status/priority filters and role-scoped authorization.

**Files changed:**
- `backend/src/Api/Controllers/TicketsController.cs`
- `backend/src/Api/Services/CurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ICurrentUserService.cs`
- `backend/src/Application/Common/Interfaces/ITicketRepository.cs`
- `backend/src/Application/Common/Interfaces/IUserRepository.cs`
- `backend/src/Application/Common/Models/PaginatedList.cs`
- `backend/src/Application/Common/Models/TicketListCriteria.cs`
- `backend/src/Application/Tickets/Models/TicketDetailDto.cs`
- `backend/src/Application/Tickets/Models/TicketSummaryDto.cs`
- `backend/src/Application/Tickets/Models/UserRefDto.cs`
- `backend/src/Application/Tickets/Queries/GetTicketDetail/*`
- `backend/src/Application/Tickets/Queries/GetTickets/*`
- `backend/src/Infrastructure/Persistence/Repositories/TicketRepository.cs`
- `backend/src/Infrastructure/Persistence/Repositories/UserRepository.cs`
- `backend/tests/Application.UnitTests/Auth/AuthCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Chat/ChatCommandHandlerTests.cs`
- `backend/tests/Application.UnitTests/Tickets/TicketQueryHandlerTests.cs`
- `docs/API_SPEC.md`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Added `GET /api/v1/tickets` and `GET /api/v1/tickets/{ticketId}`. Ticket list supports `status`, `priority`, `page`, and `pageSize`. The query handlers enforce role scope: employees see created tickets, IT agents see assigned plus unassigned queue tickets, and IT admins see all tickets. Responses include lightweight user references for creator and assignee.

**Why this approach:** Authorization stays in Application query handlers, matching the documented Clean Architecture boundary and keeping role behavior unit-testable without a database. The repository receives already-scoped criteria instead of reading HTTP claims or deciding product permissions itself.

**Alternatives considered:** Putting role checks in controllers was rejected because it would split business authorization across the API layer. Returning raw ticket entities was rejected because API DTOs keep response contracts explicit and prevent accidental field exposure.

**Follow-ups / risks:** Status update, comments, and frontend Tickets page are still separate slices. IT agent queue scope currently follows the documented v1 rule of assigned-to-me or unassigned tickets; if queues become department-based later, the criteria object can be extended.

**Reviewed by human:** [ ]

---
## [2026-08-01] Simplify chat ticket button template

**Prompt/task summary:** Check and fix the Chat page ticket button template after the user reported an error around the disabled/click bindings and conditional label.

**Files changed:**
- `frontend/src/app/features/chat/feature/chat-page.component.html`
- `docs/ai-changelog/AI_CHANGELOG.md`

**What changed:** Replaced the `@if` block inside the ticket action button with a single interpolation expression that chooses between `Ticket created`, `Creating...`, and `Create ticket`.

**Why this approach:** The Angular compiler accepted the original template, but a simple interpolation is enough for this button label and is less likely to trigger editor/template tooling issues.

**Alternatives considered:** Keeping the `@if` block was valid Angular syntax, but it was more structure than this label needed.

**Follow-ups / risks:** None.

**Reviewed by human:** [ ]

---
## Example entry (for reference - delete once the first real entry is added)

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
future - this matches the Clean Architecture principle in `docs/PROJECT_PLAN.md`of
keeping business rules in the Domain layer rather than scattered across handlers.

**Alternatives considered:** A database `CHECK` constraint enforcing transitions was
considered, but Postgres check constraints can't easily express "depends on the
*current* row value being updated" without a trigger, which would move business logic
out of C# and out of unit-test coverage. Rejected in favor of the domain-entity
approach, which is both testable and framework-agnostic.

**Follow-ups / risks:** None currently. If a "bulk reopen" admin feature is added later,
revisit whether the transition graph needs an `Admin` override path.

**Reviewed by human:** [ ]










