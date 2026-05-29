# AI Incubator — AI Knowledge Base Platform

**Participant:** Diyorbek
**Program:** AI Mentorship Program

A retrieval-augmented knowledge base. Upload documents, generate embeddings, store them in a
vector database, and chat with an assistant that answers from your own content — with sources.

## Quick Start

```powershell
# 1. Start the vector database
docker compose up -d

# 2. Run the API (http://localhost:5153)
dotnet run --project src/AiIncubator.Server

# 3. Run the client (http://localhost:5173)
cd src/AiIncubator.Client
npm install
npm run dev
```

The client runs in **mock mode** with no backend or Clerk keys configured, so the UI is
demoable offline. Set `VITE_API_URL` and `VITE_CLERK_PUBLISHABLE_KEY` (see
`src/AiIncubator.Client/.env.example`) to connect to the real API.

## Architecture

```
ai-incubator/
├── src/
│   ├── AiIncubator.Server/   .NET 10 RAG API (controllers → services → Qdrant/embeddings/chat)
│   └── AiIncubator.Client/   React 19 + Vite + Tailwind + Clerk
├── tests/AiIncubator.Server.Tests/   xUnit + Moq + FluentAssertions
├── diary/                    Mentorship progress journal (public)
├── docs/ARCHITECTURE.md      Layered design and request flow
├── plans/                    Implementation plans (source of truth)
└── docker-compose.yml        Qdrant vector database
```

Pipeline: **upload → extract text → chunk → embed (BGE-Large) → store (Qdrant) → retrieve →
prompt → answer (GLM)**. Every external dependency sits behind a project-local interface, so the
vendor or wire format can be swapped without touching consumers. See
[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## API

All endpoints return `ApiResponse<T>` (`{ success, data, error }`) and require a Clerk bearer
token (except `GET /api/health`). When Clerk is not configured the API accepts a development
identity so it runs locally without credentials.

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/api/health` | Liveness probe (anonymous) |
| POST | `/api/documents` | Upload a file (`.txt`, `.md`), extract, embed and index |
| GET | `/api/documents` | List documents (paginated) |
| DELETE | `/api/documents/{id}` | Delete a document and its vectors |
| POST | `/api/modules` | Create a knowledge module |
| POST | `/api/modules/{id}/sync` | Sync a module with its documents |
| POST | `/api/chat/sessions` | Create a chat session |
| POST | `/api/chat/sessions/{id}/messages` | Ask a question, get a grounded answer with sources |
| POST | `/api/ingest` / `/api/query` | Low-level RAG ingest/query |

OpenAPI document at `/openapi/v1.json`.

## Development

```powershell
dotnet build AiIncubator.slnx
dotnet test                                  # 49 tests
cd src/AiIncubator.Client && npm run build   # type-check + bundle
```

Qdrant integration tests are gated on `QDRANT_HOST`:

```powershell
$env:QDRANT_HOST = "localhost"; $env:QDRANT_PORT = "6334"; dotnet test
```

## Configuration

Server settings bind from `appsettings.json` / environment variables / user-secrets.

| Setting | Notes |
| --- | --- |
| `Clerk:Authority` / `Clerk:Issuer` | Clerk OIDC authority/issuer. Empty → dev auth (local only) |
| `Clerk:Audience` | Optional expected audience |
| `Cors:AllowedOrigins` | Allowed client origins (default `http://localhost:5173`) |
| `Embedding:ServerUrl` | **Required.** Internal BGE-Large embedding server |
| `Chat:ApiKey` | **Required.** GLM (z.ai) API key |
| `Qdrant:Host` / `Qdrant:Port` | Vector DB (default `localhost:6334`) |
| `Documents:MaxUploadBytes` / `AllowedExtensions` | Upload limits |

```powershell
dotnet user-secrets --project src/AiIncubator.Server set "Embedding:ServerUrl" "https://your-bge-server/"
dotnet user-secrets --project src/AiIncubator.Server set "Chat:ApiKey" "your-glm-key"
```

## Notes

- Document/module/session metadata uses in-memory stores behind interfaces (`IDocumentRepository`,
  `IModuleRepository`, `IChatSessionStore`); swap to a database without touching callers.
- PDF parsing is an extension point in `DocumentTextExtractor`; `.txt` and `.md` are supported today.
