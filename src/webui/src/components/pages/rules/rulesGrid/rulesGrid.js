// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import { Trans } from "react-i18next";

import { permissions, toDiagnosticsModel } from "services/models";
import { Btn, ComponentArray, PcsGrid, Protected } from "components/shared";
import { rulesColumnDefs, defaultRulesGridProps } from "./rulesGridConfig";
import { checkboxColumn } from "components/shared/pcsGrid/pcsGridConfig";
import {
    isFunc,
    translateColumnDefs,
    svgs,
    getParamByName,
    getFlyoutNameParam,
    getFlyoutLink,
    userHasPermission,
} from "utilities";
import {
    EditRuleFlyout,
    RuleDetailsFlyout,
    RuleStatusContainer,
    DeleteRuleContainer,
} from "../flyouts";

import "./rulesGrid.scss";

const closedFlyoutState = {
    openFlyoutName: undefined,
    softSelectedRuleId: undefined,
};

export class RulesGrid extends Component {
    constructor(props) {
        super(props);

        // Set the initial state
        this.state = {
            ...closedFlyoutState,
            selectedRules: undefined,
        };

        this.columnDefs = [
            checkboxColumn,
            rulesColumnDefs.ruleName,
            rulesColumnDefs.description,
            rulesColumnDefs.severity,
            rulesColumnDefs.filter,
            rulesColumnDefs.trigger,
            rulesColumnDefs.notificationType,
            rulesColumnDefs.status,
            rulesColumnDefs.lastTrigger,
        ];

        this.contextBtns = {
            disable: (
                <Protected key="disable" permission={permissions.updateRules}>
                    <Btn
                        className="rule-status-btn"
                        svg={svgs.disableToggle}
                        onClick={this.openStatusFlyout}
                    >
                        <Trans i18nKey="rules.flyouts.disable">Disable</Trans>
                    </Btn>
                </Protected>
            ),
            enable: (
                <Protected key="enable" permission={permissions.updateRules}>
                    <Btn
                        className="rule-status-btn enabled"
                        svg={svgs.enableToggle}
                        onClick={this.openStatusFlyout}
                    >
                        <Trans i18nKey="rules.flyouts.enable">Enable</Trans>
                    </Btn>
                </Protected>
            ),
            changeStatus: (
                <Protected
                    key="changeStatus"
                    permission={permissions.updateRules}
                >
                    <Btn
                        className="rule-status-btn"
                        svg={svgs.changeStatus}
                        onClick={this.openStatusFlyout}
                    >
                        <Trans i18nKey="rules.flyouts.changeStatus">
                            Change status
                        </Trans>
                    </Btn>
                </Protected>
            ),
            edit: (
                <Protected key="edit" permission={permissions.updateRules}>
                    <Btn svg={svgs.edit} onClick={this.openEditRuleFlyout}>
                        {props.t("rules.flyouts.edit")}
                    </Btn>
                </Protected>
            ),
            delete: (
                <Protected key="delete" permission={permissions.deleteRules}>
                    <Btn svg={svgs.trash} onClick={this.openDeleteFlyout}>
                        <Trans i18nKey="rules.flyouts.delete">Delete</Trans>
                    </Btn>
                </Protected>
            ),
        };
    }

    componentWillReceiveProps({ rowData }) {
        const { selectedRules = [], softSelectedRuleId } = this.state;
        if (rowData && (selectedRules.length || softSelectedRuleId)) {
            let updatedSoftSelectedRule;
            const selectedIds = new Set(selectedRules.map(({ id }) => id)),
                updatedSelectedRules = rowData.reduce((acc, rule) => {
                    if (selectedIds.has(rule.id)) {
                        acc.push(rule);
                    }
                    if (softSelectedRuleId && rule.id === softSelectedRuleId) {
                        updatedSoftSelectedRule = rule;
                    }
                    return acc;
                }, []);
            this.setState({
                selectedRules: updatedSelectedRules,
                softSelectedRuleId: (updatedSoftSelectedRule || {}).id,
            });
        }
    }

    onFirstDataRendered = () => {
        if (this.props.rowData && this.props.rowData.length > 0) {
            this.getDefaultFlyout(this.props.rowData);
        }
    };

    getDefaultFlyout(rowData) {
        const { location, userPermissions } = this.props;
        const flyoutName = getFlyoutNameParam(location.search);
        var isUserHasPermission = true;
        if (
            flyoutName === "edit" &&
            !userHasPermission(permissions.updateRules, userPermissions)
        ) {
            isUserHasPermission = false;
        }
        const ruleId = getParamByName(location.search, "ruleId"),
            rule = rowData.find((rule) => rule.id === ruleId);
        if (
            location.search &&
            !this.state.softSelectedRuleId &&
            rule &&
            isUserHasPermission
        ) {
            this.setState({
                softSelectedRuleId: ruleId,
                openFlyoutName: flyoutName,
                selectedRules: [rule],
            });
            this.selectRows(ruleId);
        }
    }

    selectRows(ruleId) {
        this.props.rulesGridApi.gridOptionsWrapper.gridOptions.api.forEachNode(
            (node) => (node.id === ruleId ? node.setSelected(true) : null)
        );
    }

    openEditRuleFlyout = () => {
        this.props.logEvent(toDiagnosticsModel("Rule_EditClick", {}));
        this.setState({ openFlyoutName: "edit" });
    };

    openStatusFlyout = () => this.setState({ openFlyoutName: "status" });

    openDeleteFlyout = () => this.setState({ openFlyoutName: "delete" });

    setSelectedRules = (selectedRules) => this.setState({ selectedRules });

    getOpenFlyout = () => {
        var flyoutLink;
        switch (this.state.openFlyoutName) {
            case "view":
                flyoutLink = getFlyoutLink(
                    this.props.currentTenantId,
                    this.props.activeDeviceGroupId,
                    "ruleId",
                    this.state.softSelectedRuleId,
                    "view"
                );
                const editFlyoutLink = getFlyoutLink(
                    this.props.currentTenantId,
                    this.props.activeDeviceGroupId,
                    "ruleId",
                    this.state.softSelectedRuleId,
                    "edit"
                );
                return (
                    <RuleDetailsFlyout
                        onClose={this.closeFlyout}
                        t={this.props.t}
                        ruleId={
                            this.state.softSelectedRuleId ||
                            this.state.selectedRules[0].id
                        }
                        key="view-rule-flyout"
                        logEvent={this.props.logEvent}
                        flyoutLink={flyoutLink}
                        editFlyoutLink={editFlyoutLink}
                    />
                );
            case "edit":
                flyoutLink = getFlyoutLink(
                    this.props.currentTenantId,
                    this.props.activeDeviceGroupId,
                    "ruleId",
                    this.state.softSelectedRuleId ||
                        this.state.selectedRules[0].id,
                    "edit"
                );
                return (
                    <EditRuleFlyout
                        onClose={this.closeFlyout}
                        t={this.props.t}
                        ruleId={
                            this.state.softSelectedRuleId ||
                            this.state.selectedRules[0].id
                        }
                        key="edit-rule-flyout"
                        logEvent={this.props.logEvent}
                        flyoutLink={flyoutLink}
                    />
                );
            case "status":
                return (
                    <RuleStatusContainer
                        onClose={this.closeFlyout}
                        t={this.props.t}
                        rules={this.state.selectedRules}
                        key="edit-rule-flyout"
                    />
                );
            case "delete":
                return (
                    <DeleteRuleContainer
                        onClose={this.closeFlyout}
                        t={this.props.t}
                        rule={this.state.selectedRules[0]}
                        key="delete-rule-flyout"
                        refresh={this.props.refresh}
                    />
                );
            default:
                return null;
        }
    };

    /**
     * Handles context filter changes and calls any hard select props method
     *
     * @param {Array} selectedRules A list of currently selected rules
     */
    onHardSelectChange = (selectedRules) => {
        const { onContextMenuChange, onHardSelectChange } = this.props;
        if (isFunc(onContextMenuChange)) {
            if (selectedRules.length > 1) {
                this.setSelectedRules(selectedRules);
                onContextMenuChange(this.contextBtns.changeStatus);
            } else if (selectedRules.length === 1) {
                this.setSelectedRules(selectedRules);
                if (!selectedRules[0].deleted) {
                    onContextMenuChange([
                        this.contextBtns.delete,
                        selectedRules[0].enabled
                            ? this.contextBtns.disable
                            : this.contextBtns.enable,
                        this.contextBtns.edit,
                    ]);
                } else {
                    onContextMenuChange(null);
                }
            } else {
                onContextMenuChange(null);
            }
        }
        if (isFunc(onHardSelectChange)) {
            onHardSelectChange(selectedRules);
        }
    };

    onSoftSelectChange = (ruleId) => {
        const { onSoftSelectChange, suppressFlyouts } = this.props;
        if (!suppressFlyouts) {
            if (ruleId) {
                this.setState({
                    openFlyoutName: "view",
                    softSelectedRuleId: ruleId,
                });
            } else {
                this.closeFlyout();
            }
        }
        if (isFunc(onSoftSelectChange)) {
            onSoftSelectChange(ruleId);
        }
    };

    getSoftSelectId = ({ id } = "") => id;

    closeFlyout = () => {
        this.props.location.search = undefined;
        this.setState(closedFlyoutState);
    };

    render() {
        const gridProps = {
            /* Grid Properties */
            ...defaultRulesGridProps,
            onFirstDataRendered: this.onFirstDataRendered,
            columnDefs: translateColumnDefs(this.props.t, this.columnDefs),
            sizeColumnsToFit: true,
            getSoftSelectId: this.getSoftSelectId,
            softSelectId: this.state.softSelectedRuleId || {},
            deltaRowDataMode: true,
            ...this.props, // Allow default property overrides
            getRowNodeId: ({ id }) => id,
            enableSorting: true,
            unSortIcon: true,
            context: {
                t: this.props.t,
                deviceGroups: this.props.deviceGroups,
            },
            /* Grid Events */
            onRowClicked: ({ node }) => node.setSelected(!node.isSelected()),
            onHardSelectChange: this.onHardSelectChange,
            onSoftSelectChange: this.onSoftSelectChange,
            searchPlaceholder: this.props.t("rules.searchPlaceholder"),
            searchAreaLabel: this.props.t("rules.ariaLabel")
        };

        return (
            <ComponentArray>
                <PcsGrid {...gridProps} />
                {this.props.suppressFlyouts ? null : this.getOpenFlyout()}
            </ComponentArray>
        );
    }
}
