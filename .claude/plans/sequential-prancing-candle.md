# Plan: AI Incubator Knowledge Base Platform

## Context

The AI Mentorship Program (REQUIREMENTS.md §2) requires a clean, public GitHub repo with a
root `README.md` (participant name + description), a `src/` tree, and a **public `diary/`**
with `YYYY-MM-DD.md` entries (5 sections each, 2+/week from week 2). The goal is to take the
existing private `D:\Navitas\naiton.rag.service` (.NET 10 RAG service), move it into the
current repo, rebrand it to **AiIncubator**, grow it from a text-only RAG API into a
document-centric **AI Knowledge Base** product (upload → process → embed → store → chat),
add a **Clerk-authenticated** React frontend whose design is inspired by `makvin-team/once-client`,
and make the whole thing recruiter-friendly and runnable.

Locked decisions (from the user): participant name **Diyorbek**; keep **GLM (z.ai)** chat +
**BGE-Large** embeddings behind their existing interfaces; **Week-1 vertical slice first**;
frontend = **core app screens** (Landing, Dashboard, Documents, Chat), not once-client parity.

## Current State (already applied to disk this session)

These mechanical steps were completed before plan mode engaged:

- New layout created: `src/AiIncubator.Server/`, `tests/AiIncubator.Server.Tests/`, `diary/`,
  `docs/`, `.github/workflows/`.
- Backend copied from `naiton.rag.service` (excluding bin/obj/.idea/.git).
- All `Naiton.Rag.Service` namespaces/usings → `AiIncubator.Server` (61 files); csproj files
  renamed to `AiIncubator.Server.csproj` / `AiIncubator.Server.Tests.csproj`.
- `appsettings.json` cleaned: removed internal IP + `naiton` API key, defaulted Qdrant to
  `localhost:6334`, collection `ai-incubator`; added empty `Cors`, `Clerk`, `Documents` sections.
- Test integration collection name → `ai-incubator-it-*`.
- Root `AiIncubator.slnx` and rebranded `docker-compose.yml` (`ai-incubator-qdrant`) created.
- `plans/ai-incubator-platform.md` written as the working source-of-truth.

Not yet verified: a build/test run. **Step 1 of execution is to build and confirm the rename
compiles before adding features.**

## Existing Architecture To Reuse (do not rewrite)

Interface-first, thin controllers, `ApiResponse<T>` envelope, `ExceptionMiddleware`, options
pattern. Key seams to build on:

- `Services/Ingestion/IIngestionService.IngestAsync(documentId, text, metadata, ct)` — already
  chunks → embeds → upserts to Qdrant. **Reuse verbatim** for document ingestion.
- `Services/Retrieval/IQueryService.AnswerAsync(question, topK, ct)` — full RAG. **Reuse** for chat.
- `Services/VectorStore/IVectorStore`, `Services/Embeddings/IEmbeddingClient`,
  `Services/Chat/IChatCompletionClient` — unchanged.
- `Common/ApiResponse.cs`, `Common/ApiError.cs`, `Common/Exceptions/AppException.cs`,
  `Common/Attributes/ControllerNameAttribute.cs`, `Common/Middlewares/ExceptionMiddleware.cs`.
- DI composition root: `Common/Helpers/Configurations/RagServicesRegistration.cs`.
- Conventions in `.claude/rules/api-csharp/*` (file-scoped namespaces, primary-ctor DI,
  regions, `IFoo`+`Foo`, `ConcurrentDictionary` for in-memory caches) — follow exactly.

## Backend Work (Week-1, all additive, same style)

New csproj package: `UglyToad.PdfPig` (PDF text extraction).

### 1. Documents (`Services/Documents/`, `Models/Domain/`, `Controllers/`)
- `Models/Domain/Document.cs` — Id, FileName, ContentType, ModuleId?, Status (enum
  `DocumentStatus`: Pending/Processing/Indexed/Failed), ChunkCount, SizeBytes, CreatedAt.
- `IDocumentTextExtractor` + `DocumentTextExtractor` — `.txt`/`.md` decoded as UTF-8; `.pdf`
  via PdfPig page-text concatenation. Throws `AppException(BadRequest)` on unsupported ext.
- `IDocumentRepository` + `InMemoryDocumentRepository` (`ConcurrentDictionary`).
- `IDocumentService` + `DocumentService` — orchestrates extract → `IIngestionService.IngestAsync`
  (metadata carries `document_id`, `file_name`, optional `module_id`), persists metadata, sets Status.
- `DocumentsController` (`api/documents`): `POST` multipart upload (validate ext + size from
  `DocumentsOptions`), `GET` list (paginated `page`/`pageSize`), `GET {id}`, `DELETE {id}`.

### 2. Knowledge Modules (`Services/Modules/`)
- `Models/Domain/KnowledgeModule.cs` — Id, Name, Description, DocumentCount, Status, CreatedAt.
- `IModuleRepository` + `InMemoryModuleRepository`.
- `IModuleService` + `ModuleService` — create, list, get, `SyncAsync` (re-ingest documents in
  the module whose Status != Indexed; report synced/failed counts).
- `ModulesController` (`api/modules`): `POST` create, `GET` list, `GET {id}`, `POST {id}/sync`.

### 3. Chat with documents (`Services/Chat/Sessions/`)
- `Models/Domain/ChatSession.cs`, `ChatMessage.cs` (Role, Content, Sources, CreatedAt).
- `IChatSessionStore` + `InMemoryChatSessionStore`.
- `IChatService` + `ChatService` — create session; on user message call
  `IQueryService.AnswerAsync`, persist user + assistant (with sources) messages.
- `ChatController` (`api/chat`): `POST sessions`, `GET sessions`, `GET sessions/{id}`,
  `POST sessions/{id}/messages`.

### 4. Clerk auth + CORS (`Common/Helpers/Configurations/`)
- `ClerkOptions` (Authority, Issuer, Audience). Add `Microsoft.AspNetCore.Authentication.JwtBearer`;
  configure JWT bearer to Clerk's issuer/JWKS. `AddAuthentication`/`AddAuthorization` in registration.
- `Program.cs`: `UseAuthentication`/`UseAuthorization`, named CORS policy from `Cors:AllowedOrigins`.
- `[Authorize]` on Documents, Modules, Chat, Query, Ingest controllers. Add anonymous
  `HealthController` (`GET api/health`) for liveness + mock-mode probing.
- When `Clerk:Issuer` is empty (local/dev), skip auth registration so the API still runs and
  tests pass without Clerk creds (documented).

### 5. Tests (`tests/AiIncubator.Server.Tests/`)
- Unit: `DocumentTextExtractor` (txt/md/pdf/unsupported), `DocumentService` (orchestration via
  mocked `IIngestionService`), `ChatService` (mocked `IQueryService`), `ModuleService` sync.
- Existing suite must stay green.

## Frontend Work — `src/AiIncubator.Client` (Week-1)

Vite + React 19 + TypeScript + Tailwind + React Router 7 + `@clerk/clerk-react`. Design tokens
inspired by once-client (neutral palette, rounded cards, sidebar shell), not its features.

- `lib/api.ts` — typed fetch wrapper; injects Clerk session token; **mock mode** when
  `VITE_API_URL` unset or `/api/health` unreachable (serves local fixtures so UI demos offline).
- `auth/` — `ClerkProvider`, `SignedIn`/`SignedOut` route guards.
- `components/app/` — `AppShell`, `Sidebar`, `Header`. `components/ui/` — `Button`, `Card`,
  `Input`, `Badge`, `Spinner`.
- `pages/` — `Landing` (public), `Dashboard` (counts + recent activity), `Documents`
  (drag-drop upload + list + status badges), `Chat` (session list + message thread + source
  citations), `SignIn`/`SignUp` (Clerk components).
- `.env.example` with `VITE_API_URL`, `VITE_CLERK_PUBLISHABLE_KEY`.

## Cross-cutting Deliverables

- Root `README.md`: participant **Diyorbek**, overview, quick start (docker compose up → server →
  client), architecture table, configuration table (incl. Clerk + Embedding/Chat), links to
  `docs/` and `diary/`.
- `docs/ARCHITECTURE.md`: layered diagram + request flow (upload → embed → store → chat).
- `diary/2026-05-29.md`: first entry with the five required sections.
- `.github/workflows/ci.yml`: restore/build/test server (.NET 10); install/build client (Node 22).
- Keep `docker-compose.yml` (Qdrant) at root.

## Verification

1. `dotnet build AiIncubator.slnx` — clean.
2. `dotnet test` — all green (Qdrant integration tests stay gated on `QDRANT_HOST`).
3. `cd src/AiIncubator.Client && npm install && npm run build` — succeeds.
4. Manual: `GET api/health` → 200 anonymous; `GET api/documents` without Clerk token → 401
   (when `Clerk:Issuer` configured).
5. With Qdrant up + Embedding/Chat creds: upload a `.txt`, see it Indexed, ask a question in
   Chat, get an answer with sources. (Embedding needs the private BGE URL; documented as a
   required config — the rest of the app runs/demos via mock mode without it.)

## Out of Scope (later weeks)

Payments, production deploy, monitoring, i18n, admin/learner role split, persistent DB (current
stores are in-memory behind interfaces, swappable later), full once-client landing sections.
