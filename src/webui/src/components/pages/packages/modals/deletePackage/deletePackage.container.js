// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withTranslation } from "react-i18next";
import { DeleteModal } from "components/shared";
import {
    getDeletePackageError,
    getDeletePackagePendingStatus,
    epics as packagesEpics,
} from "store/reducers/packagesReducer";
import { epics as appEpics } from "store/reducers/appReducer";

// Pass the global info needed
const mapStateToProps = (state) => ({
        isPending: getDeletePackagePendingStatus(state),
        error: getDeletePackageError(state),
    }),
    // Wrap the dispatch methods
    mapDispatchToProps = (dispatch) => ({
        deleteItem: (packageId) =>
            dispatch(packagesEpics.actions.deletePackage(packageId)),
        logEvent: (diagnosticsModel) =>
            dispatch(appEpics.actions.logEvent(diagnosticsModel)),
    });

export const PackageDeleteContainer = withTranslation()(
    connect(mapStateToProps, mapDispatchToProps)(DeleteModal)
);
