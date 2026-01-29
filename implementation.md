# Cognify Implementation Audit (README vs Code)

Scope: Evidence-based comparison of declared capabilities in the README/specs vs what exists in the codebase (backend, frontend, tests, config).

- Claimed capabilities source: [README.md](README.md), [PROJECT_SPEC.md](PROJECT_SPEC.md)
- Codebase scanned: Cognify.Server (ASP.NET Core), cognify.client (Angular), Cognify.Tests, Cognify.AppHost

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

### Frontend implemented flows
- Routes include dashboard/modules/pending/profile under an authenticated layout: [cognify.client/src/app/app.routes.ts](cognify.client/src/app/app.routes.ts#L1-L25)
- Auth is JWT-in-localStorage with optimistic user restore from token: [cognify.client/src/app/core/auth/auth.service.ts](cognify.client/src/app/core/auth/auth.service.ts#L1-L110)
- Requests automatically get `Authorization: Bearer <token>` via interceptor: [cognify.client/src/app/core/auth/auth.interceptor.ts](cognify.client/src/app/core/auth/auth.interceptor.ts#L1-L22)
- Route protection is a simple “token exists” guard (no `/me` validation on navigation): [cognify.client/src/app/core/auth/auth.guard.ts](cognify.client/src/app/core/auth/auth.guard.ts#L1-L17)

- Main layout provides sidebar nav + pending badge and refreshes pending count on load: [cognify.client/src/app/core/layout/main-layout/main-layout.component.ts](cognify.client/src/app/core/layout/main-layout/main-layout.component.ts#L1-L40), [cognify.client/src/app/core/layout/main-layout/main-layout.component.html](cognify.client/src/app/core/layout/main-layout/main-layout.component.html#L1-L55)
- Dashboard currently lists modules and opens create/edit dialog (no analytics/heatmaps): [cognify.client/src/app/features/dashboard/dashboard.component.ts](cognify.client/src/app/features/dashboard/dashboard.component.ts#L1-L90)

- Module detail view is tabbed (documents/notes/quizzes), and supports upload dialog: [cognify.client/src/app/features/modules/module-detail/module-detail.component.ts](cognify.client/src/app/features/modules/module-detail/module-detail.component.ts#L1-L125)
- Notes list supports create/edit/delete and initiates “pending quiz generation” from a note: [cognify.client/src/app/features/notes/components/notes-list/notes-list.component.ts](cognify.client/src/app/features/notes/components/notes-list/notes-list.component.ts#L1-L90)
- Note editor supports Markdown+LaTeX preview (with scroll sync), and can import extracted text from a selected document by calling AI extract: [cognify.client/src/app/features/notes/components/note-editor-dialog/note-editor-dialog.component.ts](cognify.client/src/app/features/notes/components/note-editor-dialog/note-editor-dialog.component.ts#L1-L175)
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

### OCR Agent (declared: PDF/Image, embedded image support)
- The current extraction endpoint accepts only image/* and rejects PDFs and other documents: [Cognify.Server/Controllers/AiController.cs](Cognify.Server/Controllers/AiController.cs#L35-L75)
- There is a content-type switch including .pdf, but the gate still rejects non-image: [Cognify.Server/Controllers/AiController.cs](Cognify.Server/Controllers/AiController.cs#L55-L75)

### “AI graded open-ended questions”
- There is an AI grading endpoint that returns free-form analysis: [Cognify.Server/Controllers/AiController.cs](Cognify.Server/Controllers/AiController.cs#L150-L190)
- However, quiz attempt scoring is deterministic string equality against stored correct answer JSON; no AI grading is invoked on attempt submission: [Cognify.Server/Services/AttemptService.cs](Cognify.Server/Services/AttemptService.cs#L40-L85)

### “Adaptive quiz engine” exists, but not based on knowledge state
- Generation prompt varies by requested difficulty and question type: [Cognify.Server/Services/AiPrompts.cs](Cognify.Server/Services/AiPrompts.cs#L1-L120)
- No evidence that user mastery/confidence/mistake patterns are loaded and fed into prompting.

### Duplicate/transitioning quiz generation UX
- Direct “generate then save” component exists: [cognify.client/src/app/features/modules/components/quiz-generation/quiz-generation.component.ts](cognify.client/src/app/features/modules/components/quiz-generation/quiz-generation.component.ts#L1-L140)
- Also a newer “generate as pending quiz and approve/save later” flow exists: [cognify.client/src/app/features/notes/components/notes-list/notes-list.component.ts](cognify.client/src/app/features/notes/components/notes-list/notes-list.component.ts#L30-L85)

## 3. Declared but Missing Features

These are asserted in the README/spec language as core capabilities, but have no corresponding persisted models/services/controllers in the codebase.

### Persistent User Knowledge Model
- Claimed: mastery score, confidence score, mistake patterns: [README.md](README.md#L7-L23)
- Evidence of absence: no DbSet/entity beyond quiz/doc/note/pending primitives: [Cognify.Server/Data/ApplicationDbContext.cs](Cognify.Server/Data/ApplicationDbContext.cs#L1-L20)

### Learning decay prediction + spaced repetition scheduling
- Claimed: decay prediction and automatic scheduling: [README.md](README.md#L29-L33)
- Evidence of absence:
  - No NextReview/ForgettingRisk fields/tables in the EF model surface (DbContext): [Cognify.Server/Data/ApplicationDbContext.cs](Cognify.Server/Data/ApplicationDbContext.cs#L1-L20)
  - No hosted scheduler/worker (only ad-hoc Task.Run for AI jobs; see Architecture section).

### AI Learning Dashboard (knowledge map, decay forecast, analytics, exam readiness)
- Claimed: dashboard with heatmaps/forecasts/analytics: [README.md](README.md#L34-L55)
- Evidence of current implementation: dashboard lists modules only: [cognify.client/src/app/features/dashboard/dashboard.component.ts](cognify.client/src/app/features/dashboard/dashboard.component.ts#L1-L80)

### Mistake intelligence / misconception profiling
- Claimed: mistake intelligence: [README.md](README.md#L67-L70)
- Evidence of absence: no persistence for mistake categories/patterns, and no services updating a knowledge profile on attempt submission.

## 4. Architecture Observations

### Aspire orchestration
- SQL Server + Azurite + API + Angular app are wired via .NET Aspire: [Cognify.AppHost/AppHost.cs](Cognify.AppHost/AppHost.cs#L1-L30)

### Layering
- Controllers are thin and delegate to services (typical pattern across controllers), and EF Core is used via ApplicationDbContext.

### Background work is non-durable fire-and-forget
- OCR extraction uses Task.Run with a new DI scope: [Cognify.Server/Controllers/AiController.cs](Cognify.Server/Controllers/AiController.cs#L80-L135)
- Pending quiz generation uses Task.Run and updates PendingQuiz status: [Cognify.Server/Services/PendingQuizService.cs](Cognify.Server/Services/PendingQuizService.cs#L35-L120)
- Implication: no retries/queue semantics; restarts may drop in-flight work.

## 5. AI Integration Status

### Configuration and wiring
- OpenAI client constructed from OpenAI:ApiKey configuration: [Cognify.Server/Program.cs](Cognify.Server/Program.cs#L30-L55)
- Aspire sets OpenAI:ApiKey from a secret parameter: [Cognify.AppHost/AppHost.cs](Cognify.AppHost/AppHost.cs#L15-L24)

### Implemented AI capabilities
- Question generation uses chat completion + JSON object response format parsing: [Cognify.Server/Services/AiService.cs](Cognify.Server/Services/AiService.cs#L20-L110)
- Handwriting transcription uses image input, Markdown + LaTeX rules: [Cognify.Server/Services/AiService.cs](Cognify.Server/Services/AiService.cs#L110-L175)
- Generic grading endpoint exists (returns formatted score/feedback text): [Cognify.Server/Services/AiService.cs](Cognify.Server/Services/AiService.cs#L175-L210)

## 6. Learning Model Status

Status: Missing as a persisted, evolving “cognitive model”.

- README claims: mastery/confidence/mistake patterns: [README.md](README.md#L7-L23)
- Attempt submission only computes and persists Attempt.Score and AnswersJson; no per-topic model updates: [Cognify.Server/Services/AttemptService.cs](Cognify.Server/Services/AttemptService.cs#L20-L120)
- No DB entity exists to store mastery/confidence/forgetting risk (DbContext inventory): [Cognify.Server/Data/ApplicationDbContext.cs](Cognify.Server/Data/ApplicationDbContext.cs#L1-L20)

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

Status: Missing.

- README claim: decay prediction + automatic review scheduling: [README.md](README.md#L29-L33)
- No “next review” fields/entities in the EF model surface: [Cognify.Server/Data/ApplicationDbContext.cs](Cognify.Server/Data/ApplicationDbContext.cs#L1-L20)
- No background scheduler/hosted worker; only ad-hoc Task.Run for AI jobs: [Cognify.Server/Controllers/AiController.cs](Cognify.Server/Controllers/AiController.cs#L80-L135)

---

Notes:
- This document intentionally avoids speculating about intent; conclusions are based only on code evidence and declared docs.
