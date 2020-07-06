import React from "react";
import { Observable } from "rxjs";
import "../manageDeviceGroups.scss";
import { compareByProperty } from "utilities";
import { Btn, BtnToolbar } from "components/shared";
import { ConfigService } from "services";
import { toUpdateDeviceGroupRequestModel } from "services/models";

export class DeviceGroups extends React.Component {
    // TODO: Remove constructor when args are passed in
    constructor(props) {
        super(props);

        this.state = {
            deviceGroups: this.clone(this.props.deviceGroups),
        };
    }

    onPinnedChanged = (idx) => {
        const deviceGroups = this.state.deviceGroups;
        let pinChangeGroup = deviceGroups.splice(idx, 1)[0];
        pinChangeGroup.isPinned = !pinChangeGroup.isPinned;
        const pinnedItems = deviceGroups.filter(
            (item) => item.isPinned === true
        );

        deviceGroups.splice(pinnedItems.length, 0, pinChangeGroup); // insert at end of pinned group
        deviceGroups.forEach((group, index) => {
            group.sortOrder = index;
        });
        this.setState({ deviceGroups: deviceGroups });

        // updateDeviceGroupsReducer
    };

    onDragStart = (e, index) => {
        this.draggedItem = this.state.deviceGroups[index];
        e.dataTransfer.effectAllowed = "move";
    };

    onDragOver = (index) => {
        const draggedOverItem = this.state.deviceGroups[index];

        if (this.draggedItem === draggedOverItem) {
            return;
        }
        let items = this.state.deviceGroups.filter(
            (item) => item !== this.draggedItem
        );
        items.splice(index, 0, this.draggedItem);

        let previousIdx = index > 0 ? index - 1 : 0;
        for (var i = 0; i < items.length; i++) {
            items[i].sortOrder = i;
            if (i === index) {
                items[i].isPinned = items[previousIdx].isPinned;
            }
        }

        this.setState({ deviceGroups: items });
    };

    onDragEnd = () => {
        this.draggedItem = null;
    };

    apply = (event) => {
        event.preventDefault();
        this.subscription = Observable.from(this.state.deviceGroups)
            .flatMap((devicecGroup) =>
                ConfigService.updateDeviceGroup(
                    devicecGroup.id,
                    toUpdateDeviceGroupRequestModel(devicecGroup)
                ).map(() => devicecGroup)
            )
            .subscribe(
                (deviceGroup) => {
                    this.props.insertDeviceGroups([deviceGroup]);
                },
                (error) => {}
            );
    };

    reset = () => {
        this.setState({ deviceGroups: this.clone(this.props.deviceGroups) });
    };

    clone = (objectArray) => {
        var cloneArray = [];
        objectArray.forEach((item) => {
            cloneArray.push({ ...item });
        });
        return cloneArray;
    };

    render() {
        // TODO: add deviceGroups to this.props
        const { t, onEditDeviceGroup } = this.props;
        // TODO: remove
        const { deviceGroups } = this.state;

        return (
            <div>
                <form onSubmit={this.apply}>
                    <div className="device-group">
                        <div className="group-title">
                            {" "}
                            {t("deviceGroupsFlyout.deviceGroupName")}
                        </div>
                        <div className="list">
                            {deviceGroups
                                .sort(compareByProperty("sortOrder", true))
                                .map((deviceGroup, idx) => (
                                    <div
                                        className="item"
                                        key={idx}
                                        data-index={idx}
                                        draggable
                                        onDragStart={(e) =>
                                            this.onDragStart(e, idx)
                                        }
                                        onDragOver={() => this.onDragOver(idx)}
                                        onDragEnd={this.onDragEnd}
                                    >
                                        {deviceGroup.isPinned ? (
                                            <img
                                                className="pinned"
                                                src={require("./pushpin-closed.png")}
                                                onClick={() =>
                                                    this.onPinnedChanged(idx)
                                                }
                                                alt={t(
                                                    "deviceGroupsFlyout.pinned"
                                                )}
                                            />
                                        ) : (
                                            <img
                                                className="unpinned"
                                                src={require("./pushpin-open.png")}
                                                onClick={() =>
                                                    this.onPinnedChanged(idx)
                                                }
                                                alt={t(
                                                    "deviceGroupsFlyout.unpinned"
                                                )}
                                            />
                                        )}
                                        <div
                                            className="title"
                                            key={idx}
                                            onClick={onEditDeviceGroup(
                                                deviceGroup
                                            )}
                                        >
                                            {deviceGroup.displayName}
                                        </div>
                                    </div>
                                ))}
                        </div>
                    </div>
                    <BtnToolbar>
                        <Btn primary={true} type="submit">
                            {t("deviceGroupsFlyout.save")}
                        </Btn>
                        <Btn onClick={() => this.reset()}>
                            {t("deviceGroupsFlyout.reset")}
                        </Btn>
                    </BtnToolbar>
                </form>
            </div>
        );
    }
}

export default DeviceGroups;
