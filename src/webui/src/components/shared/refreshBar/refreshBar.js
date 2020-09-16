// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import moment from "moment";
import { Btn } from "components/shared";
import { toDiagnosticsModel } from "services/models";
import { svgs, DEFAULT_TIME_FORMAT } from "utilities";
import {
    Balloon,
    BalloonPosition,
    BalloonAlignment,
} from "@microsoft/azure-iot-ux-fluent-controls/lib/components/Balloon/Balloon";

import "./refreshBar.scss";

export class RefreshBar extends Component {
    refresh = () => {
        this.props.logEvent(toDiagnosticsModel("Refresh_Click", {}));
        return !this.props.isPending && this.props.refresh();
    };

    getLastRefreshDetails = () => {
        const { t, isPending, time, isShowIconOnly } = this.props;
        if (isPending || time) {
            return (
                <span className="time">
                    <span className="refresh-text">
                        {t("refreshBar.lastRefreshed")}
                        {isShowIconOnly ? (
                            <>
                                : <br />
                            </>
                        ) : (
                            <>| </>
                        )}
                    </span>
                    {!isPending ? (
                        moment(time).format(DEFAULT_TIME_FORMAT)
                    ) : (
                        <span className="empty-text"></span>
                    )}
                </span>
            );
        }
        return null;
    };

    render() {
        const { t, isPending, isShowIconOnly } = this.props;
        return (
            <div className="last-updated-container">
                {isShowIconOnly ? (
                    <Balloon
                        position={BalloonPosition.Bottom}
                        align={BalloonAlignment.End}
                        tooltip={this.getLastRefreshDetails()}
                    >
                        <Btn
                            svg={svgs.refresh}
                            aria-label={t("refreshBar.ariaLabel")}
                            className={`refresh-btn ${
                                isPending ? "refreshing" : ""
                            }`}
                            onClick={this.refresh}
                        />
                    </Balloon>
                ) : (
                    <>
                        {this.getLastRefreshDetails()}
                        <Btn
                            svg={svgs.refresh}
                            aria-label={t("refreshBar.ariaLabel")}
                            className={`refresh-btn ${
                                isPending ? "refreshing" : ""
                            }`}
                            onClick={this.refresh}
                        />
                    </>
                )}
            </div>
        );
    }
}
