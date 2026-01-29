# 🚀 Project Status Board

## 📋 To Do

###  Notes

### 🤖 AI Generation, Agents & Quizzes

### 🧠 User Knowledge & AI Feedback

### 🎯 Adaptive Quiz Engine

### 📉 Decay & Mistake Intelligence

### 📊 AI Learning Dashboard (Frontend)

### 🤖 AI Agents & Pipelines (v2)

### 💾 File Management Enhancements

## 🏗️ In Progress

### 🤖 AI Agents & Pipelines (v2)

## ✅ Done
- [x] **[Backend]** Implement `LearningAnalyticsService` (Aggregator)
- [x] **[Backend]** Implement prompting that includes Mastery/ForgettingRisk/MistakePatterns
- [x] **[Backend]** Implement `DecayPredictionService` (Spaced Repetition)
- [x] **[Backend]** Implement `MistakeAnalysisService` (Error Pattern Detection)
- [x] **[Backend]** Update `UserKnowledgeState` with `MistakePatterns` & `ForgettingRisk`
- [x] **[Frontend]** Build Knowledge Map Visualization
- [x] **[Frontend]** Build Memory Decay Chart
- [x] **[Frontend]** Build Concept Weakness List
- [x] **[Backend]** Add `FileSizeBytes` to Document Entity/DTO
- [x] **[Frontend]** Display File Size in Document Card in MB
- [x] **[Refactor]** Define strict v2 agent contracts (OCR / QuizGen / Grading) + versioning + input-hash idempotency
- [x] **[Refactor]** Align quiz output with v2 contracts (Rubric DTO + persistence strategy; keep `QuestionSet`/`Question` compatibility)
- [x] **[Test]** Make background worker test-safe (no OpenAI key required; deterministic test runs)
- [x] **[Test]** Add idempotency tests for pending extraction and quiz creation
- [x] **[Backend]** Implement `Material`, `MaterialExtraction`, `Note` (Updated) Entities
- [x] **[Backend]** Implement `LearningInteraction`, `AnswerEvaluation`, `UserKnowledgeState` Entities
- [x] **[Backend]** Migrate DB to v2 Schema
- [x] **[Agent]** Extract embedded images + metadata for PDF materials
- [x] **[Agent]** Populate extraction contract output (blocks/confidence/images)
- [x] **[Agent]** Implement Question Generation Agent (Pipe B)
- [x] **[Agent]** Implement Grading Agent (Pipe C)
- [x] **[Agent]** Implement Learning Analytics Agent (Statistical Engine)
- [x] **[Agent]** Generate rubric content in quiz generation output (v2 contract)
- [x] **[Agent]** Phase 3: PDF text-layer extraction via pending pipeline
- [x] **[Backend]** Create `AdaptiveQuizService` (knowledge-state target selection)
- [x] **[Backend]** Add endpoint to initiate adaptive quiz (reuse PendingQuiz flow)
- [x] **[Frontend]** Add Review Queue UI (due topics) + “Generate Review Quiz” action
- [x] **[Frontend]** Add Weak Topics UI (high forgetting risk) + “Generate Weakness Quiz” action
- [x] **[Test]** Add backend + frontend tests for adaptive generation flow
- [x] **Context**: Read Project Specifications and Rules
- [x] **Database**: Create EF Core Entity Models
- [x] **Database**: Implement `ApplicationDbContext` (Relationships, Delete Behavior)
- [x] **Infra**: Configure Persistent SQL Server (Port 14333, Volume)
- [x] **Database**: Generate `InitialCreate` Migration
- [x] **[Backend]** Implement `AuthService` (Password hashing, JWT generation)
- [x] **[Backend]** Create `AuthController` (Login, Register endpoints)
- [x] **[Backend]** Implement Unit & Integration Tests (Auth Module)
- [x] **[Frontend]** Create `AuthService` (Login, Register, Token storage)
- [x] **[Frontend]** Build Login Page & Route
- [x] **[Frontend]** Build Registration Page & Route
- [x] **[Frontend]** Add Auth Interceptor (Attach JWT to requests)
- [x] **[Backend]** Implement `ModuleService` (CRUD logic, Owner validation)
- [x] **[Backend]** Create `ModulesController`
- [x] **[Frontend]** Build Dashboard/Home (Module List)
- [x] **[Frontend]** Build Create Module UI
- [x] **[Frontend]** Build Module Detail View (Tabs/Layout)
- [x] **[Frontend]** Implement Main Layout & Navigation
- [x] **[Security]** Refactor Auth: JWT Decode, Password Confirmation
- [x] **[Backend]** Implement `BlobStorageService` (Azurite integration)
- [x] **[Backend]** Implement `DocumentService` (Upload, metadata)
- [x] **[Backend]** Create `DocumentsController`
- [x] **[Frontend]** Build Document List Component
- [x] **[Frontend]** Implement File Upload UI
- [x] **[Frontend]** Fixed Upload Crash & Implemented Force Download
- [x] **[Project]** Detailed Planning & Documentation
- [x] **[Backend]** Configure OpenAI Service (Aspire/Settings)
- [x] **[Backend]** Implement `AiService` (Prompt engineering, API call)
- [x] **[Backend]** Create `AiController` (Generate questions endpoint)
- [x] **[Backend]** Implement `QuestionService` & `AttemptService`
- [x] **[Backend]** Create `AttemptsController` (Submit, Score)
- [x] **[Frontend]** Build Quiz Generation UI
- [x] **[Frontend]** Build Quiz Taking Interface
- [x] **[Frontend]** Build Quiz Result/Score View
- [x] **[Frontend]** UI Fixes & Theme Updates (Light-blue primary, dark mode consistency)
- [x] **[Frontend]** Frontend Unit Tests for AI Components
- [x] **[Frontend]** Global Notification System (Toast stack, extraction state tracking)
- [x] **[Backend]** Implement Pending Items System (ExtractedContent, PendingQuiz)
- [x] **[Backend]** Async AI Task Persistence & Fix Build Errors
- [x] **[Frontend]** New Dashboard Layout (Leetify-inspired, Side-navigation)
- [x] **[Frontend]** Build Pending Management UI (Review, Save, Delete)
- [x] **[Frontend]** Integrated Background Quiz Generation Flow
- [x] **[Test]** Improved Backend Test Coverage (PendingQuizService, NoteService - 37 tests)
- [x] **[Test]** Improved Frontend Test Coverage (PendingService, PendingComponent - 85 tests)
- [x] **[Frontend]** Refactor Document Cards (Header Status, Icons, Watermark Data)
- [x] **[Test]** Verified Document Card Refactor (96 tests pass)
- [x] **[Backend]** Implement Token Validation (`/auth/me`) & Profile Management
- [x] **[Frontend]** Implement Startup Token Validation (Modern `provideAppInitializer`)
- [x] **[Frontend]** Build Profile Management UI (Update Profile, Change Password)
- [x] **[Refactor]** Modernize Angular Config & Backend Patterns
- [x] **[Bug Fix]** Fixed `NG0203` Injection Error in App Initializer
- [x] **[Frontend]** Added Profile Link to Sidebar Navigation
- [x] **[Backend]** Implement Optional Username (Dto, Service, JWT Claim)
- [x] **[Frontend]** Implement Optional Username in Register UI & Sidebar Display
- [x] **[UI/UX]** Style Profile Component (Glassmorphism) & Add Notifications
- [x] **[UI/UX]** Refine Empty States (Shadows & Borders) for Modules/Quizzes/Notes
- [x] **[UI/UX]** Standardize Empty State Backgrounds & Consistency
- [x] **[UI/UX]** Refactor Empty States to use `mat-card` with proper Elevation
- [x] **[Bug Fix]** Fixed Password Form Validation Error on Reset
- [x] **[Testing]** Updated & Created Tests for Auth, Profile, and Register Features
- [x] **[Frontend]** Implement `MultiSelect` (Checkbox) Quiz Type & UI
- [x] **[Backend]** Implement Strict File Extension & Size Validation
- [x] **[Bug Fix]** Fixed `MultipleSelect` quiz generation fallback & mapping
- [x] **[Refactor]** Global JSON String Enum Converter for Backend & Tests
- [x] **[UI/UX]** Renamed Document Status `Ready` -> `Uploaded`
- [x] **[UI/UX]** Enhanced Quiz Cards & Taking UI with Type Indicators
- [x] **[Testing]** Verified all 56 Backend & 109 Frontend tests pass
- [x] **[Refactor]** Phase 0: Replace AI `Task.Run` with durable background worker (pending processing)
- [x] **[Refactor]** Phase 0: Normalize ExtractedContent status to enum + string conversion
- [x] **[Refactor]** Phase 0: Fix AI prompt difficulty level bug
- [x] **[Refactor]** Phase 0: Remove legacy quiz generation endpoint usage
- [x] **[Refactor]** Phase 0: Add durable AgentRun tracking (schema + worker integration)
- [x] **[Backend]** Create `UserKnowledgeState` Entity (Topic, Mastery, Confidence)
- [x] **[Backend]** Implement `KnowledgeStateService` (Update logic)
- [x] **[Backend]** Add `LearningInteraction` + `AnswerEvaluation` entities
- [x] **[Backend]** Implement `Quiz`, `QuizQuestion`, `QuizRubric` Entities (renamed + migrated)
- [x] **[Backend]** Implement `NoteService` (CRUD)
- [x] **[Backend]** Create `NotesController`
- [x] **[Frontend]** Build Note List Component
- [x] **[Frontend]** Build Note Editor (Markdown/Text)
- [x] **[Agent]** Implement Handwriting Parsing Agent (OCR)
- [x] **[Agent]** Implement Question Generation Agent (Advanced) & Grading
- [x] **[Agent]** Implement Grading Agent
