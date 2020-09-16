# azure-iot-messaging-functions
Azure Functions for writing Event Hub messages to Cosmos for our IoT Platform

## Run locally
To run the functions locally and view the function logging from your console:
  1. Install [.NET Core 2.1](https://dotnet.microsoft.com/download/dotnet-core/2.1)
  1. Install [Azure Functions CLI](https://github.com/Azure/azure-functions-core-tools#installing)
  4. `func start`

## Working in the Azure Portal
The function app (message-functions-odin-mt-poc) where these functions have been deployed to is set to readonly for these functions. They can only be editted in this repo. Once your changes have been merged to master, they can be deplyed to the function app by following these steps:
  1. Open the function app in the Azure portal
  2. From the "Overview" page, there is a link to the "Deployment options configured for ExternalGit" - click this
  3. From the deployment page, along the top there is a "Sync" button - click this
  4. Wait a few minutes and check the status of your deployment to ensure your changes were deployed successfully.

## Setting up deployment to a Function App
For directions on setting up the deployment pipeline described above, see [here](https://stackoverflow.com/a/45514955).
