// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withTranslation } from "react-i18next";

import { epics } from "store/reducers/appReducer";
import { TimeSeriesInsightsLink } from "./timeSeriesInsightsLink";

const mapDispatchToProps = (dispatch) => ({
    logEvent: (diagnosticsModel) =>
        dispatch(epics.actions.logEvent(diagnosticsModel)),
});

export const TimeSeriesInsightsLinkContainer = withTranslation()(
    connect(null, mapDispatchToProps)(TimeSeriesInsightsLink)
);
