param location string = resourceGroup().location
param adminObjectId string 

// 1. Deploy Storage (No dependencies)
module storage 'storage.bicep' = {
  name: 'storage'
  params: { location: location }
}

// 2. Deploy Key Vault (Needs Admin ID, but identity policy is handled later)
module keyvault 'keyvault.bicep' = {
  name: 'keyvault'
  params: {
    location: location
    adminObjectId: adminObjectId
    functionPrincipalId: '00000000-0000-0000-0000-000000000000' // Placeholder
  }
}

// 3. Deploy Function App
module function 'functionapp.bicep' = {
  name: 'functionapp'
  params: {
    location: location
    storageAccountName: storage.outputs.storageAccountName
    keyVaultUri: keyvault.outputs.keyVaultUri
  }
}