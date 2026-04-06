# PersonalFinance - AI-Powered Personal Finance Assistant

An intelligent personal finance assistant built with .NET Aspire, Semantic Kernel agents, and a modern React frontend.

## Architecture

- **PersonalFinance.AppHost** - .NET Aspire orchestrator managing all services
- **PersonalFinance.AgentBackend** - Multi-agent AI backend using Semantic Kernel
- **PersonalFinance.AccountApi** - Account management microservice with MCP tools
- **PersonalFinance.TransactionApi** - Transaction history microservice with MCP tools
- **PersonalFinance.PaymentApi** - Payment processing microservice with MCP tools
- **PersonalFinance.Common** - Shared models and DTOs
- **PersonalFinance.Frontend** - React + Vite + Tailwind CSS dark-themed UI

## Agents

The backend uses a **triage + specialist** multi-agent pattern rather than a single monolithic agent. This design offers several advantages:

- **Focused tool sets** — Each specialist agent only has access to the tools it needs (e.g., Account Agent can't initiate payments). This reduces hallucinations and improves accuracy since the LLM has fewer irrelevant tools to reason about.
- **Lower token costs** — Smaller system prompts and fewer tool definitions per agent mean fewer tokens per request.
- **Better routing** — The Triage Agent acts as a lightweight classifier. Once it picks a specialist, that specialist **sticks** to the conversation thread (sticky routing), avoiding repeated classification overhead.
- **Separation of concerns** — Each agent's behavior and instructions can be tuned independently without affecting the others.

| Agent | Purpose | Tools |
|-------|---------|-------|
| **Triage Agent** | Classifies user intent, routes to the right specialist | None (pure LLM classification) |
| **Account Agent** | Account info, balances, cards, beneficiaries | 5 tools via Account API |
| **Transaction Agent** | Transaction history, search, filtering | 5 tools via Transaction API |
| **Payment Agent** | Payment processing, invoice scanning | 6 tools via Payment API + Document Intelligence |

## Tech Stack

- .NET 10 + Aspire 13.1
- Semantic Kernel (agents, tool calling)
- Azure OpenAI (GPT-4.1)
- Model Context Protocol (MCP)
- Azure Document Intelligence
- React 19 + Vite + Tailwind CSS v4

## Getting Started

### Prerequisites

- .NET 10 SDK
- Node.js 20+
- Azure OpenAI resource (or API key)

### Run Locally

```bash
# From the repo root
cd src/PersonalFinance.AppHost
dotnet run
```

This starts all services via Aspire. The dashboard opens at `https://localhost:17225`.

### Configure Azure OpenAI

Set your Azure OpenAI credentials via user secrets:

```bash
cd src/PersonalFinance.AgentBackend
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4.1"
dotnet user-secrets set "AzureOpenAI:ApiKey" "your-key"
```

Or use DefaultAzureCredential by leaving ApiKey empty.

## Project Structure

```
personal-finance-agent/
├── PersonalFinance.sln
├── src/
│   ├── PersonalFinance.AppHost/          # Aspire orchestrator
│   ├── PersonalFinance.ServiceDefaults/  # Shared Aspire config
│   ├── PersonalFinance.Common/           # Shared models
│   ├── PersonalFinance.AgentBackend/     # AI agent backend
│   │   ├── Agents/                    # Agent definitions
│   │   ├── Tools/                     # SK plugins (account, transaction, payment, invoice)
│   │   ├── Services/                  # Chat orchestration
│   │   └── Hubs/                      # SSE streaming endpoints
│   ├── PersonalFinance.AccountApi/       # Account microservice
│   ├── PersonalFinance.TransactionApi/   # Transaction microservice
│   ├── PersonalFinance.PaymentApi/       # Payment microservice
│   └── PersonalFinance.Frontend/         # React SPA
└── tests/
    └── PersonalFinance.Tests/
```

## License

This project is provided as-is for educational purposes.
