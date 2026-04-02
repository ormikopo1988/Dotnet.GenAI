# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Scope

This guidance focuses on the **Dotnet.GenAI.MyCareerAssistant** application inside the `apps/` folder -- a Blazor Server AI-powered career assistant chatbot with RAG, vector search, and MCP tool integration.

## Build and Run Commands

```bash
# Build the app
dotnet build apps/Dotnet.GenAI.MyCareerAssistant/Dotnet.GenAI.MyCareerAssistant.csproj

# Run directly (requires PostgreSQL with pgvector running + Azure OpenAI config)
dotnet run --project apps/Dotnet.GenAI.MyCareerAssistant

# Run via .NET Aspire (auto-provisions PostgreSQL/pgvector, pgAdmin, Application Insights)
dotnet run --project apps/Dotnet.GenAI.MyCareerAssistant.AppHost

# Run via Docker Compose (from repo root)
docker compose -f apps/docker-compose.yml up --build

# Add EF Core migration
dotnet ef migrations add <MigrationName> --project apps/Dotnet.GenAI.MyCareerAssistant

# Standalone PostgreSQL with pgvector (if not using Aspire or Docker Compose)
docker run -d --name postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 pgvector/pgvector:pg17
```

## Architecture Overview

The app is a .NET 10 Blazor Server application that acts as an AI career chatbot. It ingests PDF documents into a PostgreSQL pgvector store, performs semantic search for RAG-style responses, and integrates external tools via MCP (Model Context Protocol) and custom function tools.

### Startup Flow

`Program.cs` orchestrates startup in this order:
1. Azure Key Vault configuration (optional, if `AZURE_KEY_VAULT_ENDPOINT` is set)
2. Aspire service defaults (`AddServiceDefaults`)
3. OpenTelemetry/logging configuration (`LoggingConfigurator`)
4. Blazor Server component registration
5. MCP client initialization (`McpClientInitializer` -- connects to GitHub MCP + Playwright MCP servers)
6. DI registration (`DependencyInjection.AddApplicationServices`)
7. Database migration (`InitialiseDatabaseAsync` -- auto-applies EF Core migrations)
8. PDF ingestion from `wwwroot/Data/` at startup
9. App run

### DI Registration Pattern

`DependencyInjection.cs` uses modular `Add*` extension methods:
- `AddAIServices` -- Azure OpenAI client, `IChatClient` (via `ChatClientBuilder` with logging + function invocation middleware), `ChatOptions` with merged tools, embedding generator
- `AddAppServices` -- system prompt settings, prompt generators, Q&A service, business inquiry service, cosine similarity service
- `AddBackgroundServices` -- `DataIngestionService` (periodic re-ingestion)
- `AddData` -- EF Core with PostgreSQL/Npgsql, snake_case naming convention, auditable entity interceptor
- `AddInjectionServices` -- pgvector store, vector collections for chunks and documents, `DataIngestor`, `SemanticSearch`
- `AddEmailSender` -- SendGrid email
- `AddSerperClient` -- Serper web search API

### AI Tool System

Tools are composed from two sources and merged into `ChatOptions.Tools`:

**Built-in tools** (`FunctionRegistry.cs`) -- created via `AIFunctionFactory.Create` using reflection on service interfaces:
- `save_question_record_to_db` / `get_semantically_similar_question_record_from_db` -- Q&A tracking
- `save_business_inquiry_record_to_db` / `get_semantically_similar_business_inquiry_record_from_db` -- business inquiry tracking
- `send_email` -- SendGrid email delivery
- `web_search` -- Serper web search

**MCP tools** (`McpClientInitializer.cs`) -- external tool servers:
- Playwright MCP (browser automation via `npx @playwright/mcp@latest`)
- GitHub MCP (via GitHub Copilot MCP endpoint, requires `GitHubPat`)
- Both are filtered by allowlists in `appsettings.json` (`AllowedGitHubTools`, `AllowedPlaywrightTools`)

**Search tool** -- additionally registered inline in `Chat.razor` via `AIFunctionFactory.Create(SearchAsync)`, calling `SemanticSearch.SearchAsync` against the pgvector store.

### Ingestion Pipeline

`IIngestionSource` interface with `PDFDirectorySource` implementation:
1. `PDFDirectorySource` enumerates `*.pdf` files in `wwwroot/Data/`
2. Uses PdfPig (`NearestNeighbourWordExtractor` + `DocstrumBoundingBoxes`) for layout-aware text extraction
3. Chunks text via Semantic Kernel's `TextChunker.SplitPlainTextParagraphs` (200 token max)
4. `DataIngestor` manages vector collection lifecycle: ensures collections exist, detects new/modified/deleted documents by version (file last-write time), upserts chunks
5. `DataIngestionService` background service re-runs ingestion on a configurable interval (`BackgroundServiceTimespanMin:DataIngestion` in config)

### Prompt System

- `PromptTemplates/system-prompt.md` -- main system prompt template with placeholders (`{OwnerName}`, `{OwnerEmail}`, `{OwnerGitHubUrl}`, `{OwnerMediumUrl}`, `{OwnerSessionizeUrl}`, `{QuestionAndAnswerSection}`)
- `SystemPromptGenerator` replaces placeholders with config values and appends answered Q&A records from the database
- The system prompt defines 7 scenarios (GitHub, Medium, Sessionize, career facts/RAG, unknown questions, business inquiries, web search) each with strict tool-use instructions
- Citation format is enforced as `<citation filename='...' page_number='...'>exact quote</citation>` -- the `ChatCitation.razor` component parses and renders these as PDF viewer links
- `PromptTemplates/suggestion-prompt.md` -- drives `ChatSuggestions.razor` to generate follow-up suggestions after each response

### Blazor UI Components

All under `Components/Pages/Chat/`:
- `Chat.razor` -- main page (`/`), manages conversation state, streaming responses via `IChatClient.GetStreamingResponseAsync`, inline search tool
- `ChatMessageList.razor` -- renders message history with client-side JS for markdown rendering
- `ChatMessageItem.razor` -- individual message rendering
- `ChatInput.razor` -- user input with JS interop
- `ChatCitation.razor` -- citation rendering, links to PDF viewer at `lib/pdf_viewer/viewer.html`
- `ChatSuggestions.razor` -- auto-generates follow-up suggestion buttons using structured output (`GetResponseAsync<string[]>`)
- `ChatHeader.razor` -- header with "New Chat" reset

Client-side: `wwwroot/app.js` handles markdown rendering (marked.js + DOMPurify for sanitization), Tailwind CSS for styling.

### Entity Model

Entity hierarchy: `BaseEntity` (int Id) -> `BaseAuditableEntity` (CreatedAt/UpdatedAt) -> `BaseEmbeddableEntity` (Embedding as comma-separated float string)

Domain entities:
- `QuestionAndAnswer` -- unanswered questions saved by AI tool, answers populated later by owner
- `BusinessInquiry` -- business requests with name/email/request

`CosineSimilarityService` computes similarity between input and stored entity embeddings (threshold: 0.8) to avoid duplicate records.

`AuditableEntityInterceptor` auto-stamps `CreatedAt`/`UpdatedAt` on save.

### Data Stores

Two separate storage systems:
1. **EF Core / PostgreSQL** -- `ApplicationDbContext` for `QuestionAndAnswer` and `BusinessInquiry` entities (relational data with embeddings stored as comma-separated strings)
2. **pgvector / VectorStoreCollection** -- `IngestedDocument` and `IngestedChunk` collections for semantic search (embeddings managed by the vector store connector)

Both use the same PostgreSQL instance (`DefaultConnection`).

## Required Configuration

Set via `appsettings.json`, user secrets, or environment variables:

| Key | Required | Description |
|-----|----------|-------------|
| `AzureOpenAI:Endpoint` | Yes | Azure OpenAI endpoint URL |
| `AzureOpenAI:Key` | Yes | Azure OpenAI API key |
| `AzureOpenAI:ChatModelDeploymentName` | Yes | Chat model deployment name |
| `AzureOpenAI:EmbeddingModelDeploymentName` | Yes | Embedding model deployment name |
| `ConnectionStrings:DefaultConnection` | Yes | PostgreSQL connection string (must be pgvector-enabled) |
| `SystemPrompt:Owner:Name/Email/GitHubUrl/MediumUrl/SessionizeUrl` | No | Owner profile for system prompt |
| `GitHubPat` | No | GitHub PAT for MCP GitHub tools |
| `EmailSender:ApiKey/SenderEmail/SenderName` | No | SendGrid email settings |
| `Serper:ApiKey` | No | Serper web search API key |
| `AZURE_KEY_VAULT_ENDPOINT` | No | Azure Key Vault URI for production secrets |
| `ApplicationInsights:ConnectionString` | No | App Insights (falls back to console OTel exporter) |

## Key Libraries

| Library | Version | Usage |
|---------|---------|-------|
| Microsoft.Extensions.AI | 10.4.x | `IChatClient` abstraction, `ChatClientBuilder`, `AIFunctionFactory`, embedding generation |
| Microsoft.SemanticKernel | 1.74.x | pgvector connector (`AddPostgresVectorStore`), `TextChunker` for ingestion |
| ModelContextProtocol | 1.2.x | MCP client for GitHub and Playwright tool servers |
| PdfPig | 0.1.14 | PDF text extraction with layout analysis |
| EF Core + Npgsql | 10.0.x | PostgreSQL data layer, migrations |
| .NET Aspire | 9.5.x / 13.2.x | Local development orchestration (AppHost) |
| SendGrid | 9.29.x | Email delivery via AI tool |
| Azure.AI.OpenAI | 2.1.x | Azure OpenAI SDK |
