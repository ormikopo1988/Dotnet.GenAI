# Deployment

## Overview

The application supports three deployment modes: direct dotnet run, .NET Aspire orchestration, and Docker Compose.

## Option 1: Direct (dotnet run)

**Prerequisites:**
- .NET 10 SDK
- PostgreSQL with pgvector extension (`pgvector/pgvector:pg17` Docker image or manual install)
- Node.js (for Playwright MCP server)
- Azure OpenAI resource with chat and embedding deployments

```bash
# Start PostgreSQL with pgvector
docker run -d --name postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=MyCareerAssistantDb -p 5432:5432 pgvector/pgvector:pg17

# Optional: pgAdmin for database management
docker network create pgnetwork
docker run -d --name postgres --network pgnetwork -e POSTGRES_PASSWORD=postgres -p 5432:5432 pgvector/pgvector:pg17
docker run -d --name my-pgadmin --network pgnetwork -e 'PGADMIN_DEFAULT_EMAIL=admin@example.com' -e 'PGADMIN_DEFAULT_PASSWORD=root' -p 82:80 dpage/pgadmin4

# Configure secrets (Azure OpenAI, etc.)
cd apps/Dotnet.GenAI.MyCareerAssistant
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:Key" "your-key"
dotnet user-secrets set "AzureOpenAI:ChatModelDeploymentName" "your-chat-deployment"
dotnet user-secrets set "AzureOpenAI:EmbeddingModelDeploymentName" "your-embedding-deployment"

# Run
dotnet run --project apps/Dotnet.GenAI.MyCareerAssistant
```

The app auto-applies EF Core migrations and ingests PDFs from `wwwroot/Data/` at startup.

## Option 2: .NET Aspire (AppHost)

**Prerequisites:**
- .NET 10 SDK
- Docker (Aspire provisions containers automatically)

The AppHost (`apps/Dotnet.GenAI.MyCareerAssistant.AppHost`) automatically provisions:
- PostgreSQL with pgvector (`pgvector/pgvector:pg17`)
- pgAdmin (port 82)
- Application Insights (in publish mode)

```bash
dotnet run --project apps/Dotnet.GenAI.MyCareerAssistant.AppHost
```

Azure OpenAI and other secrets still need to be configured via user secrets on the AppHost project (UserSecretsId: `fd54e133-9bff-42de-8963-c243f9a25f36`).

## Option 3: Docker Compose

**Prerequisites:**
- Docker and Docker Compose

```bash
docker compose -f apps/docker-compose.yml up --build
```

Compose provisions:
- **App container** -- built from `apps/Dotnet.GenAI.MyCareerAssistant/Dockerfile`, exposed on ports 5000 (HTTP) and 5001 (HTTPS)
- **PostgreSQL** (`pgvector/pgvector:pg17`) -- port 5432, with persistent volumes
- **pgAdmin** -- port 82

The Dockerfile is a multi-stage build that also installs Chrome, Node.js, and Playwright for MCP browser automation.

### HTTPS Certificate

Docker Compose expects an HTTPS development certificate at `${USERPROFILE}/.aspnet/https/aspnetapp.pfx` with password `Test1234!`. Generate it with:

```bash
dotnet dev-certs https -ep ${USERPROFILE}/.aspnet/https/aspnetapp.pfx -p Test1234!
dotnet dev-certs https --trust
```

### Environment Variables in Docker

Azure OpenAI and other secrets should be passed as environment variables in `docker-compose.yml` or via a `.env` file. The connection string is pre-configured to connect to the `postgres` service.

## Database

### Migrations

EF Core migrations are auto-applied at startup (`ApplicationDbContextInitialiser.InitialiseAsync` calls `MigrateAsync`).

To add a new migration:
```bash
dotnet ef migrations add <MigrationName> --project apps/Dotnet.GenAI.MyCareerAssistant
```

### pgvector Collections

Vector store collections (`IngestedDocument`, `IngestedChunk`) are managed separately from EF Core. They are auto-created by `DataIngestor.IngestDataAsync` via `EnsureCollectionExistsAsync`.

## PDF Data

Place PDF files in `apps/Dotnet.GenAI.MyCareerAssistant/wwwroot/Data/` before running. The ingestion pipeline processes them at startup and re-checks periodically via the background service.

## Production Considerations

- Use Azure Key Vault for secrets (set `AZURE_KEY_VAULT_ENDPOINT`)
- Configure Application Insights for telemetry
- The Dockerfile currently uses .NET 9 base images -- update to .NET 10 for production
- Review MCP tool allowlists in configuration for the production environment
