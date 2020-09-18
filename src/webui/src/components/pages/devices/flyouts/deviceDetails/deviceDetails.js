// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import { Observable, Subject } from "rxjs";
import { Trans } from "react-i18next";
import moment from "moment";
import {
    Balloon,
    BalloonPosition,
} from "@microsoft/azure-iot-ux-fluent-controls/lib/components/Balloon/Balloon";

import Config from "app.config";
import { TelemetryService, IoTHubManagerService } from "services";
import { DeviceIcon } from "./deviceIcon";
import { RulesGrid, rulesColumnDefs } from "components/pages/rules/rulesGrid";
import {
    copyToClipboard,
    int,
    svgs,
    translateColumnDefs,
    formatTime,
    DEFAULT_TIME_FORMAT,
} from "utilities";
import {
    Btn,
    BtnToolbar,
    ComponentArray,
    ErrorMsg,
    PropertyGrid as Grid,
    PropertyGridBody as GridBody,
    PropertyGridHeader as GridHeader,
    PropertyRow as Row,
    PropertyCell as Cell,
    SectionDesc,
    TimeSeriesInsightsLinkContainer,
} from "components/shared";
import { TimeIntervalDropdownContainer as TimeIntervalDropdown } from "components/shell/timeIntervalDropdown";
import Flyout from "components/shared/flyout";
import {
    TelemetryChartContainer as TelemetryChart,
    chartColorObjects,
} from "components/pages/dashboard/panels/telemetry";
import { transformTelemetryResponse } from "components/pages/dashboard/panels";
import { getEdgeAgentStatusCode } from "utilities";

import "./deviceDetails.scss";

const Section = Flyout.Section,
    serializeNestedDeviceProperties = (parentName, value) => {
        if (typeof value !== "object" || value === null) {
            let prop = {};
            prop[parentName] = value;
            return prop;
        }

        let nestedProperties = {};
        Object.entries(value).forEach(([key, value]) => {
            nestedProperties = {
                ...nestedProperties,
                ...serializeNestedDeviceProperties(
                    `${parentName}.${key}`,
                    value
                ),
            };
        });
        return nestedProperties;
    };

export class DeviceDetails extends Component {
    constructor(props) {
        super(props);

        this.state = {
            alerts: undefined,
            isAlertsPending: false,
            alertsError: undefined,

            telemetry: {},
            telemetryIsPending: true,
            telemetryError: null,
            telemetryQueryExceededLimit: false,

            showRawMessage: false,
            currentModuleStatus: undefined,
            deviceUploads: undefined,
            deviceDeployments: undefined,
            expandedValue: "no",
        };
        this.baseState = this.state;
        this.columnDefs = [
            {
                ...rulesColumnDefs.ruleName,
                cellRendererFramework: undefined, // Don't allow soft select from an open flyout
            },
            rulesColumnDefs.severity,
            rulesColumnDefs.alertStatus,
            rulesColumnDefs.explore,
        ];

        this.resetTelemetry$ = new Subject();
        this.telemetryRefresh$ = new Subject();
        if (this.props.moduleStatus) {
            this.state = {
                ...this.state,
                currentModuleStatus: this.props.moduleStatus,
            };
        } else {
            this.props.fetchModules(this.props.device.id);
        }
        this.handleDoubleClickItem = this.handleDoubleClickItem.bind(this);
    }

    componentDidMount() {
        if (!this.props.rulesLastUpdated) {
            this.props.fetchRules();
        }

        const {
                device = {},
                device: { telemetry: { interval = "0" } = {} } = {},
            } = this.props,
            deviceId = device.id;
        this.fetchAlerts(deviceId);
        this.fetchDeviceUploads(deviceId);
        this.fetchDeviceDeployments(deviceId);

        const [hours = 0, minutes = 0, seconds = 0] = interval
                .split(":")
                .map(int),
            refreshInterval = ((hours * 60 + minutes) * 60 + seconds) * 1000,
            // Telemetry stream - START
            onPendingStart = () => this.setState({ telemetryIsPending: true }),
            telemetry$ = this.resetTelemetry$
                .do((_) => this.setState({ telemetry: {} }))
                .switchMap(
                    (deviceId) =>
                        TelemetryService.getTelemetryByDeviceId(
                            [deviceId],
                            TimeIntervalDropdown.getTimeIntervalDropdownValue()
                        )
                            .flatMap((items) => {
                                this.setState({
                                    telemetryQueryExceededLimit:
                                        items.length >= 1000,
                                });
                                return Observable.of(items);
                            })
                            .merge(
                                this.telemetryRefresh$ // Previous request complete
                                    .delay(
                                        refreshInterval ||
                                            Config.dashboardRefreshInterval
                                    ) // Wait to refresh
                                    .do(onPendingStart)
                                    .flatMap((_) =>
                                        TelemetryService.getTelemetryByDeviceIdP1M(
                                            [deviceId]
                                        )
                                    )
                            )
                            .flatMap((messages) =>
                                transformTelemetryResponse(
                                    () => this.state.telemetry
                                )(messages).map((telemetry) => ({
                                    telemetry,
                                    lastMessage: messages[0],
                                }))
                            )
                            .map((newState) => ({
                                ...newState,
                                telemetryIsPending: false,
                            })) // Stream emits new state
                );
        // Telemetry stream - END

        this.telemetrySubscription = telemetry$.subscribe(
            (telemetryState) =>
                this.setState(telemetryState, () =>
                    this.telemetryRefresh$.next("r")
                ),
            (telemetryError) =>
                this.setState({ telemetryError, telemetryIsPending: false })
        );

        this.resetTelemetry$.next(deviceId);
    }

    componentWillReceiveProps(nextProps) {
        const {
            deviceModuleStatus,
            isDeviceModuleStatusPending,
            deviceModuleStatusError,
            moduleStatus,
            resetPendingAndError,
            device,
            fetchModules,
        } = nextProps;
        let tempState = {};
        /*
            deviceModuleStatus is a prop fetched by making fetchModules() API call through deviceDetails.container on demand.
            moduleStatus is a prop sent from deploymentDetailsGrid which it already has in rowData.
            Both deviceModuleStatus and moduleStatus have the same content,
                but come from different sources based on the page that opens this flyout.
            Depending on which one is available, currentModuleStatus is set in component state.
        */

        if ((this.props.device || {}).id !== device.id) {
            // Reset state if the device changes.
            resetPendingAndError();
            tempState = { ...this.baseState };

            if (moduleStatus) {
                // If moduleStatus exist in props, set it in state.
                tempState = {
                    ...tempState,
                    currentModuleStatus: moduleStatus,
                };
            } else {
                // Otherwise make an API call to get deviceModuleStatus.
                fetchModules(device.id);
            }

            const deviceId = (device || {}).id;
            this.resetTelemetry$.next(deviceId);
            this.fetchAlerts(deviceId);
            this.fetchDeviceUploads(deviceId);
        } else if (
            !moduleStatus &&
            !isDeviceModuleStatusPending &&
            !deviceModuleStatusError
        ) {
            // set deviceModuleStatus in state, if moduleStatus doesn't exist and devicesReducer successfully received the API response.
            tempState = { currentModuleStatus: deviceModuleStatus };
        }

        if (Object.keys(tempState).length) {
            this.setState(tempState);
        }
    }

    componentWillUnmount() {
        this.alertSubscription.unsubscribe();
        this.telemetrySubscription.unsubscribe();
        this.deviceUploadsSubscription.unsubscribe();
    }

    copyDevicePropertiesToClipboard = () => {
        if (this.props.device) {
            copyToClipboard(JSON.stringify(this.props.device.properties || {}));
        }
    };

    toggleRawDiagnosticsMessage = () => {
        this.setState({ showRawMessage: !this.state.showRawMessage });
    };

    applyRuleNames = (alerts, rules) =>
        alerts.map((alert) => ({
            ...alert,
            name: (rules[alert.ruleId] || {}).name,
        }));

    fetchAlerts = (deviceId) => {
        this.setState({ isAlertsPending: true });

        this.alertSubscription = TelemetryService.getAlerts({
            limit: 5,
            order: "desc",
            devices: deviceId,
        }).subscribe(
            (alerts) =>
                this.setState({
                    alerts,
                    isAlertsPending: false,
                    alertsError: undefined,
                }),
            (alertsError) =>
                this.setState({ alertsError, isAlertsPending: false })
        );
    };

    fetchDeviceUploads = (deviceId) => {
        this.deviceUploadsSubscription = TelemetryService.getDeviceUploads(
            deviceId
        ).subscribe((deviceUploads) => {
            this.setState({
                deviceUploads,
            });
        });
    };

    fetchDeviceDeployments = (deviceId) => {
        IoTHubManagerService.getDeploymentHistoryForSelectedDevice(
            deviceId
        ).subscribe((deviceDeployments) => {
            var filteredDeployments = [];
            deviceDeployments.forEach((deployment) => {
                if (deployment) {
                    filteredDeployments.push(deployment);
                }
            });
            this.setState({
                deviceDeployments: filteredDeployments,
            });
        });
    };

    downloadFile = (relativePath, fileName) => {
        TelemetryService.getDeviceUploadsFileContent(relativePath).subscribe(
            (response) => {
                var blob = new Blob([response.response], {
                    type: response.response.contentType,
                });
                let url = window.URL.createObjectURL(blob);
                let a = document.createElement("a");
                a.href = url;
                a.download = fileName;
                a.click();
            }
        );
    };

    updateTimeInterval = (timeInterval) => {
        this.props.updateTimeInterval(timeInterval);
        this.resetTelemetry$.next(this.props.device.id);
    };

    handleDoubleClickItem() {
        if (this.state.expandedValue === "no") {
            this.setState({
                expandedValue: "yes",
            });
        } else {
            this.setState({
                expandedValue: "no",
            });
        }
    }

    render() {
        const {
                t,
                onClose,
                device,
                theme,
                timeSeriesExplorerUrl,
                isDeviceModuleStatusPending,
                deviceModuleStatusError,
            } = this.props,
            { telemetry, lastMessage, currentModuleStatus } = this.state,
            lastMessageTime = (lastMessage || {}).time,
            isPending = this.state.isAlertsPending && this.props.isRulesPending,
            rulesGridProps = {
                rowData: isPending
                    ? undefined
                    : this.applyRuleNames(
                          this.state.alerts || [],
                          this.props.rules || []
                      ),
                t: this.props.t,
                deviceGroups: this.props.deviceGroups,
                domLayout: "autoHeight",
                columnDefs: translateColumnDefs(this.props.t, this.columnDefs),
                suppressFlyouts: true,
            },
            tags = Object.entries(device.tags || {}),
            properties = Object.entries(device.properties || {}),
            deviceUploads = this.state.deviceUploads || [],
            deviceDeployments = this.state.deviceDeployments || [],
            moduleQuerySuccessful =
                currentModuleStatus &&
                currentModuleStatus !== {} &&
                !isDeviceModuleStatusPending &&
                !deviceModuleStatusError,
            // Add parameters to Time Series Insights Url

            timeSeriesParamUrl = timeSeriesExplorerUrl
                ? timeSeriesExplorerUrl +
                  `&relativeMillis=1800000&timeSeriesDefinitions=[{"name":"${
                      device.id
                  }","measureName":"${
                      Object.keys(telemetry).sort()[0]
                  }","predicate":"'${device.id}'"}]`
                : undefined;

        return (
            <div onDoubleClick={this.handleDoubleClickItem}>
                <Flyout.Container
                    header={t("devices.flyouts.details.title")}
                    t={t}
                    onClose={onClose}
                    expanded={this.state.expandedValue}
                >
                    <div className="device-details-container">
                        {!device && (
                            <div className="device-details-container">
                                <ErrorMsg>
                                    {t("devices.flyouts.details.noDevice")}
                                </ErrorMsg>
                            </div>
                        )}
                        {!!device && (
                            <div className="device-details-container">
                                <Grid className="device-details-header">
                                    <Row>
                                        <Cell className="col-3">
                                            <DeviceIcon type={device.type} />
                                        </Cell>
                                        <Cell className="col-7">
                                            <div className="device-name">
                                                {device.id}
                                            </div>
                                            <div className="device-simulated">
                                                {device.isSimulated
                                                    ? t(
                                                          "devices.flyouts.details.simulated"
                                                      )
                                                    : t(
                                                          "devices.flyouts.details.notSimulated"
                                                      )}
                                            </div>
                                            <div className="device-connected">
                                                {device.connected
                                                    ? t(
                                                          "devices.flyouts.details.connected"
                                                      )
                                                    : t(
                                                          "devices.flyouts.details.notConnected"
                                                      )}
                                            </div>
                                        </Cell>
                                    </Row>
                                </Grid>

                                {!this.state.isAlertsPending &&
                                    this.state.alerts &&
                                    this.state.alerts.length > 0 && (
                                        <RulesGrid {...rulesGridProps} />
                                    )}

                                <Section.Container>
                                    <Section.Header>
                                        {t(
                                            "devices.flyouts.details.telemetry.title"
                                        )}
                                    </Section.Header>
                                    <Section.Content>
                                        <TimeIntervalDropdown
                                            onChange={this.updateTimeInterval}
                                            value={this.props.timeInterval}
                                            t={t}
                                            className="device-details-time-interval-dropdown"
                                        />
                                        {timeSeriesExplorerUrl && (
                                            <TimeSeriesInsightsLinkContainer
                                                href={timeSeriesParamUrl}
                                            />
                                        )}
                                        <TelemetryChart
                                            className="telemetry-chart"
                                            t={t}
                                            limitExceeded={
                                                this.state
                                                    .telemetryQueryExceededLimit
                                            }
                                            telemetry={telemetry}
                                            theme={theme}
                                            colors={chartColorObjects}
                                        />
                                    </Section.Content>
                                </Section.Container>

                                <Section.Container>
                                    <Section.Header>
                                        {t(
                                            "devices.flyouts.details.tags.title"
                                        )}
                                    </Section.Header>
                                    <Section.Content>
                                        <SectionDesc>
                                            <Trans
                                                i18nKey={
                                                    "devices.flyouts.details.tags.description"
                                                }
                                            >
                                                To edit, close this panel, click
                                                on
                                                <strong>
                                                    {{
                                                        jobs: t(
                                                            "devices.flyouts.jobs.title"
                                                        ),
                                                    }}
                                                </strong>
                                                then select
                                                <strong>
                                                    {{
                                                        tags: t(
                                                            "devices.flyouts.jobs.tags.radioLabel"
                                                        ),
                                                    }}
                                                </strong>
                                                .
                                            </Trans>
                                        </SectionDesc>
                                        {tags.length === 0 &&
                                            t(
                                                "devices.flyouts.details.tags.noneExist"
                                            )}
                                        {tags.length > 0 && (
                                            <Grid>
                                                <GridHeader>
                                                    <Row>
                                                        <Cell className="col-3">
                                                            {t(
                                                                "devices.flyouts.details.tags.keyHeader"
                                                            )}
                                                        </Cell>
                                                        <Cell className="col-7">
                                                            {t(
                                                                "devices.flyouts.details.tags.valueHeader"
                                                            )}
                                                        </Cell>
                                                    </Row>
                                                </GridHeader>
                                                <GridBody>
                                                    {tags.map(
                                                        (
                                                            [tagName, tagValue],
                                                            idx
                                                        ) => (
                                                            <Row key={idx}>
                                                                <Cell className="col-3">
                                                                    {tagName}
                                                                </Cell>
                                                                <Cell className="col-7">
                                                                    {tagValue.toString()}
                                                                </Cell>
                                                            </Row>
                                                        )
                                                    )}
                                                </GridBody>
                                            </Grid>
                                        )}
                                    </Section.Content>
                                </Section.Container>

                                <Section.Container>
                                    <Section.Header>
                                        {t(
                                            "devices.flyouts.details.methods.title"
                                        )}
                                    </Section.Header>
                                    <Section.Content>
                                        <SectionDesc>
                                            <Trans
                                                i18nKey={
                                                    "devices.flyouts.details.methods.description"
                                                }
                                            >
                                                To edit, close this panel, click
                                                on
                                                <strong>
                                                    {{
                                                        jobs: t(
                                                            "devices.flyouts.jobs.title"
                                                        ),
                                                    }}
                                                </strong>
                                                then select
                                                <strong>
                                                    {{
                                                        methods: t(
                                                            "devices.flyouts.jobs.methods.radioLabel"
                                                        ),
                                                    }}
                                                </strong>
                                                .
                                            </Trans>
                                        </SectionDesc>
                                        {device.methods.length === 0 ? (
                                            t(
                                                "devices.flyouts.details.methods.noneExist"
                                            )
                                        ) : (
                                            <Grid>
                                                {device.methods.map(
                                                    (methodName, idx) => (
                                                        <Row key={idx}>
                                                            <Cell>
                                                                {methodName}
                                                            </Cell>
                                                        </Row>
                                                    )
                                                )}
                                            </Grid>
                                        )}
                                    </Section.Content>
                                </Section.Container>

                                <Section.Container>
                                    <Section.Header>
                                        {t(
                                            "devices.flyouts.details.properties.title"
                                        )}
                                    </Section.Header>
                                    <Section.Content>
                                        <SectionDesc>
                                            <Trans
                                                i18nKey={
                                                    "devices.flyouts.details.properties.description"
                                                }
                                            >
                                                To edit, close this panel, click
                                                on
                                                <strong>
                                                    {{
                                                        jobs: t(
                                                            "devices.flyouts.jobs.title"
                                                        ),
                                                    }}
                                                </strong>
                                                then select
                                                <strong>
                                                    {{
                                                        properties: t(
                                                            "devices.flyouts.jobs.properties.radioLabel"
                                                        ),
                                                    }}
                                                </strong>
                                                .
                                            </Trans>
                                        </SectionDesc>
                                        {properties.length === 0 &&
                                            t(
                                                "devices.flyouts.details.properties.noneExist"
                                            )}
                                        {properties.length > 0 && (
                                            <ComponentArray>
                                                <Grid>
                                                    <GridHeader>
                                                        <Row>
                                                            <Cell className="col-3">
                                                                {t(
                                                                    "devices.flyouts.details.properties.keyHeader"
                                                                )}
                                                            </Cell>
                                                            <Cell className="col-15">
                                                                {t(
                                                                    "devices.flyouts.details.properties.valueHeader"
                                                                )}
                                                            </Cell>
                                                        </Row>
                                                    </GridHeader>
                                                    <GridBody>
                                                        {properties.map(
                                                            (
                                                                [
                                                                    propertyName,
                                                                    propertyValue,
                                                                ],
                                                                idx
                                                            ) => {
                                                                const desiredPropertyValue =
                                                                        device
                                                                            .desiredProperties[
                                                                            propertyName
                                                                        ],
                                                                    serializedProperties = serializeNestedDeviceProperties(
                                                                        propertyName,
                                                                        propertyValue
                                                                    ),
                                                                    rows = [];
                                                                Object.entries(
                                                                    serializedProperties
                                                                ).forEach(
                                                                    ([
                                                                        propertyDisplayName,
                                                                        value,
                                                                    ]) => {
                                                                        const displayValue =
                                                                                !desiredPropertyValue ||
                                                                                value ===
                                                                                    desiredPropertyValue
                                                                                    ? value.toString()
                                                                                    : t(
                                                                                          "devices.flyouts.details.properties.syncing",
                                                                                          {
                                                                                              reportedPropertyValue: value.toString(),
                                                                                              desiredPropertyValue: desiredPropertyValue.toString(),
                                                                                          }
                                                                                      ),
                                                                            truncatedDisplayName =
                                                                                propertyDisplayName.length <=
                                                                                20
                                                                                    ? propertyDisplayName
                                                                                    : `...${propertyDisplayName.substring(
                                                                                          propertyDisplayName.length -
                                                                                              17
                                                                                      )}`;
                                                                        rows.push(
                                                                            <Row
                                                                                key={
                                                                                    idx
                                                                                }
                                                                            >
                                                                                <Cell className="col-3">
                                                                                    <Balloon
                                                                                        position={
                                                                                            BalloonPosition.Left
                                                                                        }
                                                                                        tooltip={
                                                                                            <Trans>
                                                                                                {
                                                                                                    propertyDisplayName
                                                                                                }
                                                                                            </Trans>
                                                                                        }
                                                                                    >
                                                                                        {
                                                                                            truncatedDisplayName
                                                                                        }
                                                                                    </Balloon>
                                                                                </Cell>
                                                                                <Cell className="col-15">
                                                                                    {
                                                                                        displayValue
                                                                                    }
                                                                                </Cell>
                                                                            </Row>
                                                                        );
                                                                    }
                                                                );
                                                                return rows;
                                                            }
                                                        )}
                                                    </GridBody>
                                                </Grid>
                                                <Grid className="device-properties-actions">
                                                    <Row>
                                                        <Cell className="col-8">
                                                            {t(
                                                                "devices.flyouts.details.properties.copyAllProperties"
                                                            )}
                                                        </Cell>
                                                        <Cell className="col-2">
                                                            <Btn
                                                                svg={svgs.copy}
                                                                onClick={
                                                                    this
                                                                        .copyDevicePropertiesToClipboard
                                                                }
                                                            >
                                                                {t(
                                                                    "devices.flyouts.details.properties.copy"
                                                                )}
                                                            </Btn>
                                                        </Cell>
                                                    </Row>
                                                </Grid>
                                            </ComponentArray>
                                        )}
                                    </Section.Content>
                                </Section.Container>

                                <Section.Container>
                                    <Section.Header>
                                        {t(
                                            "devices.flyouts.details.diagnostics.title"
                                        )}
                                    </Section.Header>
                                    <Section.Content>
                                        <SectionDesc>
                                            {t(
                                                "devices.flyouts.details.diagnostics.description"
                                            )}
                                        </SectionDesc>

                                        <Grid className="device-details-diagnostics">
                                            <GridHeader>
                                                <Row>
                                                    <Cell className="col-3">
                                                        {t(
                                                            "devices.flyouts.details.diagnostics.keyHeader"
                                                        )}
                                                    </Cell>
                                                    <Cell className="col-15">
                                                        {t(
                                                            "devices.flyouts.details.diagnostics.valueHeader"
                                                        )}
                                                    </Cell>
                                                </Row>
                                            </GridHeader>
                                            <GridBody>
                                                <Row>
                                                    <Cell className="col-3">
                                                        {t(
                                                            "devices.flyouts.details.diagnostics.status"
                                                        )}
                                                    </Cell>
                                                    <Cell className="col-15">
                                                        {device.connected
                                                            ? t(
                                                                  "devices.flyouts.details.connected"
                                                              )
                                                            : t(
                                                                  "devices.flyouts.details.notConnected"
                                                              )}
                                                    </Cell>
                                                </Row>
                                                {device.connected && (
                                                    <ComponentArray>
                                                        <Row>
                                                            <Cell className="col-3">
                                                                {t(
                                                                    "devices.flyouts.details.diagnostics.lastMessage"
                                                                )}
                                                            </Cell>
                                                            <Cell className="col-15">
                                                                {lastMessageTime
                                                                    ? moment(
                                                                          lastMessageTime
                                                                      ).format(
                                                                          DEFAULT_TIME_FORMAT
                                                                      )
                                                                    : "---"}
                                                            </Cell>
                                                        </Row>
                                                        <Row>
                                                            <Cell className="col-3">
                                                                {t(
                                                                    "devices.flyouts.details.diagnostics.message"
                                                                )}
                                                            </Cell>
                                                            <Cell className="col-15">
                                                                <Btn
                                                                    className="raw-message-button"
                                                                    onClick={
                                                                        this
                                                                            .toggleRawDiagnosticsMessage
                                                                    }
                                                                >
                                                                    {t(
                                                                        "devices.flyouts.details.diagnostics.showMessage"
                                                                    )}
                                                                </Btn>
                                                            </Cell>
                                                        </Row>
                                                    </ComponentArray>
                                                )}
                                                {this.state.showRawMessage && (
                                                    <Row>
                                                        <pre>
                                                            {JSON.stringify(
                                                                lastMessage,
                                                                null,
                                                                2
                                                            )}
                                                        </pre>
                                                    </Row>
                                                )}
                                            </GridBody>
                                        </Grid>
                                    </Section.Content>
                                </Section.Container>

                                <Section.Container>
                                    <Section.Header>
                                        {t(
                                            "devices.flyouts.details.modules.title"
                                        )}
                                    </Section.Header>
                                    <Section.Content>
                                        <SectionDesc>
                                            {t(
                                                "devices.flyouts.details.modules.description"
                                            )}
                                        </SectionDesc>
                                        <div className="device-details-deployment-contentbox">
                                            {!moduleQuerySuccessful &&
                                                t(
                                                    "devices.flyouts.details.modules.noneExist"
                                                )}
                                            {moduleQuerySuccessful && (
                                                <ComponentArray>
                                                    <div>
                                                        {
                                                            currentModuleStatus.code
                                                        }
                                                        :{" "}
                                                        {getEdgeAgentStatusCode(
                                                            currentModuleStatus.code,
                                                            t
                                                        )}
                                                    </div>
                                                    <div>
                                                        {
                                                            currentModuleStatus.description
                                                        }
                                                    </div>
                                                </ComponentArray>
                                            )}
                                        </div>
                                    </Section.Content>
                                </Section.Container>
                                <Section.Container>
                                    <Section.Header>
                                        {t(
                                            "devices.flyouts.details.deviceUploads.title"
                                        )}
                                    </Section.Header>
                                    <Section.Content>
                                        <SectionDesc>
                                            {t(
                                                "devices.flyouts.details.deviceUploads.description"
                                            )}
                                        </SectionDesc>
                                        <div className="device-details-deviceuploads-contentbox">
                                            {deviceUploads.length === 0 &&
                                                t(
                                                    "devices.flyouts.details.deviceUploads.noneExist"
                                                )}
                                            {deviceUploads.length > 0 && (
                                                <Grid className="device-details-deviceuploads">
                                                    <GridHeader>
                                                        <Row>
                                                            <Cell className="col-7">
                                                                {t(
                                                                    "devices.flyouts.details.deviceUploads.fileName"
                                                                )}
                                                            </Cell>
                                                            <Cell className="col-3">
                                                                {t(
                                                                    "devices.flyouts.details.deviceUploads.action"
                                                                )}
                                                            </Cell>
                                                        </Row>
                                                    </GridHeader>
                                                    <GridBody>
                                                        {deviceUploads.map(
                                                            (upload, idx) => (
                                                                <Row key={idx}>
                                                                    <Cell className="col-3">
                                                                        <Balloon
                                                                            position={
                                                                                BalloonPosition.Left
                                                                            }
                                                                            tooltip={
                                                                                <div>
                                                                                    <Grid className="device-details-deviceuploads-popup">
                                                                                        <GridHeader>
                                                                                            <Row>
                                                                                                <Cell className="col-3">
                                                                                                    {t(
                                                                                                        "devices.flyouts.details.deviceUploads.property"
                                                                                                    )}
                                                                                                </Cell>
                                                                                                <Cell className="col-6">
                                                                                                    {t(
                                                                                                        "devices.flyouts.details.deviceUploads.value"
                                                                                                    )}
                                                                                                </Cell>
                                                                                            </Row>
                                                                                        </GridHeader>
                                                                                        <GridBody>
                                                                                            <Row>
                                                                                                <Cell className="col-3">
                                                                                                    Size
                                                                                                </Cell>
                                                                                                <Cell className="col-6">
                                                                                                    {upload.Size.toString()}
                                                                                                </Cell>
                                                                                            </Row>
                                                                                            <Row>
                                                                                                <Cell className="col-3">
                                                                                                    Uploaded
                                                                                                    On
                                                                                                </Cell>
                                                                                                <Cell className="col-6">
                                                                                                    {formatTime(
                                                                                                        upload.UploadedOn
                                                                                                    )}
                                                                                                </Cell>
                                                                                            </Row>
                                                                                            <Row>
                                                                                                <Cell className="col-3">
                                                                                                    Uploaded
                                                                                                    By
                                                                                                </Cell>
                                                                                                <Cell className="col-6">
                                                                                                    {
                                                                                                        upload.UploadedBy
                                                                                                    }
                                                                                                </Cell>
                                                                                            </Row>
                                                                                        </GridBody>
                                                                                    </Grid>
                                                                                </div>
                                                                            }
                                                                        >
                                                                            {
                                                                                upload.Name
                                                                            }
                                                                        </Balloon>
                                                                    </Cell>
                                                                    <Cell className="col-3">
                                                                        <Btn
                                                                            svg={
                                                                                svgs.upload
                                                                            }
                                                                            className="download-deviceupload"
                                                                            onClick={() =>
                                                                                this.downloadFile(
                                                                                    upload.BlobName,
                                                                                    upload.Name
                                                                                )
                                                                            }
                                                                        ></Btn>
                                                                    </Cell>
                                                                </Row>
                                                            )
                                                        )}
                                                    </GridBody>
                                                </Grid>
                                            )}
                                        </div>
                                    </Section.Content>
                                </Section.Container>
                                <Section.Container>
                                    <Section.Header>
                                        {t(
                                            "devices.flyouts.details.deviceDeployments.title"
                                        )}
                                    </Section.Header>
                                    <Section.Content>
                                        <SectionDesc>
                                            {t(
                                                "devices.flyouts.details.deviceDeployments.description"
                                            )}
                                        </SectionDesc>
                                        <div className="device-details-deviceDeployments-contentbox">
                                            {deviceDeployments.length === 0 &&
                                                t(
                                                    "devices.flyouts.details.deviceDeployments.noneExist"
                                                )}
                                            {deviceDeployments.length > 0 && (
                                                <Grid className="device-details-deviceDeployments">
                                                    <GridHeader>
                                                        <Row>
                                                            <Cell className="col-4">
                                                                {t(
                                                                    "devices.flyouts.details.deviceDeployments.firmwareVersion"
                                                                )}
                                                            </Cell>
                                                            <Cell className="col-4">
                                                                {t(
                                                                    "devices.flyouts.details.deviceDeployments.startDate"
                                                                )}
                                                            </Cell>
                                                            <Cell className="col-4">
                                                                {t(
                                                                    "devices.flyouts.details.deviceDeployments.endDate"
                                                                )}
                                                            </Cell>
                                                        </Row>
                                                    </GridHeader>
                                                    <GridBody>
                                                        {deviceDeployments.map(
                                                            (
                                                                deployment,
                                                                idx
                                                            ) => (
                                                                <Row key={idx}>
                                                                    <Cell className="col-4">
                                                                        {
                                                                            deployment.firmwareVersion
                                                                        }
                                                                    </Cell>
                                                                    <Cell className="col-4">
                                                                        {formatTime(
                                                                            deployment.startTime
                                                                        )}
                                                                    </Cell>
                                                                    <Cell className="col-4">
                                                                        {formatTime(
                                                                            deployment.endTime
                                                                        )}
                                                                    </Cell>
                                                                </Row>
                                                            )
                                                        )}
                                                    </GridBody>
                                                </Grid>
                                            )}
                                        </div>
                                    </Section.Content>
                                </Section.Container>
                            </div>
                        )}
                        <BtnToolbar>
                            <Btn svg={svgs.cancelX} onClick={onClose}>
                                {t("devices.flyouts.details.close")}
                            </Btn>
                        </BtnToolbar>
                    </div>
                </Flyout.Container>
            </div>
        );
    }
}
