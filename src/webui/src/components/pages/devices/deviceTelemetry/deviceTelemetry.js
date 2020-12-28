// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import { Observable, Subject } from "rxjs";
import moment from "moment";
import {
    ComponentArray,
    Btn,
    ContextMenu,
    ContextMenuAlign,
    TimeSeriesInsightsLinkContainer,
    RefreshBarContainer as RefreshBar,
} from "components/shared";

import Config from "app.config";
import { TimeIntervalDropdownContainer as TimeIntervalDropdown } from "components/shell/timeIntervalDropdown";
import {
    TelemetryChartContainer as TelemetryChart,
    chartColorObjects,
} from "components/pages/dashboard/panels/telemetry";
import { transformTelemetryResponse } from "components/pages/dashboard/panels";
import { svgs, int } from "utilities";
import { TelemetryService } from "services";

export class DeviceTelemetry extends Component {
    constructor(props) {
        super(props);

        this.resetTelemetry$ = new Subject();
        this.telemetryRefresh$ = new Subject();
        this.state = {
            telemetry: {},
            telemetryQueryExceededLimit: false,
            deviceIds: props.location.state.deviceIds,
            lastRefreshed: undefined,
            isDeviceSearch: false,
        };
    }

    componentWillMount() {
        if (
            this.props &&
            this.props.location.pathname === "/deviceSearch/telemetry"
        ) {
            this.setState({
                isDeviceSearch: true,
            });
        } else {
            this.setState({
                isDeviceSearch: false,
            });
        }
    }

    componentDidMount() {
        const {
            device: { telemetry: { interval = "0" } = {} } = {},
        } = this.props;
        const [hours = 0, minutes = 0, seconds = 0] = interval
                .split(":")
                .map(int),
            refreshInterval = ((hours * 60 + minutes) * 60 + seconds) * 1000,
            // Telemetry stream - START
            onPendingStart = () => this.setState({ telemetryIsPending: true }),
            telemetry$ = this.resetTelemetry$
                .do((_) => this.setState({ telemetry: {} }))
                .switchMap(
                    (deviceIds) =>
                        TelemetryService.getTelemetryByDeviceId(
                            deviceIds,
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
                                            deviceIds
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
                this.setState(
                    { ...telemetryState, lastRefreshed: moment() },
                    () => this.telemetryRefresh$.next("r")
                ),
            (telemetryError) =>
                this.setState({ telemetryError, telemetryIsPending: false })
        );

        this.resetTelemetry$.next(this.state.deviceIds);
    }

    componentWillUnmount() {
        this.telemetrySubscription.unsubscribe();
    }

    navigateToDevices = () => {
        if (this.state.isDeviceSearch) {
            this.props.history.push("/deviceSearch");
        } else {
            this.props.history.push("/devices");
        }
    };

    updateTimeInterval = (timeInterval) => {
        this.props.updateTimeInterval(timeInterval);
        this.resetTelemetry$.next(this.state.deviceIds);
    };

    refreshTelemetry = () => {
        this.resetTelemetry$.next(this.state.deviceIds);
    };

    render() {
        const { t, theme, timeSeriesExplorerUrl } = this.props,
            { telemetry, lastRefreshed } = this.state,
            // Add parameters to Time Series Insights Url
            timeSeriesParamUrl = timeSeriesExplorerUrl
                ? timeSeriesExplorerUrl +
                  '&relativeMillis=1800000&timeSeriesDefinitions=[{"name":"Devices","splitBy":"iothub-connection-device-id"}]'
                : undefined;
        return (
            <ComponentArray>
                <ContextMenu>
                    <ContextMenuAlign left={true}>
                        <Btn svg={svgs.return} onClick={this.navigateToDevices}>
                            {t("devices.returnToDevices")}
                        </Btn>
                    </ContextMenuAlign>
                    <ContextMenuAlign right={true}>
                        <TimeIntervalDropdown
                            onChange={this.updateTimeInterval}
                            value={this.props.timeInterval}
                            t={t}
                        />
                        <RefreshBar
                            refresh={this.refreshTelemetry}
                            time={lastRefreshed}
                            t={t}
                            isShowIconOnly={true}
                        />
                    </ContextMenuAlign>
                </ContextMenu>
                {timeSeriesExplorerUrl && (
                    <TimeSeriesInsightsLinkContainer
                        href={timeSeriesParamUrl}
                    />
                )}
                <TelemetryChart
                    className="telemetry-chart"
                    t={t}
                    limitExceeded={this.state.telemetryQueryExceededLimit}
                    telemetry={telemetry}
                    theme={theme}
                    colors={chartColorObjects}
                />
            </ComponentArray>
        );
    }
}
