// <copyright file="TelemetryTimestamp.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;

namespace Mmm.Iot.Functions.Messaging.Shared
{
    public partial class TelemetryTimestamp
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private DateTimeOffset dateTimeOffset;

        public TelemetryTimestamp(DateTime dateTime)
        {
            this.DateTime = dateTime.ToUniversalTime();
            this.dateTimeOffset = new DateTimeOffset(dateTime);
            this.EpochTimestamp = this.dateTimeOffset.ToUnixTimeMilliseconds();
            this.EpochTimestampMeasurement = TimestampMeasurement.Ms;
        }

        public TelemetryTimestamp(long epochTimestamp, TimestampMeasurement format)
        {
            this.DateTime = GetEpochAddition(epochTimestamp, format);
            this.dateTimeOffset = new DateTimeOffset(this.DateTime);
            this.EpochTimestamp = this.dateTimeOffset.ToUnixTimeMilliseconds();
            this.EpochTimestampMeasurement = TimestampMeasurement.Ms;
        }

        public DateTime DateTime { get; private set; }

        public long EpochTimestamp { get; private set; }

        public TimestampMeasurement EpochTimestampMeasurement { get; private set; }

        private static DateTime GetEpochAddition(long epochTimestamp, TimestampMeasurement format)
        {
            switch (format)
            {
                case TimestampMeasurement.S:
                    return Epoch.AddSeconds(epochTimestamp);
                case TimestampMeasurement.Ms:
                    return Epoch.AddMilliseconds(epochTimestamp);
                default:
                    throw new Exception("Unsupported TimestampMeasurement type '{format}', cannot convert timestamp to DateTime.");
            }
        }
    }
}