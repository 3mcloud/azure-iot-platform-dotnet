// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import { permissions, toDiagnosticsModel } from "services/models";
import { Protected, Btn } from "components/shared";
import { RuleEditorContainer } from "./ruleEditor";
import Flyout from "components/shared/flyout";
import { svgs } from "utilities";

export class NewRuleFlyout extends Component {
    constructor(props) {
        super(props);

        this.state = {
            expandedValue: "no",
        };
        this.expandFlyout = this.expandFlyout.bind(this);
    }

    onTopXClose = () => {
        const { logEvent, onClose } = this.props;
        logEvent(toDiagnosticsModel("Rule_TopXCloseClick", {}));
        onClose();
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
        const { onClose, t } = this.props;
        return (
            <Flyout.Container
                header={t("rules.flyouts.newRule")}
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
                <Protected permission={permissions.createRules}>
                    <RuleEditorContainer onClose={onClose} />
                </Protected>
            </Flyout.Container>
        );
    }
}
