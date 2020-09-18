// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import { ContextPanel } from "@microsoft/azure-iot-ux-fluent-controls/lib/components/ContextPanel";

import "./flyout.scss";

export const Flyout = ({ header, children, footer, onClose, t, expanded }) => (
    <ContextPanel
        header={header}
        footer={footer}
        onClose={onClose}
        attr={{
            container: {
                className:
                    expanded === "yes"
                        ? "flyout-container-md"
                        : "flyout-container-sm",
            },
            closeButton: { button: { title: t("flyout.closeTitle") } },
        }}
    >
        {children}
    </ContextPanel>
);
