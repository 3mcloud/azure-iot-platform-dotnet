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
import {
    svgs,
    getDeviceGroupParam,
    getParamByName,
    getFlyoutNameParam,
    getFlyoutLink,
    getTenantIdParam,
    copyToClipboard,
} from "utilities";

import "./packages.scss";
import { DeviceGroupDropdownContainer as DeviceGroupDropdown } from "../../shell/deviceGroupDropdown";
import { ManageDeviceGroupsBtnContainer as ManageDeviceGroupsBtn } from "../../shell/manageDeviceGroupsBtn";
import { PackageJSONContainer } from "./flyouts/packageJSON";
import { IdentityGatewayService } from "services";

const closedFlyoutState = { openFlyoutName: undefined };

export class Packages extends Component {
    constructor(props) {
        super(props);
        this.state = {
            ...closedFlyoutState,
            contextBtns: null,
            packageJson: "testjson file",
            packageId: null,
            selectedDeviceGroupId: undefined,
        };
    }

    componentWillMount() {
        if (this.props.location.search) {
            const tenantId = getTenantIdParam(this.props.location.search);
            this.props.checkTenantAndSwitch({
                tenantId: tenantId,
                redirectUrl: window.location.href,
            });
            this.setState({
                selectedDeviceGroupId: getDeviceGroupParam(
                    this.props.location.search
                ),
            });
        }
        IdentityGatewayService.VerifyAndRefreshCache();
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

    onFirstDataRendered = () => {
        if (this.props.packages.length > 0) {
            this.getDefaultFlyout(this.props.packages);
        }
    };

    onGridReady = (gridReadyEvent) => {
        this.packagesGridApi = gridReadyEvent.api;
    };

    getDefaultFlyout(rowData) {
        const { location } = this.props;
        const selectedPackageId = getParamByName(location.search, "packageId"),
            selectedPackage = rowData.find((p) => p.id === selectedPackageId);
        if (location.search && selectedPackage) {
            this.setState({
                packageJson: selectedPackage.content,
                openFlyoutName: getFlyoutNameParam(location.search),
                flyoutLink: window.location.href + location.search,
            });
            this.selectRows(selectedPackageId);
        }
    }

    selectRows(selectedPackageId) {
        this.packagesGridApi.gridOptionsWrapper.gridOptions.api.forEachNode(
            (node) =>
                node.data.id === selectedPackageId
                    ? node.setSelected(true)
                    : null
        );
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
        this.props.location.search = undefined;
        this.props.logEvent(toDiagnosticsModel("Packages_NewClose", {}));
        this.setState(closedFlyoutState);
    };

    onContextMenuChange = (contextBtns) =>
        this.setState({
            contextBtns,
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
        const flyoutLink = getFlyoutLink(
            this.props.currentTenantId,
            this.props.deviceGroupId,
            "packageId",
            rowData.id,
            "package-json"
        );
        this.setState({
            openFlyoutName: "package-json",
            packageJson: rowData.content,
            packageId: rowData.id,
            flyoutLink: flyoutLink,
        });
    };

    onCellClicked = (selectedPackage) => {
        if (selectedPackage.colDef.field === "id") {
            console.log(selectedPackage);
            copyToClipboard(selectedPackage.data.id);
        }
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
                onGridReady: this.onGridReady,
                onFirstDataRendered: this.onFirstDataRendered,
                fetchPackages,
                rowData: isPending ? undefined : packages || [],
                onContextMenuChange: this.onContextMenuChange,
                t: this.props.t,
                getSoftSelectId: this.getSoftSelectId,
                onSoftSelectChange: this.onSoftSelectChange,
                onCellClicked: this.onCellClicked,
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
                            isShowIconOnly={true}
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
                            packageId={this.state.packageId}
                            onClose={this.closeFlyout}
                            flyoutLink={this.state.flyoutLink}
                        />
                    )}
                </PageContent>
            </ComponentArray>
        );
    }
}
