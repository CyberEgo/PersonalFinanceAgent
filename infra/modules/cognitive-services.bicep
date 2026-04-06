@description('Name of the Cognitive Services account')
param name string

@description('Location for the resource')
param location string = resourceGroup().location

@description('Tags for the resource')
param tags object = {}

@description('Kind of Cognitive Services account (e.g., FormRecognizer, OpenAI)')
param kind string

@description('SKU name')
param sku string = 'S0'

resource cognitiveServices 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: name
  location: location
  tags: tags
  kind: kind
  sku: {
    name: sku
  }
  properties: {
    customSubDomainName: name
    publicNetworkAccess: 'Enabled'
  }
}

output id string = cognitiveServices.id
output name string = cognitiveServices.name
output endpoint string = cognitiveServices.properties.endpoint
output key string = cognitiveServices.listKeys().key1
