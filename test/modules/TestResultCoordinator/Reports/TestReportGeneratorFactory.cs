// Copyright (c) Microsoft. All rights reserved.
namespace TestResultCoordinator.Reports
{
    using System;
    using Microsoft.Azure.Devices.Edge.ModuleUtil;
    using Microsoft.Azure.Devices.Edge.Util;
    using TestResultCoordinator.Storage;

    class TestReportGeneratorFactory : ITestReportGeneratorFactory
    {
        const int BatchSize = 500;
        readonly ITestOperationResultStorage storage;

        internal TestReportGeneratorFactory(ITestOperationResultStorage storage)
        {
            this.storage = Preconditions.CheckNotNull(storage, nameof(storage));
        }

        public ITestResultReportGenerator Create(
            string trackingId,
            ITestReportMetadata testReportMetadata)
        {
            Preconditions.CheckNonWhiteSpace(trackingId, nameof(trackingId));
            Preconditions.CheckNotNull(testReportMetadata, nameof(testReportMetadata));

            switch (testReportMetadata.TestReportType)
            {
                case TestReportType.CountingReport:
                {
                    var metadata = (CountingReportMetadata)testReportMetadata;
                    var expectedTestResults = this.GetResults(metadata.ExpectedSource);
                    var actualTestResults = this.GetResults(metadata.ActualSource);

                    return new CountingReportGenerator(
                        trackingId,
                        metadata.ExpectedSource,
                        expectedTestResults,
                        metadata.ActualSource,
                        actualTestResults,
                        testReportMetadata.TestOperationResultType.ToString(),
                        new SimpleTestOperationResultComparer());
                }

                case TestReportType.TwinCountingReport:
                {
                    var metadata = (TwinCountingReportMetadata)testReportMetadata;
                    var expectedTestResults = this.GetTwinExpectedResults(metadata);
                    var actualTestResults = this.GetResults(metadata.ActualSource);

                    return new TwinCountingReportGenerator(
                        trackingId,
                        metadata.ExpectedSource,
                        expectedTestResults,
                        metadata.ActualSource,
                        actualTestResults,
                        testReportMetadata.TestOperationResultType.ToString(),
                        new SimpleTestOperationResultComparer());
                }

                case TestReportType.DeploymentTestReport:
                {
                        var metadata = (DeploymentTestReportMetadata)testReportMetadata;
                        var expectedTestResults = this.GetResults(metadata.ExpectedSource);
                        var actualTestResults = this.GetResults(metadata.ActualSource);

                        return new DeploymentTestReportGenerator(
                        trackingId,
                        metadata.ExpectedSource,
                        expectedTestResults,
                        metadata.ActualSource,
                        actualTestResults);
                }

                default:
                {
                    throw new NotSupportedException($"Report type {testReportMetadata.TestReportType} is not supported.");
                }
            }
        }

        ITestResultCollection<TestOperationResult> GetResults(string resultSource)
        {
            return new StoreTestResultCollection<TestOperationResult>(
                this.storage.GetStoreFromSource(resultSource),
                BatchSize);
        }

        ITestResultCollection<TestOperationResult> GetTwinExpectedResults(TwinCountingReportMetadata reportMetadata)
        {
            if (reportMetadata == null)
            {
                throw new NotSupportedException($"Report type {reportMetadata.TestReportType} requires TwinReportMetadata instead of {reportMetadata.GetType()}");
            }

            if (reportMetadata.TwinTestPropertyType == TwinTestPropertyType.Desired)
            {
                return this.GetResults(reportMetadata.ExpectedSource);
            }

            string[] sources = reportMetadata.ExpectedSource.Split('.');
            string moduleId = sources.Length > 0 ? sources[0] : Settings.Current.ModuleId;
            return new CloudTwinTestResultCollection(reportMetadata.ExpectedSource, Settings.Current.IoTHubConnectionString, moduleId, Settings.Current.TrackingId);
        }
    }
}
