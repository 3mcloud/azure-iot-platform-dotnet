// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { joinClasses } from "utilities";

import "./pageTitle.scss";

/** A presentational component containing the title for a page */
export const PageTitle = ({ titleValue, descriptionValue, className, hearderClassName, descriptionClassName}) => (
    <div className={joinClasses("page-title", className)}>
        <h1 className={joinClasses("page-title-header", hearderClassName)}>{titleValue}</h1>
        { descriptionValue && <h4 className={joinClasses(descriptionClassName)}>{descriptionValue}</h4> }
    </div>
);
