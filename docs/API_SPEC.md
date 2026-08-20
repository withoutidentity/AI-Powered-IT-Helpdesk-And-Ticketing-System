# API Specification: AI-Powered IT Helpdesk & Ticketing

**Base URL (dev):** `http://localhost:8080/api/v1`
**Auth:** Bearer JWT in `Authorization: Bearer <token>` header, except where noted.
**Content type:** `application/json` unless noted. The chat send-message endpoint uses JSON in the current no-AI slice and will switch to `text/event-stream` when streaming AI is implemented.

This is a human-readable companion to the live **Swagger/OpenAPI** UI that ASP.NET Core
generates automatically (`/swagger`). Keep both in sync; treat Swagger as the source of
truth for exact schemas once the project has real code, and this file as the design-time
reference.

---

## Conventions

### Standard error response (RFC 7807 Problem Details)
```json
{
  "type": "https://httpstatuses.com/400",
  "title": "Validation failed",
  "status": 400,
  "traceId": "00-4bf92f...-01",
  "errors": {
    "username": ["Username is required"]
  }
}
```

### Pagination (list endpoints)
Query params: `?page=1&pageSize=20&sortBy=createdAt&sortDir=desc`

Response envelope:
```json
{
  "items": [ /* ... */ ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 137,
  "totalPages": 7
}
```

### Roles referenced below
`Employee` | `ITAgent` | `ITAdmin`

---

## 1. Auth

### `POST /auth/register`
Creates a new user account. In practice this may be restricted to `ITAdmin` in
production (employees are provisioned via SSO/HR feed) — kept open here for a
self-contained demo/dev flow.

**Auth required:** No (or `ITAdmin` in prod — configurable)

**Request**
```json
{
  "username": "jane.doe",
  "email": "jane.doe@company.com",
  "password": "P@ssw0rd123!",
  "role": "Employee",
  "department": "Finance"
}
```

**Response `201 Created`**
```json
{
  "id": "b3f1e2a0-...",
  "username": "jane.doe",
  "email": "jane.doe@company.com",
  "role": "Employee",
  "department": "Finance",
  "createdAt": "2026-07-29T09:00:00Z"
}
```

**Errors:** `400` validation, `409` username/email already exists

---

### `POST /auth/login`
**Auth required:** No

**Request**
```json
{ "username": "jane.doe", "password": "P@ssw0rd123!" }
```

**Response `200 OK`**
```json
{
  "accessToken": "eyJhbGciOi...",
  "refreshToken": "8f14e45f...",
  "expiresIn": 900,
  "user": {
    "id": "b3f1e2a0-...",
    "username": "jane.doe",
    "role": "Employee"
  }
}
```

**Errors:** `401` invalid credentials, `429` too many attempts (rate limited)

---

### `POST /auth/refresh`
**Auth required:** No (uses refresh token in body)

**Request**
```json
{ "refreshToken": "8f14e45f..." }
```

**Response `200 OK`** — same shape as login response, with rotated tokens. The submitted refresh token is looked up by hash, revoked, and replaced with a newly persisted refresh token hash.

**Errors:** `401` invalid/expired/revoked refresh token

---

## 2. Users

### `GET /users/agents`
Returns IT agents that an admin can assign tickets to. The Angular Tickets detail page uses this endpoint to populate the admin-only assignment dropdown.

**Auth required:** `ITAdmin`

**Response `200 OK`**
```json
[
  {
    "id": "b7c8...",
    "username": "it.agent"
  }
]
```

**Errors:** `403` only IT admins can list assignable agents

---

## 3. Chat

> **Frontend chat implementation note:** the Angular Chat route currently consumes JSON endpoints directly. `POST /chat/conversations/{conversationId}/messages` now performs KB retrieval and returns a non-streaming Groq-generated grounded answer when matching chunks are found. Persisted source citations and token streaming are future AI/RAG slice behavior.

### `POST /chat/conversations`
Starts a new conversation for the authenticated user. The frontend prompts for a conversation title before calling this endpoint from the `New` conversation action.

**Auth required:** Yes (`Employee`, `ITAgent`, `ITAdmin`)

**Request:** *(empty body, or optional initial title)*
```json
{ "title": null }
```

**Response `201 Created`**
```json
{
  "id": "c1a2b3c4-...",
  "userId": "b3f1e2a0-...",
  "title": "New conversation",
  "createdAt": "2026-07-29T09:05:00Z",
  "lastMessageAt": "2026-07-29T09:05:00Z",
  "hasTicket": false
}
```

---

### `GET /chat/conversations`
Lists the authenticated user's own conversations (staff/admin do **not** see other
users' conversations here — that's an explicit privacy boundary, not just a UI filter).

**Auth required:** Yes
**Query:** `?page=1&pageSize=20`

`hasTicket` is `true` when the conversation has already produced its single allowed ticket. The frontend uses this value after refresh/selecting a conversation to disable the create action and show `Ticket created` immediately.

**Response `200 OK`** — paginated envelope of:
```json
{
  "id": "c1a2b3c4-...",
  "title": "Wi-Fi issue",
  "lastMessageAt": "2026-07-29T09:12:00Z",
  "hasTicket": true
}
```

---

### `GET /chat/conversations/{conversationId}/messages`
**Auth required:** Yes — must be the conversation owner, or `ITAgent`/`ITAdmin` **only
if** the conversation has an associated ticket assigned to them (support-context access,
not blanket access).

**Response `200 OK`**
```json
[
  {
    "id": "m1...",
    "sender": "User",
    "content": "How do I connect to the office Wi-Fi?",
    "intent": "Question",
    "createdAt": "2026-07-29T09:05:10Z"
  },
  {
    "id": "m2...",
    "sender": "Assistant",
    "content": "Go to Settings > Wi-Fi and select 'Office-5G'...",
    "intent": null,
    "sourceDocuments": ["IT_Manual_v2.pdf#chunk-14"],
    "createdAt": "2026-07-29T09:05:12Z"
  }
]
```

---

### `POST /chat/conversations/{conversationId}/messages`
Sends a user message, embeds the message as a KB search query, retrieves relevant chunks, sends those chunks plus the question to Groq for a grounded answer, and returns the persisted user message plus assistant response. If Groq fails, the backend falls back to a retrieval-only response. Streaming SSE replaces this JSON response in a later slice.

**Auth required:** Yes - must be the conversation owner.
**Response content-type:** `application/json` in the current no-AI slice; `text/event-stream` when streaming AI is implemented.

**Request**
```json
{ "content": "The printer on floor 3 is jammed, can someone take a look?" }
```

**Response `200 OK` (current no-AI slice)**
```json
{
  "userMessage": {
    "id": "m1...",
    "conversationId": "c1a2b3c4-...",
    "sender": "User",
    "content": "The printer on floor 3 is jammed, can someone take a look?",
    "intent": null,
    "createdAt": "2026-07-30T12:00:00Z"
  },
  "assistantMessage": {
    "id": "m2...",
    "conversationId": "c1a2b3c4-...",
    "sender": "Assistant",
    "content": "1. Open Wi-Fi settings and reconnect to Office-5G.\n2. If it still fails, restart the Wi-Fi adapter and try again.\n3. Create a ticket if the adapter is missing or the error persists.\n\nSources:\n- Office Wi-Fi Guide, chunk 0",
    "intent": null,
    "createdAt": "2026-07-30T12:00:00Z"
  }
}
```

**Future streaming response (AI slice):** this endpoint will switch to SSE frames for `intent`, `token`, optional `sources`, and `done` once Groq streaming and persisted source citations are implemented.
---

### `POST /chat/conversations/{conversationId}/messages/{messageId}/ticket`
Creates the single ticket for a conversation from a persisted user message in the current no-AI/manual handoff slice. This is the explicit version of the future AI `Action` intent path.

> **Frontend handoff implementation note:** the Angular Chat page exposes this as a conversation-level `Create ticket` action in the message panel header. The current UI uses the latest user message as the originating message, sends its content as the default title/description, and sends `Medium` priority.

**Auth required:** Yes - must be the conversation owner.

**Request** *(all fields optional; title/description default from the message content, priority defaults to `Medium`)*
```json
{
  "title": "Printer jammed - Floor 3",
  "description": "The printer on floor 3 is jammed, can someone take a look?",
  "priority": "High"
}
```

**Response `201 Created`**
```json
{
  "id": "t1042...",
  "conversationId": "c1a2b3c4-...",
  "messageId": "m1...",
  "createdBy": "b3f1e2a0-...",
  "assignedTo": null,
  "title": "Printer jammed - Floor 3",
  "description": "The printer on floor 3 is jammed, can someone take a look?",
  "status": "Open",
  "priority": "High",
  "createdAt": "2026-08-01T09:00:00Z",
  "updatedAt": "2026-08-01T09:00:00Z",
  "attachmentsJson": "[]"
}
```

**Errors:** `403` not the conversation owner, `404` conversation/message not found, `400` assistant message or invalid priority, `409` ticket already exists for the conversation

**Errors:** `403` not the conversation owner, `404` conversation not found, `429` rate limited, `502` upstream AI provider error after AI integration is enabled

---

## 4. Knowledge Base (Admin)

> Implementation status: the current backend supports JSON-based document creation,
> read APIs, server-side chunking, manual embedding reindexing, and semantic chunk search. `POST /kb/documents`
> splits pasted text into ordered chunks and stores them without vectors initially.
> `POST /kb/documents/{documentId}/reindex` embeds each chunk with Google `gemini-embedding-001`
> shortened to 768 dimensions, then stores the vectors in PostgreSQL `pgvector`
> (`document_chunks.embedding vector(768)`). The backend limits embedding provider calls to
> 2 requests/minute and 10 requests/day. File upload, PDF
> extraction, automatic background ingestion, and chat-time LLM-generated answers are planned for
> later KB/RAG slices.

### `POST /kb/documents`
Creates a knowledge-base document from pasted/admin-supplied text. The backend chunks the full pasted content automatically; admins do not need to paste one topic at a time. This is still a no-embed API and does not parse uploaded files yet.

**Auth required:** `ITAdmin`
**Content-Type:** `application/json`

**Request**
```json
{
  "title": "Office Wi-Fi Guide",
  "sourceFile": "office-wifi.md",
  "sourceType": "text/markdown",
  "content": "# Office Wi-Fi\nConnect to Office-5G using your company account...\n\n# VPN\nInstall the VPN client and sign in..."
}
```

**Response `201 Created`**
```json
{
  "id": "d1...",
  "title": "Office Wi-Fi Guide",
  "sourceFile": "office-wifi.md",
  "sourceType": "text/markdown",
  "contentHash": "64-char-sha256",
  "status": "Ready",
  "failureReason": null,
  "uploadedAt": "2026-08-02T08:00:00Z",
  "updatedAt": "2026-08-02T08:00:00Z",
  "chunks": [
    {
      "id": "ch1...",
      "documentId": "d1...",
      "chunkIndex": 0,
      "content": "# Office Wi-Fi\nConnect to Office-5G using your company account...",
      "contentHash": "64-char-sha256",
      "tokenCount": 8,
      "embeddingModel": null,
      "embeddingDimensions": null,
      "createdAt": "2026-08-02T08:00:00Z"
    },
    {
      "id": "ch2...",
      "documentId": "d1...",
      "chunkIndex": 1,
      "content": "# VPN\nInstall the VPN client and sign in...",
      "contentHash": "64-char-sha256",
      "tokenCount": 8,
      "embeddingModel": null,
      "embeddingDimensions": null,
      "createdAt": "2026-08-02T08:00:00Z"
    }
  ]
}
```

**Errors:** `400` validation, `403` only IT admins can create KB documents

---

### `GET /kb/documents`
Lists knowledge-base documents visible to IT staff.

**Auth required:** `ITAgent`, `ITAdmin`
**Query:** `?page=1&pageSize=20`

**Response `200 OK`**
```json
{
  "items": [
    {
      "id": "d1...",
      "title": "Office Wi-Fi Guide",
      "sourceFile": "office-wifi.md",
      "sourceType": "text/markdown",
      "contentHash": "64-char-sha256",
      "status": "Ready",
      "failureReason": null,
      "chunkCount": 2,
      "uploadedAt": "2026-08-02T08:00:00Z",
      "updatedAt": "2026-08-02T08:00:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1,
  "totalPages": 1
}
```

**Errors:** `400` invalid page/pageSize, `403` employees cannot view the KB admin API

---

### `GET /kb/documents/{documentId}`
Returns a document plus its currently stored chunks.

**Auth required:** `ITAgent`, `ITAdmin`

**Response `200 OK`** - same shape as the `POST /kb/documents` response.

**Errors:** `403` employees cannot view the KB admin API, `404` document not found

---

### `POST /kb/documents/{documentId}/reindex`
Embeds only chunks that do not already have the current embedding model and dimension metadata, then writes vectors to `document_chunks.embedding`. Re-running this endpoint is incremental and does not spend provider requests on chunks that are already embedded for the active model/dimensions.

**Auth required:** `ITAdmin`

**Response `200 OK`**
```json
{
  "documentId": "d1...",
  "status": "Ready",
  "embeddedChunkCount": 2,
  "embeddingModel": "gemini-embedding-001",
  "embeddingDimensions": 768,
  "updatedAt": "2026-08-11T09:00:00Z"
}
```

**Errors:** `400` embedding provider failure, local embedding rate-limit failure, or no chunks; `403` only IT admins can reindex KB documents; `404` document not found

---


### `GET /kb/search`
Embeds the query with the configured embedding provider and returns the nearest stored KB chunks using PostgreSQL `pgvector` cosine distance ordering. This is the first retrieval endpoint used to verify that document embeddings work before wiring full AI-generated answers into chat.

**Auth required:** `ITAgent`, `ITAdmin`
**Query:** `?query=wifi%20not%20working&limit=5`

- `query`: required, max 1000 characters
- `limit`: optional, defaults to `5`, range `1..10`

**Response `200 OK`**
```json
{
  "query": "wifi not working",
  "limit": 5,
  "items": [
    {
      "documentId": "d1...",
      "documentTitle": "Office Wi-Fi Guide",
      "sourceFile": "office-wifi.md",
      "sourceType": "text/markdown",
      "chunkId": "ch1...",
      "chunkIndex": 0,
      "content": "# Office Wi-Fi\nConnect to Office-5G using your company account...",
      "preview": "Office Wi-Fi Connect to Office-5G using your company account...",
      "embeddingModel": "gemini-embedding-001",
      "embeddingDimensions": 768,
      "rank": 1
    }
  ]
}
```

**Errors:** `400` invalid query/limit or embedding provider failure; `403` employees cannot search the KB admin API

---### `DELETE /kb/documents/{documentId}`
Removes the document and its chunks. Planned for a later KB management slice.

**Auth required:** `ITAdmin`
**Response:** `204 No Content`

---
## 5. Tickets

### `GET /tickets`
Lists tickets visible to the authenticated user. The Angular Tickets page consumes this endpoint for the ticket list and status/priority filters.

**Auth required:** Yes - `Employee` sees tickets they created, `ITAgent` sees tickets assigned to them plus unassigned queue tickets, `ITAdmin` sees all tickets.

**Query:** `?status=Open&priority=High&page=1&pageSize=20`

- `status`: optional, one of `Open`, `InProgress`, `Resolved`, `Closed`
- `priority`: optional, one of `Low`, `Medium`, `High`
- `page`: optional, defaults to `1`
- `pageSize`: optional, defaults to `20`, max `100`

**Response `200 OK`**
```json
{
  "items": [
    {
      "id": "t1042...",
      "conversationId": "c1a2b3c4-...",
      "messageId": "m1...",
      "title": "Printer jammed - Floor 3",
      "status": "Open",
      "priority": "Medium",
      "createdBy": { "id": "b3f1e2a0-...", "username": "jane.doe" },
      "assignedTo": null,
      "createdAt": "2026-07-29T09:06:00Z",
      "updatedAt": "2026-07-29T09:06:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1,
  "totalPages": 1
}
```

**Errors:** `400` invalid status/priority/page/pageSize

---

### `GET /tickets/{ticketId}`
Returns one ticket if it is visible to the authenticated user. The Angular Tickets page uses this detail response to show ticket metadata plus basic chat context from `conversationId` and `messageId`.

**Auth required:** Yes - owner `Employee`, assigned/unassigned-queue `ITAgent`, or `ITAdmin`.

**Response `200 OK`**
```json
{
  "id": "t1042...",
  "conversationId": "c1a2b3c4-...",
  "messageId": "m1...",
  "title": "Printer jammed - Floor 3",
  "description": "The printer on floor 3 is jammed, can someone take a look?",
  "status": "Open",
  "priority": "Medium",
  "createdBy": { "id": "b3f1e2a0-...", "username": "jane.doe" },
  "assignedTo": null,
  "createdAt": "2026-07-29T09:06:00Z",
  "updatedAt": "2026-07-29T09:06:00Z",
  "attachmentsJson": "[]"
}
```

**Errors:** `403` ticket exists but is outside the current user's scope, `404` ticket not found

---

### `PATCH /tickets/{ticketId}/status`
Updates a ticket status along the v1 workflow: `Open -> InProgress -> Resolved -> Closed`. The Angular Tickets detail view exposes this as a next-status action for `ITAgent` and `ITAdmin` users.

**Auth required:** `ITAgent` only for tickets assigned to them, `ITAdmin` for any ticket. `Employee` cannot update status in this slice.

**Request**
```json
{ "status": "InProgress" }
```

**Response `200 OK`** - updated ticket detail object.

**Errors:** `403` not allowed to update this ticket, `404` ticket not found, `400` invalid status value, `409` invalid status transition.

---

### `PATCH /tickets/{ticketId}/assignment`
Assigns a ticket to an IT agent. The Angular Tickets detail view exposes this as `Assign to me` for IT agents and an agent dropdown for IT admins.

**Auth required:** `ITAgent` can assign only themselves, and only when the ticket is unassigned or already assigned to them. `ITAdmin` can assign any visible ticket to any `ITAgent`. `Employee` cannot assign tickets.

**Request**
```json
{ "assignedToUserId": "b7c8..." }
```

**Response `200 OK`** - updated ticket detail object.

**Errors:** `403` not allowed to assign this ticket, `404` ticket or assignee not found, `400` assignee is not an IT agent or validation failed.

---

### `GET /tickets/{ticketId}/comments`
Lists comments on a ticket in chronological order.

**Auth required:** Ticket owner `Employee`, assigned/unassigned-queue `ITAgent`, or `ITAdmin`.

**Response `200 OK`**
```json
[
  {
    "id": "cm1...",
    "ticketId": "t1042...",
    "author": { "id": "b7c8...", "username": "it.agent" },
    "content": "Replaced the toner cartridge, testing now.",
    "createdAt": "2026-07-29T10:00:00Z"
  }
]
```

**Errors:** `403` ticket outside user scope, `404` ticket not found

---

### `POST /tickets/{ticketId}/comments`
Adds a comment to a ticket.

**Auth required:** Ticket owner `Employee`, assigned/unassigned-queue `ITAgent`, or `ITAdmin`.

**Request**
```json
{ "content": "Replaced the toner cartridge, testing now." }
```

**Response `201 Created`**
```json
{
  "id": "cm1...",
  "ticketId": "t1042...",
  "author": { "id": "b7c8...", "username": "it.agent" },
  "content": "Replaced the toner cartridge, testing now.",
  "createdAt": "2026-07-29T10:00:00Z"
}
```

**Errors:** `400` empty/too-long comment, `403` ticket outside user scope, `404` ticket not found

---

### `GET /tickets/{ticketId}/activities`
Lists audit activity on a ticket in chronological order. Activity rows are created when a ticket is opened, assigned, or status changes.

**Auth required:** Ticket owner `Employee`, assigned/unassigned-queue `ITAgent`, or `ITAdmin`.

**Response `200 OK`**
```json
[
  {
    "id": "ta1...",
    "ticketId": "t1042...",
    "actor": { "id": "b7c8...", "username": "it.agent" },
    "action": "StatusChanged",
    "field": "status",
    "oldValue": "Open",
    "newValue": "InProgress",
    "createdAt": "2026-07-29T10:00:00Z"
  }
]
```

**Errors:** `403` ticket outside user scope, `404` ticket not found

---
## 6. Dashboard

### `GET /dashboard/stats`
**Auth required:** `ITAgent`, `ITAdmin`

**Response `200 OK`**
```json
{
  "openTickets": 12,
  "inProgressTickets": 4,
  "resolvedToday": 7,
  "avgResolutionHours": 3.2,
  "ticketsByCategory": [
    { "category": "Network", "count": 5 },
    { "category": "Hardware", "count": 8 },
    { "category": "Account/Password", "count": 3 }
  ]
}
```

---

## 7. Health

### `GET /health/live`
**Auth required:** No
**Response `200 OK`** — process liveness only.

### `GET /health/ready`
**Auth required:** No
**Response `200 OK` / `503 Service Unavailable`** — checks DB connectivity and (optionally,
non-blocking) Groq API reachability.









