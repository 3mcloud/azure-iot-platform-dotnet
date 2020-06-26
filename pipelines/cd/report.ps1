Param(
    [Parameter(Mandatory = $True)]
    $SourceDirectory,
    [Parameter(Mandatory = $True)]
    $SerenityEnvironmentName,
    [Parameter(Mandatory = $True)]
    $AzureDevOpsPersonalAccessToken
)

Class SerenityEnvironments : System.Management.Automation.IValidateSetValuesGenerator {
    [String[]] GetValidValues() {
        return Get-SerenityEnvironmentNames
    }
}

Class SerenityServiceNames : System.Management.Automation.IValidateSetValuesGenerator {
    [String[]] GetValidValues() {
        return Get-SerenityServiceNames
    }
}

Class SerenityService {
    $Name
    $SemanticVersion
    $GitSha
    $ImageTags
    $ImageName
    $ImageSha
    $PipelineRunId
    $CommitDate
    $Environment
    $PipelineRunUrl
    $ImageTagDate
    $GitRepository
}

Class SerenityEnvironment {
    $Name
    $DisplayName
    $AzureSubscriptionName
    $AzureSubscriptionId
    $AzureDevOpsEnvironmentName
    $ApplicationCode
    $ApplicationShortCode
    $EnvironmentCategory
    $TenantId = 'facac3c4-e2a5-4257-af76-205c8a821ddb'

    [string] ToString() {
        return $this.DisplayName
    }

    [string] GetAzureAksName() {
        return "$($this.ApplicationCode)-aks-$($this.EnvironmentCategory)"
    }

    [string] GetAzureResourceGroupName() {
        return "rg-iot-$($this.ApplicationShortCode)-$($this.EnvironmentCategory)"
    }
}

Function Get-SerenityServices {
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory = $True)]
        [ValidateSet([SerenityEnvironments])]
        [Alias('EnvironmentName')]
        $Environment,
        [Parameter()]
        [ValidateSet('Json', 'Object', 'Table', 'Html')]
        [Alias('Format', 'OutputFormat')]
        $Output = 'Object'
    )

    $serenityServiceNames = Get-SerenityServiceNames | Sort-Object
    $serenityEnvironment = Get-SerenityEnvironment -Name $Environment
    Write-Verbose "Getting all services in $($serenityEnvironment.DisplayName) environment"

    Write-Verbose 'Getting Azure Resource Group info'
    $resourceGroup = Get-AzureResourceGroup -Name $serenityEnvironment.GetAzureResourceGroupName()

    Write-Verbose 'Getting Azure AKS cluster info'
    $aksCluster = Get-AzureAks -Name $serenityEnvironment.GetAzureAksName()

    if ($Null -eq $resourceGroup) {
        Write-Error "Resource group '$($serenityEnvironment.GetAzureResourceGroupName())' does not exist"
        "Resource group '$($serenityEnvironment.GetAzureResourceGroupName())' does not exist" | Add-Content debug.txt
    }

    if ($Null -eq $aksCluster) {
        Write-Error "AKS cluster '$($serenityEnvironment.GetAzureAksName())' does not exist"
        "AKS cluster '$($serenityEnvironment.GetAzureAksName())' does not exist" | Add-Content debug.txt
    }

    if ($Null -ne $resourceGroup -And $Null -ne $aksCluster) {
        Write-Verbose 'Getting Azure AKS credentials'
        $Null = Set-K8sContext -Environment $serenityEnvironment.Name

        $serenityServices = @()
        foreach ($serenityServiceName in $serenityServiceNames) {
            Write-Verbose "Processing $serenityServiceName service"
            $serenityService = Get-SerenityService -Name $serenityServiceName -Output Object
            $serenityService.Environment = $serenityEnvironment
            $serenityServices += $serenityService
        }
    }
    else {
        $serenityServices = @()
    }

    return Write-SerenityOutput -InputObject $serenityServices -Output $Output
}

Function Write-SerenityOutput {
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory = $True, ValueFromPipeline = $True)]
        $InputObject,
        [Parameter()]
        [ValidateSet('Json', 'Object', 'Table', 'Html')]
        [Alias('Format', 'OutputFormat')]
        $Output = 'Object'
    )

    switch ($Output) {
        'Json' {
            return $InputObject | ConvertTo-Json -Depth 100
        }
        'Object' {
            return $InputObject
        }
        'Table' {
            return $InputObject | Format-Table -AutoSize
        }
        'Html' {
            return ConvertTo-SerenityHtml -InputObject $InputObject
        }
        Default {
            return $InputObject
        }
    }
}

Function ConvertTo-SerenityHtml {
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory = $True, ValueFromPipeline = $True)]
        $InputObject
    )

    foreach ($item in $InputObject) {
        if ($item.GetType() -Ne [SerenityService]) {
            return ConvertTo-Html -InputObject $InputObject
        }
    }

    $stringBuilder = New-Object -TypeName System.Text.StringBuilder
    $Null = $stringBuilder.AppendLine(@'
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN"  "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
    <link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/bootstrap/4.0.0/css/bootstrap.min.css" integrity="sha384-Gn5384xqQ1aoWXA+058RXPxPg6fy4IWvTNh0E263XmFcJlSAwiGgFAW/dAiS6JXm" crossorigin="anonymous">
    <style>
        th {
            position: -webkit-sticky;
            position: sticky;
            top: 0;
            z-index: 2;
        }

        .table td.truncated {
            max-width: 300px;
        }

        .table td.truncated span {
            overflow: hidden;
            text-overflow: ellipsis;
            display: inline-block;
            white-space: nowrap;
            max-width: 100%;
        }
    </style>
    <title>Serenity Releases</title>
</head>
<body>
    <table class="table table-striped table-bordered table-dark">
        <thead>
            <tr>
'@)

    $properties = $InputObject | Get-Member -Type Property | Select-Object -ExpandProperty Name | Sort-Object
    foreach ($property in $properties) {
        $Null = $stringBuilder.Append('                <th>')
        $Null = $stringBuilder.Append($property)
        $Null = $stringBuilder.AppendLine('</th>')
    }

    $Null = $stringBuilder.AppendLine(@'
            </tr>
        </thead>
        <tbody>
'@)

    foreach ($serenityService in $InputObject) {
        $Null = $stringBuilder.AppendLine('            <tr>')
        foreach ($property in $properties) {
            $Null = $stringBuilder.AppendLine('                <td class="truncated">')
            $Null = $stringBuilder.Append('                    <span>')
            if ($serenityService.$property -is [array]) {
                foreach ($item in $serenityService.$property) {
                    $Null = $stringBuilder.Append($item)
                    $Null = $stringBuilder.Append('<br/>')
                }
            }
            else {
                $Null = $stringBuilder.Append($serenityService.$property)
            }

            $Null = $stringBuilder.AppendLine('</span>')
            $Null = $stringBuilder.AppendLine('                </td>')
        }
        $Null = $stringBuilder.AppendLine('            </tr>')
    }

    $Null = $stringBuilder.AppendLine(@'
        </tbody>
    </table>
</body>
</html>
'@)

    return $stringBuilder.ToString()
}

Function Get-SerenityService {
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory = $True, ValueFromPipeline = $True)]
        [ValidateSet([SerenityServiceNames])]
        [Alias('ServiceName', 'Service')]
        $Name,
        [Parameter()]
        [ValidateSet('Json', 'Object', 'Table', 'Html')]
        [Alias('Format', 'OutputFormat')]
        $Output = 'Object'
    )

    Begin {}

    Process {
        $serenityServices = @()
        Write-Verbose 'Getting running pods'
        $pods = kubectl get pods --selector app.kubernetes.io/name=$Name --output json |
            ConvertFrom-Json -Depth 100 |
            Select-Object -ExpandProperty items |
            Where-Object { $_.status.phase -eq 'Running' }
        if ($Null -eq $pods -or $pods.Length -eq 0) {
            return $serenityServices
        }

        foreach ($pod in $pods) {
            $containers = $pods.spec.containers | Where-Object { $_.name -eq $Name }
            foreach ($container in $containers) {
                $serenityService = New-Object -TypeName SerenityService
                $serenityService.Name = $Name
                $imageNameAndTag = ($container.image -split '/')[-1]
                $serenityService.ImageName = ($imageNameAndTag -split ':')[0]
                $imageId = $pod.status.containerStatuses | Where-Object { $_.name -eq $Name } | Select-Object -ExpandProperty imageID
                $serenityService.ImageSha = ($imageId -split '@')[-1]

                Write-Verbose 'Getting Docker Hub image tags'
                $dockerHubTags = Get-DockerHubTags -Container $name -Repository azureiot3m
                if ($dockerHubTags) {
                    $thisImageTags = $dockerHubTags |
                        Where-Object { $_.images[0].digest -eq $serenityService.ImageSha }
                    $serenityService.ImageTags = $thisImageTags |
                        Select-Object -ExpandProperty Name
                    $tagWithPipelineRunId = $thisImageTags |
                        Where-Object { $_.name -match '^\d{5,6}$' } |
                        Select-Object -First 1
                    $serenityService.PipelineRunId = $tagWithPipelineRunId.name
                    $serenityService.ImageTagDate = $tagWithPipelineRunId.last_updated
                }

                if ($Null -ne $serenityService.PipelineRunId) {
                    Write-Verbose 'Getting Azure Pipeline run info'
                    $pipelineRun = Get-AzurePipelineRun -Id $serenityService.PipelineRunId
                    if ($pipelineRun) {
                        $serenityService.PipelineRunUrl = $pipelineRun.url
                        Push-Location (Join-Path $mmmSourceDirectory $serenityRepositoryName)
                        Write-Verbose 'Inspecting Git repository'
                        $Null = git cat-file -e "$($pipelineRun.sourceVersion)^{commit}" 2>&1
                        if ($?) {
                            $serenityService.CommitDate = git show -s --format=%ci $pipelineRun.sourceVersion
                            $serenityService.GitRepository = git config --get remote.origin.url
                            $serenityService.GitSha = git rev-parse --short=4 $pipelineRun.sourceVersion
                            $serenityService.SemanticVersion = git tag --points-at $pipelineRun.sourceVersion |
                                Select-String '^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-(?<prerelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?<metadata>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$' -Raw
                        }
                        else {
                            Push-Location (Join-Path $mmmSourceDirectory $serenityRepositoryNameAlt)
                            Write-Verbose 'Inspecting old Git repository'
                            $Null = git cat-file -e "$($pipelineRun.sourceVersion)^{commit}" 2>&1
                            if (!$?) {
                                throw 'Could not find commit in either old or new repository'
                            }

                            $serenityService.CommitDate = git show -s --format=%ci $pipelineRun.sourceVersion
                            $serenityService.GitRepository = git config --get remote.origin.url
                            $serenityService.GitSha = git rev-parse --short=4 $pipelineRun.sourceVersion
                            Pop-Location
                        }

                        Pop-Location
                    }
                }

                $serenityServices += $serenityService
            }
        }

        return Write-SerenityOutput -InputObject $serenityServices -Output $Output
    }

    End {}
}

Function Get-SerenityEnvironmentNames {
    return Get-SerenityEnvironments | Select-Object -ExpandProperty Name
}

Function Get-SerenityServiceNames {
    $services = Get-ChildItem -Path (Join-Path $mmmSourceDirectory $serenityRepositoryName 'src') -Directory -Exclude services | Select-Object -ExpandProperty Name
    Get-ChildItem -Path (Join-Path $mmmSourceDirectory $serenityRepositoryName 'src/services') -Directory -Exclude common |
        Select-Object -ExpandProperty Name |
        ForEach-Object {
            $services += $_
        }

    return $services
}

Function Get-DockerHubTags {
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory=$true)]
        $Container,
        [Parameter(Mandatory=$false)]
        $Repository = 'library'
    )

    $id = "$Repository/$Container"
    if ($Null -ne $tags[$id]) {
        return $tags[$id]
    }

    $result = Invoke-RestMethod ([System.String]::Format($script:containerRegistryTagsUrl, $Repository, $Container, $script:containerRegistryPageSize))
    if ($Null -eq $result) {
        $tags[$id] = $False
    }
    else {
        $results = @()
        $results += $result.results
        while ($null -ne $result.next -and '' -ne $result.next) {
            $result = Invoke-RestMethod $result.next
            if ($null -eq $result) {
                break
            }

            $results += $result.results
        }

        $tags[$id] = $results
    }

    return $tags[$id]
}

Function Get-AzurePipelineRun {
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        $Id
    )

    if ($Null -ne $pipelineRuns[$Id]) {
        return $pipelineRuns[$Id]
    }

    $pipelineRun = az pipelines runs show --id $Id --organization https://dev.azure.com/3M-Bluebird --project AzurePlatform --only-show-errors 2>&1
    try {
        $pipelineRuns[$Id] = $pipelineRun |
        ConvertFrom-Json -Depth 100
    }
    catch {
        $pipelineRuns[$Id] = $False
    }

    return $pipelineRuns[$Id]
}

Function Get-AzureResourceGroup {
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [Alias('ResourceGroup', 'ResourceGroupName')]
        $Name
    )

    az group list --query "[?name=='$Name']" | ConvertFrom-Json -Depth 100
}

Function Get-AzureAks {
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [Alias('Cluster', 'ClusterName', 'AksName')]
        $Name
    )

    az aks list --query "[?name=='$Name']" | ConvertFrom-Json -Depth 100
}

Function Get-SerenityEnvironments {
    $codePipeline = ConvertFrom-Yaml (Get-Content -Raw (Join-Path $mmmSourceDirectory $serenityRepositoryName $serenityCodePipelineFile))
    $serenityEnvironments = @()
    foreach ($stageItem in $codePipeline.stages) {
        foreach ($parameterItem in $stageItem.jobs |
            Where-Object { $_.template -ilike '*jobs-deploy-code*' } |
            Select-Object -ExpandProperty parameters) {
                $environment = New-Object -TypeName SerenityEnvironment
                $environment.Name = $stageItem.stage
                $environment.DisplayName = $stageItem.displayName
                $environment.AzureSubscriptionName = $parameterItem.subscriptionName
                $environment.AzureSubscriptionId = $parameterItem.subscriptionId
                $environment.AzureDevOpsEnvironmentName = $parameterItem.environmentName
                $environment.ApplicationCode = $parameterItem.applicationCode
                $environment.ApplicationShortCode = $parameterItem.applicationShortCode
                $environment.EnvironmentCategory = $parameterItem.environmentCategory
                $serenityEnvironments += $environment
            }
    }

    return $serenityEnvironments
}

Function Get-SerenityEnvironment {
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [ValidateSet([SerenityEnvironments])]
        [Alias('EnvironmentName', 'Environment')]
        $Name
    )

    return Get-SerenityEnvironments | Where-Object { $_.Name -eq $Name }
}

Function Get-AzureAksCredentials {
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [ValidateSet([SerenityEnvironments])]
        [Alias('EnvironmentName')]
        $Environment
    )

    $serenityEnvironment = Get-SerenityEnvironment -Name $Environment
    az aks get-credentials --name $serenityEnvironment.GetAzureAksName() --resource-group $serenityEnvironment.GetAzureResourceGroupName() --overwrite-existing
}

Function Set-K8sContext {
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [ValidateSet([SerenityEnvironments])]
        [Alias('EnvironmentName')]
        $Environment
    )

    Get-AzureAksCredentials -Environment $Environment
    kubectl config current-context
}

Write-Verbose 'Installing YAML module'
Install-Module -Name powershell-yaml -Force
Import-Module -Name powershell-yaml -Force
$pipelineRuns = @{}
$tags = @{}
$mmmSourceDirectory = $SourceDirectory
$serenityCodePipelineFile = 'pipelines/cd/code.yaml'
$serenityRepositoryName = 'azure-iot-platform-dotnet'
$serenityRepositoryNameAlt = 'azure-iot-services-dotnet'
$script:containerRegistryPageSize = 100
$script:containerRegistryTagsUrl = 'https://registry.hub.docker.com/v2/repositories/{0}/{1}/tags/?page_size={2}'
$reportsDirectoryPath = Join-Path $SourceDirectory reports
$Null = New-Item -Type Directory -Path $reportsDirectoryPath
#Write-Verbose 'Logging into Azure DevOps'
$env:AZURE_DEVOPS_EXT_PAT = $AzureDevOpsPersonalAccessToken
#az devops login --organization https://dev.azure.com/3M-Bluebird
Write-Verbose 'Generating report'
Get-SerenityServices -Environment $SerenityEnvironmentName -Output Html |
    Set-Content (Join-Path $reportsDirectoryPath "${SerenityEnvironmentName}.html")