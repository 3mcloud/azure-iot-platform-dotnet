// Copyright (c) Microsoft. All rights reserved.

import React from "react";
import { LinkedComponent, svgs, copyToClipboard } from "utilities";
import {
    Btn,
    Flyout,
    FormControl,
    PropertyGrid as Grid,
    PropertyRow as Row,
    PropertyCell as Cell,
} from "components/shared";
import { Svg } from "components/shared/svg/svg";

import "../packageNew/packageNew.scss";
import "./packageJSON.scss";

export class PackageJSON extends LinkedComponent {
    constructor(props) {
        super(props);
        console.log(props);
        var jsonData = JSON.parse(this.props.packageJson);
        this.state = {
            packageJson: {
                jsObject: { jsonData },
            },
            expandedValue: false,
        };
        this.expandFlyout = this.expandFlyout.bind(this);
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
        const { t, theme, flyoutLink, packageId } = this.props;
        this.packageJsonLink = this.linkTo("packageJson");

        return (
            <Flyout
                header={t("packages.flyouts.packageJson.title")}
                t={t}
                onClose={() => this.onFlyoutClose("PackageJSON_CloseClick")}
                expanded={this.state.expandedValue}
                onExpand={() => {
                    this.expandFlyout();
                }}
                flyoutLink={flyoutLink}
            >
                <div>
                    <div className="pcs-renderer-cell highlight"><span><h4>Package Id</h4></span>
                        <span>
                            <Btn
                                className={"copy-icon"}
                                svg={svgs.copy}
                                onClick={() => copyToClipboard(packageId)}
                            ></Btn>
                        </span>
                    </div>
                    <div>
                        {packageId}
                    </div>
                </div>
                <br></br>
                <div>
                    <div><h4>Package JSON</h4></div>
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
                </div>
            </Flyout>
        );
    }
}
