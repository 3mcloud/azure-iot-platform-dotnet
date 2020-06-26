// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import "tsiclient";

import {
    AjaxError,
    Indicator,
    TimeSeriesInsightsLinkContainer,
} from "components/shared";
import {
    Panel,
    PanelContent,
    PanelError,
    PanelHeader,
    PanelHeaderLabel,
    PanelMsg,
    PanelOverlay,
} from "components/pages/dashboard/panel";

import { TelemetryChartContainer as TelemetryChart } from "./telemetryChart.container";

import "./telemetryPanel.scss";

export class TelemetryPanel extends Component {
    render() {
        const {
                t,
                isPending,
                telemetry,
                limitExceeded,
                lastRefreshed,
                theme,
                colors,
                error,
                timeSeriesExplorerUrl,
            } = this.props,
            showOverlay = isPending && !lastRefreshed;
        return (
            <Panel>
                <PanelHeader>
                    <PanelHeaderLabel>
                        {t("dashboard.panels.telemetry.header")}
                    </PanelHeaderLabel>
                </PanelHeader>
                <PanelContent className="telemetry-panel-container">
                    <TelemetryChart
                        t={t}
                        telemetry={telemetry}
                        limitExceeded={limitExceeded}
                        theme={theme}
                        colors={colors}
                    />
                    {!showOverlay && Object.keys(telemetry).length === 0 && (
                        <PanelMsg>{t("dashboard.noData")}</PanelMsg>
                    )}
                    {timeSeriesExplorerUrl && (
                        <TimeSeriesInsightsLinkContainer
                            href={timeSeriesExplorerUrl}
                        />
                    )}
                </PanelContent>
                {showOverlay && (
                    <PanelOverlay>
                        <Indicator />
                    </PanelOverlay>
                )}
                {error && (
                    <PanelError>
                        <AjaxError t={t} error={error} />
                    </PanelError>
                )}
            </Panel>
        );
    }
}
