@minLength(3)
@description('Base name for AI Services resources')
param name string

@description('Location for the resources')
param location string = resourceGroup().location

@description('Tags for the resources')
param tags object = {}

@description('Model deployment name')
param deploymentName string

@description('Model name')
param modelName string

@description('Model version')
param modelVersion string

@description('SKU for AI Services')
param aiServicesSku string = 'S0'

// --- AI Services account (hosts OpenAI models) ---
resource aiServices 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: '${name}-aiservices'
  location: location
  tags: tags
  kind: 'AIServices'
  sku: {
    name: aiServicesSku
  }
  properties: {
    customSubDomainName: '${name}-aiservices'
    publicNetworkAccess: 'Enabled'
  }
}

resource modelDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: aiServices
  name: deploymentName
  sku: {
    name: 'Standard'
    capacity: 10
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: modelName
      version: modelVersion
    }
  }
}

output aiServicesId string = aiServices.id
output aiServicesName string = aiServices.name
output aiServicesEndpoint string = aiServices.properties.endpoint
#disable-next-line outputs-should-not-contain-secrets
output aiServicesKey string = aiServices.listKeys().key1
output deploymentName string = modelDeployment.name
