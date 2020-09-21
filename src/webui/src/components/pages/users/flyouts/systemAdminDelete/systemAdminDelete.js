// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { IdentityGatewayService } from "services";
import { LinkedComponent, svgs, Validator } from "utilities";
import {
    AjaxError,
    Btn,
    BtnToolbar,
    Flyout,
    FormGroup,
    FormControl,
    Indicator,
    SectionDesc,
    SectionHeader,
    SummaryBody,
    SummarySection,
    Svg,
} from "components/shared";

import "./../userDelete/userDelete.scss";

export class SystemAdminDelete extends LinkedComponent {
    constructor(props) {
        super(props);
        this.state = {
            users: [],
            confirmStatus: false,
            isPending: false,
            error: undefined,
            successCount: 0,
            changesApplied: false,
            formData: {
                userId: "",
                name: "",
            },
            expandedValue: "no",
        };

        // Linked components
        this.formDataLink = this.linkTo("formData");

        this.systemAdminLink = this.formDataLink
            .forkTo("userId")
            .map(({ value }) => value)
            .check(Validator.notEmpty, () =>
                this.props.t("users.flyouts.new.validation.required")
            );
        this.deleteUsers = this.deleteUsers.bind(this);
        this.expandFlyout = this.expandFlyout.bind(this);
    }

    componentDidMount() {
        this.props.getAllSystemAdmins();
    }

    componentWillUnmount() {
        if (this.subscription) {
            this.subscription.unsubscribe();
        }
    }

    formIsValid() {
        return [this.systemAdminLink].every((link) => !link.error);
    }

    onSystemAdminSelected = (e) => {
        console.log("Super user selected");
    };

    deleteUsers = (event) => {
        event.preventDefault();
        this.setState({ isPending: true, error: null });
        console.log(event);
        console.log(this.state.formData.userId);
        IdentityGatewayService.deleteSystemAdmin(
            this.state.formData.userId
        ).subscribe(
            function (deletedUserId) {
                this.props.deleteUsers([deletedUserId]);
                this.props.getAllSystemAdmins();
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
    };

    getSummaryMessage() {
        const { t } = this.props,
            { isPending, changesApplied } = this.state;

        if (isPending) {
            return t("users.flyouts.delete.pending");
        } else if (changesApplied) {
            return t("users.flyouts.delete.applySuccess");
        }
        return t("users.flyouts.delete.affected");
    }

    expandFlyout() {
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
        const { t, allSystemAdmins, onClose, loggedInUserId } = this.props,
            { error, changesApplied } = this.state,
            completedSuccessfully = changesApplied && !error,
            summaryMessage = this.getSummaryMessage(),
            systemAdminOptions = allSystemAdmins
                ? allSystemAdmins
                      .filter((x) => x.userId !== loggedInUserId)
                      .map((user) => ({
                          label: user.name,
                          value: user.userId,
                      }))
                : [];

        return (
            <Flyout
                header={t("users.flyouts.delete.systemAdmin.title")}
                t={t}
                onClose={onClose}
                expanded={this.state.expandedValue}
            >
                <div>
                    <Btn
                        className={
                            this.state.expandedValue === "no"
                                ? "svg-reverse-icon"
                                : "svg-icon"
                        }
                        svg={svgs.ChevronRightDouble}
                        onClick={this.expandFlyout}
                    ></Btn>
                </div>
                <form
                    className="device-delete-container"
                    onSubmit={this.deleteUsers}
                >
                    {!changesApplied && (
                        <FormGroup>
                            <FormControl
                                name="userId"
                                link={this.systemAdminLink}
                                ariaLabel={t(
                                    "users.flyouts.new.systemAdmin.label"
                                )}
                                type="select"
                                options={systemAdminOptions}
                                placeholder={t(
                                    "users.flyouts.new.systemAdmin.hint"
                                )}
                                onChange={(e) => this.onSystemAdminSelected(e)}
                            />
                        </FormGroup>
                    )}
                    {!!changesApplied && (
                        <>
                            <SummarySection>
                                <SectionHeader>
                                    {t("users.flyouts.delete.summaryHeader")}
                                </SectionHeader>
                                <SummaryBody>
                                    <SectionDesc>{summaryMessage}</SectionDesc>
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
                                <Btn svg={svgs.cancelX} onClick={onClose}>
                                    {t("users.flyouts.delete.close")}
                                </Btn>
                            </BtnToolbar>
                        </>
                    )}
                    {error && (
                        <AjaxError
                            className="device-delete-error"
                            t={t}
                            error={error}
                        />
                    )}
                    {!changesApplied && (
                        <BtnToolbar>
                            <Btn
                                svg={svgs.trash}
                                disabled={!this.formIsValid()}
                                primary={true}
                                type="submit"
                            >
                                {t("devices.flyouts.delete.apply")}
                            </Btn>
                            <Btn svg={svgs.cancelX} onClick={onClose}>
                                {t("users.flyouts.delete.cancel")}
                            </Btn>
                        </BtnToolbar>
                    )}
                </form>
            </Flyout>
        );
    }
}
