// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withNamespaces } from "react-i18next";
import { Devices } from "./devices";
import {
    epics as devicesEpics,
    redux as devicesRedux,
    getDevices,
    getDevicesError,
    getDevicesLastUpdated,
    getDevicesPendingStatus,
    getDevicesByCondition,
    getDevicesByConditionError,
    getDevicesByConditionPendingStatus,
    getLoadMoreToggleState,
} from "store/reducers/devicesReducer";
import {
    redux as appRedux,
    epics as appEpics,
    getDeviceGroups,
    getDeviceGroupError,
    getActiveDeviceQueryConditions,
    getActiveDeviceGroupConditions,
} from "store/reducers/appReducer";

// Pass the devices status
const mapStateToProps = (state) => ({
        devices: getDevices(state),
        deviceError: getDevicesError(state),
        isPending: getDevicesPendingStatus(state),
        devicesByCondition: getDevicesByCondition(state),
        devicesByConditionError: getDevicesByConditionError(state),
        isDevicesByConditionPanding: getDevicesByConditionPendingStatus(state),
        deviceGroups: getDeviceGroups(state),
        deviceGroupError: getDeviceGroupError(state),
        lastUpdated: getDevicesLastUpdated(state),
        activeDeviceQueryConditions: getActiveDeviceQueryConditions(state),
        activeDeviceGroupConditions: getActiveDeviceGroupConditions(state),
        loadMoreState: getLoadMoreToggleState(state),
    }),
    // Wrap the dispatch method
    mapDispatchToProps = (dispatch) => ({
        fetchDevices: () => dispatch(devicesEpics.actions.fetchDevices()),
        fetchDevicesByCToken: () =>
            dispatch(devicesEpics.actions.fetchDevicesByCToken()),
        updateCurrentWindow: (currentWindow) =>
            dispatch(appRedux.actions.updateCurrentWindow(currentWindow)),
        logEvent: (diagnosticsModel) =>
            dispatch(appEpics.actions.logEvent(diagnosticsModel)),
        cancelDeviceCalls: (payload) =>
            dispatch(devicesRedux.actions.cancelDeviceCalls(payload)),
        checkTenantAndSwitch: (payload) =>
            dispatch(appRedux.actions.checkTenantAndSwitch(payload)),
        resetDeviceByCondition: () =>
            dispatch(devicesRedux.actions.resetDeviceByCondition()),
    });

export const DevicesContainer = withNamespaces()(
    connect(mapStateToProps, mapDispatchToProps)(Devices)
);
