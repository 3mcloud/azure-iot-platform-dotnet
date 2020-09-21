import React from "react";
import { IdentityGatewayService } from "services";
import { toDiagnosticsModel } from "services/models";
import { LinkedComponent, svgs, Validator } from "utilities";
import {
    AjaxError,
    Btn,
    BtnToolbar,
    Flyout,
    FormControl,
    FormGroup,
    FormLabel,
    Indicator,
    SectionDesc,
    SectionHeader,
    SummaryBody,
    SummarySection,
    Svg,
} from "components/shared";

import "./systemAdminNew.scss";

export class SystemAdminNew extends LinkedComponent {
    constructor(props) {
        super(props);

        this.state = {
            isPending: false,
            error: undefined,
            successCount: 0,
            changesApplied: false,
            selectedUserName: "",
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
        this.onSystemAdminSelected = this.onSystemAdminSelected.bind(this);
        this.addSystemAdmin = this.addSystemAdmin.bind(this);
        this.expandFlyout = this.expandFlyout.bind(this);
    }

    componentWillUnmount() {
        if (this.provisionSubscription) {
            this.provisionSubscription.unsubscribe();
        }
    }
    componentDidMount() {
        this.props.fetchAllNonSystemAdmins();
    }

    shouldComponentUpdate(nextProps, nextState) {
        // For setting rules. Like disable if x is true...

        // Update normally
        return true;
    }

    formIsValid() {
        return [this.systemAdminLink].every((link) => !link.error);
    }

    onSystemAdminSelected = (e) => {
        console.log("Super user selected");
    };

    onFlyoutClose = (eventName) => {
        this.props.logEvent(toDiagnosticsModel(eventName, this.state.formData));
        this.props.onClose();
    };

    addSystemAdmin = (event) => {
        event.preventDefault();
        const { formData } = this.state;

        if (this.formIsValid()) {
            this.setState({ isPending: true, error: null });

            const selectedUser = this.props.allNonSystemAdmins.filter(
                (x) => x.userId === this.state.formData.userId
            )[0].name;

            IdentityGatewayService.addSystemAdmin(
                this.state.formData.userId,
                selectedUser
            ).subscribe(
                function (user) {
                    this.setState({
                        successCount: this.state.successCount + 1,
                    });
                    this.props.fetchAllNonSystemAdmins();
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
                toDiagnosticsModel("Users_AddSystemAdminClick", formData)
            );
        }
    };

    getSummaryMessage() {
        const { t } = this.props,
            { isPending, changesApplied } = this.state;

        if (isPending) {
            return t("users.flyouts.new.pending");
        } else if (changesApplied) {
            return t("users.flyouts.new.systemAdmin.applySuccess");
        }
        return t("users.flyouts.new.affected");
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
        const { t, allNonSystemAdmins } = this.props,
            { error, changesApplied } = this.state,
            completedSuccessfully = changesApplied && !error,
            summaryMessage = this.getSummaryMessage();
        const systemAdminOptions = allNonSystemAdmins
            ? allNonSystemAdmins.map((user) => ({
                  label: user.name,
                  value: user.userId,
              }))
            : [];
        return (
            <Flyout
                header={t("users.flyouts.new.systemAdmin.title")}
                t={t}
                onClose={() => this.onFlyoutClose("Users_TopXCloseClick")}
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
                    className="users-new-container"
                    onSubmit={this.addSystemAdmin}
                >
                    {!changesApplied && (
                        <div className="users-new-content">
                            <FormGroup>
                                <FormLabel>
                                    {t("users.flyouts.new.systemAdmin.label")}
                                </FormLabel>

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
                                    onChange={(e) =>
                                        this.onSystemAdminSelected(e)
                                    }
                                />
                            </FormGroup>
                        </div>
                    )}

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
                                disabled={!this.formIsValid()}
                                type="submit"
                            >
                                {t("users.flyouts.new.systemAdmin.add")}
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
                                <Btn
                                    svg={svgs.cancelX}
                                    onClick={() =>
                                        this.onFlyoutClose("Users_CloseClick")
                                    }
                                >
                                    {t("users.flyouts.new.close")}
                                </Btn>
                            </BtnToolbar>
                        </>
                    )}
                </form>
            </Flyout>
        );
    }
}
