// Copyright (c) Microsoft. All rights reserved.
import React, { Component } from "react";
import {
    deploymentsColumnDefs,
    defaultDeploymentsGridProps,
} from "./deploymentsGridConfig";
import { Btn, PcsGrid, ComponentArray } from "components/shared";
import { isFunc, translateColumnDefs, svgs } from "utilities";
import { DeploymentDeleteContainer } from "../modals/deleteDeployment/deleteDeployment.Container";

const closedModalState = {
    openModalName: undefined,
};
export class DeploymentsGrid extends Component {
    constructor(props) {
        super(props);

        // Set the initial state
        this.state = {
            ...closedModalState,
            hardSelectedDeployments: [],
        };

        this.columnDefs = [
            deploymentsColumnDefs.checkBox,
            deploymentsColumnDefs.isActive,
            deploymentsColumnDefs.name,
            deploymentsColumnDefs.package,
            deploymentsColumnDefs.deviceGroup,
            deploymentsColumnDefs.priority,
            deploymentsColumnDefs.packageType,
            deploymentsColumnDefs.configType,
            deploymentsColumnDefs.targeted,
            deploymentsColumnDefs.applied,
            deploymentsColumnDefs.succeeded,
            deploymentsColumnDefs.failed,
            deploymentsColumnDefs.createdOn,
            deploymentsColumnDefs.createdBy,
            deploymentsColumnDefs.modifiedOn,
            deploymentsColumnDefs.modifiedBy,
        ];
    }

    getOpenModal = () => {
        if (
            this.state.openModalName === "delete-deployment" &&
            this.state.hardSelectedDeployments[0]
        ) {
            return (
                <DeploymentDeleteContainer
                    itemId={this.state.hardSelectedDeployments.map((p) => p.id)}
                    onClose={this.closeModal}
                    onDelete={this.closeModal}
                    title={this.props.t("deployments.modals.delete.title")}
                    deleteInfo={this.props.t(
                        "deployments.modals.delete.gridInfo"
                    )}
                />
            );
        }
        return null;
    };

    closeModal = () => this.setState(closedModalState);

    openModal = (modalName) => () =>
        this.setState({
            openModalName: modalName,
        });

    getSingleSelectionContextBtns = () => {
        return (
            <ComponentArray>
                <Btn
                    svg={svgs.trash}
                    onClick={this.openModal("delete-deployment")}
                >
                    {this.props.t("packages.delete")}
                </Btn>
            </ComponentArray>
        );
    };

    /**
     * Handles context filter changes and calls any hard select props method
     *
     * @param {Array} selectedDeployments A list of currently selected packages
     */
    onHardSelectChange = (selectedDeployments) => {
        const { onContextMenuChange, onHardSelectChange } = this.props;
        if (isFunc(onContextMenuChange)) {
            this.setState({
                hardSelectedDeployments: selectedDeployments,
            });
            onContextMenuChange(
                selectedDeployments.length >= 1
                    ? this.getSingleSelectionContextBtns()
                    : null
            );
        }
        if (isFunc(onHardSelectChange)) {
            onHardSelectChange(selectedDeployments);
        }
    };

    render() {
        const gridProps = {
            /* Grid Properties */
            ...defaultDeploymentsGridProps,
            columnDefs: translateColumnDefs(this.props.t, this.columnDefs),
            ...this.props, // Allow default property overrides
            getRowNodeId: ({ id }) => id,
            onHardSelectChange: this.onHardSelectChange,
            context: {
                t: this.props.t,
            },
        };

        return (
            <ComponentArray>
                <PcsGrid {...gridProps} />
                {this.getOpenModal()}
            </ComponentArray>
        );
    }
}
