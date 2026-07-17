@description('Azure Database for PostgreSQL Flexible Server name')
param serverName string

@description('Administrator username')
param administratorLogin string

@secure()
@description('Administrator password')
param administratorLoginPassword string

@description('Azure region')
param location string = resourceGroup().location

@description('Primary application database')
param primaryDatabaseName string = 'restaurantdb'

@description('Archive database')
param archiveDatabaseName string = 'restaurantarchive'

@description('Public IP address allowed to connect (your machine)')
param clientIpAddress string

resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: serverName
  location: location

  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }

  properties: {
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorLoginPassword
    version: '16'

    storage: {
      storageSizeGB: 32
    }

    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }

    network: {
      publicNetworkAccess: 'Enabled'
    }
  }
}

resource primaryDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: postgresServer
  name: primaryDatabaseName

  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

resource archiveDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: postgresServer
  name: archiveDatabaseName

  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

resource allowAzureServices 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
  parent: postgresServer
  name: 'AllowAzureServices'

  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource allowDeveloperMachine 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
  parent: postgresServer
  name: 'AllowDeveloperMachine'

  properties: {
    startIpAddress: clientIpAddress
    endIpAddress: clientIpAddress
  }
}

output serverName string = postgresServer.name

output fullyQualifiedDomainName string = postgresServer.properties.fullyQualifiedDomainName

output primaryDatabaseName string = primaryDatabase.name

output archiveDatabaseName string = archiveDatabase.name