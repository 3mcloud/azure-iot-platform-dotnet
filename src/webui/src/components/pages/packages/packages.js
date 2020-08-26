// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import { permissions, toDiagnosticsModel } from "services/models";
import { PackagesGrid } from "./packagesGrid";
import {
    AjaxError,
    Btn,
    ComponentArray,
    ContextMenu,
    ContextMenuAlign,
    PageContent,
    Protected,
    RefreshBarContainer as RefreshBar,
    PageTitle,
} from "components/shared";
import { PackageNewContainer } from "./flyouts";
import { svgs, getDeviceGroupParam } from "utilities";

import "./packages.scss";
import { DeviceGroupDropdownContainer as DeviceGroupDropdown } from "../../shell/deviceGroupDropdown";
import { ManageDeviceGroupsBtnContainer as ManageDeviceGroupsBtn } from "../../shell/manageDeviceGroupsBtn";
import { PackageJSONContainer } from "./flyouts/packageJSON";

const closedFlyoutState = { openFlyoutName: undefined };

export class Packages extends Component {
    constructor(props) {
        super(props);
        this.state = {
            ...closedFlyoutState,
            contextBtns: null,
            packageJson: "testjson file",
            selectedDeviceGroupId: undefined,
        };

        this.props.updateCurrentWindow("Packages");

        this.props.fetchPackages();
    }

    componentWillMount() {
        if (this.props.location.search) {
            this.setState({
                selectedDeviceGroupId: getDeviceGroupParam(
                    this.props.location.search
                ),
            });
        }
    }

    componentWillReceiveProps(nextProps) {
        if (
            nextProps.isPending &&
            nextProps.isPending !== this.props.isPending
        ) {
            // If the grid data refreshes, hide the flyout
            this.setState(closedFlyoutState);
        }
    }

    componentDidMount() {
        if (this.state.selectedDeviceGroupId) {
            window.history.replaceState(
                {},
                document.title,
                this.props.location.pathname
            );
        }
    }

    closeFlyout = () => {
        this.props.logEvent(toDiagnosticsModel("Packages_NewClose", {}));
        this.setState(closedFlyoutState);
    };

    onContextMenuChange = (contextBtns) =>
        this.setState({
            contextBtns,
            openFlyoutName: undefined,
        });

    openNewPackageFlyout = () => {
        this.props.logEvent(toDiagnosticsModel("Packages_NewClick", {}));
        this.setState({
            openFlyoutName: "new-Package",
        });
    };

    getSoftSelectId = ({ id } = "") => id;

    onSoftSelectChange = (packageId, rowData) => {
        //Note: only the Id is reliable, rowData may be out of date
        this.props.logEvent(
            toDiagnosticsModel("Packages_GridRowClick", {
                id: packageId,
                displayName: rowData.name,
            })
        );
        this.setState({
            openFlyoutName: "package-json",
            packageJson: rowData.content,
        });
    };

    render() {
        const {
                t,
                packages,
                error,
                isPending,
                fetchPackages,
                lastUpdated,
            } = this.props,
            gridProps = {
                fetchPackages,
                rowData: isPending ? undefined : packages || [],
                onContextMenuChange: this.onContextMenuChange,
                t: this.props.t,
                getSoftSelectId: this.getSoftSelectId,
                onSoftSelectChange: this.onSoftSelectChange,
            };

        return (
            <ComponentArray>
                <ContextMenu>
                    <ContextMenuAlign left={true}>
                        <DeviceGroupDropdown
                            deviceGroupIdFromUrl={
                                this.state.selectedDeviceGroupId
                            }
                        />
                        <Protected permission={permissions.updateDeviceGroups}>
                            <ManageDeviceGroupsBtn />
                        </Protected>
                    </ContextMenuAlign>
                    <ContextMenuAlign>
                        {this.state.contextBtns}
                        <Protected permission={permissions.createPackages}>
                            <Btn
                                svg={svgs.plus}
                                onClick={this.openNewPackageFlyout}
                            >
                                {t("packages.new")}
                            </Btn>
                        </Protected>
                        <RefreshBar
                            refresh={fetchPackages}
                            time={lastUpdated}
                            isPending={isPending}
                            t={t}
                        />
                    </ContextMenuAlign>
                </ContextMenu>
                <PageContent className="package-container">
                    <PageTitle
                        className="package-title"
                        titleValue={t("packages.title")}
                    />
                    {!!error && <AjaxError t={t} error={error} />}
                    {!error && <PackagesGrid {...gridProps} />}
                    {this.state.openFlyoutName === "new-Package" && (
                        <PackageNewContainer t={t} onClose={this.closeFlyout} />
                    )}
                    {this.state.openFlyoutName === "package-json" && (
                        <PackageJSONContainer
                            packageJson={this.state.packageJson}
                            onClose={this.closeFlyout}
                        />
                    )}
                </PageContent>
            </ComponentArray>
        );
    }
}
