// <copyright file="DeploymentsController.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using Mmm.Iot.Common.Services;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.Filters;
using Mmm.Iot.Common.Services.Helpers;
using Mmm.Iot.IoTHubManager.Services;
using Mmm.Iot.IoTHubManager.Services.Models;
using Mmm.Iot.IoTHubManager.WebService.Models;
using Newtonsoft.Json;

namespace Mmm.Iot.IoTHubManager.WebService.Controllers
{
    [Route("v1/[controller]")]
    [TypeFilter(typeof(ExceptionsFilterAttribute))]
    public class DeploymentsController : Controller
    {
        private readonly IDeployments deployments;

        public DeploymentsController(IDeployments deployments)
        {
            this.deployments = deployments;
        }

        [HttpPost]
        [Authorize("CreateDeployments")]
        public async Task<DeploymentApiModel> PostAsync([FromBody] DeploymentApiModel deployment)
        {
            if (string.IsNullOrWhiteSpace(deployment.Name))
            {
                throw new InvalidInputException("Name must be provided");
            }

            // If DeviceGroupId is provided, fill the DeviceGroup details if they are not provided
            if (!string.IsNullOrWhiteSpace(deployment.DeviceGroupId) && string.IsNullOrWhiteSpace(deployment.DeviceGroupQuery))
            {
                await this.HydrateDeploymentWithDeviceGroupDetails(deployment);
            }

            // If PackageId is provided, fill the package details if they are not provided
            if (!string.IsNullOrWhiteSpace(deployment.PackageId) && string.IsNullOrWhiteSpace(deployment.PackageContent))
            {
                await this.HydrateDeploymentWithPackageDetails(deployment);
            }

            if (string.IsNullOrWhiteSpace(deployment.DeviceGroupId))
            {
                throw new InvalidInputException("DeviceGroupId must be provided");
            }

            if (string.IsNullOrWhiteSpace(deployment.DeviceGroupName))
            {
                throw new InvalidInputException("DeviceGroupName must be provided");
            }

            if (string.IsNullOrWhiteSpace(deployment.DeviceGroupQuery) && (deployment.DeviceIds == null || (deployment.DeviceIds != null && deployment.DeviceIds.Count() == 0)))
            {
                throw new InvalidInputException("DeviceGroupQuery must be provided");
            }

            if (string.IsNullOrWhiteSpace(deployment.PackageContent))
            {
                throw new InvalidInputException("PackageContent must be provided");
            }

            if (deployment.PackageType.Equals(PackageType.DeviceConfiguration)
                && string.IsNullOrEmpty(deployment.ConfigType))
            {
                throw new InvalidInputException("Configuration type must be provided");
            }

            if (deployment.Priority < 0)
            {
                throw new InvalidInputException($"Invalid priority provided of {deployment.Priority}. " +
                                                "It must be non-negative");
            }

            return new DeploymentApiModel(await this.deployments.CreateAsync(deployment.ToServiceModel(), this.GetClaimsUserDetails(), this.GetTenantId()));
        }

        [HttpGet]
        [Authorize("ReadAll")]
        public async Task<DeploymentListApiModel> GetAsync()
        {
            return new DeploymentListApiModel(await this.deployments.ListFromStorageAsync());
        }

        [HttpGet("{id}")]
        [Authorize("ReadAll")]
        public async Task<DeploymentApiModel> GetAsync(string id, [FromQuery] bool includeDeviceStatus = false, [FromQuery] bool isLatest = true)
        {
            return new DeploymentApiModel(await this.deployments.GetAsync(id, includeDeviceStatus, isLatest));
        }

        [HttpDelete("{id}")]
        [Authorize("DeleteDeployments")]
        public async Task DeleteAsync(string id, [FromQuery] bool isDelete = true)
        {
            await this.deployments.DeleteAsync(id, this.GetClaimsUserDetails(), this.GetTenantId(), isDelete);
        }

        [HttpPut("{id}")]
        [Authorize("CreateDeployments")]
        public async Task ReactivateAsync(string id)
        {
            await this.deployments.ReactivateDeploymentAsyc(id, this.GetClaimsUserDetails(), this.GetTenantId());
        }

        [HttpPost("Devices/{id}")]
        [Authorize("ReadAll")]
        public async Task<DeviceListApiModel> GetDeploymentImpactedDevices(string id, [FromBody] List<string> deviceIds)
        {
            return new DeviceListApiModel(await this.deployments.GetDeviceListAsync(id, deviceIds, this.GetTenantId()));
        }

        [HttpPost("Modules/{id}")]
        [Authorize("ReadAll")]
        public async Task<TwinPropertiesListApiModel> GetDeploymentImpactedEdgeModules(string id, [FromBody] string query, [FromQuery] bool isLatest = false)
        {
            return new TwinPropertiesListApiModel(await this.deployments.GetModulesListAsync(id, query, isLatest));
        }

        [HttpGet("Report/{id}")]
        [Authorize("ReadAll")]
        public async Task<IActionResult> ExportDeploymentReport(string id, [FromQuery] bool isLatest = true)
        {
            List<DeviceDeploymentStatus> deviceDeploymentStatuses = new List<DeviceDeploymentStatus>();

            var deploymentStatuses = await this.deployments.GetDeploymentStatusReport(id, this.GetTenantId(), isLatest);

            if (deploymentStatuses != null && deploymentStatuses.Count > 0)
            {
                foreach (var deploymentStatus in deploymentStatuses)
                {
                    deviceDeploymentStatuses.Add(new DeviceDeploymentStatus(deploymentStatus));
                }
            }

            var stream = new MemoryStream();

            using (SpreadsheetDocument package = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
            {
                this.CreatePartsForExcel(package, deviceDeploymentStatuses);
            }

            stream.Position = 0;
            string excelName = $"DeploymentReport-{DateTime.Now.ToString("yyyyMMddHHmmssfff")}.xlsx";

            return this.File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
        }

        private async Task HydrateDeploymentWithPackageDetails(DeploymentApiModel deployment)
        {
            var package = await this.deployments.GetPackageAsync(deployment.PackageId);

            if (package == null)
            {
                throw new ResourceNotFoundException($"No Package found with packageId:{deployment.PackageId}");
            }

            deployment.PackageType = package.PackageType;
            deployment.ConfigType = package.ConfigType;
            deployment.PackageContent = package.Content;
            deployment.PackageName = package.Name;
            deployment.DeviceGroupId = string.IsNullOrWhiteSpace(deployment.DeviceGroupId) ? Guid.Empty.ToString() : deployment.DeviceGroupId;
            deployment.DeviceGroupName = string.IsNullOrWhiteSpace(deployment.DeviceGroupName) ? deployment.DeviceIds != null && deployment.DeviceIds.Any() ? "DirectToDevices: " + string.Join(',', deployment.DeviceIds) : "DirectToDevices" : deployment.DeviceGroupName;
        }

        private async Task HydrateDeploymentWithDeviceGroupDetails(DeploymentApiModel deployment)
        {
            var deviceGroup = await this.deployments.GetDeviceGroupAsync(deployment.DeviceGroupId);

            if (deviceGroup == null)
            {
                throw new ResourceNotFoundException($"No Device Group found with DeviceGroupId:{deployment.DeviceGroupId}");
            }

            deployment.DeviceGroupName = deviceGroup.DisplayName;
            deployment.DeviceGroupQuery = JsonConvert.SerializeObject(deviceGroup.Conditions);
        }

        private void CreatePartsForExcel(SpreadsheetDocument document, List<DeviceDeploymentStatus> data)
        {
            SheetData partSheetData = this.GenerateSheetdataForDetails(data);

            WorkbookPart workbookPart = document.AddWorkbookPart();
            this.GenerateWorkbookPartContent(workbookPart);

            WorkbookStylesPart workbookStylesPart = workbookPart.AddNewPart<WorkbookStylesPart>("rId3");
            OpenXMLHelper.GenerateWorkbookStylesPartContent(workbookStylesPart);

            WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>("rId1");
            OpenXMLHelper.GenerateWorksheetPartContent(worksheetPart, partSheetData);
        }

        private void GenerateWorkbookPartContent(WorkbookPart workbookPart)
        {
            Workbook workbook = new Workbook();
            Sheets sheets = new Sheets();
            Sheet sheet = new Sheet() { Name = "Deployment Report", SheetId = (UInt32Value)1U, Id = "rId1" };
            sheets.Append(sheet);
            workbook.Append(sheets);
            workbookPart.Workbook = workbook;
        }

        private SheetData GenerateSheetdataForDetails(List<DeviceDeploymentStatus> data)
        {
            SheetData sheetData = new SheetData();
            sheetData.Append(this.CreateHeaderRowForExcel());

            foreach (DeviceDeploymentStatus testmodel in data)
            {
                Row partsRows = this.GenerateRowForChildPartDetail(testmodel);
                sheetData.Append(partsRows);
            }

            return sheetData;
        }

        private Row CreateHeaderRowForExcel()
        {
            Row workRow = new Row();
            workRow.Append(OpenXMLHelper.CreateCell("Name", 2U));
            workRow.Append(OpenXMLHelper.CreateCell("Deployment Status", 2U));
            workRow.Append(OpenXMLHelper.CreateCell("Firmware", 2U));
            workRow.Append(OpenXMLHelper.CreateCell("Previous Firmware", 2U));
            workRow.Append(OpenXMLHelper.CreateCell("Start", 2U));
            workRow.Append(OpenXMLHelper.CreateCell("End", 2U));
            return workRow;
        }

        private Row GenerateRowForChildPartDetail(DeviceDeploymentStatus testmodel)
        {
            Row tRow = new Row();
            tRow.Append(OpenXMLHelper.CreateCell(testmodel.Name));
            tRow.Append(OpenXMLHelper.CreateCell(testmodel.DeploymentStatus));
            tRow.Append(OpenXMLHelper.CreateCell(testmodel.Firmware));
            tRow.Append(OpenXMLHelper.CreateCell(testmodel.PreviousFirmware));
            tRow.Append(OpenXMLHelper.CreateCell(testmodel.Start));
            tRow.Append(OpenXMLHelper.CreateCell(testmodel.End));

            return tRow;
        }
    }
}