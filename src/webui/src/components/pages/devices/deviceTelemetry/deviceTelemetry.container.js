// Copyright (c) Microsoft. All rights reserved.

import { withNamespaces } from "react-i18next";
import { connect } from "react-redux";
import { DeviceTelemetry } from "./deviceTelemetry";
import {
    redux as appRedux,
    getTimeInterval,
    getTheme,
    getTimeSeriesExplorerUrl,
} from "store/reducers/appReducer";

// Pass the global info needed
const mapStateToProps = (state) => ({
        timeInterval: getTimeInterval(state),
        theme: getTheme(state),
        timeSeriesExplorerUrl: getTimeSeriesExplorerUrl(state),
    }),
    // Wrap the dispatch methods
    mapDispatchToProps = (dispatch) => ({
        updateTimeInterval: (timeInterval) =>
            dispatch(appRedux.actions.updateTimeInterval(timeInterval)),
    });

export const DeviceTelemetryContainer = withNamespaces()(
    connect(mapStateToProps, mapDispatchToProps)(DeviceTelemetry)
);
