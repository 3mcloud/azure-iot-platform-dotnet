// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import { ContextPanel } from "@microsoft/azure-iot-ux-fluent-controls/lib/components/ContextPanel";

import { Btn } from "../forms";
import { svgs } from "utilities";
import { CopyModal } from "components/shared";

import "./flyout.scss";

const closedModalState = {
    openModalName: undefined,
    flyoutLink: undefined,
};

export class Flyout extends Component {
    constructor(props) {
        super(props);

        // Set the initial state
        this.state = closedModalState;
    }

    openModal = (modalName, flyoutLink) => () =>
        this.setState({
            openModalName: modalName,
            flyoutLink: flyoutLink,
        });
    closeModal = () => this.setState(closedModalState);

    getOpenModal = () => {
        const { t } = this.props;
        if (this.state.openModalName === "copy-link") {
            return (
                <CopyModal
                    t={t}
                    onClose={this.closeModal}
                    title={this.props.t(
                        "deviceGroupDropDown.modals.copyLink.title"
                    )}
                    copyLink={this.state.flyoutLink}
                />
            );
        }
        return null;
    };

    render() {
        const {
            header,
            children,
            footer,
            onClose,
            t,
            expanded,
            onExpand,
            flyoutLink,
        } = this.props;
        return (
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
                        children: [
                            onExpand && (
                                <Btn
                                    key={"expandedButton"}
                                    className={"svg-icon"}
                                    icon={
                                        expanded ? "backToWindow" : "fullScreen"
                                    }
                                    onClick={onExpand}
                                ></Btn>
                            ),
                            flyoutLink && (
                                <Btn
                                    svg={svgs.copyLink}
                                    key={"getLinkButton"}
                                    className={"svg-icon getlink-button"}
                                    onClick={this.openModal(
                                        "copy-link",
                                        flyoutLink
                                    )}
                                    title={t("flyout.getLinkTitle")}
                                ></Btn>
                            ),
                        ],
                    },
                }}
            >
                {this.getOpenModal()}
                {children}
            </ContextPanel>
        );
    }
}
