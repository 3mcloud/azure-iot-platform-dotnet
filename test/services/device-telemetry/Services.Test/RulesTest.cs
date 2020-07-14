// <copyright file="RulesTest.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Mmm.Iot.Common.Services.Config;
using Mmm.Iot.Common.Services.Exceptions;
using Mmm.Iot.Common.Services.External.AsaManager;
using Mmm.Iot.Common.Services.External.StorageAdapter;
using Mmm.Iot.Common.Services.Http;
using Mmm.Iot.Common.Services.Models;
using Mmm.Iot.Common.TestHelpers;
using Mmm.Iot.DeviceTelemetry.Services.External;
using Moq;
using Newtonsoft.Json;
using Xunit;
using HttpRequest = Mmm.Iot.Common.Services.Http.HttpRequest;
using HttpResponse = Mmm.Iot.Common.Services.Http.HttpResponse;

namespace Mmm.Iot.DeviceTelemetry.Services.Test
{
    public class RulesTest
    {
        private const string TenantId = "test_tenant";
        private const int Limit = 1000;
        private readonly Mock<IStorageAdapterClient> storageAdapter;
        private readonly Mock<IAsaManagerClient> asaManager;
        private readonly Mock<ILogger<Rules>> logger;
        private readonly AppConfig config;
        private readonly Mock<IRules> rulesMock;
        private readonly Mock<IAlarms> alarms;
        private readonly Mock<IHttpClient> httpClientMock;
        private readonly IRules rules;
        private readonly IDiagnosticsClient diagnosticsClient;
        private readonly Mock<IHttpContextAccessor> httpContextAccessor;

        public RulesTest()
        {
            this.storageAdapter = new Mock<IStorageAdapterClient>();
            this.asaManager = new Mock<IAsaManagerClient>();
            this.logger = new Mock<ILogger<Rules>>();
            this.config = new AppConfig
            {
                ExternalDependencies = new ExternalDependenciesConfig
                {
                    DiagnosticsServiceUrl = "http://localhost:9006/v1",
                    DiagnosticsMaxLogRetries = 3,
                },
            };
            this.rulesMock = new Mock<IRules>();
            this.alarms = new Mock<IAlarms>();
            this.httpClientMock = new Mock<IHttpClient>();
            this.httpContextAccessor = new Mock<IHttpContextAccessor>();
            this.diagnosticsClient = new DiagnosticsClient(this.httpClientMock.Object, this.config, new Mock<ILogger<DiagnosticsClient>>().Object, this.httpContextAccessor.Object);

            this.httpContextAccessor
                .Setup(t => t.HttpContext.Request.HttpContext.Items)
                .Returns(new Dictionary<object, object>()
                    {
                        { "TenantID", TenantId },
                    });
            this.asaManager
                .Setup(t => t.BeginRulesConversionAsync())
                .ReturnsAsync(new BeginConversionApiModel());

            this.rules = new Rules(this.storageAdapter.Object, this.asaManager.Object, this.logger.Object, this.alarms.Object, this.diagnosticsClient);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task InitialListIsEmptyAsync()
        {
            // Arrange
            this.ThereAreNoRulessInStorage();

            // Act
            var list = await this.rulesMock.Object.GetListAsync(null, 0, Limit, null, false);

            // Assert
            Assert.Empty(list);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task GetListWithValuesAsync()
        {
            // Arrange
            this.ThereAreSomeRulesInStorage();

            // Act
            var list = await this.rulesMock.Object.GetListAsync(null, 0, Limit, null, false);

            // Assert
            Assert.NotEmpty(list);

            foreach (Rule rule in list)
            {
                Assert.NotNull(rule.Actions);
            }
        }

        /**
         * Verify call to delete on non-deleted rule will get and update rule as expected.
         */
        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task VerifyBasicDeleteAsync()
        {
            // Arrange
            Rule test = new Rule
            {
                Enabled = true,
                Deleted = false,
            };

            this.SetUpStorageAdapterGet(test);

            // Act
            await this.rules.DeleteAsync("id");

            this.storageAdapter.Verify(x => x.GetAsync(It.IsAny<string>(), "id"), Times.Once);
            this.storageAdapter.Verify(x => x.UpdateAsync(It.IsAny<string>(), "id", It.IsAny<string>(), "123"), Times.Once);

            this.asaManager
                .Verify(
                    x => x.BeginRulesConversionAsync(),
                    Times.Once);
        }

        /**
         * If rule is already deleted and delete is called, verify it will not throw exception
         */
        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task VerifyDeleteDoesNotFailIfAlreadyDeletedAsync()
        {
            // Arrange
            Rule test = new Rule
            {
                Enabled = false,
                Deleted = true,
            };

            this.SetUpStorageAdapterGet(test);

            // Act
            await this.rules.DeleteAsync("id");

            this.storageAdapter.Verify(x => x.GetAsync(It.IsAny<string>(), "id"), Times.Once);
            this.storageAdapter.Verify(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            this.asaManager
                .Verify(
                    x => x.BeginRulesConversionAsync(),
                    Times.Never);
        }

        /**
         * If rule does not exist and delete is called, verify it will not throw exception
         */
        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task VerifyDeleteDoesNotFailIfRuleNotExistsAsync()
        {
            // Arrange
            Rule test = new Rule
            {
                Enabled = false,
                Deleted = true,
            };

            this.storageAdapter
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new ResourceNotFoundException());

            // Act
            await this.rules.DeleteAsync("id");

            this.storageAdapter.Verify(x => x.GetAsync(It.IsAny<string>(), "id"), Times.Once);
            this.storageAdapter.Verify(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            this.asaManager
                .Verify(
                    x => x.BeginRulesConversionAsync(),
                    Times.Never);
        }

        /** If get rule throws an exception that is not a resource not found exception,
         * delete should throw that exception.
         */
        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task VerifyDeleteFailsIfGetRuleThrowsException()
        {
            // Arrange
            Rule test = new Rule
            {
                Enabled = false,
                Deleted = true,
            };

            this.storageAdapter
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception());

            // Act
            await Assert.ThrowsAsync<Exception>(async () => await this.rules.DeleteAsync("id"));

            this.storageAdapter.Verify(x => x.GetAsync(It.IsAny<string>(), "id"), Times.Once);
            this.storageAdapter.Verify(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            this.asaManager
                .Verify(
                    x => x.BeginRulesConversionAsync(),
                    Times.Never);
        }

        /**
         * If upsert is called on a deleted rule, verify a NotFoundException will be thrown.
         */
        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task VerifyCannotUpdateDeletedRuleAsync()
        {
            // Arrange
            Rule test = new Rule
            {
                Enabled = false,
                Deleted = true,
                Id = "id",
                ETag = "123",
            };
            this.SetUpStorageAdapterGet(test);

            // Act
            await Assert.ThrowsAsync<ResourceNotFoundException>(async () => await this.rules.UpsertIfNotDeletedAsync(test));

            // Assert
            this.storageAdapter.Verify(x => x.GetAsync(It.IsAny<string>(), "id"), Times.Once);
            this.storageAdapter.Verify(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            this.asaManager
                .Verify(
                    x => x.BeginRulesConversionAsync(),
                    Times.Never);
        }

        /**
         * If GetListAsync() is called with includeDeleted = false, verify no
         * deleted rules will be returned
         */
        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task VerifyGetBehaviorIfDontIncludeDeleted()
        {
            // Arrange
            Rule test = new Rule
            {
                Enabled = false,
                Deleted = true,
                Id = "id",
                ETag = "123",
            };
            string ruleString = JsonConvert.SerializeObject(test);
            ValueApiModel model = new ValueApiModel
            {
                Data = ruleString,
                ETag = "123",
                Key = "id",
            };
            ValueListApiModel result = new ValueListApiModel();
            result.Items = new List<ValueApiModel> { model };
            this.storageAdapter.Setup(x => x.GetAllAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(result));

            // Act
            List<Rule> rulesList = await this.rules.GetListAsync("asc", 0, Limit, null, false);

            // Assert
            Assert.Empty(rulesList);
            this.storageAdapter.Verify(x => x.GetAllAsync(It.IsAny<string>()), Times.Once);
        }

        /**
         * If GetListAsync() is called with includeDeleted = true, verify
         * deleted rules will be returned
         */
        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task VerifyGetBehaviorIfDoIncludeDeleted()
        {
            // Arrange
            Rule test = new Rule
            {
                Enabled = false,
                Deleted = true,
                Id = "id",
                ETag = "123",
            };
            string ruleString = JsonConvert.SerializeObject(test);
            ValueApiModel model = new ValueApiModel
            {
                Data = ruleString,
                ETag = "123",
                Key = "id",
            };
            ValueListApiModel result = new ValueListApiModel();
            result.Items = new List<ValueApiModel> { model };
            this.storageAdapter.Setup(x => x.GetAllAsync(It.IsAny<string>()))
                .Returns(Task.FromResult(result));

            // Act
            List<Rule> rulesList = await this.rules.GetListAsync("asc", 0, Limit, null, true);

            // Assert
            Assert.Single(rulesList);
            this.storageAdapter.Verify(x => x.GetAllAsync(It.IsAny<string>()), Times.Once);
        }

        /**
          * If upsert is called with a rule that is not created and a
          * specified Id, it should be created with that Id.
        */
        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task UpsertNewRuleWithId_CreatesNewRuleWithId()
        {
            // Arrange
            string newRuleId = "TESTRULEID" + DateTime.Now.ToString("yyyyMMddHHmmss");
            Rule test = new Rule
            {
                Enabled = true,
                Id = newRuleId,
            };

            string ruleString = JsonConvert.SerializeObject(test);

            ValueApiModel result = new ValueApiModel
            {
                Data = ruleString,
                ETag = "1234",
                Key = newRuleId,
            };

            this.storageAdapter
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new ResourceNotFoundException());

            this.storageAdapter.Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(result));

            // Act
            Rule rule = await this.rules.UpsertIfNotDeletedAsync(test);

            // Assert
            Assert.Equal(newRuleId, rule.Id);
        }

        /**
        * On creating a new rule, new rule id should be returned
        */
        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task CreateNewRule_ReturnsNewId()
        {
            // Arrange
            Rule test = new Rule
            {
                Enabled = true,
            };

            string newRuleId = "TESTRULEID" + DateTime.Now.ToString("yyyyMMddHHmmss");
            Rule resultRule = new Rule
            {
                Enabled = true,
                Id = newRuleId,
            };

            string ruleString = JsonConvert.SerializeObject(resultRule);

            ValueApiModel result = new ValueApiModel
            {
                Data = ruleString,
                ETag = "1234",
                Key = newRuleId,
            };

            this.storageAdapter.Setup(x => x.CreateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(result));

            // Act
            Rule rule = await this.rules.CreateAsync(test);

            // Assert
            Assert.Equal(newRuleId, rule.Id);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task DeleteRule_QueriesRuleCountAndLogs()
        {
            // Arrange
            ValueListApiModel fakeRules = new ValueListApiModel();
            fakeRules.Items = new List<ValueApiModel>();
            fakeRules.Items.Add(this.CreateFakeRule("rule1"));
            fakeRules.Items.Add(this.CreateFakeRule("rule2"));
            this.storageAdapter.Setup(x => x.GetAllAsync(It.IsAny<string>())).Returns(Task.FromResult(fakeRules));
            IHttpResponse fakeOkResponse = new HttpResponse(HttpStatusCode.OK, string.Empty, null);
            this.httpClientMock.Setup(x => x.PostAsync(It.IsAny<HttpRequest>())).ReturnsAsync(fakeOkResponse);

            Rule test = new Rule
            {
                Enabled = true,
                Deleted = false,
            };

            this.SetUpStorageAdapterGet(test);

            // Act
            await this.rules.DeleteAsync("id");

            // Assert
            this.storageAdapter.Verify(x => x.GetAllAsync(It.IsAny<string>()), Times.Once);
            this.httpClientMock.Verify(x => x.PostAsync(It.IsAny<HttpRequest>()), Times.Exactly(2));

            this.asaManager
                .Verify(
                    x => x.BeginRulesConversionAsync(),
                    Times.Once);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task DeleteRule_RetriesLogOnError()
        {
            // Arrange
            ValueListApiModel fakeRules = new ValueListApiModel();
            fakeRules.Items = new List<ValueApiModel>();
            fakeRules.Items.Add(this.CreateFakeRule("rule1"));
            fakeRules.Items.Add(this.CreateFakeRule("rule2"));
            this.storageAdapter.Setup(x => x.GetAllAsync(It.IsAny<string>())).Returns(Task.FromResult(fakeRules));
            this.httpClientMock.SetupSequence(x => x.PostAsync(It.IsAny<HttpRequest>()))
                .Throws<Exception>()
                .ReturnsAsync(new HttpResponse(HttpStatusCode.ServiceUnavailable, string.Empty, null))
                .ReturnsAsync(new HttpResponse(HttpStatusCode.OK, string.Empty, null))
                .ReturnsAsync(new HttpResponse(HttpStatusCode.OK, string.Empty, null));

            Rule test = new Rule
            {
                Enabled = true,
                Deleted = false,
            };

            this.SetUpStorageAdapterGet(test);

            // Act
            await this.rules.DeleteAsync("id");

            // Assert
            this.storageAdapter.Verify(x => x.GetAllAsync(It.IsAny<string>()), Times.Once);
            this.httpClientMock.Verify(x => x.PostAsync(It.IsAny<HttpRequest>()), Times.Exactly(4));

            this.asaManager
                .Verify(
                    x => x.BeginRulesConversionAsync(),
                    Times.Once);
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public async Task ThrowsOnInvalidInput()
        {
            // Arrange
            var xssString = "<body onload=alert('test1')>";
            var xssList = new List<string>
            {
                "<body onload=alert('test1')>",
                "<IMG SRC=j&#X41vascript:alert('test2')>",
            };

            var rule = new Rule()
            {
                ETag = xssString,
                Id = xssString,
                Name = xssString,
                DateCreated = xssString,
                DateModified = xssString,
                Enabled = true,
                Description = xssString,
                GroupId = xssString,
                Severity = SeverityType.Critical,
                Conditions = new List<Condition>
                {
                    new Condition()
                    {
                        Field = "sample_conddition",
                        Operator = RuleConditionOperatorType.Equals,
                        Value = "1",
                    },
                },
                Actions = new List<IAction>
                {
                    new EmailAction(
                        new Dictionary<string, object>
                        {
                            { "recipients", new Newtonsoft.Json.Linq.JArray() { "sampleEmail@gmail.com", "sampleEmail2@gmail.com" } },
                            { "subject", "Test Email" },
                            { "notes", "Test Email Notes." },
                        }),
                },
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidInputException>(async () => await this.rules.DeleteAsync(xssString));
            await Assert.ThrowsAsync<InvalidInputException>(async () => await this.rules.DeleteAsync(xssString));
            await Assert.ThrowsAsync<InvalidInputException>(async () => await this.rules.GetAsync(xssString));
            await Assert.ThrowsAsync<InvalidInputException>(async () => await this.rules.GetListAsync(xssString, 0, 1, xssString, false));
            await Assert.ThrowsAsync<InvalidInputException>(async () => await this.rules.GetAlarmCountForListAsync(null, null, xssString, 0, Limit, xssList.ToArray()));
            await Assert.ThrowsAsync<InvalidInputException>(async () => await this.rules.CreateAsync(rule));
            await Assert.ThrowsAsync<InvalidInputException>(async () => await this.rules.UpsertIfNotDeletedAsync(rule));
        }

        [Fact]
        [Trait(Constants.Type, Constants.UnitTest)]
        public void InputValidationPassesWithValidRule()
        {
            // Arrange
            this.ThereAreSomeRulesInStorage();

            List<Rule> rulesList = this.GetSampleRulesList();

            // Act & Assert
            foreach (var rule in rulesList)
            {
                rule.Validate();
            }
        }

        private void ThereAreNoRulessInStorage()
        {
            this.rulesMock.Setup(x => x.GetListAsync(null, 0, Limit, null, false))
                .ReturnsAsync(new List<Rule>());
        }

        private void ThereAreSomeRulesInStorage()
        {
            var sampleRules = this.GetSampleRulesList();

            this.rulesMock.Setup(x => x.GetListAsync(null, 0, Limit, null, false))
                .ReturnsAsync(sampleRules);
        }

        private List<Rule> GetSampleRulesList()
        {
            var sampleConditions = new List<Condition>
            {
                new Condition()
                {
                    Field = "sample_conddition",
                    Operator = RuleConditionOperatorType.Equals,
                    Value = "1",
                },
            };

            var sampleActions = new List<IAction>
            {
                new EmailAction(
                    new Dictionary<string, object>
                    {
                        { "recipients", new Newtonsoft.Json.Linq.JArray() { "sampleEmail@gmail.com", "sampleEmail2@gmail.com" } },
                        { "subject", "Test Email" },
                        { "notes", "Test Email Notes." },
                    }),
            };

            var sampleRules = new List<Rule>
            {
                new Rule()
                {
                    Name = "Sample 1",
                    Enabled = true,
                    Description = "Sample description 1 -- Pressure >= 298",
                    GroupId = "Prototyping devices",
                    Severity = SeverityType.Critical,
                    Conditions = sampleConditions,
                    Actions = sampleActions,
                },
                new Rule()
                {
                    Name = "Sample 2",
                    Enabled = true,
                    Description = "Sample description 2",
                    GroupId = "Prototyping devices",
                    Severity = SeverityType.Warning,
                    Conditions = sampleConditions,
                    Actions = sampleActions,
                },
                new Rule()
                {
                    ETag = "*",
                    Name = "Sample 3",
                    Enabled = true,
                    Calculation = CalculationType.Instant,
                    Description = "Sample description 2.",
                    GroupId = "Chillers",
                    Severity = SeverityType.Warning,
                    Conditions = sampleConditions,
                    Actions = sampleActions,
                },
            };

            return sampleRules;
        }

        /**
         * Set up storage adapater to return given rule as part of ValueApiModel on GetAsync
         */
        private void SetUpStorageAdapterGet(Rule toReturn)
        {
            string ruleString = JsonConvert.SerializeObject(toReturn);
            ValueApiModel result = new ValueApiModel
            {
                Data = ruleString,
                ETag = "123",
                Key = "id",
            };

            this.storageAdapter
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(result));
        }

        private ValueApiModel CreateFakeRule(string ruleId)
        {
            Rule test = new Rule
            {
                Enabled = true,
                Id = ruleId,
            };

            string ruleString = JsonConvert.SerializeObject(test);

            return new ValueApiModel
            {
                Data = ruleString,
                ETag = "1234",
                Key = ruleId,
            };
        }
    }
}