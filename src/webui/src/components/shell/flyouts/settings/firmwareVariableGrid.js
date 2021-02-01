/* eslint-disable jsx-a11y/anchor-is-valid */
import React from "react";
import {
    PropertyGrid as Grid,
    PropertyRow as Row,
    PropertyCell as Cell,
} from "components/shared";

const key = "settingsFlyout.firmware.variables";

class FirmwareVariableGrid extends React.Component {
    render() {
        const { t } = this.props,
            variables = {
                version: [""],
                blobData: ["FileUri", "CheckSum"],
                packageFile: [
                    "name",
                    "lastModified",
                    "lastModifiedDate",
                    "size",
                    "type",
                ],
                deployment: ["id"],
                uniqueId:[""],
            },
            variableRows = [];

        Object.entries(variables).forEach((parent) => {
            parent[1].forEach((child) => {
                variableRows.push(
                    <Row>
                        <Cell>{parent[0]}</Cell>
                        <Cell>{child}</Cell>
                        <Cell>
                            {t(
                                `${key}.descriptions.${parent[0]}${
                                    child ? "." + child : ""
                                }`
                            )}
                        </Cell>
                    </Row>
                );
            });
        });

        return (
            <Grid>
                <Row>
                    <Cell>{t(`${key}.grid.parent`)}</Cell>
                    <Cell>{t(`${key}.grid.child`)}</Cell>
                    <Cell>{t(`${key}.grid.description`)}</Cell>
                </Row>
                {variableRows}
            </Grid>
        );
    }
}

export default FirmwareVariableGrid;
