# 🚀 Project Status Board

## 📋 To Do

### 🔐 Authentication & Users
- [ ] **[Backend]** Implement `AuthService` (Password hashing, JWT generation)
- [ ] **[Backend]** Create `AuthController` (Login, Register endpoints)
- [ ] **[Frontend]** Create `AuthService` (Login, Register, Token storage)
- [ ] **[Frontend]** Build Login Page & Route
- [ ] **[Frontend]** Build Registration Page & Route
- [ ] **[Frontend]** Add Auth Interceptor (Attach JWT to requests)

### 📦 Modules Management
- [ ] **[Backend]** Implement `ModuleService` (CRUD logic, Owner validation)
- [ ] **[Backend]** Create `ModulesController`
- [ ] **[Frontend]** Build Dashboard/Home (Module List)
- [ ] **[Frontend]** Build Create Module UI
- [ ] **[Frontend]** Build Module Detail View (Tabs/Layout)

### 📄 Documents & Storage
- [ ] **[Backend]** Implement `BlobStorageService` (Azurite integration)
- [ ] **[Backend]** Implement `DocumentService` (Upload, metadata)
- [ ] **[Backend]** Create `DocumentsController`
- [ ] **[Frontend]** Build Document List Component
- [ ] **[Frontend]** Implement File Upload UI
- [ ] **[Frontend]** Document Preview & Status Indicator

### 📝 Notes
- [ ] **[Backend]** Implement `NoteService` (CRUD)
- [ ] **[Backend]** Create `NotesController`
- [ ] **[Frontend]** Build Note List Component
- [ ] **[Frontend]** Build Note Editor (Markdown/Text)

### 🤖 AI Generation & Quizzes
- [ ] **[Backend]** Configure OpenAI Service (Aspire/Settings)
- [ ] **[Backend]** Implement `AiService` (Prompt engineering, API call)
- [ ] **[Backend]** Create `AiController` (Generate questions endpoint)
- [ ] **[Backend]** Implement `QuestionService` & `AttemptService`
- [ ] **[Backend]** Create `AttemptsController` (Submit, Score)
- [ ] **[Frontend]** Build Quiz Generation UI
- [ ] **[Frontend]** Build Quiz Taking Interface
- [ ] **[Frontend]** Build Quiz Result/Score View

## 🏗️ In Progress
- [ ] **[Project]** Detailed Planning & Documentation

## ✅ Done
- [x] **Context**: Read Project Specifications and Rules
- [x] **Database**: Create EF Core Entity Models
- [x] **Database**: Implement `ApplicationDbContext` (Relationships, Delete Behavior)
- [x] **Infra**: Configure Persistent SQL Server (Port 14333, Volume)
- [x] **Database**: Generate `InitialCreate` Migration
