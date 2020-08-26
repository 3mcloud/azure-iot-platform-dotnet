// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import "../cellRenderer.scss";

export const IsActiveDeploymentRenderer = ({ value, context: { t } }) => (
    <div className="pcs-renderer-cell highlight">
        {value ? (
            <div className="small-green-circle"></div>
        ) : (
            <div className="small-black-circle"></div>
        )}
    </div>
);
