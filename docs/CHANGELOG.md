# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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

[Unreleased]: https://github.com/3mcloud/azure-iot-platform-dotnet/compare/5.0.0...HEAD
[5.0.1]: https://github.com/3mcloud/azure-iot-platform-dotnet/compare/5.0.1...5.0.0
[5.0.0]: https://github.com/3mcloud/azure-iot-platform-dotnet/releases/tag/5.0.0
