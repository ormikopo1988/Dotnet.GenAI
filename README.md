## Overview

`Dotnet.GenAI` is a comprehensive .NET repository demonstrating modern AI/GenAI technologies. It contains complete applications and educational demos showcasing how to build intelligent systems using Azure OpenAI, semantic search, and vector databases.

## Repository Structure

### apps -- Complete Applications

#### **Dotnet.GenAI.MyCareerAssistant**
A full-stack Blazor web application that serves as an AI-powered career assistant chatbot. Key features include:

- **Intelligent Chat UI** -- Interactive Blazor components with real-time message streaming
- **Vector-Based Semantic Search** -- Ingests PDF documents into a PostgreSQL vector store (pgvector) for intelligent retrieval
- **Multi-Tool Integration** -- Supports GitHub, Medium, Sessionize, and web search via MCP tools
- **Citation & Attribution** -- Displays source citations with PDF viewer integration
- **System Prompt Framework** -- Dynamic prompt generation based on ingested Q&A data and document context
- **Database & Migrations** -- EF Core with pgvector support for embedding storage and retrieval
- **Docker & Aspire Support** -- Multi-container deployment with PostgreSQL, pgAdmin, and local development orchestration

**Key Files:**
- `Program.cs` -- Startup, service registration, and data ingestion
- `Chat.razor` -- Main chat component
- `PDFDirectorySource.cs` -- PDF ingestion pipeline
- `system-prompt.md` -- Prompt templates and tool scenarios
- `Dockerfile` -- Containerized deployment

**Technology Stack:** Blazor, .NET, Azure OpenAI, pgvector, PostgreSQL, DOMPurify, marked.js, PDF.js

### demos -- Educational Examples

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
- `GithubClient.cs` -- GitHub API wrapper for user/repo data
- `InvoiceApiClient.cs` -- Sample REST API client
- `HostConfig.cs` -- Configuration helpers for AI services and vector stores
- `DocumentationClient.cs` -- Local documentation lookup

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

## Local Development Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/products/docker-desktop/) (for PostgreSQL container or Aspire/Compose hosting)
- [Node.js](https://nodejs.org/) (required for Playwright MCP server)
- An [Azure OpenAI](https://azure.microsoft.com/products/ai-services/openai-service) resource with a **chat model** and an **embedding model** deployment

### Option 1: .NET Aspire (recommended)

The easiest path -- Aspire auto-provisions PostgreSQL (pgvector), pgAdmin, and wires the connection string. You only need to configure Azure OpenAI secrets.

**1. Set Azure OpenAI secrets on the AppHost project:**

```bash
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com/" --project apps/Dotnet.GenAI.MyCareerAssistant
dotnet user-secrets set "AzureOpenAI:Key" "your-key" --project apps/Dotnet.GenAI.MyCareerAssistant
dotnet user-secrets set "AzureOpenAI:ChatModelDeploymentName" "your-chat-deployment" --project apps/Dotnet.GenAI.MyCareerAssistant
dotnet user-secrets set "AzureOpenAI:EmbeddingModelDeploymentName" "your-embedding-deployment" --project apps/Dotnet.GenAI.MyCareerAssistant
```

**2. Run:**

```bash
dotnet run --project apps/Dotnet.GenAI.MyCareerAssistant.AppHost
```

Aspire provisions PostgreSQL (pgvector:pg17) on startup, applies EF Core migrations, ingests PDFs from `wwwroot/Data/`, and opens the Aspire dashboard. pgAdmin is available on port 82.

### Option 2: Direct (dotnet run)

Run the app directly against an external PostgreSQL instance.

**1. Start PostgreSQL with pgvector:**

```bash
docker run -d --name postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=MyCareerAssistantDb -p 5432:5432 pgvector/pgvector:pg17
```

Optionally add pgAdmin for database management:

```bash
docker network create pgnetwork
docker run -d --name postgres --network pgnetwork -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=MyCareerAssistantDb -p 5432:5432 pgvector/pgvector:pg17
docker run -d --name my-pgadmin --network pgnetwork -e 'PGADMIN_DEFAULT_EMAIL=admin@example.com' -e 'PGADMIN_DEFAULT_PASSWORD=root' -p 82:80 dpage/pgadmin4
```

**2. Configure secrets:**

```bash
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com/" --project apps/Dotnet.GenAI.MyCareerAssistant
dotnet user-secrets set "AzureOpenAI:Key" "your-key" --project apps/Dotnet.GenAI.MyCareerAssistant
dotnet user-secrets set "AzureOpenAI:ChatModelDeploymentName" "your-chat-deployment" --project apps/Dotnet.GenAI.MyCareerAssistant
dotnet user-secrets set "AzureOpenAI:EmbeddingModelDeploymentName" "your-embedding-deployment" --project apps/Dotnet.GenAI.MyCareerAssistant
```

The default connection string in `appsettings.json` points to `localhost:5432` with `postgres/postgres` credentials, so no additional database configuration is needed if you used the command above.

**3. Run:**

```bash
dotnet run --project apps/Dotnet.GenAI.MyCareerAssistant
```

### Option 3: Docker Compose

Compose provisions the app, PostgreSQL (pgvector), and pgAdmin in a single command.

**1. Generate an HTTPS development certificate:**

```bash
dotnet dev-certs https -ep ${USERPROFILE}/.aspnet/https/aspnetapp.pfx -p Test1234!
dotnet dev-certs https --trust
```

**2. Configure secrets:**

Pass Azure OpenAI settings as environment variables in `docker-compose.yml` or create a `.env` file in the `apps/` directory. The PostgreSQL connection string is pre-configured in the compose file.

**3. Run:**

```bash
docker compose -f apps/docker-compose.yml up --build
```

The app is available on port 5000 (HTTP) and 5001 (HTTPS). pgAdmin is on port 82.

### Optional Services

These are not required to run the app -- features degrade gracefully when not configured:

| Secret | Enables |
|--------|---------|
| `GitHubPat` | GitHub MCP tools (repo search, issue listing, etc.) |
| `EmailSender:ApiKey` / `SenderEmail` / `SenderName` | Email sending via SendGrid |
| `Serper:ApiKey` | Web search tool |
| `SystemPrompt:Owner:Name` / `Email` / `GitHubUrl` / `MediumUrl` / `SessionizeUrl` | Personalized system prompt (placeholders stay empty otherwise) |

### PDF Data

Place PDF files in `apps/Dotnet.GenAI.MyCareerAssistant/wwwroot/Data/` before starting the app. The ingestion pipeline processes them at startup and re-checks periodically via a background service.

### Database Migrations

EF Core migrations are auto-applied at startup. To add a new migration:

```bash
dotnet ef migrations add <MigrationName> --project apps/Dotnet.GenAI.MyCareerAssistant
```

## Deploy to Azure

The app can be deployed to Azure using the Azure Developer CLI (`azd`) with .NET Aspire, or manually via Azure Container Apps / Azure App Service.

### Option 1: Azure Developer CLI with Aspire (recommended)

The AppHost already defines Azure-compatible resources (Application Insights, PostgreSQL). Use `azd` to provision and deploy:

**1. Install the [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd).**

**2. Initialize the project:**

```bash
azd init
```

Select the Aspire AppHost project when prompted (`apps/Dotnet.GenAI.MyCareerAssistant.AppHost`).

**3. Provision infrastructure and deploy:**

```bash
azd up
```

This provisions an Azure Container Apps environment, Azure Database for PostgreSQL Flexible Server (with pgvector), Application Insights, and deploys the app. You will be prompted for a subscription and region.

**4. Configure secrets in the deployed environment:**

After provisioning, set the Azure OpenAI and optional service secrets as environment variables on the Container App, either through the Azure Portal or via the CLI:

```bash
az containerapp update \
  --name <app-name> \
  --resource-group <resource-group> \
  --set-env-vars \
    "AzureOpenAI__Endpoint=https://your-resource.openai.azure.com/" \
    "AzureOpenAI__Key=your-key" \
    "AzureOpenAI__ChatModelDeploymentName=your-chat-deployment" \
    "AzureOpenAI__EmbeddingModelDeploymentName=your-embedding-deployment"
```

For production, use Azure Key Vault instead of environment variables. Set the `AZURE_KEY_VAULT_ENDPOINT` environment variable on the Container App, and the app will load secrets from Key Vault automatically using `DefaultAzureCredential`.

**5. Subsequent deployments:**

```bash
azd deploy
```

### Option 2: Azure Container Apps (manual)

**1. Create Azure resources:**

- **Azure Container Apps environment** -- to host the app container
- **Azure Database for PostgreSQL Flexible Server** -- enable the `vector` extension for pgvector support
- **Azure OpenAI resource** -- with chat and embedding model deployments
- **Azure Container Registry** -- to store the Docker image
- **Azure Key Vault** (recommended) -- for secret management
- **Application Insights** (optional) -- for telemetry

**2. Build and push the Docker image:**

```bash
docker build -t <your-acr>.azurecr.io/mycareerassistant:latest -f apps/Dotnet.GenAI.MyCareerAssistant/Dockerfile apps/
az acr login --name <your-acr>
docker push <your-acr>.azurecr.io/mycareerassistant:latest
```

**3. Deploy the Container App:**

```bash
az containerapp create \
  --name mycareerassistant \
  --resource-group <resource-group> \
  --environment <container-app-env> \
  --image <your-acr>.azurecr.io/mycareerassistant:latest \
  --target-port 80 \
  --ingress external \
  --env-vars \
    "ConnectionStrings__DefaultConnection=Host=<pg-host>;Port=5432;Database=MyCareerAssistantDb;Username=<user>;Password=<password>;Ssl Mode=Require" \
    "AzureOpenAI__Endpoint=https://your-resource.openai.azure.com/" \
    "AzureOpenAI__Key=your-key" \
    "AzureOpenAI__ChatModelDeploymentName=your-chat-deployment" \
    "AzureOpenAI__EmbeddingModelDeploymentName=your-embedding-deployment" \
    "AZURE_KEY_VAULT_ENDPOINT=https://your-vault.vault.azure.net/"
```

### Production Considerations

- **Secrets**: Use Azure Key Vault for all sensitive configuration. The app loads secrets automatically when `AZURE_KEY_VAULT_ENDPOINT` is set.
- **Database**: Enable the `vector` extension on Azure PostgreSQL Flexible Server (`CREATE EXTENSION IF NOT EXISTS vector;`). EF Core migrations are auto-applied at startup.
- **Telemetry**: Set `APPLICATIONINSIGHTS_CONNECTION_STRING` to enable OpenTelemetry export to Azure Monitor.
- **MCP tools**: Review the `AllowedGitHubTools` and `AllowedPlaywrightTools` allowlists in configuration for the production environment.
- **Dockerfile**: The current Dockerfile uses .NET 9 base images -- update to .NET 10 for production.
