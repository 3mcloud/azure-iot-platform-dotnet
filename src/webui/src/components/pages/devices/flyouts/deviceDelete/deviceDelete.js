// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import { from } from "rxjs";
import { Toggle } from "@microsoft/azure-iot-ux-fluent-controls/lib/components/Toggle";

import { IoTHubManagerService } from "services";
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
import { map, mergeMap } from "rxjs/operators";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./deviceDelete.module.scss"));

export class DeviceDelete extends Component {
    constructor(props) {
        super(props);
        this.state = {
            physicalDevices: [],
            containsSimulatedDevices: false,
            confirmStatus: false,
            isPending: false,
            error: undefined,
            successCount: 0,
            changesApplied: false,
            expandedValue: false,
        };
        this.expandFlyout = this.expandFlyout.bind(this);
    }

    componentDidMount() {
        if (this.props.devices) {
            this.populateDevicesState(this.props.devices);
        }
    }

    UNSAFE_componentWillReceiveProps(nextProps) {
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

    deleteDevices = (event) => {
        event.preventDefault();
        this.setState({ isPending: true, error: null });

        this.subscription = from(this.state.physicalDevices)
            .pipe(
                mergeMap(({ id }) =>
                    IoTHubManagerService.deleteDevice(id).pipe(map(() => id))
                )
            )
            .subscribe(
                (deletedDeviceId) => {
                    this.setState({
                        successCount: this.state.successCount + 1,
                    });
                    this.props.deleteDevices([deletedDeviceId]);
                },
                (error) =>
                    this.setState({
                        error,
                        isPending: false,
                        changesApplied: true,
                    }),
                () => {
                    this.setState({
                        isPending: false,
                        changesApplied: true,
                        confirmStatus: false,
                    });

                    if (this.state.successCount > 0) {
                        this.props.fetchDeviceStatistics();
                    }
                }
            );
    };

    getSummaryMessage() {
        const { t } = this.props,
            { isPending, changesApplied } = this.state;

        if (isPending) {
            return t("devices.flyouts.delete.pending");
        } else if (changesApplied) {
            return t("devices.flyouts.delete.applySuccess");
        }
        return t("devices.flyouts.delete.affected");
    }

    expandFlyout() {
        if (this.state.expandedValue) {
            this.setState({
                expandedValue: false,
            });
        } else {
            this.setState({
                expandedValue: true,
            });
        }
    }

    render() {
        const { t, onClose } = this.props,
            {
                physicalDevices,
                containsSimulatedDevices,
                confirmStatus,
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
            <Flyout
                header={t("devices.flyouts.delete.title")}
                t={t}
                onClose={onClose}
                expanded={this.state.expandedValue}
                onExpand={() => {
                    this.expandFlyout();
                }}
            >
                <Protected permission={permissions.deleteDevices}>
                    <form
                        className={css("device-delete-container")}
                        onSubmit={this.deleteDevices}
                    >
                        <div className={css("device-delete-header")}>
                            {t("devices.flyouts.delete.header")}
                        </div>
                        <div className={css("device-delete-descr")}>
                            {t("devices.flyouts.delete.description")}
                        </div>
                        <Toggle
                            name="device-flyouts-delete"
                            attr={{
                                button: {
                                    "aria-label": t(
                                        "devices.flyouts.delete.header"
                                    ),
                                },
                            }}
                            on={confirmStatus}
                            onChange={this.toggleConfirm}
                            onLabel={t("devices.flyouts.delete.confirmYes")}
                            offLabel={t("devices.flyouts.delete.confirmNo")}
                        />
                        {containsSimulatedDevices && (
                            <div className={css("simulated-device-selected")}>
                                <Svg
                                    src={svgs.infoBubble}
                                    className={css("info-icon")}
                                />
                                {t(
                                    "devices.flyouts.delete.simulatedNotSupported"
                                )}
                            </div>
                        )}

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
                                        className={css("summary-icon")}
                                        src={svgs.apply}
                                    />
                                )}
                            </SummaryBody>
                        </SummarySection>

                        {error && (
                            <AjaxError
                                className={css("device-delete-error")}
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
                                        physicalDevices.length === 0 ||
                                        !confirmStatus
                                    }
                                    type="submit"
                                >
                                    {t("devices.flyouts.delete.apply")}
                                </Btn>
                                <Btn svg={svgs.cancelX} onClick={onClose}>
                                    {t("devices.flyouts.delete.cancel")}
                                </Btn>
                            </BtnToolbar>
                        )}
                        {!!changesApplied && (
                            <BtnToolbar>
                                <Btn svg={svgs.cancelX} onClick={onClose}>
                                    {t("devices.flyouts.delete.close")}
                                </Btn>
                            </BtnToolbar>
                        )}
                    </form>
                </Protected>
            </Flyout>
        );
    }
}
