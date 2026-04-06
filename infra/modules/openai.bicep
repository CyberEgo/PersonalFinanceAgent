@description('Name of the Azure OpenAI account')
param name string

@description('Location for the resource')
param location string = resourceGroup().location

@description('Tags for the resource')
param tags object = {}

@description('SKU name')
param sku string = 'S0'

@description('Model deployment name')
param deploymentName string

@description('Model name')
param modelName string

@description('Model version')
param modelVersion string

resource openAi 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: name
  location: location
  tags: tags
  kind: 'OpenAI'
  sku: {
    name: sku
  }
  properties: {
    customSubDomainName: name
    publicNetworkAccess: 'Enabled'
  }
}

resource deployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: openAi
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

output id string = openAi.id
output name string = openAi.name
output endpoint string = openAi.properties.endpoint
output key string = openAi.listKeys().key1
output deploymentName string = deployment.name
