// <copyright file="DeviceServiceModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;

namespace Mmm.Iot.Functions.DeploymentSync.Shared
{
    public class DeviceServiceModel
    {
        public DeviceServiceModel(
            string etag,
            string id,
            int c2DMessageCount,
            DateTime lastActivity,
            bool connected,
            bool enabled,
            bool isEdgeDevice,
            DateTime lastStatusUpdated,
            TwinServiceModel twin,
            AuthenticationMechanismServiceModel authentication,
            string ioTHubHostName)
        {
            this.Etag = etag;
            this.Id = id;
            this.C2DMessageCount = c2DMessageCount;
            this.LastActivity = lastActivity;
            this.Connected = connected;
            this.Enabled = enabled;
            this.IsEdgeDevice = isEdgeDevice;
            this.LastStatusUpdated = lastStatusUpdated;
            this.Twin = twin;
            this.IoTHubHostName = ioTHubHostName;
            this.Authentication = authentication;
        }

        public DeviceServiceModel(Device azureDevice, Twin azureTwin, string ioTHubHostName, bool isConnected)
            : this(
                etag: azureDevice.ETag,
                id: azureDevice.Id,
                c2DMessageCount: azureDevice.CloudToDeviceMessageCount,
                lastActivity: azureDevice.LastActivityTime,
                connected: isConnected || azureDevice.ConnectionState.Equals(DeviceConnectionState.Connected),
                enabled: azureDevice.Status.Equals(DeviceStatus.Enabled),
                isEdgeDevice: azureDevice.Capabilities?.IotEdge ?? azureTwin.Capabilities?.IotEdge ?? false,
                lastStatusUpdated: azureDevice.StatusUpdatedTime,
                twin: new TwinServiceModel(azureTwin),
                ioTHubHostName: ioTHubHostName,
                authentication: new AuthenticationMechanismServiceModel(azureDevice.Authentication))
        {
        }

        public DeviceServiceModel(Device azureDevice, Twin azureTwin, string ioTHubHostName)
            : this(
                azureDevice,
                azureTwin,
                ioTHubHostName,
                azureDevice.ConnectionState.Equals(DeviceConnectionState.Connected))
        {
        }

        public DeviceServiceModel(Twin azureTwin, string ioTHubHostName, bool isConnected)
            : this(
                etag: azureTwin.ETag,
                id: azureTwin.DeviceId,
                c2DMessageCount: azureTwin.CloudToDeviceMessageCount ?? azureTwin.CloudToDeviceMessageCount ?? 0,
                lastActivity: azureTwin.LastActivityTime ?? azureTwin.LastActivityTime ?? default,
                connected: isConnected || azureTwin.ConnectionState.Equals(DeviceConnectionState.Connected),
                enabled: azureTwin.Status.Equals(DeviceStatus.Enabled),
                isEdgeDevice: azureTwin.Capabilities?.IotEdge ?? azureTwin.Capabilities?.IotEdge ?? false,
                lastStatusUpdated: azureTwin.StatusUpdatedTime ?? azureTwin.StatusUpdatedTime ?? default,
                twin: new TwinServiceModel(azureTwin),
                ioTHubHostName: ioTHubHostName,
                authentication: null)
        {
        }

        public string Etag { get; set; }

        public string Id { get; set; }

        public int C2DMessageCount { get; set; }

        public DateTime LastActivity { get; set; }

        public bool Connected { get; set; }

        public bool Enabled { get; set; }

        public bool IsEdgeDevice { get; set; }

        public DateTime LastStatusUpdated { get; set; }

        public TwinServiceModel Twin { get; set; }

        public string IoTHubHostName { get; set; }

        public AuthenticationMechanismServiceModel Authentication { get; set; }

        public Device ToAzureModel(bool ignoreEtag = true)
        {
            var device = new Device(this.Id)
            {
                ETag = ignoreEtag ? null : this.Etag,
                Status = this.Enabled ? DeviceStatus.Enabled : DeviceStatus.Disabled,
                Authentication = this.Authentication?.ToAzureModel(),
                Capabilities = this.IsEdgeDevice ? new DeviceCapabilities()
                {
                    IotEdge = this.IsEdgeDevice,
                }
                : null,
            };

            return device;
        }
    }
}