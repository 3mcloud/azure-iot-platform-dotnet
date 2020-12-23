// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withNamespaces } from "react-i18next";
import { AdvanceSearch } from "./advanceSearch";
import { epics as devicesEpics } from "store/reducers/devicesReducer";
import {
    redux as appRedux,
    epics as appEpics,
} from "store/reducers/appReducer";

const mapDispatchToProps = (dispatch) => ({
        fetchDevicesByCondition: (data) => dispatch(devicesEpics.actions.fetchDevicesByCondition(data)),
        setActiveDeviceQueryConditions: (queryConditions) =>
            dispatch(
                appRedux.actions.setActiveDeviceQueryConditions(queryConditions)
            ),
        logEvent: (diagnosticsModel) =>
            dispatch(appEpics.actions.logEvent(diagnosticsModel)),
    });

export const AdvanceSearchContainer = withNamespaces()(
    connect(null, mapDispatchToProps)(AdvanceSearch)
);
