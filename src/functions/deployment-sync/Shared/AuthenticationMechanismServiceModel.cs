// <copyright file="AuthenticationMechanismServiceModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using Microsoft.Azure.Devices;

namespace Mmm.Iot.Functions.DeploymentSync.Shared
{
    public class AuthenticationMechanismServiceModel
    {
        public AuthenticationMechanismServiceModel()
        {
        }

        internal AuthenticationMechanismServiceModel(AuthenticationMechanism azureModel)
        {
            switch (azureModel.Type)
            {
                case Microsoft.Azure.Devices.AuthenticationType.Sas:
                    this.PrimaryKey = azureModel.SymmetricKey.PrimaryKey;
                    this.SecondaryKey = azureModel.SymmetricKey.SecondaryKey;
                    break;
                case Microsoft.Azure.Devices.AuthenticationType.SelfSigned:
                    this.AuthenticationType = AuthenticationType.SelfSigned;
                    this.PrimaryThumbprint = azureModel.X509Thumbprint.PrimaryThumbprint;
                    this.SecondaryThumbprint = azureModel.X509Thumbprint.SecondaryThumbprint;
                    break;
                case Microsoft.Azure.Devices.AuthenticationType.CertificateAuthority:
                    this.AuthenticationType = AuthenticationType.CertificateAuthority;
                    this.PrimaryThumbprint = azureModel.X509Thumbprint.PrimaryThumbprint;
                    this.SecondaryThumbprint = azureModel.X509Thumbprint.SecondaryThumbprint;
                    break;
                default:
                    throw new ArgumentException("Not supported authentcation type");
            }
        }

        public string PrimaryKey { get; set; }

        public string SecondaryKey { get; set; }

        public string PrimaryThumbprint { get; set; }

        public string SecondaryThumbprint { get; set; }

        public AuthenticationType AuthenticationType { get; set; }

        public AuthenticationMechanism ToAzureModel()
        {
            var auth = new AuthenticationMechanism();

            switch (this.AuthenticationType)
            {
                case AuthenticationType.Sas:
                    {
                        auth.SymmetricKey = new SymmetricKey()
                        {
                            PrimaryKey = this.PrimaryKey,
                            SecondaryKey = this.SecondaryKey,
                        };

                        auth.Type = Microsoft.Azure.Devices.AuthenticationType.Sas;

                        break;
                    }

                case AuthenticationType.SelfSigned:
                    {
                        auth.X509Thumbprint = new X509Thumbprint()
                        {
                            PrimaryThumbprint = this.PrimaryThumbprint,
                            SecondaryThumbprint = this.SecondaryThumbprint,
                        };

                        auth.Type = Microsoft.Azure.Devices.AuthenticationType.SelfSigned;

                        break;
                    }

                case AuthenticationType.CertificateAuthority:
                    {
                        auth.X509Thumbprint = new X509Thumbprint()
                        {
                            PrimaryThumbprint = this.PrimaryThumbprint,
                            SecondaryThumbprint = this.SecondaryThumbprint,
                        };

                        auth.Type = Microsoft.Azure.Devices.AuthenticationType.CertificateAuthority;

                        break;
                    }

                default:
                    throw new ArgumentException("Not supported authentcation type");
            }

            return auth;
        }
    }
}