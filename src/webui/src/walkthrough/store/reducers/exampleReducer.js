// Copyright (c) Microsoft. All rights reserved.

// <reducer>
import "rxjs";
import { of } from "rxjs";
import moment from "moment";
import { schema, normalize } from "normalizr";
import update from "immutability-helper";
import { createSelector } from "reselect";
import { ExampleService } from "walkthrough/services";
import {
    createReducerScenario,
    createEpicScenario,
    errorPendingInitialState,
    pendingReducer,
    errorReducer,
    setPending,
    toActionCreator,
    getPending,
    getError,
} from "store/utilities";
import { catchError, map } from "rxjs/operators";

// ========================= Epics - START
const handleError = (fromAction) => (error) =>
    of(redux.actions.registerError(fromAction.type, { error, fromAction }));

export const epics = createEpicScenario({
    /** Loads the example items */
    fetchExamples: {
        type: "EXAMPLES_FETCH",
        epic: (fromAction) =>
            ExampleService.getExampleItems().pipe(
                map(toActionCreator(redux.actions.updateExamples, fromAction)),
                catchError(handleError(fromAction))
            ),
    },
});
// ========================= Epics - END

// ========================= Schemas - START
const itemSchema = new schema.Entity("examples"),
    itemListSchema = new schema.Array(itemSchema),
    // ========================= Schemas - END

    // ========================= Reducers - START
    initialState = {
        ...errorPendingInitialState,
        entities: {},
        items: [],
        lastUpdated: "",
    },
    updateExamplesReducer = (state, { payload, fromAction }) => {
        const {
            entities: { examples },
            result,
        } = normalize(payload, itemListSchema);
        return update(state, {
            entities: { $set: examples },
            items: { $set: result },
            lastUpdated: { $set: moment() },
            ...setPending(fromAction.type, false),
        });
    },
    /* Action types that cause a pending flag */
    fetchableTypes = [epics.actionTypes.fetchExamples];

export const redux = createReducerScenario({
    updateExamples: { type: "EXAMPLES_UPDATE", reducer: updateExamplesReducer },
    registerError: { type: "EXAMPLE_REDUCER_ERROR", reducer: errorReducer },
    isFetching: { multiType: fetchableTypes, reducer: pendingReducer },
});

export const reducer = { examples: redux.getReducer(initialState) };
// ========================= Reducers - END

// ========================= Selectors - START
export const getExamplesReducer = (state) => state.examples;
export const getEntities = (state) => getExamplesReducer(state).entities || {};
export const getItems = (state) => getExamplesReducer(state).items || [];
export const getExamplesLastUpdated = (state) =>
    getExamplesReducer(state).lastUpdated;
export const getExamplesError = (state) =>
    getError(getExamplesReducer(state), epics.actionTypes.fetchExamples);
export const getExamplesPendingStatus = (state) =>
    getPending(getExamplesReducer(state), epics.actionTypes.fetchExamples);
export const getExamples = createSelector(
    getEntities,
    getItems,
    (entities, items) => items.map((id) => entities[id])
);
// ========================= Selectors - END

// </reducer>
