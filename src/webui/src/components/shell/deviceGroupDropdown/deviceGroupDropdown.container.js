// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withNamespaces } from "react-i18next";
import {
    epics as appEpics,
    getDeviceGroups,
    getActiveDeviceGroupId,
    getUserCurrentTenant,
} from "store/reducers/appReducer";
import { getDeviceStatistics } from "store/reducers/devicesReducer";

import { DeviceGroupDropdown } from "./deviceGroupDropdown";

const mapStateToProps = (state) => ({
        deviceGroups: getDeviceGroups(state),
        activeDeviceGroupId: getActiveDeviceGroupId(state),
        deviceStatistics: getDeviceStatistics(state),
        currentTenantId: getUserCurrentTenant(state),
    }),
    // Wrap the dispatch method
    mapDispatchToProps = (dispatch) => ({
        changeDeviceGroup: (id) =>
            dispatch(appEpics.actions.updateActiveDeviceGroup(id)),
        logEvent: (diagnosticsModel) =>
            dispatch(appEpics.actions.logEvent(diagnosticsModel)),
    });

export const DeviceGroupDropdownContainer = withNamespaces()(
    connect(mapStateToProps, mapDispatchToProps)(DeviceGroupDropdown)
);
