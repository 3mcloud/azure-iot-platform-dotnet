// Copyright (c) Microsoft. All rights reserved.

import "rxjs";
import { of } from "rxjs";
import update from "immutability-helper";
import { DeviceSimulationService } from "services";
import {
    createReducerScenario,
    createEpicScenario,
    errorPendingInitialState,
    toActionCreator,
    pendingReducer,
    errorReducer,
    setPending,
    getPending,
    getError,
} from "store/utilities";
import { catchError, map } from "rxjs/operators";

// ========================= Epics - START
const handleError = (fromAction) => (error) =>
    of(redux.actions.registerError(fromAction.type, { error, fromAction }));

export const epics = createEpicScenario({
    /** Loads the simulation status */
    fetchSimulationStatus: {
        type: "SIMULATION_STATUS_FETCH",
        epic: (fromAction) =>
            DeviceSimulationService.getSimulatedDevices().pipe(
                map(
                    toActionCreator(
                        redux.actions.getSimulationStatus,
                        fromAction
                    )
                ),
                catchError(handleError(fromAction))
            ),
    },

    /** Toggles the simulation status */
    toggleSimulationStatus: {
        type: "SIMULATION_TOGGLE_STATUS",
        epic: (fromAction) =>
            DeviceSimulationService.toggleSimulation(
                fromAction.payload.etag,
                fromAction.payload.enabled
            ).pipe(
                map(
                    toActionCreator(
                        redux.actions.getSimulationStatus,
                        fromAction
                    )
                ),
                catchError(handleError(fromAction))
            ),
    },

    /** Loads the options for Device Models */
    fetchSimulationDeviceModelOptions: {
        type: "SIMULATION_DEVICE_MODEL_OPTIONS_FETCH",
        epic: (fromAction) =>
            DeviceSimulationService.getDeviceModelSelectOptions().pipe(
                map(
                    toActionCreator(
                        redux.actions.getDeviceModelOptions,
                        fromAction
                    )
                ),
                catchError(handleError(fromAction))
            ),
    },
});
// ========================= Epics - END

/* Action types that cause a pending flag */
const fetchableTypes = [
        epics.actionTypes.toggleSimulationStatus,
        epics.actionTypes.fetchSimulationStatus,
    ],
    // ========================= Reducers - START
    initialState = {
        ...errorPendingInitialState,
        simulationEnabled: undefined,
        simulationEtag: undefined,
        simulationDeviceModelOptions: undefined,
    },
    simulationStatusReducer = (state, { payload, fromAction }) => {
        return update(state, {
            simulationEnabled: { $set: payload.enabled },
            simulationEtag: { $set: payload.eTag },
            ...setPending(fromAction.type, false),
        });
    },
    simulationDeviceModelOptionsReducer = (state, { payload, fromAction }) =>
        update(state, {
            simulationDeviceModelOptions: { $set: payload },
        });

export const redux = createReducerScenario({
    getSimulationStatus: {
        type: "SIMULATION_STATUS",
        reducer: simulationStatusReducer,
    },
    getDeviceModelOptions: {
        type: "SIMULATION_DEVICE_MODEL_OPTIONS",
        reducer: simulationDeviceModelOptionsReducer,
    },
    registerError: { type: "SIMULATION_REDUCER_ERROR", reducer: errorReducer },
    isFetching: { multiType: fetchableTypes, reducer: pendingReducer },
});

export const reducer = { deviceSimulation: redux.getReducer(initialState) };
// ========================= Reducers - END

// ========================= Selectors - START
export const getSimulationReducer = (state) => state.deviceSimulation;
export const isSimulationEnabled = (state) =>
    getSimulationReducer(state).simulationEnabled;
export const getSimulationEtag = (state) =>
    getSimulationReducer(state).simulationEtag;
export const getSimulationDeviceModelOptions = (state) =>
    getSimulationReducer(state).simulationDeviceModelOptions;
export const getSimulationError = (state) =>
    getError(
        getSimulationReducer(state),
        epics.actionTypes.fetchSimulationStatus
    );
export const getSimulationPendingStatus = (state) =>
    getPending(
        getSimulationReducer(state),
        epics.actionTypes.fetchSimulationStatus
    );
export const getToggleSimulationError = (state) =>
    getError(
        getSimulationReducer(state),
        epics.actionTypes.toggleSimulationStatus
    );
export const getToggleSimulationPendingStatus = (state) =>
    getPending(
        getSimulationReducer(state),
        epics.actionTypes.toggleSimulationStatus
    );
// ========================= Selectors - END
