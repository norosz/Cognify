# Backend Agent Rules (.NET / ASP.NET Core)

These rules are **hard constraints** for agents working on the backend. If a task conflicts with any rule below, the agent must propose an alternative that complies.

---

## 1) Architecture and boundaries

### 1.1 Layering (required)
- **Controllers**: HTTP-only concerns (routing, status codes, request/response shaping).
- **Services**: business logic, orchestration, authorization decisions, and transactions.
- **Data access**: performed **only inside Services** through `DbContext` (or abstractions explicitly provided by the project).
- **DTOs**: API inputs/outputs must be DTOs, not EF entities.

### 1.2 Forbidden patterns
- **Do not introduce Repository pattern or Unit of Work pattern** (no `IRepository<>`, no `IUnitOfWork`, no generic repo wrappers).
- **Do not place `DbContext` usage in Controllers.**
- **Do not expose EF entities directly** in controllers.
- **Do not add new "God" services** (huge services that hold unrelated responsibilities).

### 1.3 Folder / naming conventions (default)
- `Controllers/`
- `Services/` (interfaces in same folder or `Services/Interfaces/`)
- `Dtos/` grouped by feature (e.g., `Dtos/Users/`)
- `Models/` or `Entities/` for EF entities
- `Data/` for `DbContext`, configurations, migrations
- `Validators/` if using FluentValidation
- `Middlewares/` for exception handling, correlation id, etc.

---

## 2) API conventions

### 2.1 Endpoints
- Prefer RESTful conventions:
  - `GET /resource` (list + filters)
  - `GET /resource/{id}` (single)
  - `POST /resource` (create)
  - `PUT /resource/{id}` (replace) or `PATCH` (partial)
  - `DELETE /resource/{id}` (delete)
- Every endpoint must return the correct status code:
  - `200` / `201` / `204`
  - `400` validation errors
  - `401` unauthenticated
  - `403` unauthorized
  - `404` not found
  - `409` conflict (concurrency, uniqueness)
  - `500` only for unexpected errors

### 2.2 DTO requirements
- All request DTOs must have validation rules.
- All response DTOs must be **stable** (avoid leaking internal structure).
- Map entities ↔ DTOs explicitly (manual mapping acceptable; AutoMapper only if already in project).

### 2.3 Pagination and filtering (if listing)
- Must support at least:
  - `page`, `pageSize`
  - `sortBy`, `sortDir`
  - feature-specific filters
- Responses must include:
  - items
  - total count
  - page metadata

---

## 3) Data access rules (EF Core)

### 3.1 Queries
- Use `AsNoTracking()` for read-only queries unless tracking is required.
- Use projection (`Select`) to DTOs for large lists to avoid over-fetching.
- Avoid N+1: use `Include` wisely or project with joins.

### 3.2 Writes
- Use transactions only when needed (multi-aggregate consistency).
- Handle concurrency where applicable (rowversion/concurrency tokens if present).
- Enforce uniqueness/constraints at both:
  - DB level (unique indexes) **and**
  - service level (pre-check + conflict handling)

### 3.3 Migrations
- Do not generate migrations unless explicitly requested.
- If a schema change is required, document the migration plan clearly.

---

## 4) Validation, errors, and logging

### 4.1 Validation
- Use DataAnnotations **or** FluentValidation (prefer whichever already exists).
- Validation is mandatory for:
  - request bodies
  - route parameters (e.g., GUID format)
  - query parameters

### 4.2 Error handling
- All exceptions must be translated into consistent problem responses:
  - Use `ProblemDetails` (RFC 7807) shape.
- Do not leak stack traces or sensitive details.

### 4.3 Logging
- Log at appropriate levels:
  - `Information`: lifecycle events
  - `Warning`: expected exceptional flows (not found, conflict, retry)
  - `Error`: unexpected exceptions (with correlation id)
- Never log secrets/tokens/PII.

---

## 5) Security rules

### 5.1 Authentication/Authorization
- Do not bypass auth checks.
- Authorization decisions live in Services (or dedicated policy handlers), not Controllers.
- If using JWT/Identity/AAD, follow existing project patterns.

### 5.2 Input safety
- Always validate and sanitize:
  - file names
  - URLs
  - HTML inputs (if any)
- Prevent mass assignment: map only allowed fields.

---

## 6) Code quality requirements

### 6.1 Clean code
- Methods should be short and single-purpose.
- Prefer pure functions where possible.
- Avoid magic strings; use constants/options.

### 6.2 Async correctness
- Use `async/await` end-to-end.
- Do not block on async (`.Result`, `.Wait()` forbidden).

### 6.3 Nullability
- Nullable reference types must be enabled (if project uses them).
- No `!` null-forgiveness unless justified with comment.

---

## 7) Deliverables checklist for any backend change

Every backend PR must include:
- ✅ Controller endpoint(s) (if API surface changes)
- ✅ Service method(s) with business rules
- ✅ DTOs + validation
- ✅ Error handling mapping
- ✅ Tests (see TEST_RULES.md)
- ✅ Update docs/comments where needed

---

## 8) “Stop conditions” (agent must halt and report)
- You discover schema mismatch needing migration but migrations are not requested.
- Authorization expectations are unclear and no existing policy can be reused.
- Requirements would force repository/UoW introduction (forbidden): propose compliant alternative.
