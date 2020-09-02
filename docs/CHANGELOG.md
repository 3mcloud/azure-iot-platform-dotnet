# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

<!---
To easily get a list of committed changes between current master and the previous release use:
git log --oneline --no-decorate --topo-order ^<previousRelease> master
where <previousRelease> is the release name e.g 5.1.0
-->
## [5.2.3] - 2020-08-18
### Fixed
- Asa-maager twin update job properly triggers device group conversion
- Updated methods to throw expcetion when there is no collection
- Prevent Azure Function calls when alerting is diabled
- Updated ResourceNotFoundException in Rules Methods
- Removed telemetry entries in health probes to reduce logging costs

### Added
- Improved control over tracking and managing the deployments imposed by IoT Hub


## [5.2.2] - 2020-07-26
### Fixed
- Added and updated translations for phrases for German, English, French, Spanish, Hindi, Tamil, and Vietnamese
- Pinned device groups are now properly saved
- Active device group now switches on new session
- Fixed issues with alerts being disabled for rules affecting device groups with numeric conditions
- Re-enable cross-partition queries for alarms
- Location for DPS now uses configuration value instead of hard-coded "eastus"

## [5.2.1] - 2020-07-15
### Fixed
- Corrected the application version number

## [5.2.0] - 2020-07-15
### Added
- Show application version number in settings flyout with link to changelog for release notes
- Updated the display of device names in telemetry chart
- Device group sorting
- Package firmware JSON template is now fully customizable with a configurable default
- Access device file uploads in device details flyout
- Allow the creation of supported methods per device group
- Timeframe for telemetry chart in device details flyout is now configurable
- Telemetry chart displays explanation when incomplete dataset is shown due to message count limits
- Enable configuration of device telemetry message retrieval count limit

### Fixed
- Re-enable cross-partition queries for device telemetry messages
- Clarified language in system settings privacy notice
- Telemetry chart attributes no longer show unnecessary left-right scroll buttons
- Enabling advanced alerting in the settings panel no longer shows as failed
- Default logo now appears with correct size
- Prevent stack trace from appearing in error message when API returns HTTP 500
- Now default to read batched data timestamp in seconds in ASA
- Sign-in to Outlook is no longer required for enabling emails on rules
- Prevent occasional blank screen in Edge browser
- Custom role names now appear correctly in User Profile flyout and elsewhere

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

[5.2.3]: https://github.com/3mcloud/azure-iot-platform-dotnet/releases/tag/5.2.3
[5.2.2]: https://github.com/3mcloud/azure-iot-platform-dotnet/releases/tag/5.2.2
[5.2.1]: https://github.com/3mcloud/azure-iot-platform-dotnet/releases/tag/5.2.1
[5.2.0]: https://github.com/3mcloud/azure-iot-platform-dotnet/releases/tag/5.2.0
[5.1.0]: https://github.com/3mcloud/azure-iot-platform-dotnet/releases/tag/5.1.0
[5.0.1]: https://github.com/3mcloud/azure-iot-platform-dotnet/releases/tag/5.0.1
[5.0.0]: https://github.com/3mcloud/azure-iot-platform-dotnet/releases/tag/5.0.0
