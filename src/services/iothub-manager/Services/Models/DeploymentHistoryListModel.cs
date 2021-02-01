// <copyright file="DeploymentHistoryListModel.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Text;

namespace Mmm.Iot.IoTHubManager.Services.Models
{
    public class DeploymentHistoryListModel
    {
        public DeploymentHistoryListModel(IEnumerable<DeploymentHistoryModel> deploymentHistoryModel, string continuationToken = null)
        {
            this.ContinuationToken = continuationToken;
            this.Items = new List<DeploymentHistoryModel>(deploymentHistoryModel);
        }

        public string ContinuationToken { get; set; }

        public List<DeploymentHistoryModel> Items { get; set; }
    }
}