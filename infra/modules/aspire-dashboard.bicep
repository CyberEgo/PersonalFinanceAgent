param containerAppsEnvironmentName string

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-10-02-preview' existing = {
  name: containerAppsEnvironmentName
}

resource aspireDashboard 'Microsoft.App/managedEnvironments/dotNetComponents@2024-10-02-preview' = {
  parent: containerAppsEnvironment
  name: 'aspire-dashboard'
  properties: {
    componentType: 'AspireDashboard'
  }
}

output name string = aspireDashboard.name
