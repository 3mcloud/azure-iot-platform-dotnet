// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withTranslation } from "react-i18next";
import { Maintenance } from "./maintenance";
import {
    epics as appEpics,
    redux as appRedux,
    getTheme,
    getTimeInterval,
    getDeviceGroups,
    getAlerting,
} from "store/reducers/appReducer";
import {
    epics as rulesEpics,
    getEntities as getRuleEntities,
    getRulesError,
    getRulesPendingStatus,
    getRulesLastUpdated,
} from "store/reducers/rulesReducer";
import {
    getEntities as getDeviceEntities,
    getDevicesLastUpdated,
    getDevicesPendingStatus,
} from "store/reducers/devicesReducer";

// Pass the devices status
const mapStateToProps = (state) => ({
        alerting: getAlerting(state),
        deviceEntities: getDeviceEntities(state),
        deviceLastUpdated: getDevicesLastUpdated(state),
        devicesIsPending: getDevicesPendingStatus(state),
        rulesEntities: getRuleEntities(state),
        rulesError: getRulesError(state),
        rulesIsPending: getRulesPendingStatus(state),
        rulesLastUpdated: getRulesLastUpdated(state),
        deviceGroups: getDeviceGroups(state),
        theme: getTheme(state),
        timeInterval: getTimeInterval(state),
    }),
    // Wrap the dispatch method
    mapDispatchToProps = (dispatch) => ({
        fetchRules: () => dispatch(rulesEpics.actions.fetchRules()),
        updateTimeInterval: (timeInterval) =>
            dispatch(appRedux.actions.updateTimeInterval(timeInterval)),
        updateCurrentWindow: (currentWindow) =>
            dispatch(appRedux.actions.updateCurrentWindow(currentWindow)),
        logEvent: (diagnosticsModel) =>
            dispatch(appEpics.actions.logEvent(diagnosticsModel)),
    });

export const MaintenanceContainer = withTranslation()(
    connect(mapStateToProps, mapDispatchToProps)(Maintenance)
);
