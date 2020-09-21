// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import { permissions, toDiagnosticsModel } from "services/models";
import { Protected } from "components/shared";
import { RuleEditorContainer } from "./ruleEditor";
import Flyout from "components/shared/flyout";

export class NewRuleFlyout extends Component {
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
        const { onClose, t } = this.props;
        return (
            <Flyout.Container
                header={t("rules.flyouts.newRule")}
                t={t}
                onClose={this.onTopXClose}
                expanded={this.state.expandedValue}
                onExpand={() => {
                    this.expandFlyout();
                }}
            >
                <Protected permission={permissions.createRules}>
                    <RuleEditorContainer onClose={onClose} />
                </Protected>
            </Flyout.Container>
        );
    }
}
