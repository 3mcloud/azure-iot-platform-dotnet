// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import { Toggle } from "@microsoft/azure-iot-ux-fluent-controls/lib/components/Toggle";

import { permissions, toDiagnosticsModel } from "services/models";
import { DevicesGridContainer } from "./devicesGrid";
import { DeviceGroupDropdownContainer as DeviceGroupDropdown } from "components/shell/deviceGroupDropdown";
import { ManageDeviceGroupsBtnContainer as ManageDeviceGroupsBtn } from "components/shell/manageDeviceGroupsBtn";
import { ResetActiveDeviceQueryBtnContainer as ResetActiveDeviceQueryBtn } from "components/shell/resetActiveDeviceQueryBtn";
import {
    AjaxError,
    Btn,
    ComponentArray,
    ContextMenuAgile,
    PageContent,
    PageTitle,
    Protected,
    RefreshBarContainer as RefreshBar,
    SearchInput,
    JsonEditorModal,
} from "components/shared";
import { DeviceNewContainer } from "./flyouts/deviceNew";
import { AdvanceSearchContainer } from "./advanceSearch";
import { SIMManagementContainer } from "./flyouts/SIMManagement";
import { CreateDeviceQueryBtnContainer as CreateDeviceQueryBtn } from "components/shell/createDeviceQueryBtn";
import { svgs, getDeviceGroupParam, getTenantIdParam } from "utilities";

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
            loadMore: props.loadMoreState,
            isDeviceSearch: false,
        };

        this.props.updateCurrentWindow("Devices");
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

        if (this.props && this.props.location.pathname === "/deviceSearch") {
            this.props.resetDeviceByCondition();
            this.setState({
                isDeviceSearch: true,
            });
        } else {
            this.setState({
                isDeviceSearch: false,
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

    priorityChildren = () => {
        const { t } = this.props;
        const { isDeviceSearch } = this.state;

        let children = [];

        if (!isDeviceSearch) {
            children.push([
                <DeviceGroupDropdown
                    updateLoadMore={this.updateLoadMoreOnDeviceGroupChange}
                    deviceGroupIdFromUrl={this.state.selectedDeviceGroupId}
                />,
                <Protected permission={permissions.updateDeviceGroups}>
                    <ManageDeviceGroupsBtn />
                </Protected>,
                <CreateDeviceQueryBtn />,
            ]);
        }

        if (
            !isDeviceSearch &&
            this.props.activeDeviceQueryConditions.length !== 0
        ) {
            children.push(<ResetActiveDeviceQueryBtn />);
        }

        if (
            this.state.contextBtns &&
            this.state.contextBtns.props.children.length > 0
        ) {
            children = children.concat(this.state.contextBtns.props.children);
        }

        children.push(
            <Protected permission={permissions.updateSIMManagement}>
                <Btn svg={svgs.simmanagement} onClick={this.openSIMManagement}>
                    {t("devices.flyouts.SIMManagement.title")}
                </Btn>
            </Protected>
        );

        return children;
    };

    switchLoadMore = (value) => {
        if (!value) {
            this.setState({ loadMore: false });
            return this.props.cancelDeviceCalls({
                makeSubsequentCalls: false,
            });
        } else {
            this.setState({ loadMore: true });
            this.props.cancelDeviceCalls({
                makeSubsequentCalls: true,
            });
            return this.props.fetchDevicesByCToken();
        }
    };

    refreshDevices = () => {
        this.setState({ loadMore: false });
        this.props.cancelDeviceCalls({ makeSubsequentCalls: false });
        return this.props.fetchDevices();
    };

    updateLoadMoreOnDeviceGroupChange = () => {
        this.setState({ loadMore: false });
        this.props.cancelDeviceCalls({ makeSubsequentCalls: false });
    };

    render() {
        const {
                t,
                devices,
                deviceGroupError,
                deviceError,
                isPending,
                devicesByCondition,
                devicesByConditionError,
                isDevicesByConditionPanding,
                lastUpdated,
                routeProps,
            } = this.props,
            { isDeviceSearch } = this.state,
            deviceData = isDeviceSearch ? devicesByCondition : devices,
            dataError = isDeviceSearch ? devicesByConditionError : deviceError,
            isDataPending = isDeviceSearch
                ? isDevicesByConditionPanding
                : isPending,
            gridProps = {
                onGridReady: this.onGridReady,
                rowData: isDataPending ? undefined : deviceData || [],
                onContextMenuChange: this.onContextMenuChange,
                t: this.props.t,
            },
            newDeviceFlyoutOpen = this.state.openFlyoutName === "new-device",
            simManagementFlyoutOpen =
                this.state.openFlyoutName === "sim-management",
            error = deviceGroupError || dataError;

        return (
            <ComponentArray>
                <ContextMenuAgile
                    farChildren={[
                        <Protected permission={permissions.createDevices}>
                            {!this.state.isDeviceSearch && (
                                <Btn
                                    svg={svgs.plus}
                                    onClick={this.openNewDeviceFlyout}
                                >
                                    {t("devices.flyouts.new.contextMenuName")}
                                </Btn>
                            )}
                        </Protected>,
                        !this.state.isDeviceSearch && (
                            <RefreshBar
                                refresh={this.refreshDevices}
                                time={lastUpdated}
                                isPending={isPending}
                                t={t}
                                isShowIconOnly={true}
                            />
                        ),
                    ]}
                    priorityChildren={this.priorityChildren()}
                />
                <PageContent className="devices-container">
                    <PageTitle
                        titleValue={
                            !this.state.isDeviceSearch
                                ? t("devices.title")
                                : t("devices.deviceSearchTitle")
                        }
                        descriptionValue={
                            !this.state.isDeviceSearch
                                ? t("devices.titleDescription")
                                : t("devices.deviceSearchTitleDescription")
                        }
                    />
                    {!!error && <AjaxError t={t} error={error} />}
                    {this.state.isDeviceSearch && <AdvanceSearchContainer />}
                    <div className="search-left-div">
                        <SearchInput
                            onChange={this.searchOnChange}
                            onClick={this.onSearchClick}
                            aria-label={t("devices.ariaLabel")}
                            placeholder={t("devices.searchPlaceholder")}
                        />
                    </div>
                    <div className="cancel-right-div">
                        {!this.state.isDeviceSearch && (
                            <Toggle
                                attr={{
                                    button: {
                                        "aria-label": t("devices.loadMore"),
                                        type: "button",
                                    },
                                }}
                                on={this.state.loadMore}
                                onLabel={t("devices.loadMore")}
                                offLabel={t("devices.loadMore")}
                                onChange={this.switchLoadMore}
                            />
                        )}
                    </div>
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
