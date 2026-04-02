## Overview

`Dotnet.GenAI` is a comprehensive .NET repository demonstrating modern AI/GenAI technologies. It contains complete applications and educational demos showcasing how to build intelligent systems using Azure OpenAI, semantic search, and vector databases.

## Repository Structure

### apps – Complete Applications

#### **Dotnet.GenAI.MyCareerAssistant**
A full-stack Blazor web application that serves as an AI-powered career assistant chatbot. Key features include:

- **Intelligent Chat UI** – Interactive Blazor components with real-time message streaming
- **Vector-Based Semantic Search** – Ingests PDF documents into a PostgreSQL vector store (pgvector) for intelligent retrieval
- **Multi-Tool Integration** – Supports GitHub, Medium, Sessionize, and web search via MCP tools
- **Citation & Attribution** – Displays source citations with PDF viewer integration
- **System Prompt Framework** – Dynamic prompt generation based on ingested Q&A data and document context
- **Database & Migrations** – EF Core with pgvector support for embedding storage and retrieval
- **Docker & Aspire Support** – Multi-container deployment with PostgreSQL, pgAdmin, and local development orchestration

**Key Files:**
- `Program.cs` – Startup, service registration, and data ingestion
- `Chat.razor` – Main chat component
- `PDFDirectorySource.cs` – PDF ingestion pipeline
- `system-prompt.md` – Prompt templates and tool scenarios
- `Dockerfile` – Containerized deployment

**Technology Stack:** Blazor, .NET, Azure OpenAI, pgvector, PostgreSQL, DOMPurify, marked.js, PDF.js

### demos – Educational Examples

#### **Dotnet.GenAI.BasicAzureOpenAISample**
Simple introductory example demonstrating basic Azure OpenAI chat completion and function calling patterns.

#### **Dotnet.GenAI.RawImplementation**
Raw HTTP-based implementation of Azure OpenAI API calls without high-level SDKs, showing request/response structure for chat and tool use.

#### **Dotnet.GenAI.ExtensionsConsoleAgent**
Console-based agent using Microsoft Semantic Kernel with plugin architecture for structured task execution.

#### **Dotnet.GenAI.SemanticKernelConsoleAgent**
Advanced console agent demonstrating Semantic Kernel's agent framework with kernel plugins, filters, and complex orchestration.

#### **Dotnet.GenAI.Common** (Shared Library)
Reusable services and configurations:
- `GithubClient.cs` – GitHub API wrapper for user/repo data
- `InvoiceApiClient.cs` – Sample REST API client
- `HostConfig.cs` – Configuration helpers for AI services and vector stores
- `DocumentationClient.cs` – Local documentation lookup

**Technology Stack:** Azure OpenAI, Semantic Kernel, Microsoft Extensions AI, .NET configuration abstractions

## Technology Stack Summary

| Layer | Technologies |
|-------|--------------|
| **Frontend** | Blazor, ASP.NET Core, Tailwind CSS, marked.js, DOMPurify, PDF.js |
| **Backend** | .NET, Entity Framework Core, Semantic Kernel |
| **AI/ML** | Azure OpenAI, Azure AI Search, text-embedding-ada-002 |
| **Data** | PostgreSQL, pgvector, EF Core migrations |
| **Containers** | Docker, Docker Compose, .NET Aspire |
| **External APIs** | GitHub API, Serper (web search), Microsoft MCP |
