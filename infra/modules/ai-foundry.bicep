@minLength(3)
@description('Base name for AI Foundry resources')
param name string

@description('Location for the resources')
param location string = resourceGroup().location

@description('Tags for the resources')
param tags object = {}

@description('AI Services resource ID (created separately to avoid RP race conditions)')
param aiServicesId string

@description('AI Services endpoint')
param aiServicesEndpoint string

@secure()
@description('AI Services API key')
param aiServicesKey string

// --- Storage Account (required by AI Hub) ---
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: take('aifst${replace(name, '-', '')}', 24)
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
  }
}

// --- Key Vault (required by AI Hub) ---
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${name}-kv'
  location: location
  tags: tags
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enableSoftDelete: true
  }
}

// --- AI Foundry Hub ---
resource aiHub 'Microsoft.MachineLearningServices/workspaces@2024-10-01' = {
  name: '${name}-hub'
  location: location
  tags: tags
  kind: 'Hub'
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    friendlyName: '${name}-hub'
    storageAccount: storageAccount.id
    keyVault: keyVault.id
    publicNetworkAccess: 'Enabled'
  }
}

// --- Hub connection to AI Services ---
resource aiServicesConnection 'Microsoft.MachineLearningServices/workspaces/connections@2024-10-01' = {
  parent: aiHub
  name: '${name}-aiservices-connection'
  properties: {
    category: 'AIServices'
    authType: 'ApiKey'
    target: aiServicesEndpoint
    isSharedToAll: true
    credentials: {
      key: aiServicesKey
    }
    metadata: {
      ApiType: 'Azure'
      ResourceId: aiServicesId
    }
  }
}

// --- AI Foundry Project ---
resource aiProject 'Microsoft.MachineLearningServices/workspaces@2024-10-01' = {
  name: '${name}-project'
  location: location
  tags: tags
  kind: 'Project'
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    friendlyName: '${name}-project'
    hubResourceId: aiHub.id
  }
  dependsOn: [
    aiServicesConnection
  ]
}

output aiHubId string = aiHub.id
output aiProjectId string = aiProject.id
