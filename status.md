
# 🚀 Project Status Board — V2 Alpha

This board tracks the **V2 Alpha** refinement scope (wiring + UX bugfixes + small backend extensions).
Authoritative implementation details live in [implementation.md](implementation.md).

---

## 🎯 V2 Alpha Scope (commitment)

- Quizzes have dedicated pages (detail/stats/history, results, review)
- Quiz submit shows loading + prevents double-submit
- Quiz cards show difficulty (color) + category (AI suggested + user applied)
- Module page refactor with module-level stats and an Exams section
- Module Final Exam:
	- fixed until regenerate
	- tracked via separate `ExamAttempt`
	- current exam pointer: `Module.CurrentFinalExamQuizId`
- Statistics moved to its own page (`/statistics`)
- Notes:
	- show related uploaded + extracted documents (downloadable)
	- images render inline
	- split user vs AI note content (two inputs)
- Review queue behavior matches backend (knowledge-state due items + forgetting risk ordering)
- `includeExams` query param supported on analytics endpoints (default false) and will be wired to a persisted user toggle
- Categories (Module + Quiz):
	- AI suggest is gated by content (module: noteCount + quizCount >= 1; quiz: questionCount >= 3)
	- AI suggest never overwrites current category; user applies via `PUT .../category`
	- AI suggestions + applied categories are stored as history batches (scrollable dropdown on focus)
- Category breakdown analytics (combined modules + quizzes) added to Statistics page

---

## 🧭 Decisions (locked)

- Separate `ExamAttempt` entity/table (not stored in `Attempt`).
- Final Exam is fixed until regenerate; retake creates additional `ExamAttempt` rows.
- `includeExams` param on analytics endpoints; frontend will persist a user toggle and pass it consistently.
- Concept identification via embeddings + topic clustering per module; clusters labeled by AI.
- Module detail layout: Quizzes section appears above Module Stats.
- Category history pagination is cursor-based (cursor = batchId), take default = 10.

---

## ✅ Process (per feature)

Before each commit/push:
- Update [worklog.md](worklog.md) (STRICT format)
- Update [status.md](status.md) (move items across sections)
- Add/update tests if feasible

## 🏗️ In Progress

- (none)

---

## ⏭️ Next (ordered)

1) Categories history + gating (backend + frontend)
2) Persisted includeExams toggle wiring (analytics + review queue)
3) Combined category breakdown analytics (endpoint + Statistics card)
4) Bugfix sweep (module layout, quiz routing/UX, notes UX)

---

## 📋 To Do (V2 Alpha)

### Epic A — Categories (modules + quizzes)

- [ ] Backend: add category history tables (batches + items)
- [ ] Backend: persist `AI` history batch on `POST .../categories/suggest`
- [ ] Backend: persist `Applied` history batch on `PUT .../category`
- [ ] Backend: add history endpoints (cursor=batchId, take=10)
- [ ] Backend: enforce eligibility gating
	- module: `noteCount + quizCount >= 1`
	- quiz: `questionCount >= 3`
- [ ] Backend: expose `questionCount` on quiz detail DTO
- [ ] Frontend: wire module/quiz category suggest/apply/history services
- [ ] Frontend: category input opens scrollable history on focus (soft dedupe display)
- [ ] Frontend: show/edit categories on module cards + module detail + quiz cards + quiz detail

### Epic B — includeExams toggle

- [ ] Frontend: add persisted includeExams toggle (localStorage)
- [ ] Frontend: pass includeExams to all analytics calls + review queue

### Epic C — Category breakdown analytics

- [ ] Backend: add combined category breakdown endpoint (modules + quizzes)
- [ ] Frontend: add “Category Breakdown” card on Statistics page (default by attempts)

### Epic D — Bugfix sprint (selected)

- [x] Move Quizzes section above Module Stats (module detail)
- [ ] Quiz cards: click card navigates to quiz detail; remove “View Details” button
- [ ] Quiz routing: back from quiz detail returns to the correct module
- [ ] Result/review UX: score colors, review accordion hidden by default, retake redirects to quiz detail
- [ ] Notes UX: scrollable content, image thumbnails + modal, correct “AI generated” labeling
- [ ] Notes actions: generate quiz, delete documents, extract content
- [ ] Rename “Extract text” → “Extract content”
- [ ] Rename Pending to “Quizzes & Exams”

---

## ✅ Done (since V2 Alpha declaration)

- **[Docs]** Overwrite implementation plan to code-first sprint plan (categories history + category stats + bugfix checklist)
- **[Docs]** Replace audit-style docs with V2 Alpha plan (routes/endpoints/checklists)
- **[Backend]** Add schema: `ExamAttempt`, `Module.CurrentFinalExamQuizId` + migration
- **[Backend]** Implement exam endpoints + final exam regenerate/save pointer flow
- **[Backend]** Add `includeExams` filtering to analytics + review queue
- **[Backend]** Implement module stats and quiz stats endpoints
- **[Backend]** Implement attempt review endpoints with mistake details
- **[Backend]** Implement AI explanation endpoint for mistakes
- **[Backend]** Implement category suggestion + override endpoints
- **[Backend]** Implement note sources endpoint (related docs + extractions)
- **[Frontend]** Add quiz submit loading + disabled state
- **[Frontend]** Add difficulty badge colors on quiz cards
- **[Frontend]** Add module statistics section on module page
- **[Frontend]** Add quiz detail page with stats and attempt history
- **[Frontend]** Add attempt result and review pages with AI explanations
- **[Frontend]** Add statistics page and move analytics off dashboard
- **[Frontend]** Add final exam UI and exam attempt flows
- **[Frontend]** Notes detail page with sources, downloads, inline images, and split inputs
- **[Frontend]** Refactor module page to card layout
- **[Backend]** Concept clustering pipeline (per module) + concept IDs stored
- **[Docs/Quality]** Add `ng build` quality gate (removed per request)
- **[Tests]** Fix backend test build errors after constructor/signature changes
- **[Backend]** Fix duplicate CreateNoteDto properties build error
- **[Tests]** Add backend tests for concept clustering and note sources
- **[Tests]** Fix concept clustering and note source tests
- **[Backend/Tests]** Fix concept clustering tracking and note sources download URL
- **[Tests]** Stabilize concept clustering test seed data
- **[Backend]** Fix concept clustering tracking query for tests
- **[Backend]** Remove cascade delete on LearningInteraction -> ExamAttempt
- **[Frontend]** Add MatCardModule import for module detail view

---

## 🗂️ Done Archive (pre-V2 Alpha)

The previous status board contained a very large historical “Done” ledger (Jan 2026 stabilization + v2 pipeline buildout).
It has been intentionally removed here to keep the V2 Alpha board readable; use git history/blame to retrieve it.
