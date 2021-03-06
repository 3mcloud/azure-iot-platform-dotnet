// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";

import { getEnumTranslation } from "utilities";
import {
    PropertyGrid as Grid,
    PropertyGridHeader as GridHeader,
    PropertyRow as Row,
    PropertyCell as Cell,
} from "components/shared";
import { SeverityRenderer } from "components/shared/cellRenderers";
import Flyout from "components/shared/flyout";
import { ruleCalculations, getRuleTimePeriodLabel } from "services/models";
import { RuleSummaryContainer as RuleSummary } from "../ruleSummary";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./ruleViewer.module.scss"));

const Section = Flyout.Section;

export class RuleViewer extends Component {
    constructor(props) {
        super(props);

        this.state = {
            devicesAffected: 0,
        };
    }

    componentWillUnmount() {
        if (this.subscription) {
            this.subscription.unsubscribe();
        }
    }

    getDeviceGroupName(groupId) {
        const deviceGroup = this.props.deviceGroups.find(
            (group) => group.id === groupId
        );
        return (deviceGroup || {}).displayName || groupId;
    }

    render() {
        const { rule, t } = this.props,
            calculation = t(
                `rules.flyouts.ruleEditor.calculationOptions.${rule.calculation.toLowerCase()}`
            );

        return (
            <div className={css("view-rule-flyout-container")}>
                <Section.Container>
                    <Section.Content>
                        <div className={css("rule-name")}>{rule.name}</div>
                        <div className={css("rule-prop-value")}>
                            {rule.description}
                        </div>

                        <div className={css("rule-prop-label")}>
                            {t("rules.flyouts.ruleEditor.deviceGroup")}
                        </div>
                        <div className={css("rule-prop-value")}>
                            {this.getDeviceGroupName(rule.groupId)}
                        </div>

                        <div className={css("rule-prop-label")}>
                            {t("rules.flyouts.ruleEditor.calculation")}
                        </div>
                        <div className={css("rule-prop-value")}>
                            {calculation}
                        </div>

                        {calculation === ruleCalculations[0] && (
                            <div>
                                <div className={css("rule-prop-label")}>
                                    {t("rules.flyouts.ruleEditor.timePeriod")}
                                </div>
                                <div className={css("rule-prop-value")}>
                                    {getRuleTimePeriodLabel(rule.timePeriod)}
                                </div>
                            </div>
                        )}
                    </Section.Content>
                </Section.Container>

                <Section.Container collapsable={false}>
                    <Section.Header>
                        {t("rules.flyouts.ruleEditor.conditions")}
                    </Section.Header>
                    <Section.Content>
                        {rule.conditions.length > 0 && (
                            <Grid>
                                <GridHeader>
                                    <Row>
                                        <Cell className="col-4">
                                            {t(
                                                "rules.flyouts.ruleEditor.condition.field"
                                            )}
                                        </Cell>
                                        <Cell className="col-3">
                                            {t(
                                                "rules.flyouts.ruleEditor.condition.operator"
                                            )}
                                        </Cell>
                                        <Cell className="col-3">
                                            {t(
                                                "rules.flyouts.ruleEditor.condition.value"
                                            )}
                                        </Cell>
                                    </Row>
                                </GridHeader>
                                {rule.conditions.map((condition, idx) => (
                                    <Row key={idx}>
                                        <Cell className="col-4">
                                            {condition.field}
                                        </Cell>
                                        <Cell className="col-3">
                                            {getEnumTranslation(
                                                t,
                                                "rules.flyouts.ruleEditor.condition.operatorOptions",
                                                condition.operator
                                            )}
                                        </Cell>
                                        <Cell className="col-3">
                                            {condition.value}
                                        </Cell>
                                    </Row>
                                ))}
                            </Grid>
                        )}
                    </Section.Content>
                </Section.Container>

                {rule.actions && rule.actions.length > 0 && (
                    <Section.Container collapsable={false}>
                        <Section.Content>
                            <div className={css("rule-action-title")}>
                                {t("rules.flyouts.ruleEditor.actions.action")}
                            </div>
                            <div className={css("rule-prop-label")}>
                                {t(
                                    "rules.flyouts.ruleEditor.actions.emailAddresses"
                                )}
                            </div>
                            <div className={css("rule-prop-value")}>
                                {rule.actions[0].parameters.recipients.join(
                                    ", "
                                )}
                            </div>
                            <div className={css("rule-prop-label")}>
                                {t(
                                    "rules.flyouts.ruleEditor.actions.emailSubject"
                                )}
                            </div>
                            <div className={css("rule-prop-value")}>
                                {rule.actions[0].parameters.subject}
                            </div>
                            <div className={css("rule-prop-label")}>
                                {t(
                                    "rules.flyouts.ruleEditor.actions.emailComments"
                                )}
                            </div>
                            <div className={css("rule-prop-value")}>
                                {rule.actions[0].parameters.notes}
                            </div>
                        </Section.Content>
                    </Section.Container>
                )}

                <Section.Container>
                    <Section.Content>
                        <div className={css("rule-prop-label")}>
                            {t("rules.flyouts.ruleEditor.severityLevel")}
                        </div>
                        <div className={css("rule-prop-value")}>
                            <SeverityRenderer
                                value={rule.severity}
                                context={{ t }}
                            />
                        </div>

                        <div className={css("rule-prop-label")}>
                            {t("rules.flyouts.ruleEditor.ruleStatus")}
                        </div>
                        <div className={css("rule-prop-value")}>
                            {rule.enabled
                                ? t("rules.flyouts.ruleEditor.ruleEnabled")
                                : t("rules.flyouts.ruleEditor.ruleDisabled")}
                        </div>
                    </Section.Content>
                </Section.Container>
                <RuleSummary
                    rule={rule}
                    includeSummaryStatus={false}
                    includeRuleInfo={false}
                />
            </div>
        );
    }
}
