// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import { ContextPanel } from "@microsoft/azure-iot-ux-fluent-controls/lib/components/ContextPanel";
import { Btn } from "components/shared";

import "./flyout.scss";

export const Flyout = ({
    header,
    children,
    footer,
    onClose,
    t,
    expanded,
    onExpand,
}) => (
    <ContextPanel
        header={header}
        footer={footer}
        onClose={onClose}
        attr={{
            container: {
                className: expanded
                    ? "flyout-container-md"
                    : "flyout-container-sm",
            },
            closeButton: { button: { title: t("flyout.closeTitle") } },
            header: {
                children: onExpand && (
                    <Btn
                        id={"expandedButton"}
                        className={"svg-icon"}
                        icon={expanded ? "backToWindow" : "fullScreen"}
                        onClick={onExpand}
                    ></Btn>
                ),
            },
        }}
    >
        {children}
    </ContextPanel>
);
