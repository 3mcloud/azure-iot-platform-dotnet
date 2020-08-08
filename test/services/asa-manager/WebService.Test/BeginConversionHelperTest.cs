// <copyright file="BeginConversionHelperTest.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using Microsoft.Extensions.Logging;
using Mmm.Iot.AsaManager.Services.External.IotHubManager;
using Mmm.Iot.AsaManager.Services.Models.IotHub;
using Mmm.Iot.AsaManager.WebService.Helpers;
using Mmm.Iot.Common.Services.Models;
using Mmm.Iot.Common.Services.Wrappers;
using Mmm.Iot.Common.TestHelpers;
using Moq;
using Xunit;

namespace Mmm.IoT.AsaManager.WebService.Test
{
    public class BeginConversionHelperTest
    {
        private readonly Mock<IIotHubManagerClient> mockIotHubManagerClient;
        private readonly Mock<ILogger<BeginConversionHelper>> mockLogger;
        private readonly Mock<IKeyGenerator> mockKeyGen;
        private readonly Mock<ConvertAsync> mockConversion;
        private readonly IBeginConversionHelper conversionHelper;
        private readonly Random rand;

        public BeginConversionHelperTest()
        {
            this.mockIotHubManagerClient = new Mock<IIotHubManagerClient>();
            this.mockLogger = new Mock<ILogger<BeginConversionHelper>>();
            this.mockKeyGen = new Mock<IKeyGenerator>();
            this.mockConversion = new Mock<ConvertAsync>();
            this.conversionHelper = new BeginConversionHelper(
                this.mockIotHubManagerClient.Object,
                this.mockLogger.Object,
                this.mockKeyGen.Object);
            this.rand = new Random();
        }

        [Fact]
        public void BeginConversionTest()
        {
            var operationId = this.rand.NextString();

            this.mockKeyGen
                .Setup(
                    x => x.Generate())
                .Returns(operationId);

            var responseOperationId = this.conversionHelper.BeginConversion(this.mockConversion.Object, this.rand.NextString());

            this.mockKeyGen
                .Verify(
                    x => x.Generate(),
                    Times.Once);
            this.mockConversion
                .Verify(
                    x => x(
                        It.IsAny<string>(),
                        It.Is<string>(s => s == operationId)),
                    Times.Once);

            Assert.Equal(operationId, responseOperationId);
        }

        [Fact]
        public void BeginConversionTestOnBackgroundFailure()
        {
            var operationId = this.rand.NextString();

            this.mockKeyGen
                .Setup(
                    x => x.Generate())
                .Returns(operationId);
            this.mockConversion
                .Setup(
                    x => x(
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                .ThrowsAsync(new Exception());

            var responseOperationId = this.conversionHelper.BeginConversion(this.mockConversion.Object, this.rand.NextString());

            this.mockKeyGen
                .Verify(
                    x => x.Generate(),
                    Times.Once);
            this.mockConversion
                .Verify(
                    x => x(
                        It.IsAny<string>(),
                        It.Is<string>(s => s == operationId)),
                    Times.Once);

            Assert.Equal(operationId, responseOperationId);
        }

        [Fact]
        public void BeginConversionTestOnIotHubJobComplete()
        {
            var operationId = this.rand.NextString();
            var jobId = this.rand.NextString();

            this.mockKeyGen
                .Setup(
                    x => x.Generate())
                .Returns(operationId);
            this.mockIotHubManagerClient
                .Setup(
                    x => x.GetJobAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                .ReturnsAsync(
                    new JobModel
                    {
                        JobId = jobId,
                        Status = JobStatus.Completed,
                    });

            var responseOperationId = this.conversionHelper.WatchIotHubJobAndBeginConversion(
                jobId,
                this.mockConversion.Object,
                this.rand.NextString());

            this.mockKeyGen
                .Verify(
                    x => x.Generate(),
                    Times.Once);
            this.mockIotHubManagerClient
                .Verify(
                    x => x.GetJobAsync(
                        It.Is<string>(s => s == jobId),
                        It.IsAny<string>()),
                    Times.AtLeastOnce);
            this.mockConversion
                .Verify(
                    x => x(
                        It.IsAny<string>(),
                        It.Is<string>(s => s == operationId)),
                    Times.Once);

            Assert.Equal(operationId, responseOperationId);
        }

        [Fact]
        public void BeginConversionTestOnIotHubJobFailed()
        {
            var operationId = this.rand.NextString();
            var jobId = this.rand.NextString();

            this.mockKeyGen
                .Setup(
                    x => x.Generate())
                .Returns(operationId);
            this.mockIotHubManagerClient
                .Setup(
                    x => x.GetJobAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>()))
                .ReturnsAsync(
                    new JobModel
                    {
                        JobId = jobId,
                        Status = JobStatus.Failed,
                    });

            var responseOperationId = this.conversionHelper.WatchIotHubJobAndBeginConversion(
                jobId,
                this.mockConversion.Object,
                this.rand.NextString());

            this.mockKeyGen
                .Verify(
                    x => x.Generate(),
                    Times.Once);
            this.mockIotHubManagerClient
                .Verify(
                    x => x.GetJobAsync(
                        It.Is<string>(s => s == jobId),
                        It.IsAny<string>()),
                    Times.AtLeastOnce);
            this.mockConversion
                .Verify(
                    x => x(
                        It.IsAny<string>(),
                        It.Is<string>(s => s == operationId)),
                    Times.Never);

            Assert.Equal(operationId, responseOperationId);
        }
    }
}