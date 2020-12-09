// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withNamespaces } from "react-i18next";
import { CreateDeviceQuery } from "./createDeviceQuery";
import { epics as devicesEpics } from "store/reducers/devicesReducer";
import {
    redux as appRedux,
    epics as appEpics,
    getActiveDeviceQueryConditions,
    getActiveDeviceGroupConditions,
    getActiveDeviceGroup,
} from "store/reducers/appReducer";

const mapStateToProps = (state) => ({
        activeDeviceQueryConditions: getActiveDeviceQueryConditions(state),
        activeDeviceGroupConditions: getActiveDeviceGroupConditions(state),
        activeDeviceGroup: getActiveDeviceGroup(state),
    }),
    mapDispatchToProps = (dispatch) => ({
        fetchDevices: () => dispatch(devicesEpics.actions.fetchDevices()),
        fetchDeviceStatistics: () =>
            dispatch(devicesEpics.actions.fetchDeviceStatistics()),
        closeFlyout: () =>
            dispatch(appRedux.actions.setCreateDeviceQueryFlyoutStatus(false)),
        setActiveDeviceQueryConditions: (queryConditions) =>
            dispatch(
                appRedux.actions.setActiveDeviceQueryConditions(queryConditions)
            ),
        insertDeviceGroup: (deviceGroup) =>
            dispatch(appRedux.actions.insertDeviceGroups([deviceGroup])),
        logEvent: (diagnosticsModel) =>
            dispatch(appEpics.actions.logEvent(diagnosticsModel)),
    });

export const CreateDeviceQueryContainer = withNamespaces()(
    connect(mapStateToProps, mapDispatchToProps)(CreateDeviceQuery)
);
