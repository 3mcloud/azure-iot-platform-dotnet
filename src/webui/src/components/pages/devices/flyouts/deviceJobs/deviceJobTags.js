// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import { Link } from "react-router-dom";
import { from } from "rxjs";
import update from "immutability-helper";

import { IoTHubManagerService } from "services";
import {
    toSubmitTagsJobRequestModel,
    toDiagnosticsModel,
} from "services/models";
import { LinkedComponent } from "utilities";
import { svgs, Validator } from "utilities";
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
import { distinct, filter, map, mergeMap, reduce } from "rxjs/operators";
const classnames = require("classnames/bind");
const css = classnames.bind(require("./deviceJobs.module.scss"));

update.extend("$autoArray", (val, obj) => update(obj || [], val));

const isNumeric = (value) => typeof value === "number",
    isAlphaNumericRegex = /^[a-zA-Z0-9]*$/,
    nonAlphaNumeric = (x) => !x.match(isAlphaNumericRegex),
    tagJobConstants = {
        multipleValues: "Multiple",
        stringType: "Text",
        numberType: "Number",
    },
    initialState = {
        isPending: false,
        error: undefined,
        successCount: 0,
        changesApplied: false,
        jobName: undefined,
        jobId: undefined,
        commonTags: [],
        deletedTags: [],
    },
    newTag = () => ({
        name: "",
        value: "",
        type: tagJobConstants.stringType,
    });

export class DeviceJobTags extends LinkedComponent {
    constructor(props) {
        super(props);
        this.state = initialState;

        // Linked components
        this.jobNameLink = this.linkTo("jobName")
            .reject(nonAlphaNumeric)
            .check(Validator.notEmpty, () =>
                this.props.t("devices.flyouts.jobs.validation.required")
            );

        this.tagsLink = this.linkTo("commonTags");
    }

    componentDidMount() {
        if (this.props.devices) {
            this.populateState(this.props.devices);
        }
    }

    UNSAFE_componentWillReceiveProps(nextProps) {
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
        this.populateStateSubscription = from(devices)
            .pipe(
                map(({ tags }) => new Set(Object.keys(tags))),
                reduce((commonTags, deviceTags) =>
                    commonTags
                        ? new Set(
                              [...commonTags].filter((tag) =>
                                  deviceTags.has(tag)
                              )
                          )
                        : deviceTags
                ), // At this point, a stream of a single event. A common set of tags.
                mergeMap((commonTagsSet) =>
                    from(devices).pipe(
                        mergeMap(({ tags }) => Object.entries(tags)),
                        filter(([tag]) => commonTagsSet.has(tag))
                    )
                ),
                distinct(([tagName, tagVal]) => `${tagName} ${tagVal}`),
                reduce(
                    (acc, [tagName, tagVal]) =>
                        update(acc, {
                            [tagName]: {
                                $autoArray: {
                                    $push: [tagVal],
                                },
                            },
                        }),
                    {}
                ),
                mergeMap((tagToValMap) => Object.entries(tagToValMap)),
                reduce(
                    (newState, [name, values]) => {
                        const value =
                                values.length === 1
                                    ? values[0]
                                    : tagJobConstants.multipleValues,
                            type = values.every(isNumeric)
                                ? tagJobConstants.numberType
                                : tagJobConstants.stringType;
                        return update(newState, {
                            commonTags: { $push: [{ name, value, type }] },
                        });
                    },
                    { ...initialState, jobName: this.state.jobName }
                )
            )
            .subscribe((newState) => this.setState(newState));
    }

    formIsValid() {
        return [this.jobNameLink].every((link) => !link.error);
    }

    apply = (event) => {
        event.preventDefault();
        if (this.formIsValid()) {
            this.setState({
                isPending: true,
                updatedTags: this.state.commonTags.filter(
                    ({ value }) => value !== tagJobConstants.multipleValues
                ),
            });

            this.props.logEvent(
                toDiagnosticsModel("Devices_NewJobApply_Click", {})
            );

            const { devices } = this.props,
                { commonTags, deletedTags } = this.state,
                updatedTags = commonTags.filter(
                    ({ value }) => value !== tagJobConstants.multipleValues
                ),
                request = toSubmitTagsJobRequestModel(
                    devices,
                    update(this.state, { updatedTags: { $set: updatedTags } })
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
                    this.props.updateTags({
                        deviceIds: devices.map(({ id }) => id),
                        updatedTags,
                        deletedTags,
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

    addTag = () => this.tagsLink.set([...this.tagsLink.value, newTag()]);

    deleteTag = (index) => (evt) => {
        this.setState(
            update(this.state, {
                commonTags: {
                    $set: this.tagsLink.value.filter((_, idx) => index !== idx),
                },
                deletedTags: { $push: [this.tagsLink.value[index].name] },
            })
        );
    };

    render() {
        const { t, onClose, devices } = this.props,
            {
                isPending,
                error,
                successCount,
                changesApplied,
                commonTags = [],
            } = this.state,
            summaryCount = changesApplied ? successCount : devices.length,
            completedSuccessfully =
                changesApplied && successCount === devices.length,
            summaryMessage = this.getSummaryMessage(),
            typeOptions = [
                {
                    value: tagJobConstants.numberType,
                    label: t("devices.flyouts.jobs.tags.typeNumber"),
                },
                {
                    value: tagJobConstants.stringType,
                    label: t("devices.flyouts.jobs.tags.typeString"),
                },
            ],
            // Link these values in render because they need to update based on component state
            tagLinks = this.tagsLink.getLinkedChildren((tagLink) => {
                const name = tagLink
                        .forkTo("name")
                        .check(
                            Validator.notEmpty,
                            this.props.t(
                                "devices.flyouts.jobs.validation.required"
                            )
                        ),
                    value = tagLink
                        .forkTo("value")
                        .check(
                            Validator.notEmpty,
                            this.props.t(
                                "devices.flyouts.jobs.validation.required"
                            )
                        ),
                    type = tagLink
                        .forkTo("type")
                        .map(({ value }) => value)
                        .check(
                            Validator.notEmpty,
                            this.props.t(
                                "devices.flyouts.jobs.validation.required"
                            )
                        ),
                    edited = !(!name.value && !value.value && !type.value),
                    error =
                        (edited && (name.error || value.error || type.error)) ||
                        "";
                return { name, value, type, edited, error };
            }),
            editedTags = tagLinks.filter(({ edited }) => edited),
            tagsHaveErrors = editedTags.some(({ error }) => !!error);

        return (
            <form onSubmit={this.apply}>
                <FormSection className={css("device-job-tags-container")}>
                    <SectionHeader>
                        {t("devices.flyouts.jobs.tags.title")}
                    </SectionHeader>
                    <SectionDesc>
                        {t("devices.flyouts.jobs.tags.description")}
                    </SectionDesc>

                    <FormGroup>
                        <FormLabel>
                            {t("devices.flyouts.jobs.jobName")}
                        </FormLabel>
                        <div className={css("help-message")}>
                            {t("devices.flyouts.jobs.jobNameHelpMessage")}
                        </div>
                        <FormControl
                            className="long"
                            link={this.jobNameLink}
                            type="text"
                            placeholder={t("devices.flyouts.jobs.jobNameHint")}
                        />
                    </FormGroup>

                    <Grid className={css("data-grid")}>
                        <GridHeader>
                            <Row>
                                <Cell className="col-3">
                                    {t("devices.flyouts.jobs.tags.keyHeader")}
                                </Cell>
                                <Cell className="col-3">
                                    {t("devices.flyouts.jobs.tags.valueHeader")}
                                </Cell>
                                <Cell className="col-3">
                                    {t("devices.flyouts.jobs.tags.typeHeader")}
                                </Cell>
                                <Cell className="col-1"></Cell>
                            </Row>
                            <Row className={css("action-row")}>
                                <Btn svg={svgs.plus} onClick={this.addTag}>
                                    {t("devices.flyouts.jobs.tags.add")}
                                </Btn>
                            </Row>
                        </GridHeader>
                        {Object.keys(commonTags).length === 0 &&
                            summaryCount === 1 && (
                                <div className={css("device-jobs-info")}>
                                    {t(
                                        "devices.flyouts.details.tags.noneExist"
                                    )}
                                </div>
                            )}
                        {Object.keys(commonTags).length === 0 &&
                            summaryCount > 1 && (
                                <ErrorMsg className={css("device-jobs-error")}>
                                    {t("devices.flyouts.jobs.tags.noneExist")}
                                </ErrorMsg>
                            )}
                        <GridBody>
                            {Object.keys(commonTags).length > 0 &&
                                tagLinks.map(
                                    (
                                        { name, value, type, edited, error },
                                        idx
                                    ) => (
                                        <ComponentArray key={idx}>
                                            <Row
                                                className={
                                                    error
                                                        ? css("error-data-row")
                                                        : ""
                                                }
                                            >
                                                <Cell className="col-3">
                                                    <FormControl
                                                        className="small"
                                                        type="text"
                                                        link={name}
                                                        errorState={!!error}
                                                    />
                                                </Cell>
                                                <Cell className="col-3">
                                                    <FormControl
                                                        className="small"
                                                        type="text"
                                                        link={value}
                                                        errorState={!!error}
                                                    />
                                                </Cell>
                                                <Cell className="col-3">
                                                    <FormControl
                                                        className="small"
                                                        type="select"
                                                        ariaLabel={t(
                                                            "devices.flyouts.jobs.tags.typeHeader"
                                                        )}
                                                        link={type}
                                                        options={typeOptions}
                                                        clearable={false}
                                                        searchable={true}
                                                        errorState={!!error}
                                                    />
                                                </Cell>
                                                <Cell className="col-1">
                                                    <Btn
                                                        className="icon-only-btn"
                                                        svg={svgs.trash}
                                                        onClick={this.deleteTag(
                                                            idx
                                                        )}
                                                    />
                                                </Cell>
                                            </Row>
                                            {error ? (
                                                <Row
                                                    className={css(
                                                        "error-msg-row"
                                                    )}
                                                >
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
                                    className={css("summary-icon")}
                                    src={svgs.apply}
                                />
                            )}
                        </SummaryBody>
                    </SummarySection>

                    {error && (
                        <AjaxError
                            className={css("device-jobs-error")}
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
                                    tagsHaveErrors ||
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
