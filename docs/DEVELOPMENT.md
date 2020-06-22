# Prerequisites
* .NET Core SDK version 3.1
* Azure CLI

Ensure the `dotnet` and `az` binaries are available in a terminal

# One-Time Setup
Ensure the `AppConfigurationConnectionString` is set before building so that the `Mmm.Iot.Config.ClassGeneration` NuGet package can execute during build to automatically generate configuration classes in `./common/Services/Config` based on key-value pairs in Azure App Configuration and Azure Key Vault. This can be done in one of two ways:

1. [Set an environment variable](##Set-an-environment-variable)
1. [Use `dotnet user-secrets`](##Use-dotnet-user-secrets) (recommended)

Either way, you will need to choose an Azure App Configuration instance and make note of its `<name>` and `<resource-group>` for use in the steps below.

## Set an environment variable
### Windows
In a PowerShell shell:
```
[System.Environment]::SetEnvironmentVariable('AppConfigurationConnectionString', (az appconfig credential list --name <name> --resource-group <resource-group> --query "[?name=='Primary'].connectionString | [0]" --output tsv), 'User')
```

### Non-Windows
Set the `AppConfigurationConnectionString` environment variable in the Bash configuration file of your choice.

## Use dotnet user-secrets
### Windows
In a PowerShell shell:
```
dotnet user-secrets set --project ./src/services/common/Services/Services.csproj AppConfigurationConnectionString (az appconfig credential list --name <name> --resource-group <resource-group> --query "[?name=='Primary'].connectionString | [0]" --output tsv)
```
Then check the value of the secret:
```
dotnet user-secrets list --project ./src/services/common/Services/Services.csproj --json | Select-Object -Skip 1 | Select-Object -SkipLast 1 | ConvertFrom-Json | Select-Object -ExpandProperty AppConfigurationConnectionString
```

### Non-Windows
In a Bash shell:
```
dotnet user-secrets set --project ./src/services/common/Services/Services.csproj AppConfigurationConnectionString `az appconfig credential list --name <name> --resource-group <resource-group> --query "[?name=='Primary'].connectionString | [0]" --output tsv`
```
Then check the value of the secret:
```
dotnet user-secrets list --project ./src/services/common/Services/Services.csproj --json | sed '1d;$d' | jq --raw-output '.AppConfigurationConnectionString'
```

# Building
## Build all services
```
dotnet build Mmm.Iot.sln
```
## Build an individual service
```
dotnet build ./<service-name>/<service-name>.sln
```
E.g., to build the Storage Adapter service:
```
dotnet build ./storage-adapter/storage-adapter.sln
```

## Build a Docker image for an individual service
You must provide a value for the `AppConfigurationConnectionString` environment variable to the Docker build. This value is a secret and must not be set directly in the Dockerfile via the `ENV` instruction. Therefore, you must provide the value in the `--build-args` option of the `docker build` command.

```
docker build --file ./<service-name>/WebService/Dockerfile --build-arg AppConfigurationConnectionString=$AppConfgurationConnectionString .
```
where `$AppConfigurationConnectionString` is either an environment variable or dotnet user secret.

E.g., to build the Storage Adapter container image:
```
docker build --file ./storage-adapter/WebService/Dockerfile --build-arg AppConfigurationConnectionString=$AppConfgurationConnectionString .
```

# Running
## Run all services

### Azure DevSpaces
#### Non-Windows
First, setup your Azure DevSpaces for use
```
azds use \
  --name <Name of your managed kubernetes cluster> \
  --resource-group <Name of the resource group your cluster is within>
```

After setting up Azure DevSpaces, you can build and run each microservice in Azure DevSpaces. From the root of the project, run the script ```up.sh```. To build and run each service in Azure DevSpaces.
```
./up.sh
```

Each service that is built and ran by ```up.sh``` has a unqiue url in the format of ```http://my-dev-space.s.default.service-name.ABC1234DE.cus.azds.io```. You can use this url to begin making requests to each service's API endpoints.

## Run an individual service
The simplest is to use `dotnet run` to spin up a service on a random port on localhost:
```
dotnet run --project ./<service-name>/WebService/WebService.csproj
```
# Debugging
Use either Visual Studio or Visual Studio Code
