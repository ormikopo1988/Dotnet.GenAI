## Overview
`Dotnet.GenAI.MyCareerAssistant` is a Blazor-based AI chat application that lets users query custom ingested data (PDFs) with an LLM-backed chat UI and vector-backed semantic search. 

Core responsibilities include:
- Ingest PDF content into a vector store.
- Expose an interactive chat UI that queries the vector store and LLM.
- Hostable locally, via Docker, or as an Aspire AppHost.

Relevant root files:
- `Program.cs` — app startup and service wiring  
- `Dockerfile` — multi-stage Docker build  
- `docker-compose.yml` — local multi-container composition

## Key Concepts & Components

### Web UI (Blazor + client JS)
- Main entry and render mode: `Components/App.razor` includes the app shell and registers client script `wwwroot/app.js`.
- Chat page: `Components/Pages/Chat/Chat.razor` — primary chat component and lifecycle (implements `IDisposable`).
  - Chat UI subcomponents and assets:
    - `Components.Pages.Chat.Chat`
    - `Components/Pages/Chat/ChatInput.razor.js` — client JS used by the chat input component
    - `Components/Pages/Chat/ChatMessageList.razor.js` — message rendering helpers
    - `Components/Pages/Chat/ChatCitation.razor` — citation UI + viewer linking
    - `wwwroot/app.js` — client DOM/markdown handling (uses DOMPurify + marked)
    - `wwwroot/lib/pdf_viewer/viewer.mjs` and `wwwroot/lib/pdf_viewer/viewer.html` — PDF viewer used for citations

### Ingestion & Vector Store
- Ingestion orchestration: `Dotnet.GenAI.MyCareerAssistant.Services.Ingestion.DataIngestor` — coordinates ingestion, chunk deletion, and writes to vector collections.
- PDF source implementation: `Dotnet.GenAI.MyCareerAssistant.Services.Ingestion.PDFDirectorySource`: 
  - Validates source dir, enumerates `*.pdf`, computes `SourceFileId` and `SourceFileVersion`, builds `IngestedDocument` list, and creates `IngestedChunk`s via PDF parsing.
  - Page paragraph extraction uses `UglyToad.PdfPig` and helpers: `NearestNeighbourWordExtractor`, `DocstrumBoundingBoxes`, `TextChunker`.
- Model classes:
  - `Dotnet.GenAI.MyCareerAssistant.Services.Ingestion.IngestedDocument` — document metadata with a dummy vector for some vector DBs.
  - `Dotnet.GenAI.MyCareerAssistant.Services.Ingestion.IngestedChunk` — chunk model annotated for vector store indexing (vector mapping from Text).
- Storage: Vector store collections are used via `VectorStoreCollection<,>` (wired into DI in startup).

### Prompting & Suggestions
- System prompt template: `PromptTemplates/system-prompt.md` — defines role, tools, and strict citation output rules (special XML citation format).
- Suggestion prompt generator: `Dotnet.GenAI.MyCareerAssistant.Services.SuggestionPromptGenerator` — reads `./PromptTemplates/suggestion-prompt.md`.
- The system prompt mandates a citation XML format; the UI includes components that render citations and link to PDF pages.

### Dataflow at Startup
- Key startup: `Program.cs` wires services, optionally loads secrets from Azure Key Vault if `AZURE_KEY_VAULT_ENDPOINT` is set, adds application services, and calls:
  - `await DataIngestor.IngestDataAsync(app.Services, new PDFDirectorySource(Path.Combine(builder.Environment.WebRootPath, "Data")));`
- This means the default behavior is to ingest PDFs found under `wwwroot/Data` on application start. See the inline comment in `Program.cs` about prompt injection risk.

### Database & Migrations
- EF Core migrations are present in `Migrations/` folder. Example: `20251105102201_InitialMigration.cs` and `20251105102213_EnableVectorExtension.Designer.cs`. These configure DB schema for documents/chunks metadata and vector extension usage (Npgsql + pgvector).
- AppHost config for PG / pgvector in `AppHost.cs` and `docker-compose` uses `pgvector/pgvector:pg17`.

### Hosting & Dev Ops
- Docker:
  - `Dockerfile` — multi-stage build (sdk -> publish -> final).
  - `docker-compose.yml` composes app + `pgvector` postgres + `pgadmin` and maps ports (`5000`/`5001` -> `80`/`443`).
  - `docker-compose.yml` sets container env for DB connection string: `ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=MyCareerAssistantDb;Username=postgres;Password=postgres;Include Error Detail=true`.
- Aspire AppHost:
  - `AppHost.cs` demonstrates automated provisioning and container references for local integration/testing using Aspire Hosting.

## Config & Secrets
- Local config: `appsettings.json` — default dev values, background ingest interval, allowed tools etc.
- Production overrides: `appsettings.Production.json` — contains `AZURE_KEY_VAULT_ENDPOINT` placeholder and Application Insights role.
- Optional Azure Key Vault integration is configured in `Program.cs`:
  - If `AZURE_KEY_VAULT_ENDPOINT` is provided the app calls `builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential())`.
- Docker compose and AppHost environment variables wire DB and credentials in compose and `AppHost`.

## Running Locally

1. Build and run with dotnet:
  ````sh
  # from repository root
  cd Dotnet.GenAI.MyCareerAssistant
  dotnet build
  dotnet run
  ````
  The app will ingest PDFs from `wwwroot/Data` at startup (see `Program.cs`).

2. With Docker Compose:
  ````sh
  docker compose up --build
  ````
  - Exposes HTTP/HTTPS as configured in `docker-compose.yml`.
  - Contains `postgres` image using `pgvector` for embedding-vector support.

3. Publishing container image:
  - See `Dockerfile` for multi-stage publish; standard `docker build` using the Dockerfile works.

## Security & Operational Notes
- Ingested content is treated as trusted for performance and functionality. The app logs a prompt-injection warning in `Program.cs`: "Important: ensure that any content you ingest is trusted..."
- The system prompt enforces a strict citation format in `PromptTemplates/system-prompt.md`. Be careful: it instructs the model to emit XML citations exactly in a given format.
- Secrets should not be committed to source. Local dev uses `appsettings.json` and user secrets; production uses Key Vault or environment variables.

## Extensibility Points
- Add new ingestion sources: implement `IIngestionSource` and register with `DataIngestor`. See `PDFDirectorySource`.
- Replace LLM provider / embeddings: DI wiring occurs in startup (`Program.cs`) through `AddApplicationServices`.
- Change chunking/parsing: `PDFDirectorySource.GetPageParagraphs` uses `TextChunker` and layout helpers — replace or augment to support other content types.

## File / Symbol Quick Reference
- Startup & config:
  - `Program.cs` — startup, Key Vault, ingestion call
  - `appsettings.json`  
  - `appsettings.Production.json`
- Ingestion & vector models:
  - `Dotnet.GenAI.MyCareerAssistant.Services.Ingestion.PDFDirectorySource`
  - `Dotnet.GenAI.MyCareerAssistant.Services.Ingestion.DataIngestor`
  - `Dotnet.GenAI.MyCareerAssistant.Services.Ingestion.IngestedDocument`
  - `Dotnet.GenAI.MyCareerAssistant.Services.Ingestion.IngestedChunk`
- UI / client:
  - `Chat.razor`
  - `ChatInput.razor`
  - `ChatMessageList.razor`
  - `ChatCitation.razor`
  - `app.js`
  - `viewer.mjs`
- Prompt templates & generators:
  - `system-prompt.md`
  - `SuggestionPromptGenerator.cs`
- Hosting & containers:
  - `Dockerfile`
  - `docker-compose.yml`
  - `AppHost.cs`

## Troubleshooting & Common Tasks
- No PDFs ingested:
  - Ensure files exist under `wwwroot/Data`.
  - Confirm `DataIngestor.IngestDataAsync` runs in startup (`Program.cs`).
- Database errors with pgvector:
  - Confirm `postgres` service uses `pgvector/pgvector:pg17` in `docker-compose.yml` or `AppHost`.
  - Run migrations in the container or from host against the DB configured via `ConnectionStrings__DefaultConnection`.
- To change ingestion schedule: edit timing settings in `appsettings.json` / `production` file.
