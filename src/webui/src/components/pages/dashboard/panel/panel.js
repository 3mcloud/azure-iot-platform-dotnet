// Copyright (c) Microsoft. All rights reserved.

import React from "react";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./panel.module.scss"));

export const Panel = ({ className, children }) => (
    <div className={css("panel-container", className)}>{children}</div>
);
