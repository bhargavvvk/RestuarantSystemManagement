@description('Location for the App Service')
param location string = resourceGroup().location

@description('App Service name')
param appServiceName string = 'bk-restaurant-api'

@description('App Service Plan name')
param appServicePlanName string = 'restaurant-api-plan'

@description('Key Vault URI')
param keyVaultUri string

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location

  sku: {
    name: 'B1'
    tier: 'Basic'
  }

  kind: 'linux'

  properties: {
    reserved: true
  }
}

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: appServiceName
  location: location

  kind: 'app,linux'

  identity: {
    type: 'SystemAssigned'
  }

  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true

    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'

      appSettings: [
        {
          name: 'KeyVaultUri'
          value: keyVaultUri
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
      ]
    }
  }
}

output appServiceName string = webApp.name

output principalId string = webApp.identity.principalId

output defaultHostName string = webApp.properties.defaultHostName