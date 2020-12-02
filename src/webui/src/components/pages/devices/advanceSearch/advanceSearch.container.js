// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withNamespaces } from "react-i18next";
import { AdvanceSearch } from "./advanceSearch";
import { epics as devicesEpics } from "store/reducers/devicesReducer";
import {
    redux as appRedux,
    epics as appEpics,
    getActiveDeviceQueryConditions,
} from "store/reducers/appReducer";

const mapStateToProps = (state) => ({
        activeDeviceQueryConditions: getActiveDeviceQueryConditions(state),
    }),
    mapDispatchToProps = (dispatch) => ({
        fetchDevices: () => dispatch(devicesEpics.actions.fetchDevices()),
        setActiveDeviceQueryConditions: (queryConditions) =>
            dispatch(
                appRedux.actions.setActiveDeviceQueryConditions(queryConditions)
            ),
        logEvent: (diagnosticsModel) =>
            dispatch(appEpics.actions.logEvent(diagnosticsModel)),
    });

export const AdvanceSearchContainer = withNamespaces()(
    connect(mapStateToProps, mapDispatchToProps)(AdvanceSearch)
);
