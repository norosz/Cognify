# Cognify: AI-Powered Learning Platform – Detailed Project Specification

---

## 1. Project Overview

Cognify is a **containerized, AI-assisted learning platform** built with a modern full-stack architecture, designed for demonstration, education, and rapid prototyping. It leverages:

- **ASP.NET Core 10** for backend services (REST API)
- **Angular 19** for frontend UI
- **.NET Aspire** for orchestration and local development
- **Azure-compatible services** (Azurite for Blob Storage, OpenAI API for AI)
- **Docker** for containerization

**Identity:**
"An AI-powered adaptive learning system that models the learner’s cognition and dynamically adjusts teaching strategy."

**Primary goals:**
- **Cognitive Modeling**: Track user mastery, confidence, and memory decay.
- **Adaptive Content**: Generate content tailored to the user's specific state (Low Mastery vs High Mastery).
- **Continuous Learning Loop**: Operate as a closed-loop AI system (User Action → Data → AI Analysis → Model Update → Adaptive Content).
- **Mistake Intelligence**: Classify recurring errors to build a learner-specific misconception profile.
- **Distributed Architecture**: Demonstrate complex state management with .NET Aspire.

---

## 2. Solution Structure & Components

```
Cognify.slnx                # Solution file
PROJECT_SPEC.md             # Project specification
Cognify.AppHost/            # Aspire orchestration (AppHost.cs, appsettings)
Cognify.Server/             # ASP.NET Core API (Controllers, Dockerfile, Program.cs)
cognify.client/             # Angular app (src/, package.json, Docker-ready)
Cognify.ServiceDefaults/    # Shared .NET code (extensions, config)
```

### Key Files & Folders
- **Cognify.AppHost/**: Orchestrates all services using .NET Aspire
- **Cognify.Server/**: Backend API, OpenAPI, Dockerfile, Entity Framework Core, JWT Auth
- **cognify.client/**: Angular 19 app, Material UI, REST API integration, JWT Auth
- **Cognify.ServiceDefaults/**: Shared .NET code, service discovery, telemetry

---

## 3. Technology Stack & Dependencies

### Backend (.NET 10, ASP.NET Core)
- Microsoft.NET.Sdk.Web
- Entity Framework Core (database access)
- JWT Authentication
- OpenAI API integration
- Azure Blob Storage SDK
- Microsoft.AspNetCore.OpenApi (OpenAPI/Swagger)
- Microsoft.Extensions.Http.Resilience, ServiceDiscovery (from ServiceDefaults)
- OpenTelemetry (instrumentation, tracing)
- **UglyToad.PdfPig** (PDF Parsing)

### Frontend (Angular 19)
- @angular/core, @angular/material, @angular/forms, @angular/router, etc.
- rxjs, tslib, zone.js
- Dev: @angular/cli, @angular-devkit/build-angular, typescript, karma, jasmine

### Orchestration & Infrastructure
- .NET Aspire (Aspire.AppHost, Aspire.Hosting.JavaScript)
- Docker (multi-stage builds, containerization)
- Azurite (local Azure Blob Storage emulator)
- OpenAI API (external)

---

## 4. Application Architecture

```
┌─────────────────────────────┐
│        Angular Frontend     │
│      (Containerized)        │
└──────────────┬──────────────┘
               │ HTTP (REST)
┌──────────────▼──────────────┐
│     ASP.NET Core API         │
│   (Containerized Service)   │
└──────────────┬──────────────┘
               │ EF Core
┌──────────────▼──────────────┐
│        Relational DB         │
│    (Aspire Resource)        │
└──────────────┬──────────────┘
               │ Blob SDK
┌──────────────▼──────────────┐
│          Azurite             │
│  (Blob Storage Emulator)    │
└──────────────┬──────────────┘
               │ HTTPS
┌──────────────▼──────────────┐
│         OpenAI API           │
│    (External AI Provider)   │
└─────────────────────────────┘
```

---

## 5. Core Features & Use Cases

1. **User Knowledge Model & Tracking**
   - Persistent tracking of cognitive state per topic (Mastery, Confidence, Mistake Patterns).
   - Updates based on interactions (Quiz answers, Self-evaluations, Document uploads).

2. **Adaptive Quiz Engine**
   - **Low Mastery**: Focus on conceptual understanding.
   - **High Mastery**: Challenge with edge cases and application.
   - **Forgetting Risk**: Spaced repetition to prevent decay.

3. **AI Learning Dashboard**
   - **Knowledge Map**: Heatmap of mastery.
   - **Decay Forecast**: Prediction of when topics will be forgotten.
   - **Weakness Visualization**: Pinpoint conceptual gaps.
   - **Exam Readiness Score**: AI-estimated preparedness.

4. **Mistake Intelligence**
   - Classification of recurring conceptual errors.
   - Building learner-specific misconception profiles.

5. **Learning Management**
   - User registration and authentication (JWT).
   - Creation/management of modules and documents (Blob Storage).
   - Notes creation and management.
   - Quiz attempts and scoring.

6. **Global Notification System**
   - Toast-style notifications (top-right, stacked).
   - Support for success, warning, error, and loading states.
   - Auto-dismiss after 5 seconds (except loading).
   - Loading notifications with spinner (for async operations).
   - Extraction state tracking (prevents duplicate operations).

---

## 6. AI Agents – Pipeline & Contracts Spec (v2)

The core logic is implemented via three backend AI agents with strict contracts:

### 1. OCR Agent
- **Function**: Extracts text and layout from Images/PDFs.
- **Input**: `ContractVersion`, `ContentType`, `Language`, `Hints`
- **Output**: `ContractVersion`, `ExtractedText`, `BlocksJson` (layout), `Confidence`
- **Idempotency**: Reprocessing same materialId overwrites/no-ops safely.

### 2. Question Generation Agent (Advanced)
- **Function**: Generates adaptive quizzes based on notes and user knowledge state.
- **Input**: `ContractVersion`, `NoteContent`, `QuestionType`, `Difficulty`, `QuestionCount`, `KnowledgeStateSnapshot`, `MistakeFocus`
- **Output**: `ContractVersion`, `QuizQuestions` (Prompt, Options, Key), `QuizRubric`
- **Features**: Deterministic output seeded by input context where feasible.

### 3. Grading Agent
- **Function**: Evaluates answers against a rubric and detects mistakes.
- **Input**: `ContractVersion`, `QuestionPrompt`, `UserAnswer`, `AnswerKey`, `Rubric`, `KnownMistakePatterns`
- **Output**: `ContractVersion`, `Score`, `MaxScore`, `Feedback`, `DetectedMistakes`, `ConfidenceEstimate`, `RawAnalysis`
- **Correlation**: Logs `AnswerEvaluationId` and update signals.

### 4. Learning Analytics Agent (Statistical Engine)
- **Function**: Aggregates interaction data to provide high-level learning insights.
- **Input**: `LearningInteractions`, `AttemptHistory`, `KnowledgeStates`
- **Output**: `PerformanceTrends` (Time series), `TopicDistribution`, `RetentionHeatmap`, `LearningVelocity`.
- **Metrics**: Calculates AI-estimated readiness and predicts optimal review windows.

---

## 7. End-to-End Pipelines

### Pipeline A2 — Document Upload → Extract Text + Extract Images
**Trigger**: User uploads a file (PDF, Office, HTML, E-book) to a module.

**Steps**:
1.  **Store Blob**: Save raw file to `{moduleId}/{documentId}_{filename}`.
2.  **Create Material**: DB record `Material` linked to the `Document` with `Status = Uploaded`, `HasEmbeddedImages = unknown`.
3.  **Run Extraction**:
    -   **Text**: Extract pure text content where possible (e.g., PDF Text layer).
    -   **Images**: Extract embedded images as separate binary assets (e.g., Figures, Charts).
    -   Store image blobs: `extracted/{documentId}/images/{imageId}.{ext}`.
4.  **Create Extraction Record**:
    -   `ExtractedMarkdown` (Text + inline LaTeX).
    -   `Images[]` metadata (list of extracted images + page references).
5.  **Create Note Draft**: Conversion is user-approved via Pending → “Review & Save” (keeps embedded image links).
    -   Embedded image links use generated download URLs.
6.  **Update Status**: `Material.Status = Processed`, `HasEmbeddedImages = true/false`.

### Pipeline B — Generate Adaptive Quiz
**Trigger**: User requests quiz.

**Steps**:
1.  **Resolve Context**: Notes, Extracted Texts, Module Summary.
2.  **Load Knowledge**: Fetch `UserKnowledgeState` (Mastery, Confidence, Forgetting Risk).
3.  **Call Agent**: Send `QuizGenRequest` to Question Generation Agent.
4.  **Persist**:
    -   `Quiz` entity.
    -   `QuizQuestions`: (Prompt, Type, Difficulty, TargetsMistake).
    -   `QuizRubric`: Structured scoring rules.
5.  **Return**: Adaptive quiz tailored to user state.

### Pipeline C — Submit Answer → Grade → Update Model
**Trigger**: User answers a question.

**Steps**:
1.  **Save Interaction**: `LearningInteraction` (Answer, TimeAdjusted).
2.  **Call Agent**: Send `GradeRequest` (Answer, Rubric, MistakePatterns) to Grading Agent.
3.  **Persist**:
    -   `AnswerEvaluation`: Score, Feedback, MatchedRubricItems.
    -   `DetectedMistakes`: Categories + Descriptions.
4.  **Update Knowledge Model**:
    -   Apply Mastery Delta (Correctness + Difficulty + Confidence).
    -   Increment Mistake Pattern Occurrences.
    -   Update Forgetting Risk & Next Review Date.
5.  **Return**: Evaluation feedback to frontend.

---

## 8. Domain Model & Data Schema (v2)

### Tables
- **Material**
    - `Id`, `UserId`, `FileName`, `ContentType`, `BlobPath`
    - `Status` (Uploaded/Processed/Failed), `CreatedAt`, `UpdatedAt`
- **MaterialExtraction**
    - `Id`, `MaterialId`, `ExtractedText`, `BlocksJson`, `OverallConfidence`
    - `ImagesJson` (List of extracted image references)
- **Note**
    - `Id`, `UserId`, `Title`, `Content` (Markdown + LaTeX)
    - `SourceMaterialId` (nullable), `EmbeddedImagesJson` (nullable)
- **Quiz**
    - `Id`, `UserId`, `Topic`, `Title`, `CreatedAt`
- **QuizQuestion**
    - `Id`, `QuizId`, `Type`, `Prompt`, `ChoicesJson` (nullable)
    - `AnswerKeyJson`, `Explanation`
    - `Difficulty`, `TargetsMistakeCategory` (nullable)
- **QuizRubricItem**
    - `Id`, `QuestionId`, `Points`, `RequiresText`
- **LearningInteraction**
    - `Id`, `UserId`, `Topic`, `Type` (QuizAnswer/SelfEval/NoteReview)
    - `QuestionId` (nullable), `UserAnswer`, `IsCorrect`, `TimeSpentSeconds`
- **AnswerEvaluation**
    - `Id`, `InteractionId`, `Score`, `MaxScore`
    - `MatchedRubricJson`, `MissedRubricJson`, `Feedback`
    - `DetectedMistakesJson`, `ConfidenceEstimate`
- **UserKnowledgeState**
    - `Id`, `UserId`, `Topic`
    - `MasteryScore`, `ConfidenceScore`
    - `ForgettingRisk`, `NextReviewAt`, `LastReviewedAt`
    - `MistakePatternsJson`, `PerformanceTrend`

---

## 9. Implementation Rules

- **Controller → Service split**: Controllers are thin; Services handle orchestration and DB updates.
- **Strict DTOs**: No "dynamic" JSON handling outside a single serialization boundary.
- **Idempotency**: OCR/Agents safe to retry.
- **Auditability**: Store `AgentRun` logs (Provider, Model, Usage, Duration).
- **Error Handling**: Fail gracefully; map exceptions to `ProblemDetails` and store failure status.
- **Testing**: Unit tests for orchestration; Contract tests for JSON shape.

---

## 10. Document Processing Extensions – Supported Formats

### A) Formats with Embedded Images (Must extract both)
- **PDF**: `.pdf` (Text layer + Raster images + Vector figures)
- **Office**: `.docx`, `.pptx`, `.xlsx`
- **Web**: `.html`, `.htm`, `.mhtml`
- **E-books**: `.epub`

### B) Image-only Formats (OCR Required)
- `.png`, `.jpg`, `.jpeg`, `.webp`, `.bmp`, `.tiff`

### C) Text-only Formats
- `.txt`, `.md`, `.json`, `.yaml`, `.yml` (Direct read)

---

## 11. Security & Authorization

- **JWT-based authentication**
- **Owner-Only Access**: Users can only access their own modules/materials.

---

## 12. References

- See `Cognify.Server/Dockerfile` for backend containerization.
- See `cognify.client/package.json` for Angular dependencies.
- See `Cognify.ServiceDefaults/` for shared .NET code.

---

*Updated January 28, 2026 – v2 Specification.*
