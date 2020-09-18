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
        const { onClose, t } = this.props;
        return (
            <div onDoubleClick={this.handleDoubleClickItem}>
                <Flyout.Container
                    header={t("rules.flyouts.newRule")}
                    t={t}
                    onClose={this.onTopXClose}
                    expanded={this.state.expandedValue}
                >
                    <Protected permission={permissions.createRules}>
                        <RuleEditorContainer onClose={onClose} />
                    </Protected>
                </Flyout.Container>
            </div>
        );
    }
}
