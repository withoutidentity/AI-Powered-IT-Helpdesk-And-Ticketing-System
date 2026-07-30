# API Specification: AI-Powered IT Helpdesk & Ticketing

**Base URL (dev):** `http://localhost:8080/api/v1`
**Auth:** Bearer JWT in `Authorization: Bearer <token>` header, except where noted.
**Content type:** `application/json` unless noted (SSE endpoint uses `text/event-stream`).

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

## 2. Chat

### `POST /chat/conversations`
Starts a new conversation for the authenticated user.

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
  "createdAt": "2026-07-29T09:05:00Z"
}
```

---

### `GET /chat/conversations`
Lists the authenticated user's own conversations (staff/admin do **not** see other
users' conversations here — that's an explicit privacy boundary, not just a UI filter).

**Auth required:** Yes
**Query:** `?page=1&pageSize=20`

**Response `200 OK`** — paginated envelope of:
```json
{
  "id": "c1a2b3c4-...",
  "title": "Wi-Fi issue",
  "lastMessageAt": "2026-07-29T09:12:00Z",
  "hasOpenTicket": true
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

### `POST /chat/conversations/{conversationId}/messages` (streaming)
Sends a user message and streams the assistant's reply.

**Auth required:** Yes — must be the conversation owner.
**Response content-type:** `text/event-stream`

**Request**
```json
{ "content": "The printer on floor 3 is jammed, can someone take a look?" }
```

**Response (SSE stream)**
```
event: intent
data: {"intent":"Action"}

event: token
data: {"delta":"Got"}

event: token
data: {"delta":" it"}

event: token
data: {"delta":" — I've opened ticket #1042 for the floor 3 printer."}

event: done
data: {"messageId":"m3...","ticketId":"t1042...","intent":"Action"}
```

For `Question` intent, an additional `event: sources` frame is emitted before `done`,
listing the knowledge-base chunks used as context.

**Errors:** `403` not the conversation owner, `404` conversation not found, `429` rate
limited, `502` upstream AI provider error (surfaced as a graceful in-chat error message,
not a raw 502 to the end user in the UI layer)

---

## 3. Knowledge Base (Admin)

### `POST /kb/documents`
Uploads a document for the RAG pipeline. Multipart form upload.

**Auth required:** `ITAdmin`
**Content-Type:** `multipart/form-data`

**Request:** form field `file` (PDF/Markdown/plain text, max 10MB)

**Response `202 Accepted`** *(processing is async — chunking/embedding happens in a
background job)*
```json
{
  "id": "d1...",
  "title": "IT_Manual_v2.pdf",
  "status": "Processing",
  "uploadedAt": "2026-07-29T08:00:00Z"
}
```

---

### `GET /kb/documents`
**Auth required:** `ITAdmin`

**Response `200 OK`**
```json
[
  {
    "id": "d1...",
    "title": "IT_Manual_v2.pdf",
    "status": "Ready",
    "chunkCount": 42,
    "uploadedAt": "2026-07-29T08:00:00Z"
  }
]
```

---

### `POST /kb/documents/{documentId}/reindex`
Re-runs chunking + embedding (e.g. after changing the embedding model).

**Auth required:** `ITAdmin`
**Response:** `202 Accepted`

---

### `DELETE /kb/documents/{documentId}`
Removes the document and its chunks.

**Auth required:** `ITAdmin`
**Response:** `204 No Content`

---

## 4. Tickets

### `GET /tickets`
**Auth required:** `ITAgent` (sees tickets assigned to them, or unassigned in their
queue), `ITAdmin` (sees all)

**Query:** `?status=Open&priority=High&assignedTo=me&page=1&pageSize=20`

**Response `200 OK`** — paginated envelope of:
```json
{
  "id": "t1042...",
  "title": "Printer jammed - Floor 3",
  "status": "Open",
  "priority": "Medium",
  "createdBy": "jane.doe",
  "assignedTo": null,
  "createdAt": "2026-07-29T09:06:00Z"
}
```

---

### `GET /tickets/{ticketId}`
**Auth required:** Owner (`Employee` who created it), or `ITAgent`/`ITAdmin`

**Response `200 OK`**
```json
{
  "id": "t1042...",
  "title": "Printer jammed - Floor 3",
  "description": "The printer on floor 3 is jammed, can someone take a look?",
  "status": "Open",
  "priority": "Medium",
  "conversationId": "c1a2b3c4-...",
  "createdBy": { "id": "b3f1e2a0-...", "username": "jane.doe" },
  "assignedTo": null,
  "comments": [],
  "createdAt": "2026-07-29T09:06:00Z",
  "updatedAt": "2026-07-29T09:06:00Z"
}
```

---

### `PATCH /tickets/{ticketId}/status`
**Auth required:** `ITAgent` (assigned agent only), `ITAdmin`

**Request**
```json
{ "status": "InProgress", "assignedTo": "b7c8..." }
```

**Response `200 OK`** — updated ticket object.

**Errors:** `409` invalid status transition (e.g. `Closed → Open` must go through
`InProgress`/`Resolved` per the defined lifecycle)

---

### `POST /tickets/{ticketId}/comments`
**Auth required:** Ticket owner, assigned `ITAgent`, or `ITAdmin`

**Request**
```json
{ "content": "Replaced the toner cartridge, testing now." }
```

**Response `201 Created`**
```json
{
  "id": "cm1...",
  "ticketId": "t1042...",
  "authorId": "b7c8...",
  "content": "Replaced the toner cartridge, testing now.",
  "createdAt": "2026-07-29T10:00:00Z"
}
```

---

## 5. Dashboard

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

## 6. Health

### `GET /health/live`
**Auth required:** No
**Response `200 OK`** — process liveness only.

### `GET /health/ready`
**Auth required:** No
**Response `200 OK` / `503 Service Unavailable`** — checks DB connectivity and (optionally,
non-blocking) Groq API reachability.
