# Frontend Agent Rules (Angular)

These rules are **hard constraints** for agents working on the Angular frontend.

---

## 1) Project structure and patterns

### 1.1 Standalone-first (preferred)
- Prefer **standalone components** and `provide*` APIs unless the project is module-based already.
- Keep features isolated:
  - `src/app/features/<feature>/...`
  - `src/app/core/...` (singleton services, interceptors, guards)
  - `src/app/shared/...` (reusable UI components, pipes, directives)

### 1.2 Forbidden patterns
- Do not put business logic inside components beyond simple UI orchestration.
- Do not call `HttpClient` directly from components; use Services.
- Do not hardcode environment URLs in code.

---

## 2) API and environment rules

### 2.1 Base URL / Aspire integration
- Backend base URL must come from **configuration**, not hardcoded:
  - `environment.*.ts` or runtime config (e.g., `window.__env`)
- If the project uses Aspire, prefer:
  - **relative URLs via proxy** during dev, or
  - injected `API_BASE_URL` environment variable (whichever is already used).

### 2.2 HTTP patterns
- Use typed models and DTO-like interfaces for API responses.
- Centralize API calls in feature services:
  - `UsersApiService`, `EventsApiService`, etc.
- Use interceptors for:
  - auth tokens
  - global error handling
  - correlation/request id (if applicable)

### 2.3 Error handling UX
- No raw errors dumped to the UI.
- Provide user-friendly messages and fallback states.
- If a global error handler exists, reuse it.

---

## 3) State, forms, and performance

### 3.1 State
- Prefer simple state:
  - component signals/observables
  - feature-level services
- Avoid introducing heavy state libraries unless already present (NgRx, etc.).

### 3.2 Forms
- Use Reactive Forms for non-trivial forms.
- Validate on:
  - required fields
  - length/range
  - format (email, url, etc.)
- Display validation feedback consistently.

### 3.3 Performance
- Use `OnPush` change detection where appropriate.
- Use `trackBy` (or `@for` track) for large lists.
- Avoid nested subscriptions; use `switchMap`, `combineLatest`, `takeUntilDestroyed`, etc.

---

## 4) UI/UX and styling

### 4.1 Consistency
- Reuse existing components and styles before creating new ones.
- Keep spacing/typography consistent with the project’s design system (Material/Tailwind/custom).

### 4.2 Accessibility
- Every interactive element must be keyboard accessible.
- Provide labels for inputs (including `aria-*` where needed).
- Ensure color contrast is not broken by new changes.

---

## 5) Internationalization (if used)
- Do not introduce hardcoded user-facing strings if the project uses i18n.
- Use existing translation keys/namespaces and follow naming conventions.

---

## 6) Code quality requirements

### 6.1 Linting and formatting
- Code must pass existing lint rules.
- Avoid `any` unless unavoidable; document why.

### 6.2 RxJS / async correctness
- Always unsubscribe appropriately:
  - `takeUntilDestroyed()` preferred
  - `async` pipe preferred in templates
- Avoid side-effects in `map`; use `tap` where intended.

### 6.3 Security
- Never store tokens in places the project policy forbids.
- Sanitize untrusted HTML (use Angular sanitization APIs).
- Do not bypass CORS/proxy security patterns.

---

## 7) Deliverables checklist for any frontend change
Every frontend PR must include:
- ✅ Feature service changes (API calls)
- ✅ Component/UI updates
- ✅ Error handling behavior
- ✅ Unit tests (see TEST_RULES.md)
- ✅ Updated docs/comments if behavior changed

---

## 8) “Stop conditions” (agent must halt and report)
- The API contract is unclear (missing fields/status codes).
- The change would require a new state library (forbidden unless already present).
- You would need to hardcode environment URLs (forbidden): propose a compliant config solution.
