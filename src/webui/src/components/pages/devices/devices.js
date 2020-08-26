// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";

import { permissions, toDiagnosticsModel } from "services/models";
import { DevicesGridContainer } from "./devicesGrid";
import { DeviceGroupDropdownContainer as DeviceGroupDropdown } from "components/shell/deviceGroupDropdown";
import { ManageDeviceGroupsBtnContainer as ManageDeviceGroupsBtn } from "components/shell/manageDeviceGroupsBtn";
import { ResetActiveDeviceQueryBtnContainer as ResetActiveDeviceQueryBtn } from "components/shell/resetActiveDeviceQueryBtn";
import {
    AjaxError,
    Btn,
    ComponentArray,
    ContextMenu,
    ContextMenuAlign,
    PageContent,
    PageTitle,
    Protected,
    RefreshBarContainer as RefreshBar,
    SearchInput,
} from "components/shared";
import { DeviceNewContainer } from "./flyouts/deviceNew";
import { SIMManagementContainer } from "./flyouts/SIMManagement";
import { CreateDeviceQueryBtnContainer as CreateDeviceQueryBtn } from "components/shell/createDeviceQueryBtn";
import { svgs, getDeviceGroupParam } from "utilities";

import "./devices.scss";

const closedFlyoutState = { openFlyoutName: undefined };

export class Devices extends Component {
    constructor(props) {
        super(props);
        this.state = {
            ...closedFlyoutState,
            contextBtns: null,
            selectedDeviceGroupId: undefined,
        };

        this.props.updateCurrentWindow("Devices");
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
            // If the grid data refreshes, hide most flyouts and deselect soft selections
            switch (this.state.openFlyoutName) {
                case "create-device-query":
                    // leave this flyout open even on grid refresh
                    break;
                default:
                    this.setState(closedFlyoutState);
            }
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

    closeFlyout = () => this.setState(closedFlyoutState);

    openSIMManagement = () =>
        this.setState({ openFlyoutName: "sim-management" });
    openNewDeviceFlyout = () => {
        this.setState({ openFlyoutName: "new-device" });
        this.props.logEvent(toDiagnosticsModel("Devices_NewClick", {}));
    };
    openCloudToDeviceFlyout = () => {
        this.setState({ openFlyoutName: "c2d-message" });
        this.props.logEvent(toDiagnosticsModel("Devices_C2DClick", {}));
    };

    onContextMenuChange = (contextBtns) =>
        this.setState({
            contextBtns,
            openFlyoutName: undefined,
        });

    onGridReady = (gridReadyEvent) => (this.deviceGridApi = gridReadyEvent.api);

    searchOnChange = ({ target: { value } }) => {
        if (this.deviceGridApi) {
            this.deviceGridApi.setQuickFilter(value);
        }
    };

    onSearchClick = () => {
        this.props.logEvent(toDiagnosticsModel("Devices_Search", {}));
    };

    render() {
        const {
                t,
                devices,
                deviceGroupError,
                deviceError,
                isPending,
                lastUpdated,
                fetchDevices,
            } = this.props,
            gridProps = {
                onGridReady: this.onGridReady,
                rowData: isPending ? undefined : devices || [],
                onContextMenuChange: this.onContextMenuChange,
                t: this.props.t,
            },
            newDeviceFlyoutOpen = this.state.openFlyoutName === "new-device",
            simManagementFlyoutOpen =
                this.state.openFlyoutName === "sim-management",
            error = deviceGroupError || deviceError;

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
                            <ResetActiveDeviceQueryBtn />
                        ) : null}
                    </ContextMenuAlign>
                    <ContextMenuAlign>
                        <CreateDeviceQueryBtn />
                        <SearchInput
                            onChange={this.searchOnChange}
                            onClick={this.onSearchClick}
                            aria-label={t("devices.ariaLabel")}
                            placeholder={t("devices.searchPlaceholder")}
                        />
                        {this.state.contextBtns}
                        <Protected permission={permissions.updateSIMManagement}>
                            <Btn
                                svg={svgs.simmanagement}
                                onClick={this.openSIMManagement}
                            >
                                {t("devices.flyouts.SIMManagement.title")}
                            </Btn>
                        </Protected>
                        <Protected permission={permissions.createDevices}>
                            <Btn
                                svg={svgs.plus}
                                onClick={this.openNewDeviceFlyout}
                            >
                                {t("devices.flyouts.new.contextMenuName")}
                            </Btn>
                        </Protected>
                        <RefreshBar
                            refresh={fetchDevices}
                            time={lastUpdated}
                            isPending={isPending}
                            t={t}
                        />
                    </ContextMenuAlign>
                </ContextMenu>
                <PageContent className="devices-container">
                    <PageTitle titleValue={t("devices.title")} />
                    {!!error && <AjaxError t={t} error={error} />}
                    {!error && <DevicesGridContainer {...gridProps} />}
                    {newDeviceFlyoutOpen && (
                        <DeviceNewContainer onClose={this.closeFlyout} />
                    )}
                    {simManagementFlyoutOpen && (
                        <SIMManagementContainer onClose={this.closeFlyout} />
                    )}
                </PageContent>
            </ComponentArray>
        );
    }
}
