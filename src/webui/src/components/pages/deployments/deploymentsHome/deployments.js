// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";

import { permissions, toDiagnosticsModel } from "services/models";
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
import { DeviceGroupDropdownContainer as DeviceGroupDropdown } from "components/shell/deviceGroupDropdown";
import { ManageDeviceGroupsBtnContainer as ManageDeviceGroupsBtn } from "components/shell/manageDeviceGroupsBtn";
import { ResetActiveDeviceQueryBtnContainer as ResetActiveDeviceQueryBtn } from "components/shell/resetActiveDeviceQueryBtn";
import { DeploymentsGrid } from "./deploymentsGrid";
import { DeploymentNewContainer, DeploymentStatusContainer } from "./flyouts";
import {
    svgs,
    getDeviceGroupParam,
    getParamByName,
    getFlyoutNameParam,
    getFlyoutLink,
} from "utilities";
import { CreateDeviceQueryBtnContainer as CreateDeviceQueryBtn } from "components/shell/createDeviceQueryBtn";
import {
    Balloon,
    BalloonPosition,
    BalloonAlignment,
} from "@microsoft/azure-iot-ux-fluent-controls/lib/components/Balloon/Balloon";

import "./deployments.scss";
import { IdentityGatewayService } from "services";

const closedFlyoutState = { openFlyoutName: undefined };

export class Deployments extends Component {
    constructor(props) {
        super(props);
        this.state = {
            ...closedFlyoutState,
            contextBtns: null,
            selectedDeviceGroupId: undefined,
        };

        this.props.updateCurrentWindow("Deployments");

        if (!this.props.lastUpdated && !this.props.error) {
            this.props.fetchDeployments();
        }
    }

    componentWillMount() {
        if (this.props.location.search) {
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

    componentDidMount() {
        if (this.state.selectedDeviceGroupId) {
            window.history.replaceState(
                {},
                document.title,
                this.props.location.pathname
            );
        }
    }

    onGridReady = (gridReadyEvent) => {
        this.deploymentsGridApi = gridReadyEvent.api;
    };

    onFirstDataRendered = () => {
        if (this.props.deployments.length > 0) {
            this.getDefaultFlyout(this.props.deployments);
        }
    };

    getDefaultFlyout(rowData) {
        const { location } = this.props;
        const deploymentId = getParamByName(location.search, "deploymentId"),
            deployment = rowData.find((dep) => dep.id === deploymentId);
        if (location.search && !this.state.deployment && deployment) {
            this.setState({
                deployment: deployment,
                relatedDeployments: rowData.filter(
                    (x) =>
                        x.deviceGroupId === deployment.deviceGroupId &&
                        x.id !== deployment.id
                ),
                openFlyoutName: getFlyoutNameParam(location.search),
                flyoutLink: window.location.href + location.search,
            });
            this.selectRows(deploymentId);
        }
    }

    selectRows(deploymentId) {
        this.deploymentsGridApi.gridOptionsWrapper.gridOptions.api.forEachNode(
            (node) =>
                node.data.id === deploymentId ? node.setSelected(true) : null
        );
    }

    closeFlyout = () => {
        this.props.location.search = undefined;
        this.setState(closedFlyoutState);
    };

    onContextMenuChange = (contextBtns) =>
        this.setState({
            contextBtns,
        });

    openNewDeploymentFlyout = () => {
        this.props.logEvent(toDiagnosticsModel("Deployments_NewClick", {}));
        this.setState({
            openFlyoutName: "newDeployment",
        });
    };

    getSoftSelectId = ({ id } = "") => id;

    onSoftSelectChange = (deploymentId, rowData) => {
        //Note: only the Id is reliable, rowData may be out of date
        this.props.logEvent(
            toDiagnosticsModel("Deployments_GridRowClick", {
                id: deploymentId,
                displayName: rowData.name,
            })
        );
        this.props.history.push(
            `/deployments/${deploymentId}/${rowData.isLatest}`
        );
    };

    onCellClicked = (selectedDeployment) => {
        if (selectedDeployment.colDef.field === "isActive") {
            const flyoutLink = getFlyoutLink(
                this.props.activeDeviceGroupId,
                "deploymentId",
                selectedDeployment.data.id,
                "deployment-status"
            );
            this.setState({
                openFlyoutName: "deployment-status",
                deployment: selectedDeployment.data,
                relatedDeployments: selectedDeployment.node.gridOptionsWrapper.gridOptions.rowData.filter(
                    (x) =>
                        x.deviceGroupId ===
                            selectedDeployment.data.deviceGroupId &&
                        x.id !== selectedDeployment.data.id
                ),
                flyoutLink: flyoutLink,
            });
        }
    };

    render() {
        const {
                t,
                deployments,
                error,
                isPending,
                fetchDeployments,
                lastUpdated,
                allActiveDeployments,
            } = this.props,
            gridProps = {
                onGridReady: this.onGridReady,
                onFirstDataRendered: this.onFirstDataRendered,
                rowData: isPending ? undefined : deployments || [],
                refresh: fetchDeployments,
                onContextMenuChange: this.onContextMenuChange,
                t: t,
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
                        {this.props.activeDeviceQueryConditions.length !== 0 ? (
                            <>
                                <CreateDeviceQueryBtn />
                                <ResetActiveDeviceQueryBtn />
                            </>
                        ) : null}
                    </ContextMenuAlign>
                    <ContextMenuAlign>
                        {this.state.contextBtns}
                        <Protected permission={permissions.createDeployments}>
                            <Btn
                                svg={svgs.plus}
                                onClick={this.openNewDeploymentFlyout}
                            >
                                {t("deployments.flyouts.new.contextMenuName")}
                            </Btn>
                        </Protected>
                        <RefreshBar
                            refresh={fetchDeployments}
                            time={lastUpdated}
                            isPending={isPending}
                            t={t}
                            isShowIconOnly={true}
                        />
                    </ContextMenuAlign>
                </ContextMenu>
                <PageContent className="deployments-page-container">
                    <PageTitle
                        className="deployments-title"
                        titleValue={t("deployments.title")}
                    />
                    <h1 className="right-corner">
                        <Balloon
                            position={BalloonPosition.Bottom}
                            align={BalloonAlignment.Center}
                            tooltip={
                                <div>
                                    Number of Active deployments associated with
                                    the current device group
                                </div>
                            }
                        >
                            {deployments.filter((x) => x.isActive).length}
                        </Balloon>
                        /
                        <Balloon
                            position={BalloonPosition.Bottom}
                            align={BalloonAlignment.Center}
                            tooltip={
                                <div>
                                    Number of Active deployments associated with
                                    all device group
                                </div>
                            }
                        >
                            {
                                allActiveDeployments.filter((x) => x.isActive)
                                    .length
                            }
                        </Balloon>
                        /
                        <Balloon
                            position={BalloonPosition.Bottom}
                            align={BalloonAlignment.Center}
                            tooltip={
                                <div>
                                    Total number of deployments available in IOT
                                    Hub
                                </div>
                            }
                        >
                            {100 -
                                allActiveDeployments.filter((x) => x.isActive)
                                    .length}
                        </Balloon>
                    </h1>
                    {!!error && <AjaxError t={t} error={error} />}
                    {!error && <DeploymentsGrid {...gridProps} />}
                    {this.state.openFlyoutName === "newDeployment" && (
                        <DeploymentNewContainer
                            t={t}
                            onClose={this.closeFlyout}
                        />
                    )}
                    {this.state.openFlyoutName === "deployment-status" && (
                        <DeploymentStatusContainer
                            selectedDeployment={this.state.deployment}
                            relatedDeployments={this.state.relatedDeployments}
                            t={t}
                            onClose={this.closeFlyout}
                            flyoutLink={this.state.flyoutLink}
                        />
                    )}
                </PageContent>
            </ComponentArray>
        );
    }
}
