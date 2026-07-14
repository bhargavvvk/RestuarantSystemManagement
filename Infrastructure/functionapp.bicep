param location string = resourceGroup().location
param functionAppName string = 'func-restapi-${uniqueString(resourceGroup().id)}'
param storageAccountName string
param keyVaultUri string

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

// 1. ADD THIS: The Hosting Plan definition
resource hostingPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${functionAppName}-plan'
  location: location
  sku: { name: 'Y1', tier: 'Dynamic' }
  kind: 'functionapp'
  properties: { reserved: true }
}

// 2. Updated Function App referencing the hostingPlan
resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp,linux'
  identity: { type: 'SystemAssigned' }
  properties: {
    serverFarmId: hostingPlan.id // Now this reference will work!
    httpsOnly: true
    siteConfig: {
      appSettings: [
        { name: 'FUNCTIONS_WORKER_RUNTIME', value: 'dotnet-isolated' }
        { name: 'FUNCTIONS_EXTENSION_VERSION', value: '~4' }
        { name: 'AzureWebJobsStorage', value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}' }
        { name: 'ConnectionStrings__Default', value: '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/ConnectionStrings--Default)' }
        { name: 'ConnectionStrings__Archive', value: '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/ConnectionStrings--Archive)' }
        { name: 'Jwt__Key', value: '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/Jwt--Key)' }
      ]
    }
  }
}
output functionPrincipalId string = functionApp.identity.principalId
output functionAppName string = functionApp.name