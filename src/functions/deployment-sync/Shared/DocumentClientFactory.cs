// <copyright file="DocumentClientFactory.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Text.RegularExpressions;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace Mmm.Iot.Functions.DeploymentSync.Shared
{
    public class DocumentClientFactory : IFactory<IDocumentClient>
    {
        private readonly string connectionStringRegex = "^AccountEndpoint=(?<endpoint>.*);AccountKey=(?<key>.*);$";

        public IDocumentClient Create(string docDBConnectionString)
        {
            try
            {
                var match = Regex.Match(docDBConnectionString, this.connectionStringRegex);
                if (!match.Success)
                {
                    string message = "Invalid Connection String for CosmosDb";
                    throw new Exception(message);
                }

                Uri docDbEndpoint = new Uri(match.Groups["endpoint"].Value);
                string docDbKey = match.Groups["key"].Value;
                return new DocumentClient(docDbEndpoint, docDbKey);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Create  failed", ex);
            }
        }
    }
}