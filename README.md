# Cognify: AI-Powered Learning Platform

> **A containerized, AI-assisted learning platform built with .NET 10, Angular 19, and .NET Aspire.**

Cognify is a modern full-stack educational tool designed to demonstrate the power of distributed application architecture and generative AI in education. It allows users to organize learning materials, process documents, and generate interactive quizzes using AI.

## 🚀 Why Cognify?

Cognify serves as a comprehensive reference implementation for modern cloud-native development, tackling real-world educational challenges.

*   **🎓 AI-Driven Learning**: Automatically generates quizzes and assessments from your study notes using OpenAI.
*   **🏗️ Modern Architecture**: Built on **.NET 10** and **Angular 19**, showcasing the latest in web development.
*   **☁️ Cloud-Native Ready**: Fully containerized with **Docker** and orchestrated by **.NET Aspire** for seamless local development and deployment.
*   **🔒 Secure & Private**: Implements robust JWT-based authentication and ownership policies, ensuring your data remains yours.
*   **🛠️ Developer Experience**: Demonstrates clean separation of concerns, persistent storage integration (SQL & Blob), and automated testing.

## 🌟 Key Features

*   **User Management**: Secure registration and login flow.
*   **Module System**: Organize topics into learning modules.
*   **Document Processing**: Upload PDFs and images (future OCR integration planned).
*   **Smart Notes**: Create structured notes that serve as the context for AI generation.
*   **Interactive Quizzes**: Take AI-generated tests with immediate feedback and scoring.

## 💻 Technology Stack

*   **Backend**: ASP.NET Core 10, Entity Framework Core, SQL Server
*   **Frontend**: Angular 19, Angular Material, RxJS
*   **Orchestration**: .NET Aspire
*   **Storage**: Azurite (Blob Storage Emulator)
*   **AI**: OpenAI API

## 🏃 Getting Started

### Prerequisites
*   [.NET 10 SDK](https://dotnet.microsoft.com/)
*   [Node.js](https://nodejs.org/) (LTS)
*   [Docker Desktop](https://www.docker.com/products/docker-desktop)

### Running the Application
1.  **Clone the repository**:
    ```bash
    git clone https://github.com/norosz/Cognify.git
    cd Cognify
    ```
2.  **Trust the development certificate**:
    ```bash
    dotnet dev-certs https --trust
    ```
3.  **Start with .NET Aspire**:
    ```bash
    cd Cognify.AppHost
    dotnet run
    ```
    This will launch the **Aspire Dashboard**, where you can view and access the running Backend API and Frontend Client.

---
*Built with ❤️ for education and technology.*
