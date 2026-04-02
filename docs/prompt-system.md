# Prompt System

## Overview

The application uses a template-based prompt system with two distinct prompts: a system prompt for the main conversation and a suggestion prompt for generating follow-up questions.

## System Prompt

### Template Location
`PromptTemplates/system-prompt.md`

### Generation Flow
`SystemPromptGenerator.GenerateSystemPromptAsync`:
1. Reads the template file from disk
2. Replaces owner profile placeholders with values from `SystemPromptSettings` (bound to `SystemPrompt` config section)
3. Fetches all answered Q&A records from the database
4. Replaces `{QuestionAndAnswerSection}` with formatted Q&A pairs

### Placeholders

| Placeholder | Config Key | Purpose |
|-------------|-----------|---------|
| `{OwnerName}` | `SystemPrompt:Owner:Name` | Name used throughout the persona |
| `{OwnerEmail}` | `SystemPrompt:Owner:Email` | Email for notifications |
| `{OwnerGitHubUrl}` | `SystemPrompt:Owner:GitHubUrl` | GitHub profile URL |
| `{OwnerMediumUrl}` | `SystemPrompt:Owner:MediumUrl` | Medium profile URL |
| `{OwnerSessionizeUrl}` | `SystemPrompt:Owner:SessionizeUrl` | Sessionize profile URL |
| `{QuestionAndAnswerSection}` | Database | Dynamic Q&A pairs from `QuestionAndAnswer` table |

Missing values default to "N/A".

### Scenario-Based Tool Routing

The system prompt defines 7 scenarios, each with specific tool-use instructions. The model is instructed to run **only one scenario** per response:

1. **Open-source contributions** -- Use GitHub MCP tools; fallback to Playwright MCP
2. **Blog/writing contributions** -- Use Playwright MCP to navigate Medium
3. **Public speaking/conferences** -- Use Playwright MCP to navigate Sessionize
4. **Career facts (RAG)** -- Use search tool, emit citations in XML format
5. **Unknown questions** -- Check for similar Q&A, save new, email owner
6. **Business inquiries** -- Collect info, check duplicates, save, email owner
7. **Web search** -- Use `web_search` for general online lookup

### Citation Format

For RAG scenarios, the system prompt enforces a strict XML citation format:
```xml
<citation filename='string' page_number='number'>exact quote here</citation>
```

Rules:
- Max 5 words per quote, taken word-for-word from search results
- Citations appear at the end of the response only
- No surrounding text around citation tags
- The `ChatCitation.razor` component parses these and renders clickable PDF viewer links

## Suggestion Prompt

### Template Location
`PromptTemplates/suggestion-prompt.md`

### Usage
`SuggestionPromptGenerator.GenerateSuggestionPromptAsync` reads the template and returns it as-is (no placeholder substitution).

`ChatSuggestions.razor` uses it after each assistant response:
1. Takes system messages + last 5 user/assistant messages (to limit context)
2. Appends the suggestion prompt as a user message
3. Calls `IChatClient.GetResponseAsync<string[]>` for structured output
4. Renders up to 3 follow-up suggestion buttons

### Suggestion Rules (from template)
- Up to 3 suggestions
- Maximum 6 words each
- Phrased as user questions
- Empty list if no good suggestions exist

## Dynamic Q&A Enrichment

The system prompt is regenerated on each new conversation (in `Chat.razor.OnInitializedAsync` and `ResetConversationAsync`). This means newly answered Q&A records are immediately available in subsequent conversations without restart.

The Q&A lifecycle:
1. User asks a question the AI cannot answer
2. AI calls `save_question_record_to_db` and `send_email` to notify the owner
3. Owner answers the question in the database (via direct DB access or admin interface)
4. Next conversation picks up the Q&A pair via `SystemPromptGenerator`
