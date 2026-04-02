# Architecture

## High-Level Overview

Dotnet.GenAI.MyCareerAssistant is a .NET 10 Blazor Server application that serves as an AI-powered career assistant chatbot. It combines Retrieval-Augmented Generation (RAG) with external tool integrations to answer questions about a person's career, background, and professional experience.

```
User (Browser)
     |
     v
[Blazor Server UI]  -- Chat.razor, ChatInput, ChatMessageList, ChatCitation, ChatSuggestions
     |
     v
[IChatClient]       -- Azure OpenAI via ChatClientBuilder (logging + function invocation middleware)
     |
     +-- [Built-in Tools]        -- FunctionRegistry (Q&A, Business Inquiry, Email, Web Search)
     +-- [MCP Tools]             -- GitHub MCP, Playwright MCP (browser automation)
     +-- [Search Tool]           -- SemanticSearch against pgvector (inline in Chat.razor)
     |
     v
[Data Layer]
     +-- [EF Core / PostgreSQL]  -- QuestionAndAnswer, BusinessInquiry entities
     +-- [pgvector Collections]  -- IngestedDocument, IngestedChunk (vector store)
```

## Request Flow

1. User sends a message via `ChatInput.razor`
2. `Chat.razor` adds it to the conversation and calls `IChatClient.GetStreamingResponseAsync`
3. The AI model receives the system prompt (generated from template + Q&A records) and the conversation history
4. Based on user intent, the model invokes tools:
   - **Search tool** -- queries pgvector for semantically similar document chunks
   - **Built-in tools** -- saves questions/inquiries, sends emails, performs web searches
   - **MCP tools** -- navigates GitHub/Medium/Sessionize via Playwright, queries GitHub API
5. The response streams back to the UI with citations in XML format
6. `ChatCitation.razor` parses citation tags and renders links to the PDF viewer
7. `ChatSuggestions.razor` generates follow-up suggestions via a separate AI call

## Component Architecture

### Startup Pipeline

```
Program.cs
  |-- Azure Key Vault (optional)
  |-- AddServiceDefaults() (Aspire)
  |-- LoggingConfigurator (OpenTelemetry)
  |-- Blazor Server registration
  |-- McpClientInitializer (GitHub + Playwright MCP clients)
  |-- DependencyInjection.AddApplicationServices()
  |     |-- AddAIServices (AzureOpenAIClient, IChatClient, ChatOptions, EmbeddingGenerator)
  |     |-- AddAppServices (prompt generators, Q&A service, business inquiry, cosine similarity)
  |     |-- AddBackgroundServices (DataIngestionService)
  |     |-- AddData (EF Core, PostgreSQL, interceptors)
  |     |-- AddInjectionServices (pgvector store, vector collections, DataIngestor, SemanticSearch)
  |     |-- AddEmailSender (SendGrid)
  |     |-- AddSerperClient (Serper web search)
  |-- InitialiseDatabaseAsync() (auto-migrate)
  |-- DataIngestor.IngestDataAsync() (initial PDF ingestion)
  |-- app.RunAsync()
```

### AI Tool Composition

Tools come from three sources and are merged at startup:

1. **FunctionRegistry** (`FunctionRegistry.cs`) -- built-in tools created via `AIFunctionFactory.Create` with reflection on service interfaces
2. **MCP tools** (`McpClientInitializer.cs`) -- external tool servers filtered by allowlists in `appsettings.json`
3. **Search tool** -- registered inline in `Chat.razor` at component initialization

The `ChatOptions.Tools` collection is injected via DI and extended in the Chat component.

### Dual Data Store Design

The app uses two storage systems on the same PostgreSQL instance:

| Store | Purpose | Technology |
|-------|---------|------------|
| **EF Core** | `QuestionAndAnswer` and `BusinessInquiry` entities with relational queries + cosine similarity via in-memory computation | Npgsql + EF Core + snake_case naming |
| **pgvector Collections** | `IngestedDocument` and `IngestedChunk` for semantic vector search during RAG | Microsoft.SemanticKernel.Connectors.PgVector |

### Entity Hierarchy

```
BaseEntity (int Id)
  └── BaseAuditableEntity (CreatedAt, UpdatedAt)
        └── BaseEmbeddableEntity (Embedding: comma-separated float string)
              ├── QuestionAndAnswer (Question, Answer?)
              └── BusinessInquiry (Name, Email, Request)
```

`AuditableEntityInterceptor` auto-stamps timestamps. `CosineSimilarityService` computes embedding similarity with a 0.8 threshold to prevent duplicate records.

## Hosting Options

1. **Direct** (`dotnet run`) -- requires pre-existing PostgreSQL with pgvector
2. **.NET Aspire** (`AppHost`) -- auto-provisions PostgreSQL (pgvector:pg17), pgAdmin, Application Insights
3. **Docker Compose** -- multi-container with app + PostgreSQL + pgAdmin, includes Chrome/Playwright for MCP browser tools
