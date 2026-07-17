@description('Existing Storage Account Name')
param storageAccountName string

// Reference the existing storage account
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

// Update the existing storage account to enable static website
resource updateStaticWebsite 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccount.name
  location: storageAccount.location
  kind: 'StorageV2'
  sku: {
    name: storageAccount.sku.name
  }
  properties: {
    staticWebsite: {
      enabled: true
      indexDocument: 'index.html'
      error404Document: 'index.html'
    }
  }
}

output websiteUrl string = storageAccount.properties.primaryEndpoints.web