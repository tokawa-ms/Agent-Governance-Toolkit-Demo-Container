[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $SubscriptionId,

    [Parameter(Mandatory)]
    [string] $ResourceGroupName,

    [Parameter(Mandatory)]
    [string] $Location,

    [Parameter(Mandatory)]
    [string] $GitHubOrganization,

    [Parameter(Mandatory)]
    [string] $GitHubRepository,

    [string] $GitHubEnvironment = 'production',
    [string] $DeploymentApplicationName = 'agt-demo-github-deploy'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Invoke-AzJson {
    param([Parameter(Mandatory)][string[]] $Arguments)

    $json = & az @Arguments --output json --only-show-errors
    if ($LASTEXITCODE -ne 0) {
        throw "Azure CLI command failed: az $($Arguments[0..([Math]::Min(2, $Arguments.Count - 1))] -join ' ') ..."
    }
    return ($json | Out-String) | ConvertFrom-Json
}

function Get-OrCreateApplication {
    param([Parameter(Mandatory)][string] $DisplayName)

    $escapedName = $DisplayName.Replace("'", "''")
    $applications = Invoke-AzJson -Arguments @('ad', 'app', 'list', '--filter', "displayName eq '$escapedName'")
    if (@($applications).Count -gt 1) {
        throw "More than one Entra application is named '$DisplayName'. Use a unique name."
    }
    if (@($applications).Count -eq 1) {
        return @($applications)[0]
    }
    return Invoke-AzJson -Arguments @('ad', 'app', 'create', '--display-name', $DisplayName)
}

function Get-OrCreateServicePrincipal {
    param([Parameter(Mandatory)][string] $ApplicationId)

    & az ad sp show --id $ApplicationId --output none --only-show-errors *> $null
    if ($LASTEXITCODE -ne 0) {
        & az ad sp create --id $ApplicationId --output none --only-show-errors
        if ($LASTEXITCODE -ne 0) {
            throw "Unable to create the service principal for application '$ApplicationId'."
        }
    }

    return Invoke-AzJson -Arguments @('ad', 'sp', 'show', '--id', $ApplicationId)
}

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw 'Azure CLI is required but was not found on PATH.'
}

& az account set --subscription $SubscriptionId --only-show-errors
if ($LASTEXITCODE -ne 0) {
    throw "Unable to select subscription '$SubscriptionId'."
}

& az group create --name $ResourceGroupName --location $Location --only-show-errors --output none
if ($LASTEXITCODE -ne 0) {
    throw "Unable to create or update resource group '$ResourceGroupName'."
}

$subscription = Invoke-AzJson -Arguments @('account', 'show')
$scope = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroupName"
$deploymentApplication = Get-OrCreateApplication -DisplayName $DeploymentApplicationName
$deploymentServicePrincipal = Get-OrCreateServicePrincipal -ApplicationId $deploymentApplication.appId

$federatedCredentialName = "github-$GitHubOrganization-$GitHubRepository-$GitHubEnvironment".ToLowerInvariant()
$federatedCredential = @{
    name        = $federatedCredentialName
    issuer      = 'https://token.actions.githubusercontent.com'
    subject     = "repo:$GitHubOrganization/$GitHubRepository`:environment:$GitHubEnvironment"
    description = "GitHub Actions environment $GitHubEnvironment"
    audiences   = @('api://AzureADTokenExchange')
}
$existingCredential = Invoke-AzJson -Arguments @(
    'ad', 'app', 'federated-credential', 'list',
    '--id', $deploymentApplication.appId
) | Where-Object name -eq $federatedCredentialName

if (-not $existingCredential) {
    $credentialJson = $federatedCredential | ConvertTo-Json -Compress
    $credentialFile = [System.IO.Path]::GetTempFileName()
    try {
        [System.IO.File]::WriteAllText(
            $credentialFile,
            $credentialJson,
            [System.Text.UTF8Encoding]::new($false)
        )
        & az ad app federated-credential create `
            --id $deploymentApplication.appId `
            --parameters "@$credentialFile" `
            --only-show-errors `
            --output none
        if ($LASTEXITCODE -ne 0) {
            throw 'Unable to create the GitHub OIDC federated credential.'
        }
    }
    finally {
        Remove-Item -LiteralPath $credentialFile -Force -ErrorAction SilentlyContinue
    }
}

foreach ($role in @('Contributor', 'Role Based Access Control Administrator')) {
    $assignments = Invoke-AzJson -Arguments @(
        'role', 'assignment', 'list',
        '--assignee-object-id', $deploymentServicePrincipal.id,
        '--scope', $scope,
        '--role', $role
    )
    if (@($assignments).Count -eq 0) {
        & az role assignment create `
            --assignee-object-id $deploymentServicePrincipal.id `
            --assignee-principal-type ServicePrincipal `
            --role $role `
            --scope $scope `
            --only-show-errors `
            --output none
        if ($LASTEXITCODE -ne 0) {
            throw "Unable to assign '$role' at resource-group scope."
        }
    }
}

Write-Output "AZURE_SUBSCRIPTION_ID=$($subscription.id)"
Write-Output "AZURE_TENANT_ID=$($subscription.tenantId)"
Write-Output "AZURE_CLIENT_ID=$($deploymentApplication.appId)"
Write-Output "AZURE_RESOURCE_GROUP=$ResourceGroupName"
Write-Output "AZURE_LOCATION=$Location"
