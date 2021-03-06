// Copyright (c) Microsoft. All rights reserved.

import "rxjs";
import { of } from "rxjs";
import moment from "moment";
import { schema, normalize } from "normalizr";
import update from "immutability-helper";
import { createSelector } from "reselect";
import { ConfigService } from "services";
import { ofType } from "redux-observable";
import { map, distinctUntilChanged, catchError } from "rxjs/operators";

import { getActiveDeviceGroupId, redux as appRedux } from "./appReducer";
import {
    createReducerScenario,
    createEpicScenario,
    errorPendingInitialState,
    resetPendingAndErrorReducer,
    pendingReducer,
    errorReducer,
    setPending,
    getPending,
    getError,
    toActionCreator,
} from "store/utilities";

// ========================= Epics - START
const handleError = (fromAction) => (error) =>
    of(redux.actions.registerError(fromAction.type, { error, fromAction }));

export const epics = createEpicScenario({
    /** Loads Packages*/
    fetchPackages: {
        type: "PACKAGES_FETCH",
        epic: (fromAction, store) =>
            ConfigService.getPackages(getActiveDeviceGroupId(store.value)).pipe(
                map(toActionCreator(redux.actions.updatePackages, fromAction)),
                catchError(handleError(fromAction))
            ),
    },
    /** Loads filtered Packages*/
    fetchFilteredPackages: {
        type: "PACKAGES_FILTERED_FETCH",
        epic: (fromAction, store) =>
            ConfigService.getFilteredPackages(
                getActiveDeviceGroupId(store.value),
                fromAction.payload.packageType,
                fromAction.payload.configType
            ).pipe(
                map(toActionCreator(redux.actions.updatePackages, fromAction)),
                catchError(handleError(fromAction))
            ),
    },
    /** Create a new package */
    createPackage: {
        type: "PACKAGES_CREATE",
        epic: (fromAction) =>
            ConfigService.createPackage(fromAction.payload).pipe(
                map(toActionCreator(redux.actions.insertPackage, fromAction)),
                catchError(handleError(fromAction))
            ),
    },
    /** Delete package */
    deletePackage: {
        type: "PACKAGES_DELETE",
        epic: (fromAction) =>
            ConfigService.deletePackage(fromAction.payload).pipe(
                map(toActionCreator(redux.actions.deletePackage, fromAction)),
                catchError(handleError(fromAction))
            ),
    },
    /** Gets configuration types*/
    fetchConfigTypes: {
        type: "PACKAGES_CONFIG_TYPE_FETCH",
        epic: (fromAction) =>
            ConfigService.getConfigTypes().pipe(
                map(
                    toActionCreator(redux.actions.updateConfigTypes, fromAction)
                ),
                catchError(handleError(fromAction))
            ),
    },

    /* Update the devices if the selected device group changes */
    refreshPackages: {
        type: "PACKAGES_REFRESH",
        rawEpic: ($actions) =>
            $actions.pipe(
                ofType(appRedux.actionTypes.updateActiveDeviceGroup),
                map(({ payload }) => payload),
                distinctUntilChanged(),
                map((_) => epics.actions.fetchPackages())
            ),
    },
});
// ========================= Epics - END

// ========================= Schemas - START
const packageSchema = new schema.Entity("packages"),
    packageListSchema = new schema.Array(packageSchema),
    // ========================= Schemas - END

    // ========================= Reducers - START
    initialState = { ...errorPendingInitialState, entities: {} },
    insertPackageReducer = (state, { payload, fromAction }) => {
        const {
            entities: { packages },
            result,
        } = normalize({ ...payload, isNew: true }, packageSchema);

        if (state.entities) {
            return update(state, {
                entities: { $merge: packages },
                items: { $splice: [[state.items.length, 0, result]] },
                ...setPending(fromAction.type, false),
            });
        }
        return update(state, {
            entities: { $set: packages },
            items: { $set: [result] },
            ...setPending(fromAction.type, false),
        });
    },
    deletePackageReducer = (state, { payload, fromAction }) => {
        const idx = state.items.indexOf(payload);
        return update(state, {
            entities: { $unset: [payload] },
            items: { $splice: [[idx, 1]] },
            ...setPending(fromAction.type, false),
        });
    },
    updatePackagesReducer = (state, { payload, fromAction }) => {
        const {
            entities: { packages },
            result,
        } = normalize(payload, packageListSchema);
        return update(state, {
            entities: { $set: packages },
            items: { $set: result },
            lastUpdated: { $set: moment() },
            ...setPending(fromAction.type, false),
        });
    },
    updateConfigTypesReducer = (state, { payload, fromAction }) => {
        return update(state, {
            configTypes: { $set: payload },
            ...setPending(fromAction.type, false),
        });
    },
    /* Action types that cause a pending flag */
    fetchableTypes = [
        epics.actionTypes.fetchPackages,
        epics.actionTypes.fetchConfigTypes,
        epics.actionTypes.createPackage,
        epics.actionTypes.deletePackage,
    ];

export const redux = createReducerScenario({
    insertPackage: { type: "PACKAGE_INSERT", reducer: insertPackageReducer },
    deletePackage: { type: "PACKAGES_DELETE", reducer: deletePackageReducer },
    updatePackages: { type: "PACKAGES_UPDATE", reducer: updatePackagesReducer },
    updateConfigTypes: {
        type: "PACKAGES_CONFIG_TYPES_UPDATE",
        reducer: updateConfigTypesReducer,
    },
    registerError: { type: "PACKAGES_REDUCER_ERROR", reducer: errorReducer },
    resetPendingAndError: {
        type: "PACKAGES_REDUCER_RESET_ERROR_PENDING",
        reducer: resetPendingAndErrorReducer,
    },
    isFetching: { multiType: fetchableTypes, reducer: pendingReducer },
});

export const reducer = { packages: redux.getReducer(initialState) };
// ========================= Reducers - END

// ========================= Selectors - START
export const getPackagesReducer = (state) => state.packages;
export const getEntities = (state) => getPackagesReducer(state).entities || {};
export const getItems = (state) => getPackagesReducer(state).items || [];
export const getPackagesLastUpdated = (state) =>
    getPackagesReducer(state).lastUpdated;
export const getPackagesError = (state) =>
    getError(getPackagesReducer(state), epics.actionTypes.fetchPackages);
export const getPackagesPendingStatus = (state) =>
    getPending(getPackagesReducer(state), epics.actionTypes.fetchPackages);
export const getCreatePackageError = (state) =>
    getError(getPackagesReducer(state), epics.actionTypes.createPackage);
export const getCreatePackagePendingStatus = (state) =>
    getPending(getPackagesReducer(state), epics.actionTypes.createPackage);
export const getDeletePackageError = (state) =>
    getError(getPackagesReducer(state), epics.actionTypes.deletePackage);
export const getDeletePackagePendingStatus = (state) =>
    getPending(getPackagesReducer(state), epics.actionTypes.deletePackage);
export const getPackages = createSelector(
    getEntities,
    getItems,
    (entities, items) => items.map((id) => entities[id])
);
export const getConfigTypes = (state) =>
    getPackagesReducer(state).configTypes || [];
export const getConfigTypesError = (state) =>
    getError(getPackagesReducer(state), epics.actionTypes.fetchConfigTypes);
export const getConfigTypesPendingStatus = (state) =>
    getPending(getPackagesReducer(state), epics.actionTypes.fetchConfigTypes);
// ========================= Selectors - END
