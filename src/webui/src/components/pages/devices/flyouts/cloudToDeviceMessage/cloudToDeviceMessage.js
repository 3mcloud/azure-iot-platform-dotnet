// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import { Observable } from "rxjs";

import { LinkedComponent } from "utilities";
import { IoTHubManagerService } from "services";
import { svgs } from "utilities";
import { permissions } from "services/models";
import {
    FormGroup,
    FormControl,
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

import "./cloudToDeviceMessage.scss";

export class CloudToDeviceMessage extends LinkedComponent {
    constructor(props) {
        super(props);
        this.state = {
            physicalDevices: [],
            containsSimulatedDevices: false,
            confirmStatus: true,
            isPending: false,
            error: undefined,
            successCount: 0,
            changesApplied: false,
            jsonPayload: {
                jsObject: {
                    message: "Message to send to device",
                },
            },
            expandedValue: "no",
        };
        this.jsonPayloadLink = this.linkTo("jsonPayload").check(
            (jsonPayloadObject) => !jsonPayloadObject.error,
            () => this.props.t("devices.flyouts.c2dMessage.validation.invalid")
        );
        this.handleDoubleClickItem = this.handleDoubleClickItem.bind(this);
    }

    componentDidMount() {
        if (this.props.devices) {
            this.populateDevicesState(this.props.devices);
        }
    }

    componentWillReceiveProps(nextProps) {
        if (
            nextProps.devices &&
            (this.props.devices || []).length !== nextProps.devices.length
        ) {
            this.populateDevicesState(nextProps.devices);
        }
    }

    componentWillUnmount() {
        if (this.subscription) {
            this.subscription.unsubscribe();
        }
    }

    populateDevicesState = (devices = []) => {
        const physicalDevices = devices.filter(
            ({ isSimulated }) => !isSimulated
        );
        this.setState({
            physicalDevices,
            containsSimulatedDevices: physicalDevices.length !== devices.length,
        });
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

    sendCloudToDeviceMessage = (event) => {
        event.preventDefault();
        this.setState({ isPending: true, error: null });

        this.subscription = Observable.from(this.state.physicalDevices)
            .flatMap(({ id }) =>
                IoTHubManagerService.sendCloudToDeviceMessages(
                    id,
                    JSON.stringify(this.state.jsonPayload.jsObject)
                ).map(() => id)
            )
            .subscribe(
                (sentDeviceId) => {
                    this.setState({
                        successCount: this.state.successCount + 1,
                    });
                },
                (error) =>
                    this.setState({
                        error,
                        isPending: false,
                        changesApplied: true,
                    }),
                () =>
                    this.setState({
                        isPending: false,
                        changesApplied: true,
                        confirmStatus: false,
                    })
            );
    };

    getSummaryMessage() {
        const { t } = this.props,
            { isPending, changesApplied } = this.state;

        if (isPending) {
            return t("devices.flyouts.c2dMessage.pending");
        } else if (changesApplied) {
            return t("devices.flyouts.c2dMessage.applySuccess");
        }
        return t("devices.flyouts.c2dMessage.affected");
    }

    handleDoubleClickItem() {
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
        const { t, onClose, theme } = this.props,
            {
                physicalDevices,
                containsSimulatedDevices,
                isPending,
                error,
                successCount,
                changesApplied,
            } = this.state,
            summaryCount = changesApplied
                ? successCount
                : physicalDevices.length,
            completedSuccessfully = changesApplied && !error,
            summaryMessage = this.getSummaryMessage();

        return (
            <div onDoubleClick={this.handleDoubleClickItem}>
                <Flyout
                    header={t("devices.flyouts.c2dMessage.title")}
                    t={t}
                    onClose={onClose}
                    expanded={this.state.expandedValue}
                >
                    <Protected permission={permissions.deleteDevices}>
                        <form
                            className="device-c2dMessage-container"
                            onSubmit={this.sendCloudToDeviceMessage}
                        >
                            <div className="device-c2dMessage-header">
                                {t("devices.flyouts.c2dMessage.header")}
                            </div>
                            <div className="device-c2dMessage-descr">
                                {t("devices.flyouts.c2dMessage.description")}
                            </div>
                            <FormGroup>
                                <br />
                                <div className="help-message">
                                    {t(
                                        "devices.flyouts.c2dMessage.jsonPayloadMessage"
                                    )}
                                </div>
                                <FormControl
                                    link={this.jsonPayloadLink}
                                    type="jsoninput"
                                    height="200px"
                                    theme={theme}
                                />
                            </FormGroup>
                            {containsSimulatedDevices && (
                                <div className="simulated-device-selected">
                                    <Svg
                                        path={svgs.infoBubble}
                                        className="info-icon"
                                    />
                                    {t(
                                        "devices.flyouts.c2dMessage.simulatedNotSupported"
                                    )}
                                </div>
                            )}

                            <SummarySection>
                                <SectionHeader>
                                    {t(
                                        "devices.flyouts.c2dMessage.summaryHeader"
                                    )}
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
                                    className="device-c2dMessage-error"
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
                                            physicalDevices.length === 0
                                        }
                                        type="submit"
                                    >
                                        {t("devices.flyouts.c2dMessage.apply")}
                                    </Btn>
                                    <Btn svg={svgs.cancelX} onClick={onClose}>
                                        {t("devices.flyouts.c2dMessage.cancel")}
                                    </Btn>
                                </BtnToolbar>
                            )}
                            {!!changesApplied && (
                                <BtnToolbar>
                                    <Btn svg={svgs.cancelX} onClick={onClose}>
                                        {t("devices.flyouts.c2dMessage.close")}
                                    </Btn>
                                </BtnToolbar>
                            )}
                        </form>
                    </Protected>
                </Flyout>
            </div>
        );
    }
}
