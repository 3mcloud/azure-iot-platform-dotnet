// <copyright file="KeyValueDocument.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using Microsoft.Azure.Documents;

namespace Mmm.Iot.Functions.DeploymentSync.Shared
{
    internal sealed class KeyValueDocument : Resource
    {
        public KeyValueDocument(string collectionId, string key, string data)
        {
            this.Id = GenerateId(collectionId, key);
            this.CollectionId = collectionId;
            this.Key = key;
            this.Data = data;
        }

        public string CollectionId { get; }

        public string Key { get; }

        public string Data { get; }

        public static string GenerateId(string collectionId, string key)
        {
            return $"{collectionId.ToLowerInvariant()}.{key.ToLowerInvariant()}";
        }
    }
}