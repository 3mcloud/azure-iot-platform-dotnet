// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import { SelectInput } from "@microsoft/azure-iot-ux-fluent-controls/lib/components/Input/SelectInput";

import { toDiagnosticsModel } from "services/models";
import { svgs, compareByProperty } from "utilities";
import { ComponentArray, Btn, CopyModal } from "components/shared";

import "./deviceGroupDropdown.scss";

const closedModalState = {
    openModalName: undefined,
};

export class DeviceGroupDropdown extends Component {
    constructor(props) {
        super(props);
        this.state = {
            deviceGroupIdFromUrl: this.props.deviceGroupIdFromUrl,
        };
    }

    onChange = (deviceGroupIds) => (value) => {
        this.setState({ deviceGroupIdFromUrl: null });
        this.props.logEvent(toDiagnosticsModel("DeviceGroupFilter_Select", {}));
        // Don't try to update the device group if the device id doesn't exist
        if (deviceGroupIds.indexOf(value) > -1) {
            this.props.changeDeviceGroup(value);
            if (this.props.updateLoadMore) {
                this.props.updateLoadMore();
            }
        }
        this.props.logEvent(toDiagnosticsModel("DeviceFilter_Select", {}));
    };

    openModal = (modalName) => () =>
        this.setState({
            openModalName: modalName,
        });
    closeModal = () => this.setState(closedModalState);

    getOpenModal = () => {
        const { t } = this.props;
        const link =
            window.location.href +
            "?tenantId=" +
            this.props.currentTenantId +
            "&deviceGroupId=" +
            this.props.activeDeviceGroupId;
        if (this.state.openModalName === "copy-link") {
            return (
                <CopyModal
                    t={t}
                    onClose={this.closeModal}
                    title={this.props.t(
                        "deviceGroupDropDown.modals.copyLink.title"
                    )}
                    copyLink={link}
                />
            );
        }
        return null;
    };

    deviceGroupsToOptions = (deviceGroups) =>
        deviceGroups
            .sort(compareByProperty("sortOrder", true))
            .sort(compareByProperty("isPinned", false))
            .map(({ id, displayName }) => ({
                label: displayName,
                value: id,
            }));

    componentDidMount() {
        const { deviceGroups } = this.props,
            deviceGroupIds = deviceGroups.map(({ id }) => id);
        if (
            this.state.deviceGroupIdFromUrl &&
            deviceGroups &&
            deviceGroups.length > 0 &&
            deviceGroupIds.indexOf(this.state.deviceGroupIdFromUrl) <= -1
        ) {
            alert("Devicegroup doesn't exist");
        }
    }

    render() {
        const { deviceGroups, deviceStatistics } = this.props,
            deviceGroupIds = deviceGroups.map(({ id }) => id);
        let activeDeviceGroupId = this.props.activeDeviceGroupId;
        if (
            deviceGroups &&
            deviceGroups.length > 0 &&
            this.state.deviceGroupIdFromUrl
        ) {
            if (deviceGroupIds.indexOf(this.state.deviceGroupIdFromUrl) > -1) {
                this.props.changeDeviceGroup(this.state.deviceGroupIdFromUrl);
                activeDeviceGroupId = this.state.deviceGroupIdFromUrl;
            }
        }
        return (
            <ComponentArray>
                <SelectInput
                    name="device-group-dropdown"
                    className="device-group-dropdown"
                    attr={{
                        select: {
                            className: "device-group-dropdown-select",
                            "aria-label": this.props.t(
                                "deviceGroupDropDown.ariaLabel"
                            ),
                        },
                        chevron: {
                            className: "device-group-dropdown-chevron",
                        },
                    }}
                    options={this.deviceGroupsToOptions(deviceGroups)}
                    value={activeDeviceGroupId}
                    onChange={this.onChange(deviceGroupIds)}
                />
                <Btn svg={svgs.copyLink} onClick={this.openModal("copy-link")}>
                    Get Link
                </Btn>
                <div>
                    <label className="devices-loaded-label">
                        Devices Loaded
                    </label>
                    <p className="devices-loaded-value">
                        {" "}
                        {deviceStatistics
                            ? deviceStatistics.loadedDeviceCount
                            : undefined}
                        /{" "}
                        {deviceStatistics
                            ? deviceStatistics.totalDeviceCount
                            : undefined}
                    </p>
                </div>
                {this.getOpenModal()}
            </ComponentArray>
        );
    }
}
