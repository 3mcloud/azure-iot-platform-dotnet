// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { IoTHubManagerService } from "services";
import { toDiagnosticsModel } from "services/models";
import { LinkedComponent } from "utilities";
import Flyout from "components/shared/flyout";
import CreateDeviceQueryForm from "./views/createDeviceQueryForm";

import "./createDeviceQuery.scss";

const toOption = (value, label) => ({
    label: label || value,
    value,
});

export class CreateDeviceQuery extends LinkedComponent {
    constructor(props) {
        super(props);

        this.state = {
            filterOptions: [],
            filtersError: undefined,
            expandedValue: false,
        };
        this.expandFlyout = this.expandFlyout.bind(this);
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
        if (this.subscription) {
            this.subscription.unsubscribe();
        }
    }

    toggleNewFilter = () => {
        this.setState({});
    };

    onCloseFlyout = () => {
        this.props.logEvent(
            toDiagnosticsModel("CreateDeviceQuery_TopXCloseClick", {})
        );
        // blank query to return to just the device group devices
        this.props.closeFlyout();
    };

    expandFlyout() {
        if (this.state.expandedValue) {
            this.setState({
                expandedValue: false,
            });
        } else {
            this.setState({
                expandedValue: true,
            });
        }
    }

    render() {
        const { t } = this.props;

        return (
            <Flyout.Container
                header={t("createDeviceQuery.title")}
                t={t}
                onClose={this.onCloseFlyout}
                expanded={this.state.expandedValue}
                onExpand={() => {
                    this.expandFlyout();
                }}
            >
                <div className="manage-filters-flyout-container">
                    <CreateDeviceQueryForm {...this.props} {...this.state} />
                </div>
            </Flyout.Container>
        );
    }
}
