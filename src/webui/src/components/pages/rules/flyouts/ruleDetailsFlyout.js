// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import { svgs } from "utilities";
import { permissions, toDiagnosticsModel } from "services/models";
import {
    Btn,
    ComponentArray,
    Protected,
    ProtectedError,
} from "components/shared";
import { RuleEditorContainer } from "./ruleEditor";
import { RuleViewerContainer } from "./ruleViewer";
import Flyout from "components/shared/flyout";

import "./ruleDetailsFlyout.scss";

export class RuleDetailsFlyout extends Component {
    constructor(props) {
        super(props);

        // Set the initial state
        this.state = {
            isEditable: false,
            expandedValue: "no",
        };
        this.expandFlyout = this.expandFlyout.bind(this);
    }

    componentWillReceiveProps(nextProps) {
        if (this.props.ruleId !== nextProps.ruleId) {
            this.setState({ isEditable: false });
        }
    }

    onTopXClose = () => {
        const { logEvent, onClose } = this.props;
        if (this.state.isEditable) {
            logEvent(toDiagnosticsModel("Rule_TopXCloseClick", {}));
        }
        onClose();
    };

    goToEditMode = () => {
        this.props.logEvent(toDiagnosticsModel("Rule_EditClick", {}));
        this.setState({ isEditable: true });
    };

    expandFlyout() {
        if (this.state.expandedValue === "no") {
            this.setState({
                expandedValue: "yes",
            });
        } else {
            this.setState({
                expandedValue: "no",
            });
        }
    }

    render() {
        const { t, onClose, ruleId } = this.props,
            { isEditable } = this.state;

        return (
            <Flyout.Container
                header={
                    isEditable
                        ? t("rules.flyouts.editRule")
                        : t("rules.flyouts.viewRule")
                }
                t={t}
                onClose={this.onTopXClose}
                expanded={this.state.expandedValue}
            >
                <div>
                    <Btn
                        className={
                            this.state.expandedValue === "no"
                                ? "svg-reverse-icon"
                                : "svg-icon"
                        }
                        svg={svgs.ChevronRightDouble}
                        onClick={this.expandFlyout}
                    ></Btn>
                </div>
                <div className="rule-details">
                    {!isEditable ? (
                        <ComponentArray>
                            <RuleViewerContainer
                                onClose={onClose}
                                ruleId={ruleId}
                            />
                            <Protected permission={permissions.updateRules}>
                                <Btn
                                    className="edit-mode-btn"
                                    svg={svgs.edit}
                                    onClick={this.goToEditMode}
                                >
                                    {t("rules.flyouts.edit")}
                                </Btn>
                            </Protected>
                        </ComponentArray>
                    ) : (
                        <Protected
                            id="rule-details-edit"
                            permission={permissions.updateRules}
                        >
                            {(hasPermission, permission) =>
                                hasPermission ? (
                                    <RuleEditorContainer
                                        onClose={onClose}
                                        ruleId={ruleId}
                                    />
                                ) : (
                                    <ProtectedError
                                        t={t}
                                        permission={permission}
                                    />
                                )
                            }
                        </Protected>
                    )}
                </div>
            </Flyout.Container>
        );
    }
}
