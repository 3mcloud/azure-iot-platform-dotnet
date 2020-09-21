// Copyright (c) Microsoft. All rights reserved.
import React, { Component } from "react";
import { joinClasses } from "utilities";
import { Btn } from "components/shared";
import { Dropdown } from "@microsoft/azure-iot-ux-fluent-controls/lib/components/Dropdown";

import "./contextMenu.scss";

/**
 * ContextMenuAgile shows the menu buttons in dropdown when these are crowded in menu.
 *
 * @param props Control properties are priorityChildren, farChildren.
 * prirityChildren can be used to render elements in left side of menu, if elements are crowded, crowded elements are rendered in a more'...' dropdown.
 * farChildren can be used to render elements in right side of menu.
 *
 * Note: These props properties expect data in form of array and currently dropdown elements supports only buttons.
 */
export class ContextMenuAgile extends Component {
    constructor(props) {
        super(props);
        this.onResize = this.onResize.bind(this);
        this.toggleDropdown = this.toggleDropdown.bind(this);
        this.dropdownOpen = this.dropdownOpen.bind(this);
        this.dropdownClose = this.dropdownClose.bind(this);

        this.state = {
            visible: false,
            measureChildWidths: true,
            measureContainer: true,
            childrenWidths: [],
            initialMoreWidth: 0,
            containerWidth: 0,
        };
        this.container = React.createRef();
        this.priorityMenu = React.createRef();
        this.extraMenu = React.createRef();
        this.farMenu = React.createRef();

        this.resizeObserver = new ResizeObserver(() => {
            this.onResize();
        });
    }

    componentDidMount() {
        const outerWidth = this.container.current
            ? this.container.current.getBoundingClientRect().width
            : 0;
        const farMenuWidth = this.farMenu.current
            ? this.farMenu.current.getBoundingClientRect().width
            : 0;
        const containerWidth = outerWidth - farMenuWidth;

        this.setState({
            childrenWidths: this.priorityMenu.current
                ? Array.from(this.priorityMenu.current.children).map(
                      (item) => item.getBoundingClientRect().width
                  )
                : [],
            initialMoreWidth: this.extraMenu.current
                ? this.extraMenu.current.getBoundingClientRect().width
                : 0,
            containerWidth: containerWidth,
        });

        if (this.container.current) {
            this.resizeObserver.observe(this.container.current);
        }
    }

    shouldComponentUpdate(nextProps, nextState) {
        let state = undefined;

        if (
            nextProps.priorityChildren.length !==
            this.props.priorityChildren.length
        ) {
            state = {
                ...state,
                measureChildWidths: true,
            };
        }

        if (state) {
            this.setState(state);
        }

        // Update normally
        return true;
    }

    componentDidUpdate(prevProps, prevState) {
        let nextState = undefined;

        if (this.state.measureContainer) {
            const outerWidth = this.container.current
                ? this.container.current.getBoundingClientRect().width
                : 0;
            const farMenuWidth = this.farMenu.current
                ? this.farMenu.current.getBoundingClientRect().width
                : 0;
            const containerWidth = outerWidth - farMenuWidth;

            nextState = {
                containerWidth: containerWidth,
                measureContainer: false,
            };
        }

        if (this.state.measureChildWidths) {
            const widths = this.priorityMenu.current
                ? Array.from(this.priorityMenu.current.children).map(
                      (item) => item.getBoundingClientRect().width
                  )
                : [];
            nextState = {
                ...nextState,
                childrenWidths: widths,
                measureChildWidths: false,
            };
        }

        if (nextState) {
            this.setState(nextState);
        }
    }

    componentWillUnmount() {
        if (this.resizeObserver) {
            this.resizeObserver.disconnect();
        }
    }

    allowedMenuElementsCount(
        elementsArray,
        containerWidth,
        initialWidth,
        minElementsInMenu
    ) {
        let total = initialWidth * 1.5;

        for (let i = 0; i < elementsArray.length; i++) {
            if (total + elementsArray[i] > containerWidth) {
                return i < minElementsInMenu ? minElementsInMenu : i;
            } else {
                total += elementsArray[i];
            }
        }

        return elementsArray.length;
    }

    updateMenu(priorityChildren) {
        const {
            measureChildWidths,
            childrenWidths,
            containerWidth,
            initialMoreWidth,
        } = this.state;

        if (measureChildWidths) {
            return [priorityChildren, [null]];
        }

        let moreMenuWidth = this.extraMenu.current
            ? this.extraMenu.current.getBoundingClientRect().width
            : 0;

        if (moreMenuWidth) {
            moreMenuWidth = initialMoreWidth;
        }

        const priorityMenuLength = this.allowedMenuElementsCount(
            childrenWidths,
            containerWidth,
            moreMenuWidth,
            1
        );
        const children = priorityChildren;
        const priorityChildrenArray = children.slice(0, priorityMenuLength);
        const moreChildrenArray =
            priorityChildrenArray.length !== children.length
                ? children.slice(priorityMenuLength, children.length)
                : [];

        return [priorityChildrenArray, moreChildrenArray];
    }

    toggleDropdown(event) {
        event.preventDefault();
        this.setState((prevState) => ({
            visible: !prevState.visible,
        }));
    }

    dropdownOpen(event) {
        event.preventDefault();
        this.setState({
            visible: true,
        });
    }

    dropdownClose(event) {
        event.preventDefault();
        this.setState({
            visible: false,
        });
    }

    onResize() {
        if (this.container && this.container.current) {
            this.setState({ measureContainer: true });
        }
    }

    render() {
        const { visible } = this.state;
        const { priorityChildren, farChildren } = this.props;
        const [priorityChildrenArray, extraChildrenArray] = this.updateMenu(
            priorityChildren
        );
        return (
            <div
                className={joinClasses("context-menu-container")}
                ref={this.container}
            >
                {priorityChildrenArray && (
                    <div
                        className={joinClasses(
                            "context-menu-align-container",
                            "left"
                        )}
                    >
                        <div
                            ref={this.priorityMenu}
                            className="context-menu-align-item"
                        >
                            {priorityChildrenArray.length > 0 &&
                                priorityChildrenArray
                                    .filter((x) => x)
                                    .map((item, i) => (
                                        <div
                                            className={`context-menu-align-item item-${i}`}
                                            key={`item-${i}`}
                                        >
                                            {item}
                                        </div>
                                    ))}
                        </div>
                        {extraChildrenArray && extraChildrenArray.length > 0 && (
                            <div
                                ref={this.extraMenu}
                                className="context-menu-align-item"
                            >
                                <Dropdown
                                    dropdown={extraChildrenArray
                                        .filter((x) => x)
                                        .map((item, i) => (
                                            <div
                                                className={`extraMenuDropdown-option dropdown-item-${i}`}
                                                key={`dropdown-item-${i}`}
                                            >
                                                {item}
                                            </div>
                                        ))}
                                    position={2}
                                    align={3}
                                    visible={visible}
                                    containerWidth={false}
                                    onMouseEnter={this.dropdownOpen}
                                    onMouseLeave={this.dropdownClose}
                                    onOuterEvent={this.dropdownClose}
                                    attr={{
                                        dropdown: {
                                            className: "extraMenuDropdown",
                                            onClick: this.dropdownClose,
                                        },
                                    }}
                                >
                                    <Btn
                                        icon="more"
                                        className="dropdown-icon"
                                        onClick={this.toggleDropdown}
                                    />
                                </Dropdown>
                            </div>
                        )}
                    </div>
                )}
                {farChildren && (
                    <div
                        className={joinClasses(
                            "context-menu-align-container",
                            "right"
                        )}
                        ref={this.farMenu}
                    >
                        {farChildren.length > 0 &&
                            farChildren.map((item, i) => (
                                <div
                                    className={`context-menu-align-item far-item-${i}`}
                                    key={`far-item-${i}`}
                                >
                                    {item}
                                </div>
                            ))}
                    </div>
                )}
            </div>
        );
    }
}
