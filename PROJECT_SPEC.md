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
   - **Weakness Visualization**: Pinpointing conceptual gaps.
   - **Exam Readiness Score**: AI-estimated preparedness.

4. **Mistake Intelligence**
   - Classification of recurring conceptual errors.
   - Building learner-specific misconception profiles.

5. **Learning Management**
   - User registration and authentication (JWT).
   - Creation/management of modules and documents (Blob Storage).
   - Notes creation and management.
   - Quiz attempts and scoring.

---

## 6. Domain Model & Data Schema

### Entities
- **User**: Authenticated learner
- **Module**: Learning unit (topic/lesson)
- **Document**: Uploaded file (stored in Blob Storage)
- **Note**: Structured learning content (can optionally link to a Document)
- **UserKnowledgeState**: (NEW) AI Persistent memory of the learner.
    - Fields: Topic, MasteryScore (0-1), ConfidenceScore (0-1), MistakePatterns (JSON), ForgettingRisk (Enum), LastReviewedAt, ExamReadinessScore.
    - **Note**: This forms the basis of the "Continuous Learning Loop".
- **QuestionSet**: AI-generated assessment
- **Question**: Individual question item.
    - Types: `MultipleChoice`, `TrueFalse`, `OpenText`, `Matching`, `Ordering` (New types added).
- **Attempt**: User quiz attempt (Feed for the Knowledge Model)

### Architecture Changes (v2)
- **KnowledgeStateService**: Manages the cognitive model.
- **LearningAnalyticsService**: Aggregates performance data.
- **AdaptiveQuizService**: generating optimal next questions.
- **DecayPredictionService**: Spaced repetition logic.
- **MistakeAnalysisService**: Error classification.

---

## 7. API Endpoints (High-Level)

- **Authentication**
  - POST /api/auth/register
  - POST /api/auth/login
- **Modules**
  - GET /api/modules
  - POST /api/modules
  - GET /api/modules/{id}
- **Documents**
  - POST /api/modules/{id}/documents
  - GET /api/modules/{id}/documents
  - POST /api/documents/{id}/set-text
- **Notes**
  - POST /api/modules/{id}/notes
  - GET /api/modules/{id}/notes
  - GET /api/notes/{id}
- **AI**
  - POST /api/ai/questions/from-note/{noteId}
- **Attempts**
  - POST /api/question-sets/{id}/attempts
  - GET /api/question-sets/{id}/attempts/me

---

## 8. Security & Authorization

- JWT-based authentication
- Users can only access their own modules
- All child entities inherit module ownership
- Policy-based authorization (`OwnerOnly`)

---

## 9. Containerization & Orchestration

- **Docker**: Multi-stage builds for backend and frontend
- **.NET Aspire**: Service discovery, environment config, container lifecycle
- **Azurite**: Local blob storage for development
- **OpenAI API**: External AI provider

---

## 10. Success Criteria

- Aspire launches all services successfully
- Containers communicate correctly
- AI functionality demonstrably works
- Clean separation of concerns
- Stable demo for presentation

---

## 11. Expanded AI Agents & Functionality (v2)

The application architecture now officially supports advanced AI workflows:

1.  **Handwriting Parsing Agent (OCR)**:
    -   **Workflow**: User selects an *existing* uploaded document (image/PDF) -> Agent specifically processes it using GPT-4o Vision -> Extracted text is returned for editing/saving as a Note.
    -   **Optionality**: Parsing is fully optional. Users can create a Note manually or import from a Document.

2.  **Advanced Question Generation Agent**:
    -   **Workflow**: Agent analyzes Note content -> Generates specific assessment types based on user request.
    -   **Supported Types**:
        -   **Quizzes**: Multiple Choice, True/False.
        -   **Open Questions**: Free-text answers requiring AI grading.
        -   **Pairing/Matching**: Concept definition matching.
    -   **Adaptive Logic**: Questions are tailored to difficulty levels (Beginner, Intermediate, Advanced).

3.  **Grading Agent**:
    -   An agent to evaluate free-text answers and provide structural feedback (not just correct/incorrect).

---

## 12. Not in Scope (v1)

- Real-time collaboration
- Multi-language support
- Complex role-based access control (RBAC) beyond 'Owner'

---

## 12. References

- See `Cognify.Server/Dockerfile` for backend containerization
- See `cognify.client/package.json` for Angular dependencies
- See `Cognify.AppHost/Cognify.AppHost.csproj` for Aspire orchestration
- See `Cognify.ServiceDefaults/` for shared .NET code

---

*Generated January 25, 2026 – based on current project structure and dependencies.*
