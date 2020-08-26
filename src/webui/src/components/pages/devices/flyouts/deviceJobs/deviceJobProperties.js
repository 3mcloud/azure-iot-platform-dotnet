// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import { Link } from "react-router-dom";
import { Observable } from "rxjs";
import update from "immutability-helper";

import { IoTHubManagerService } from "services";
import {
    toSubmitPropertiesJobRequestModel,
    toDiagnosticsModel,
} from "services/models";
import { svgs, LinkedComponent, Validator } from "utilities";
import {
    AjaxError,
    Btn,
    BtnToolbar,
    ComponentArray,
    ErrorMsg,
    FormControl,
    FormGroup,
    FormLabel,
    FormSection,
    Indicator,
    PropertyGrid as Grid,
    PropertyGridBody as GridBody,
    PropertyGridHeader as GridHeader,
    PropertyRow as Row,
    PropertyCell as Cell,
    SectionDesc,
    SectionHeader,
    SummaryBody,
    SummaryCount,
    SummarySection,
    Svg,
} from "components/shared";

update.extend("$autoArray", (val, obj) => update(obj || [], val));

const isNumeric = (value) => typeof value === "number",
    isAlphaNumericRegex = /^[a-zA-Z0-9]*$/,
    nonAlphaNumeric = (x) => !x.match(isAlphaNumericRegex);

export const propertyJobConstants = {
    firmware: "Firmware",
    multipleValues: "Multiple",
    stringType: "Text",
    numberType: "Number",
};

const initialState = {
    isPending: false,
    error: undefined,
    successCount: 0,
    changesApplied: false,
    jobName: undefined,
    jobId: undefined,
    commonProperties: [],
    deletedProperties: [],
};

export class DeviceJobProperties extends LinkedComponent {
    constructor(props) {
        super(props);
        this.state = initialState;

        // Linked components
        this.jobNameLink = this.linkTo("jobName")
            .reject(nonAlphaNumeric)
            .check(Validator.notEmpty, () =>
                this.props.t("devices.flyouts.jobs.validation.required")
            );

        this.propertiesLink = this.linkTo("commonProperties");
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

    populateState = (devices) => {
        const { t } = this.props;

        if (this.populateStateSubscription) {
            this.populateStateSubscription.unsubscribe();
        }

        // Rework device data so the reported and desired values are grouped together under the property name.
        const devicesWithProps = devices.map((device) => {
            const properties = {};
            Object.keys(device.properties).forEach((propertyName) => {
                const reported = device.properties[propertyName],
                    desired = device.desiredProperties[propertyName],
                    inSync =
                        !desired ||
                        (typeof device.properties[propertyName] === "object"
                            ? JSON.stringify(desired) ===
                              JSON.stringify(reported)
                            : reported === desired),
                    display = inSync ? reported : desired;
                properties[propertyName] = {
                    reported,
                    desired,
                    display,
                    inSync,
                };
            });
            return { id: device.id, properties };
        });
        debugger;
        this.populateStateSubscription = Observable.from(devicesWithProps)
            .map(({ properties }) => new Set(Object.keys(properties)))
            .reduce((commonProperties, deviceProperties) =>
                commonProperties
                    ? new Set(
                          [...commonProperties].filter((property) =>
                              deviceProperties.has(property)
                          )
                      )
                    : deviceProperties
            ) // At this point, a stream of a single event. A common set of properties.
            .flatMap((commonPropertiesSet) =>
                Observable.from(devicesWithProps)
                    .flatMap(({ properties }) => Object.entries(properties))
                    .filter(([property]) => commonPropertiesSet.has(property))
            )
            .distinct(
                ([propertyName, propertyVal]) =>
                    `${propertyName} ${propertyVal.display}`
            )
            .reduce(
                (acc, [propertyName, propertyVal]) =>
                    update(acc, {
                        [propertyName]: {
                            $autoArray: {
                                $push: [propertyVal],
                            },
                        },
                    }),
                {}
            )
            .flatMap((propertyToValMap) => Object.entries(propertyToValMap))
            .reduce(
                (newState, [name, values]) => {
                    const valueData = values.reduce(
                        (valAcc, { reported, desired, display, inSync }) => {
                            if (!valAcc.reported) {
                                valAcc.reported = reported;
                                valAcc.display = display;
                            }
                            if (!inSync) {
                                valAcc.anyOutOfSync = true;
                            }
                            if (reported !== valAcc.reported) {
                                valAcc.reported =
                                    propertyJobConstants.multipleValues;
                                valAcc.display = valAcc.anyOutOfSync
                                    ? t(
                                          "devices.flyouts.jobs.properties.syncing",
                                          {
                                              reportedPropertyValue:
                                                  propertyJobConstants.multipleValues,
                                              desiredPropertyValue: "",
                                          }
                                      )
                                    : propertyJobConstants.multipleValues;
                            }
                            if (!isNumeric(reported)) {
                                valAcc.type = propertyJobConstants.stringType;
                            }
                            return valAcc;
                        },
                        {
                            reported: undefined,
                            display: undefined,
                            anyOutOfSync: false,
                            type: propertyJobConstants.numberType,
                        }
                    );
                    return update(newState, {
                        commonProperties: {
                            $push: [
                                {
                                    name,
                                    value: valueData.display,
                                    jsonValue: {
                                        jsObject: valueData.display,
                                    },
                                    type: valueData.type,
                                    readOnly:
                                        name ===
                                            propertyJobConstants.firmware ||
                                        valueData.anyOutOfSync,
                                },
                            ],
                        },
                    });
                },
                { ...initialState, jobName: this.state.jobName }
            )
            .subscribe((newState) => this.setState(newState));
    };

    formIsValid() {
        return [this.jobNameLink].every((link) => !link.error);
    }

    apply = (event) => {
        debugger;
        event.preventDefault();
        if (this.formIsValid()) {
            this.setState({ isPending: true });
            this.props.logEvent(
                toDiagnosticsModel("Devices_NewJobApply_Click", {})
            );
            const { devices } = this.props,
                { commonProperties, deletedProperties } = this.state,
                updatedProperties = commonProperties.filter(
                    ({ value, readOnly }) =>
                        value !== propertyJobConstants.multipleValues &&
                        !readOnly
                ),
                request = toSubmitPropertiesJobRequestModel(
                    devices,
                    update(this.state, {
                        updatedProperties: { $set: updatedProperties },
                    })
                );

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
                    this.props.updateProperties({
                        deviceIds: devices.map(({ id }) => id),
                        updatedProperties,
                        deletedProperties,
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

    onJsonChange = (e) => {
        var stateCommonProperties = this.state.commonProperties;
        console.log(stateCommonProperties);
        stateCommonProperties.forEach((property) => {
            property.value = property.jsonValue.jsObject;
        });
        this.setState({
            commonProperties: stateCommonProperties,
        });
    };

    render() {
        const { t, onClose, devices, theme } = this.props,
            {
                isPending,
                error,
                successCount,
                changesApplied,
                commonProperties = [],
            } = this.state,
            summaryCount = changesApplied ? successCount : devices.length,
            completedSuccessfully =
                changesApplied && successCount === devices.length,
            summaryMessage = this.getSummaryMessage(),
            // Link these values in render because they need to update based on component state
            propertyLinks = this.propertiesLink.getLinkedChildren(
                (propertyLink) => {
                    const name = propertyLink
                            .forkTo("name")
                            .check(
                                Validator.notEmpty,
                                this.props.t(
                                    "devices.flyouts.jobs.validation.required"
                                )
                            ),
                        value = propertyLink
                            .forkTo("value")
                            .check(
                                Validator.notEmpty,
                                this.props.t(
                                    "devices.flyouts.jobs.validation.required"
                                )
                            ),
                        jsonValue = propertyLink
                            .forkTo("jsonValue")
                            .check(
                                Validator.notEmpty,
                                this.props.t(
                                    "devices.flyouts.jobs.validation.required"
                                )
                            ),
                        type = propertyLink
                            .forkTo("type")
                            .map(({ value }) => value)
                            .check(
                                Validator.notEmpty,
                                this.props.t(
                                    "devices.flyouts.jobs.validation.required"
                                )
                            ),
                        readOnly = propertyLink.forkTo("readOnly"),
                        edited = !(!name.value && !value.value && !type.value),
                        error =
                            (edited &&
                                (name.error || value.error || type.error)) ||
                            "";
                    return {
                        name,
                        value,
                        jsonValue,
                        type,
                        readOnly,
                        edited,
                        error,
                    };
                }
            ),
            editedProperties = propertyLinks.filter(({ edited }) => edited),
            propertiesHaveErrors = editedProperties.some(
                ({ error }) => !!error
            );

        return (
            <form onSubmit={this.apply}>
                <FormSection className="device-job-properties-container">
                    <SectionHeader>
                        {t("devices.flyouts.jobs.properties.title")}
                    </SectionHeader>
                    <SectionDesc>
                        {t("devices.flyouts.jobs.properties.description")}
                    </SectionDesc>

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

                    <Grid className="data-grid">
                        <GridHeader>
                            <Row>
                                <Cell className="col-3">
                                    {t(
                                        "devices.flyouts.jobs.properties.keyHeader"
                                    )}
                                </Cell>
                                <Cell className="col-3">
                                    {t(
                                        "devices.flyouts.jobs.properties.valueHeader"
                                    )}
                                </Cell>
                                <Cell className="col-3">
                                    {t(
                                        "devices.flyouts.jobs.properties.typeHeader"
                                    )}
                                </Cell>
                                <Cell className="col-1"></Cell>
                            </Row>
                        </GridHeader>
                        {Object.keys(commonProperties).length === 0 &&
                            summaryCount === 1 && (
                                <div className="device-jobs-info">
                                    {t(
                                        "devices.flyouts.details.properties.noneExist"
                                    )}
                                </div>
                            )}
                        {Object.keys(commonProperties).length === 0 &&
                            summaryCount > 1 && (
                                <ErrorMsg className="device-jobs-error">
                                    {t(
                                        "devices.flyouts.jobs.properties.noneExist"
                                    )}
                                </ErrorMsg>
                            )}
                        <GridBody>
                            {Object.keys(commonProperties).length > 0 &&
                                propertyLinks.map(
                                    (
                                        {
                                            name,
                                            jsonValue,
                                            value,
                                            type,
                                            readOnly,
                                            edited,
                                            error,
                                        },
                                        idx
                                    ) => (
                                        <ComponentArray>
                                            <Row
                                                id={idx}
                                                className={
                                                    error
                                                        ? "error-data-row"
                                                        : ""
                                                }
                                            >
                                                <div>{name.value}</div>
                                                <br />
                                                <div className="col-3">
                                                    <FormControl
                                                        className="small"
                                                        type="jsoninput"
                                                        link={jsonValue}
                                                        theme={
                                                            theme
                                                                ? theme
                                                                : "light"
                                                        }
                                                        errorState={!!error}
                                                        readOnly={
                                                            readOnly.value
                                                        }
                                                        onChange={
                                                            this.onJsonChange
                                                        }
                                                    />
                                                </div>
                                                {readOnly.value && (
                                                    <div>Syncing</div>
                                                )}
                                                &nbsp;&nbsp; &nbsp;
                                                &nbsp;&nbsp; &nbsp;
                                                &nbsp;&nbsp; &nbsp;
                                                <div>{type.value}</div>
                                            </Row>
                                            {error ? (
                                                <Row className="error-msg-row">
                                                    <ErrorMsg>{error}</ErrorMsg>
                                                </Row>
                                            ) : null}
                                        </ComponentArray>
                                    )
                                )}
                        </GridBody>
                    </Grid>

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
                                disabled={
                                    !this.formIsValid() ||
                                    propertiesHaveErrors ||
                                    isPending
                                }
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
