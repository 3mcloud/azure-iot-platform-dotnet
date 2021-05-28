// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withTranslation } from "react-i18next";
import { DeploymentNew } from "./deploymentNew";
import {
    getCreateDeploymentError,
    getCreateDeploymentPendingStatus,
    getLastItemId,
    epics as deploymentsEpics,
    redux as deploymentsRedux,
} from "store/reducers/deploymentsReducer";
import {
    getPackages,
    getPackagesPendingStatus,
    getPackagesError,
    epics as packagesEpics,
    redux as packagesRedux,
    getConfigTypes,
    getConfigTypesError,
    getConfigTypesPendingStatus,
} from "store/reducers/packagesReducer";
import {
    getDeviceGroups,
    getActiveDeviceGroupId,
    epics as appEpics,
} from "store/reducers/appReducer";
import {
    getDevices,
    getDevicesByConditionError,
    getDevicesByConditionPendingStatus,
    epics as devicesEpics,
    redux as devicesRedux,
} from "store/reducers/devicesReducer";

// Pass the global info needed
const mapStateToProps = (state) => ({
        packages: getPackages(state),
        packagesPending: getPackagesPendingStatus(state),
        packagesError: getPackagesError(state),
        deviceGroups: getDeviceGroups(state),
        deviceGroupId: getActiveDeviceGroupId(state),
        devices: getDevices(state),
        devicesPending: getDevicesByConditionPendingStatus(state),
        devicesError: getDevicesByConditionError(state),
        createIsPending: getCreateDeploymentPendingStatus(state),
        createError: getCreateDeploymentError(state),
        createdDeploymentId: getLastItemId(state),
        configTypes: getConfigTypes(state),
        configTypesError: getConfigTypesError(state),
        configTypesIsPending: getConfigTypesPendingStatus(state),
    }),
    // Wrap the dispatch methods
    mapDispatchToProps = (dispatch) => ({
        createDeployment: (deploymentModel) =>
            dispatch(
                deploymentsEpics.actions.createDeployment(deploymentModel)
            ),
        resetCreatePendingError: () =>
            dispatch(
                deploymentsRedux.actions.resetPendingAndError(
                    deploymentsEpics.actions.createDeployment
                )
            ),
        fetchPackages: (packageType, configType) =>
            dispatch(
                packagesEpics.actions.fetchFilteredPackages({
                    packageType,
                    configType,
                })
            ),
        resetPackagesPendingError: () =>
            dispatch(
                packagesRedux.actions.resetPendingAndError(
                    packagesEpics.actions.fetchPackages
                )
            ),
        fetchDevices: (condition) =>
            dispatch(devicesEpics.actions.fetchDevices()),
        resetDevicesPendingError: () =>
            dispatch(
                devicesRedux.actions.resetPendingAndError(
                    devicesEpics.actions.fetchDevicesByCondition
                )
            ),
        logEvent: (diagnosticsModel) =>
            dispatch(appEpics.actions.logEvent(diagnosticsModel)),
        fetchConfigTypes: () =>
            dispatch(packagesEpics.actions.fetchConfigTypes()),
    });

export const DeploymentNewContainer = withTranslation()(
    connect(mapStateToProps, mapDispatchToProps)(DeploymentNew)
);
