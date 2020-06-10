// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import update from "immutability-helper";
import { Observable } from "rxjs";
import "tsiclient";
//import { PivotMenu } from "@microsoft/azure-iot-ux-fluent-controls/lib/components/Pivot";
import { AdvancedPivotMenu } from "./advancedPivotMenu.js";
import { toDiagnosticsModel } from "services/models";

import "./telemetryChart.scss";
var jstz = require("jstz");
var timezone = jstz.determine().name();

const maxDatums = 1000; // Max telemetry messages for the telemetry graph

// Extend the immutability helper to include object autovivification
update.extend("$auto", (val, obj) => update(obj || {}, val));

export const chartColors = [
    "#01B8AA",
    "#F2C80F",
    "#E81123",
    "#3599B8",
    "#33669A",
    "#26FFDE",
    "#E0E7EE",
    "#FDA954",
    "#FD625E",
    "#FF4EC2",
    "#FFEE91",
];
export const chartColorObjects = chartColors.map((color) => ({ color }));

/**
 *  A helper function containing the logic to convert a getTelemetry response
 *  object into the chart object structure.
 *
 * @param getCurrentTelemetry A function that returns an object of formatted telemetry messages
 * @param items An array of telemetry messages from the getTelemetry response object
 */
export const transformTelemetryResponse = (getCurrentTelemetry) => (items) =>
    Observable.from(items)
        .flatMap(({ data, deviceId, time }) =>
            Observable.from(Object.keys(data))
                .filter(
                    (metric) =>
                        metric.indexOf("Unit") < 0 && !isNaN(data[metric])
                )
                .map((metric) => ({
                    metric,
                    deviceId,
                    time,
                    data: data[metric],
                }))
        )
        .reduce(
            (acc, { metric, deviceId, time, data }) =>
                update(acc, {
                    [metric]: {
                        $auto: {
                            [deviceId]: {
                                $auto: {
                                    "": {
                                        $auto: {
                                            [time]: {
                                                $auto: {
                                                    val: { $set: data },
                                                },
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    },
                }),
            getCurrentTelemetry()
        )
        // Remove overflowing items
        .map((telemetry) => {
            Object.keys(telemetry).forEach((metric) => {
                Object.keys(telemetry[metric]).forEach((deviceId) => {
                    const datums = Object.keys(telemetry[metric][deviceId][""]);
                    if (datums.length > maxDatums) {
                        telemetry[metric][deviceId][""] = datums
                            .sort()
                            .slice(datums.length - maxDatums, datums.length)
                            .reduce(
                                (acc, time) => ({
                                    ...acc,
                                    [time]:
                                        telemetry[metric][deviceId][""][time],
                                }),
                                {}
                            );
                    }
                });
            });
            return telemetry;
        });

export class TelemetryChart extends Component {
    static telemetryChartCount = 0;

    constructor(props) {
        super(props);

        this.chartId = `telemetry-chart-container-${TelemetryChart.telemetryChartCount++}`;

        this.state = {
            telemetryKeys: [],
            telemetryKey: "",
            renderChart: true,
        };

        window.addEventListener("blur", this.handleWindowBlur);
        window.addEventListener("focus", this.handleWindowFocus);

        this.tsiClient = new window.TsiClient();
    }

    handleWindowBlur = () => this.setState({ renderChart: false });
    handleWindowFocus = () => this.setState({ renderChart: true });

    componentDidMount() {
        this.lineChart = new this.tsiClient.ux.LineChart(
            document.getElementById(this.chartId)
        );
    }

    componentWillUnmount() {
        window.removeEventListener("blur", this.handleWindowBlur);
        window.removeEventListener("focus", this.handleWindowFocus);
    }

    componentWillReceiveProps({ telemetry }) {
        const telemetryKeys = Object.keys(telemetry)
                .filter(
                    (key) =>
                        this.props.deviceGroup.telemetryFormat.length === 0 ||
                        this.props.deviceGroup.telemetryFormat.filter(
                            (format) => format.key === key
                        ).length > 0
                )
                .sort(),
            currentKey = this.state.telemetryKey;
        this.setState({
            telemetryKeys,
            telemetryKey:
                currentKey in telemetry ? currentKey : telemetryKeys[0],
        });
    }

    componentWillUpdate({ telemetry, theme }, { telemetryKey }) {
        let chartData = [];
        if (
            Object.keys(telemetry).length &&
            telemetryKey &&
            telemetry[telemetryKey]
        ) {
            chartData = Object.keys(telemetry[telemetryKey]).map(
                (deviceId) => ({
                    [deviceId]: telemetry[telemetryKey][deviceId],
                })
            );
        }
        const noAnimate = telemetryKey === this.state.telemetryKey; // will be false if there is no telemetry data
        // Set a timeout to allow the panel height to be calculated before updating the graph
        setTimeout(() => {
            if (
                this &&
                this.state &&
                this.lineChart &&
                this.state.renderChart
            ) {
                this.lineChart.render(
                    chartData,
                    {
                        // Chart options object: see https://github.com/microsoft/tsiclient/blob/master/docs/UX.md#chart-options
                        noAnimate: noAnimate, // If the telemetryKey changes, animate
                        theme: theme === "mmm" ? "light" : theme, // theme may only be light or dark, handle mmm theme setting
                        includeDots: true,
                        yAxisState: "shared", // Default to all values being on the same axis
                        grid: false,
                        legend: "compact",
                        offset: timezone,
                    },
                    this.props.colors
                );
            }
        }, 10);
    }

    setTelemetryKey = (telemetryKey) => () => {
        this.props.logEvent(
            toDiagnosticsModel("TelemetryChartFilter_Click", {})
        );
        this.setState({ telemetryKey });
    };

    render() {
        const { telemetry } = this.props,
            { telemetryKeys } = this.state,
            telemetryList = telemetryKeys.map((key) => {
                const count = Object.keys(telemetry[key]).length,
                    keyFormatMatch = this.props.deviceGroup.telemetryFormat.find(
                        (format) => format.key === key
                    );
                return {
                    label: `${
                        keyFormatMatch ? keyFormatMatch.displayName : key
                    } [${count}]`,
                    key,
                    onClick: this.setTelemetryKey(key),
                };
            });

        return (
            <div className="telemetry-chart-container">
                <AdvancedPivotMenu
                    className="options-container"
                    links={telemetryList}
                    active={this.state.telemetryKey}
                />
                <div className="chart-container" id={this.chartId} />
                <p>Displaying in local timezone: {timezone}</p>
            </div>
        );
    }
}
