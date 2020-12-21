// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";

import { Btn } from "components/shared";
import { svgs } from "utilities";
import { toDiagnosticsModel } from "services/models";

export class ResetActiveDeviceQueryBtn extends Component {
    onClick = () => {
        this.props.logEvent(
            toDiagnosticsModel("ResetActiveDeviceQuery_Click", {})
        );
        this.props.resetActiveDeviceQueryConditions();
        this.props.fetchDevices();
        this.props.fetchDeviceStatistics();
    };

    render() {
        return (
            <Btn svg={svgs.cancelX} onClick={this.onClick}>
                {this.props.t("resetActiveDeviceQuery.title")}
            </Btn>
        );
    }
}
