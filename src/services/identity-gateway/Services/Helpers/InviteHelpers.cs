// <copyright file="InviteHelpers.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.IdentityGateway.Services;
using Mmm.Iot.IdentityGateway.Services.Models;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Mmm.Iot.IdentityGateway.Services.Helpers
{
    public class InviteHelpers : IInviteHelpers
    {
        private const string ResourceName = "Mmm.Iot.IdentityGateway.Services.files.InviteEmail.html";
        private const string DefaultFromEmail = "iotnoreply@mmm.com";
        private const string DefaultFromMessage = "3M IoT Platform Team";
        private const string DefaultSubject = "Invitation to IoT Platform";
        private const string DefaultMessageBody = "Tap the button below to accept this invitation. If you did not expect an invitation, you can delete this email.";
        private readonly ILogger logger;
        private readonly AppConfig appConfig;
        private string inviteFromEmail;
        private UserSettingsContainer userSettingsContainer;

        public InviteHelpers(ILogger<InviteHelpers> logger, UserSettingsContainer userSettingsContainer, AppConfig appConfig)
        {
            this.logger = logger;
            this.userSettingsContainer = userSettingsContainer;
            this.appConfig = appConfig;
            this.inviteFromEmail = string.IsNullOrWhiteSpace(this.appConfig.Global.InviteFromEmail) ? DefaultFromEmail : this.appConfig.Global.InviteFromEmail;
        }

        public async Task<UserSettingsModel> CreateUserSettings(Invitation invitation, string userId)
        {
            UserSettingsInput settingsInput = new UserSettingsInput
            {
                UserId = userId,
                SettingKey = "InviteUserMetaData",
                Value = invitation.InviteUserMetaData,
            };
            return await this.userSettingsContainer.CreateAsync(settingsInput);
        }

        public SendGridMessage CreateMessage(Invitation invitation, string forwardedFor, string inviteToken, string requestHost)
        {
            var msg = new SendGridMessage();

            msg.SetFrom(new EmailAddress(invitation.FromEmail ?? this.inviteFromEmail, invitation.FromMessage ?? DefaultFromMessage));

            var recipients = new List<EmailAddress>
            {
                new EmailAddress(invitation.EmailAddress),
            };
            msg.AddTos(recipients);
            var assembly = Assembly.GetExecutingAssembly();
            Func<IDictionary<string, object>, string> template;

            // Load the email template from file
            using (Stream stream = assembly.GetManifestResourceStream(ResourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                template = Mustachio.Parser.Parse(reader.ReadToEnd());
            }

            msg.SetSubject(invitation.EmailSubject ?? DefaultSubject);

            Uri uri = new Uri(invitation.InviteUri ?? forwardedFor ?? "https://" + requestHost);

            // Set the model for the template
            dynamic model = new ExpandoObject();
            model.link = uri.AbsoluteUri + "#invite=" + inviteToken;
            model.message = invitation.MessageBody ?? DefaultMessageBody;

            // Set the content by doing a render on the template with the model
            msg.AddContent(MimeType.Html, template(model));
            return msg;
        }
    }
}