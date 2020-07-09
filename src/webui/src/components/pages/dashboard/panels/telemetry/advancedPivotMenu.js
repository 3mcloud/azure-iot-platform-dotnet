import React from "react";
import ReactDOM from "react-dom";
import { PivotMenu } from "@microsoft/azure-iot-ux-fluent-controls/lib/components/Pivot";
import "./advancedPivotMenu.scss";

export class AdvancedPivotMenu extends React.Component {
    constructor(props) {
        super(props);

        this.refs = React.createRef();
        this.state = {
            prevDisable: false,
            nextDisable: false,
            showSliderIcons: false,
        };
    }

    componentDidMount() {
        this.checkButtons(this.refs.offsetWidth, this.refs.scrollWidth);
    }

    componentWillReceiveProps(nextprops) {
        if (nextprops.links && nextprops.links.length > 5) {
            this.setState({ showSliderIcons: true });
        } else {
            this.setState({ showSliderIcons: false });
        }
    }

    checkButtons = (offsetWidthValue, scrollWidthValue) => {
        var target = ReactDOM.findDOMNode(this).querySelectorAll(
            "[class^=Pivot_pivot-menu]"
        );
        this.setState({
            prevDisable: this.refs.scrollLeft < 0 ? true : false,
            nextDisable:
                target[0].scrollLeft >= scrollWidthValue + 600 ? true : false,
        });
    };

    slideLeft = () => {
        const offsetWidthValue = this.refs.offsetWidth,
            scrollWidthValue = this.refs.scrollWidth;
        var target = ReactDOM.findDOMNode(this).querySelectorAll(
            "[class^=Pivot_pivot-menu]"
        );
        target[0].scrollLeft -= offsetWidthValue / 2;
        this.checkButtons(offsetWidthValue, scrollWidthValue);
    };

    slideRight = () => {
        const offsetWidthValue = this.refs.offsetWidth,
            scrollWidthValue = this.refs.scrollWidth;

        var target = ReactDOM.findDOMNode(this).querySelectorAll(
            "[class^=Pivot_pivot-menu]"
        );
        target[0].scrollLeft += offsetWidthValue / 2;
        this.checkButtons(offsetWidthValue, scrollWidthValue);
    };

    render() {
        const { showSliderIcons } = { ...this.state };
        return (
            <div
                className="slider-container"
                ref={(el) => {
                    this.refs = el;
                }}
            >
                {showSliderIcons && (
                    <div
                        className={`btnSlider btnSliderLeft ${
                            this.state.prevDisable ? "disableSlider" : ""
                        }`}
                        onClick={this.slideLeft.bind(this)}
                    >
                        {"<"}
                    </div>
                )}
                <div className="slider-wrapper">
                    <PivotMenu {...this.props} />
                </div>
                {showSliderIcons && (
                    <div
                        className={`btnSlider btnSliderRight ${
                            this.state.nextDisable ? "disableSlider" : ""
                        }`}
                        onClick={this.slideRight.bind(this)}
                    >
                        {">"}
                    </div>
                )}
            </div>
        );
    }
}
