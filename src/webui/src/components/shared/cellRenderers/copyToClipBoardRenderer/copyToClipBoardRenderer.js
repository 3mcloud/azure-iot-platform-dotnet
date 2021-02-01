// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import "../cellRenderer.scss";
import { Svg } from "components/shared/svg/svg";
import { svgs } from "utilities";

export const CopyToClipBoardRenderer = ({ value, context: { t } }) => (
    <div className="pcs-renderer-cell highlight">
        <Svg path={svgs.copy} className="pcs-renderer-icon" />
    </div>
);
