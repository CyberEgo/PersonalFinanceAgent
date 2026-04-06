# PersonalFinance - AI-Powered Personal Finance Assistant

An intelligent personal finance assistant that shows how to wire up [Microsoft Agent Framework](https://aka.ms/agent-framework), [Model Context Protocol (MCP)](https://modelcontextprotocol.io/), and [.NET Aspire](https://aspire.dev/) with a modern React frontend into a working application you can deploy to Azure.

## Features

- **Multi-agent handoff orchestration** — Triage agent routes to 4 specialist agents (Account, Transaction, Payment, Invoice)
- **Model Context Protocol (MCP)** — Each microservice exposes tools via MCP, keeping agent code decoupled from business logic
- **Real-time dashboard updates** — SSE-based payment event broadcasting refreshes the UI without full page reloads
- **Invoice scanning** — Upload bills/invoices and the AI agent extracts payment details via Azure Document Intelligence
- **Idempotent payment processing** — Deterministic idempotency keys prevent duplicate payments even if the agent retries
- **Aspire orchestration** — Service discovery, health checks, and configuration across all services
- **One-command Azure deployment** — `azd up` provisions and deploys everything

## Architecture

The app is split into several services:

- **[Aspire](https://aspire.dev/)** orchestrates everything (service discovery, health checks, config)
- **AgentBackend** runs the multi-agent AI logic via [Microsoft Agent Framework](https://aka.ms/agent-framework)
- **AccountApi / TransactionApi / PaymentApi** are microservices exposing tools via [MCP](https://modelcontextprotocol.io/)
- **Frontend** is a React SPA with a floating chat widget and financial dashboard
- **Azure OpenAI** provides GPT-4.1 for agent reasoning
- **Azure SQL** stores account, transaction, and payment data

### Agents

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

- [.NET 10](https://dotnet.microsoft.com/download/dotnet/10.0) + [Aspire](https://aspire.dev/) 13.1
- [Microsoft Agent Framework](https://aka.ms/agent-framework) (multi-agent handoff workflows, tool calling)
- [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/ai-extensions) (IChatClient abstraction)
- [Azure OpenAI](https://learn.microsoft.com/azure/ai-services/openai/) (GPT-4.1)
- [Model Context Protocol (MCP)](https://modelcontextprotocol.io/)
- [Azure Document Intelligence](https://learn.microsoft.com/azure/ai-services/document-intelligence/)
- [React 19](https://react.dev/) + [Vite](https://vite.dev/) + [Tailwind CSS v4](https://tailwindcss.com/)

## Getting Started

### 1. Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- [Node.js 20+](https://nodejs.org/)
- [Azure Developer CLI (`azd`)](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd)
- [Azure Subscription](https://azure.microsoft.com/free)

### 2. Clone Repository

```bash
git clone https://github.com/CyberEgo/PersonalFinanceAgent.git
cd PersonalFinanceAgent
```

### 3. Configure Azure OpenAI

Set your Azure OpenAI credentials via user secrets:

```bash
cd src/PersonalFinance.AgentBackend
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4.1"
dotnet user-secrets set "AzureOpenAI:ApiKey" "your-key"
```

Or use `DefaultAzureCredential` by leaving `ApiKey` empty.

### 4. Run Locally

Start all services with .NET Aspire:

```bash
cd src/PersonalFinance.AppHost
dotnet run
```

**What happens next:**

1. Open Aspire Dashboard (URL shown in terminal output)
2. All services start (AgentBackend, AccountApi, TransactionApi, PaymentApi, Frontend)
3. Look for ✅ "Running" status on all resources
4. Click the **frontend** endpoint to open the app

### 5. Deploy to Azure

Deploy the entire application to Azure Container Apps with one command:

```bash
# Login to Azure
azd auth login

# Provision resources and deploy
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

### 6. Clean Up Resources

When finished, remove all Azure resources:

```bash
azd down --force --purge
```

### Azure Resources Provisioned

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
PersonalFinanceAgent/
├── PersonalFinance.sln
├── azure.yaml                            # azd deployment definition
├── infra/                                # Bicep IaC templates
├── src/
│   ├── PersonalFinance.AppHost/          # Aspire orchestrator
│   ├── PersonalFinance.ServiceDefaults/  # Shared Aspire config
│   ├── PersonalFinance.Common/           # Shared models & EF Core entities
│   ├── PersonalFinance.AgentBackend/     # AI agent backend
│   │   ├── Agents/                       # Agent definitions & handoff workflow
│   │   ├── Tools/                        # AI tool plugins (account, transaction, payment, invoice)
│   │   ├── Services/                     # Chat orchestration & payment event broadcasting
│   │   └── Hubs/                         # SSE streaming endpoints
│   ├── PersonalFinance.AccountApi/       # Account microservice + MCP tools
│   ├── PersonalFinance.TransactionApi/   # Transaction microservice + MCP tools
│   ├── PersonalFinance.PaymentApi/       # Payment microservice + MCP tools
│   └── PersonalFinance.Frontend/         # React SPA
└── tests/
    └── PersonalFinance.Tests/
```

## Additional Resources

### Microsoft Agent Framework

- [Framework Documentation](https://aka.ms/agent-framework)
- [Multi-Agent Orchestration](https://learn.microsoft.com/agent-framework/user-guide/workflows/orchestrations/overview)
- [AG-UI Protocol](https://docs.ag-ui.com/introduction)

### Model Context Protocol

- [MCP Specification](https://modelcontextprotocol.io/)
- [MCP Server Registry](https://github.com/modelcontextprotocol/servers)

### Aspire

- [Aspire Documentation](https://aspire.dev/)
- [Integrations](https://aspire.dev/integrations/overview/)
- [Deployment](https://aspire.dev/deployment/overview/)

### Azure

- [Azure OpenAI Service](https://learn.microsoft.com/azure/ai-services/openai/)
- [Azure Document Intelligence](https://learn.microsoft.com/azure/ai-services/document-intelligence/)
- [Azure Container Apps](https://learn.microsoft.com/azure/container-apps/)
- [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/)

## License

This project is provided as-is for educational purposes.
