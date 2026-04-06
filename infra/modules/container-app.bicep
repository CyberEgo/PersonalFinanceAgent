param name string
param location string
param tags object = {}
param containerAppsEnvironmentId string
param containerRegistryName string
param containerName string = 'main'
param env array = []
param secrets array = []
param external bool = true
param targetPort int = 8080

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: containerRegistryName
}

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    managedEnvironmentId: containerAppsEnvironmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: external
        targetPort: targetPort
        transport: 'http'
      }
      registries: [
        {
          server: containerRegistry.properties.loginServer
          username: containerRegistry.listCredentials().username
          passwordSecretRef: 'registry-password'
        }
      ]
      secrets: union(
        [
          {
            name: 'registry-password'
            value: containerRegistry.listCredentials().passwords[0].value
          }
        ],
        secrets
      )
    }
    template: {
      containers: [
        {
          name: containerName
          image: 'mcr.microsoft.com/dotnet/samples:aspnetapp'
          resources: {
            cpu: json('0.5')
            memory: '1.0Gi'
          }
          env: env
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 1
      }
    }
  }
}

output id string = containerApp.id
output name string = containerApp.name
output fqdn string = containerApp.properties.configuration.ingress.fqdn
