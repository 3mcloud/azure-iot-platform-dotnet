// <copyright file="TenantListModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Mmm.Iot.IdentityGateway.Services.Models
{
    public class TenantListModel
    {
        public TenantListModel()
        {
        }

        public TenantListModel(List<TenantModel> models)
        {
            this.Models = models;
        }

        public TenantListModel(string batchMethod, List<TenantModel> models)
        {
            this.BatchMethod = batchMethod;
            this.Models = models;
        }

        [JsonProperty("Method")]
        public string BatchMethod { get; set; }

        [JsonProperty("Models")]
        public List<TenantModel> Models { get; set; }
    }
}