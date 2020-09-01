// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";

import { permissions } from "services/models";
import { svgs } from "utilities";
import { Protected } from "components/shared";
import Shell from "components/shell/shell";

import Config from "app.config";
import {
    ManageDeviceGroupsContainer,
    SettingsContainer,
    HelpContainer,
    ProfileContainer,
    CreateDeviceQueryContainer,
} from "components/shell/flyouts";
import {
    DashboardContainer,
    DevicesContainer,
    UsersContainer,
    RulesContainer,
    MaintenanceContainer,
    PackagesContainer,
    DeploymentsRouter,
} from "./pages";
import { IdentityGatewayService } from "services";

class App extends Component {
    constructor(props) {
        super(props);

        this.state = { openFlyout: "" };
    }

    componentWillMount() {
        IdentityGatewayService.VerifyAndRefreshCache();
    }

    closeFlyout = () => this.setState({ openFlyout: "" });

    openSystemSettings = () => this.setState({ openFlyout: "settings" });
    openHelpFlyout = () => this.setState({ openFlyout: "help" });
    openUserProfile = () => this.setState({ openFlyout: "profile" });

    render() {
        const { deviceGroupFlyoutIsOpen, deviceQueryFlyoutIsOpen } = this.props,
            { openFlyout } = this.state,
            pagesConfig = [
                {
                    to: "/dashboard",
                    exact: true,
                    svg: svgs.tabs.dashboard,
                    labelId: "tabs.dashboard",
                    component: DashboardContainer,
                },
                {
                    to: "/devices",
                    exact: true,
                    svg: svgs.tabs.devices,
                    labelId: "tabs.devices",
                    component: DevicesContainer,
                },
                {
                    to: "/users",
                    exact: true,
                    svg: svgs.tabs.users,
                    labelId: "tabs.users",
                    component: UsersContainer,
                },
                {
                    to: "/rules",
                    exact: true,
                    svg: svgs.tabs.rules,
                    labelId: "tabs.rules",
                    component: RulesContainer,
                },
                {
                    to: "/packages",
                    exact: true,
                    svg: svgs.tabs.packages,
                    labelId: "tabs.packages",
                    component: PackagesContainer,
                },
                {
                    to: "/deployments",
                    exact: false,
                    svg: svgs.tabs.deployments,
                    labelId: "tabs.deployments",
                    component: DeploymentsRouter,
                },
                {
                    to: "/maintenance",
                    exact: false,
                    svg: svgs.tabs.maintenance,
                    labelId: "tabs.maintenance",
                    component: MaintenanceContainer,
                },
            ],
            crumbsConfig = [
                {
                    path: "/dashboard",
                    crumbs: [{ to: "/dashboard", labelId: "tabs.dashboard" }],
                },
                {
                    path: "/devices",
                    crumbs: [{ to: "/devices", labelId: "tabs.devices" }],
                },
                {
                    path: "/rules",
                    crumbs: [{ to: "/rules", labelId: "tabs.rules" }],
                },
                {
                    path: "/packages",
                    crumbs: [{ to: "/packages", labelId: "tabs.packages" }],
                },
                {
                    path: "/deployments",
                    crumbs: [
                        { to: "/deployments", labelId: "tabs.deployments" },
                    ],
                },
                {
                    path: "/deployments/:id/:isLatest",
                    crumbs: [
                        { to: "/deployments", labelId: "tabs.deployments" },
                        { to: "/deployments/:id", matchParam: "id" },
                        {
                            to: "/deployments/:id/:isLatest",
                            matchParam: "isLatest",
                        },
                    ],
                },
                {
                    path: "/maintenance",
                    crumbs: [
                        { to: "/maintenance", labelId: "tabs.maintenance" },
                    ],
                },
                {
                    path: "/maintenance/notifications",
                    crumbs: [
                        { to: "/maintenance", labelId: "tabs.maintenance" },
                        {
                            to: "/maintenance/notifications",
                            labelId: "maintenance.notifications",
                        },
                    ],
                },
                {
                    path: "/maintenance/rule/:id",
                    crumbs: [
                        { to: "/maintenance", labelId: "tabs.maintenance" },
                        {
                            to: "/maintenance/notifications",
                            labelId: "maintenance.notifications",
                        },
                        { to: "/maintenance/rule/:id", matchParam: "id" },
                    ],
                },
                {
                    path: "/maintenance/jobs",
                    crumbs: [
                        { to: "/maintenance", labelId: "tabs.maintenance" },
                        {
                            to: "/maintenance/jobs",
                            labelId: "maintenance.jobs",
                        },
                    ],
                },
                {
                    path: "/maintenance/job/:id",
                    crumbs: [
                        { to: "/maintenance", labelId: "tabs.maintenance" },
                        {
                            to: "/maintenance/jobs",
                            labelId: "maintenance.jobs",
                        },
                        { to: "/maintenance/rule/:id", matchParam: "id" },
                    ],
                },
            ],
            shellProps = {
                pagesConfig,
                crumbsConfig,
                openSystemSettings: this.openSystemSettings,
                openHelpFlyout: this.openHelpFlyout,
                openUserProfile: this.openUserProfile,
                openFlyout: this.state.openFlyout,
                ...this.props,
            };

        // Allow certain pages to have limited access (no nav bar, settings)
        let limitedAccess = Config.limitedAccessUrls.includes(
            window.location.pathname
        );
        console.log(limitedAccess);
        return (
            <Protected permission={permissions.readAll}>
                {(hasPermission) => (
                    <Shell
                        denyAccess={!hasPermission && !limitedAccess}
                        limitedAccess={limitedAccess}
                        {...shellProps}
                    >
                        {deviceGroupFlyoutIsOpen && (
                            <ManageDeviceGroupsContainer />
                        )}
                        {deviceQueryFlyoutIsOpen && (
                            <CreateDeviceQueryContainer />
                        )}
                        {openFlyout === "settings" && (
                            <SettingsContainer onClose={this.closeFlyout} />
                        )}
                        {openFlyout === "help" && (
                            <HelpContainer onClose={this.closeFlyout} />
                        )}
                        {openFlyout === "profile" && (
                            <ProfileContainer onClose={this.closeFlyout} />
                        )}
                    </Shell>
                )}
            </Protected>
        );
    }
}

export default App;
