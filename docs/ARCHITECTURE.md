# Architecture

## Overview

AI Incubator is a retrieval-augmented knowledge base. The .NET API ingests documents, embeds
their chunks, stores the vectors in Qdrant, and answers questions grounded in retrieved context.
The React client is a thin presentation layer over the API.

## Layers (AiIncubator.Server)

| Layer | Types | Role |
| --- | --- | --- |
| Controllers | `Documents`, `Modules`, `Chat`, `Ingest`, `Query`, `Health` | HTTP surface, kebab-case routes, thin delegation |
| Documents | `IDocumentService`, `IDocumentTextExtractor`, `IDocumentRepository` | Upload, extract, persist metadata |
| Modules | `IModuleService`, `IModuleRepository` | Group documents, sync state |
| Chat | `IChatService`, `IChatSessionStore` | Sessions and message history |
| Ingestion | `IIngestionService`, `ITextSplitter` | Chunk, embed, upsert |
| Retrieval | `IQueryService`, `RagPromptBuilder` | Embed query, retrieve, prompt, answer |
| Embeddings | `IEmbeddingClient` → `BgeLargeEmbeddingClient` | Internal BGE-Large server |
| Chat model | `IChatCompletionClient` → `GlmChatClient` | GLM (z.ai), OpenAI-compatible |
| VectorStore | `IVectorStore` → `QdrantVectorStore` | Collection bootstrap, upsert, search, delete |
| Auth | `AddAiIncubatorAuth`, `DevAuthenticationHandler` | Clerk JWT, dev fallback |
| Common | `ApiResponse<T>`, `ApiError`, `AppException`, `ExceptionMiddleware` | Uniform envelope and error funnel |

Every external dependency sits behind a project-local interface. The in-memory repositories
(`InMemoryDocumentRepository`, `InMemoryModuleRepository`, `InMemoryChatSessionStore`) can be
replaced with a database implementation without touching their consumers.

## Request flow

### Upload and index

```
Client → POST /api/documents (multipart)
  → DocumentService.UploadAsync
    → DocumentTextExtractor.ExtractAsync        (.txt / .md → text)
    → IngestionService.IngestAsync
        → CharacterTextSplitter.Split           (chunks)
        → BgeLargeEmbeddingClient.EmbedBatch    (vectors)
        → QdrantVectorStore.Upsert              (vectors + payload incl. source_document_id)
    → DocumentRepository.Update (Status = Indexed, ChunkCount)
```

### Chat with documents

```
Client → POST /api/chat/sessions/{id}/messages
  → ChatService.SendMessageAsync
    → append user message
    → QueryService.AnswerAsync
        → embed question → QdrantVectorStore.Search (top-K)
        → RagPromptBuilder.Build → GlmChatClient.Complete
    → append assistant message (with sources) → return
```

## Authentication

`AddAiIncubatorAuth` registers Clerk JWT bearer validation when `Clerk:Issuer`/`Clerk:Authority`
is configured (tokens validated against Clerk's JWKS via OIDC discovery). When unconfigured, a
`DevAuthenticationHandler` authenticates every request as a static development user so the API and
tests run locally. Controllers are `[Authorize]`; `HealthController` is `[AllowAnonymous]`.

## Client (AiIncubator.Client)

- `lib/ApiProvider.tsx` builds a typed `ApiClient`; injects the Clerk token when enabled and
  falls back to an in-memory mock client when `VITE_API_URL` is unset.
- `components/app` (AppShell, Sidebar, Header) + `components/ui` (Button, Card, Input, Badge,
  Spinner). Pages: Landing, Dashboard, Documents, Chat, SignIn/SignUp.
- `auth/RequireAuth.tsx` guards `/app` routes; open in mock mode.
