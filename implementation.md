# Cognify Implementation Audit (README vs Code)

Scope: Evidence-based comparison of declared capabilities in the README/specs vs what exists in the codebase (backend, frontend, tests, config).

- Claimed capabilities source: [README.md](README.md), [PROJECT_SPEC.md](PROJECT_SPEC.md)
- Codebase scanned: Cognify.Server (ASP.NET Core), cognify.client (Angular), Cognify.Tests, Cognify.AppHost

## 0. Current Known Issues (Jan 29, 2026)

This document previously focused on feature presence. The January 29 audit identified several **compile/runtime blockers** and **contract mismatches**. The frontend blockers and pending-status mismatches have now been addressed; remaining issues are listed below.

### Remaining known issues
- None from the Jan 29 blocker set. See [status.md](status.md) for open backlog items (mistake patterns UI, quality gates).

### Resolved in Jan 29 stabilization
- Dashboard template parsing errors and analytics binding drift resolved: [cognify.client/src/app/features/dashboard/dashboard.component.html](cognify.client/src/app/features/dashboard/dashboard.component.html)
- Note editor preview binding collision resolved: [cognify.client/src/app/features/notes/components/note-editor-dialog/note-editor-dialog.component.html](cognify.client/src/app/features/notes/components/note-editor-dialog/note-editor-dialog.component.html)
- TypeScript `rootDir` set and app template tags fixed: [cognify.client/tsconfig.json](cognify.client/tsconfig.json), [cognify.client/src/app/app.component.html](cognify.client/src/app/app.component.html)
- Pending status alignment + duplicate polling removed by centralizing in core PendingService: [cognify.client/src/app/core/services/pending.service.ts](cognify.client/src/app/core/services/pending.service.ts), [cognify.client/src/app/features/pending/pending.component.ts](cognify.client/src/app/features/pending/pending.component.ts)
- Error responses standardized to RFC7807 `ProblemDetails`: [Cognify.Server/Program.cs](Cognify.Server/Program.cs), [Cognify.Server/Controllers/AiController.cs](Cognify.Server/Controllers/AiController.cs), [Cognify.Server/Controllers/AdaptiveQuizzesController.cs](Cognify.Server/Controllers/AdaptiveQuizzesController.cs), [Cognify.Server/Controllers/LearningAnalyticsController.cs](Cognify.Server/Controllers/LearningAnalyticsController.cs)

## 1. Confirmed Implemented Features (with file evidence)

### Authentication (JWT)
- Register/login/me/change-password/update-profile endpoints: [Cognify.Server/Controllers/AuthController.cs](Cognify.Server/Controllers/AuthController.cs#L1-L120)
- JWT creation + claims: [Cognify.Server/Services/AuthService.cs](Cognify.Server/Services/AuthService.cs#L1-L120)
- JWT bearer auth configured: [Cognify.Server/Program.cs](Cognify.Server/Program.cs#L40-L80)

### Modules & Notes
- Modules CRUD endpoints: [Cognify.Server/Controllers/ModulesController.cs](Cognify.Server/Controllers/ModulesController.cs#L1-L75)
- Notes CRUD endpoints: [Cognify.Server/Controllers/NotesController.cs](Cognify.Server/Controllers/NotesController.cs#L1-L90)
- Note model includes QuestionSets navigation: [Cognify.Server/Models/Note.cs](Cognify.Server/Models/Note.cs#L1-L25)

### Documents + Blob Storage (upload/list/delete)
- Initiate upload / complete upload / list / delete: [Cognify.Server/Controllers/DocumentsController.cs](Cognify.Server/Controllers/DocumentsController.cs#L1-L120)
- SAS token generation + DB record lifecycle: [Cognify.Server/Services/DocumentService.cs](Cognify.Server/Services/DocumentService.cs#L1-L210)
- Aspire orchestrates SQL + Azurite + sets OpenAI key param: [Cognify.AppHost/AppHost.cs](Cognify.AppHost/AppHost.cs#L1-L30)
- Document upload integration tests: [Cognify.Tests/Controllers/DocumentsControllerTests.cs](Cognify.Tests/Controllers/DocumentsControllerTests.cs#L1-L200)

### Quiz System (persisted question sets + attempts)
- Question set CRUD API: [Cognify.Server/Controllers/QuestionSetsController.cs](Cognify.Server/Controllers/QuestionSetsController.cs#L1-L70)
- Persist QuestionSet + Question entities: [Cognify.Server/Services/QuestionService.cs](Cognify.Server/Services/QuestionService.cs#L1-L180)
- Attempt submission API: [Cognify.Server/Controllers/AttemptsController.cs](Cognify.Server/Controllers/AttemptsController.cs#L1-L50)
- Attempt scoring/persistence: [Cognify.Server/Services/AttemptService.cs](Cognify.Server/Services/AttemptService.cs#L1-L140)
- EF Core DbSets show persisted domain objects: [Cognify.Server/Data/ApplicationDbContext.cs](Cognify.Server/Data/ApplicationDbContext.cs#L1-L20)

### “Pending” workflow for long-running AI tasks
- Pending extracted contents + pending quizzes + count: [Cognify.Server/Controllers/PendingController.cs](Cognify.Server/Controllers/PendingController.cs#L1-L220)
- ExtractedContent persistence + save-as-note behavior: [Cognify.Server/Services/ExtractedContentService.cs](Cognify.Server/Services/ExtractedContentService.cs#L1-L170)
- PendingQuiz persistence + save-as-quiz behavior: [Cognify.Server/Services/PendingQuizService.cs](Cognify.Server/Services/PendingQuizService.cs#L1-L220)
- Pending items migration: [Cognify.Server/Migrations/20260127134639_AddPendingItemsEntities.cs](Cognify.Server/Migrations/20260127134639_AddPendingItemsEntities.cs#L1-L150)
- Frontend polling + toasts for state transitions: [cognify.client/src/app/core/services/pending.service.ts](cognify.client/src/app/core/services/pending.service.ts#L1-L220)

### Extraction pipeline (PDF/Image/Text/Office) via Pending + Background Worker
- Extraction is initiated as a pending job (Accepted/202) and processed asynchronously: [Cognify.Server/Controllers/AiController.cs](Cognify.Server/Controllers/AiController.cs#L30-L85)
- Background worker performs PDF text-layer extraction, embedded image extraction, OCR fallback, and basic Office/text extraction: [Cognify.Server/Services/AiBackgroundWorker.cs](Cognify.Server/Services/AiBackgroundWorker.cs#L55-L175)
- Extraction output is stored both as `ExtractedContent` (pending UX) and as a `MaterialExtraction` record for the v2 pipeline model: [Cognify.Server/Services/MaterialExtractionService.cs](Cognify.Server/Services/MaterialExtractionService.cs#L1-L45)

### Frontend implemented flows
- Routes include dashboard/modules/pending/profile under an authenticated layout: [cognify.client/src/app/app.routes.ts](cognify.client/src/app/app.routes.ts#L1-L25)
- Auth is JWT-in-localStorage with optimistic user restore from token: [cognify.client/src/app/core/auth/auth.service.ts](cognify.client/src/app/core/auth/auth.service.ts#L1-L110)
- Requests automatically get `Authorization: Bearer <token>` via interceptor: [cognify.client/src/app/core/auth/auth.interceptor.ts](cognify.client/src/app/core/auth/auth.interceptor.ts#L1-L22)
- Route protection uses a “token exists” guard: [cognify.client/src/app/core/auth/auth.guard.ts](cognify.client/src/app/core/auth/auth.guard.ts#L1-L17)
- Startup validates the token against the backend (`/auth/me`) via `provideAppInitializer`: [cognify.client/src/app/app.config.ts](cognify.client/src/app/app.config.ts#L1-L55)

- Main layout provides sidebar nav + pending badge and refreshes pending count on load: [cognify.client/src/app/core/layout/main-layout/main-layout.component.ts](cognify.client/src/app/core/layout/main-layout/main-layout.component.ts#L1-L40), [cognify.client/src/app/core/layout/main-layout/main-layout.component.html](cognify.client/src/app/core/layout/main-layout/main-layout.component.html#L1-L55)
- Dashboard includes modules + review/weakness actions and analytics visualizations (gauges, trends, heatmap, decay forecast): [cognify.client/src/app/features/dashboard/dashboard.component.html](cognify.client/src/app/features/dashboard/dashboard.component.html), [cognify.client/src/app/features/dashboard/dashboard.component.ts](cognify.client/src/app/features/dashboard/dashboard.component.ts)

- Module detail view is tabbed (documents/notes/quizzes), and supports upload dialog: [cognify.client/src/app/features/modules/module-detail/module-detail.component.ts](cognify.client/src/app/features/modules/module-detail/module-detail.component.ts#L1-L125)
- Notes list supports create/edit/delete and initiates “pending quiz generation” from a note: [cognify.client/src/app/features/notes/components/notes-list/notes-list.component.ts](cognify.client/src/app/features/notes/components/notes-list/notes-list.component.ts#L1-L90)
- Note editor implementation exists (Markdown+LaTeX preview, import extracted content), but the preview binding currently has a compile-time collision that must be fixed: [cognify.client/src/app/features/notes/components/note-editor-dialog/note-editor-dialog.component.html](cognify.client/src/app/features/notes/components/note-editor-dialog/note-editor-dialog.component.html), [cognify.client/src/app/features/notes/components/note-editor-dialog/note-editor-dialog.component.ts](cognify.client/src/app/features/notes/components/note-editor-dialog/note-editor-dialog.component.ts)
- Markdown+LaTeX rendering is implemented via `marked` + KaTeX in a pipe: [cognify.client/src/app/shared/pipes/markdown-latex.pipe.ts](cognify.client/src/app/shared/pipes/markdown-latex.pipe.ts#L1-L180)

- Documents list loads/deletes docs and can trigger extraction; extraction starts processing and pushes the user toward Pending: [cognify.client/src/app/features/modules/components/document-list/document-list.component.ts](cognify.client/src/app/features/modules/components/document-list/document-list.component.ts#L1-L170)
- Client-side SAS upload flow exists (initiate -> direct blob PUT -> complete). It bypasses interceptors for Azure Blob requests and manually sets auth headers for API calls: [cognify.client/src/app/features/modules/services/documents.service.ts](cognify.client/src/app/features/modules/services/documents.service.ts#L1-L150)
- Document selection dialog for importing note content filters to Uploaded docs: [cognify.client/src/app/features/modules/components/document-selection-dialog/document-selection-dialog.component.ts](cognify.client/src/app/features/modules/components/document-selection-dialog/document-selection-dialog.component.ts#L1-L70)

- Quiz list aggregates quizzes by note and opens the quiz-taking dialog; quiz-taking supports Ordering/Matching/MultipleSelect and submits attempts: [cognify.client/src/app/features/modules/components/quiz-list/quiz-list.component.ts](cognify.client/src/app/features/modules/components/quiz-list/quiz-list.component.ts#L1-L140), [cognify.client/src/app/features/modules/components/quiz-taking/quiz-taking.component.ts](cognify.client/src/app/features/modules/components/quiz-taking/quiz-taking.component.ts#L1-L260)
- Quiz API client supports create/get/list/delete question sets and submit attempts (there is also a `generateQuestions(noteId)` method pointing to `/api/ai/questions/from-note/{noteId}`, which appears legacy vs the current backend route): [cognify.client/src/app/features/modules/services/quiz.service.ts](cognify.client/src/app/features/modules/services/quiz.service.ts#L1-L55)

- Pending items polling/count + status-change notifications are implemented client-side: [cognify.client/src/app/core/services/pending.service.ts](cognify.client/src/app/core/services/pending.service.ts#L1-L230)
- Toast/notification system is implemented via a BehaviorSubject stream: [cognify.client/src/app/core/services/notification.service.ts](cognify.client/src/app/core/services/notification.service.ts#L1-L95)
- Profile UI supports update-profile + change-password: [cognify.client/src/app/features/profile/profile.component.ts](cognify.client/src/app/features/profile/profile.component.ts#L1-L120)

## 2. Partially Implemented Features

### Pending polling/notifications are implemented but duplicated
- A global pending poller exists, and the Pending page also polls and emits notifications, causing redundant requests/toasts.
- Evidence: [cognify.client/src/app/app.component.ts](cognify.client/src/app/app.component.ts), [cognify.client/src/app/features/pending/services/pending.service.ts](cognify.client/src/app/features/pending/services/pending.service.ts), [cognify.client/src/app/features/pending/pages/pending/pending.component.ts](cognify.client/src/app/features/pending/pages/pending/pending.component.ts)

### Embedded images are extracted but not fully integrated into Notes/UX
- PDF extraction uploads embedded images and stores metadata: [Cognify.Server/Services/AiBackgroundWorker.cs](Cognify.Server/Services/AiBackgroundWorker.cs#L409-L452)
- The extracted markdown currently appends an "Embedded Images" list (filenames/page numbers) rather than emitting resolvable `![...](...)` references: [Cognify.Server/Services/AiBackgroundWorker.cs](Cognify.Server/Services/AiBackgroundWorker.cs#L393-L407)
- `MaterialExtraction.ImagesJson` is persisted, and Notes can store `EmbeddedImagesJson`, but the frontend rendering path for these images is not clearly wired end-to-end.

### Pending failure status string is inconsistent between API and UI
- Backend uses enum names (e.g., `Failed`), while parts of the UI check for `Error`.
- Evidence: [Cognify.Server/Dtos/PendingQuizDtos.cs](Cognify.Server/Dtos/PendingQuizDtos.cs), [cognify.client/src/app/features/pending/pages/pending/pending.component.ts](cognify.client/src/app/features/pending/pages/pending/pending.component.ts)

### “AI graded open-ended questions”
- OpenText questions are graded via the grading agent during attempt submission: [Cognify.Server/Services/AttemptService.cs](Cognify.Server/Services/AttemptService.cs#L150-L260)
- Non-OpenText question types remain deterministic matching/scoring (by design).

### “Adaptive quiz engine” exists, but not based on knowledge state
- Adaptive quiz creation selects targets from review queue or weak states and maps mastery → difficulty: [Cognify.Server/Services/AdaptiveQuizService.cs](Cognify.Server/Services/AdaptiveQuizService.cs#L1-L170)
- Quiz generation includes a `KnowledgeStateSnapshot` string (adaptive context) when available: [Cognify.Server/Services/AiBackgroundWorker.cs](Cognify.Server/Services/AiBackgroundWorker.cs#L204-L242)
- Remaining gap: the mistake taxonomy is still coarse (see below), and “topic selection” is relatively simple (first eligible vs best-ranked).

### Mistake Intelligence is present but currently coarse
- Knowledge updates persist mistake patterns in JSON and record per-question `AnswerEvaluation` entities: [Cognify.Server/Services/KnowledgeStateService.cs](Cognify.Server/Services/KnowledgeStateService.cs#L1-L101)
- The mistake classifier currently emits only basic categories (`IncorrectAnswer`, `Unanswered`) unless provided by the grading agent: [Cognify.Server/Services/MistakeAnalysisService.cs](Cognify.Server/Services/MistakeAnalysisService.cs#L1-L72)

### Duplicate/transitioning quiz generation UX
- Direct “generate then save” component exists: [cognify.client/src/app/features/modules/components/quiz-generation/quiz-generation.component.ts](cognify.client/src/app/features/modules/components/quiz-generation/quiz-generation.component.ts#L1-L140)
- Also a newer “generate as pending quiz and approve/save later” flow exists: [cognify.client/src/app/features/notes/components/notes-list/notes-list.component.ts](cognify.client/src/app/features/notes/components/notes-list/notes-list.component.ts#L30-L85)

### Upload allow-list vs Extraction support mismatch (.json/.yaml)
- Upload allows `.json` and `.yaml`: [Cognify.Server/Services/DocumentService.cs](Cognify.Server/Services/DocumentService.cs#L21-L26)
- Extraction content-type detection does not map these extensions, so they become `application/octet-stream` and will fail extraction: [Cognify.Server/Services/AiBackgroundWorker.cs](Cognify.Server/Services/AiBackgroundWorker.cs#L156-L178)

### Spec drift (paths/contracts)
- Blob paths differ from the spec’s `materials/{userId}/{materialId}/...` convention (e.g. source docs use `{moduleId}/{documentId}_{fileName}`): [Cognify.Server/Services/DocumentService.cs](Cognify.Server/Services/DocumentService.cs#L56-L78)
- Extracted images are stored under `extracted/{documentId}/images/...` and are not currently emitted as resolvable markdown links: [Cognify.Server/Services/AiBackgroundWorker.cs](Cognify.Server/Services/AiBackgroundWorker.cs#L393-L452)
- Contract DTOs differ from the spec’s named fields (e.g. OCR contract is `ContentType/Language/Hints`): [Cognify.Server/Dtos/Ai/Contracts/OcrContract.cs](Cognify.Server/Dtos/Ai/Contracts/OcrContract.cs#L1-L14)

## 3. Previously flagged as “missing” — now confirmed implemented

The following items were previously documented as missing, but are present in the current codebase.

### Persistent User Knowledge Model
- Persisted entities exist (`UserKnowledgeState`, `LearningInteraction`, `AnswerEvaluation`) and are registered in EF Core: [Cognify.Server/Data/ApplicationDbContext.cs](Cognify.Server/Data/ApplicationDbContext.cs#L1-L22)
- Attempt submission updates the knowledge state and persists interactions/evaluations: [Cognify.Server/Services/AttemptService.cs](Cognify.Server/Services/AttemptService.cs#L60-L100), [Cognify.Server/Services/KnowledgeStateService.cs](Cognify.Server/Services/KnowledgeStateService.cs#L1-L101)
- Knowledge states + review queue endpoints exist: [Cognify.Server/Controllers/KnowledgeStatesController.cs](Cognify.Server/Controllers/KnowledgeStatesController.cs#L1-L35)

### Learning decay prediction + spaced repetition scheduling
- Forgetting risk + next review time are computed and stored: [Cognify.Server/Services/KnowledgeStateService.cs](Cognify.Server/Services/KnowledgeStateService.cs#L40-L75)
- Decay model implementation: [Cognify.Server/Services/DecayPredictionService.cs](Cognify.Server/Services/DecayPredictionService.cs#L1-L70)

### AI Learning Dashboard + Analytics
- Analytics endpoints exist (summary/trends/topics/heatmap/forecast): [Cognify.Server/Controllers/LearningAnalyticsController.cs](Cognify.Server/Controllers/LearningAnalyticsController.cs#L1-L90)
- Frontend dashboard consumes and visualizes these analytics: [cognify.client/src/app/features/dashboard/dashboard.component.ts](cognify.client/src/app/features/dashboard/dashboard.component.ts#L1-L160)

### Durable background workers (replacing ad-hoc Task.Run)
- AI extraction + quiz generation are processed via `BackgroundService` polling with `AgentRun` tracking: [Cognify.Server/Services/AiBackgroundWorker.cs](Cognify.Server/Services/AiBackgroundWorker.cs#L1-L220), [Cognify.Server/Models/AgentRun.cs](Cognify.Server/Models/AgentRun.cs#L1-L55)
- Analytics recomputation runs in a scheduled background worker: [Cognify.Server/Services/LearningAnalyticsBackgroundWorker.cs](Cognify.Server/Services/LearningAnalyticsBackgroundWorker.cs#L1-L120)

## 4. Architecture Observations

### Aspire orchestration
- SQL Server + Azurite + API + Angular app are wired via .NET Aspire: [Cognify.AppHost/AppHost.cs](Cognify.AppHost/AppHost.cs#L1-L30)

### Layering
- Controllers are thin and delegate to services (typical pattern across controllers), and EF Core is used via ApplicationDbContext.

### Background work is durable-at-rest but still “polling-based”
- Work is stored in the DB (PendingQuiz/ExtractedContent) and processed by hosted background workers: [Cognify.Server/Services/AiBackgroundWorker.cs](Cognify.Server/Services/AiBackgroundWorker.cs#L1-L220)
- Each run is tracked via `AgentRun` (status, input hash, output JSON): [Cognify.Server/Models/AgentRun.cs](Cognify.Server/Models/AgentRun.cs#L1-L55)
- Remaining limitation: this is not a real queue/broker; it’s DB polling, so throughput/retries are basic and “in-flight” guarantees depend on implementation details.

## 5. AI Integration Status

### Configuration and wiring
- OpenAI client constructed from OpenAI:ApiKey configuration: [Cognify.Server/Program.cs](Cognify.Server/Program.cs#L30-L55)
- Aspire sets OpenAI:ApiKey from a secret parameter: [Cognify.AppHost/AppHost.cs](Cognify.AppHost/AppHost.cs#L15-L24)

### Implemented AI capabilities
- Question generation uses chat completion + JSON object response format parsing: [Cognify.Server/Services/AiService.cs](Cognify.Server/Services/AiService.cs#L20-L110)
- Handwriting transcription uses image input, Markdown + LaTeX rules: [Cognify.Server/Services/AiService.cs](Cognify.Server/Services/AiService.cs#L110-L175)
- Generic grading endpoint exists (returns formatted score/feedback text): [Cognify.Server/Services/AiService.cs](Cognify.Server/Services/AiService.cs#L175-L210)

## 6. Learning Model Status

Status: Implemented (baseline) with clear room to deepen “mistake intelligence”.

- Knowledge state is persisted and updated on attempt submission: [Cognify.Server/Services/AttemptService.cs](Cognify.Server/Services/AttemptService.cs#L60-L100)
- Spaced repetition fields (`ForgettingRisk`, `NextReviewAt`) exist and are updated: [Cognify.Server/Models/UserKnowledgeState.cs](Cognify.Server/Models/UserKnowledgeState.cs#L1-L40)
- Per-question evaluations are stored (`AnswerEvaluation`): [Cognify.Server/Models/AnswerEvaluation.cs](Cognify.Server/Models/AnswerEvaluation.cs#L1-L32)

## 7. Quiz System Status

Status: Implemented for generation, persistence, taking, and deterministic scoring.

Backend:
- AI generation endpoint: [Cognify.Server/Controllers/AiController.cs](Cognify.Server/Controllers/AiController.cs#L110-L150)
- Persisted QuestionSets/Questions via QuestionService: [Cognify.Server/Services/QuestionService.cs](Cognify.Server/Services/QuestionService.cs#L1-L180)
- Attempt submission scoring is strict correct-answer string match: [Cognify.Server/Services/AttemptService.cs](Cognify.Server/Services/AttemptService.cs#L40-L85)

Frontend:
- Quiz taking supports multiple types and submits attempts: [cognify.client/src/app/features/modules/components/quiz-taking/quiz-taking.component.ts](cognify.client/src/app/features/modules/components/quiz-taking/quiz-taking.component.ts#L1-L250)

Tests:
- Attempt scoring tests: [Cognify.Tests/Services/AttemptServiceTests.cs](Cognify.Tests/Services/AttemptServiceTests.cs#L1-L170)
- Pending quiz conversion tests: [Cognify.Tests/Services/PendingQuizServiceTests.cs](Cognify.Tests/Services/PendingQuizServiceTests.cs#L1-L210)

## 8. Scheduler / Spaced Repetition Status

Status: Implemented at the data-model level (review queue + next review dates).

- Review queue endpoint exists: [Cognify.Server/Controllers/KnowledgeStatesController.cs](Cognify.Server/Controllers/KnowledgeStatesController.cs#L1-L35)
- Next review dates are computed and persisted: [Cognify.Server/Services/KnowledgeStateService.cs](Cognify.Server/Services/KnowledgeStateService.cs#L40-L75)

Remaining gap:
- There is no separate “review scheduler” that automatically creates quizzes or nudges users without user action; the current UX is “Review Queue + Generate Review Quiz” on demand.

---

Notes:
- This document intentionally avoids speculating about intent; conclusions are based only on code evidence and declared docs.
