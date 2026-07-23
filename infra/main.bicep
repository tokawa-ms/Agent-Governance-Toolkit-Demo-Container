targetScope = 'resourceGroup'

@description('Azure region for all regional resources.')
param location string = 'eastasia'

@description('Short environment label used in resource names.')
@minLength(2)
@maxLength(12)
param environmentName string = 'demo'

@description('Globally unique Azure Container Registry name.')
@minLength(5)
@maxLength(50)
param containerRegistryName string

@description('Private blob container used for governance audit records.')
@allowed([
  'agt-audit'
])
param auditContainerName string = 'agt-audit'

@description('Address space assigned to the demo virtual network.')
param virtualNetworkAddressPrefix string = '10.20.0.0/23'

@description('Dedicated subnet for the Container Apps workload profiles environment.')
param containerAppsSubnetPrefix string = '10.20.0.0/27'

@description('Dedicated subnet for private endpoints.')
param privateEndpointSubnetPrefix string = '10.20.0.32/27'

var suffix = uniqueString(subscription().subscriptionId, resourceGroup().id)
var storageAccountName = take('stagt${environmentName}${suffix}', 24)
var logAnalyticsName = 'log-agt-${environmentName}-${suffix}'
var appInsightsName = 'appi-agt-${environmentName}-${suffix}'
var virtualNetworkName = 'vnet-agt-${environmentName}-${suffix}'
var containerAppsEnvironmentName = 'cae-agt-${environmentName}-standard'
var runtimeIdentityName = 'id-agt-${environmentName}-${suffix}'
var privateDnsZoneName = 'privatelink.blob.${environment().suffixes.storage}'
var blobDataContributorRoleDefinitionId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
)
var acrPullRoleDefinitionId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '7f951dda-4ed3-4680-a7ca-43fe172d538d'
)

resource virtualNetwork 'Microsoft.Network/virtualNetworks@2023-11-01' = {
  name: virtualNetworkName
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [
        virtualNetworkAddressPrefix
      ]
    }
  }
}

resource containerAppsSubnet 'Microsoft.Network/virtualNetworks/subnets@2023-11-01' = {
  parent: virtualNetwork
  name: 'snet-container-apps'
  properties: {
    addressPrefix: containerAppsSubnetPrefix
    delegations: [
      {
        name: 'Microsoft.App-environments'
        properties: {
          serviceName: 'Microsoft.App/environments'
        }
      }
    ]
  }
}

resource privateEndpointSubnet 'Microsoft.Network/virtualNetworks/subnets@2023-11-01' = {
  parent: virtualNetwork
  name: 'snet-private-endpoints'
  properties: {
    addressPrefix: privateEndpointSubnetPrefix
    privateEndpointNetworkPolicies: 'Disabled'
  }
  dependsOn: [
    containerAppsSubnet
  ]
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    defaultToOAuthAuthentication: true
    publicNetworkAccess: 'Disabled'
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Deny'
      ipRules: []
      virtualNetworkRules: []
    }
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storage
  name: 'default'
  properties: {
    deleteRetentionPolicy: {
      enabled: true
      days: 7
    }
    containerDeleteRetentionPolicy: {
      enabled: true
      days: 7
    }
  }
}

resource auditContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: auditContainerName
  properties: {
    publicAccess: 'None'
  }
}

resource privateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: privateDnsZoneName
  location: 'global'
}

resource privateDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: privateDnsZone
  name: 'link-${virtualNetwork.name}'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: virtualNetwork.id
    }
  }
}

resource blobPrivateEndpoint 'Microsoft.Network/privateEndpoints@2023-11-01' = {
  name: 'pe-${storage.name}-blob'
  location: location
  properties: {
    subnet: {
      id: privateEndpointSubnet.id
    }
    privateLinkServiceConnections: [
      {
        name: 'blob'
        properties: {
          privateLinkServiceId: storage.id
          groupIds: [
            'blob'
          ]
        }
      }
    ]
  }
}

resource blobPrivateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-11-01' = {
  parent: blobPrivateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'blob'
        properties: {
          privateDnsZoneId: privateDnsZone.id
        }
      }
    ]
  }
}

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: containerRegistryName
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
    publicNetworkAccess: 'Enabled'
  }
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

resource runtimeIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: runtimeIdentityName
  location: location
}

resource auditBlobContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(auditContainer.id, runtimeIdentity.id, blobDataContributorRoleDefinitionId)
  scope: auditContainer
  properties: {
    roleDefinitionId: blobDataContributorRoleDefinitionId
    principalId: runtimeIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource registryPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(registry.id, runtimeIdentity.id, acrPullRoleDefinitionId)
  scope: registry
  properties: {
    roleDefinitionId: acrPullRoleDefinitionId
    principalId: runtimeIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: containerAppsEnvironmentName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
    vnetConfiguration: {
      infrastructureSubnetId: containerAppsSubnet.id
      internal: false
    }
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
    ]
    zoneRedundant: false
  }
}

@description('ACR resource name; no registry credentials are exposed.')
output containerRegistryName string = registry.name

@description('ACR login endpoint; no registry credentials are exposed.')
output containerRegistryLoginServer string = registry.properties.loginServer

@description('Storage account resource name; shared-key and public network access are disabled.')
output storageAccountName string = storage.name

@description('Blob service URI resolved through Private DNS from the Container Apps VNet.')
output storageAccountUri string = storage.properties.primaryEndpoints.blob

@description('Private audit blob container name.')
output auditContainerName string = auditContainer.name

@description('Workspace-based Application Insights connection string.')
output applicationInsightsConnectionString string = appInsights.properties.ConnectionString

@description('Log Analytics workspace resource name.')
output logAnalyticsWorkspaceName string = logAnalytics.name

@description('Standard Container Apps environment name.')
output containerAppsEnvironmentName string = containerAppsEnvironment.name

@description('Runtime user-assigned managed identity resource ID.')
output runtimeIdentityResourceId string = runtimeIdentity.id

@description('Runtime user-assigned managed identity client ID.')
output runtimeIdentityClientId string = runtimeIdentity.properties.clientId

@description('Virtual network resource ID.')
output virtualNetworkId string = virtualNetwork.id

@description('Blob private endpoint resource ID.')
output blobPrivateEndpointId string = blobPrivateEndpoint.id
