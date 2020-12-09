// Copyright (c) Microsoft. All rights reserved.

import "rxjs";
import { Observable } from "rxjs";
import moment from "moment";
import { schema, normalize } from "normalizr";
import update from "immutability-helper";
import dot from "dot-object";
import { createSelector } from "reselect";
import { IoTHubManagerService } from "services";
import {
    getActiveDeviceGroupId,
    getActiveDeviceGroupConditions,
} from "./appReducer";
import {
    createReducerScenario,
    createEpicScenario,
    errorPendingInitialState,
    pendingReducer,
    errorReducer,
    setPending,
    resetPendingAndErrorReducer,
    getPending,
    getError,
    toActionCreator,
} from "store/utilities";
import { packagesEnum } from "services/models";

// ========================= Epics - START
const handleError = (fromAction) => (error) =>
        Observable.of(
            redux.actions.registerError(fromAction.type, { error, fromAction })
        ),
    getDeployedDeviceIds = (payload) => {
        return Object.keys(dot.pick("deviceStatuses", payload))
            .map((id) => `'${id}'`)
            .join();
    },
    createEdgeAgentQuery = (ids) =>
        `"deviceId IN [${ids}] AND moduleId = '$edgeAgent'"`,
    createDevicesQuery = (ids) => `"deviceId IN [${ids}]"`;

export const epics = createEpicScenario({
    /** Loads all Deployments */
    fetchDeployments: {
        type: "DEPLOYMENTS_FETCH",
        epic: (fromAction) =>
            IoTHubManagerService.getDeployments()
                .map(
                    toActionCreator(redux.actions.updateDeployments, fromAction)
                )
                .catch(handleError(fromAction)),
    },
    /** Loads a single Deployment */
    fetchDeployment: {
        type: "DEPLOYMENT_DETAILS_FETCH",
        epic: (fromAction) =>
            IoTHubManagerService.getDeployment(
                fromAction.payload.id,
                fromAction.payload.isLatest
            )
                .flatMap((response) => [
                    toActionCreator(
                        redux.actions.updateDeployment,
                        fromAction
                    )(response),
                    epics.actions.fetchDeployedDevices(response),
                ])
                .catch(handleError(fromAction)),
    },
    /** Loads the queried edgeAgents and devices */
    fetchDeployedDevices: {
        type: "DEPLOYED_DEVICES_FETCH",
        epic: (fromAction) => {
            if (
                fromAction.payload.packageType ===
                packagesEnum.deviceConfiguration
            ) {
                return IoTHubManagerService.getDevicesByQueryForDeployment(
                    fromAction.payload.id,
                    createDevicesQuery(
                        getDeployedDeviceIds(fromAction.payload)
                    ),
                    fromAction.payload.isLatest
                )
                    .map(
                        toActionCreator(
                            redux.actions.updateADMDeployedDevices,
                            fromAction
                        )
                    )
                    .catch(handleError(fromAction));
            }
            return Observable.forkJoin(
                IoTHubManagerService.getModulesByQueryForDeployment(
                    fromAction.payload.id,
                    createEdgeAgentQuery(
                        getDeployedDeviceIds(fromAction.payload)
                    ),
                    fromAction.payload.isLatest
                ),
                IoTHubManagerService.getDevicesByQueryForDeployment(
                    fromAction.payload.id,
                    createDevicesQuery(
                        getDeployedDeviceIds(fromAction.payload)
                    ),
                    fromAction.payload.isLatest
                )
            )
                .map(
                    toActionCreator(
                        redux.actions.updateDeployedDevices,
                        fromAction
                    )
                )
                .catch(handleError(fromAction));
        },
    },
    /** Create a new deployment */
    createDeployment: {
        type: "DEPLOYMENTS_CREATE",
        epic: (fromAction) =>
            IoTHubManagerService.createDeployment(fromAction.payload)
                .map(
                    toActionCreator(redux.actions.insertDeployment, fromAction)
                )
                .catch(handleError(fromAction)),
    },
    /** Delete deployment */
    deleteDeployment: {
        type: "DEPLOYMENTS_DELETE",
        epic: (fromAction) =>
            IoTHubManagerService.deleteDeployment(fromAction.payload)
                .map(
                    toActionCreator(redux.actions.deleteDeployment, fromAction)
                )
                .catch(handleError(fromAction)),
    },
    reactivateDeployment: {
        type: "DEPLOYMENTS_REACTIVATE",
        epic: (fromAction) =>
            IoTHubManagerService.reactivateDeployment(fromAction.payload)
                .map(
                    toActionCreator(
                        redux.actions.reactivateDeployment,
                        fromAction
                    )
                )
                .catch(handleError(fromAction)),
    },
});
// ========================= Epics - END

// ========================= Schemas - START
const deploymentSchema = new schema.Entity("deployments"),
    deploymentListSchema = new schema.Array(deploymentSchema),
    deployedDevicesSchema = new schema.Entity("deployedDevices"),
    deployedDevicesListSchema = new schema.Array(deployedDevicesSchema),
    // ========================= Schemas - END

    // ========================= Reducers - START
    initialState = { ...errorPendingInitialState, entities: {} },
    insertDeploymentReducer = (state, { payload, fromAction }) => {
        const {
            entities: { deployments },
            result,
        } = normalize({ ...payload, isNew: true }, deploymentSchema);
        if (state.entities && state.entities.deployments) {
            return update(state, {
                entities: { deployments: { $merge: deployments } },
                items: { $splice: [[0, 0, result]] },
                ...setPending(fromAction.type, false),
            });
        }
        return update(state, {
            entities: { deployments: { $set: deployments } },
            items: { $set: [result] },
            ...setPending(fromAction.type, false),
        });
    },
    deleteDeploymentReducer = (state, { fromAction }) => {
        const idx = state.items.indexOf(fromAction.payload);
        return update(state, {
            entities: { deployments: { $unset: [fromAction.payload] } },
            items: { $splice: [[idx, 1]] },
            ...setPending(fromAction.type, false),
        });
    },
    reactivateDeploymentReducer = (state, { fromAction }) => {
        const idx = state.items.indexOf(fromAction.payload);
        return update(state, {
            entities: { deployments: { $unset: [fromAction.payload] } },
            items: { $splice: [[idx, 1]] },
            ...setPending(fromAction.type, false),
        });
    },
    updateDeploymentsReducer = (state, { payload, fromAction }) => {
        const {
            entities: { deployments },
            result,
        } = normalize(payload, deploymentListSchema);
        return update(state, {
            entities: { deployments: { $set: deployments } },
            items: { $set: result },
            lastUpdated: { $set: moment() },
            ...setPending(fromAction.type, false),
        });
    },
    updateDeploymentReducer = (state, { payload, fromAction }) => {
        return update(state, {
            currentDeployment: { $set: payload },
            currentDeploymentLastUpdated: { $set: moment() },
            ...setPending(fromAction.type, false),
        });
    },
    updateEdgeDeployedDevicesReducer = (
        state,
        { payload: [modules, devices], fromAction }
    ) => {
        const normalizedDevices =
                normalize(devices.items, deployedDevicesListSchema).entities
                    .deployedDevices || {},
            normalizedModules =
                normalize(modules, deployedDevicesListSchema).entities
                    .deployedDevices || {},
            deployedDevices = Object.keys(normalizedDevices).reduce(
                (acc, deviceId) => ({
                    ...acc,
                    [deviceId]: {
                        ...(acc[deviceId] || {}),
                        firmware: normalizedDevices[deviceId].firmware,
                        device: normalizedDevices[deviceId],
                    },
                }),
                normalizedModules
            );
        return update(state, {
            entities: {
                deployedDevices: { $set: deployedDevices },
            },
            ...setPending(fromAction.type, false),
        });
    },
    updateADMDeployedDevicesReducer = (state, { payload, fromAction }) => {
        const normalizedDevices =
                normalize(payload.items, deployedDevicesListSchema).entities
                    .deployedDevices || {},
            deployedDevices = Object.keys(normalizedDevices).reduce(
                (acc, deviceId) => ({
                    ...acc,
                    [deviceId]: {
                        id: deviceId,
                        start:
                            normalizedDevices[deviceId].lastFwUpdateStartTime,
                        end: normalizedDevices[deviceId].lastFwUpdateEndTime,
                        firmware: normalizedDevices[deviceId].firmware,
                        device: normalizedDevices[deviceId],
                    },
                }),
                []
            );
        return update(state, {
            entities: {
                deployedDevices: { $set: deployedDevices },
            },
            ...setPending(fromAction.type, false),
        });
    },
    resetDeployedDevicesReducer = (state) =>
        update(state, {
            entities: {
                $unset: ["deployedDevices"],
            },
        }),
    /* Action types that cause a pending flag */
    fetchableTypes = [
        epics.actionTypes.fetchDeployment,
        epics.actionTypes.fetchDeployments,
        epics.actionTypes.createDeployment,
        epics.actionTypes.deleteDeployment,
        epics.actionTypes.fetchDeployedDevices,
        epics.actionTypes.reactivateDeployment,
    ];

export const redux = createReducerScenario({
    insertDeployment: {
        type: "DEPLOYMENT_INSERT",
        reducer: insertDeploymentReducer,
    },
    deleteDeployment: {
        type: "DEPLOYMENTS_DELETE",
        reducer: deleteDeploymentReducer,
    },
    reactivateDeployment: {
        type: "DEPLOYMENTS_REACTIVATE",
        reducer: reactivateDeploymentReducer,
    },
    updateDeployments: {
        type: "DEPLOYMENTS_UPDATE",
        reducer: updateDeploymentsReducer,
    },
    updateDeployment: {
        type: "DEPLOYMENTS_DETAILS_UPDATE",
        reducer: updateDeploymentReducer,
    },
    updateDeployedDevices: {
        type: "DEPLOYED_DEVICES_UPDATE",
        reducer: updateEdgeDeployedDevicesReducer,
    },
    updateADMDeployedDevices: {
        type: "ADM_DEPLOYED_DEVICES_UPDATE",
        reducer: updateADMDeployedDevicesReducer,
    },
    registerError: { type: "DEPLOYMENTS_REDUCER_ERROR", reducer: errorReducer },
    resetDeployedDevices: {
        type: "DEPLOYMETS_RESET_DEPLOYED_DEVICES",
        reducer: resetDeployedDevicesReducer,
    },
    resetPendingAndError: {
        type: "DEPLOYMENTS_REDUCER_RESET_ERROR_PENDING",
        reducer: resetPendingAndErrorReducer,
    },
    isFetching: { multiType: fetchableTypes, reducer: pendingReducer },
});

export const reducer = { deployments: redux.getReducer(initialState) };
// ========================= Reducers - END

// ========================= Selectors - START
export const getDeploymentsReducer = (state) => state.deployments;
export const getEntities = (state) =>
    getDeploymentsReducer(state).entities || {};
export const getDeploymentsEntities = (state) =>
    getEntities(state).deployments || {};
export const getItems = (state) => getDeploymentsReducer(state).items || [];
export const getDeploymentsLastUpdated = (state) =>
    getDeploymentsReducer(state).lastUpdated;
export const getDeploymentsError = (state) =>
    getError(getDeploymentsReducer(state), epics.actionTypes.fetchDeployments);
export const getDeploymentsPendingStatus = (state) =>
    getPending(
        getDeploymentsReducer(state),
        epics.actionTypes.fetchDeployments
    );
export const getCreateDeploymentError = (state) =>
    getError(getDeploymentsReducer(state), epics.actionTypes.createDeployment);
export const getCreateDeploymentPendingStatus = (state) =>
    getPending(
        getDeploymentsReducer(state),
        epics.actionTypes.createDeployment
    );
export const getDeleteDeploymentError = (state) =>
    getError(getDeploymentsReducer(state), epics.actionTypes.deleteDeployment);
export const getDeleteDeploymentPendingStatus = (state) =>
    getPending(
        getDeploymentsReducer(state),
        epics.actionTypes.deleteDeployment
    );

export const getReactivateDeploymentError = (state) =>
    getError(
        getDeploymentsReducer(state),
        epics.actionTypes.reactivateDeployment
    );
export const getReactivateDeploymentPendingStatus = (state) =>
    getPending(
        getDeploymentsReducer(state),
        epics.actionTypes.reactivateDeployment
    );
export const getDeployments = createSelector(
    getDeploymentsEntities,
    getItems,
    getActiveDeviceGroupId,
    getActiveDeviceGroupConditions,
    (deployments, items, deviceGroupId, deviceGroupConditions = []) =>
        items.reduce((acc, id) => {
            const deployment = deployments[id],
                activeDeviceGroup =
                    deviceGroupConditions.length > 0 ? deviceGroupId : false;
            return (deployment &&
                deployment.deviceGroupId &&
                deployment.deviceGroupId === activeDeviceGroup) ||
                !activeDeviceGroup
                ? [...acc, deployment]
                : acc;
        }, [])
);
export const getAllDeployments = createSelector(
    getDeploymentsEntities,
    getItems,
    (deployments, items) =>
        items.reduce((acc, id) => {
            const deployment = deployments[id];
            return deployment ? [...acc, deployment] : acc;
        }, [])
);
export const getCurrentDeploymentDetails = (state) =>
    getDeploymentsReducer(state).currentDeployment || {};
export const getCurrentDeploymentLastUpdated = (state) =>
    getDeploymentsReducer(state).currentDeploymentLastUpdated;
export const getDeviceStatuses = (state) =>
    getCurrentDeploymentDetails(state).deviceStatuses || {};
export const getCurrentDeploymentDetailsPendingStatus = (state) =>
    getPending(getDeploymentsReducer(state), epics.actionTypes.fetchDeployment);
export const getCurrentDeploymentDetailsError = (state) =>
    getError(getDeploymentsReducer(state), epics.actionTypes.fetchDeployment);
export const getDeployedDevicesEntities = (state) =>
    getEntities(state).deployedDevices || {};
export const getDeployedDevicesPendingStatus = (state) =>
    getPending(
        getDeploymentsReducer(state),
        epics.actionTypes.fetchDeployedDevices
    );
export const getDeployedDevicesError = (state) =>
    getError(
        getDeploymentsReducer(state),
        epics.actionTypes.fetchDeployedDevices
    );
export const getDeployedDevices = createSelector(
    getDeployedDevicesEntities,
    getDeviceStatuses,
    (DeployedDevicesEntities, deviceStatuses) =>
        Object.values(
            Object.keys(deviceStatuses).reduce(
                (acc, deviceId) => ({
                    ...acc,
                    [deviceId]: {
                        ...(acc[deviceId] || {}),
                        deploymentStatus: deviceStatuses[deviceId],
                    },
                }),
                DeployedDevicesEntities
            )
        )
);
export const getLastItemId = (state) =>
    getItems(state).length > 0 ? getItems(state)[0] : "";
// ========================= Selectors - END
