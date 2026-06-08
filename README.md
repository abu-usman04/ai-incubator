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
| POST | `/api/quizzes` | Generate a grounded quiz from a topic or module |
| GET | `/api/quizzes` | List quizzes (paginated) |
| POST | `/api/quizzes/{id}/attempts` | Start a quiz attempt |
| POST | `/api/quizzes/attempts/{attemptId}/answers` | Submit one answer, get live score |
| POST | `/api/learning-plans` | Generate a multi-week learning plan (planning agent) |
| GET | `/api/learning-plans` | List plans (paginated) |
| POST | `/api/learning-plans/{planId}/tasks/{taskId}/complete` | Mark a plan task complete |
| POST | `/api/ingest` / `/api/query` | Low-level RAG ingest/query |

OpenAPI document at `/openapi/v1.json`.

## Development

```powershell
dotnet build AiIncubator.slnx
dotnet test                                  # 85 tests
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
| `Telegram:Enabled` | Start the bot (polling) and reminder worker. Default `false` |
| `Telegram:ReminderIntervalSeconds` | How often due plan tasks are checked (default `300`) |
| `Telegram:BotToken` | **Secret.** BotFather token — `.env` / user-secrets only, never `appsettings.json` |

```powershell
dotnet user-secrets --project src/AiIncubator.Server set "Embedding:ServerUrl" "https://your-bge-server/"
dotnet user-secrets --project src/AiIncubator.Server set "Chat:ApiKey" "your-glm-key"
```

## Notes

- Document/module/session/quiz/plan metadata uses in-memory stores behind interfaces
  (`IDocumentRepository`, `IModuleRepository`, `IChatSessionStore`, `IQuizRepository`,
  `ILearningPlanRepository`); swap to a database without touching callers.
- PDF parsing is an extension point in `DocumentTextExtractor`; `.txt` and `.md` are supported today.
- Quiz generation and the learning-plan agent reuse retrieval (`IQueryService.RetrieveAsync`) and the
  GLM client (`IChatCompletionClient.ChatAsync` with tool/function calling) — no duplicated RAG/LLM plumbing.

## Telegram bot

Disabled by default. To enable: set `Telegram:Enabled=true` and supply the BotFather token via
`.env` (`Telegram__BotToken=...`) or user-secrets — never in `appsettings.json`. When enabled the
server long-polls for updates and runs a reminder worker.

- Link a chat: send `/link <clerkUserId>` to the bot.
- Take a quiz in chat: send `/quiz <quizId>`, then reply with the option number (multiple choice) or
  free text (short answer). The bot grades each answer and reports a running score.
- Reminders: the worker checks due `PlanTask`s on an interval and messages linked chats.

The `telegram:configure` skill can save the token and review access policy instead of editing files
by hand.

### Manual end-to-end test

1. Create a bot with BotFather and copy the token.
2. `dotnet user-secrets --project src/AiIncubator.Server set "Telegram:BotToken" "<token>"` and set
   `Telegram:Enabled=true`.
3. Start the server, open the bot in Telegram, send `/link dev-user`.
4. Generate a quiz (`POST /api/quizzes`), then send `/quiz <quizId>` and answer through to the score.
5. Generate a plan with a near-term due task; within `ReminderIntervalSeconds` the bot sends a reminder.
