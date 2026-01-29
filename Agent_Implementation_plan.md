# Agent Implementation Plan (AI)

Current goal: plan the AI roadmap + implementation sequencing for Cognify (no coding yet).

---

## AI Roadmap (phased, with missing scope + refactors)

### Phase 0 — “Make AI reliable” (hardening + refactor sprint)
Deliverables (no new “smartness” yet; make it production-grade-ish):

- Replace `Task.Run` with a durable background job model:
  - Create `AgentRun` + `BackgroundJob` (or a single `AgentRun`) table, store: status, timestamps, error, correlation id, model, prompt version, input hash, output blob/JSON, token usage.
  - Process via `BackgroundService` + DB polling/claiming OR an in-memory queue + DB persisted jobs.
- Consolidate and version AI contracts:
  - One canonical question schema (already close) + explicit DTOs for AI responses; contract tests.
- Fix API/UX drift:
  - Confirm frontend quiz-gen calls align with the current AI endpoint.
  - Fix prompt `difficultyLevel` bug in `AiPrompts.cs`.
- Improve error handling + telemetry:
  - All AI failures return stable `ProblemDetails`; log with correlation; never leak raw exceptions to clients.
- Normalize “pending” state modeling:
  - Convert string statuses like `"Processing" | "Ready" | "Error"` into enums for extracted content (currently string in `ExtractedContent.cs`).

Success criteria:
- Pending items survive restarts; retries are safe; contracts don’t break clients.

---

### Phase 1 — Knowledge Model MVP (the core differentiator)
Deliverables:

- Persist per-user learning state:
  - `UserKnowledgeState` (Topic, MasteryScore, ConfidenceScore, ForgettingRisk, NextReviewAt, LastReviewedAt, MistakePatternsJson/normalized table).
  - `LearningInteraction` (quiz answer events, time spent, difficulty, etc.).
  - `AnswerEvaluation` (score + feedback + detected mistakes).
- Update loop:
  - When an attempt is submitted (currently deterministic scoring in `AttemptService.cs`), also create interaction records and update knowledge state.
- Minimal spaced repetition:
  - Use a simple algorithm (e.g., SM-2-like scheduling) to compute NextReviewAt and ForgettingRisk.

Success criteria:
- System can answer: “What topics am I weak at?” + “What should I review next?”

---

### Phase 2 — Adaptive Quiz Engine (knowledge-state-driven generation)
Deliverables:

- `AdaptiveQuizService` (spec calls it out) that:
  - Selects topics/items based on mastery + forgetting risk + mistake patterns.
  - Builds prompts using: note content + recent mistakes + desired objective (recall vs application).
- “Deterministic where feasible” prompting:
  - Prompt versioning, stable JSON schema, seeded randomness via explicit “seed” parameter stored in DB.
- Optional:
  - Split “generation” from “rubric” (store rubric separately).

Success criteria:
- Two users with different states get meaningfully different quizzes from the same notes.

---

## Phase 2 – Detailed Execution Plan (concrete backlog)

Goal: make quiz generation *meaningfully adaptive* using the Phase 1 `UserKnowledgeState` + review queue.

### 2.1 Backend scope (minimum lovable)
- Build `AdaptiveQuizService` that selects focus targets from knowledge state:
  - **Review mode**: due items from `NextReviewAt <= now` (uses `GetReviewQueueAsync`).
  - **Weakness mode**: highest `ForgettingRisk` / lowest `MasteryScore` from `GetMyStatesAsync`.
- Convert selected targets to quiz inputs:
  - Use `SourceNoteId` when available (Phase 1 already stores it).
  - Fallback when `SourceNoteId` is null: generate from a chosen note/module context (defer if unclear).
- Compute *adaptive parameters*:
  - Difficulty mapping from mastery (e.g., <0.4 easy, 0.4–0.7 mixed, >0.7 hard).
  - Question mix: more retrieval for low mastery; more application/edge-cases for high mastery.
  - Mistake focus: summarize top keys from `MistakePatternsJson` into prompt guidance.

### 2.2 Backend API (proposed)
- `POST /api/adaptive-quizzes` to initiate an adaptive quiz request.
  - Request DTO includes: `mode` (Review|Weakness|Note), `maxTopics`, `questionCount`, optional `noteId`.
  - Response returns a `PendingQuizDto` (reuse pending approval workflow).
- Reuse `PendingQuizService` + background worker to actually generate questions.
  - Add a new generation path that builds prompts with knowledge-state context.
  - Ensure AgentRun correlation is written (already implemented in Phase 0).

### 2.3 Frontend scope
- Add a lightweight `KnowledgeStatesApiService`:
  - `GET /api/knowledge-states`
  - `GET /api/knowledge-states/review-queue`
- Dashboard UX:
  - “Review Queue” card (due topics) with **Generate Review Quiz** action.
  - “Weak Topics” mini list (top forgetting risk) with **Generate Weakness Quiz** action.
- Generation action should create a pending quiz and route user to `/pending?tab=quizzes` (consistent with existing UX).

### 2.4 Tests (must-have)
- Backend:
  - Unit tests for target selection (due items vs weakness items).
  - Unit tests verifying difficulty mapping and prompt includes knowledge summary.
- Frontend:
  - Service tests with `HttpTestingController`.
  - Component tests for the dashboard review queue states (loading/empty/populated).

### 2.5 Phase 2 acceptance criteria
- A user can see a review queue from real data (`/api/knowledge-states/review-queue`).
- Clicking “Generate Review Quiz” creates a pending quiz and eventually a saved quiz.
- Two users with different `MasteryScore/ForgettingRisk` see different difficulty/mix.
- No new schema changes required for Phase 2 (unless explicitly requested).

---

### Phase 3 — OCR/Extraction v2 pipeline (documents, not just images)
Deliverables:

- Expand extraction beyond handwriting images:
  - PDF text-layer extraction (PdfPig is already listed in spec); image extraction if needed.
- Adopt v2 model gradually:
  - Either introduce `Material`/`MaterialExtraction` as in spec, or evolve existing `Document` into “Material” without breaking UI.
- Idempotent extraction:
  - Same doc processed twice → same “job” updated, no duplicates.

Success criteria:
- Upload PDF → extracted note draft + embedded images metadata (if supported).

---

### Phase 4 — Learning Analytics + Dashboard (the “wow” UI)
Deliverables:

- `LearningAnalyticsService` to compute:
  - retention heatmap, trends, topic distribution, learning velocity, exam readiness score (as in spec).
- Frontend dashboard:
  - knowledge map visualization, decay forecast chart, weakness list (aligns with `status.md`).

Success criteria:
- Dashboard shows actionable insights, not just module lists.

---

## Concrete Implementation Plan (what to build, in what order)

### Backend (services + data + endpoints)

#### Background jobs foundation (Phase 0)
- Add entities: `AgentRun` (and optionally `BackgroundJobLock`).
- Add services:
  - `IAgentRunService` (create/run/update)
  - `IAiOrchestrator` (build prompts, call AI, validate JSON, persist outputs)
- Add HostedService worker that:
  - claims pending jobs
  - executes with retries + backoff
  - marks final status

#### Unify AI endpoints
- Keep controllers thin:
  - `/api/ai/extractions` start job
  - `/api/ai/quizzes` start job
  - `/api/pending/...` continues to surface “work-in-progress”
- Remove/avoid new endpoints that return raw AI text; always return structured DTOs.

#### Knowledge model (Phase 1)
- Add entities + DbSets (new migration later when requested).
- Create `KnowledgeStateService`:
  - `ApplyAttemptResult(attempt, questionSet, answers, timings?)`
  - updates mastery/confidence and schedules reviews.

#### Adaptive quiz (Phase 2)
- `AdaptiveQuizService`:
  - fetch knowledge state + content
  - generate “quiz plan” (topics + question types + difficulty)
  - dispatch AI job to produce structured questions.

#### OCR v2 (Phase 3)
- Add PDF extraction service:
  - text layer extraction first (fast win)
  - image extraction later
- Store extracted markdown + blocks metadata.

---

### Frontend (minimal disruption, build toward v2)
- Consolidate quiz generation UI:
  - Prefer the pending-based flow you already have via `pending.service.ts`.
  - Remove/retire the legacy `generateQuestions(noteId)` call in `quiz.service.ts` or re-point it to the canonical endpoint.
- Add “My Review Queue” UI:
  - uses NextReviewAt and forgetting risk once Phase 1 lands.
- Add analytics dashboard last (Phase 4) once data exists.

---

## Testing strategy (aligned with TEST_RULES)

- Service-layer unit tests:
  - Knowledge update math (mastery delta, scheduling).
  - Job claiming/retry behavior (no flakiness, no real network).
- Integration tests:
  - End-to-end: create pending quiz → worker completes → save as quiz.
- Contract tests:
  - Validate AI JSON shape parsing (already fragile area in `AiService.cs`).

---

## Refactoring Backlog (high-impact “make it good”)

- Replace `Task.Run` background work with durable worker: `AiController.cs`, `PendingQuizService.cs`.
- Fix question schema prompt bug (`difficultyLevel`): `AiPrompts.cs`.
- Resolve quiz-gen endpoint mismatch: `quiz.service.ts` vs `AiController.cs`.
- Replace status strings with enums for extracted content: `ExtractedContent.cs`.
- Decide on “v2 schema migration” approach (big-bang vs incremental) to reconcile current entities with spec’s `Material`/`LearningInteraction`/etc.
