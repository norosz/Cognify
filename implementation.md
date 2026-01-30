# Cognify V2 Alpha Implementation Plan

This document is the **authoritative working plan** for V2 Alpha.

It defines:
- Frontend routes/pages we will build/refactor
- Backend endpoints (existing + new/extended)
- DTO/model additions (including separate `ExamAttempt`)
- Feature-by-feature checklist with acceptance criteria

Reference docs:
- Vision/spec: [PROJECT_SPEC.md](PROJECT_SPEC.md)
- Current status board: [status.md](status.md)

---

## 0) V2 Alpha Scope (what “done” means)

### Must-have UX/Flows
1) Quiz submit shows a loading state (disable actions, show progress UI)
2) Quiz difficulty shown on cards (color-coded badge) + immediate UX feedback
3) Quiz statistics visible (attempt count, best/avg score, progress/trend)
4) Module page refactor: card-based layout + module statistics section
5) Quizzes have separate pages (detail + stats + attempt history)
6) Quiz result/review pages show mistakes (green/red), retake, and AI explanations
7) Module-based exams (Final Exam) aggregate all quizzes in the module
8) Dashboard is smaller; analytics/statistics have their own page
9) Review Queue rules are explicit and predictable
10) Categories for modules/quizzes: AI suggests, user overrides
11) Users can see mistakes they made (by question + by concept)
12) AI explanation for mistakes (on demand, per question/answer)

### Notes + Documents
5b) Notes page shows related uploaded + extracted documents, downloadable
5c) Separate user note and AI-generated note inside a note (two inputs)
5d) Images are pre-rendered inline (not download-only)

---

## 1) Key Decisions (locked)

- **Exam attempts are separate:** introduce `ExamAttempt` table (do not reuse `Attempt`).
- **Final Exam is fixed until regenerate:** user retakes the same exam until explicitly regenerated.
- **Final Exam “current” pointer:** `Module.CurrentFinalExamQuizId` (nullable FK) tracks the current exam quiz.
- **Analytics can include exams:** analytics endpoints support `includeExams=true|false` (default `false`).
- **Review Queue rule:** a quiz/topic enters review when:
	- Low score on attempt (configurable, default `< 60%`) OR
	- Repeated mistakes on the same concept (threshold default `>= 2`), OR
	- Spaced repetition due time (existing knowledge-state mechanism).
- **Concept identification:** embeddings + topic clustering per module; clusters get AI labels; concept IDs are stable.

---

## 2) Frontend Routes (V2 Alpha)

Existing (keep):
- `/dashboard`
- `/modules`
- `/modules/:moduleId`
- `/pending`
- `/profile`

Add/Refactor (V2 Alpha):
- `/statistics` — analytics page (move large dashboard analytics here)
- `/quizzes/:quizId` — quiz detail + stats + attempt history
- `/quizzes/:quizId/attempts/:attemptId/results` — practice attempt result page
- `/quizzes/:quizId/attempts/:attemptId/review` — practice attempt review page (green/red + AI explanations)

Module Final Exam:
- `/modules/:moduleId/final-exam` — show current exam + regenerate + start
- `/modules/:moduleId/exam-attempts/:examAttemptId/results` — exam result page
- `/modules/:moduleId/exam-attempts/:examAttemptId/review` — exam review page

Notes:
- `/notes/:noteId` — note detail page (two inputs + related docs + inline images)

---

## 3) Backend API Surface (V2 Alpha)

### 3.1 Conventions
- All errors must use RFC7807 `ProblemDetails`.
- Default analytics behavior excludes exams unless `includeExams=true`.
- All new endpoints should be authenticated and owner-scoped.

### 3.2 Existing Endpoints (reference)

Auth:
- `POST /auth/register`
- `POST /auth/login`
- `GET /auth/me`
- `POST /auth/change-password`
- `PUT /auth/update-profile`

Modules/Notes/Documents:
- Modules CRUD under `/api/modules/...`
- Notes CRUD under `/api/notes/...`
- Documents: initiate upload / complete upload / list / delete under `/api/documents/...`

Pending:
- `GET /api/pending/count`
- `GET /api/pending/extracted-contents`
- `POST /api/pending/extracted-contents/{id}/save-as-note`
- `GET /api/pending/quizzes`
- `POST /api/pending/quizzes/{id}/save-as-quiz`

Quizzes (QuestionSets) & Attempts:
- QuestionSets under `/api/questionsets/...`
- Attempts submit under `/api/attempts` and attempt list per quiz under `/api/quizzes/{quizId}/attempts/me`

Analytics & knowledge:
- Analytics under `/api/learningAnalytics/...`
- Review queue under `/api/knowledgeStates/review-queue`

### 3.3 New/Extended Endpoints (V2 Alpha)

#### Module Stats
- `GET /api/modules/{moduleId}/stats`
	- Returns module-level KPIs:
		- practice attempts (count, avg, best)
		- progress/momentum trend
		- weak concepts (top N)
		- optional exam summary (separate section)

#### Quiz Stats
- `GET /api/quizzes/{quizId}/stats`
	- Returns quiz-level KPIs:
		- attempt count, best/avg, last attempt
		- per-question correctness and per-concept breakdown

#### Quiz Attempt Review (practice)
- `GET /api/quizzes/{quizId}/attempts/{attemptId}`
- `GET /api/quizzes/{quizId}/attempts/{attemptId}/review`
	- Returns per-question review payload:
		- user answer, correct answer, correctness
		- concept id/label
		- mistake categories
		- optional cached AI explanation

#### Notes: related sources + downloads + images
- `GET /api/notes/{noteId}/sources`
	- Returns:
		- related uploaded documents
		- related extracted contents
		- image URLs suitable for inline rendering (SAS)

#### Categories (AI suggest + user override)
- `POST /api/modules/{moduleId}/categories/suggest`
- `PUT /api/modules/{moduleId}/category`
- `POST /api/quizzes/{quizId}/categories/suggest`
- `PUT /api/quizzes/{quizId}/category`

#### AI: Explain mistakes (on demand)
- `POST /api/ai/explain-mistake`
	- Input: question prompt/context, user answer, correct answer, concept label, optional note/module context
	- Output: explanation markdown + key takeaways

#### Review policy (configurable)
- `GET /api/knowledgeStates/review-policy`
- `PUT /api/knowledgeStates/review-policy`
	- Defaults:
		- `lowScoreThresholdPercent = 60`
		- `repeatMistakeThreshold = 2`

#### Module Final Exam (fixed until regenerate)

Final exam is module-scoped and stored as a normal quiz (QuestionSet) attached to a special note, plus an explicit pointer:
- `Module.CurrentFinalExamQuizId` is the current exam quiz.

Endpoints:
- `GET /api/modules/{moduleId}/final-exam`
	- Returns the current final exam quiz (or null if none), plus metadata (last generated, source coverage).
- `POST /api/modules/{moduleId}/final-exam/regenerate`
	- Triggers generation via Pending (returns `pendingQuizId`).
- `POST /api/modules/{moduleId}/final-exam/pending/{pendingQuizId}/save`
	- Saves pending quiz as a real quiz AND sets `CurrentFinalExamQuizId`.

Exam attempts:
- `POST /api/modules/{moduleId}/exam-attempts`
- `GET /api/modules/{moduleId}/exam-attempts/me`
- `GET /api/modules/{moduleId}/exam-attempts/{examAttemptId}`
- `GET /api/modules/{moduleId}/exam-attempts/{examAttemptId}/review`

#### Analytics includeExams
All analytics endpoints support `includeExams` query param (default false):
- `GET /api/learningAnalytics/summary?includeExams=false`
- `GET /api/learningAnalytics/trends?includeExams=false`
- `GET /api/learningAnalytics/topics?includeExams=false`
- `GET /api/learningAnalytics/heatmap?includeExams=false`
- `GET /api/learningAnalytics/forecast?includeExams=false`
- `GET /api/knowledgeStates/review-queue?includeExams=false`

---

## 4) Data Model Additions (V2 Alpha)

### 4.1 `ExamAttempt` (new)
- Stored separately from practice `Attempt`
- Fields (minimum):
	- `Id`, `UserId`, `ModuleId`, `QuizId` (the current final exam quiz), `SubmittedAt`, `ScorePercent`, `TimeSpentSeconds`, `AnswersJson`

### 4.2 `Module.CurrentFinalExamQuizId` (new)
- Nullable FK to Quiz (QuestionSet)
- Represents “current final exam” and keeps it fixed until regenerate/save.

### 4.3 Concepts & clustering (new/extended)
- Introduce a stable concept ID per question (and optionally per interaction):
	- `ConceptId`, `ConceptLabel`
- Derived by embedding + clustering within a module.

### 4.4 Categories (new/extended)
- Store final category label for module/quiz:
	- `CategoryLabel`, `CategorySource` (`User` / `AI`)

---

## 5) Feature Checklist (V2 Alpha)

### 5.1 Quiz submission UX
- [ ] Loading state when submitting a quiz attempt (disable submit + show spinner/progress)
- [ ] Prevent double-submit (idempotent UX)
- [ ] Show toast + route to Results page on success

Acceptance:
- Submitting shows loading within 100ms and blocks additional clicks
- After response, user lands on result page with score

### 5.2 Quiz cards: difficulty + category
- [ ] Difficulty badge on quiz card (color-coded)
- [ ] Category chip (AI suggestion visible; user override saved)

Acceptance:
- Quiz list shows difficulty + category at a glance

### 5.3 Module page refactor + module statistics
- [ ] Module page uses card layout for quizzes and key stats
- [ ] Module stats section shows progress, attempts, weak concepts
- [ ] Separate “Exams” section with final exam status

### 5.4 Quiz pages (separate pages)
- [ ] Quiz detail page shows metadata + quiz-level stats
- [ ] Quiz attempt history list (practice)

### 5.5 Result + review pages
- [ ] Result page: retake + review
- [ ] Review page: per-question (green good / red mistake)
- [ ] Mistake list + AI explanation per mistake (on demand)

### 5.6 Review Queue semantics
- [ ] Review queue includes:
	- due topics (existing spaced repetition)
	- low-score attempts
	- repeated concept mistakes
- [ ] Policy is configurable (60% / 2 mistakes defaults)

### 5.7 Notes: sources + split content + images
- [ ] Note detail page shows:
	- user note input
	- AI note content input
	- related docs (uploaded + extracted), downloadable
	- inline images (pre-render)

### 5.8 Statistics page
- [ ] Move dashboard analytics to `/statistics`
- [ ] Dashboard becomes summary + actions, not a “big stats wall”

### 5.9 Module Final Exam (fixed until regenerate)
- [ ] Module final exam page shows current exam and attempt history
- [ ] “Regenerate exam” creates new pending quiz then save promotes it
- [ ] Exam attempts stored as `ExamAttempt` (separate)

---

## 6) Risks / Non-goals (V2 Alpha)

Non-goals for V2 Alpha (explicitly defer if time is tight):
- Fully automated scheduling (background-generated quizzes without user action)
- Advanced misconception taxonomy UI beyond concept clustering + mistake categories
- Multi-user sharing/collaboration

---

## 7) Archive

The previous `implementation.md` was an evidence-based “Implementation Audit (README vs Code)”.
If you need the historical audit text, use git history/blame to retrieve prior versions.
