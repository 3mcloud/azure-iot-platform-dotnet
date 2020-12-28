// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import { Trans } from "react-i18next";
import Config from "app.config";
import { Svg } from "components/shared/svg/svg";
import { svgs, getEnumTranslation } from "utilities";
import {
    Btn,
    BtnToolbar,
    ErrorMsg,
    Hyperlink,
    PropertyGrid as Grid,
    PropertyRow as Row,
    PropertyCell as Cell,
} from "components/shared";
import Flyout from "components/shared/flyout";
import { Policies } from "utilities";

import "./profile.scss";
import TenantGrid from "./tenantGrid";

const Section = Flyout.Section,
    jwt_decode = require("jwt-decode");
export const Profile = (props) => {
    const {
            t,
            user,
            logout,
            switchTenant,
            createTenant,
            tenants,
            fetchTenants,
            onClose,
            deleteTenantThenSwitch,
            updateTenant,
            currentTenant,
            isSystemAdmin,
        } = props,
        roleArray = Array.from(user.roles).map(
            (r) =>
                Policies.filter((p) => p.Role.toLowerCase() === r).concat({
                    DisplayName: "No Roles",
                })[0].DisplayName
        ),
        permissionArray = Array.from(user.permissions);

    return (
        <Flyout.Container
            header={t("profileFlyout.title")}
            t={t}
            onClose={onClose}
        >
            <div className="profile-container">
                {!user && (
                    <div className="profile-container">
                        <ErrorMsg className="profile-error">
                            {t("profileFlyout.noUser")}
                        </ErrorMsg>
                        <Trans i18nKey={"profileFlyout.description"}>
                            <Hyperlink
                                href={
                                    Config.contextHelpUrls.rolesAndPermissions
                                }
                                target="_blank"
                            >
                                {t("profileFlyout.learnMore")}
                            </Hyperlink>
                            about roles and permisions
                        </Trans>
                    </div>
                )}
                {user && (
                    <div className="profile-container">
                        <div className="profile-header">
                            <h2>{user.email}</h2>
                            <Grid className="profile-header-grid">
                                <Row>
                                    <Cell className="col-7">
                                        <Trans
                                            i18nKey={
                                                "profileFlyout.description"
                                            }
                                        >
                                            <Hyperlink
                                                href={
                                                    Config.contextHelpUrls
                                                        .rolesAndPermissions
                                                }
                                                target="_blank"
                                            >
                                                {t("profileFlyout.learnMore")}
                                            </Hyperlink>
                                            about roles and permisions
                                        </Trans>
                                    </Cell>
                                    <Cell className="col-3">
                                        <Btn primary={true} onClick={logout}>
                                            {t("profileFlyout.logout")}
                                        </Btn>
                                    </Cell>
                                </Row>
                            </Grid>
                        </div>

                        <Section.Container>
                            <Section.Header>
                                {t("profileFlyout.tenants.tenantHeader")}
                            </Section.Header>
                            <Section.Content>
                                <div className="pcs-renderer-cell">
                                    <Svg
                                        path={svgs.star}
                                        className="pcs-renderer-icon"
                                    />
                                    <div className="current-tenant-text">
                                        {currentTenant && currentTenant !== ""
                                            ? "Current: " + currentTenant
                                            : ""}
                                    </div>
                                </div>
                                {/* Create the list of available tenants if there are any */
                                !tenants || tenants.length === 0 ? (
                                    t("profileFlyout.tenants.noTenant")
                                ) : (
                                    <Grid>
                                        <Row>
                                            <Cell>
                                                {t(
                                                    "profileFlyout.tenants.tenantNameColumn"
                                                )}
                                            </Cell>
                                            <Cell>
                                                {t(
                                                    "profileFlyout.tenants.tenantRoleColumn"
                                                )}
                                            </Cell>
                                            <Cell>
                                                {t(
                                                    "profileFlyout.tenants.tenantActionColumn"
                                                )}
                                            </Cell>
                                        </Row>
                                        <TenantGrid
                                            updateTenant={updateTenant}
                                            fetchTenants={fetchTenants}
                                            currentTenant={currentTenant}
                                            switchTenant={switchTenant}
                                            deleteTenantThenSwitch={
                                                deleteTenantThenSwitch
                                            }
                                            tenants={tenants}
                                            t={t}
                                            isSystemAdmin={isSystemAdmin}
                                        ></TenantGrid>
                                    </Grid>
                                )}
                                {isSystemAdmin && (
                                    <Grid>
                                        <Cell id="create-tenant-cell">
                                            <Btn
                                                className="create-tenant-button"
                                                primary={true}
                                                onClick={() =>
                                                    createTenant().subscribe(
                                                        (r) => fetchTenants()
                                                    )
                                                }
                                            >
                                                {t(
                                                    "profileFlyout.tenants.createTenant"
                                                )}
                                            </Btn>
                                        </Cell>
                                    </Grid>
                                )}
                            </Section.Content>
                        </Section.Container>

                        <Section.Container>
                            <Section.Header>
                                {t("profileFlyout.roles")}
                            </Section.Header>
                            <Section.Content>
                                {roleArray.length === 0 ? (
                                    t("profileFlyout.noRoles")
                                ) : (
                                    <Grid>
                                        {roleArray.map((roleName, idx) => (
                                            <Row key={idx}>
                                                <Cell>{roleName}</Cell>
                                            </Row>
                                        ))}
                                    </Grid>
                                )}
                            </Section.Content>
                        </Section.Container>

                        <Section.Container>
                            <Section.Header>
                                {t("profileFlyout.permissions")}
                            </Section.Header>
                            <Section.Content>
                                {permissionArray.length === 0 ? (
                                    t("profileFlyout.noPermissions")
                                ) : (
                                    <Grid>
                                        {permissionArray.map(
                                            (permissionName, idx) => (
                                                <Row key={idx}>
                                                    <Cell>
                                                        {getEnumTranslation(
                                                            t,
                                                            "permissions",
                                                            permissionName
                                                        )}
                                                    </Cell>
                                                </Row>
                                            )
                                        )}
                                    </Grid>
                                )}
                            </Section.Content>
                        </Section.Container>
                        {global.DeploymentConfig.developmentMode ? (
                            <Section.Container>
                                <Section.Header>
                                    Development Variables
                                </Section.Header>
                                <Section.Content>
                                    <Grid>
                                        id_token: <br />
                                        {user.token}
                                    </Grid>
                                    <Grid>
                                        payload: <br />
                                        {JSON.stringify(
                                            jwt_decode(user.token),
                                            null,
                                            2
                                        )}
                                    </Grid>
                                </Section.Content>
                            </Section.Container>
                        ) : (
                            ""
                        )}
                        <BtnToolbar>
                            <Btn svg={svgs.cancelX} onClick={onClose}>
                                {t("profileFlyout.close")}
                            </Btn>
                        </BtnToolbar>
                    </div>
                )}
            </div>
        </Flyout.Container>
    );
};
