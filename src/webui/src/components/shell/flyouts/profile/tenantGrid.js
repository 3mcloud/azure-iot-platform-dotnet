/* eslint-disable jsx-a11y/anchor-is-valid */
import React from "react";
import { svgs } from "utilities";
import { permissions } from "services/models";

import {
    Btn,
    PropertyRow as Row,
    PropertyCell as Cell,
    Protected,
    FormControl,
} from "components/shared";
class TenantGrid extends React.Component {
    constructor(props) {
        super(props);
        this.state = { tenantName: "", tenantId: "", isEdit: false };

        this.handleChange = this.handleChange.bind(this);
        this.handleSubmit = this.handleSubmit.bind(this);
        this.handleCancel = this.handleCancel.bind(this);
        this.onEdit = this.onEdit.bind(this);
    }
    onEdit(idx) {
        this.setState({
            isEdit: true,
            tenantName: this.props.tenants[idx].displayName,
            tenantId: this.props.tenants[idx].id,
        });
    }
    handleChange(event) {
        this.setState({ tenantName: event.target.value });
    }

    componentWillUnmount() {
        this.setState({ isEdit: false, tenantName: "", tenantId: "" });
    }

    handleCancel(event) {
        this.setState({ isEdit: false, tenantName: "", tenantId: "" });
    }

    handleSubmit(event) {
        this.setState({ isEdit: false, tenantName: "", tenantId: "" });
        this.props
            .updateTenant(this.state.tenantId, this.state.tenantName)
            .subscribe(
                (response) => {
                    if (response) {
                        this.props.fetchTenants();
                    } else {
                        alert(
                            "Error ocurred while trying to update Tenant Name . Please try again later."
                        );
                    }
                },
                (error) => {
                    alert("An error ocurred while trying to update tenants.");
                    this.props.fetchTenants();
                }
            );
        event.preventDefault();
    }

    render() {
        return this.props.tenants.map((tenant, idx) => (
            <Row key={tenant.id}>
                {!this.state.isEdit && (
                    <Cell>
                        {tenant.displayName === this.props.currentTenant ? (
                            tenant.displayName
                        ) : (
                            <a
                                onClick={() =>
                                    this.props.switchTenant(tenant.id)
                                }
                                href="#"
                            >
                                {tenant.displayName}
                            </a>
                        )}
                        <p className="iotHub-Name">{tenant.iotHubName}</p>
                    </Cell>
                )}
                {this.state.isEdit && this.state.tenantId !== tenant.id && (
                    <Cell>
                        {tenant.displayName === this.props.currentTenant ? (
                            tenant.displayName
                        ) : (
                            <a
                                onClick={() =>
                                    this.props.switchTenant(tenant.id)
                                }
                                href="#"
                            >
                                {tenant.displayName}
                            </a>
                        )}
                        <p className="iotHub-Name">{tenant.iotHubName}</p>
                    </Cell>
                )}
                {this.state.isEdit && this.state.tenantId === tenant.id && (
                    <div>
                        <FormControl
                            value={this.state.tenantName}
                            type="text"
                            onChange={this.handleChange}
                        />
                        <p className="iotHub-Name">{tenant.iotHubName}</p>
                    </div>
                )}
                <Cell>{tenant.role}</Cell>
                <Cell>
                    {this.props.isSystemAdmin &&
                    tenant.displayName === this.props.currentTenant &&
                    this.props.tenants.length > 1 ? (
                        <Protected permission={permissions.deleteTenant}>
                            <Btn
                                className="delete-tenant-button"
                                primary={true}
                                onClick={() =>
                                    this.props.deleteTenantThenSwitch(
                                        idx !== 0
                                            ? this.props.tenants[idx - 1].id
                                            : this.props.tenants[idx + 1].id
                                    )
                                }
                            >
                                {this.props.t(
                                    "profileFlyout.tenants.deleteTenant"
                                )}
                            </Btn>
                        </Protected>
                    ) : (
                        ""
                    )}
                    {!this.state.isEdit && (
                        <Btn
                            svg={svgs.edit}
                            onClick={() => this.onEdit(idx)}
                        ></Btn>
                    )}
                    {this.state.isEdit && this.state.tenantId === tenant.id && (
                        <Btn
                            className="small"
                            svg={svgs.checkmark}
                            onClick={this.handleSubmit}
                        >
                            {this.props.t("profileFlyout.tenants.saveTenant")}
                        </Btn>
                    )}
                    {this.state.isEdit && this.state.tenantId === tenant.id && (
                        <Btn
                            className="small"
                            svg={svgs.cancelX}
                            onClick={this.handleCancel}
                        >
                            {this.props.t("profileFlyout.tenants.cancel")}
                        </Btn>
                    )}
                </Cell>
            </Row>
        ));
    }
}

export default TenantGrid;
