// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { DeviceJobMethods } from "./deviceJobMethods";
import {
    epics as appEpics,
    getTheme,
    getActiveDeviceGroup,
} from "store/reducers/appReducer";

const mapStateToProps = (state) => ({
        theme: getTheme(state),
        deviceGroup: getActiveDeviceGroup(state),
    }),
    // Wrap the dispatch method
    mapDispatchToProps = (dispatch) => ({
        logEvent: (diagnosticsModel) =>
            dispatch(appEpics.actions.logEvent(diagnosticsModel)),
    });

export const DeviceJobMethodsContainer = connect(
    mapStateToProps,
    mapDispatchToProps
)(DeviceJobMethods);
