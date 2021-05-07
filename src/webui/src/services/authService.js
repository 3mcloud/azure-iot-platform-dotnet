// Copyright (c) Microsoft. All rights reserved.

import Config from "app.config";
import { UserManager, WebStorageStateStore } from "oidc-client";
import { Observable } from "rxjs";
import { HttpClient } from "utilities/httpClient";
import { toUserModel, authDisabledUser } from "./models";
import { Policies } from "utilities";

const jwt_decode = require("jwt-decode");

export class AuthService {
    static authContext; // Created on AuthService.initialize()
    static authEnabled = true;
    static aadInstance = "";
    static _userManager = null;
    static settings = {};
    static ENDPOINT = Config.serviceUrls.auth;

    static initialize() {
        if (typeof global.DeploymentConfig === "undefined") {
            alert(
                "The dashboard configuration is missing.\n\nVerify the content of webui-config.js."
            );
            throw new Error(
                "The global configuration is missing. Verify the content of webui-config.js."
            );
        }
        AuthService.authEnabled = global.DeploymentConfig.authEnabled;
        AuthService.settings = {
            authority: global.DeploymentConfig.issuer,
            client_id: "IoTPlatform",
            redirect_uri: window.location.origin,
            post_logout_redirect_uri: window.location.origin,
            response_type: "id_token",
            response_mode: "fragment",
            userStore: new WebStorageStateStore({ store: window.localStorage }),
            loadUserInfo: false,
        };
        AuthService._userManager = new UserManager(AuthService.settings);
    }

    static isInvitation(hash) {
        return hash.includes("invite");
    }
    static isCallback(hash) {
        return (
            hash.includes("id_token") ||
            hash.includes("access_token") ||
            hash.includes("error_description")
        );
    }

    static isDisabled() {
        return AuthService.authEnabled === false;
    }

    static isEnabled() {
        return !AuthService.isDisabled();
    }

    static onLoad(successCallback) {
        AuthService.initialize();
        if (AuthService.isDisabled()) {
            console.debug("Skipping Auth onLoad because Auth is disabled");
            if (successCallback) {
                successCallback();
            }
            return;
        }
        console.log("Loading Auth....");
        //Redirect if it is an invitation link
        if (AuthService.isInvitation(window.location.hash)) {
            let parsedHash = new URLSearchParams(
                window.location.hash.substr(1) // skip the first char (#)
            );
            AuthService.settings.extraQueryParams = {
                invite: parsedHash.get("invite"),
            };
            AuthService._userManager = new UserManager(AuthService.settings);
            console.log("Invitation detected: redirecting for authentication");
            AuthService._userManager.signinRedirect();
        }

        // Attempt to sign in if the current window is not a callback
        if (!AuthService.isCallback(window.location.hash)) {
            console.log("is not a callback.. attempt to find user");
            AuthService.getCurrentUser().subscribe((user) => {
                if (user) {
                    console.log(`Signed in as ${user.name} with ${user.email}`);
                    if (successCallback) {
                        successCallback();
                    }
                } else {
                    console.log("The user is not signed in");
                    AuthService._userManager.signinRedirect();
                }
            });
        } else {
            window.location.hash = decodeURIComponent(window.location.hash); // decode hash
            AuthService._userManager.signinRedirectCallback().then((user) => {
                window.location = window.location.origin;
            });
        }
    }

    static getUserName(callback) {
        if (AuthService.isDisabled()) {
            return;
        }

        if (AuthService._userManager.getUser()) {
            Observable.of({ Name: "Temp Name", Email: "temp.name@contoso.com" })
                .map((data) =>
                    data ? { Name: data.Name, Email: data.Email } : null
                )
                .subscribe(callback);
        } else {
            console.log("The user is not signed in");
            AuthService._userManager.signinRedirect();
        }
    }

    static switchTenant(tenant) {
        if (AuthService.isDisabled()) {
            return;
        }

        AuthService._userManager.getUser().then((user) => {
            if (user) {
                HttpClient.post(
                    `${global.DeploymentConfig.issuer}/connect/switch/${tenant}`
                ).subscribe((new_token) => {
                    user.id_token = new_token;
                    user.profile.tenant = tenant;
                    let claims = JSON.parse(atob(new_token.split(".")[1]));
                    if (typeof claims.role === "string") {
                        claims.role = [claims.role.toLowerCase()];
                    }
                    user.profile.role = claims.role;

                    AuthService._userManager.storeUser(user);
                    window.location = window.location.origin;
                });
            } else {
                console.log("Must be logged in to switch tenants");
            }
        });
    }

    /** Returns a the current user */
    static getCurrentUser() {
        if (AuthService.isDisabled()) {
            return Observable.of(authDisabledUser);
        }

        return Observable.create((observer) => {
            return AuthService._userManager.getUser().then((user) => {
                if (user) {
                    // Following two should be Arrays but claims will return a string if only one value.
                    let roles =
                        typeof user.profile.role === "string"
                            ? [user.profile.role.toLowerCase()]
                            : user.profile.role,
                        availableTenants =
                            typeof user.profile.available_tenants === "string"
                                ? [user.profile.available_tenants]
                                : user.profile.available_tenants;

                    if (roles === undefined) {
                        roles = [];
                    }

                    // Followed format but really shouldnt the data structure be a Map? not a list? -- Andrew Schmidt
                    let flattenedPermissions = Policies.filter(
                        (policy) =>
                            roles.indexOf(policy.Role.toLowerCase()) > -1
                    )
                        .map((policy) => policy.AllowedActions)
                        .flat();
                    let isSystemAdmin = user.profile.is_systemAdmin;
                    observer.next(
                        toUserModel({
                            id: user.profile.sub,
                            name: user.profile.name,
                            email: user.profile.email,
                            roles: roles,
                            allowedActions: flattenedPermissions,
                            tenant: user.profile.tenant,
                            availableTenants: availableTenants,
                            token: user.id_token,
                            isSystemAdmin: isSystemAdmin,
                        })
                    );
                } else {
                    observer.next(null);
                }
                observer.complete();
            });
        });
    }

    static logout() {
        if (AuthService.isDisabled()) {
            return;
        }

        AuthService._userManager.signoutRedirect();
    }

    /**
     * Returns true if the token is not expired. Else, it returns false and enforces Logout.
     */
    static checkTokenValidity(token) {
        if (token) {
            const decodedToken = jwt_decode(token);
            return (
                decodedToken && decodedToken.exp * 1000 > new Date().getTime()
            );
        }
        return false;
    }

    /**
     * Acquires token from the cache if it is not expired.
     * Otherwise sends request to AAD to obtain a new token.
     */
    static getAccessToken() {
        if (AuthService.isDisabled()) {
            return Observable.of("client-auth-disabled");
        }

        return Observable.create((observer) => {
            return AuthService._userManager.getUser().then((user) => {
                if (user && this.checkTokenValidity(user.id_token)) {
                    observer.next(user.id_token);
                } else {
                    this.logout(); //Logging out because The User obj is empty.
                    console.log(
                        "Authentication Error while Aquiring Access Token"
                    );
                    observer.error(
                        "Authentication Error while Aquiring Access Token"
                    );
                }
                observer.complete();
            });
        });
    }
}
