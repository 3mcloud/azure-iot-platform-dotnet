# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

<!---
To easily get a list of committed changes between current master and the previous release use:
git log --oneline --no-decorate --topo-order ^<previousRelease> master
where <previousRelease> is the release name e.g 5.1.0
-->

## [5.1.0] - 2020-06-12
### Added
- Cache IoT Hub device twin query results to greatly reduce throttling and latency
- Clicking a package name in the Packages page now displays the package JSON data
- Improved logo formatting and styles

### Fixed
- Custom fields added to package JSON are now properly persisted and deployed
- Alerting infrastructure now properly configured to send emails when alerts trigger

## [5.0.1] - 2020-06-09
### Fixed
- Use local timezone for time display in telemetry chart
- Greatly reduce frequency of UI rendering errors resulting from IoT Hub query throttling
- New deployments now have package name and version
- Add missing files related to package management that were lost when repository was transplanted

## [5.0.0] - 2020-05-22
### Added
- Multi-tenancy: sandboxed IoT environments within a single deployment infrastructure but with separate data storage and users per tenant
- Identity Gateway microservice for chaining authentication flow to another OAuth provider
- Azure Pipelines YAML for deploying infrastructure and code

### Changed
- Streaming is now done serverless using Azure Functions
- Application configuration uses Azure App Configuration service in addition to Azure Key Vault
- Code base rearchitected to use common library and reduce duplication

[5.1.0]: https://github.com/3mcloud/azure-iot-platform-dotnet/releases/tag/5.1.0%2B7c220f1fbb11917602c14497d5410b242c0ed11a.63385
[5.0.1]: https://github.com/3mcloud/azure-iot-platform-dotnet/releases/tag/5.0.1
[5.0.0]: https://github.com/3mcloud/azure-iot-platform-dotnet/releases/tag/5.0.0
