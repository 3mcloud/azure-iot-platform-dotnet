// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import { SelectInput } from "@microsoft/azure-iot-ux-fluent-controls/lib/components/Input/SelectInput";

import { toDiagnosticsModel } from "services/models";
import { svgs, compareByProperty } from "utilities";
import { ComponentArray, Btn } from "components/shared";

import "./deviceGroupDropdown.scss";
import { CopyModal } from "components/shared/copyModal/copyModal";

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
            "?deviceGroupId=" +
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

    render() {
        const { deviceGroups } = this.props,
            deviceGroupIds = deviceGroups.map(({ id }) => id);
        let activeDeviceGroupId = this.props.activeDeviceGroupId;
        if (deviceGroups && this.state.deviceGroupIdFromUrl) {
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
                {this.getOpenModal()}
            </ComponentArray>
        );
    }
}
