// <copyright file="DeviceTelemetryKeyConstants.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

namespace Mmm.Iot.Functions.Messaging.Shared
{
    public static class DeviceTelemetryKeyConstants
    {
        // telemetry json augment values
        public const string Id = "id";
        public const string DeviceId = "deviceId";
        public const string DateTimeReceived = "_dateTimeReceived";
        public const string TimeReceived = "_timeReceived";
        public const string Schema = "_schema";

        public const string MessageSchema = "message";

        // property values that are attached by the iot hub
        public const string Tenant = "tenant";
        public const string BatchedTelemetry = "batchedTelemetry";

        // system property values that are attached by the iot hub
        public const string IotHubConnectionDeviceId = "iothub-connection-device-id";
        public const string IotHubEnqueuedTime = "iothub-enqueuedtime";

        // batch data keys
        public const string BatchedDataPointTimestampKey = "UnixTS";
        public const string BatchedDataPointTimestampMeasurementKey = "UnixTSMeasurement";

        // schema values
        public const string TelemetrySchema = "message";
        public const string TwinChangeSchema = "twinchange";
        public const string LifecycleSchema = "lifecycle";
    }
}