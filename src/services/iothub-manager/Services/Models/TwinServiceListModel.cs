// <copyright file="TwinServiceListModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Mmm.Iot.IoTHubManager.Services.Models
{
    public class TwinServiceListModel
    {
        public TwinServiceListModel()
        {
        }

        public TwinServiceListModel(IEnumerable<TwinServiceModel> twins, string continuationToken = null)
        {
            this.ContinuationToken = continuationToken;
            this.Items = new List<TwinServiceModel>(twins);
        }

        public string ContinuationToken { get; set; }

        public List<TwinServiceModel> Items { get; set; }
    }
}