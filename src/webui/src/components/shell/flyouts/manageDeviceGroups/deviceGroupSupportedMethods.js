// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import { Observable } from "rxjs";
import update from "immutability-helper";

import { LinkedComponent } from "utilities";
import { svgs, Validator } from "utilities";
import {
    AjaxError,
    Btn,
    ComponentArray,
    ErrorMsg,
    FormControl,
    PropertyGrid as Grid,
    PropertyGridBody as GridBody,
    PropertyGridHeader as GridHeader,
    PropertyRow as Row,
    PropertyCell as Cell,
} from "components/shared";

import Flyout from "components/shared/flyout";
const Section = Flyout.Section;
update.extend("$autoArray", (val, obj) => update(obj || [], val));

const initialState = {
        isPending: false,
        error: undefined,
        successCount: 0,
        changesApplied: false,
        jobName: undefined,
        jobId: undefined,
        methods: [],
        deletedKeys: [],
    },
    newMethod = () => ({
        method: "",
    });

export class DeviceGroupSupportedMethods extends LinkedComponent {
    constructor(props) {
        super(props);
        this.state = initialState;

        this.methodsLink = this.linkTo("methods", props.onMethodsChange);
    }

    componentDidMount() {
        if (this.props.methods) {
            this.populateState(this.props.methods);
        }
    }

    componentWillReceiveProps(nextProps) {
        if (
            nextProps.methods &&
            (this.state.methods || []).length !== nextProps.methods.length
        ) {
            this.populateState(nextProps.methods);
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

    populateState(methods) {
        if (this.populateStateSubscription) {
            this.populateStateSubscription.unsubscribe();
        }
        this.populateStateSubscription = Observable.of(methods).subscribe(
            (methods) =>
                this.setState(
                    update(this.state, {
                        methods: { $set: methods },
                    })
                )
        );
    }

    formIsValid() {
        return [this.methodsLink].every((link) => !link.error);
    }

    getSummaryMessage() {
        const { t } = this.props,
            { isPending, changesApplied } = this.state;

        if (isPending) {
            return t("deviceGroupsFlyout.pending");
        } else if (changesApplied) {
            return t("deviceGroupsFlyout.applySuccess");
        }
        return t("deviceGroupsFlyout.affected");
    }

    addMethod = () =>
        this.methodsLink.set([...this.methodsLink.value, newMethod()]);

    deleteMethod = (index) => (evt) => {
        var methodMinusIndex = this.methodsLink.value.filter(
            (_, idx) => index !== idx
        );
        this.setState(
            update(this.state, {
                methods: {
                    $set:
                        methodMinusIndex.length > 0
                            ? methodMinusIndex
                            : [newMethod()],
                },
                deletedKeys: { $push: [this.methodsLink.value[index].key] },
            }),
            function () {
                if (this.props.onMethodsChange) {
                    this.props.onMethodsChange(this.methodsLink.value);
                }
            }.bind(this)
        );
    };

    render() {
        this.getSummaryMessage();
        const { t, methods } = this.props,
            { error } = this.state,
            // Link these values in render because they need to update based on component state
            methodsLinks = this.methodsLink.getLinkedChildren((methodsLink) => {
                const method = methodsLink
                        .forkTo("method")
                        .check(
                            Validator.notEmpty,
                            this.props.t(
                                "deviceGroupsFlyout.supportedMethods.validation.required"
                            )
                        ),
                    edited = !!method.value,
                    error = (edited && method.error) || "";
                return { method, edited, error };
            });
        // editedformat = methodsLinks.filter(({ edited }) => edited);
        // formatHaveErrors = editedformat.some(({ error }) => !!error);

        return (
            <Section.Container>
                <Section.Header>
                    {t(
                        "deviceGroupsFlyout.supportedMethods.supportedMethodHeader"
                    )}
                </Section.Header>
                <Section.Content>
                    <Grid className="data-grid">
                        <GridHeader>
                            <Row>
                                <Cell className="col-3">
                                    {t(
                                        "deviceGroupsFlyout.supportedMethods.methodHeader"
                                    )}
                                </Cell>
                                <Cell className="col-1"></Cell>
                            </Row>
                        </GridHeader>
                        <GridBody>
                            {Object.keys(methods).length > 0 &&
                                methodsLinks.map(
                                    ({ method, edited, error }, idx) => (
                                        <ComponentArray key={idx}>
                                            <Row
                                                className={
                                                    error
                                                        ? "error-data-row"
                                                        : ""
                                                }
                                            >
                                                <Cell className="col-3">
                                                    <FormControl
                                                        type="text"
                                                        link={method}
                                                        errorState={!!error}
                                                    />
                                                </Cell>
                                                <Cell className="col-1">
                                                    <Btn
                                                        className="icon-only-btn"
                                                        svg={svgs.trash}
                                                        onClick={this.deleteMethod(
                                                            idx
                                                        )}
                                                    />
                                                </Cell>
                                            </Row>
                                            {error ? (
                                                <Row className="error-msg-row">
                                                    <ErrorMsg>{error}</ErrorMsg>
                                                </Row>
                                            ) : null}
                                        </ComponentArray>
                                    )
                                )}

                            <Row className="action-row">
                                <Btn svg={svgs.plus} onClick={this.addMethod}>
                                    {t(
                                        "deviceGroupsFlyout.supportedMethods.add"
                                    )}
                                </Btn>
                            </Row>
                        </GridBody>
                    </Grid>

                    {error && (
                        <AjaxError
                            className="device-jobs-error"
                            t={t}
                            error={error}
                        />
                    )}
                </Section.Content>
            </Section.Container>
        );
    }
}
