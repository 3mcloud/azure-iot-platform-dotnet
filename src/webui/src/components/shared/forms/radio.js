// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";
import PropTypes from "prop-types";

import { Svg } from "components/shared/svg/svg";
import { FormLabel } from "./formLabel";
import { isFunc, svgs } from "utilities";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./styles/radio.module.scss"));

let radioInputCnt = 0;

export class Radio extends Component {
    constructor(props) {
        super(props);
        this.formGroupId = `radioInputId${radioInputCnt++}`;
        this.radioInputElement = React.createRef();
    }

    onChange = (evt) => {
        const { onChange, link } = this.props;
        if (link && isFunc(link.onChange)) {
            link.onChange(evt);
        }
        if (isFunc(onChange)) {
            onChange(evt);
        }
    };

    // Needs to be a stateful component in order to access refs
    render() {
        const {
                className,
                children,
                id,
                disabled,
                link,
                formGroupId,
                ...rest
            } = this.props,
            valueOverrides = link
                ? {
                      checked: link.value === rest.value,
                      onChange: this.onChange,
                  }
                : {},
            radioProps = { ...rest, ...valueOverrides };
        let contentChildren = children;
        if (typeof contentChildren === "string") {
            contentChildren = <FormLabel>{contentChildren}</FormLabel>;
        }
        const childrenWithProps = React.Children.map(
            contentChildren,
            (child) => {
                if (React.isValidElement(child) && isFunc(child.type)) {
                    return React.cloneElement(child, {
                        formGroupId: this.formGroupId,
                        disabled:
                            disabled ||
                            (radioProps.checked === undefined
                                ? false
                                : !radioProps.checked),
                    });
                }
                return child;
            }
        );

        return (
            <div className={css("radio-container", className)}>
                <div className={css("radio-input-container")}>
                    <input
                        {...radioProps}
                        {...valueOverrides}
                        type="radio"
                        disabled={disabled}
                        id={id || this.formGroupId}
                        ref={this.radioInputElement}
                    />
                    <Svg
                        src={svgs.radioSelected}
                        className={css("radio-icon", { disabled: disabled })}
                        onClick={() => this.radioInputElement.current.click()}
                    />
                </div>
                <div className={css("input-contents")}>{childrenWithProps}</div>
            </div>
        );
    }
}
//className, children, id, checked, disabled
Radio.propTypes = {
    checked: PropTypes.bool,
    children: PropTypes.node,
    className: PropTypes.string,
    disabled: PropTypes.bool,
    id: PropTypes.string,
};
