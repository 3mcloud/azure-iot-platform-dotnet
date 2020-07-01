// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import { Link } from "react-router-dom";
import { Observable } from "rxjs";

import { IoTHubManagerService } from "services";
import {
    toSubmitMethodJobRequestModel,
    toDiagnosticsModel,
} from "services/models";
import { LinkedComponent } from "utilities";
import { svgs, Validator } from "utilities";
import {
    AjaxError,
    Btn,
    BtnToolbar,
    FormControl,
    FormGroup,
    FormLabel,
    FormSection,
    Indicator,
    SectionDesc,
    SectionHeader,
    SummaryBody,
    SummaryCount,
    SummarySection,
    Svg,
} from "components/shared";

const isAlphaNumericRegex = /^[a-zA-Z0-9]*$/,
    nonAlphaNumeric = (x) => !x.match(isAlphaNumericRegex),
    initialState = {
        isPending: false,
        error: undefined,
        successCount: 0,
        changesApplied: false,
        jobName: undefined,
        jobId: undefined,
        methodName: undefined,
        jsonPayload: {
            jsObject: {},
        },
        commonMethods: [],
    };

export class DeviceJobMethods extends LinkedComponent {
    constructor(props) {
        super(props);
        this.state = initialState;

        // Linked components
        this.jobNameLink = this.linkTo("jobName")
            .reject(nonAlphaNumeric)
            .check(Validator.notEmpty, () =>
                this.props.t("devices.flyouts.jobs.validation.required")
            );

        this.methodNameLink = this.linkTo("methodName")
            .map(({ value }) => value)
            .check(Validator.notEmpty, () =>
                this.props.t("devices.flyouts.jobs.validation.required")
            );

        this.jsonPayloadLink = this.linkTo("jsonPayload").check(
            (jsonPayloadObject) => !jsonPayloadObject.error,
            () => this.props.t("devices.flyouts.jobs.validation.invalid")
        );
    }

    componentDidMount() {
        if (this.props.devices) {
            this.populateState(this.props.devices);
        }
    }

    componentWillReceiveProps(nextProps) {
        if (
            nextProps.devices &&
            (this.props.devices || []).length !== nextProps.devices.length
        ) {
            this.populateState(nextProps.devices);
        }
    }

    componentWillUnmount() {
        if (this.populateStateSubscription) {
            this.populateStateSubscription.unsubscribe();
        }
        if (this.submitJobSubscription) {
            this.submitJobSubscription.unsubscribe();
        }
    }

    populateState(devices) {
        if (this.populateStateSubscription) {
            this.populateStateSubscription.unsubscribe();
        }
        this.populateStateSubscription = Observable.from(devices)
            .map(({ methods }) => new Set(methods))
            .reduce((commonMethods, deviceMethods) =>
                commonMethods
                    ? new Set(
                          [...commonMethods].filter((method) =>
                              deviceMethods.has(method)
                          )
                      )
                    : deviceMethods
            )
            .subscribe((commonMethodSet) => {
                const commonMethods = Array.from(
                    new Set([
                        ...commonMethodSet,
                        ...this.props.deviceGroup.supportedMethods.map(
                            (t) => t.method
                        ),
                    ])
                );
                this.setState({ commonMethods });
            });
    }

    formIsValid() {
        return [
            this.jobNameLink,
            this.methodNameLink,
            this.jsonPayloadLink,
        ].every((link) => !link.error);
    }

    apply = (event) => {
        event.preventDefault();
        if (this.formIsValid()) {
            this.setState({ isPending: true });
            this.props.logEvent(
                toDiagnosticsModel("Devices_NewJobApply_Click", {})
            );

            const { devices } = this.props,
                request = toSubmitMethodJobRequestModel(devices, this.state);

            if (this.submitJobSubscription) {
                this.submitJobSubscription.unsubscribe();
            }
            this.submitJobSubscription = IoTHubManagerService.submitJob(
                request
            ).subscribe(
                ({ jobId }) => {
                    this.setState({
                        jobId,
                        successCount: devices.length,
                        isPending: false,
                        changesApplied: true,
                    });
                },
                (error) => {
                    this.setState({
                        error,
                        isPending: false,
                        changesApplied: true,
                    });
                }
            );
        }
    };

    getSummaryMessage() {
        const { t } = this.props,
            { isPending, changesApplied } = this.state;

        if (isPending) {
            return t("devices.flyouts.jobs.pending");
        } else if (changesApplied) {
            return t("devices.flyouts.jobs.applySuccess");
        }
        return t("devices.flyouts.jobs.affected");
    }

    render() {
        const { t, onClose, devices, theme } = this.props,
            {
                isPending,
                error,
                successCount,
                changesApplied,
                commonMethods = [],
            } = this.state,
            summaryCount = changesApplied ? successCount : devices.length,
            completedSuccessfully =
                changesApplied && successCount === devices.length,
            summaryMessage = this.getSummaryMessage(),
            methodOptions = (commonMethods || []).map((name) => ({
                value: name,
                label: name,
            }));

        return (
            <form onSubmit={this.apply}>
                <FormSection className="device-job-Methods-container">
                    <SectionHeader>
                        {t("devices.flyouts.jobs.methods.title")}
                    </SectionHeader>
                    <SectionDesc>
                        {t("devices.flyouts.jobs.methods.description")}
                    </SectionDesc>

                    <FormGroup>
                        <FormLabel>
                            {t("devices.flyouts.jobs.methods.methodName")}
                        </FormLabel>
                        <FormControl
                            className="long"
                            type="select"
                            ariaLabel={t(
                                "devices.flyouts.jobs.methods.methodName"
                            )}
                            link={this.methodNameLink}
                            options={methodOptions}
                            placeholder={t(
                                "devices.flyouts.jobs.methods.methodNameHint"
                            )}
                            clearable={false}
                            searchable={true}
                            errorState={!!error}
                        />
                    </FormGroup>

                    <p>
                        NOTE: Methods can be reported from the device using the
                        reported property "SupportedMethods" (comma seperated
                        list). An alternative is to define them in the device
                        group. It is preferred to come from the device since
                        then the device will definitely support the method.{" "}
                    </p>

                    <FormGroup>
                        <FormLabel>
                            {t("devices.flyouts.jobs.jobName")}
                        </FormLabel>
                        <div className="help-message">
                            {t("devices.flyouts.jobs.jobNameHelpMessage")}
                        </div>
                        <FormControl
                            className="long"
                            link={this.jobNameLink}
                            type="text"
                            placeholder={t("devices.flyouts.jobs.jobNameHint")}
                        />
                    </FormGroup>

                    <FormGroup>
                        <FormLabel>
                            {t("devices.flyouts.jobs.methods.jsonPayload")}
                        </FormLabel>
                        <div className="help-message">
                            {t(
                                "devices.flyouts.jobs.methods.jsonPayloadMessage"
                            )}
                        </div>
                        <FormControl
                            link={this.jsonPayloadLink}
                            type="jsoninput"
                            height="200px"
                            theme={theme}
                        />
                    </FormGroup>

                    <SummarySection>
                        <SectionHeader>
                            {t("devices.flyouts.jobs.summaryHeader")}
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
                            className="device-jobs-error"
                            t={t}
                            error={error}
                        />
                    )}
                    {!changesApplied && (
                        <BtnToolbar>
                            <Btn
                                svg={svgs.reconfigure}
                                primary={true}
                                disabled={!this.formIsValid() || isPending}
                                type="submit"
                            >
                                {t("devices.flyouts.jobs.apply")}
                            </Btn>
                            <Btn svg={svgs.cancelX} onClick={onClose}>
                                {t("devices.flyouts.jobs.cancel")}
                            </Btn>
                        </BtnToolbar>
                    )}
                    {!!changesApplied && (
                        <BtnToolbar>
                            <Link
                                to={`/maintenance/job/${this.state.jobId}`}
                                className="btn btn-primary"
                            >
                                {t("devices.flyouts.jobs.viewStatus")}
                            </Link>
                            <Btn svg={svgs.cancelX} onClick={onClose}>
                                {t("devices.flyouts.jobs.close")}
                            </Btn>
                        </BtnToolbar>
                    )}
                </FormSection>
            </form>
        );
    }
}
