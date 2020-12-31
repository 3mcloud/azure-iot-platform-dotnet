// Copyright (c) Microsoft. All rights reserved.

import { withNamespaces } from "react-i18next";
import { connect } from "react-redux";
import { Deployments } from "./deployments";
import {
    getDeploymentsError,
    getDeploymentsPendingStatus,
    getDeployments,
    getDeploymentsLastUpdated,
    epics as deploymentsEpics,
    getAllDeployments,
} from "store/reducers/deploymentsReducer";
import {
    redux as appRedux,
    epics as appEpics,
    getActiveDeviceQueryConditions,
    getActiveDeviceGroupId,
    getUserCurrentTenant,
} from "store/reducers/appReducer";

// Pass the global info needed
const mapStateToProps = (state) => ({
        isPending: getDeploymentsPendingStatus(state),
        error: getDeploymentsError(state),
        deployments: getDeployments(state),
        lastUpdated: getDeploymentsLastUpdated(state),
        activeDeviceQueryConditions: getActiveDeviceQueryConditions(state),
        allActiveDeployments: getAllDeployments(state),
        activeDeviceGroupId: getActiveDeviceGroupId(state),
        currentTenantId: getUserCurrentTenant(state),
    }),
    // Wrap the dispatch methods
    mapDispatchToProps = (dispatch) => ({
        fetchDeployments: () =>
            dispatch(deploymentsEpics.actions.fetchDeployments()),
        updateCurrentWindow: (currentWindow) =>
            dispatch(appRedux.actions.updateCurrentWindow(currentWindow)),
        logEvent: (diagnosticsModel) =>
            dispatch(appEpics.actions.logEvent(diagnosticsModel)),
        checkTenantAndSwitch: (payload) =>
            dispatch(appRedux.actions.checkTenantAndSwitch(payload)),
    });

export const DeploymentsContainer = withNamespaces()(
    connect(mapStateToProps, mapDispatchToProps)(Deployments)
);
