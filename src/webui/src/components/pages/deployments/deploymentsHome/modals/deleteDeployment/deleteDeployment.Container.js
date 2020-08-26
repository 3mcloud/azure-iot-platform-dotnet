// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withNamespaces } from "react-i18next";
import { DeleteModal } from "components/shared";
import {
    getDeleteDeploymentError,
    epics as deploymentEpics,
    getDeleteDeploymentPendingStatus,
} from "store/reducers/deploymentsReducer";
import { epics as appEpics } from "store/reducers/appReducer";

// Pass the global info needed
const mapStateToProps = (state) => ({
        isPending: getDeleteDeploymentPendingStatus(state),
        error: getDeleteDeploymentError(state),
    }),
    // Wrap the dispatch methods
    mapDispatchToProps = (dispatch) => ({
        deleteItem: (deploymentId) =>
            dispatch(deploymentEpics.actions.deleteDeployment(deploymentId)),
        logEvent: (diagnosticsModel) =>
            dispatch(appEpics.actions.logEvent(diagnosticsModel)),
    });

export const DeploymentDeleteContainer = withNamespaces()(
    connect(mapStateToProps, mapDispatchToProps)(DeleteModal)
);
