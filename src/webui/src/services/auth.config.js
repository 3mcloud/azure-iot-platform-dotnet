import { AuthConfig } from "oidc-client";

// class IoTPlatformAuth extends AuthConfig {
//   tenant: string;
// }
let authConfig = new AuthConfig({
    issuer: "http://localhost:5000/",
    // URL of the SPA to redirect the user to after login
    redirectUri: window.location.origin,

    clientId: "f12b0b58-8089-422c-a3b1-e1dacb99883b", //tenantId

    // set the scope for the permissions the client should request
    // The first three are defined by OIDC. The 4th is a usecase-specific one
    //scope: 'openid ebce2d28-8fb8-4cc7-83ae-accc9d73ee9d',
    scope: "",

    strictDiscoveryDocumentValidation: false,
    loadUserInfo: false,
});

export default authConfig;
