# Test Rules (Backend + Frontend)

These are **hard constraints** for writing and maintaining tests. Agents must follow existing test frameworks already in the repo; the rules below define defaults when choice is open.

---

## 1) General principles

- Tests must be **deterministic** (no reliance on real time, external networks, or shared state).
- Prefer **fast unit tests**; add integration tests only for critical flows.
- Every bug fix must include a regression test.
- Tests must read like documentation:
  - Arrange / Act / Assert
  - clear naming
  - minimal noise

---

## 2) Backend tests (.NET)

### 2.1 Framework defaults
- Prefer `xUnit` (or the existing framework in the repository).
- Use `FluentAssertions` if already present; otherwise plain asserts are fine.

### 2.2 What to test
- **Service layer** is the primary unit-test target:
  - business rules
  - authorization decisions (where feasible)
  - validation boundaries
  - error translation (conflict/not found)
- Controller tests are optional unless routing/serialization is complex.

### 2.3 Integration tests
- Use `WebApplicationFactory` for API-level integration tests when needed.
- Use a test database strategy consistent with the repo:
  - SQLite in-memory, Testcontainers, or local ephemeral DB
- Do not require developer machine prerequisites that aren’t documented.

### 2.4 Mocks and fakes
- Mock external services (email, blob storage, message bus).
- Prefer fakes over heavy mocks when behavior matters.
- Do not mock `DbContext` for EF Core; prefer:
  - SQLite in-memory
  - real provider with in-memory DB

### 2.5 Naming convention
- `MethodName_StateUnderTest_ExpectedBehavior`
  - e.g., `CreateUser_WhenEmailExists_ReturnsConflict`

---

## 3) Frontend tests (Angular)

### 3.1 Framework defaults
- Use the repo’s existing stack:
  - Karma/Jasmine or Jest
- Use Angular Testing Library if already present; otherwise default Angular TestBed.

### 3.2 What to test
- **Services**:
  - API calls (HttpTestingController)
  - mapping of DTOs
  - error handling branches
- **Components**:
  - rendering states (loading/empty/error)
  - form validation
  - user interactions
- Avoid snapshot-only testing unless already used and justified.

### 3.3 Mocking
- Mock HTTP via Angular testing utilities.
- Mock timers using fakeAsync/tick (or Jest fake timers), never real delays.

---

## 4) End-to-end tests (optional)
- Only add E2E (Playwright/Cypress) if explicitly requested or the repo already has it.
- E2E must run headless and be stable in CI.
- Avoid brittle selectors; prefer `data-testid`.

---

## 5) CI expectations
- Tests must be runnable via one command:
  - Backend: `dotnet test`
  - Frontend: `npm test` (or the repo’s equivalent)
- No interactive prompts.
- Keep execution time reasonable; split slow suites if needed.

---

## 6) Deliverables checklist for test changes
Every test PR must include:
- ✅ New/updated tests
- ✅ Clear test naming
- ✅ No flaky behavior
- ✅ Notes if any special setup is required

---

## 7) “Stop conditions” (agent must halt and report)
- The only way to test would require real external services (forbidden): propose mocks/fakes.
- The repo lacks any test harness; propose the minimal setup consistent with current tooling.
