// <copyright file="TriggerRuleActions.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Functions.RuleActions.Helpers;
using Mmm.Iot.Functions.RuleActions.Models;
using Newtonsoft.Json;

namespace Mmm.Iot.Functions.RuleActions
{
    public static class TriggerRuleActions
    {
        [FunctionName("TriggerRuleActions")]
        public static async Task Run(
            [EventHubTrigger("actions-eventhub", Connection = "ActionsEventHubConnectionString", ConsumerGroup = "%ActionsEventHubConsumerGroup%")] EventData[] events,
            ILogger log,
            ExecutionContext context)
        {
            var exceptions = new List<Exception>();
            var sendGridApiKey = Environment.GetEnvironmentVariable("SendGridApiKey", EnvironmentVariableTarget.Process);
            var sendGridClientHelper = new SendGridClientHelper(sendGridApiKey);
            var actionProcessor = new ActionsProcessorHelper(sendGridClientHelper);

            foreach (EventData eventData in events)
            {
                try
                {
                    var messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
                    var alertMessageSplit = messageBody.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string alertMessage in alertMessageSplit)
                    {
                        try
                        {
                            var actionEvent = JsonConvert.DeserializeObject<ActionEvent>(alertMessage);
                            await actionProcessor.TriggerActions(context, actionEvent);
                        }
                        catch (Exception e)
                        {
                            // We need to keep processing the rest of the batch - capture this exception and continue.
                            exceptions.Add(e);
                        }
                    }
                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    exceptions.Add(new Exception("Unable to process one of the nested alert events.", e));
                }
            }

            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.
            if (exceptions.Count > 1)
            {
                throw new AggregateException(exceptions);
            }
            else if (exceptions.Count == 1)
            {
                throw exceptions.Single();
            }
        }
    }
}