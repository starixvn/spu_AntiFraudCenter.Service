namespace Fanex.NAP.SPU.AFC.Tests.Features.SystemParameter
{
    using Fanex.NAP.SPU.AFC.CC.Features.SystemParameter;
    using Fanex.NAP.SPU.AFC.Features.SystemParameter;
    using FluentAssertions;
    using NSubstitute;
    using System;
    using Xunit;
    using static Fanex.NAP.SPU.AFC.Shares.Consts;

    public class SystemParameterTests
    {
        private readonly ISystemParameterDataAccess systemParameterDataAccess;
        private readonly ISystemParameterServices systemParameterServices;

        public SystemParameterTests()
        {
            systemParameterDataAccess = Substitute.For<ISystemParameterDataAccess>();

            systemParameterServices = new SystemParameterServices(systemParameterDataAccess);
        }

        [Theory]
        [InlineData(SystemParameters.MatchMonitorLastScannedTrans)]
        public void GetLastScannedId_WhenSuccess_ReturnLastScannedId(SystemParameters paraID)
        {
            var output = new SystemParameterEntity
            {
                ParameterDataType = "BIGINT UNSIGNED",
                ParameterValue = "212121"
            };

            systemParameterDataAccess.GetPreviousTimingExecution(paraID).Returns(output);

            var result = systemParameterServices.GetLastScannedId(paraID);

            result.Should().Be(Int64.Parse(output.ParameterValue));
            result.Should().NotBe(null);
        }

        [Theory]
        [InlineData(SystemParameters.ProblemAccountLastScannedTime)]
        [InlineData(SystemParameters.RobotAccountLastScannedTime)]
        public void GetLastScannedTime_WhenSuccess_ReturnLastScannedTime(SystemParameters paraID)
        {
            var output = new SystemParameterEntity
            {
                ParameterDataType = "DATETIME",
                ParameterValue = "2021-07-05 22:50:55"
            };

            systemParameterDataAccess.GetPreviousTimingExecution(paraID).Returns(output);

            var result = systemParameterServices.GetLastScannedDateTime(paraID);

            result.Should().Be(DateTime.Parse(output?.ParameterValue));
        }

        [Theory]
        [InlineData(SystemParameters.ProblemAccountLastScannedTime, "2021-07-05 22:50:53")]
        public void UpdateTimingExecution_WhenSuccess(SystemParameters paraId, string lastScannedTime)
        {
            systemParameterDataAccess.UpdateTimingExecution(lastScannedTime, paraId);

            bool isSuccess = systemParameterServices.UpdateTimingExecution(lastScannedTime, paraId);

            isSuccess.Should().BeTrue();
        }
    }
}