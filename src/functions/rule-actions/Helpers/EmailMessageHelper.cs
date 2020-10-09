// <copyright file="EmailMessageHelper.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Mmm.Iot.Functions.RuleActions.Models;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Mmm.Iot.Functions.RuleActions.Helpers
{
    public static class EmailMessageHelper
    {
        public const string EmailFrom = "iotplatformnoreply@mmm.com";
        public const string EmailFromMessage = "3M IoT Platform Alerts & Analytics";
        public const string EmailSubjectPrefix = "IoT Platform Device Alert";

        public static SendGridMessage GenerateAlertEmailMessage(ExecutionContext context, EmailFormatModel messageInfo)
        {
            var msg = new SendGridMessage();
            msg.SetFrom(new EmailAddress(EmailFrom, EmailFromMessage));
            msg.AddTos(messageInfo.Recipients.Select(email => new EmailAddress(email)).ToList());

            if (!string.IsNullOrEmpty(messageInfo.Subject))
            {
                msg.SetSubject($"{EmailSubjectPrefix}: {messageInfo.Subject}");
            }
            else
            {
                msg.SetSubject(EmailSubjectPrefix);
            }

            Func<IDictionary<string, object>, string> template;

            string templateFilePath = Path.Combine(context.FunctionAppDirectory, "Files", "AlertEmail.html");

            using (Stream stream = File.OpenRead(templateFilePath))
            using (StreamReader reader = new StreamReader(stream))
            {
                template = Mustachio.Parser.Parse(reader.ReadToEnd());
            }

            // Set the model with the necessary template format values
            dynamic model = new ExpandoObject();
            model.DeviceId = messageInfo.DeviceId;
            model.Description = messageInfo.Description;
            model.Severity = messageInfo.Severity;
            model.Time = messageInfo.Time;
            model.Notes = messageInfo.Notes;

            msg.AddContent(MimeType.Html, template(model));

            return msg;
        }
    }
}