// <copyright file="CloudTableClientFactory.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using Microsoft.Azure.Cosmos.Table;

namespace Mmm.Iot.Functions.DeploymentSync.Shared
{
    public class CloudTableClientFactory : IFactory<CloudTableClient>
    {
        public CloudTableClient Create(string cloudTableConnectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(cloudTableConnectionString);
            return storageAccount.CreateCloudTableClient();
        }
    }
}