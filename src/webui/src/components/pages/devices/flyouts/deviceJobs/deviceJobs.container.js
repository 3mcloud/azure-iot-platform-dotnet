// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withNamespaces } from "react-i18next";
import { DeviceJobs } from "./deviceJobs";
import { redux as devicesRedux } from "store/reducers/devicesReducer";
import { epics as appEpics, getTheme } from "store/reducers/appReducer";

// Pass the global info needed
const mapStateToProps = (state) => ({
        theme: getTheme(state),
    }),
    // Wrap the dispatch method
    mapDispatchToProps = (dispatch) => ({
        updateTags: (device) =>
            dispatch(devicesRedux.actions.updateTags(device)),
        updateProperties: (device) =>
            dispatch(devicesRedux.actions.updateProperties(device)),
        logEvent: (diagnosticsModel) =>
            dispatch(appEpics.actions.logEvent(diagnosticsModel)),
    });

export const DeviceJobsContainer = withNamespaces()(
    connect(mapStateToProps, mapDispatchToProps)(DeviceJobs)
);
