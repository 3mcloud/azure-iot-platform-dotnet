// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import { Observable } from "rxjs";
import { Toggle } from "@microsoft/azure-iot-ux-fluent-controls/lib/components/Toggle";

import { IdentityGatewayService } from "services";
import { svgs } from "utilities";
import { permissions } from "services/models";
import {
    AjaxError,
    Btn,
    BtnToolbar,
    Flyout,
    Indicator,
    Protected,
    SectionDesc,
    SectionHeader,
    SummaryBody,
    SummaryCount,
    SummarySection,
    Svg,
} from "components/shared";

import "./userDelete.scss";

export class UserDelete extends Component {
    constructor(props) {
        super(props);
        this.state = {
            users: [],
            confirmStatus: false,
            isPending: false,
            error: undefined,
            successCount: 0,
            changesApplied: false,
            expandedValue: "no",
        };
        this.expandFlyout = this.expandFlyout.bind(this);
    }

    componentDidMount() {
        if (this.props.users) {
            this.populateUsersState(this.props.users);
        }
    }

    componentWillReceiveProps(nextProps) {
        if (
            nextProps.users &&
            (this.props.users || []).length !== nextProps.users.length
        ) {
            this.populateUsersState(nextProps.users);
        }
    }

    componentWillUnmount() {
        if (this.subscription) {
            this.subscription.unsubscribe();
        }
    }

    populateUsersState = (users = []) => {
        this.setState({ users });
    };

    toggleConfirm = (value) => {
        if (this.state.changesApplied) {
            this.setState({
                confirmStatus: value,
                changesApplied: false,
                successCount: 0,
            });
        } else {
            this.setState({ confirmStatus: value });
        }
    };

    deleteUsers = (event) => {
        event.preventDefault();
        this.setState({ isPending: true, error: null });

        this.subscription = Observable.from(this.state.users)
            .flatMap(
                ({ id }) => IdentityGatewayService.deleteUser(id).map(() => id) // On success return id
            )
            .subscribe(
                function (deletedUserId) {
                    this.setState({
                        successCount: this.state.successCount + 1,
                    });
                    this.props.deleteUsers([deletedUserId]);
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
        const { t, onClose } = this.props,
            {
                users,
                confirmStatus,
                isPending,
                error,
                successCount,
                changesApplied,
            } = this.state,
            summaryCount = changesApplied ? successCount : users.length,
            completedSuccessfully = changesApplied && !error,
            summaryMessage = this.getSummaryMessage();

        return (
            <Flyout
                header={t("users.flyouts.delete.title")}
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
                <Protected permission={permissions.deleteDevices}>
                    <form
                        className="device-delete-container"
                        onSubmit={this.deleteUsers}
                    >
                        <div className="device-delete-header">
                            {t("users.flyouts.delete.header")}
                        </div>
                        <div className="device-delete-descr">
                            {t("users.flyouts.delete.description")}
                        </div>
                        <Toggle
                            name="device-flyouts-delete"
                            attr={{
                                button: {
                                    "aria-label": t(
                                        "users.flyouts.delete.header"
                                    ),
                                },
                            }}
                            on={confirmStatus}
                            onChange={this.toggleConfirm}
                            onLabel={t("users.flyouts.delete.confirmYes")}
                            offLabel={t("users.flyouts.delete.confirmNo")}
                        />
                        <SummarySection>
                            <SectionHeader>
                                {t("devices.flyouts.delete.summaryHeader")}
                            </SectionHeader>
                            <SummaryBody>
                                <SummaryCount>{summaryCount}</SummaryCount>
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
                                    primary={true}
                                    disabled={
                                        isPending ||
                                        users.length === 0 ||
                                        !confirmStatus
                                    }
                                    type="submit"
                                >
                                    {t("devices.flyouts.delete.apply")}
                                </Btn>
                                <Btn svg={svgs.cancelX} onClick={onClose}>
                                    {t("users.flyouts.delete.cancel")}
                                </Btn>
                            </BtnToolbar>
                        )}
                        {!!changesApplied && (
                            <BtnToolbar>
                                <Btn svg={svgs.cancelX} onClick={onClose}>
                                    {t("users.flyouts.delete.close")}
                                </Btn>
                            </BtnToolbar>
                        )}
                    </form>
                </Protected>
            </Flyout>
        );
    }
}
