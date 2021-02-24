// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { RuleDetails } from "./ruleDetails";
import {
    epics as appEpics,
    getActiveDeviceQueryConditions,
    redux as appRedux,
    getActiveDeviceGroupId,
    getUserCurrentTenant,
    getUser,
} from "store/reducers/appReducer";

const mapStateToProps = (state) => ({
        activeDeviceQueryConditions: getActiveDeviceQueryConditions(state),
        activeDeviceGroupId: getActiveDeviceGroupId(state),
        currentTenantId: getUserCurrentTenant(state),
        userPermissions: getUser(state).permissions,
    }),
    // Wrap the dispatch method
    mapDispatchToProps = (dispatch) => ({
        logEvent: (diagnosticsModel) =>
            dispatch(appEpics.actions.logEvent(diagnosticsModel)),
        checkTenantAndSwitch: (payload) =>
            dispatch(appRedux.actions.checkTenantAndSwitch(payload)),
    });

export const RuleDetailsContainer = connect(
    mapStateToProps,
    mapDispatchToProps
)(RuleDetails);
