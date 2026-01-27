# 🚀 Project Status Board

## 📋 To Do

### � Notes
- [x] **[Backend]** Implement `NoteService` (CRUD)
- [x] **[Backend]** Create `NotesController`
- [x] **[Frontend]** Build Note List Component
- [x] **[Frontend]** Build Note Editor (Markdown/Text)

### 🤖 AI Generation, Agents & Quizzes


- [x] **[Agent]** Implement Handwriting Parsing Agent (OCR)
- [x] **[Agent]** Implement Question Generation Agent (Advanced) & Grading
- [x] **[Agent]** Implement Grading Agent
### 🧠 User Knowledge & AI Feedback
- [ ] **[Backend]** Create `UserKnowledgeState` Entity (Topic, Mastery, Confidence)
- [ ] **[Backend]** Implement `KnowledgeStateService` (Update logic)
- [ ] **[Backend]** Implement `LearningAnalyticsService` (Aggregator)

### 🎯 Adaptive Quiz Engine
- [ ] **[Backend]** Create `AdaptiveQuizService` (Contextual prompting)
- [ ] **[Backend]** Implement Prompt Engineering for Mastery/Confidence levels

### 📉 Decay & Mistake Intelligence
- [ ] **[Backend]** Implement `DecayPredictionService` (Spaced Repetition)
- [ ] **[Backend]** Implement `MistakeAnalysisService` (Error Pattern Detection)
- [ ] **[Backend]** Update `UserKnowledgeState` with `MistakePatterns` & `ForgettingRisk`

### 📊 AI Learning Dashboard (Frontend)
- [ ] **[Frontend]** Build Knowledge Map Visualization
- [ ] **[Frontend]** Build Memory Decay Chart
- [ ] **[Frontend]** Build Concept Weakness List

## 🏗️ In Progress



## ✅ Done
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
