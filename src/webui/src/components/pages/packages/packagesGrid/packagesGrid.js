// Copyright (c) Microsoft. All rights reserved.
import React, { Component } from "react";
import { permissions } from "services/models";
import {
    packagesColumnDefs,
    defaultPackagesGridProps,
} from "./packagesGridConfig";
import { Btn, ComponentArray, PcsGrid, Protected } from "components/shared";
import { isFunc, translateColumnDefs, svgs } from "utilities";
import { PackageDeleteContainer } from "../modals";
import { ConfigService } from "services/configService";

import "./packagesGrid.scss";

const closedModalState = {
    openModalName: undefined,
};

export class PackagesGrid extends Component {
    constructor(props) {
        super(props);

        // Set the initial state
        this.state = {
            ...closedModalState,
            hardSelectedPackages: [],
            packageActivationPending: false,
        };

        this.columnDefs = [
            packagesColumnDefs.checkBox,
            packagesColumnDefs.name,
            packagesColumnDefs.packageType,
            packagesColumnDefs.configType,
            packagesColumnDefs.dateCreated,
            packagesColumnDefs.active,
            packagesColumnDefs.version,
            packagesColumnDefs.lastModified,
            packagesColumnDefs.lastModifiedBy,
            // We will not fully implement tagging at this time - just the active / inactive field
            // packagesColumnDefs.tags
        ];
    }

    getOpenModal = () => {
        if (
            this.state.openModalName === "delete-package" &&
            this.state.hardSelectedPackages[0]
        ) {
            return (
                <PackageDeleteContainer
                    itemId={this.state.hardSelectedPackages.map((p) => p.id)}
                    onClose={this.closeModal}
                    onDelete={this.closeModal}
                    title={this.props.t("packages.modals.delete.title")}
                    deleteInfo={this.props.t("packages.modals.delete.info")}
                />
            );
        }
        return null;
    };

    getSingleSelectionContextBtns = (showActivateButton, selectionLength) => {
        return (
            <ComponentArray>
                {selectionLength === 1 && (
                    <Protected permission={permissions.tagPackages}>
                        <Btn
                            svg={
                                showActivateButton
                                    ? svgs.checkmark
                                    : svgs.cancelX
                            }
                            onClick={
                                showActivateButton
                                    ? this.activatePackage
                                    : this.deactivatePackage
                            }
                            disable={this.state.packageActivationPending}
                        >
                            {showActivateButton
                                ? this.props.t("packages.activate")
                                : this.props.t("packages.deactivate")}
                        </Btn>
                    </Protected>
                )}
                <Protected permission={permissions.deletePackages}>
                    <Btn
                        svg={svgs.trash}
                        onClick={this.openModal("delete-package")}
                    >
                        {this.props.t("packages.delete")}
                    </Btn>
                </Protected>
            </ComponentArray>
        );
    };

    /**
     * Handles context filter changes and calls any hard select props method
     *
     * @param {Array} selectedPackages A list of currently selected packages
     */
    onHardSelectChange = (selectedPackages) => {
        const { onContextMenuChange, onHardSelectChange } = this.props;
        if (isFunc(onContextMenuChange)) {
            this.setState({
                hardSelectedPackages: selectedPackages,
            });
            onContextMenuChange(
                selectedPackages.length >= 1
                    ? this.getSingleSelectionContextBtns(
                          !selectedPackages[0].active,
                          selectedPackages.length
                      )
                    : null
            );
        }
        if (isFunc(onHardSelectChange)) {
            onHardSelectChange(selectedPackages);
        }
    };

    closeModal = () => this.setState(closedModalState);

    openModal = (modalName) => () =>
        this.setState({
            openModalName: modalName,
        });

    activatePackage = () => {
        this.updateSelectedPackagesActiveStatus(true);
    };

    deactivatePackage = () => {
        this.updateSelectedPackagesActiveStatus(false);
    };

    updateSelectedPackagesActiveStatus = (isActivate) => {
        this.setState({ packageActivationPending: true });
        const statusFunc = isActivate
            ? ConfigService.activatePackage
            : ConfigService.deactivatePackage;
        statusFunc(this.state.hardSelectedPackages[0].id).subscribe(
            () => {
                this.props.fetchPackages();
            },
            (error) => {
                // do nothing
            },
            () => {
                this.setState({ packageActivationPending: false });
            }
        );
    };

    render() {
        const gridProps = {
            /* Grid Properties */
            ...defaultPackagesGridProps,
            columnDefs: translateColumnDefs(this.props.t, this.columnDefs),
            sizeColumnsToFit: true,
            deltaRowDataMode: true,
            ...this.props, // Allow default property overrides
            getRowNodeId: ({ id }) => id,
            enableSorting: true,
            unSortIcon: true,
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
