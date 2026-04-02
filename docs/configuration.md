# Configuration

## Overview

The application uses a layered configuration system: `appsettings.json` for defaults, user secrets for local development, environment variables for containers, and optionally Azure Key Vault for production.

## Configuration Sources (in priority order)

1. **Azure Key Vault** -- loaded if `AZURE_KEY_VAULT_ENDPOINT` is set (uses `DefaultAzureCredential`)
2. **Environment variables** -- used in Docker Compose and Aspire
3. **User secrets** -- for local development (UserSecretsId: `fab3a36e-2346-42cf-8804-6d7edf07c5fc`)
4. **appsettings.Production.json** -- production overrides
5. **appsettings.json** -- default values

## Required Settings

### Azure OpenAI

| Key | Description |
|-----|-------------|
| `AzureOpenAI:Endpoint` | Azure OpenAI service endpoint URL |
| `AzureOpenAI:Key` | Azure OpenAI API key |
| `AzureOpenAI:ChatModelDeploymentName` | Deployment name for the chat model |
| `AzureOpenAI:EmbeddingModelDeploymentName` | Deployment name for the embedding model (e.g., text-embedding-ada-002) |

### Database

| Key | Description |
|-----|-------------|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string. Must point to a pgvector-enabled instance. Default: `Server=localhost;Port=5432;Database=MyCareerAssistantDb;Username=postgres;Password=postgres;Include Error Detail=true` |

## Optional Settings

### Owner Profile (System Prompt)

Configured under `SystemPrompt:Owner`. These values are injected into the system prompt template.

| Key | Description |
|-----|-------------|
| `SystemPrompt:Owner:Name` | Owner's name (used as AI persona) |
| `SystemPrompt:Owner:Email` | Owner's email (for notifications) |
| `SystemPrompt:Owner:GitHubUrl` | GitHub profile URL |
| `SystemPrompt:Owner:MediumUrl` | Medium profile URL |
| `SystemPrompt:Owner:SessionizeUrl` | Sessionize profile URL |

### External Services

| Key | Description |
|-----|-------------|
| `GitHubPat` | GitHub Personal Access Token for MCP GitHub tools. If empty, GitHub tools are not loaded. |
| `EmailSender:ApiKey` | SendGrid API key |
| `EmailSender:SenderEmail` | From email address |
| `EmailSender:SenderName` | From display name |
| `Serper:ApiKey` | Serper.dev API key for web search |

### Observability

| Key | Description |
|-----|-------------|
| `ApplicationInsights:ConnectionString` or `APPLICATIONINSIGHTS_CONNECTION_STRING` | Azure Application Insights connection string. If not set, OpenTelemetry exports to console. |
| `ApplicationInsights:CloudRoleName` | Service name in App Insights (default: `Dotnet.GenAI.MyCareerAssistant`) |

### Background Services

| Key | Description |
|-----|-------------|
| `BackgroundServiceTimespanMin:DataIngestion` | Interval in minutes between ingestion runs (default: 1) |

### MCP Tool Allowlists

`AllowedGitHubTools` and `AllowedPlaywrightTools` arrays in `appsettings.json` whitelist which MCP tools are exposed to the AI model. Only tools listed in these arrays are registered.

## Settings Classes

| Class | Config Section | Description |
|-------|---------------|-------------|
| `SystemPromptSettings` | `SystemPrompt` | Owner profile for prompt generation |
| `EmailSenderSettings` | `EmailSender` | SendGrid configuration |
| `SerperSettings` | `Serper` | Serper API configuration |

## Running Without Optional Services

- Without `GitHubPat` -- GitHub MCP tools are skipped; Playwright is still available
- Without `EmailSender:ApiKey` -- `send_email` tool throws at runtime if invoked
- Without `Serper:ApiKey` -- `web_search` tool will fail if invoked
- Without `ApplicationInsights:ConnectionString` -- telemetry exports to console
