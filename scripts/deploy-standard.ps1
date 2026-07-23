[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $ResourceGroupName,

    [Parameter(Mandatory)]
    [string] $EnvironmentName,

    [Parameter(Mandatory)]
    [string] $ContainerAppName,

    [Parameter(Mandatory)]
    [string] $ContainerRegistryName,

    [Parameter(Mandatory)]
    [string] $Image,

    [Parameter(Mandatory)]
    [string] $RuntimeIdentityResourceId,

    [Parameter(Mandatory)]
    [string] $RuntimeIdentityClientId,

    [Parameter(Mandatory)]
    [string] $StorageAccountUri,

    [string] $AuditContainerName = 'agt-audit',

    [Parameter(Mandatory)]
    [string] $ApplicationInsightsConnectionString,

    [switch] $PreflightOnly
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Invoke-Az {
    param(
        [Parameter(Mandatory)]
        [string[]] $Arguments,

        [switch] $Capture
    )

    if ($Capture) {
        $result = & az @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "Azure CLI command failed: az $($Arguments[0..([Math]::Min(2, $Arguments.Count - 1))] -join ' ') ..."
        }
        return $result
    }

    & az @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Azure CLI command failed: az $($Arguments[0..([Math]::Min(2, $Arguments.Count - 1))] -join ' ') ..."
    }
}

function Test-AzResource {
    param([Parameter(Mandatory)][string[]] $Arguments)

    & az @Arguments --only-show-errors *> $null
    return $LASTEXITCODE -eq 0
}

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw 'Azure CLI is required but was not found on PATH.'
}

$requiredCreateFlags = @(
    '--environment',
    '--user-assigned',
    '--registry-identity',
    '--min-replicas',
    '--max-replicas',
    '--scale-rule-http-concurrency'
)
$createHelp = (& az containerapp create --help 2>&1 | Out-String)
foreach ($flag in $requiredCreateFlags) {
    if ($createHelp -notmatch [regex]::Escape($flag)) {
        throw "The installed Container Apps extension does not expose '$flag'. Upgrade the extension before deployment."
    }
}

$environmentJson = Invoke-Az -Capture -Arguments @(
    'containerapp', 'env', 'show',
    '--name', $EnvironmentName,
    '--resource-group', $ResourceGroupName,
    '--query', '{id:id,mode:properties.environmentMode,subnet:properties.vnetConfiguration.infrastructureSubnetId,profiles:properties.workloadProfiles}',
    '--output', 'json',
    '--only-show-errors'
)
$environment = ($environmentJson | Out-String) | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace($environment.id) -or [string]::IsNullOrWhiteSpace($environment.subnet)) {
    throw "Standard Container Apps environment '$EnvironmentName' is not VNet integrated."
}
if ([string] $environment.mode -match '^(?i:express)$') {
    throw "Environment '$EnvironmentName' is Express, not Standard."
}
if (-not (@($environment.profiles) | Where-Object workloadProfileType -eq 'Consumption')) {
    throw "Environment '$EnvironmentName' does not expose a Consumption workload profile."
}

$registryLoginServer = (Invoke-Az -Capture -Arguments @(
    'acr', 'show',
    '--name', $ContainerRegistryName,
    '--resource-group', $ResourceGroupName,
    '--query', 'loginServer',
    '--output', 'tsv',
    '--only-show-errors'
)).Trim()

if ($PreflightOnly) {
    Write-Host "Preflight passed for VNet-integrated Standard Container Apps environment '$EnvironmentName'."
    return
}

$environmentVariables = @(
    'ASPNETCORE_URLS=http://+:8080',
    "AZURE_CLIENT_ID=$RuntimeIdentityClientId",
    "Storage__AccountUri=$StorageAccountUri",
    "Storage__AuditContainerName=$AuditContainerName",
    "APPLICATIONINSIGHTS_CONNECTION_STRING=$ApplicationInsightsConnectionString"
)

$appExists = Test-AzResource -Arguments @(
    'containerapp', 'show',
    '--name', $ContainerAppName,
    '--resource-group', $ResourceGroupName
)

if (-not $appExists) {
    Invoke-Az -Arguments (@(
        'containerapp', 'create',
        '--name', $ContainerAppName,
        '--resource-group', $ResourceGroupName,
        '--environment', $EnvironmentName,
        '--image', $Image,
        '--ingress', 'external',
        '--target-port', '8080',
        '--transport', 'auto',
        '--allow-insecure', 'false',
        '--cpu', '0.5',
        '--memory', '1.0Gi',
        '--workload-profile-name', 'Consumption',
        '--min-replicas', '0',
        '--max-replicas', '1',
        '--scale-rule-name', 'http-scaling',
        '--scale-rule-http-concurrency', '20',
        '--user-assigned', $RuntimeIdentityResourceId,
        '--registry-server', $registryLoginServer,
        '--registry-identity', $RuntimeIdentityResourceId,
        '--env-vars'
    ) + $environmentVariables + @(
        '--only-show-errors',
        '--output', 'none'
    ))
}
else {
    $existingEnvironmentId = (Invoke-Az -Capture -Arguments @(
        'containerapp', 'show',
        '--name', $ContainerAppName,
        '--resource-group', $ResourceGroupName,
        '--query', 'properties.environmentId',
        '--output', 'tsv',
        '--only-show-errors'
    )).Trim()
    if ([string]::IsNullOrWhiteSpace($existingEnvironmentId)) {
        $existingEnvironmentId = (Invoke-Az -Capture -Arguments @(
            'containerapp', 'show',
            '--name', $ContainerAppName,
            '--resource-group', $ResourceGroupName,
            '--query', 'properties.managedEnvironmentId',
            '--output', 'tsv',
            '--only-show-errors'
        )).Trim()
    }
    if ($existingEnvironmentId -ne $environment.id) {
        throw "Container app '$ContainerAppName' belongs to a different Container Apps environment."
    }

    Invoke-Az -Arguments @(
        'containerapp', 'identity', 'assign',
        '--name', $ContainerAppName,
        '--resource-group', $ResourceGroupName,
        '--user-assigned', $RuntimeIdentityResourceId,
        '--only-show-errors',
        '--output', 'none'
    )
    Invoke-Az -Arguments @(
        'containerapp', 'registry', 'set',
        '--name', $ContainerAppName,
        '--resource-group', $ResourceGroupName,
        '--server', $registryLoginServer,
        '--identity', $RuntimeIdentityResourceId,
        '--only-show-errors',
        '--output', 'none'
    )
    Invoke-Az -Arguments (@(
        'containerapp', 'update',
        '--name', $ContainerAppName,
        '--resource-group', $ResourceGroupName,
        '--image', $Image,
        '--min-replicas', '0',
        '--max-replicas', '1',
        '--workload-profile-name', 'Consumption',
        '--set-env-vars'
    ) + $environmentVariables + @(
        '--only-show-errors',
        '--output', 'none'
    ))
}

Invoke-Az -Arguments @(
    'containerapp', 'ingress', 'enable',
    '--name', $ContainerAppName,
    '--resource-group', $ResourceGroupName,
    '--type', 'external',
    '--target-port', '8080',
    '--transport', 'auto',
    '--allow-insecure', 'false',
    '--only-show-errors',
    '--output', 'none'
)

$appStateJson = Invoke-Az -Capture -Arguments @(
    'containerapp', 'show',
    '--name', $ContainerAppName,
    '--resource-group', $ResourceGroupName,
    '--query', '{fqdn:properties.configuration.ingress.fqdn,external:properties.configuration.ingress.external,targetPort:properties.configuration.ingress.targetPort,minReplicas:properties.template.scale.minReplicas,maxReplicas:properties.template.scale.maxReplicas,identity:identity.userAssignedIdentities,registries:properties.configuration.registries,env:properties.template.containers[0].env}',
    '--output', 'json',
    '--only-show-errors'
)
$appState = ($appStateJson | Out-String) | ConvertFrom-Json
if (-not $appState.external -or $appState.targetPort -ne 8080) {
    throw "Container app '$ContainerAppName' ingress is not external on target port 8080."
}
if ($appState.minReplicas -ne 0 -or $appState.maxReplicas -ne 1) {
    throw "Container app '$ContainerAppName' replica limits are not min 0 and max 1."
}
if (-not $appState.identity.PSObject.Properties[$RuntimeIdentityResourceId]) {
    throw "Container app '$ContainerAppName' is missing the runtime user-assigned managed identity."
}
$configuredRegistry = @($appState.registries) |
    Where-Object server -eq $registryLoginServer |
    Select-Object -First 1
if (-not $configuredRegistry -or $configuredRegistry.identity -ne $RuntimeIdentityResourceId) {
    throw "Container app '$ContainerAppName' is not configured for managed-identity ACR pull."
}

$configuredEnvironment = @{}
foreach ($entry in @($appState.env)) {
    $configuredEnvironment[$entry.name] = $entry.value
}
foreach ($entry in $environmentVariables) {
    $name, $value = $entry -split '=', 2
    if ($configuredEnvironment[$name] -ne $value) {
        throw "Container app '$ContainerAppName' environment variable '$name' is not configured correctly."
    }
}

Write-Host "Deployment completed: https://$($appState.fqdn)"
