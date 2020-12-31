// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { DevicesGrid } from "./devicesGrid";
import {
    epics as appEpics,
    getActiveDeviceGroupId,
    getUser,
    getUserCurrentTenant,
} from "store/reducers/appReducer";

const mapStateToProps = (state) => ({
        activeDeviceGroupId: getActiveDeviceGroupId(state),
        userPermissions: getUser(state).permissions,
        currentTenantId: getUserCurrentTenant(state),
    }),
    // Wrap the dispatch method
    mapDispatchToProps = (dispatch) => ({
        logEvent: (diagnosticsModel) =>
            dispatch(appEpics.actions.logEvent(diagnosticsModel)),
    });

export const DevicesGridContainer = connect(
    mapStateToProps,
    mapDispatchToProps
)(DevicesGrid);
