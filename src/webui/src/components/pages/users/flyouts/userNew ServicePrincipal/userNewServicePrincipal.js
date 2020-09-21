// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { IdentityGatewayService } from "./node_modules/services";
import {
    permissions,
    toDiagnosticsModel,
} from "./node_modules/services/models";
import { LinkedComponent, svgs, Validator } from "./node_modules/utilities";
import {
    AjaxError,
    Btn,
    BtnToolbar,
    Flyout,
    FormControl,
    FormGroup,
    FormLabel,
    Indicator,
    Protected,
    SectionDesc,
    SectionHeader,
    SummaryBody,
    SummarySection,
    Svg,
} from "./node_modules/components/shared";

import "./userNewServicePrincipal.scss";
import { Policies } from "./node_modules/utilities";

const isGuidRegex = /^(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1})$/,
    guid = (x) => x.match(isGuidRegex),
    userOptions = {
        labelName: "users.flyouts.new.servicePrincipal.label",
        user: {
            labelName: "users.flyouts.new.user.user",
            value: false,
        },
        edgeUser: {
            labelName: "users.flyouts.new.user.edgeUser",
            value: true,
        },
    },
    userRolesOptions = Policies.map((p) => {
        return {
            label: p.DisplayName,
            value: p.Role.toLowerCase(),
        };
    });

export class UserNewServicePrincipal extends LinkedComponent {
    constructor(props) {
        super(props);

        this.state = {
            isPending: false,
            error: undefined,
            successCount: 0,
            changesApplied: false,
            formData: {
                appid: "",
                role: "",
            },
            provisionedUser: {},
            expandedValue: false,
        };

        // Linked components
        this.formDataLink = this.linkTo("formData");

        this.appLink = this.formDataLink
            .forkTo("appid")
            .check(Validator.notEmpty, () =>
                this.props.t("users.flyouts.new.validation.required")
            )
            .check(guid, () =>
                this.props.t("users.flyouts.new.validation.invalidGuid")
            );

        this.roleLink = this.formDataLink
            .forkTo("role")
            .map(({ value }) => value)
            .check(Validator.notEmpty, () =>
                this.props.t("users.flyouts.new.validation.required")
            );
        this.expandFlyout = this.expandFlyout.bind(this);
    }

    componentWillUnmount() {
        if (this.provisionSubscription) {
            this.provisionSubscription.unsubscribe();
        }
    }

    shouldComponentUpdate(nextProps, nextState) {
        // For setting rules. Like disable if x is true...

        // Update normally
        return true;
    }

    formIsValid() {
        return [this.appLink, this.roleLink].every((link) => !link.error);
    }

    formControlChange = () => {
        if (this.state.changesApplied) {
            this.setState({
                successCount: 0,
                changesApplied: false,
                provisionedUser: {},
            });
        }
    };
    roleChange = (selectedObject) => {
        console.log(this.state.formData);
    };

    onFlyoutClose = (eventName) => {
        this.props.logEvent(toDiagnosticsModel(eventName, this.state.formData));
        this.props.onClose();
    };

    invite = (event) => {
        event.preventDefault();
        const { formData } = this.state;

        if (this.formIsValid()) {
            this.setState({ isPending: true, error: null });

            IdentityGatewayService.addSP(
                this.state.formData.appid,
                this.state.formData.role
            ).subscribe(
                function (user) {
                    this.setState({
                        successCount: this.state.successCount + 1,
                    });
                    this.props.insertUsers(user);
                }.bind(this),
                (error) =>
                    this.setState({
                        error,
                        isPending: false,
                        changesApplied: true,
                    }), // On Error
                () =>
                    this.setState({
                        isPending: false,
                        changesApplied: true,
                        confirmStatus: false,
                    }) // On Completed
            );

            this.props.logEvent(
                toDiagnosticsModel("Users_SPAddClick", formData)
            );
        }
    };

    getSummaryMessage() {
        const { t } = this.props,
            { isPending, changesApplied } = this.state;

        if (isPending) {
            return t("users.flyouts.new.pending");
        } else if (changesApplied) {
            return t("users.flyouts.new.servicePrincipal.applySuccess");
        }
        return t("users.flyouts.new.affected");
    }

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
        const { t } = this.props,
            { isPending, error, changesApplied } = this.state,
            completedSuccessfully = changesApplied && !error,
            summaryMessage = this.getSummaryMessage();
        console.log(permissions.inviteUsers);
        return (
            <Flyout
                header={t("users.flyouts.new.title")}
                t={t}
                onClose={() => this.onFlyoutClose("Users_TopXCloseClick")}
                expanded={this.state.expandedValue}
                onExpand={() => {
                    this.expandFlyout();
                }}
            >
                <Protected permission={permissions.inviteUsers}>
                    <form
                        className="users-new-container"
                        onSubmit={this.invite}
                    >
                        <div className="users-new-content">
                            <FormGroup>
                                <FormLabel>
                                    {t(userOptions.labelName)}
                                </FormLabel>
                                <FormControl
                                    link={this.appLink}
                                    type="text"
                                    onChange={this.formControlChange}
                                />
                            </FormGroup>

                            <FormGroup>
                                <FormLabel>
                                    {t("users.flyouts.new.roles.label")}
                                </FormLabel>
                                <FormControl
                                    name="roleSelect"
                                    link={this.roleLink}
                                    ariaLabel={t(
                                        "users.flyouts.new.roles.label"
                                    )}
                                    type="select"
                                    options={userRolesOptions}
                                    placeholder={t(
                                        "users.flyouts.new.roles.hint"
                                    )}
                                    onChange={this.roleChange}
                                />
                            </FormGroup>
                        </div>

                        {error && (
                            <AjaxError
                                className="users-new-error"
                                t={t}
                                error={error}
                            />
                        )}
                        {!changesApplied && (
                            <BtnToolbar>
                                <Btn
                                    primary={true}
                                    disabled={isPending || !this.formIsValid()}
                                    type="submit"
                                >
                                    {t(
                                        "users.flyouts.new.servicePrincipal.apply"
                                    )}
                                </Btn>
                                <Btn
                                    svg={svgs.cancelX}
                                    onClick={() =>
                                        this.onFlyoutClose("Users_CancelClick")
                                    }
                                >
                                    {t("users.flyouts.new.cancel")}
                                </Btn>
                            </BtnToolbar>
                        )}
                        {!!changesApplied && (
                            <>
                                <SummarySection>
                                    <SectionHeader>
                                        {t("users.flyouts.new.summaryHeader")}
                                    </SectionHeader>
                                    <SummaryBody>
                                        <SectionDesc>
                                            {summaryMessage}
                                        </SectionDesc>
                                        {this.state.isPending && <Indicator />}
                                        {completedSuccessfully && (
                                            <Svg
                                                className="summary-icon"
                                                path={svgs.apply}
                                            />
                                        )}
                                    </SummaryBody>
                                </SummarySection>
                                <BtnToolbar>
                                    <Btn
                                        svg={svgs.cancelX}
                                        onClick={() =>
                                            this.onFlyoutClose(
                                                "Users_CloseClick"
                                            )
                                        }
                                    >
                                        {t("users.flyouts.new.close")}
                                    </Btn>
                                </BtnToolbar>
                            </>
                        )}
                    </form>
                </Protected>
            </Flyout>
        );
    }
}
