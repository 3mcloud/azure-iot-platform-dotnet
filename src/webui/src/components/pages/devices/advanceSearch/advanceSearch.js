// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { toDiagnosticsModel } from "services/models";
import { toDeviceConditionModel } from "services/models/configModels.js";
import { svgs, LinkedComponent, Validator } from "utilities";
import {
    AjaxError,
    Btn,
    BtnToolbar,
    FormControl,
    PropertyRow as Row,
    PropertyCell as Cell,
    PropertyGrid as Grid,
} from "components/shared";

import "./advanceSearch.scss";

// A counter for creating unique keys per new condition
let conditionKey = 0;

const operators = ["EQ", "GT", "LT", "GE", "LE", "LK"],
    valueTypes = ["Number", "Text"],
    // Creates a state object for a condition
    newCondition = () => ({
        field: undefined,
        operator: undefined,
        type: undefined,
        value: "",
        key: conditionKey++, // Used by react to track the rendered elements
    }),
    toFormConditionModels = (models = []) => {
        return models.map((model) => {
            return {
                field: model.key,
                operator: model.operator,
                value: model.value || "",
                type: model.value
                    ? isNaN(model.value)
                        ? "Text"
                        : "Number"
                    : undefined,
                key: conditionKey++,
            };
        });
    };

export class AdvanceSearch extends LinkedComponent {
    constructor(props) {
        super(props);

        this.state = {
            filterOptions: [{ label: "Device Name", value: "deviceId" }],
            deviceQueryConditions:
                this.props.activeDeviceQueryConditions.length === 0
                    ? [newCondition()]
                    : toFormConditionModels(
                          this.props.activeDeviceQueryConditions
                      ),
            isPending: false,
            error: undefined,
        };

        // State to input links
        this.conditionsLink = this.linkTo("deviceQueryConditions");
        this.subscriptions = [];
    }

    conditionIsNew(condition) {
        return (
            !condition.field &&
            !condition.operator &&
            !condition.type &&
            !condition.value
        );
    }

    formIsValid() {
        return [this.conditionsLink].every((link) => !link.error);
    }

    componentWillUnmount() {
        this.subscriptions.forEach((sub) => sub.unsubscribe());
    }

    queryDevices = () => {
        return new Promise((resolve, reject) => {
            try {
                this.setState({ error: undefined, isPending: true }, () => {
                    const rawQueryConditions = this.state.deviceQueryConditions.filter(
                        (condition) => {
                            // remove conditions that are new (have not been edited)
                            return !this.conditionIsNew(condition);
                        }
                    );
                    this.props.setActiveDeviceQueryConditions(
                        rawQueryConditions.map((condition) => {
                            return toDeviceConditionModel(condition);
                        })
                    );
                    this.props.fetchDevices();
                    resolve();
                });
            } catch (error) {
                reject(error);
            }
        });
    };

    apply = (event) => {
        this.props.logEvent(toDiagnosticsModel("CreateDeviceQuery_Create", {}));
        event.preventDefault();
        this.queryDevices()
            .then(() => {
                this.setState({ error: undefined, isPending: false });
            })
            .catch((error) => {
                this.setState({ error: error, isPending: false });
            });
    };

    addCondition = () => {
        this.props.logEvent(
            toDiagnosticsModel("CreateDeviceQuery_AddCondition", {})
        );
        return this.conditionsLink.set([
            ...this.conditionsLink.value,
            newCondition(),
        ]);
    };

    deleteCondition = (index) => () => {
        this.props.logEvent(
            toDiagnosticsModel("CreateDeviceQuery_RemoveCondition", {})
        );
        return this.conditionsLink.set(
            this.conditionsLink.value.filter((_, idx) => index !== idx)
        );
    };

    resetFlyoutAndDevices = () => {
        return new Promise((resolve, reject) => {
            const resetConditions = [newCondition()];
            try {
                this.setState(
                    {
                        deviceQueryConditions: resetConditions,
                        error: undefined,
                        isPending: true,
                    },
                    () => {
                        this.props.setActiveDeviceQueryConditions([]);
                        this.props.fetchDevices(); // reload the devices grid with a blank query
                        this.render();
                        resolve();
                    }
                );
            } catch (error) {
                reject(error);
            }
        });
    };

    onReset = () => {
        this.props.logEvent(toDiagnosticsModel("CreateDeviceQuery_Reset", {}));
        this.resetFlyoutAndDevices()
            .then(() => {
                this.setState({ error: undefined, isPending: false });
            })
            .catch((error) => {
                this.setState({ error: error, isPending: false });
            });
    };

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
                        );
                    let error =
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

        return (
            <form onSubmit={this.apply}>
                <div className="manage-filters-container">
                    <Grid>
                        {conditionLinks.length > 0 && (
                            <Row>
                                <Cell className="col-1"></Cell>
                                <Cell className="col-1"></Cell>
                                <Cell className="col-3">Field</Cell>
                                <Cell className="col-3">Operator</Cell>
                                <Cell className="col-2">Value</Cell>
                                <Cell className="col-2">Type</Cell>
                            </Row>
                        )}
                        {conditionLinks.map((condition, idx) => (
                            <Row
                                key={this.state.deviceQueryConditions[idx].key}
                                // className="deviceExplorer-conditions"
                            >
                                <Cell className="col-1">
                                    <Btn
                                        className="btn-icon"
                                        svg={svgs.plus}
                                        onClick={this.addCondition}
                                    />
                                </Cell>
                                <Cell className="col-1">
                                    <Btn
                                        className="btn-icon"
                                        icon="cancel"
                                        onClick={this.deleteCondition(idx)}
                                    />
                                </Cell>
                                <Cell className="col-3">
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
                                        options={this.state.filterOptions}
                                        link={condition.field}
                                    />
                                </Cell>
                                <Cell className="col-3">
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
                                </Cell>
                                <Cell className="col-2">
                                    <FormControl
                                        type="text"
                                        placeholder={t(
                                            "deviceQueryConditions.valuePlaceholder"
                                        )}
                                        link={condition.value}
                                    />
                                </Cell>
                                <Cell className="col-2">
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
                                </Cell>
                            </Row>
                        ))}
                    </Grid>
                    <Btn
                        className="add-btn"
                        svg={svgs.plus}
                        onClick={this.addCondition}
                    >
                        Add a condition
                    </Btn>
                    <BtnToolbar>
                        <Btn
                            primary
                            disabled={
                                !this.formIsValid() ||
                                conditionHasErrors ||
                                this.state.isPending
                            }
                            type="submit"
                        >
                            Query
                        </Btn>
                        <Btn
                            disabled={this.state.isPending}
                            svg={svgs.cancelX}
                            onClick={this.onReset}
                        >
                            Reset
                        </Btn>
                    </BtnToolbar>
                    {this.state.error && (
                        <AjaxError t={t} error={this.state.error} />
                    )}
                </div>
            </form>
        );
    }
}
