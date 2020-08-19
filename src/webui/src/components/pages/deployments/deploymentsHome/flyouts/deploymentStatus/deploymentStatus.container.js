// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withNamespaces } from "react-i18next";
import { DeploymentStatus } from "./deploymentStatus";
import {
    getDeleteDeploymentError,
    getDeleteDeploymentPendingStatus,
    epics as deploymentsEpics,
    redux as deploymentsRedux,
} from "store/reducers/deploymentsReducer";
import { epics as appEpics } from "store/reducers/appReducer";

// Pass the global info needed
const mapStateToProps = (state) => ({
        deleteIsPending: getDeleteDeploymentPendingStatus(state),
        deleteError: getDeleteDeploymentError(state),
    }),
    // Wrap the dispatch methods
    mapDispatchToProps = (dispatch) => ({
        fetchDeployments: () =>
            dispatch(deploymentsEpics.actions.fetchDeployments()),
        fetchDeployment: (id) =>
            dispatch(deploymentsEpics.actions.fetchDeployment(id)),
        resetDeployedDevices: () =>
            dispatch(deploymentsRedux.actions.resetDeployedDevices()),
        deleteItem: (deploymentId) =>
            dispatch(deploymentsEpics.actions.deleteDeployment(deploymentId)),
        reactivateDeployment: (deploymentId) =>
            dispatch(
                deploymentsEpics.actions.reactivateDeployment(deploymentId)
            ),
        logEvent: (diagnosticsModel) =>
            dispatch(appEpics.actions.logEvent(diagnosticsModel)),
    });

export const DeploymentStatusContainer = withNamespaces()(
    connect(mapStateToProps, mapDispatchToProps)(DeploymentStatus)
);
