# Plan: Week-2 — Quiz Mode, Learning Plan Agent, Telegram Bot

Source of truth for the Week-2 vertical slice on top of the existing AI Incubator RAG platform. Keep this file up to date as work proceeds; do not append-only. No code blocks here by design — describe behavior, types, and signatures in prose so the implementation cannot be misread.

## Goal

Add three additive features that reuse the existing Week-1 pipeline (ingestion, embeddings, Qdrant, retrieval, GLM chat, Clerk auth) without duplicating any RAG or LLM plumbing:

1. Quiz Mode — generate grounded quizzes from a module/documents, run attempts one question at a time, grade live.
2. Learning Plan Agent — a GLM tool-calling agent that produces a typed multi-week plan grounded in the knowledge base.
3. Telegram Bot — send reminders for due plan tasks and run quizzes interactively in chat.

## Decisions (locked)

- LLM access: inject the existing `IChatCompletionClient`. Use `CompleteAsync` for plain text and `ChatAsync` (full GLM surface with `tools`) for structured tool/function calling. Both are already on the interface, so all new services are mockable with Moq. Never call GLM HTTP directly.
- Retrieval: reuse retrieval by extracting a `RetrieveAsync(query, topK)` method onto `IQueryService` that returns the grounded `RetrievedChunk` list (embed then vector search). `AnswerAsync` is refactored to call `RetrieveAsync` internally so behavior is unchanged. Quiz generation grounds on these chunks. This does not bypass the vector store.
- Telegram: use the official `Telegram.Bot` NuGet package version `22.10.0.1`. Confirmed it restores and builds on this machine (probed in a throwaway project), so the raw-`HttpClient` fallback is NOT used. Bot token is read only from `.env` / user-secrets, never committed.
- Persistence: all new stores are in-memory `ConcurrentDictionary`-backed repositories behind interfaces, registered in DI, swappable later — matching the existing in-memory repos.
- Auth: every new controller carries `[Authorize]` exactly like the existing controllers. The Telegram background worker and webhook/polling path are not behind Clerk (the bot authenticates via its token); Telegram chats are linked to a Clerk user via a stored mapping.
- Structured output: agents must return typed objects. Tool/function arguments returned by GLM are JSON strings; parse them with `System.Text.Json` into typed request models and validate. On parse/validation failure, throw `AppException(InternalServerError, ..., "INTERNAL_ERROR")`.
- IDs: `Guid.NewGuid().ToString("N")`. Timestamps: `DateTime.UtcNow`. Matches existing services.

## Conventions to follow (already in the repo)

- One class/interface per file. Files 200-400 lines, methods under 30 lines, max 4 params (else options object).
- Controllers: `[Authorize]`, `[ApiController]`, `[Route("api/kebab-case/")]`, `[ControllerName(name: "kebab-case")]`, primary-constructor DI, thin — no try/catch, let `ExceptionMiddleware` handle errors. `[ProducesResponseType]` and XML summary/remarks (with sample request) on every endpoint. Return `ApiResponse<T>.Ok(...)`.
- Services: throw `AppException(statusCode, message, code)` with canonical codes (`NOT_FOUND`, `BAD_REQUEST`, `INTERNAL_ERROR`). Interface + implementation, registered in DI.
- Domain entities in `Models/Domain/`, request DTOs in `Models/RequestModels/<Domain>/`, enums in `Common/Enums/`, options in `Common/Helpers/Configurations/`.
- All I/O async. List endpoints paginated via `PagedResult<T>` (Items, Page, PageSize, TotalCount). In-memory repos use `ConcurrentDictionary`.
- Tests: xUnit + Moq + FluentAssertions, mirroring `tests/AiIncubator.Server.Tests/` layout. Service tests new the service with mocked deps. Controller tests use `RagWebApplicationFactory` (which skips `.env`, so dev auth is active and requests are authorized).
- Frontend: React 19 + TS, 2-space indent, single quotes, semicolons, named prop interfaces, no `React.FC`, no `any`. Typed client in `lib/api.ts`, types in `lib/types.ts`, mock implementation in `lib/mock.ts` (mock mode must keep working with no backend). Pages under `src/pages/`, routes in `App.tsx`, nav in `Sidebar.tsx`. String-literal unions for enums (not TS `enum`).

---

## Feature 1 — Quiz Mode

### 1.1 Enums (`Common/Enums/`)

- `QuestionType` — `MultipleChoice`, `ShortAnswer`.
- `AttemptStatus` — `InProgress`, `Completed`.
- `QuizStatus` — `Ready`. (Single state for now; reserved for future draft/archived.)

### 1.2 Domain models (`Models/Domain/`)

- `Quiz` — `Id`, `Title`, `Topic`, `ModuleId` (nullable), `Status` (`QuizStatus`), `Questions` (`IReadOnlyList<QuizQuestion>`), `CreatedAt`.
- `QuizQuestion` — `Id`, `Type` (`QuestionType`), `Prompt`, `Options` (`IReadOnlyList<string>`; empty for short answer), `CorrectOptionIndex` (nullable int; set for multiple choice), `ExpectedAnswer` (nullable string; the reference answer for short-answer LLM grading), `Points` (default 1).
- `QuizAttempt` — `Id`, `QuizId`, `Status` (`AttemptStatus`), `Answers` (`IList<QuizAnswer>`), `Score`, `MaxScore`, `StartedAt`, `CompletedAt` (nullable).
- `QuizAnswer` — `QuestionId`, `SelectedOptionIndex` (nullable), `Text` (nullable), `IsCorrect`, `AwardedPoints`, `Feedback`.

### 1.3 Repositories (`Services/Quiz/`)

- `IQuizRepository` + `InMemoryQuizRepository`: `AddAsync`, `GetByIdAsync`, `ListAsync(page, pageSize)`, `CountAsync`. ConcurrentDictionary keyed by quiz id, list ordered by `CreatedAt` desc.
- `IQuizAttemptRepository` + `InMemoryQuizAttemptRepository`: `AddAsync`, `GetByIdAsync`, `UpdateAsync`.

### 1.4 Retrieval reuse (refactor — do this first, it is shared)

- Add `RetrieveAsync(string query, int topK, CancellationToken)` returning `IReadOnlyList<RetrievedChunk>` to `IQueryService`.
- Move the embed-then-search body out of `AnswerAsync` into `RetrieveAsync`; have `AnswerAsync` call it. Keep `ResolveTopK` clamping. No behavior change to `AnswerAsync`.

### 1.5 Quiz generation tool schema

- Define a GLM function definition named `submit_quiz` whose JSON-Schema `parameters` describe an array of questions, each with `type` (enum multiple_choice/short_answer), `prompt`, `options` (array of strings), `correctOptionIndex` (int), `expectedAnswer` (string). Build a `GlmChatRequest` with `Tools` = this definition, `ToolChoice = "auto"`, a system prompt instructing the model to write grounded questions ONLY from the supplied context and to call `submit_quiz`, and a user prompt containing the retrieved chunk text plus the requested counts/types.
- Define a private typed model for parsing the tool arguments (e.g. `GeneratedQuiz` / `GeneratedQuestion` under `Services/Quiz/` or `Models/Quiz/`). Parse `response.Choices[0].Message.ToolCalls[0].Function.Arguments` (a JSON string) with camelCase options; map into `Quiz`/`QuizQuestion`. If no tool call is returned or parsing fails, throw `INTERNAL_ERROR`.

### 1.6 Service (`Services/Quiz/IQuizService` + `QuizService`)

- `GenerateAsync(GenerateQuizRequest request, CancellationToken)`:
  - Validate: topic or moduleId present; `questionCount` within 1..20 (clamp/validate, else `BAD_REQUEST`).
  - Retrieval query = topic (or module name/topic). Call `queryService.RetrieveAsync(query, topK)`. If zero chunks, throw `BAD_REQUEST` ("No indexed content to build a quiz from.").
  - Build the generation request (1.5), call `chat.ChatAsync`, parse to typed quiz, persist via `IQuizRepository`, return `Quiz`.
- `GetAsync(id)` — `NOT_FOUND` if missing.
- `ListAsync(page, pageSize)` — returns `PagedResult<Quiz>`.
- `StartAttemptAsync(quizId)` — load quiz (`NOT_FOUND`), create `QuizAttempt` with `Status=InProgress`, `MaxScore`=sum of question points, persist, return attempt.
- `SubmitAnswerAsync(attemptId, SubmitAnswerRequest)` — load attempt (`NOT_FOUND`); reject if already `Completed` (`BAD_REQUEST`); find the question (`BAD_REQUEST` if unknown id or already answered). Grade:
  - Multiple choice: exact match `SelectedOptionIndex == CorrectOptionIndex`. Award full or zero points. Feedback states correct option.
  - Short answer: call `GradeShortAnswerAsync` (1.7). Award points from the graded verdict.
  - Append `QuizAnswer`, add awarded points to `attempt.Score`, persist. If all questions answered, set `Status=Completed`, `CompletedAt`. Return the graded `QuizAnswer` plus running score (return shape: a small result object or the updated attempt; choose returning the graded `QuizAnswer` and let the controller also expose score — keep one return type: return the updated `QuizAttempt` so the UI gets live score and the latest answer). Decide: return `QuizAttempt`.
- `GetAttemptAsync(attemptId)` — `NOT_FOUND` if missing; result/score view.

### 1.7 Short-answer grading

- `GradeShortAnswerAsync(QuizQuestion question, string answerText, CancellationToken)` private: build a GLM request with a `grade_answer` function (parameters: `isCorrect` bool, `points` number, `feedback` string), system prompt = strict grader given the question, the `ExpectedAnswer`, and the learner's answer; parse tool args to a typed verdict. Fallback to incorrect-with-feedback on parse failure (do not throw; grading must not 500 a submission) — log nothing to console, surface neutral feedback.

### 1.8 Request models (`Models/RequestModels/Quiz/`)

- `GenerateQuizRequest` — `Topic` (string?), `ModuleId` (string?), `QuestionCount` (int, default 5), `TopK` (int, default from RagOptions), `IncludeShortAnswer` (bool, default true).
- `SubmitAnswerRequest` — `QuestionId` (string), `SelectedOptionIndex` (int?), `Text` (string?).

### 1.9 Controller (`Controllers/QuizController` — route `api/quizzes/`, name `quizzes`)

- POST `api/quizzes` → `GenerateAsync`. Returns `ApiResponse<Quiz>`.
- GET `api/quizzes?page=&pageSize=` → `ListAsync`. Returns `ApiResponse<PagedResult<Quiz>>`.
- GET `api/quizzes/{id}` → `GetAsync`. 200 / 404.
- POST `api/quizzes/{id}/attempts` → `StartAttemptAsync`. Returns `ApiResponse<QuizAttempt>`.
- POST `api/quizzes/attempts/{attemptId}/answers` → `SubmitAnswerAsync`. Returns `ApiResponse<QuizAttempt>`. 200 / 400 / 404.
- GET `api/quizzes/attempts/{attemptId}` → `GetAttemptAsync`. 200 / 404.

### 1.10 Tests (write first, RED → GREEN)

- `QueryServiceTests`: new test that `RetrieveAsync` returns chunks from the vector store and `AnswerAsync` still works (refactor safety).
- `QuizServiceTests` (Moq `IQueryService`, `IChatCompletionClient`, real in-memory repos):
  - Generate parses a stubbed `submit_quiz` tool call into a typed quiz with the right question count/types.
  - Generate with zero retrieved chunks throws `BAD_REQUEST`.
  - Generate with bad/missing tool call throws `INTERNAL_ERROR`.
  - StartAttempt sets `MaxScore` and `InProgress`.
  - SubmitAnswer multiple-choice correct/incorrect scoring (no LLM call for MC).
  - SubmitAnswer short-answer uses the grader stub and awards points.
  - SubmitAnswer marks attempt `Completed` after the last question; running `Score` correct.
  - Unknown attempt/question → `NOT_FOUND` / `BAD_REQUEST`.
- `QuizControllerTests` (via `RagWebApplicationFactory`, mock `IQuizService` replaced in the factory): generate returns 200 with quiz; submit returns 200 with updated attempt; service `BAD_REQUEST`/`NOT_FOUND` map to 400/404. (Extend the factory to expose a `Mock<IQuizService>` and `Replace` it, mirroring the existing `Query`/`Ingestion` pattern.)

---

## Feature 2 — Learning Plan Agent

### 2.1 Enum (`Common/Enums/`)

- `PlanTaskType` — `Read`, `Quiz`, `Chat`.

### 2.2 Domain models (`Models/Domain/`)

- `LearningPlan` — `Id`, `Goal`, `Summary` (string?), `Weeks` (`IReadOnlyList<PlanWeek>`), `CreatedAt`.
- `PlanWeek` — `WeekNumber` (int), `Theme` (string), `Tasks` (`IReadOnlyList<PlanTask>`).
- `PlanTask` — `Id`, `Type` (`PlanTaskType`), `Title`, `Description`, `ReferenceId` (nullable; moduleId for Read/Chat, topic/quizId for Quiz), `IsComplete` (bool), `DueAt` (nullable DateTime).

### 2.3 Repository (`Services/LearningPlans/`)

- `ILearningPlanRepository` + `InMemoryLearningPlanRepository`: `AddAsync`, `GetByIdAsync`, `UpdateAsync`, `ListAsync(page, pageSize)`, `CountAsync`. Also `ListDueTasksAsync(DateTime nowUtc)` returning flattened `(LearningPlan, PlanWeek, PlanTask)` tuples or a small `DuePlanTask` record for incomplete tasks whose `DueAt <= nowUtc` — used by the Telegram reminder dispatcher. Keep it on the repo so it stays testable.

### 2.4 Planning agent tool schema

- Define a GLM function `submit_learning_plan` whose parameters describe `summary` (string) and `weeks` (array), each week with `weekNumber` (int), `theme` (string), `tasks` (array), each task with `type` (enum read/quiz/chat), `title`, `description`, `referenceId` (string, optional). System prompt: act as a learning planner; build a multi-week plan ONLY from the listed available modules/documents; reference real module/document IDs in `referenceId`; call `submit_learning_plan`. User prompt: the participant goal plus a compact catalog of available modules (id, name, description) from `IModuleService.ListAsync` and indexed documents (id, fileName) from `IDocumentRepository`.
- Private typed parse models (`GeneratedPlan` etc.). Parse tool-call arguments → map to `LearningPlan`. Validate: at least one week; every task has a title and a valid type. Drop or null `referenceId`s that do not match a known module/document id (do not fail the whole plan for a bad reference; null it and continue). If no tool call / unparseable → `INTERNAL_ERROR`.

### 2.5 Service (`Services/LearningPlans/ILearningPlanService` + `LearningPlanService`)

- `GeneratePlanAsync(GeneratePlanRequest request, CancellationToken)`:
  - Validate goal non-blank (`BAD_REQUEST`).
  - Gather catalog: modules via `IModuleService.ListAsync`, documents via `IDocumentRepository.ListAsync(1, large)` (cap the catalog size).
  - Build agent request (2.4), `chat.ChatAsync`, parse + validate, optionally set `DueAt` per week (e.g. weekNumber offset from now) — keep simple: `DueAt = CreatedAt + (weekNumber-1)*7 days` so reminders have something to fire on. Persist, return `LearningPlan`.
- `GetAsync(id)` — `NOT_FOUND`.
- `ListAsync(page, pageSize)` — `PagedResult<LearningPlan>`.
- `CompleteTaskAsync(planId, taskId)` — load plan (`NOT_FOUND`), find task (`NOT_FOUND`), set `IsComplete=true`, persist, return updated plan. Because `PlanTask.IsComplete` must be mutable, model `PlanTask` with a settable `IsComplete`; weeks/tasks lists may be rebuilt immutably on update.

### 2.6 Request models (`Models/RequestModels/LearningPlans/`)

- `GeneratePlanRequest` — `Goal` (string), `WeekCount` (int?, default 4), `ModuleIds` (string[]? optional scoping).

### 2.7 Controller (`Controllers/LearningPlanController` — route `api/learning-plans/`, name `learning-plans`)

- POST `api/learning-plans` → `GeneratePlanAsync`. `ApiResponse<LearningPlan>`.
- GET `api/learning-plans?page=&pageSize=` → `ListAsync`. `ApiResponse<PagedResult<LearningPlan>>`.
- GET `api/learning-plans/{id}` → `GetAsync`. 200 / 404.
- POST `api/learning-plans/{planId}/tasks/{taskId}/complete` → `CompleteTaskAsync`. 200 / 404.

### 2.8 Tests (write first)

- `LearningPlanServiceTests` (mock `IChatCompletionClient`, `IModuleService`, `IDocumentRepository`, real in-memory repo):
  - Generate parses a stubbed `submit_learning_plan` tool call into a typed plan with weeks/tasks.
  - Generate nulls a `referenceId` that does not match any catalog id; keeps valid ones.
  - Blank goal → `BAD_REQUEST`; missing/garbled tool call → `INTERNAL_ERROR`.
  - CompleteTask flips `IsComplete`; unknown plan/task → `NOT_FOUND`.
  - `ListDueTasksAsync` returns only incomplete tasks with `DueAt <= now`.
- `LearningPlanControllerTests` (factory + mock service): 200 paths and 400/404 mapping; complete-task 200.

---

## Feature 3 — Telegram Bot

### 3.1 Package + options

- Add `Telegram.Bot` `22.10.0.1` to `AiIncubator.Server.csproj`.
- `TelegramOptions` (`Common/Helpers/Configurations/`, SectionName `Telegram`): `BotToken` (string, from `.env`/user-secrets), `Enabled` (bool, default false), `ReminderIntervalSeconds` (int, default 300). Register with `Configure<TelegramOptions>` in a new `TelegramServicesRegistration.AddTelegramServices`.

### 3.2 Chat-link repository (`Services/Telegram/`)

- `ITelegramChatLinkRepository` + `InMemoryTelegramChatLinkRepository`: map Telegram `chatId` ⇄ Clerk `userId`. Methods: `LinkAsync(chatId, userId)`, `GetUserIdAsync(chatId)`, `GetChatIdsForUserAsync(userId)`, `ListAsync`. ConcurrentDictionary keyed by chatId.

### 3.3 Sender (`Services/Telegram/ITelegramService` + `TelegramService`)

- Wrap `ITelegramBotClient`. `SendMessageAsync(long chatId, string text, CancellationToken)`. Keep thin; this is the seam unit tests mock.
- Register `ITelegramBotClient` as a singleton built from `TelegramOptions.BotToken` ONLY when `Enabled` and token non-empty; otherwise register a no-op `ITelegramService` so the app and tests run without a token.

### 3.4 Reminder dispatch

- `IReminderDispatcher` + `ReminderDispatcher`: `DispatchDueRemindersAsync(DateTime nowUtc, CancellationToken)` — query `ILearningPlanRepository.ListDueTasksAsync`, for each due task resolve the plan owner's linked chat ids and send a reminder via `ITelegramService`. Mark the task reminded (add a `LastRemindedAt` to `PlanTask` or track sent ids in-memory to avoid duplicate sends; simplest: an in-memory `HashSet` of `taskId` already reminded inside the dispatcher).
- `ReminderBackgroundService : BackgroundService` — loop every `ReminderIntervalSeconds`, call `IReminderDispatcher.DispatchDueRemindersAsync(DateTime.UtcNow, ct)`. Registered via `AddHostedService` ONLY when `TelegramOptions.Enabled`. The dispatcher (not the worker) holds the logic, so it is unit-tested without timers.

### 3.5 Interactive quiz over Telegram

- `ITelegramQuizConversation` + `TelegramQuizConversation`: drives a quiz in chat. State per chatId (in-memory): current `attemptId`, current question index. Commands: `/quiz <quizId>` starts an attempt (via `IQuizService.StartAttemptAsync`) and sends question 1 (MC questions sent with numbered options); a plain reply is mapped to a `SubmitAnswerRequest` (numbered option → `SelectedOptionIndex`, free text → `Text`) and graded via `IQuizService.SubmitAnswerAsync`; bot replies with correctness + running score, then sends the next question or a final summary.
- `ITelegramUpdateHandler` + `TelegramUpdateHandler`: routes incoming Telegram updates to link/quiz/help handlers. Keep routing logic free of the Telegram SDK transport so it is unit-testable (handler takes a minimal normalized update: chatId, text).
- Receiving updates: when `Enabled`, a hosted receiver (long polling via `Telegram.Bot` `ReceiverOptions`) forwards updates to `ITelegramUpdateHandler`. Guarded by `Enabled` so CI/tests never open a connection.

### 3.6 Config / secrets

- `.env.example`: add `Telegram__BotToken=` and a comment that the bot is enabled via `Telegram:Enabled=true` (config/user-secrets), token via `.env`/user-secrets only. Never put the token in `appsettings.json`.
- `appsettings.json`: add a `Telegram` section with `Enabled=false`, `ReminderIntervalSeconds=300` (no token).

### 3.7 Tests (write first)

- `ReminderDispatcherTests` (mock `ILearningPlanRepository`, `ITelegramChatLinkRepository`, `ITelegramService`): due task to a linked user sends exactly one message; no linked chat sends nothing; already-reminded task is not re-sent.
- `TelegramQuizConversationTests` (mock `IQuizService`, `ITelegramService`): `/quiz` starts an attempt and sends the first question; a numbered reply submits the right option index and the bot reports score; conversation ends with a summary after the last question.
- `TelegramUpdateHandlerTests`: routes `/quiz` to the quiz conversation, a link command to the link repo, unknown input to help.
- No live-network tests. The end-to-end live check is documented manual steps (see DoD).

### 3.8 `telegram:configure` skill

- After the bot code exists, the bot token / access policy is saved via the existing `telegram:configure` skill rather than by hand. Note this in the README Telegram section.

---

## Cross-cutting

### C.1 DI registration

- Quiz + Learning Plan services/repos: register in `RagServicesRegistration.AddRagServices` next to the existing registrations (singletons for repos, scoped for services), and `Configure` any new options. Add `IQuizService`/`ILearningPlanService` as scoped, repos as singletons.
- Telegram: new `TelegramServicesRegistration.AddTelegramServices(configuration)` called from `Program.cs` after `AddRagServices`. Registers `TelegramOptions`, chat-link repo, dispatcher, quiz conversation, update handler, sender, and (conditionally) `ITelegramBotClient` + hosted services. Background/receiver services added only when `Enabled`.

### C.2 Frontend

- `lib/types.ts`: add `QuestionType`, `AttemptStatus` string-unions; `Quiz`, `QuizQuestion`, `QuizAttempt`, `QuizAnswer`; `PlanTaskType`; `LearningPlan`, `PlanWeek`, `PlanTask`; extend `ApiClient` with quiz + plan methods.
- `lib/api.ts`: add `generateQuiz`, `listQuizzes`, `getQuiz`, `startAttempt`, `submitAnswer`, `getAttempt`, `generatePlan`, `listPlans`, `getPlan`, `completeTask` against the new endpoints, reusing the existing `request` helper and bearer-token attach.
- `lib/mock.ts`: implement the same methods with in-memory fixtures so mock mode keeps working offline (one sample quiz, deterministic MC grading, one sample plan).
- `pages/Quiz.tsx` + route `/app/quiz`: list/generate quizzes, run an attempt one question at a time, show live score and per-answer feedback. Reuse `Card`/`Button`/`Input`/`Spinner`.
- `pages/LearningPlan.tsx` + route `/app/plan`: generate a plan from a goal, render weeks → tasks with completion checkboxes and a progress indicator; call `completeTask`.
- `App.tsx`: add the two child routes under `/app`. `Sidebar.tsx`: add `Quiz` and `Learning Plan` nav links.

### C.3 Docs

- README: add the new endpoints to the API table (quizzes, learning-plans), add a short Telegram subsection (enable flag + token via `.env`/user-secrets + `telegram:configure` skill), add config rows (`Telegram:Enabled`, `Telegram:ReminderIntervalSeconds`) and the `Telegram__BotToken` env var. Update only the affected sections; do not regenerate the README.
- `diary/2026-06-08.md`: five sections (What was done, Problems encountered, Solutions, Next plan, Time spent).

---

## Build / execution order (TDD throughout: RED → GREEN → REFACTOR)

1. Refactor `IQueryService.RetrieveAsync` (test first), keep `AnswerAsync` green.
2. Quiz enums + domain models + repos (+ repo behavior tests).
3. `QuizService` generation/grading/attempts (tests first, mocked GLM + retrieval).
4. `QuizController` + request models (+ controller tests via factory).
5. Learning Plan enums + domain + repo (+ tests, incl. `ListDueTasksAsync`).
6. `LearningPlanService` agent (tests first), then `LearningPlanController`.
7. DI registration for features 1-2; run full `dotnet build` + `dotnet test`.
8. Telegram: add package, `TelegramOptions`, link repo, sender (+ no-op), dispatcher, quiz conversation, update handler (tests first for dispatcher/conversation/handler).
9. `TelegramServicesRegistration` + conditional hosted services; wire in `Program.cs`.
10. Frontend: types → api → mock → pages → routes/sidebar; `npm run build`.
11. Docs: README affected sections + diary entry.
12. Full validation pass (below).

---

## Validation / Definition of Done

- `dotnet build AiIncubator.slnx` passes.
- `dotnet test` passes, including all new Week-2 tests (Qdrant integration tests stay gated on `QDRANT_HOST`).
- `npm run build` passes for the client (type-check + bundle), mock mode still works with no backend.
- Unauthorized requests to the new `api/quizzes`, `api/learning-plans` endpoints return 401 (controllers carry `[Authorize]`; verified the same way as existing controllers).
- No secrets committed: `Telegram__BotToken` only in `.env`/user-secrets; `appsettings.json` has no token.
- Telegram end-to-end (manual, documented in the diary / README): with `Telegram:Enabled=true` and a real token, the bot (a) sends a reminder for a due plan task and (b) runs a full quiz round in chat (start → answer → graded → score). If live testing is unavailable in the environment, the documented manual steps plus the dispatcher/conversation unit tests stand in.

## Out of scope (later)

- Persistent database, payments, role split, production deploy/monitoring, webhook-mode Telegram (polling only here), quiz analytics, plan editing UI beyond complete-task.
