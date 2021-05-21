import { camelCaseReshape } from "utilities";
import { Policies } from "utilities";
export const toUserTenantModel = (response = []) =>
    response.map((user) => {
        user = camelCaseReshape(user, {
            userId: "id",
            name: "name",
            type: "type",
            roleList: "role",
            createdBy: "createdBy",
            createdTime: "dateCreated",
        });
        user.role = user.role
            .map(
                (r) =>
                    Policies.filter((p) => p.Role.toLowerCase() === r).concat({
                        DisplayName: null,
                    })[0].DisplayName
            )
            .filter((t) => t != null)
            .join(",");
        return user;
    });
