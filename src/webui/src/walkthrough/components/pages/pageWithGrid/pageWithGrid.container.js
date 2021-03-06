// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withTranslation } from "react-i18next";

import {
    epics as exampleEpics,
    getExamples,
    getExamplesError,
    getExamplesLastUpdated,
    getExamplesPendingStatus,
} from "walkthrough/store/reducers/exampleReducer";
import { PageWithGrid } from "./pageWithGrid";

// Pass the data
const mapStateToProps = (state) => ({
        data: getExamples(state),
        error: getExamplesError(state),
        isPending: getExamplesPendingStatus(state),
        lastUpdated: getExamplesLastUpdated(state),
    }),
    // Wrap the dispatch method
    mapDispatchToProps = (dispatch) => ({
        fetchData: () => dispatch(exampleEpics.actions.fetchExamples()),
    });

export const PageWithGridContainer = withTranslation()(
    connect(mapStateToProps, mapDispatchToProps)(PageWithGrid)
);
