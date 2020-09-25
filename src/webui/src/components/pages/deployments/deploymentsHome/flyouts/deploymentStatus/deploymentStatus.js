// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import { toDiagnosticsModel } from "services/models";
import { svgs, LinkedComponent, formatDate } from "utilities";
import {
    BtnToolbar,
    Btn,
    Flyout,
    Indicator,
    ComponentArray,
} from "components/shared";
import { Toggle } from "@microsoft/azure-iot-ux-fluent-controls/lib/components/Toggle";

import "./deploymentStatus.scss";
import { IoTHubManagerService } from "services";

export class DeploymentStatus extends LinkedComponent {
    constructor(props) {
        super(props);

        this.state = {
            selectedDeployment: {},
            gridData: [],
            isActive: false,
            haschanged: false,
            changesApplied: false,
            expandedValue: false,
        };
        this.activateOrInactivateDeployment = this.activateOrInactivateDeployment.bind(
            this
        );
        this.expandFlyout = this.expandFlyout.bind(this);
    }

    genericCloseClick = (eventName) => {
        const { onClose, logEvent } = this.props;
        logEvent(toDiagnosticsModel(eventName, {}));
        onClose();
    };

    componentDidMount() {
        if (this.props.selectedDeployment) {
            this.setState({
                isActive: this.props.selectedDeployment.isActive,
            });
        }
    }

    componentWillReceiveProps(nextprops) {
        this.setState({
            isActive: nextprops.selectedDeployment.isActive,
        });
    }

    onDeploymentStatusChange = (updatedStatus) => {
        setTimeout(() => {
            if (this.props.selectedDeployment.isActive !== updatedStatus) {
                this.setState({
                    isActive: updatedStatus,
                    haschanged: true,
                });
            }
        }, 10);
    };

    apply = (event) => {
        event.preventDefault();
        if (this.state.haschanged) {
            this.activateOrInactivateDeployment(
                this.props.selectedDeployment.id
            );
        }
    };

    activateOrInactivateDeployment(deploymentId) {
        this.setState({ changesApplied: true });
        if (this.state.isActive) {
            IoTHubManagerService.reactivateDeployment(
                deploymentId
            ).subscribe(() => this.postUpdatingDeployment());
        } else {
            IoTHubManagerService.deleteDeployment(
                deploymentId,
                false
            ).subscribe(() => this.postUpdatingDeployment());
        }
    }

    postUpdatingDeployment() {
        this.setState({ changesApplied: false });
        this.genericCloseClick("DeploymentStatus_CloseClick");
        this.props.fetchDeployments();
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
        const { t } = this.props;
        const { changesApplied } = this.state;
        return (
            <Flyout
                header={t("deployments.flyouts.status.title")}
                t={t}
                onClose={() =>
                    this.genericCloseClick("DeploymentStatus_CloseClick")
                }
                expanded={this.state.expandedValue}
                onExpand={() => {
                    this.expandFlyout();
                }}
            >
                <div className="new-deployment-content">
                    <form className="new-deployment-form" onSubmit={this.apply}>
                        <div>
                            {t(
                                "deployments.flyouts.status.deploymentLimitText"
                            )}
                        </div>
                        <br />
                        <h3>{this.props.selectedDeployment.name}</h3>
                        <br />
                        <Toggle
                            className="simulation-toggle-button"
                            name={t("this.props.selectedDeployment.name")}
                            attr={{
                                button: {
                                    "aria-label": t(
                                        "settingsFlyout.simulationToggle"
                                    ),
                                    type: "button",
                                },
                            }}
                            on={this.state.isActive}
                            onLabel={t("deployments.flyouts.status.active")}
                            offLabel={t("deployments.flyouts.status.inActive")}
                            onChange={this.onDeploymentStatusChange}
                        />
                        <br />
                        <br />
                        <div>
                            <h3>
                                {t(
                                    "deployments.flyouts.status.relatedDeployment.title"
                                )}
                            </h3>
                        </div>
                        <div>
                            {this.props.relatedDeployments.length === 0 && (
                                <div>
                                    {t(
                                        "deployments.flyouts.status.relatedDeployment.noRelatedDeploymentText"
                                    )}
                                </div>
                            )}
                            <ul>
                                {this.props.relatedDeployments.map(
                                    (deployment) => {
                                        return (
                                            <li key={deployment.id}>
                                                {deployment.name}
                                                &nbsp;&nbsp;&nbsp;&nbsp;
                                                {formatDate(
                                                    deployment.createdDateTimeUtc
                                                )}{" "}
                                                &nbsp;-&nbsp;
                                                {formatDate(
                                                    deployment.modifiedDate
                                                )}
                                                <br />
                                            </li>
                                        );
                                    }
                                )}
                            </ul>
                        </div>
                        {!!changesApplied && (
                            <ComponentArray>
                                <Indicator />
                                {t("deployments.flyouts.status.updating")}
                            </ComponentArray>
                        )}
                        <div>
                            <BtnToolbar>
                                <Btn
                                    primary={true}
                                    type="submit"
                                    disabled={!this.state.haschanged}
                                >
                                    {t("deployments.flyouts.status.apply")}
                                </Btn>
                                <Btn
                                    svg={svgs.cancelX}
                                    onClick={() =>
                                        this.genericCloseClick(
                                            "DeploymentStatus_CloseClick"
                                        )
                                    }
                                >
                                    {this.state.hasChanged
                                        ? t("deployments.flyouts.status.cancel")
                                        : t("deployments.flyouts.status.close")}
                                </Btn>
                            </BtnToolbar>
                        </div>
                    </form>
                </div>
            </Flyout>
        );
    }
}
