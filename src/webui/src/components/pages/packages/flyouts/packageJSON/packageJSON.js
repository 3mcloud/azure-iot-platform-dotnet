// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import { LinkedComponent } from "utilities";
import { Flyout, FormControl } from "components/shared";

import "../packageNew/packageNew.scss";

export class PackageJSON extends LinkedComponent {
    constructor(props) {
        super(props);
        var jsonData = JSON.parse(this.props.packageJson);
        this.state = {
            packageJson: {
                jsObject: { jsonData },
            },
            expandedValue: "no",
        };
        this.handleDoubleClickItem = this.handleDoubleClickItem.bind(this);
    }
    onFlyoutClose = (eventName) => {
        this.props.onClose();
    };

    componentWillReceiveProps(nextProps) {
        if (nextProps.packageJson !== this.props.packageJson) {
            var jsonData = JSON.parse(nextProps.packageJson);
            this.state = {
                packageJson: {
                    jsObject: { jsonData },
                },
            };
        }
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
        const { t, theme } = this.props;
        this.packageJsonLink = this.linkTo("packageJson");

        return (
            <div onDoubleClick={this.handleDoubleClickItem}>
                <Flyout
                    header={t("packages.flyouts.packageJson.title")}
                    t={t}
                    onClose={() => this.onFlyoutClose("PackageJSON_CloseClick")}
                    expanded={this.state.expandedValue}
                >
                    <div className="new-package-content">
                        <form className="new-package-form">
                            <FormControl
                                link={this.packageJsonLink}
                                type="jsoninput"
                                height="100%"
                                theme={theme}
                            />
                        </form>
                    </div>
                </Flyout>
            </div>
        );
    }
}
