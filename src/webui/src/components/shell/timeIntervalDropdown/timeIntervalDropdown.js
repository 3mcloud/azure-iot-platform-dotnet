// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import { SelectInput } from "@microsoft/azure-iot-ux-fluent-controls/lib/components/Input/SelectInput";

import { isFunc } from "utilities";
import { toDiagnosticsModel } from "services/models";

import "./timeIntervalDropdown.scss";

const optionValues = [
    { value: "PT15M" },
    { value: "PT1H" },
    { value: "P1D" },
    { value: "P7D" },
    { value: "P1M" },
];

let currentValue = "PT1H";

export class TimeIntervalDropdown extends Component {
    onChange = (propOnChange) => (value) => {
        this.props.logEvent(toDiagnosticsModel("TimeFilter_Select", {}));
        if (isFunc(propOnChange)) {
            currentValue = value;
            propOnChange(value);
        }
    };

    static getTimeIntervalDropdownValue() {
        return currentValue;
    }

    render() {
        const options = optionValues.map(({ value }) => ({
                label: this.props.t(`timeInterval.${value}`),
                value,
            })),
            className = this.props.className || "time-interval-dropdown";

        return (
            <SelectInput
                name="time-interval-dropdown"
                className={className}
                attr={{
                    select: {
                        className: "time-interval-dropdown-select",
                        "aria-label": this.props.t("timeInterval.ariaLabel"),
                    },
                    chevron: {
                        className: "time-interval-dropdown-chevron",
                    },
                }}
                options={options}
                value={this.props.value}
                onChange={this.onChange(this.props.onChange)}
            />
        );
    }
}
