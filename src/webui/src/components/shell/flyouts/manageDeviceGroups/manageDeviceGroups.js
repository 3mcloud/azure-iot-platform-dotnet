// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { IoTHubManagerService } from "services";
import { permissions, toDiagnosticsModel } from "services/models";
import { Btn, Protected } from "components/shared";
import { svgs, LinkedComponent } from "utilities";
import Flyout from "components/shared/flyout";
import DeviceGroupForm from "./views/deviceGroupForm";
import DeviceGroups from "./views/deviceGroups";

import "./manageDeviceGroups.scss";

const toOption = (value, label) => ({
    label: label || value,
    value,
});

export class ManageDeviceGroups extends LinkedComponent {
    constructor(props) {
        super(props);

        this.state = {
            addNewDeviceGroup: false,
            selectedDeviceGroup: undefined,
            filterOptions: [],
            filtersError: undefined,
            expandedValue: "no",
        };
        this.handleDoubleClickItem = this.handleDoubleClickItem.bind(this);
    }

    componentDidMount() {
        this.subscription = IoTHubManagerService.getDeviceProperties().subscribe(
            (items) => {
                const filterOptions = items.map((item) => toOption(item));
                this.setState({ filterOptions });
            },
            (filtersError) => this.setState({ filtersError })
        );
    }

    componentWillUnmount() {
        if (this.subscription) this.subscription.unsubscribe();
    }

    toggleNewFilter = () => {
        if (!this.state.addNewDeviceGroup) {
            this.props.logEvent(toDiagnosticsModel("DeviceGroup_NewClick", {}));
        }
        this.setState({ addNewDeviceGroup: !this.state.addNewDeviceGroup });
    };

    closeForm = () =>
        this.setState({
            addNewDeviceGroup: false,
            selectedDeviceGroup: undefined,
        });

    onEditDeviceGroup = (selectedDeviceGroup) => () => {
        this.props.logEvent(toDiagnosticsModel("DeviceGroup_EditClick", {}));
        this.setState({ selectedDeviceGroup });
    };

    onCloseFlyout = () => {
        this.props.logEvent(
            toDiagnosticsModel("DeviceGroup_TopXCloseClick", {})
        );
        this.props.closeFlyout();
    };

    handleDoubleClickItem() {
        if (this.state.expandedValue === "no") {
            this.setState({
                expandedValue: "yes",
            });
        } else {
            this.setState({
                expandedValue: "no",
            });
        }
    }

    render() {
        const { t, deviceGroups = [] } = this.props;
        const btnStyle = {
            margin: "0px",
            paddingLeft: "10px",
        };
        return (
            <div onDoubleClick={this.handleDoubleClickItem}>
                <Flyout.Container
                    header={t("deviceGroupsFlyout.title")}
                    t={t}
                    onClose={this.onCloseFlyout}
                    expanded={this.state.expandedValue}
                >
                    <div className="manage-filters-flyout-container">
                        {this.state.addNewDeviceGroup ||
                        !!this.state.selectedDeviceGroup ? (
                            <DeviceGroupForm
                                {...this.props}
                                {...this.state}
                                cancel={this.closeForm}
                            />
                        ) : (
                            <div>
                                <Protected
                                    permission={permissions.createDeviceGroups}
                                >
                                    <Btn
                                        className="add-btn"
                                        style={btnStyle}
                                        svg={svgs.plus}
                                        onClick={this.toggleNewFilter}
                                    >
                                        {t("deviceGroupsFlyout.create")}
                                    </Btn>
                                </Protected>
                                {deviceGroups.length > 0 && (
                                    <DeviceGroups
                                        {...this.props}
                                        onEditDeviceGroup={
                                            this.onEditDeviceGroup
                                        }
                                    />
                                )}
                            </div>
                        )}
                    </div>
                </Flyout.Container>
            </div>
        );
    }
}
