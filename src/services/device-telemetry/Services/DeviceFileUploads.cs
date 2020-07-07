// <copyright file="DeviceFileUploads.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.DeviceTelemetry.Services.Models;

namespace Mmm.Iot.DeviceTelemetry.Services
{
    public class DeviceFileUploads : IDeviceFileUploads
    {
        private const string FileUploadStore = "iot-file-upload";
        private const string DateFormat = "yyyy-MM-dd'T'HH:mm:sszzz";
        private readonly AppConfig config;
        private readonly ILogger logger;

        public DeviceFileUploads(AppConfig config, ILogger<DeviceFileUploads> logger)
        {
            this.config = config;
            this.logger = logger;
        }

        public async Task<IEnumerable<DeviceUpload>> GetDeviceUploads(string tenantId, string deviceId)
        {
            CloudStorageAccount storageAccount = null;
            CloudBlobContainer cloudBlobContainer = null;
            string storageConnectionString = this.config.Global.StorageAccountConnectionString;
            List<DeviceUpload> files = new List<DeviceUpload>();

            if (string.IsNullOrEmpty(tenantId))
            {
                this.logger.LogError(new Exception("Tenant ID is blank, cannot find container without tenandId."), "Tenant ID is blank, cannot find container without tenandId.");
                return files;
            }

            if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount))
            {
                try
                {
                    CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();

                    // Create a container
                    cloudBlobContainer = cloudBlobClient.GetContainerReference($"{tenantId}-{FileUploadStore}");

                    CloudBlobDirectory cloudDirectory = cloudBlobContainer.GetDirectoryReference(deviceId);

                    Func<CloudBlobDirectory, Task<List<CloudBlockBlob>>> getBlobsFromContatiner = null;

                    getBlobsFromContatiner = async dir =>
                    {
                        List<CloudBlockBlob> blobItems = new List<CloudBlockBlob>();
                        BlobContinuationToken currentToken = null;

                        do
                        {
                            var listingResult = await dir.ListBlobsSegmentedAsync(currentToken);
                            currentToken = listingResult.ContinuationToken;

                            foreach (IListBlobItem listItem in listingResult.Results)
                            {
                                if (listItem is CloudBlockBlob)
                                {
                                    blobItems.Add((CloudBlockBlob)listItem);
                                }
                                else if (listItem is CloudBlobDirectory)
                                {
                                    blobItems.AddRange(await getBlobsFromContatiner((CloudBlobDirectory)listItem));
                                }
                            }
                        }
                        while (currentToken != null);

                        return blobItems;
                    };

                    var blobs = await getBlobsFromContatiner(cloudDirectory);

                    if (blobs != null && blobs.Count > 0)
                    {
                        foreach (var blobItem in blobs)
                        {
                            files.Add(new DeviceUpload()
                            {
                                BlobName = blobItem.Name,
                                UploadedBy = deviceId,
                                UploadedOn = blobItem.Properties.Created?.ToString(DateFormat),
                                Size = blobItem.Properties.Length,
                            });
                        }
                    }

                    return files;
                }
                catch (StorageException ex)
                {
                    this.logger.LogError(new Exception($"Exception in the UploadToBlob method- Message: {ex.Message} : Stack Trace - {ex.StackTrace.ToString()}"), $"Exception in the UploadToBlob method- Message: {ex.Message} : Stack Trace - {ex.StackTrace.ToString()}");
                    return files;
                }
            }
            else
            {
                this.logger.LogError(new Exception("Error parsing CloudStorageAccount in FileUploads method"), "Error parsing CloudStorageAccount in FileUploads method");
                return files;
            }
        }

        public CloudBlockBlob Download(string tenantId, string blobName)
        {
            CloudStorageAccount storageAccount = null;
            CloudBlobContainer cloudBlobContainer = null;
            string storageConnectionString = this.config.Global.StorageAccountConnectionString;

            if (string.IsNullOrEmpty(tenantId))
            {
                this.logger.LogError(new Exception("Tenant ID is blank, cannot find container without tenandId."), "Tenant ID is blank, cannot find container without tenandId.");
                return null;
            }

            if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount))
            {
                try
                {
                    CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();

                    // Create a container
                    cloudBlobContainer = cloudBlobClient.GetContainerReference($"{tenantId}-{FileUploadStore}");

                    CloudBlockBlob blockBlob = cloudBlobContainer.GetBlockBlobReference(blobName);

                    return blockBlob;
                }
                catch (StorageException ex)
                {
                    this.logger.LogError(new Exception($"Exception in the UploadToBlob method- Message: {ex.Message} : Stack Trace - {ex.StackTrace.ToString()}"), $"Exception in the UploadToBlob method- Message: {ex.Message} : Stack Trace - {ex.StackTrace.ToString()}");
                    return null;
                }
            }

            return null;
        }
    }
}