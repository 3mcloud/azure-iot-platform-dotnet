// <copyright file="ActionsProcessorHelper.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Mmm.Iot.Functions.RuleActions.Models;
using Newtonsoft.Json.Linq;
using SendGrid;

namespace Mmm.Iot.Functions.RuleActions.Helpers
{
    public class ActionsProcessorHelper
    {
        private readonly SendGridClientHelper sendGridClientHelper;

        private ISendGridClient emailClient;

        public ActionsProcessorHelper(SendGridClientHelper sendGridClientHelper)
        {
            this.sendGridClientHelper = sendGridClientHelper;
        }

        private ISendGridClient EmailClient
        {
            get
            {
                if (this.emailClient == null)
                {
                    this.emailClient = this.sendGridClientHelper.GetClient();
                }

                return this.emailClient;
            }
        }

        public async Task TriggerActions(ExecutionContext context, ActionEvent actionEvent)
        {
            var unableToTriggerExceptions = new List<Exception>();

            foreach (RuleAction action in actionEvent.RuleActions)
            {
                switch (action.Type)
                {
                    case RuleActionType.Email:
                        await this.TriggerEmailAction(context, actionEvent, action);
                        break;
                    default:
                        unableToTriggerExceptions.Add(new Exception($"No action trigger handler existed for Rule Action Type {action.Type}"));
                        break;
                }
            }

            if (unableToTriggerExceptions.Count > 0)
            {
                throw new AggregateException("Unable to trigger one or more actions", unableToTriggerExceptions);
            }
        }

        private async Task TriggerEmailAction(ExecutionContext context, ActionEvent actionInfo, RuleAction action)
        {
            var hasRecipients = action.Parameters.TryGetValue("Recipients", out object recipientsResult);
            if (!hasRecipients)
            {
                throw new ArgumentException("The given email action has no listed recipients");
            }

            var recipients = new List<string>();
            try
            {
                var recipientsJArray = (JArray)recipientsResult;
                recipients = recipientsJArray.Select(token => token.Value<string>()).ToList();
            }
            catch (InvalidCastException ice)
            {
                throw new ArgumentException("The given email action did not have a 'Recipients' parameter that could be converted to a list of strings", ice);
            }

            if (recipients.Count == 0)
            {
                throw new ArgumentException("The given email action did not contain any recipients");
            }

            action.Parameters.TryGetValue("Subject", out object subject);
            action.Parameters.TryGetValue("Notes", out object notes);

            var timeReceived = new DateTime(1970, 1, 1).AddMilliseconds(double.Parse(actionInfo.DeviceMsgReceived));

            var messageInfo = new EmailFormatModel
            {
                Recipients = recipients,
                Subject = (string)subject,
                DeviceId = actionInfo.DeviceId,
                Description = actionInfo.RuleDescription,
                Severity = actionInfo.RuleSeverity,
                Time = timeReceived.ToString(),
                Notes = (string)notes,
            };

            var message = EmailMessageHelper.GenerateAlertEmailMessage(context, messageInfo);
            await this.EmailClient.SendEmailAsync(message);
        }
    }
}