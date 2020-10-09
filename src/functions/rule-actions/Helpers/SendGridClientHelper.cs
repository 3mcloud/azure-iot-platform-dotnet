// <copyright file="SendGridClientHelper.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using SendGrid;

namespace Mmm.Iot.Functions.RuleActions.Helpers
{
    public class SendGridClientHelper : ISendGridClientHelper
    {
        private readonly string apiKey;

        public SendGridClientHelper(string apiKey)
        {
            this.apiKey = apiKey;
        }

        public ISendGridClient GetClient()
        {
            return new SendGridClient(this.apiKey);
        }
    }
}