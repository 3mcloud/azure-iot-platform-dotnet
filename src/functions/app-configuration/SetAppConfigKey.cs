// <copyright file="SetAppConfigKey.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.IO;
using System.Threading.Tasks;
using Azure.ApplicationModel.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Mmm.Iot.Functions.AppConfiguration
{
    public static class SetAppConfigKey
    {
        [FunctionName("SetAppConfigKey")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", "delete", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            string connectionstring = req.Query["connectionstring"];
            string name = req.Query["name"];
            string value = req.Query["value"];
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            if (req.Method.Equals("POST"))
            {
                connectionstring = connectionstring ?? data?.connectionstring;
                name = name ?? data?.name;
                value = value ?? data?.value;
                Azure.Response<ConfigurationSetting> res;
                try
                {
                    var client = new ConfigurationClient(connectionstring);
                    var setting = new ConfigurationSetting(name, value);
                    res = client.Set(setting);
                }
                catch (NullReferenceException ex)
                {
                    log.LogError("Object reference not set to an instance of an object. Can be due to incorrect connection string.\n" + ex);
                    return new BadRequestObjectResult("Object reference not set to an instance of an object. Can be due to incorrect connection string.\n" + ex);
                }
                catch (Exception ex)
                {
                    log.LogError(ex.ToString());
                    return new BadRequestObjectResult(ex);
                }

                return new OkObjectResult($"{req.Method} AppConfig {name} \n {res}");
            }

            if (req.Method.Equals("DELETE"))
            {
                connectionstring = connectionstring ?? data?.connectionstring;
                name = name ?? data?.name;
                Azure.Response res;
                try
                {
                    var client = new ConfigurationClient(connectionstring);
                    res = client.Delete(name);
                }
                catch (NullReferenceException ex)
                {
                    log.LogError("Object reference not set to an instance of an object. Can be due to incorrect connection string.\n" + ex);
                    return new BadRequestObjectResult("Object reference not set to an instance of an object. Can be due to incorrect connection string.\n" + ex);
                }
                catch (Exception ex)
                {
                    log.LogError(ex.ToString());
                    return new BadRequestObjectResult(ex);
                }

                return new OkObjectResult($"{req.Method} AppConfig {name} \n {res}");
            }

            log.LogError("Bad Request");
            return new BadRequestResult();
        }
    }
}