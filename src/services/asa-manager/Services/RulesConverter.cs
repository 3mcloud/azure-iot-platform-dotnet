// <copyright file="RulesConverter.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mmm.Iot.AsaManager.Services.Models;
using Mmm.Iot.AsaManager.Services.Models.Rules;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.External.BlobStorage;
using Mmm.Iot.Common.Services.External.StorageAdapter;
using Newtonsoft.Json;

namespace Mmm.Iot.AsaManager.Services
{
    public class RulesConverter : Converter, IConverter
    {
        public RulesConverter(
            IBlobStorageClient blobClient,
            IStorageAdapterClient storageAdapterClient,
            ILogger<RulesConverter> log)
                : base(blobClient, storageAdapterClient, log)
        {
        }

        public override string Entity
        {
            get
            {
                return "rules";
            }
        }

        public override string FileExtension
        {
            get
            {
                return "json";
            }
        }

        public override async Task<ConversionApiModel> ConvertAsync(string tenantId, string operationId = null)
        {
            ValueListApiModel rules = null;
            try
            {
                rules = await this.StorageAdapterClient.GetAllAsync(this.Entity, tenantId);
            }
            catch (Exception e)
            {
                this.Logger.LogError(e, "Unable to query {entity} using storage adapter. OperationId: {operationId}. TenantId: {tenantId}", this.Entity, operationId, tenantId);
                throw e;
            }

            if (rules.Items.Count() == 0 || rules == null)
            {
                string errorMessage = $"No entities were receieved from storage adapter to convert to {this.Entity}. OperationId: {operationId}. TenantId: {tenantId}";
                this.Logger.LogError(new Exception(errorMessage), errorMessage);
                throw new ResourceNotFoundException("No entities were receieved from storage adapter to convert to rules.");
            }

            List<RuleReferenceDataModel> jsonRulesList = new List<RuleReferenceDataModel>();
            try
            {
                foreach (ValueApiModel item in rules.Items)
                {
                    try
                    {
                        RuleDataModel dataModel = JsonConvert.DeserializeObject<RuleDataModel>(item.Data);
                        RuleModel ruleModel = new RuleModel(item.Key, dataModel);

                        // return a RuleReferenceModel which is a conversion from the RuleModel into a SAjob readable format with additional metadata
                        RuleReferenceDataModel referenceModel = new RuleReferenceDataModel(ruleModel);
                        jsonRulesList.Add(referenceModel);
                    }
                    catch (Exception)
                    {
                        this.Logger.LogInformation("Unable to convert a rule to the proper reference data model for {entity}. OperationId: {operationId}. TenantId: {tenantId}", this.Entity, operationId, tenantId);
                    }
                }

                if (jsonRulesList.Count() == 0)
                {
                    throw new ResourceNotSupportedException("No rules were able to be converted to the proper rule reference data model.");
                }
            }
            catch (Exception e)
            {
                this.Logger.LogError(e, "Unable to convert {entity} queried from storage adapter to appropriate data model. OperationId: {operationId}. TenantId: {tenantId}", this.Entity, operationId, tenantId);
                throw e;
            }

            string fileContent = null;
            try
            {
                fileContent = JsonConvert.SerializeObject(jsonRulesList, Formatting.Indented);
            }
            catch (Exception e)
            {
                this.Logger.LogError(e, "Unable to serialize the IEnumerable of {entity} data models for the temporary file content. OperationId: {operationId}. TenantId: {tenantId}", this.Entity, operationId, tenantId);
                throw e;
            }

            string blobFilePath = await this.WriteFileContentToBlobAsync(fileContent, tenantId, operationId);

            ConversionApiModel conversionResponse = new ConversionApiModel
            {
                TenantId = tenantId,
                BlobFilePath = blobFilePath,
                Entities = rules,
                OperationId = operationId,
            };
            this.Logger.LogInformation("Successfully Completed {entity} conversion\n{model}", this.Entity, JsonConvert.SerializeObject(conversionResponse));
            return conversionResponse;
        }
    }
}