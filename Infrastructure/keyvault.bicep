param location string = resourceGroup().location
param keyVaultName string = 'kv-bk-${uniqueString(resourceGroup().id)}'
param adminObjectId string 
param functionPrincipalId string

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: { family: 'A', name: 'standard' }
    tenantId: subscription().tenantId
    enableSoftDelete: true
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: adminObjectId
        permissions: { secrets: ['Get', 'List', 'Set', 'Delete'] }
      }
      {
        tenantId: subscription().tenantId
        objectId: functionPrincipalId
        permissions: { secrets: ['Get', 'List'] }
      }
    ]
  }
}
output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri