// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import { permissions, toDiagnosticsModel } from "services/models";
import { Protected, ProtectedError } from "components/shared";
import { RuleEditorContainer } from "./ruleEditor";
import Flyout from "components/shared/flyout";

export class EditRuleFlyout extends Component {
    constructor(props) {
        super(props);

        this.state = {
            expandedValue: "no",
        };
        this.handleDoubleClickItem = this.handleDoubleClickItem.bind(this);
    }

    onTopXClose = () => {
        const { logEvent, onClose } = this.props;
        logEvent(toDiagnosticsModel("Rule_TopXCloseClick", {}));
        onClose();
    };

    handleDoubleClickItem() {
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
        const { onClose, t, ruleId } = this.props;
        return (
            <div onDoubleClick={this.handleDoubleClickItem}>
                <Flyout.Container
                    header={t("rules.flyouts.editRule")}
                    t={t}
                    onClose={this.onTopXClose}
                    expanded={this.state.expandedValue}
                >
                    <Protected permission={permissions.updateRules}>
                        {(hasPermission, permission) =>
                            hasPermission ? (
                                <RuleEditorContainer
                                    onClose={onClose}
                                    ruleId={ruleId}
                                />
                            ) : (
                                <div>
                                    <ProtectedError
                                        t={t}
                                        permission={permission}
                                    />
                                    <p>
                                        A read-only view will be added soon as
                                        part of another PBI.
                                    </p>
                                </div>
                            )
                        }
                    </Protected>
                </Flyout.Container>
            </div>
        );
    }
}
