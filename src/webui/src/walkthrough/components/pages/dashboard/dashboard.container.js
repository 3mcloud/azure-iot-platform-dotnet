// Copyright (c) Microsoft. All rights reserved.

import { connect } from "react-redux";
import { withTranslation } from "react-i18next";

import { Dashboard } from "./dashboard";

export const DashboardContainer = withTranslation()(
    connect(null, null)(Dashboard)
);
