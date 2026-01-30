
# 🚀 Project Status Board — V2 Alpha

This board tracks the **V2 Alpha** refinement scope (quiz pages, stats, exams, notes UX, review queue clarity).
Authoritative implementation details live in [implementation.md](implementation.md).

---

## 🎯 V2 Alpha Scope (commitment)

- Quizzes have dedicated pages (detail/stats/history, results, review)
- Quiz submit shows loading + prevents double-submit
- Quiz cards show difficulty (color) + category (AI suggested, user override)
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
- Review queue rules:
	- low score (<60% default, configurable) OR repeated concept mistakes (>=2 default) OR due topics
- `includeExams` query param added to analytics endpoints (default false)

---

## 🧭 Decisions (locked)

- Separate `ExamAttempt` entity/table (not stored in `Attempt`).
- Final Exam is fixed until regenerate; retake creates additional `ExamAttempt` rows.
- `includeExams` param on analytics endpoints; practice stats exclude exams by default.
- Concept identification via embeddings + topic clustering per module; clusters labeled by AI.

---

## 🏗️ In Progress

- (none)

---

## ⏭️ Next (ordered)

1) **[Backend]** Add schema: `ExamAttempt`, `Module.CurrentFinalExamQuizId` + migration
2) **[Backend]** Implement exam endpoints + final exam regenerate/save pointer flow
3) **[Backend]** Add `includeExams` filtering to analytics + review queue endpoints
4) **[Frontend]** Add `/statistics` page; shrink dashboard to actions + summary
5) **[Frontend]** Add quiz pages: detail/stats, results, review (green/red + AI explain)
6) **[Frontend]** Refactor module page to card layout + module stats + exam section
7) **[Frontend]** Notes detail page: sources list + downloads + inline images + split inputs
8) **[Backend/Frontend]** Categories: AI suggest + user override for modules/quizzes
9) **[Backend]** Concept clustering pipeline (per module) + concept IDs stored

---

## 📋 To Do (V2 Alpha)

- [ ] **[UX]** Quiz submit loading indicator + disabled state
- [ ] **[UX]** Difficulty badge colors on quiz cards (consistent palette)
- [ ] **[Backend]** Module stats endpoint `/api/modules/{moduleId}/stats`
- [ ] **[Backend]** Quiz stats endpoint `/api/quizzes/{quizId}/stats`
- [ ] **[Backend]** Attempt review endpoint(s) for mistakes + explanations
- [ ] **[Frontend]** Quiz result page: retake + review CTA
- [ ] **[Frontend]** Review page: green/red borders + AI explain button
- [ ] **[Docs/Quality]** Add `ng build` quality gate

---

## ✅ Done (since V2 Alpha declaration)

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

---

## 🗂️ Done Archive (pre-V2 Alpha)

The previous status board contained a very large historical “Done” ledger (Jan 2026 stabilization + v2 pipeline buildout).
It has been intentionally removed here to keep the V2 Alpha board readable; use git history/blame to retrieve it.
