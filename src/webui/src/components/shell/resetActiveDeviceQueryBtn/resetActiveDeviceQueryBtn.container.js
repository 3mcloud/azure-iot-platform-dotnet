// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withNamespaces } from "react-i18next";
import {
    epics as appEpics,
    redux as appRedux,
} from "store/reducers/appReducer";
import { epics as devicesEpics } from "store/reducers/devicesReducer";

import { ResetActiveDeviceQueryBtn } from "./resetActiveDeviceQueryBtn";

const mapDispatchToProps = (dispatch) => ({
    resetActiveDeviceQueryConditions: () =>
        dispatch(appRedux.actions.setActiveDeviceQueryConditions([])),
    fetchDevices: () => dispatch(devicesEpics.actions.fetchDevices()),
    fetchDeviceStatistics: () => dispatch(devicesEpics.actions.fetchDeviceStatistics()),
    logEvent: (diagnosticsModel) =>
        dispatch(appEpics.actions.logEvent(diagnosticsModel)),
});

export const ResetActiveDeviceQueryBtnContainer = withNamespaces()(
    connect(null, mapDispatchToProps)(ResetActiveDeviceQueryBtn)
);
