// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { DeviceGroupSupportedMethods } from "./deviceGroupSupportedMethods";
import { epics as appEpics } from "store/reducers/appReducer";

// Wrap the dispatch method
const mapDispatchToProps = (dispatch) => ({
    logEvent: (diagnosticsModel) =>
        dispatch(appEpics.actions.logEvent(diagnosticsModel)),
});

export const DeviceGroupSupportedMethodsContainer = connect(
    null,
    mapDispatchToProps
)(DeviceGroupSupportedMethods);
