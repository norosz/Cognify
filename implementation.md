# Cognify — Current Implementation Plan (Code-First)

This is the **authoritative execution plan** for the next sprint.

Principle: **code is the source of truth**. This plan documents what we will implement next (wiring + bugfixes + small backend extensions) and what we will not.

Related docs:
- [status.md](status.md) — sprint board (what’s next / in progress / done)
- [worklog.md](worklog.md) — strict worklog entries (must be updated before each commit)
- [PROJECT_SPEC.md](PROJECT_SPEC.md) — legacy (to be rewritten later, code-first)
- [README.md](README.md) — legacy (to be rewritten later, code-first)

---

## 0) Working Agreement (how we ship)

For every feature/bugfix PR chunk:
1) Implement the change (frontend and/or backend)
2) Add/update tests when feasible (backend tests preferred; frontend tests only if the repo already has the pattern in that area)
3) Run the smallest relevant test scope (then expand if needed)
4) Update [worklog.md](worklog.md) (STRICT format)
5) Update [status.md](status.md) (move TODO → Done for the completed item)
6) Commit + push

---

## 1) Locked Decisions

### 1.1 Categories (Module + Quiz)

- Users can **view and edit** category for modules and quizzes.
- AI can **suggest** categories, but:
	- suggestions **never overwrite** the current category
	- category changes happen only when user accepts/applies a suggestion (or types their own) and calls `PUT .../category`
- A user can request **more suggestions** via a button (best UX).
- Suggest eligibility (UI and backend must match):
	- Module: allow AI suggest only if `noteCount + quizCount >= 1`
	- Quiz: allow AI suggest only if `questionCount >= 3`
- Category history is stored in **batches**:
	- every AI suggest call persists a new history batch (`source=AI`)
	- every apply action persists a new history batch (`source=Applied`, single item)
- Category history dropdown:
	- opens on input focus
	- scrollable
	- shows **only AI suggested + applied** entries
	- uses **soft dedupe display**: collapse identical labels in the dropdown (e.g., `Math (3)`) while still storing everything
- History endpoints:
	- `take` default: `10`
	- cursor pagination: **cursor is `batchId`** (stable)

### 1.2 Analytics includeExams toggle

- Analytics/review endpoints already support `includeExams` query param (default `false`).
- Frontend must expose a **user toggle**, persist it (localStorage), and pass it consistently to analytics + review queue requests.

### 1.3 Category statistics (combined)

- Add a **combined** “stats per category” API (modules + quizzes).
- Default visualization: by `practiceAttemptCount`.

### 1.4 UI layout decision

- On module detail page, **Quizzes section appears above Module Stats**.

### 1.5 Out of scope for this sprint

- Quiz timer and timer analytics (items 17–18 in the bug list) are deferred.

### 1.6 Final Exam v2 (module-scoped, note-less)

- Final exams are **module-scoped** and can be **note-less** (unlike practice quizzes).
	- Only final exams are allowed to have `NoteId = null`.
- Final exam questions must be generated from **user-selected module notes**.
	- A note is eligible for selection if it is a real user note (not a synthetic/system marker note).
	- New notes default to `IncludeInFinalExam = false`.
- If a user tries to regenerate/start a final exam with **zero selected notes**:
	- backend returns `400 ProblemDetails` with a stable code: `FinalExam.NoNotesSelected`
	- UI blocks with a friendly dialog offering:
		- “Include all notes” (bulk-select) + auto-retry regenerate
		- “Cancel”

### 1.7 Statistics v2 (single page with tabs)

- Statistics remains a single route/page with tabs:
	- **Practice** tab: learning/practice analytics
	- **Exams** tab: exam-only analytics
- Category breakdown v2 defaults to **group by Module Category**.
- Quiz-category filtering is supported via **multi-select**, but applies to **Practice only**.
- Exams inherit the module category and do not expose quiz-category filters.

### 1.8 Module creation category UX

- When creating a module, the user can optionally provide a category.
- UI must communicate:
	- Category is **optional**.
	- AI can generate a category.
- If the user does not provide a category, backend will generate a default category on create (from module title/description) so “Uncategorized” is not shown in normal flow.

---

## 2) Current Code Inventory (what exists today)

### 2.1 Existing backend endpoints we will wire/extend

Categories (existing):
- `POST /api/modules/{moduleId}/categories/suggest`
- `PUT /api/modules/{moduleId}/category`
- `POST /api/quizzes/{quizId}/categories/suggest`
- `PUT /api/quizzes/{quizId}/category`

Concepts (exists; currently unwired in UI):
- `GET /api/modules/{moduleId}/concepts`
- `POST /api/modules/{moduleId}/concepts/refresh`

Analytics (exists; currently unwired includeExams toggle in UI):
- `GET /api/learning-analytics/*?includeExams={bool}`
- `GET /api/knowledge-states/review-queue?includeExams={bool}`

### 2.2 Frontend key surfaces

- Dashboard: module cards + review queue + weakness quiz
- Module detail: module KPIs + tabs for documents/notes/quizzes
- Statistics page: analytics dashboards
- Notes detail page: content + sources list
- Quiz cards, quiz detail, quiz results/review

---

## 3) Epics (P0) — User Stories + Acceptance Criteria

### Epic A) Categories: view/edit + AI suggest + history (modules + quizzes)

User stories:
- As a user, I can view the current category for a module/quiz.
- As a user, I can set/change the category for a module/quiz at any time.
- As a user, I can request AI category suggestions once the module/quiz has enough content.
- As a user, I can see history of AI suggestions and applied categories.

Acceptance criteria:
- Suggest eligibility is identical in UI and backend:
	- module: `noteCount + quizCount >= 1`
	- quiz: `questionCount >= 3`
- `POST .../categories/suggest`:
	- returns a list of suggestions
	- persists an `AI` history batch + items
	- does not modify `CategoryLabel`
- `PUT .../category`:
	- sets the category
	- persists an `Applied` history batch with one item (the applied label)
- Category input UX:
	- on focus, opens a scrollable dropdown
	- shows a soft-deduped list of labels with counts
	- supports “Load more” (cursor paging) and “More suggestions”
	- shows AI icon for `AI` batches and check icon for `Applied`

---

### Epic B) Category history API contract (cursor = batchId)

Allowed `source` values:
- `AI` — sparkle/auto icon, label “AI suggestion”
- `Applied` — check/verified icon, label “Applied”

Endpoints:
- Module: `GET /api/modules/{moduleId}/categories/history?take=10&cursor={batchId?}`
- Quiz: `GET /api/quizzes/{quizId}/categories/history?take=10&cursor={batchId?}`

Ordering and cursor semantics:
- Sort by `createdAt DESC, batchId DESC`
- If `cursor` is provided, return batches strictly older than the cursor in this order

Response envelope:
```json
{
	"items": [
		{
			"batchId": "...",
			"createdAt": "2026-01-30T00:00:00Z",
			"source": "AI",
			"items": [
				{ "label": "Math", "confidence": 0.78, "rationale": "..." }
			]
		}
	],
	"nextCursor": "..." 
}
```

Validation:
- `take` default `10`, max `50`
- invalid `take` or invalid/not-owned `cursor` → `400 ProblemDetails`

---

### Epic C) includeExams user toggle (persisted)

User stories:
- As a user, I can toggle whether exams are included in analytics.
- As a user, I can toggle whether exams influence my review queue.

Acceptance criteria:
- Toggle is persisted (localStorage) and applied consistently.
- All analytics calls pass `includeExams`.
- Review queue call passes `includeExams`.

---

### Epic D) Category breakdown analytics (combined)

User stories:
- As a user, I can see activity and performance broken down by category.

P0 KPI payload per category:
- `categoryLabel` (empty/null bucketed to `Uncategorized`)
- `moduleCount`, `quizCount`
- Practice attempt KPIs: `practiceAttemptCount`, `practiceAverageScore`, `practiceBestScore`, `lastPracticeAttemptAt`
- Exam attempt KPIs: `examAttemptCount`, `examAverageScore`, `examBestScore`, `lastExamAttemptAt`

Acceptance criteria:
- One combined endpoint returns per-category rows.
- Statistics page shows a “Category Breakdown” card:
	- default metric: `practiceAttemptCount`
	- toggle metric: `practiceAverageScore`

---

### Epic E) Final Exam v2 (module-scoped, note-less, selected notes)

User stories:
- As a user, I can mark which notes are included in my final exam.
- As a user, I can regenerate the final exam and it uses only my selected notes.
- As a user, if I forgot to select notes, the app guides me to fix it quickly.

Acceptance criteria:
- Notes:
	- `IncludeInFinalExam` flag exists and defaults to `false`.
	- User can toggle it in:
		- module notes list
		- note detail page
- Final exam regeneration:
	- final exam quizzes can be created without a note (`NoteId = null`) and are module-owned.
	- question generation source is the set of selected notes for the module.
	- if no notes are selected, regenerate returns `400 ProblemDetails` with code `FinalExam.NoNotesSelected`.
	- UI shows a dialog with “Include all notes” (bulk select) and retries regenerate.

---

### Epic F) Statistics v2 (Practice/Exams tabs + category filters)

User stories:
- As a user, I can switch between practice analytics and exam analytics.
- As a user, I can see category breakdown grouped by module category.
- As a user, I can filter practice analytics by one or more quiz categories.

Acceptance criteria:
- Statistics page uses tabs:
	- Practice tab shows existing practice analytics surfaces.
	- Exams tab shows exam attempt analytics only.
- Category breakdown v2:
	- defaults to grouping by module category.
	- supports multi-select quiz-category filters (practice only).
	- exams ignore quiz-category filters and inherit module category.

---

### Epic G) Module creation category defaults

User stories:
- As a user, I can optionally provide a category when I create a module.
- As a user, if I skip category, the app still assigns a sensible category.

Acceptance criteria:
- Create module dialog includes an optional category input with helper text that it’s not mandatory.
- Backend creates modules with a populated category:
	- user-provided category wins
	- otherwise AI generates a default category from title/description

---

## 4) UI/UX Bugfix Sprint (P0/P1)

These items are treated as bugfixes/UX corrections.

### Module / Dashboard
1) Quizzes section is above Module Stats on module detail.
9) Regenerate exam button disabled unless module has content (notes or quizzes or docs).
16) Module cards show labeled counts:
	 - Documents: X
	 - Notes: Y
	 - Quizzes & Exams: Z
15) Review queue correctness: verify behavior and fix if needed.

### Notes page
2) Note content area is smaller and scrollable; pre-rendered images are thumbnail-sized; clicking opens a modal/large view.
3) Extracted notes are labeled as AI-generated (not “Your notes”).
4) Notes page supports “Generate quiz”.
5) Notes page supports deleting related documents.
6) Notes page supports “Extract content” (same as upload flow).

### Upload / Pending
7) Rename “Extract text” → “Extract content”.
14) Pending quizzes renamed to “Quizzes & Exams”.

### Quiz cards / routing / results
8) Quiz “View Details” back-navigation returns to the correct module (not dashboard).
10) Remove “View Details” button; clicking the card navigates to quiz detail.
11) Score color thresholds:
		- <50% red
		- 50–70% white
		- 70%+ green
12) Review UI is under the score and hidden by default (accordion/collapsible).
13) After retake, redirect user to quiz detail page.

### Additional UX polish (new)
19) Quiz detail Back returns to Module Detail → “Quizzes & Exams” tab.
20) Remove quiz accordions/collapsibles that hide key content (prefer flat sections).
21) Module detail quiz tab label is “Quizzes & Exams”.

### Deferred
17) Optional quiz timer.
18) Statistics for timer.

---

## 5) Implementation Tasks (Numbered)

### P0 — Categories + history + gating
1) Backend: add history tables (batches + items) and DbContext configuration.
2) Backend: persist `AI` history batch in module/quiz suggest endpoints.
3) Backend: persist `Applied` history batch on `PUT .../category`.
4) Backend: implement history endpoints with cursor pagination (`batchId`).
5) Backend: enforce eligibility:
	 - module: `noteCount + quizCount >= 1`
	 - quiz: `questionCount >= 3`
6) Backend: expose `questionCount` in quiz detail DTO so UI can gate without extra calls.

7) Frontend: add modules/quizzes service methods for suggest/apply/history.
8) Frontend: category input with on-focus dropdown + soft dedupe display + “Load more” + “More suggestions”.
9) Frontend: show category on module cards + module detail + quiz cards + quiz detail.

### P0 — includeExams toggle
10) Frontend: persisted `includeExams` toggle and pass it into analytics + review queue calls.

### P0 — Category breakdown analytics
11) Backend: add a combined `category-breakdown` analytics endpoint (modules + quizzes, KPIs above).
12) Frontend: Statistics page card for category breakdown.

### P0/P1 — Bugfix list
13) Apply the UI/UX bugfix sprint items (1–16), adding tests when feasible.

---

## 6) Tests (expectations)

Backend (preferred):
- Category history persistence + paging + validation tests
- Eligibility validation tests returning RFC7807
- Category breakdown endpoint tests

Frontend:
- Only add unit tests if the surrounding feature area already has tests; otherwise rely on integration/manual verification for UI-only changes.

