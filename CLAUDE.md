# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

.NET 10 repository with two areas: `apps/` (a full Blazor Server AI career chatbot) and `demos/` (educational AI pattern examples). The primary focus is **Dotnet.GenAI.MyCareerAssistant**.

## Build and Run

```bash
# Build the main app
dotnet build apps/Dotnet.GenAI.MyCareerAssistant/Dotnet.GenAI.MyCareerAssistant.csproj

# Run via .NET Aspire (recommended -- auto-provisions PostgreSQL/pgvector + pgAdmin)
dotnet run --project apps/Dotnet.GenAI.MyCareerAssistant.AppHost

# Run directly (requires external PostgreSQL with pgvector + Azure OpenAI config)
dotnet run --project apps/Dotnet.GenAI.MyCareerAssistant

# Run via Docker Compose
docker compose -f apps/docker-compose.yml up --build

# Add EF Core migration
dotnet ef migrations add <MigrationName> --project apps/Dotnet.GenAI.MyCareerAssistant

# Standalone PostgreSQL with pgvector (if not using Aspire or Docker Compose)
docker run -d --name postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 pgvector/pgvector:pg17
```

No test projects exist. No CI/CD workflows are configured. No `global.json` or `Directory.Build.props`.

## Architecture

### Startup Pipeline (`Program.cs`)

Key Vault (optional via `AZURE_KEY_VAULT_ENDPOINT`) -> Aspire ServiceDefaults -> OpenTelemetry (`LoggingConfigurator`) -> Blazor Server -> MCP clients initialized (`McpClientInitializer`) -> DI registration (`DependencyInjection.cs`) -> DB migration (`ApplicationDbContextInitialiser`) -> PDF ingestion (`DataIngestor` + `PDFDirectorySource` from `wwwroot/Data/`) -> run

### Three-Source Tool System

AI tools are composed from three sources and merged into `ChatOptions.Tools`:

1. **Built-in tools** (`FunctionRegistry.cs`) -- uses reflection + `AIFunctionFactory.Create()` to register: `save_question_record_to_db`, `get_semantically_similar_question_record_from_db`, `save_business_inquiry_record_to_db`, `get_semantically_similar_business_inquiry_record_from_db`, `send_email`, `web_search`
2. **MCP tools** (`McpClientInitializer.cs`) -- Playwright (stdio transport via `npx @playwright/mcp@latest`) and GitHub (HTTP transport to `api.githubcopilot.com/mcp/`), both filtered by allowlists in `appsettings.json` (`AllowedPlaywrightTools`, `AllowedGitHubTools`)
3. **Search tool** -- registered inline in `Chat.razor.OnInitializedAsync()`, wraps `SemanticSearch.SearchAsync()` for pgvector RAG queries

### Dual Data Stores (same PostgreSQL instance)

- **EF Core** (`ApplicationDbContext`) -- relational storage for `QuestionAndAnswer` and `BusinessInquiry` entities, with cosine similarity via embedding column. Uses snake_case naming convention (`UseSnakeCaseNamingConvention()`), `AuditableEntityInterceptor` for auto-stamping `CreatedAt`/`UpdatedAt`
- **pgvector collections** -- `IngestedChunk` and `IngestedDocument` for semantic vector search on ingested PDFs. Collection names: `data-dotnet_genai_mycareerassistant-chunks`, `data-dotnet_genai_mycareerassistant-documents`

### Entity Hierarchy

```
BaseEntity (Id: int)
  -> BaseAuditableEntity (CreatedAt, UpdatedAt: DateTimeOffset)
    -> BaseEmbeddableEntity (Embedding: string -- comma-separated floats)
      -> QuestionAndAnswer (Question, Answer?)
      -> BusinessInquiry (Name, Email, Request)
```

### DI Registration Pattern (`DependencyInjection.cs`)

Single extension method `AddApplicationServices()` delegates to private methods for each concern: `AddAIServices`, `AddAppServices`, `AddBackgroundServices`, `AddData`, `AddInjectionServices`, `AddEmailSender`, `AddSerperClient`. Settings use manual `configuration.Bind()` into singleton instances (not `IOptions<T>`).

`IChatClient` is a singleton built with `ChatClientBuilder` wrapping `AzureOpenAIClient`, with logging and function invocation middleware. `ChatOptions` is registered as transient (new tool set per scope).

### Prompt System

Templates in `PromptTemplates/` (copied to output on build). `SystemPromptGenerator` replaces placeholders (`{OwnerName}`, `{OwnerEmail}`, etc.) and appends dynamic Q&A data from DB. System prompt defines 7 scenario-based tool routing rules and enforces XML citation format: `<citation filename='...' page_number='...'>max 5 word quote</citation>`, parsed by `ChatCitation.razor`.

### Chat Component (`Chat.razor`)

Main page component at `/`. Injects `IChatClient`, `SemanticSearch`, `ISystemPromptGenerator`, `ChatOptions`. Uses `GetStreamingResponseAsync()` with cancellation support. Tracks `statefulMessageCount` for conversation continuity. Partial responses from cancelled streams are preserved in history.

### Ingestion Pipeline

`DataIngestor.IngestDataAsync()` orchestrates vector collection creation, document diffing, and upserts. `PDFDirectorySource` uses PdfPig (`NearestNeighbourWordExtractor` + `DocstrumBoundingBoxes`) for text extraction and Semantic Kernel's `TextChunker` for 200-token chunks. `DataIngestionService` runs ingestion periodically as a hosted service (interval from `BackgroundServiceTimespanMin:DataIngestion`).

### Aspire AppHost

`AppHost.cs` provisions PostgreSQL (pgvector:pg17 image), pgAdmin, and wires the connection string `DefaultConnection` to the main app. The app waits for DB health before starting.

## Required Configuration

Set via `appsettings.json`, user secrets, or environment variables:

| Key | Required | Purpose |
|-----|----------|---------|
| `AzureOpenAI:Endpoint` | Yes | Azure OpenAI endpoint |
| `AzureOpenAI:Key` | Yes | Azure OpenAI API key |
| `AzureOpenAI:ChatModelDeploymentName` | Yes | Chat model deployment |
| `AzureOpenAI:EmbeddingModelDeploymentName` | Yes | Embedding model deployment |
| `ConnectionStrings:DefaultConnection` | Yes | PostgreSQL with pgvector |
| `SystemPrompt:Owner:*` | No | Owner profile for prompt placeholders |
| `GitHubPat` | No | GitHub PAT for MCP tools (GitHub tools disabled if empty) |
| `EmailSender:ApiKey` | No | SendGrid API key |
| `Serper:ApiKey` | No | Web search API key |

## Demo Projects (`demos/`)

Educational progression from basic to advanced AI patterns, all .NET 10:
- **BasicAzureOpenAISample** -- simple chat + function calling
- **RawImplementation** -- raw HTTP Azure OpenAI API calls (no SDK)
- **ExtensionsConsoleAgent** -- Microsoft.Extensions.AI console agent
- **SemanticKernelConsoleAgent** -- Semantic Kernel agent with filters
- **Common** -- shared library (GithubClient, InvoiceApiClient, HostConfig, DocumentationClient)

## Detailed Documentation

See `docs/` for deeper coverage: [architecture](docs/architecture.md), [ingestion pipeline](docs/ingestion-pipeline.md), [AI tools](docs/ai-tools.md), [prompt system](docs/prompt-system.md), [configuration](docs/configuration.md), [deployment](docs/deployment.md).
