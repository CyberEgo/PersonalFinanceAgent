var builder = DistributedApplication.CreateBuilder(args);

// SQL Server database
var sql = builder.AddSqlServer("sql")
    .AddDatabase("personalfinancedb");

// Azure OpenAI configuration (provided via user-secrets locally, or azd env for cloud)
var aoaiEndpoint = builder.AddParameter("AzureOpenAIEndpoint", secret: false);
var aoaiDeployment = builder.AddParameter("AzureOpenAIDeploymentName", secret: false);
var aoaiApiKey = builder.AddParameter("AzureOpenAIApiKey", secret: true);

// Document Intelligence configuration (provided via user-secrets locally, or azd env for cloud)
var diEndpoint = builder.AddParameter("DocumentIntelligenceEndpoint", secret: false);
var diApiKey = builder.AddParameter("DocumentIntelligenceApiKey", secret: true);

// Business API microservices
var accountApi = builder.AddProject<Projects.PersonalFinance_AccountApi>("accountapi")
    .WithReference(sql)
    .WaitFor(sql)
    .WithExternalHttpEndpoints();

var transactionApi = builder.AddProject<Projects.PersonalFinance_TransactionApi>("transactionapi")
    .WithReference(sql)
    .WaitFor(sql)
    .WithExternalHttpEndpoints();

var paymentApi = builder.AddProject<Projects.PersonalFinance_PaymentApi>("paymentapi")
    .WithReference(sql)
    .WaitFor(sql)
    .WithExternalHttpEndpoints();

// Agent Backend - depends on all business APIs
var agentBackend = builder.AddProject<Projects.PersonalFinance_AgentBackend>("agentbackend")
    .WithReference(accountApi)
    .WithReference(transactionApi)
    .WithReference(paymentApi)
    .WithReference(sql)
    .WaitFor(sql)
    .WithEnvironment("AzureOpenAI__Endpoint", aoaiEndpoint)
    .WithEnvironment("AzureOpenAI__DeploymentName", aoaiDeployment)
    .WithEnvironment("AzureOpenAI__ApiKey", aoaiApiKey)
    .WithEnvironment("DocumentIntelligence__Endpoint", diEndpoint)
    .WithEnvironment("DocumentIntelligence__ApiKey", diApiKey)
    .WithExternalHttpEndpoints()
    .WithUrlForEndpoint("https", url => url.DisplayText = "API")
    .WithUrlForEndpoint("http", url => url.DisplayText = "API")
    .WithUrlForEndpoint("https", ep => new() { Url = "/devui/", DisplayText = "Dev UI" })
    .WithUrlForEndpoint("http", ep => new() { Url = "/devui/", DisplayText = "Dev UI" })
    .WaitFor(accountApi)
    .WaitFor(transactionApi)
    .WaitFor(paymentApi);

// Frontend - Vite React app
builder.AddViteApp("frontend", "../PersonalFinance.Frontend")
    .WithReference(agentBackend)
    .WithHttpEndpoint(port: 5173, name: "vite", env: "PORT")
    .WithExternalHttpEndpoints();

builder.Build().Run();
