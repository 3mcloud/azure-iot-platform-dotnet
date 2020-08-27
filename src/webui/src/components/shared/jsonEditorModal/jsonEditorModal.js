// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";

import { Btn, BtnToolbar, Modal, FormControl } from "components/shared";
import { svgs } from "utilities";
import { toSinglePropertyDiagnosticsModel } from "services/models";

import "./jsonEditorModal.scss";

export class JsonEditorModal extends Component {
    constructor(props) {
        super(props);

        this.state = {
            updatedJson: this.props.jsonData,
        };
    }

    genericCloseClick = (eventName) => {
        const { onClose, logEvent } = this.props;
        logEvent(
            toSinglePropertyDiagnosticsModel(
                eventName,
                "JsonEditorModal_CloseClick"
            )
        );
        onClose();
    };

    onJsonChange = (e) => {
        this.setState({ updatedJson: e });
    };

    render() {
        const { t, title, jsonData } = this.props;
        return (
            <Modal
                onClose={() =>
                    this.genericCloseClick("JsonEditorModal_ModalClose")
                }
                className="json-editor-modal-container"
            >
                <form>
                    <div className="json-editor-header-container">
                        <div className="json-editor-title">{title}</div>
                    </div>
                    <div className="json-editor-info">
                        <FormControl
                            link={jsonData}
                            type="jsoninput"
                            height="400px"
                            theme="mmm"
                            onChange={this.onJsonChange}
                        />
                    </div>
                    <div className="json-editor-summary">
                        <BtnToolbar>
                            <Btn
                                svg={svgs.cancelX}
                                onClick={() =>
                                    this.genericCloseClick(
                                        "JsonEditorModal_CloseClick"
                                    )
                                }
                            >
                                {t("modal.close")}
                            </Btn>
                        </BtnToolbar>
                    </div>
                </form>
            </Modal>
        );
    }
}
