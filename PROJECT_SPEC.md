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
- **Adaptive Content**: Generate content tailored to the user's specific state.
- **Continuous Feedback**: Use every interaction to refine the user model.
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

1. User registration and authentication (JWT)
2. Creation and management of learning modules
3. Upload and management of learning documents (Blob Storage)
4. Creation and management of notes
5. **AI-powered question generation** (OpenAI API)
6. Quiz attempts and scoring
7. Review of learning progress

---

## 6. Domain Model & Data Schema

### Entities
- **User**: Authenticated learner
- **Module**: Learning unit (topic/lesson)
- **Document**: Uploaded file (stored in Blob Storage)
- **Note**: Structured learning content
- **UserKnowledgeState**: (NEW) AI Persistent memory of the learner.
    - Fields: Topic, MasteryScore (0-1), ConfidenceScore (0-1), MistakePatterns (JSON), ForgettingRisk (Enum), LastReviewedAt.
- **QuestionSet**: AI-generated assessment
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

## 11. Future AI Agents & Roadmap

The application architecture must support pluggable AI agents for future enhancements:
1.  **Handwriting Parsing Agent**: A specialized agent/service to process uploaded images (notes/documents) and extract handwritten text into digital Notes (OCR).
2.  **Question Generation Agent**: An advanced agent to analyze Note content and generate tailored QuestionSets (Multiple Choice, True/False, Open Ended).
3.  **Grading Agent**: An agent to evaluate free-text answers and provide feedback.

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
