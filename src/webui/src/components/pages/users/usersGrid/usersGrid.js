// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import { permissions, toDiagnosticsModel } from "services/models";
import { Btn, ComponentArray, PcsGrid, Protected } from "components/shared";
import {
    userColumnDefs,
    defaultUserGridProps,
    defaultColDef,
} from "./usersGridConfig";
import { UserDeleteContainer } from "../flyouts/userDelete";
import { isFunc, svgs, translateColumnDefs } from "utilities";
import { checkboxColumn } from "components/shared/pcsGrid/pcsGridConfig";

const closedFlyoutState = {
    openFlyoutName: undefined,
    softSelectedUserId: undefined,
};

/**
 * A grid for displaying users
 *
 * Encapsulates the PcsGrid props
 */
export class UsersGrid extends Component {
    constructor(props) {
        super(props);

        // Set the initial state
        this.state = closedFlyoutState;

        // Default user grid columns
        this.columnDefs = [
            checkboxColumn,
            userColumnDefs.name,
            userColumnDefs.role,
            userColumnDefs.type,
            userColumnDefs.createdBy,
            userColumnDefs.dateCreated,
        ];

        this.contextBtns = (
            <ComponentArray>
                <Protected permission={permissions.deleteUsers}>
                    <Btn svg={svgs.trash} onClick={this.openFlyout("delete")}>
                        {props.t("users.flyouts.delete.title")}
                    </Btn>
                </Protected>
            </ComponentArray>
        );
    }

    /**
     * Get the grid api options
     *
     * @param {Object} gridReadyEvent An object containing access to the grid APIs
     */
    onGridReady = (gridReadyEvent) => {
        this.userGridApi = gridReadyEvent.api;
        // Call the onReady props if it exists
        if (isFunc(this.props.onGridReady)) {
            this.props.onGridReady(gridReadyEvent);
        }
    };

    openFlyout = (flyoutName) => () =>
        this.setState({
            openFlyoutName: flyoutName,
            softSelectedUserId: undefined,
        });

    getOpenFlyout = () => {
        switch (this.state.openFlyoutName) {
            case "delete":
                return (
                    <UserDeleteContainer
                        key="delete-user-key"
                        onClose={this.closeFlyout}
                        users={this.userGridApi.getSelectedRows()}
                    />
                );
            default:
                return null;
        }
    };

    closeFlyout = () => this.setState(closedFlyoutState);

    /**
     * Handles soft select props method
     *
     * @param userId The ID of the currently soft selected user
     */
    onSoftSelectChange = (userId) => {
        const { onSoftSelectChange } = this.props;
        if (userId) {
            this.setState({
                openFlyoutName: "details",
                softSelectedUserId: userId,
            });
        } else {
            this.closeFlyout();
        }
        if (isFunc(onSoftSelectChange)) {
            onSoftSelectChange(userId);
        }
    };

    /**
     * Handles context filter changes and calls any hard select props method
     *
     * @param {Array} selectedUsers A list of currently selected users
     */
    onHardSelectChange = (selectedUsers) => {
        const { onContextMenuChange, onHardSelectChange } = this.props;
        if (isFunc(onContextMenuChange)) {
            onContextMenuChange(
                selectedUsers.length > 0 ? this.contextBtns : null
            );
        }
        if (isFunc(onHardSelectChange)) {
            onHardSelectChange(selectedUsers);
        }
    };

    onColumnMoved = () => {
        this.props.logEvent(toDiagnosticsModel("Users_ColumnArranged", {}));
    };

    onSortChanged = () => {
        this.props.logEvent(toDiagnosticsModel("Users_Sort_Click", {}));
    };

    getSoftSelectId = ({ id } = "") => id;

    render() {
        const gridProps = {
            /* Grid Properties */
            ...defaultUserGridProps,
            columnDefs: translateColumnDefs(this.props.t, this.columnDefs),
            defaultColDef: defaultColDef,
            sizeColumnsToFit: true,
            getSoftSelectId: this.getSoftSelectId,
            softSelectId: this.state.softSelectedUserId || {},
            ...this.props, // Allow default property overrides
            immutableData: true,
            getRowNodeId: ({ id }) => id,
            context: {
                t: this.props.t,
            },
            /* Grid Events */
            onRowClicked: ({ node }) => node.setSelected(!node.isSelected()),
            onGridReady: this.onGridReady,
            onSoftSelectChange: this.onSoftSelectChange,
            onHardSelectChange: this.onHardSelectChange,
            onColumnMoved: this.onColumnMoved,
            onSortChanged: this.onSortChanged,
        };

        return [
            <PcsGrid key="user-grid-key" {...gridProps} />,
            this.getOpenFlyout(),
        ];
    }
}
