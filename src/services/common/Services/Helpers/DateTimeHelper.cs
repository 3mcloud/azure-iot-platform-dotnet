// <copyright file="DateTimeHelper.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using Newtonsoft.Json.Linq;

namespace Mmm.Iot.Common.Services.Helpers
{
    public class DateTimeHelper
    {
        public static string FormatDate(JValue dateTime, string format)
        {
            DateTimeOffset date;
            try
            {
                var dateInUnix = dateTime.Value<long>();

                date = DateTimeOffset.FromUnixTimeMilliseconds(dateInUnix);

                if (date.Year <= 1970)
                {
                    date = DateTimeOffset.FromUnixTimeMilliseconds(dateInUnix * 1000);
                }

                return date.ToString(format);
            }
            catch (Exception)
            {
            }

            try
            {
                date = dateTime.Value<DateTime>();
                return date.ToString(format);
            }
            catch (Exception)
            {
            }

            return dateTime.ToString();
        }
    }
}