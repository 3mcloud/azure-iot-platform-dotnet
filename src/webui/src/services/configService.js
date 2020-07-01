// Copyright (c) Microsoft. All rights reserved.

// eslint-disable-next-line no-unused-vars
import Config from "app.config";
import { HttpClient } from "utilities/httpClient";
import {
    prepareLogoResponse,
    toDeviceGroupModel,
    toDeviceGroupsModel,
    toSolutionSettingActionsModel,
    toSolutionSettingThemeModel,
    toSolutionSettingFirmwareModel,
    toNewPackageRequestModel,
    toPackagesModel,
    toPackageModel,
    toConfigTypesModel,
    toNewFirmwareUploadRequestModel,
    toFirmwareModel,
} from "./models";
import { Observable } from "rxjs";

const ENDPOINT = Config.serviceUrls.config;
export const INACTIVE_PACKAGE_TAG = "reserved.inactive";

/** Contains methods for calling the config service */
export class ConfigService {
    /** Returns a the account's device groups */
    static getDeviceGroups() {
        return HttpClient.get(`${ENDPOINT}devicegroups`).map(
            toDeviceGroupsModel
        );
    }

    /** Creates a new device group */
    static createDeviceGroup(payload) {
        return HttpClient.post(`${ENDPOINT}devicegroups`, payload).map(
            toDeviceGroupModel
        );
    }

    static updateDeviceGroup(id, payload) {
        return HttpClient.put(`${ENDPOINT}devicegroups/${id}`, payload).map(
            toDeviceGroupModel
        );
    }

    /** Delete a device group */
    static deleteDeviceGroup(id) {
        return HttpClient.delete(`${ENDPOINT}devicegroups/${id}`).map(
            (_) => id
        );
    }

    static getLogo() {
        let options = {};
        options.responseType = "blob";
        options.headers = {
            Accept: undefined,
            "Content-Type": undefined,
        };
        return HttpClient.get(`${ENDPOINT}solution-settings/logo`, options).map(
            prepareLogoResponse
        );
    }

    static setLogo(logo, header) {
        const options = {
            headers: header,
            responseType: "blob",
        };

        if (!logo) {
            logo = "";
        }

        options.headers.Accept = undefined;
        return HttpClient.put(
            `${ENDPOINT}solution-settings/logo`,
            logo,
            options
        ).map(prepareLogoResponse);
    }

    /* Get solution settings.
      The API name is "solution-settings/theme" even though it deals with diagnosticsOptIn,
      AzureMapsKeys, name, description and UI theme. */
    static getSolutionSettings() {
        return (
            HttpClient.get(`${ENDPOINT}solution-settings/theme`)
                .map(toSolutionSettingThemeModel)
                /* When the application loads for the first time, there won't be "solution-settings"
       in the storage. In that case, service will throw a 404 not found error.
       But we need to ignore that and stick with the static values defined in UI state */
                .catch((error) =>
                    error.status === 404
                        ? Observable.empty()
                        : Observable.throw(error)
                )
        );
    }

    /* Update solution settings.
     The API name is "solution-settings/theme" even though it deals with diagnosticsOptIn,
      AzureMapsKeys, name, description and UI theme.*/
    static updateSolutionSettings(model) {
        return HttpClient.put(`${ENDPOINT}solution-settings/theme`, model).map(
            toSolutionSettingThemeModel
        );
    }

    static setDefaultFirmwareSetting(model) {
        return HttpClient.post(
            `${ENDPOINT}solution-settings/defaultFirmware`,
            model
        );
    }

    static getDefaultFirmwareSetting() {
        return HttpClient.get(
            `${ENDPOINT}solution-settings/defaultFirmware`
        ).map(toSolutionSettingFirmwareModel);
    }

    static getActionSettings() {
        return HttpClient.get(`${ENDPOINT}solution-settings/actions`).map(
            toSolutionSettingActionsModel
        );
    }

    /** Creates a new package */
    static createPackage(packageModel) {
        let options = {
            headers: {
                Accept: undefined,
                "Content-Type": undefined,
            },
        };
        return HttpClient.post(
            `${ENDPOINT}packages`,
            toNewPackageRequestModel(packageModel),
            options
        ).map(toPackageModel);
    }
    /** Creates a new package */
    static uploadFirmware(firmwareFile) {
        let options = {
            headers: {
                Accept: undefined,
                "Content-Type": undefined,
            },
            timeout: 300000, // 5 min
        };
        return HttpClient.post(
            `${ENDPOINT}packages/UploadFile`,
            toNewFirmwareUploadRequestModel(firmwareFile),
            options
        ).map(toFirmwareModel);
    }

    /** Returns all the account's packages */
    static getPackages(deviceGroup) {
        return HttpClient.get(
            `${ENDPOINT}packages?tags=devicegroup.*&tags=devicegroup.${deviceGroup}&tagOperator=OR`
        ).map(toPackagesModel);
    }

    /** Returns filtered packages */
    static getFilteredPackages(deviceGroup, packageType, configType) {
        return HttpClient.get(
            `${ENDPOINT}packages?packagetype=${packageType}&configtype=${configType}&tags=%5B%22devicegroup.*%22%2C+%22devicegroup.${deviceGroup}%22%5D&tagOperator=OR`
        ).map(toPackagesModel);
    }

    static activatePackage(id) {
        return HttpClient.delete(
            `${ENDPOINT}packages/${id}/tags/${INACTIVE_PACKAGE_TAG}`
        ).map(toPackageModel);
    }

    static deactivatePackage(id) {
        return HttpClient.put(
            `${ENDPOINT}packages/${id}/tags/${INACTIVE_PACKAGE_TAG}`
        ).map(toPackageModel);
    }

    static addPackageTag(id, tag) {
        return HttpClient.put(`${ENDPOINT}packages/${id}/tags/${tag}`).map(
            toPackageModel
        );
    }

    static removePackageTag(id, tag) {
        return HttpClient.delete(`${ENDPOINT}packages/${id}/tags/${tag}`).map(
            toPackageModel
        );
    }

    /** Returns all the account's packages */
    static getConfigTypes() {
        return HttpClient.get(`${ENDPOINT}configtypes`).map(toConfigTypesModel);
    }

    /** Delete a package */
    static deletePackage(id) {
        return HttpClient.delete(`${ENDPOINT}packages/${id}`).map((_) => id);
    }
}
