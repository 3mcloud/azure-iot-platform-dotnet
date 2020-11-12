// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import Flyout from "components/shared/flyout";

import "./help.scss";

const Section = Flyout.Section;

const platformDocLinks = [
    {
        translationId: "header.publicDocumentation",
        url: "https://3mcloud.github.io/azure-iot-platform-dotnet/",
    },
    {
        translationId: "header.innerSourceDocumentation",
        url: "https://3miotdocs.blob.core.windows.net/innersource/index.html",
    },
    {
        translationId: "header.devopsDocumentation",
        url: "https://3miotdocs.blob.core.windows.net/devops/index.html",
    },
];

const microsoftDocLinks = [
    {
        translationId: "header.getStarted",
        url:
            "https://docs.microsoft.com/en-us/azure/iot-accelerators/iot-accelerators-remote-monitoring-monitor",
    },
    {
        translationId: "header.iotAcceleratorDocumentation",
        url: "https://docs.microsoft.com/en-us/azure/iot-accelerators/",
    },
];

export const Help = (props) => {
    const { t, onClose } = props;
    return (
        <Flyout.Container
            header={t("helpFlyout.platformTitle")}
            t={t}
            onClose={onClose}
        >
            <ul className="help-list">
                {platformDocLinks.map(({ url, translationId }) => (
                    <li key={translationId}>
                        <a target="_blank" rel="noopener noreferrer" href={url}>
                            {t(translationId)}
                        </a>
                    </li>
                ))}
            </ul>
            <Section.Container>
                <Section.Header>
                    {t("helpFlyout.microsoftTitle")}{" "}
                </Section.Header>
                <Section.Content className="simulation-description">
                    <ul className="help-list">
                        {microsoftDocLinks.map(({ url, translationId }) => (
                            <li key={translationId}>
                                <a
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    href={url}
                                >
                                    {t(translationId)}
                                </a>
                            </li>
                        ))}
                    </ul>
                </Section.Content>
            </Section.Container>
        </Flyout.Container>
    );
};
