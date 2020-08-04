<#
    .DESCRIPTION
        A run book for creating a new IoT Hub for a tenant

    .NOTES
        AUTHOR: Nate Oelke
                Sean Dubiel
#>

param
(
    [Parameter (Mandatory = $false)]
    [object] $WebhookData
)

# Make sure this runbook was triggered by a webhook
if (-Not $WebhookData) {
    # Error
    Write-Error "This runbook is meant to be started from an Azure alert webhook only."
}

# Retrieve the data from the Webhook request body
$data = (ConvertFrom-Json -InputObject $WebhookData.RequestBody)

# Authenticate with the service principal
$connectionName = "AzureRunAsConnection"
try {
    $servicePrincipalConnection = Get-AutomationConnection -Name $connectionName

    "Logging in to Azure..."
    $connectionResult = Connect-AzAccount -Tenant $servicePrincipalConnection.TenantID `
                             -ApplicationId $servicePrincipalConnection.ApplicationID   `
                             -CertificateThumbprint $servicePrincipalConnection.CertificateThumbprint `
                             -ServicePrincipal `
                             -Subscription $data.subscriptionId

    "Successfully Logged in using ServicePrincipal"
}
catch {
    if (!$servicePrincipalConnection) {
        $ErrorMessage = "Connection $connectionName not found."
        throw $ErrorMessage
    } else {
        Write-Error -Message $_.Exception
        throw $_.Exception
    }
}

# Get data
$appConfigConnectionString = $data.appConfigConnectionString
$setAppConfigEndpoint = $data.setAppConfigEndpoint
$batchedTelemetryProperty = '$twin.properties.desired.batchedTelemetry';
$data.token

# Define template for creating IoT Hub
$iotHubTemplate = @"
{
    "name": "$($data.iotHubName)",
    "type": "Microsoft.Devices/IotHubs",
    "location": "$($data.location)",
    "sku": {
        "name": "S1",
        "tier": "Standard",
        "capacity":1
    },
    "properties": {
        "routing": {
            "enrichments": [
                {
                    "key": "tenant",
                    "value": "$($data.tenantId)",
                    "endpointNames": [
                        "event-hub-telemetry",
                        "event-hub-twin-change",
                        "event-hub-lifecycle" 
                    ]
                },
                {
                    "key": "batchedTelemetry",
                    "value": "$($batchedTelemetryProperty)",
                    "endpointNames": [
                        "event-hub-telemetry",
                        "events"
                    ]
                }
            ],
            "endpoints": {
                "serviceBusQueues": [

                ],
                "serviceBusTopics": [

                ],
                "eventHubs": [
                    {
                        "connectionString": "$($data.telemetryEventHubConnString)",
                        "name": "event-hub-telemetry",
                        "subscriptionId": "$($data.subscriptionId)",
                        "resourceGroup": "$($data.resourceGroup)"
                    },
                    {
                        "connectionString": "$($data.twinChangeEventHubConnString)",
                        "name": "event-hub-twin-change",
                        "subscriptionId": "$($data.subscriptionId)",
                        "resourceGroup": "$($data.resourceGroup)"
                    },
                    {
                        "connectionString": "$($data.lifecycleEventHubConnString)",
                        "name": "event-hub-lifecycle",
                        "subscriptionId": "$($data.subscriptionId)",
                        "resourceGroup": "$($data.resourceGroup)"
                    }
                ],
                "storageContainers": []
            },
            "routes": [
                {
                    "name": "telemetry",
                    "source": "DeviceMessages",
                    "condition": "true",
                    "endpointNames": [
                        "event-hub-telemetry"
                    ],
                    "isEnabled": true
                },
                {
                    "name": "lifecycle",
                    "source": "DeviceLifecycleEvents",
                    "condition": "true",
                    "endpointNames": [
                        "event-hub-lifecycle"
                    ],
                    "isEnabled": true
                },
                {
                    "name": "twin-change",
                    "source": "TwinChangeEvents",
                    "condition": "true",
                    "endpointNames": [
                        "event-hub-twin-change"
                    ],
                    "isEnabled": true
                },
                {
                    "name": "events",
                    "source": "DeviceMessages",
                    "condition": "true",
                    "endpointNames": [
                        "events"
                    ],
                    "isEnabled": true
                }
            ]
        }
    },
    "storageEndpoints": {
        "$$default": {
            "sasTtlAsIso8601": "PT1H",
            "connectionString": "DefaultEndpointsProtocol=https;EndpointSuffix=core.windows.net;AccountName=crsliotstorageacctdev;AccountKey=****",
            "containerName": "209d7f79-39aa-4e2a-8e9c-56f04f7633f9-iot-file-upload"
        }
    }
}
"@

# Create IoT Hub using Azure REST API
function createIoThubUsingRestApi(){
    $requestHeader = @{
       "Authorization" = "Bearer " + $data.token
       "Content-Type" = "application/json"
    }
    
    $iotHubUri = "https://management.azure.com/subscriptions/$($data.subscriptionId)/resourceGroups/$($data.resourceGroup)/providers/Microsoft.Devices/IotHubs/$($data.iotHubName)?api-version=2019-03-22-preview"
    
    # Create IoT Hub using Azure REST API
    $result = (Invoke-RestMethod -Method Put -Headers $requestheader -Uri $iotHubUri -Body $iotHubTemplate)          
    Write-Output $iotHubTemplate    

    # Wait for IoT Hub to be created
    $tries = 0
    while (($result.properties.state -ne "Active") -and ($tries -lt 30)) {
    Start-Sleep -Second 15
    $result = (Invoke-RestMethod -Method Get -Headers $requestheader -Uri $iotHubUri)
    $tries++
    }
}

# Set up the file upload endpoint on the Iot hub
function createFileUploadEndpoint(){
    # get the context from exiting storage account
    $storageAccount = Get-AzStorageAccount -ResourceGroupName $data.resourceGroup `
                                             -Name $data.storageAccount
    $ctx = $storageAccount.Context
    $containerName = [string]::Concat($data.tenantId,'-iot-file-upload')
    New-AzStorageContainer -Name $containerName -Context $ctx -Permission Off

    # get the storage account connection string 
    $storageAcctkey = Get-AzStorageAccountKey -Name $data.storageAccount `
                             -ResourceGroupName $data.resourceGroup

    if ([string]::IsNullOrEmpty($storageAcctkey)){ 
         Write-Output "Failed to retrieve key for $($data.storageAccount)" 
    } 
    else {
        $storageAcctPrikey = $storageAcctkey.Value[1]
    # set up the file upload endpoint on the Iot hub
        Set-AzIotHub -ResourceGroupName $data.resourceGroup `
                        -Name $($data.iotHubName)`
                        -FileUploadNotificationTtl "01:00:00" `
                        -FileUploadSasUriTtl "01:00:00" `
                        -EnableFileUploadNotifications $true `
                        -FileUploadStorageConnectionString "DefaultEndpointsProtocol=https;AccountName=$($data.storageAccount);AccountKey=$storageAcctPrikey;EndpointSuffix=core.windows.net" `
                        -FileUploadContainerName $containerName `
                        -FileUploadNotificationMaxDeliveryCount 10
        
        Write-Output "File Upload endpoint created successfully on $($data.iotHubName)"
    }    
}

# Load the connection string
function loadPolicyToIoThub(){
    $requestHeader = @{
        "Authorization" = "Bearer " + $data.token
        "Content-Type" = "application/json"
     }
    $policy = "iothubowner" 
    $iotHubKeysUri = "https://management.azure.com/subscriptions/$($data.subscriptionId)/resourceGroups/$($data.resourceGroup)/providers/Microsoft.Devices/IotHubs/$($data.iotHubName)/IotHubKeys/$policy/listkeys?api-version=2019-03-22-preview"
    $result = (Invoke-RestMethod -Method Post -Headers $requestheader -Uri $iotHubKeysUri)

    # Create the connection string
    $sharedAccessKey = $result.primaryKey
    $connectionString = "HostName=$($data.iotHubName).azure-devices.net;SharedAccessKeyName=$policy;SharedAccessKey=$sharedAccessKey"
    return $connectionString
}

# Create DPS and add current iothub to DPS
function addDPSToIoThub($connectionString){
    New-AzIoTDeviceProvisioningService -Name $data.dpsName -Location $data.location `
                                        -ResourceGroupName $data.resourceGroup

    Add-AzIoTDeviceProvisioningServiceLinkedHub -ResourceGroupName $data.resourceGroup `
                                                -Name $data.dpsName `
                                                -IotHubConnectionString $connectionString `
                                                -IotHubLocation $data.location

    Write-Output "DPS $($data.dpsName) successfully linked to IoTHub $($data.iotHubName)"    
}

# Write the IoT Hub connection string to app config (invoke appConfig function)
function addIoTHubConnStringToAppconfig($connectionString){
    $requestHeader = @{
       "Content-Type" = "application/json"
    }
    $appConfigKey = "tenant:$($data.tenantId):iotHubConnectionString"
    $appConfigBody = @"
      {
        connectionstring : "$appConfigConnectionString", name : "$appConfigKey", value : "$connectionString"
      }
"@
    $result = (Invoke-RestMethod -Method Post -Headers $requestheader -Uri $setAppConfigEndpoint -Body $appConfigBody)      
}                                            

# Write to table storage <tenant table>
function writeToTableStorage(){
    Write-Output ("Trying to write to table storage")
    $storageAccount = $data.storageAccount
    $tableName = "tenant"
    $table = Get-AzTableTable -resourceGroup $data.resourceGroup -tableName $tableName -storageAccountName $storageAccount
    $row = Get-AzTableRowByPartitionKeyRowKey -Table $table -PartitionKey $data.tenantId[0] -RowKey $data.tenantId
    Write-Output "$($row.RowKey)"
    Write-Output "$($row.PartitionKey)"
    if ($row.RowKey -and $row.PartitionKey) {
       # If the rowkey and partition key are non-empty values, the row exists.
       # If the row exists, fill in the fields related to the IoT Hub
        $row.IsIotHubDeployed = $true
        $row.IotHubName = $data.iotHubName
        $row | Update-AzTableRow -Table $table
        Write-Output "IotHubName and IsIotHubDeployed updated for tenant $($data.tenantId) in tenant table"
        Write-Output "Finished creating a new IotHub for the Tenant"
        } 
        else {
            # If the row does not exist, there is a problem with the tenant, it most liekly was deleted or cleaned up before this runbook could complete.
            Write-Error "No Table Storage row exists for $($data.tenantId) in the tenant table. The row may have been deleted before the IoT Hub could be fully deployed.";
            Write-Output "Finished creating a new IotHub, however Table Storage could not be updated. This tenant may be in a failing state, or may already be deleted.";
        }    
}

#Adding the users as SysAdmins for Newly created Tenant
function addUsersAsAdminsForNewTenant(){
    try{
        Write-Output ("Trying to write to 'User' table storage")
        $storageAccount = $data.storageAccount
        $sysAdminTableName = "SystemAdmin"
        $usersTableName = "user"
        $resourceGroup  =  $data.resourceGroup
        $adminTable = Get-AzTableTable -resourceGroup $resourceGroup -tableName $sysAdminTableName -storageAccountName $storageAccount
        $usersTable = Get-AzTableTable -resourceGroup $resourceGroup -tableName $usersTableName -storageAccountName $storageAccount
        $adminUsers = Get-AzTableRow -table $adminTable
        foreach ($user in $adminUsers) {
            $timeStamp = Get-Date -Format 'yyyy-MM-ddTHH:mm:ss.fffffffZ'
            Write-Output $user
            Write-Output $user.Name.toString()
            if($user.Name -ne $null){
                $result = Add-AzTableRow -table $usersTable -partitionKey $user.PartitionKey `
                        -rowKey $data.tenantId `
                        -property @{"Timestamp"=$timeStamp;"Name"=$user.Name;"Roles"='["admin"]';"Type"="Member"}
            }
        }
        Write-Output ("Added admin users to user table for new tenant $($data.tenantId).")
    }
    catch {
        Write-Error -Message $_.Exception
    }
}

try {
    # Call the functions 
    createIoThubUsingRestApi
    createFileUploadEndpoint
    $connString = loadPolicyToIoThub
    addDPSToIoThub -connectionString $connString
    addIoTHubConnStringToAppconfig -connectionString $connString
    writeToTableStorage
    addUsersAsAdminsForNewTenant
}
catch {
    Write-Error -Message $_.Exception
    throw $_.Exception
}
