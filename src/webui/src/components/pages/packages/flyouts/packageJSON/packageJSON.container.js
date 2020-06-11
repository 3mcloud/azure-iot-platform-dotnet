// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withNamespaces } from "react-i18next";
import { PackageJSON } from "./packageJSON";
import { getTheme } from "store/reducers/appReducer";

// Pass the global info needed
const mapStateToProps = (state) => ({
    theme: getTheme(state),
});

export const PackageJSONContainer = withNamespaces()(
    connect(mapStateToProps, null)(PackageJSON)
);
