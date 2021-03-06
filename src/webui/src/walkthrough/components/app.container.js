// Copyright (c) Microsoft. All rights reserved.

import { withRouter } from "react-router-dom";
import { connect } from "react-redux";
import { withTranslation } from "react-i18next";

import {
    epics as appEpics,
    getTheme,
    getDeviceGroupFlyoutStatus,
} from "store/reducers/appReducer";
import App from "./app";

const mapStateToProps = (state) => ({
        theme: getTheme(state),
        deviceGroupFlyoutIsOpen: getDeviceGroupFlyoutStatus(state),
    }),
    // Wrap with the router and wrap the dispatch method
    mapDispatchToProps = (dispatch) => ({
        registerRouteEvent: (pathname) =>
            dispatch(appEpics.actions.detectRouteChange(pathname)),
    });

export const AppContainer = withRouter(
    withTranslation()(connect(mapStateToProps, mapDispatchToProps)(App))
);
