// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";

import { Btn, BtnToolbar, Modal } from "components/shared";
import { svgs } from "utilities";
import { toSinglePropertyDiagnosticsModel } from "services/models";

import "./confirmationModal.scss";

export class ConfirmationModal extends Component {
    genericCloseClick = (eventName) => {
        const { onCancel, logEvent } = this.props;
        logEvent(
            toSinglePropertyDiagnosticsModel(
                eventName,
                "ConfirmationModal_CloseClick"
            )
        );
        onCancel();
    };

    onConfirmation = (eventName) => {
        const { onOk, logEvent } = this.props;
        logEvent(
            toSinglePropertyDiagnosticsModel(
                eventName,
                "ConfirmationModal_OkClick"
            )
        );
        onOk();
    };

    render() {
        const { t, title, confirmationInfo } = this.props;

        return (
            <Modal
                onClose={() =>
                    this.genericCloseClick("ConfirmationModal_ModalClose")
                }
                className="confirmation-modal-container"
            >
                <div className="confirmation-header-container">
                    <div className="confirmation-title">{title}</div>
                </div>
                <div className="confirmation-info">{confirmationInfo}</div>
                <div className="confirmation-summary">
                    <BtnToolbar>
                        <Btn
                            svg={svgs.apply}
                            primary={true}
                            onClick={() =>
                                this.onConfirmation("ConfirmationModal_OkClick")
                            }
                        >
                            {t("modal.ok")}
                        </Btn>
                        <Btn
                            svg={svgs.cancelX}
                            onClick={() =>
                                this.genericCloseClick(
                                    "ConfirmationModal_CancelClick"
                                )
                            }
                        >
                            {t("modal.cancel")}
                        </Btn>
                    </BtnToolbar>
                </div>
            </Modal>
        );
    }
}
