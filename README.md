# 🧠 Cognify: Personalized Cognitive Tutor Platform

> **An AI-powered adaptive learning system that models the learner’s cognition and dynamically adjusts teaching strategy.**

Cognify goes beyond simple quizzes to build a persistent **User Knowledge Model**, predicting memory decay and adapting content to your specific learning style and mastery level.

## 🚀 Key Capabilities

### 🧠 1. User Knowledge Model
Persistent tracking of your cognitive state per topic:
- **Mastery Score**: How well you understand a concept.
- **Confidence Score**: Your self-reported confidence.
- **Mistake Patterns**: AI analysis of your recurring errors.

### 🔁 2. AI Feedback Loop
Every interaction updates your profile. The system learns from:
- Quiz answers & time taken.
- Self-evaluations.
- Document uploads.

### 🎯 3. Adaptive Quiz Engine
No more generic tests. The AI generates diverse assessment types:
-   **Multiple Choice & True/False**: Quick retrieval practice.
-   **Open-Ended Questions**: Conceptual explanation (AI Graded).
-   **Matching/Pairing**: Terminology reinforcement.
-   **Context**: Tailored to your specific documents and notes.
-   **Adaptive Difficulty**: Scales from "Recall" to "Application" loops.

### 📉 4. Learning Decay Prediction
The system predicts when you are likely to forget a topic and schedules reviews automatically effectively implementing AI-driven Spaced Repetition.

### 📊 5. AI Learning Dashboard
Visualize your brain:
- **Knowledge Map**: Heatmap of your mastery.
- **Decay Forecast**: When to review what.
- **Weakness Visualization**: Pinpoint conceptual gaps.
- **Statistical Analytics**: Performance trends, topic distribution, and learning velocity metrics.

### ♻️ Continuous Learning Loop
Cognify operates as a closed-loop AI system:
> **User Action → Data Capture → AI Analysis → Knowledge Model Update → Adaptive Content**

The AI is not a one-time generator but a persistent decision engine that evolves with every interaction. Unlike stateless AI chat systems, Cognify maintains a persistent cognitive state for each learner, allowing long-term adaptation and memory modeling.

- **Exam Readiness Score**: AI-estimated preparedness based on mastery distribution and decay prediction.

### 🤖 6. AI Agents (v2)
- **OCR Agent**: PDF/Image extraction with embedded image support.
- **Question Gen Agent**: Adaptive quiz creation based on knowledge state.
- **Grading Agent**: Deterministic rubric-based grading with feedback.
- **Learning Analytics Agent**: Aggregated statistical engine for performance trends.

### ❌ Mistake Intelligence
AI classifies recurring conceptual errors and builds a learner-specific misconception profile to drive targeted remediation.

> Cognify introduces an AI-driven domain layer where learning state, analytics, and content generation form a continuous adaptive feedback system.

## 💻 Tech Stack
- **Backend**: ASP.NET Core 10 (Adaptive Services, Knowledge State)
- **Frontend**: Angular 19 (Visualization, Dashboard)
- **AI**: Azure OpenAI (Contextual Generation, Analysis)
- **Orchestration**: .NET Aspire
- **Data**: SQL Server (Structured State) + Azure Blob (Materials)

## 🏃 Getting Started
1. Clone the repo.
2. `dotnet dev-certs https --trust`
3. `cd Cognify.AppHost` & `dotnet run`.
