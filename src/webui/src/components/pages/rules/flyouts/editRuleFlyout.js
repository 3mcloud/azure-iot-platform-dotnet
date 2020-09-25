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
            expandedValue: false,
        };
        this.expandFlyout = this.expandFlyout.bind(this);
    }

    onTopXClose = () => {
        const { logEvent, onClose } = this.props;
        logEvent(toDiagnosticsModel("Rule_TopXCloseClick", {}));
        onClose();
    };

    expandFlyout() {
        if (this.state.expandedValue) {
            this.setState({
                expandedValue: false,
            });
        } else {
            this.setState({
                expandedValue: true,
            });
        }
    }

    render() {
        const { onClose, t, ruleId } = this.props;
        return (
            <Flyout.Container
                header={t("rules.flyouts.editRule")}
                t={t}
                onClose={this.onTopXClose}
                expanded={this.state.expandedValue}
                onExpand={() => {
                    this.expandFlyout();
                }}
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
                                <ProtectedError t={t} permission={permission} />
                                <p>
                                    A read-only view will be added soon as part
                                    of another PBI.
                                </p>
                            </div>
                        )
                    }
                </Protected>
            </Flyout.Container>
        );
    }
}
