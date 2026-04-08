targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment (e.g., dev, prod)')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@description('Project name used in resource group naming')
param projectName string = 'personalfinance'

@description('Name for the Document Intelligence resource')
param documentIntelligenceName string = ''

@description('SKU for Document Intelligence')
param documentIntelligenceSku string = 'S0'

@description('Location for Azure OpenAI (may differ from primary location due to model availability)')
param openAiLocation string = 'swedencentral'

@description('Azure OpenAI model deployment name')
param openAiDeploymentName string = 'gpt-4.1'

@description('Azure OpenAI model name')
param openAiModelName string = 'gpt-4.1'

@description('Azure OpenAI model version')
param openAiModelVersion string = '2025-04-14'

@description('Azure SQL administrator login')
param sqlAdminLogin string = 'sqladmin'

@secure()
@description('Azure SQL administrator password')
param sqlAdminPassword string

var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, projectName, environmentName, location))
var tags = { 'azd-env-name': environmentName }

// Resource group
resource rg 'Microsoft.Resources/resourceGroups@2024-11-01' = {
  name: 'rg-${projectName}-${environmentName}'
  location: location
  tags: tags
}

// Document Intelligence
module documentIntelligence './modules/cognitive-services.bicep' = {
  name: 'document-intelligence'
  scope: rg
  params: {
    name: !empty(documentIntelligenceName) ? documentIntelligenceName : '${abbrs.cognitiveServicesAccounts}${resourceToken}'
    location: location
    tags: tags
    kind: 'FormRecognizer'
    sku: documentIntelligenceSku
  }
}

// Azure AI Foundry (Hub + Project + AI Services)
module aiFoundry './modules/ai-foundry.bicep' = {
  name: 'ai-foundry'
  scope: rg
  params: {
    name: '${abbrs.aiFoundry}${resourceToken}'
    location: openAiLocation
    tags: tags
    deploymentName: openAiDeploymentName
    modelName: openAiModelName
    modelVersion: openAiModelVersion
  }
}

// Azure SQL Server + Database
module sqlServer './modules/sql-server.bicep' = {
  name: 'sql-server'
  scope: rg
  params: {
    name: '${abbrs.sqlServers}${resourceToken}'
    location: location
    tags: tags
    adminLogin: sqlAdminLogin
    adminPassword: sqlAdminPassword
    databaseName: 'personalfinancedb'
  }
}

// Log Analytics workspace (required by Container Apps Environment)
module logAnalytics './modules/log-analytics.bicep' = {
  name: 'log-analytics'
  scope: rg
  params: {
    name: '${abbrs.logAnalyticsWorkspaces}${resourceToken}'
    location: location
    tags: tags
  }
}

// Azure Container Registry
module containerRegistry './modules/container-registry.bicep' = {
  name: 'container-registry'
  scope: rg
  params: {
    name: '${abbrs.containerRegistries}${resourceToken}'
    location: location
    tags: tags
  }
}

// Container Apps Environment
module containerAppsEnvironment './modules/container-apps-environment.bicep' = {
  name: 'container-apps-environment'
  scope: rg
  params: {
    name: '${abbrs.containerAppsEnvironments}${resourceToken}'
    location: location
    tags: tags
    logAnalyticsWorkspaceId: logAnalytics.outputs.id
  }
}

// Aspire Dashboard (managed .NET component for observability)
module aspireDashboard './modules/aspire-dashboard.bicep' = {
  name: 'aspire-dashboard'
  scope: rg
  params: {
    containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
  }
}

// Account API Container App
module accountApi './modules/container-app.bicep' = {
  name: 'account-api'
  scope: rg
  params: {
    name: '${abbrs.containerApps}accountapi-${resourceToken}'
    location: location
    tags: union(tags, { 'azd-service-name': 'accountapi' })
    containerAppsEnvironmentId: containerAppsEnvironment.outputs.id
    containerRegistryName: containerRegistry.outputs.name
    containerName: 'accountapi'
    external: false
    env: [
      { name: 'ConnectionStrings__personalfinancedb', secretRef: 'sql-connection-string' }
      { name: 'OTEL_EXPORTER_OTLP_ENDPOINT', value: 'http://aspire-dashboard:18889' }
    ]
    secrets: [
      { name: 'sql-connection-string', value: sqlServer.outputs.connectionString }
    ]
  }
}

// Transaction API Container App
module transactionApi './modules/container-app.bicep' = {
  name: 'transaction-api'
  scope: rg
  params: {
    name: '${abbrs.containerApps}transactionapi-${resourceToken}'
    location: location
    tags: union(tags, { 'azd-service-name': 'transactionapi' })
    containerAppsEnvironmentId: containerAppsEnvironment.outputs.id
    containerRegistryName: containerRegistry.outputs.name
    containerName: 'transactionapi'
    external: false
    env: [
      { name: 'ConnectionStrings__personalfinancedb', secretRef: 'sql-connection-string' }
      { name: 'OTEL_EXPORTER_OTLP_ENDPOINT', value: 'http://aspire-dashboard:18889' }
    ]
    secrets: [
      { name: 'sql-connection-string', value: sqlServer.outputs.connectionString }
    ]
  }
}

// Payment API Container App
module paymentApi './modules/container-app.bicep' = {
  name: 'payment-api'
  scope: rg
  params: {
    name: '${abbrs.containerApps}paymentapi-${resourceToken}'
    location: location
    tags: union(tags, { 'azd-service-name': 'paymentapi' })
    containerAppsEnvironmentId: containerAppsEnvironment.outputs.id
    containerRegistryName: containerRegistry.outputs.name
    containerName: 'paymentapi'
    external: false
    env: [
      { name: 'ConnectionStrings__personalfinancedb', secretRef: 'sql-connection-string' }
      { name: 'OTEL_EXPORTER_OTLP_ENDPOINT', value: 'http://aspire-dashboard:18889' }
    ]
    secrets: [
      { name: 'sql-connection-string', value: sqlServer.outputs.connectionString }
    ]
  }
}

// Agent Backend Container App
module agentBackend './modules/container-app.bicep' = {
  name: 'agent-backend'
  scope: rg
  params: {
    name: '${abbrs.containerApps}agentbackend-${resourceToken}'
    location: location
    tags: union(tags, { 'azd-service-name': 'agentbackend' })
    containerAppsEnvironmentId: containerAppsEnvironment.outputs.id
    containerRegistryName: containerRegistry.outputs.name
    containerName: 'agentbackend'
    env: [
      { name: 'ConnectionStrings__personalfinancedb', secretRef: 'sql-connection-string' }
      { name: 'DocumentIntelligence__Endpoint', value: documentIntelligence.outputs.endpoint }
      { name: 'DocumentIntelligence__ApiKey', secretRef: 'di-api-key' }
      { name: 'AzureOpenAI__Endpoint', value: aiFoundry.outputs.aiServicesEndpoint }
      { name: 'AzureOpenAI__ApiKey', secretRef: 'openai-api-key' }
      { name: 'AzureOpenAI__DeploymentName', value: openAiDeploymentName }
      { name: 'services__accountapi__https__0', value: 'https://${accountApi.outputs.fqdn}' }
      { name: 'services__transactionapi__https__0', value: 'https://${transactionApi.outputs.fqdn}' }
      { name: 'services__paymentapi__https__0', value: 'https://${paymentApi.outputs.fqdn}' }
      { name: 'OTEL_EXPORTER_OTLP_ENDPOINT', value: 'http://aspire-dashboard:18889' }
    ]
    secrets: [
      { name: 'sql-connection-string', value: sqlServer.outputs.connectionString }
      { name: 'di-api-key', value: documentIntelligence.outputs.key }
      { name: 'openai-api-key', value: aiFoundry.outputs.aiServicesKey }
    ]
  }
}

// Frontend Container App (nginx + React SPA)
module frontend './modules/container-app.bicep' = {
  name: 'frontend'
  scope: rg
  params: {
    name: '${abbrs.containerApps}frontend-${resourceToken}'
    location: location
    tags: union(tags, { 'azd-service-name': 'frontend' })
    containerAppsEnvironmentId: containerAppsEnvironment.outputs.id
    containerRegistryName: containerRegistry.outputs.name
    containerName: 'frontend'
    targetPort: 8080
    env: [
      { name: 'BACKEND_URL', value: 'https://${agentBackend.outputs.fqdn}' }
      { name: 'OTEL_EXPORTER_OTLP_ENDPOINT', value: 'http://aspire-dashboard:18889' }
    ]
  }
}

// Outputs consumed by azd and passed to services
output DOCUMENT_INTELLIGENCE_ENDPOINT string = documentIntelligence.outputs.endpoint
output DOCUMENT_INTELLIGENCE_NAME string = documentIntelligence.outputs.name
output DOCUMENT_INTELLIGENCE_RESOURCE_GROUP string = rg.name
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = containerRegistry.outputs.loginServer
output AZURE_CONTAINER_REGISTRY_NAME string = containerRegistry.outputs.name
output AZURE_CONTAINER_APPS_ENVIRONMENT_ID string = containerAppsEnvironment.outputs.id
output AZURE_OPENAI_ENDPOINT string = aiFoundry.outputs.aiServicesEndpoint
output AZURE_OPENAI_DEPLOYMENT_NAME string = aiFoundry.outputs.deploymentName
output AI_FOUNDRY_HUB_ID string = aiFoundry.outputs.aiHubId
output AI_FOUNDRY_PROJECT_ID string = aiFoundry.outputs.aiProjectId
output SQL_SERVER_FQDN string = sqlServer.outputs.fqdn
output FRONTEND_FQDN string = frontend.outputs.fqdn
output ASPIRE_DASHBOARD_NAME string = aspireDashboard.outputs.name
