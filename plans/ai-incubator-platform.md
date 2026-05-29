# Plan: AI Incubator Knowledge Base Platform

## Participant and Program

- Participant: Diyorbek
- Program: AI Mentorship Program. Requirements section 2 (GitHub Repository).
- Required at repo root: README.md (name + description), src/, public diary/ with YYYY-MM-DD.md entries.
- Diary cadence: 2+ entries per week from week 2. Each entry has five sections: What was done, Problems encountered, Solutions, Next plan, Time spent. One Loom per week.

## Decisions (locked)

- AI providers: keep GLM (z.ai, public, key required) for chat and BGE-Large (internal server, URL required) for embeddings, exactly as ported. Both stay behind their existing interfaces so they remain swappable.
- Auth: Clerk. Backend validates Clerk-issued JWT bearer tokens. Frontend uses Clerk React SDK.
- Sequencing: Week-1 vertical slice first, then iterate.
- Frontend: core app screens only (Landing, Dashboard, Documents, Chat). Design inspired by once-client, not feature parity.

## Target Layout

- Root: README.md, diary/, docs/, plans/, .github/workflows/, docker-compose.yml, AiIncubator.slnx, .gitignore
- src/AiIncubator.Server: ported .NET 10 backend, namespace AiIncubator.Server
- src/AiIncubator.Client: React 19 + TypeScript + Vite + Tailwind + React Router 7 + Clerk
- tests/AiIncubator.Server.Tests: ported xUnit suite, namespace AiIncubator.Server.Tests

## Backend Work (Week 1)

Existing pipeline kept intact: Ingestion (CharacterTextSplitter) to Embeddings (BGE) to VectorStore (Qdrant) to Retrieval (QueryService + RagPromptBuilder) to Chat (GLM). All behind interfaces with ApiResponse envelope and ExceptionMiddleware.

New, additive, same style (interface-first, thin controllers, ApiResponse):

1. Documents
   - IDocumentRepository plus in-memory thread-safe implementation. Document entity: Id, FileName, ContentType, ModuleId, Status, ChunkCount, SizeBytes, CreatedAt.
   - IDocumentTextExtractor plus implementation: plain text and markdown read directly, PDF via UglyToad.PdfPig.
   - IDocumentService: orchestrates extract to ingest (reuse IIngestionService) and persists metadata, sets Status.
   - DocumentsController: POST upload (multipart), GET list (paginated), GET by id, DELETE.

2. Knowledge Modules
   - IModuleRepository plus in-memory implementation. KnowledgeModule: Id, Name, Description, DocumentCount, Status, CreatedAt.
   - IModuleService: create, list, get, sync (re-ingest documents flagged pending in the module).
   - ModulesController: POST create, GET list, GET by id, POST sync.

3. Chat with documents
   - IChatSessionStore plus in-memory implementation. ChatSession: Id, Title, Messages, CreatedAt. ChatMessage: Role, Content, Sources, CreatedAt.
   - IChatService: create session, append user message, run RAG via IQueryService, append assistant message with sources.
   - ChatController: POST sessions, GET sessions, GET session by id, POST sessions/{id}/messages.

4. Clerk auth
   - ClerkOptions: Authority, Issuer, Audience. JwtBearer authentication against Clerk JWKS.
   - [Authorize] on Documents, Modules, Chat, Query, Ingest controllers. Health endpoint anonymous.
   - CORS policy allowing the client origin from configuration.

## Frontend Work (Week 1)

- Vite scaffold. Tailwind. React Router 7. @clerk/clerk-react.
- lib/api.ts: typed fetch wrapper attaching Clerk session token; mock mode when VITE_API_URL absent or backend unreachable.
- auth: ClerkProvider, SignedIn/SignedOut guards, protected routes.
- components/app: AppShell, Sidebar, Header. components/ui: Button, Card, Input, Badge, Spinner.
- pages: Landing (public), Dashboard, Documents (upload + list), Chat (session + message stream), SignIn/SignUp.
- Design tokens inspired by once-client: clean, modern, neutral palette, rounded cards, sidebar layout.

## Cross-cutting

- README at root: Diyorbek, project overview, quick start (docker + server + client), architecture table, configuration, links to docs and diary.
- docs/ARCHITECTURE.md: layered diagram and request flow.
- diary/2026-05-29.md: first entry, five sections.
- .github/workflows/ci.yml: restore/build/test server; install/build client.
- docker-compose.yml: Qdrant, renamed.

## Validation

- dotnet build AiIncubator.slnx passes.
- dotnet test passes (Qdrant integration tests stay gated on QDRANT_HOST).
- npm run build for client passes.
- Manual: unauthorized request to a protected endpoint returns 401.

## Out of Scope (later weeks)

- Payments, mobile-specific UI, production deploy, monitoring, i18n, admin/learner role split, persistent database (current stores are in-memory behind interfaces, swappable later).
