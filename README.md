# PersonalFinance - AI-Powered Personal Finance Assistant

An intelligent personal finance assistant built with .NET Aspire, Microsoft Agent Framework, and a modern React frontend.

## Architecture

- **PersonalFinance.AppHost** - .NET Aspire orchestrator managing all services
- **PersonalFinance.AgentBackend** - Multi-agent AI backend using Microsoft Agent Framework
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
- Microsoft Agent Framework (multi-agent handoff workflows, tool calling)
- Microsoft.Extensions.AI (IChatClient abstraction)
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

### Deploy to Azure

The project includes full Infrastructure as Code (Bicep) and is ready to deploy with the [Azure Developer CLI (`azd`)](https://learn.microsoft.com/azure/developer/azure-developer-cli/overview).

#### Prerequisites

- [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd) installed
- An Azure subscription

#### Provision and Deploy

```bash
# Provision infrastructure and deploy all services in one step
azd up
```

This will:
1. Prompt you for a subscription, location, and environment name
2. Provision all Azure resources (Container Apps, Azure SQL, Azure OpenAI, Container Registry, Log Analytics, etc.)
3. Build Docker images for all 5 services
4. Deploy them to Azure Container Apps

#### Deploy Individual Services

After the initial `azd up`, you can redeploy individual services without re-provisioning:

```bash
azd deploy agentbackend   # AI agent backend
azd deploy frontend       # React frontend
azd deploy accountapi     # Account API
azd deploy transactionapi # Transaction API
azd deploy paymentapi     # Payment API
```

#### Other Useful Commands

```bash
azd env list              # List environments
azd env get-values        # Show all environment variables
azd monitor               # Open Azure Monitor dashboard
azd down                  # Tear down all Azure resources
```

#### Azure Resources Provisioned

| Resource | Purpose |
|----------|---------|
| Azure Container Apps Environment | Hosts all 5 microservices |
| Azure Container Registry | Stores Docker images |
| Azure SQL Database | Account, transaction, and payment data |
| Azure OpenAI | GPT-4.1 for agent reasoning |
| Azure AI Services | Document Intelligence for invoice scanning |
| Log Analytics Workspace | Centralized logging and monitoring |

## Project Structure

```
personal-finance-agent/
├── PersonalFinance.sln
├── src/
│   ├── PersonalFinance.AppHost/          # Aspire orchestrator
│   ├── PersonalFinance.ServiceDefaults/  # Shared Aspire config
│   ├── PersonalFinance.Common/           # Shared models
│   ├── PersonalFinance.AgentBackend/     # AI agent backend
│   │   ├── Agents/                    # Agent definitions & handoff workflow
│   │   ├── Tools/                     # AI tool plugins (account, transaction, payment, invoice)
│   │   ├── Services/                  # Chat orchestration & payment event broadcasting
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
