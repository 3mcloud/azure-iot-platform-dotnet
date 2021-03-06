// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";

import {
    AjaxError,
    Indicator,
    StatGroup,
    StatProperty,
} from "components/shared";
import { svgs, renderUndefined, isDef } from "utilities";
import {
    Panel,
    PanelHeader,
    PanelHeaderLabel,
    PanelContent,
    PanelOverlay,
    PanelError,
} from "components/pages/dashboard/panel";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./overviewPanel.module.scss"));

export class OverviewPanel extends Component {
    constructor(props) {
        super(props);

        this.state = { isPending: true };
    }

    render() {
        const {
                alerting,
                t,
                error,
                isPending,
                activeDeviceGroup,
                openCriticalCount,
                openWarningCount,
                onlineDeviceCount,
                offlineDeviceCount,
            } = this.props,
            deviceDataloaded =
                isDef(onlineDeviceCount) && isDef(offlineDeviceCount),
            showOverlay =
                isPending &&
                (!openCriticalCount || !openWarningCount || !deviceDataloaded),
            total = deviceDataloaded
                ? onlineDeviceCount + offlineDeviceCount
                : undefined,
            deviceGroupName = activeDeviceGroup
                ? activeDeviceGroup.displayName
                : t("dashboard.panels.overview.allDevices");

        return (
            <Panel>
                <PanelHeader>
                    <PanelHeaderLabel>
                        {t("dashboard.panels.overview.header")}
                    </PanelHeaderLabel>
                </PanelHeader>
                <PanelContent className={css("device-stats-container")}>
                    <div className={css("stat-header")}>{deviceGroupName}</div>
                    <StatGroup className={css("stats-group")}>
                        {alerting.jobState === "Running" && (
                            <StatProperty
                                className={css("stat-property")}
                                value={renderUndefined(openCriticalCount)}
                                label={t("dashboard.panels.overview.critical")}
                                svg={svgs.critical}
                                size="medium"
                                svgClassName={css("severity-critical")}
                            />
                        )}
                        {alerting.jobState === "Running" && (
                            <StatProperty
                                className={css("stat-property")}
                                value={renderUndefined(openWarningCount)}
                                label={t("dashboard.panels.overview.warnings")}
                                svg={svgs.warning}
                                size="medium"
                                svgClassName={css("severity-warning")}
                            />
                        )}
                        <StatProperty
                            className={css("stat-property")}
                            value={renderUndefined(total)}
                            label={t("dashboard.panels.overview.total")}
                            size="medium"
                        />
                        <StatProperty
                            className={css("stat-property")}
                            value={renderUndefined(onlineDeviceCount)}
                            label={t("dashboard.panels.overview.connected")}
                            size="medium"
                        />
                        <StatProperty
                            className={css("stat-property")}
                            value={renderUndefined(offlineDeviceCount)}
                            label={t("dashboard.panels.overview.notConnected")}
                            size="medium"
                        />
                    </StatGroup>
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
