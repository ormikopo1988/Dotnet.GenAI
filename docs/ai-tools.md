# AI Tools

## Overview

The application provides the AI model with a rich set of tools from three sources. These tools are merged into `ChatOptions.Tools` and made available during chat interactions.

## Tool Sources

### 1. Built-in Function Tools (FunctionRegistry.cs)

Created via `AIFunctionFactory.Create` using reflection on service interface methods. Each tool is registered with a descriptive name and description that guides the AI model's tool selection.

| Tool Name | Service | Description |
|-----------|---------|-------------|
| `save_question_record_to_db` | `IQuestionAndAnswerService.CreateAsync` | Saves unanswered questions to the database for the owner to answer later |
| `get_semantically_similar_question_record_from_db` | `IQuestionAndAnswerService.GetBySemanticMeaningAsync` | Checks for semantically similar existing questions to avoid duplicates |
| `save_business_inquiry_record_to_db` | `IBusinessInquiryService.CreateAsync` | Saves business inquiries with name, email, and request |
| `get_semantically_similar_business_inquiry_record_from_db` | `IBusinessInquiryService.GetBySemanticMeaningAsync` | Checks for similar existing inquiries per email to avoid duplicates |
| `send_email` | `EmailSender.SendEmailAsync` | Sends HTML-formatted email via SendGrid |
| `web_search` | `SerperClient.WebSearchAsync` | Performs web search via Serper API |

### 2. MCP (Model Context Protocol) Tools

External tool servers connected at startup via `McpClientInitializer`:

**Playwright MCP** (`npx @playwright/mcp@latest`):
- Browser automation for navigating Medium, Sessionize, and other web pages
- Runs with `--no-sandbox --isolated` flags
- Tools filtered by `AllowedPlaywrightTools` in `appsettings.json`

**GitHub MCP** (GitHub Copilot MCP endpoint):
- GitHub API access for repository, issue, PR, and code search operations
- Requires `GitHubPat` configuration
- Connected via HTTP transport to `https://api.githubcopilot.com/mcp/`
- Tools filtered by `AllowedGitHubTools` in `appsettings.json`

### 3. Semantic Search Tool (Chat.razor)

Registered inline in the Chat component at initialization:
- `SearchAsync` -- wraps `SemanticSearch.SearchAsync` to query the pgvector store
- Parameters: `searchPhrase` (required), `filenameFilter` (optional)
- Returns up to 5 results formatted as XML `<result>` elements with filename and page number attributes
- The AI model uses this for RAG-style retrieval from ingested PDF content

## Tool Invocation Flow

```
User message -> IChatClient.GetStreamingResponseAsync
  |
  v
Azure OpenAI model decides which tool(s) to invoke
  |
  v
ChatClientBuilder's FunctionInvocation middleware
  |-- Resolves the tool from ChatOptions.Tools
  |-- Invokes the tool function
  |-- Returns results to the model
  |
  v
Model generates final response (potentially with citations)
```

## System Prompt Tool Scenarios

The system prompt (`PromptTemplates/system-prompt.md`) defines 7 distinct scenarios that guide tool usage:

1. **Open-source contributions** -- GitHub MCP tools, fallback to Playwright
2. **Blog/writing contributions** -- Playwright MCP to navigate Medium
3. **Public speaking/conferences** -- Playwright MCP to navigate Sessionize
4. **Career/education facts** -- Search tool (RAG) with citation output
5. **Unknown questions** -- Check for similar Q&A -> save new question -> email owner
6. **Business inquiries** -- Collect details -> check duplicates -> save -> email owner
7. **Web search** -- `web_search` tool for general facts
