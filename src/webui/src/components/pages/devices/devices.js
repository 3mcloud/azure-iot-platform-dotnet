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
    JsonEditorModal,
} from "components/shared";
import { DeviceNewContainer } from "./flyouts/deviceNew";
import { SIMManagementContainer } from "./flyouts/SIMManagement";
import { CreateDeviceQueryBtnContainer as CreateDeviceQueryBtn } from "components/shell/createDeviceQueryBtn";
import { svgs, getDeviceGroupParam } from "utilities";

import "./devices.scss";
import { IdentityGatewayService } from "services";

const closedFlyoutState = { openFlyoutName: undefined };

const closedModalState = {
    openModalName: undefined,
};

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
        IdentityGatewayService.VerifyAndRefreshCache();
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

    openModal = (modalName, jsonValue) => {
        this.setState({
            openModalName: modalName,
            modalJson: jsonValue,
        });
    };

    closeModal = () => this.setState(closedModalState);

    getOpenModal = () => {
        const { t, theme, logEvent } = this.props;
        if (this.state.openModalName === "json-editor") {
            return (
                <JsonEditorModal
                    t={t}
                    title={t(
                        "devices.flyouts.details.properties.editPropertyValue"
                    )}
                    onClose={this.closeModal}
                    jsonData={this.state.modalJson}
                    logEvent={logEvent}
                    theme={theme ? theme : "light"}
                />
            );
        }
        return null;
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
                routeProps,
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
                            isShowIconOnly={true}
                        />
                    </ContextMenuAlign>
                </ContextMenu>
                <PageContent className="devices-container">
                    <PageTitle titleValue={t("devices.title")} />
                    {!!error && <AjaxError t={t} error={error} />}
                    <SearchInput
                        onChange={this.searchOnChange}
                        onClick={this.onSearchClick}
                        aria-label={t("devices.ariaLabel")}
                        placeholder={t("devices.searchPlaceholder")}
                    />
                    {!error && (
                        <DevicesGridContainer
                            {...gridProps}
                            {...routeProps}
                            openPropertyEditorModal={this.openModal}
                        />
                    )}
                    {newDeviceFlyoutOpen && (
                        <DeviceNewContainer onClose={this.closeFlyout} />
                    )}
                    {simManagementFlyoutOpen && (
                        <SIMManagementContainer onClose={this.closeFlyout} />
                    )}
                </PageContent>
                {this.getOpenModal()}
            </ComponentArray>
        );
    }
}
