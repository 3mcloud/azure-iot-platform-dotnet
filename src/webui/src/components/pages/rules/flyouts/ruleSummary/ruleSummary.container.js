// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withNamespaces } from "react-i18next";
import { RuleSummary } from "./ruleSummary";
import { getDeviceGroups } from "store/reducers/appReducer";

const mapStateToProps = (state, props) => ({
    deviceGroups: getDeviceGroups(state),
});

export const RuleSummaryContainer = withNamespaces()(
    connect(mapStateToProps, null)(RuleSummary)
);
