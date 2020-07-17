// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { permissions, toDiagnosticsModel } from "services/models";
import { svgs, LinkedComponent, Validator } from "utilities";
import { DeviceGroupDelete } from "./deviceGroupDelete";
import {
    AjaxError,
    Btn,
    BtnToolbar,
    // ComponentArray,
    FormControl,
    FormGroup,
    FormLabel,
    Indicator,
    Protected,
} from "components/shared";
import { ConfigService } from "services";
import {
    toCreateDeviceGroupRequestModel,
    toUpdateDeviceGroupRequestModel,
} from "services/models";

import Flyout from "components/shared/flyout";
import { DeviceGroupTelemetryFormatContainer } from "../deviceGroupTelemetryFormat.container";
import { DeviceGroupSupportedMethodsContainer } from "../deviceGroupSupportedMethods.container";

const Section = Flyout.Section;

// A counter for creating unique keys per new condition
let conditionKey = 0;

// Creates a state object for a condition
const newCondition = () => ({
        field: undefined,
        operator: undefined,
        type: undefined,
        value: "",
        key: conditionKey++, // Used by react to track the rendered elements
    }),
    operators = ["EQ", "GT", "LT", "GE", "LE"],
    valueTypes = ["Number", "Text"];

const conditionIsNew = (condition) => {
    return (
        !condition.field &&
        !condition.operator &&
        !condition.type &&
        !condition.value
    );
};

class DeviceGroupForm extends LinkedComponent {
    constructor(props) {
        super(props);

        this.state = {
            id: undefined,
            eTag: undefined,
            displayName: "",
            conditions: [newCondition()],
            telemetryFormat: [],
            supportedMethods: [],
            isPending: false,
            error: undefined,
            isEdit: this.props.selectedDeviceGroup,
            isDelete: undefined,
            IsPinned: false,
            SortOrder: 0,
        };

        // State to input links
        this.nameLink = this.linkTo("displayName").check(
            Validator.notEmpty,
            () => this.props.t("deviceGroupsFlyout.errorMsg.nameCantBeEmpty")
        );

        this.conditionsLink = this.linkTo("conditions");
        this.subscriptions = [];
    }

    formIsValid() {
        return [this.nameLink, this.conditionsLink].every(
            (link) => !link.error
        );
    }

    componentDidMount() {
        if (this.state.isEdit) {
            this.computeState(this.props);
        }
    }

    componentWillUnmount() {
        this.subscriptions.forEach((sub) => sub.unsubscribe());
    }

    computeState = ({
        selectedDeviceGroup: {
            id,
            eTag,
            conditions,
            displayName,
            telemetryFormat,
            supportedMethods,
            isPinned,
            sortOrder,
        },
    }) => {
        if (this.state.isEdit) {
            this.setState({
                id,
                eTag,
                displayName,
                conditions: conditions.map((condition) => ({
                    field: condition.key,
                    operator: condition.operator,
                    type: isNaN(condition.value) ? "Text" : "Number",
                    value: condition.value,
                    key: conditionKey++,
                })),
                telemetryFormat: telemetryFormat,
                supportedMethods: supportedMethods,
                isPinned: isPinned,
                sortOrder: sortOrder,
            });
        }
    };

    toSelectOption = ({ id, name }) => ({ value: id, label: name });

    selectServiceCall = () => {
        if (this.state.isEdit) {
            return ConfigService.updateDeviceGroup(
                this.state.id,
                toUpdateDeviceGroupRequestModel(this.state)
            );
        }
        return ConfigService.createDeviceGroup(
            toCreateDeviceGroupRequestModel(this.state)
        );
    };

    apply = (event) => {
        this.props.logEvent(toDiagnosticsModel("DeviceGroup_Save", {}));
        event.preventDefault();
        this.setState({ error: undefined, isPending: true });
        // Remove all empty rows
        this.setState(
            {
                telemetryFormat: this.state.telemetryFormat.filter(
                    (t) => t.key !== "" || t.displayName !== ""
                ),
                conditions: this.state.conditions.filter(
                    (condition) => !conditionIsNew(condition)
                ),
            },
            function () {
                this.subscriptions.push(
                    this.selectServiceCall().subscribe(
                        (deviceGroup) => {
                            this.props.insertDeviceGroups([deviceGroup]);
                            this.props.cancel();
                        },
                        (error) => this.setState({ error, isPending: false })
                    )
                );
            }.bind(this)
        );
    };

    updateTelemetryFormat = (value) =>
        this.setState({ telemetryFormat: value });
    updateSupportedMethods = (value) =>
        this.setState({ supportedMethods: value });
    addCondition = () => {
        this.props.logEvent(toDiagnosticsModel("DeviceGroup_AddCondition", {}));
        return this.conditionsLink.set([
            ...this.conditionsLink.value,
            newCondition(),
        ]);
    };

    deleteCondition = (index) => () => {
        this.props.logEvent(
            toDiagnosticsModel("DeviceGroup_RemoveCondition", {})
        );
        return this.conditionsLink.set(
            this.conditionsLink.value.filter((_, idx) => index !== idx)
        );
    };

    deleteDeviceGroup = () => {
        this.setState({
            isDelete: true,
        });
    };

    onCancel = () => {
        this.props.logEvent(toDiagnosticsModel("DeviceGroup_Cancel", {}));
        this.props.cancel();
    };

    closeDeleteForm = () =>
        this.setState({
            isDelete: false,
        });

    render() {
        const { t } = this.props,
            // Create the state link for the dynamic form elements
            conditionLinks = this.conditionsLink.getLinkedChildren(
                (conditionLink) => {
                    const field = conditionLink
                            .forkTo("field")
                            .map(({ value }) => value)
                            .check(
                                Validator.notEmpty,
                                t(
                                    "deviceQueryConditions.errorMsg.fieldCantBeEmpty"
                                )
                            ),
                        operator = conditionLink
                            .forkTo("operator")
                            .map(({ value }) => value)
                            .check(
                                Validator.notEmpty,
                                t(
                                    "deviceQueryConditions.errorMsg.operatorCantBeEmpty"
                                )
                            ),
                        type = conditionLink
                            .forkTo("type")
                            .map(({ value }) => value)
                            .check(
                                Validator.notEmpty,
                                t(
                                    "deviceQueryConditions.errorMsg.typeCantBeEmpty"
                                )
                            ),
                        value = conditionLink
                            .forkTo("value")
                            .check(
                                Validator.notEmpty,
                                t(
                                    "deviceQueryConditions.errorMsg.valueCantBeEmpty"
                                )
                            )
                            .check(
                                (val) =>
                                    type.value === "Number"
                                        ? !isNaN(val)
                                        : true,
                                t("deviceQueryConditions.errorMsg.selectedType")
                            ),
                        edited = !(
                            !field.value &&
                            !operator.value &&
                            !value.value &&
                            !type.value
                        ),
                        error =
                            (edited &&
                                (field.error ||
                                    operator.error ||
                                    value.error ||
                                    type.error)) ||
                            "";
                    return { field, operator, value, type, edited, error };
                }
            ),
            editedConditions = conditionLinks.filter(({ edited }) => edited),
            conditionHasErrors = editedConditions.some(({ error }) => !!error),
            operatorOptions = operators.map((value) => ({
                label: t(`deviceQueryConditions.operatorOptions.${value}`),
                value,
            })),
            typeOptions = valueTypes.map((value) => ({
                label: t(`deviceQueryConditions.typeOptions.${value}`),
                value,
            }));
        const { telemetryFormat, supportedMethods } = this.state;
        return (
            <div>
                {!this.state.isDelete ? (
                    <form onSubmit={this.apply}>
                        <Section.Container
                            collapsable={false}
                            className="borderless"
                        >
                            <Section.Header>
                                {this.state.isEdit
                                    ? t("deviceGroupsFlyout.edit")
                                    : t("deviceGroupsFlyout.new")}
                            </Section.Header>
                            <Section.Content>
                                <FormGroup>
                                    <FormLabel isRequired="true">
                                        {t("deviceGroupsFlyout.name")}
                                    </FormLabel>
                                    <FormControl
                                        type="text"
                                        className="long"
                                        placeholder={t(
                                            "deviceGroupsFlyout.namePlaceHolder"
                                        )}
                                        link={this.nameLink}
                                    />
                                </FormGroup>
                                <Btn
                                    className="add-btn"
                                    svg={svgs.plus}
                                    onClick={this.addCondition}
                                >
                                    {t("deviceQueryConditions.add")}
                                </Btn>
                                {conditionLinks.map((condition, idx) => (
                                    <Section.Container
                                        key={this.state.conditions[idx].key}
                                    >
                                        <Section.Header>
                                            {t(
                                                "deviceQueryConditions.condition",
                                                {
                                                    headerCount: idx + 1,
                                                }
                                            )}
                                        </Section.Header>
                                        <Section.Content>
                                            <FormGroup>
                                                <FormLabel isRequired="true">
                                                    {t(
                                                        "deviceQueryConditions.field"
                                                    )}
                                                </FormLabel>
                                                {this.props.filtersError ? (
                                                    <AjaxError
                                                        t={t}
                                                        error={
                                                            this.props
                                                                .filtersError
                                                        }
                                                    />
                                                ) : (
                                                    <FormControl
                                                        type="select"
                                                        ariaLabel={t(
                                                            "deviceQueryConditions.field"
                                                        )}
                                                        className="long"
                                                        searchable={false}
                                                        clearable={false}
                                                        placeholder={t(
                                                            "deviceQueryConditions.fieldPlaceholder"
                                                        )}
                                                        options={
                                                            this.props
                                                                .filterOptions
                                                        }
                                                        link={condition.field}
                                                    />
                                                )}
                                            </FormGroup>
                                            <FormGroup>
                                                <FormLabel isRequired="true">
                                                    {t(
                                                        "deviceQueryConditions.operator"
                                                    )}
                                                </FormLabel>
                                                <FormControl
                                                    type="select"
                                                    ariaLabel={t(
                                                        "deviceQueryConditions.operator"
                                                    )}
                                                    className="long"
                                                    searchable={false}
                                                    clearable={false}
                                                    options={operatorOptions}
                                                    placeholder={t(
                                                        "deviceQueryConditions.operatorPlaceholder"
                                                    )}
                                                    link={condition.operator}
                                                />
                                            </FormGroup>
                                            <FormGroup>
                                                <FormLabel isRequired="true">
                                                    {t(
                                                        "deviceQueryConditions.value"
                                                    )}
                                                </FormLabel>
                                                <FormControl
                                                    type="text"
                                                    placeholder={t(
                                                        "deviceQueryConditions.valuePlaceholder"
                                                    )}
                                                    link={condition.value}
                                                />
                                            </FormGroup>
                                            <FormGroup>
                                                <FormLabel isRequired="true">
                                                    {t(
                                                        "deviceQueryConditions.type"
                                                    )}
                                                </FormLabel>
                                                <FormControl
                                                    type="select"
                                                    ariaLabel={t(
                                                        "deviceQueryConditions.type"
                                                    )}
                                                    className="short"
                                                    clearable={false}
                                                    searchable={false}
                                                    options={typeOptions}
                                                    placeholder={t(
                                                        "deviceQueryConditions.typePlaceholder"
                                                    )}
                                                    link={condition.type}
                                                />
                                            </FormGroup>
                                            <BtnToolbar>
                                                <Btn
                                                    onClick={this.deleteCondition(
                                                        idx
                                                    )}
                                                >
                                                    {t(
                                                        "deviceQueryConditions.remove"
                                                    )}
                                                </Btn>
                                            </BtnToolbar>
                                        </Section.Content>
                                    </Section.Container>
                                ))}
                                {this.state.isPending && (
                                    <Indicator pattern="bar" size="medium" />
                                )}

                                <DeviceGroupTelemetryFormatContainer
                                    t={t}
                                    format={telemetryFormat}
                                    onTelemetryChange={
                                        this.updateTelemetryFormat
                                    }
                                />
                                <DeviceGroupSupportedMethodsContainer
                                    t={t}
                                    methods={supportedMethods}
                                    onMethodsChange={
                                        this.updateSupportedMethods
                                    }
                                />
                                <BtnToolbar>
                                    <Protected
                                        permission={
                                            permissions.updateDeviceGroups
                                        }
                                    >
                                        <Btn
                                            primary
                                            disabled={
                                                !this.formIsValid() ||
                                                conditionHasErrors ||
                                                this.state.isPending
                                            }
                                            type="submit"
                                        >
                                            {t("deviceGroupsFlyout.save")}
                                        </Btn>
                                    </Protected>
                                    <Btn
                                        svg={svgs.cancelX}
                                        onClick={this.onCancel}
                                    >
                                        {t("deviceGroupsFlyout.cancel")}
                                    </Btn>
                                    {// Don't show delete btn if it is a new group or the group is currently active
                                    this.state.isEdit && (
                                        <Protected
                                            permission={
                                                permissions.deleteDeviceGroups
                                            }
                                        >
                                            <Btn
                                                svg={svgs.trash}
                                                onClick={this.deleteDeviceGroup}
                                                disabled={this.state.isPending}
                                            >
                                                {t(
                                                    "deviceQueryConditions.delete"
                                                )}
                                            </Btn>
                                        </Protected>
                                    )}
                                </BtnToolbar>
                                {this.state.error && (
                                    <AjaxError t={t} error={this.state.error} />
                                )}
                            </Section.Content>
                        </Section.Container>
                    </form>
                ) : (
                    <DeviceGroupDelete
                        {...this.props}
                        {...this.state}
                        cancelDelete={this.closeDeleteForm}
                        closeDeviceGroup={this.onCancel}
                    />
                )}
            </div>
        );
    }
}

export default DeviceGroupForm;
