// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { Summary } from "./summary";
import {
    epics as appEpics,
    redux as appRedux,
    getActiveDeviceQueryConditions,
} from "store/reducers/appReducer";

const mapStateToProps = (state) => ({
        activeDeviceQueryConditions: getActiveDeviceQueryConditions(state),
    }),
    // Wrap the dispatch method
    mapDispatchToProps = (dispatch) => ({
        logEvent: (diagnosticsModel) =>
            dispatch(appEpics.actions.logEvent(diagnosticsModel)),
        checkTenantAndSwitch: (payload) =>
            dispatch(appRedux.actions.checkTenantAndSwitch(payload)),
    });

export const SummaryContainer = connect(
    mapStateToProps,
    mapDispatchToProps
)(Summary);
