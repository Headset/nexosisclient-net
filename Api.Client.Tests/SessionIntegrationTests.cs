using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Nexosis.Api.Client;
using Nexosis.Api.Client.Model;
using Xunit;

namespace Api.Client.Tests
{
    public class SessionIntegrationTests : IClassFixture<IntegrationTestFixture>
    {
        private readonly IntegrationTestFixture fixture;
        private readonly string productFilePath = Path.Combine(new DirectoryInfo(AppContext.BaseDirectory).Parent.Parent.Parent.FullName, @"CsvFiles\producttest.csv");

        public SessionIntegrationTests(IntegrationTestFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task GetBalanceWillGiveItBack()
        {
            var actual = await fixture.Client.GetAccountBalance();
            Assert.NotNull(actual.Balance);
            Assert.Equal(0, actual.Cost.Amount);
            Assert.Equal("USD", actual.Balance.Currency);
        }

        [Fact]
        public async Task CreateForecastWithCsvStartsNewSession()
        {  
            using (var file = File.OpenText(productFilePath))
            {
                var actual = await fixture.Client.Sessions.CreateForecast(file, "sales", DateTimeOffset.Parse("2017-03-25 -0:00"), DateTimeOffset.Parse("2017-04-25 -0:00"));
                Assert.NotNull(actual.SessionId);
            }
        }

        [Fact]
        public async Task CreateForecastWithDataDirectlyStartsNewSession()
        {
            var dataSet = DataSetGenerator.Run(DateTime.Parse("2016-08-01"), DateTime.Parse("2017-03-26"), "instances");
            var actual = await fixture.Client.Sessions.CreateForecast(dataSet, "instances", DateTimeOffset.Parse("2017-03-26"), DateTimeOffset.Parse("2017-04-25") );
            Assert.NotNull(actual.SessionId);
        }

        [Fact]
        public async Task ForcastFromSavedDataSetStartsNewSession()
        {
            var dataSetName = $"testDataSet-{DateTime.Now:s}";
            var dataSet = DataSetGenerator.Run(DateTime.Parse("2016-08-01"), DateTime.Parse("2017-03-26"), "instances");
            await fixture.Client.DataSets.Create(dataSetName, dataSet);

            var actual = await fixture.Client.Sessions.CreateForecast(dataSetName, "instances", DateTimeOffset.Parse("2017-03-26"), DateTimeOffset.Parse("2017-04-25") );
            Assert.NotNull(actual.SessionId);
        } 

        [Fact]
        public async Task StartImpactWithCsvStartsNewSession()
        {  
            using (var file = File.OpenText(productFilePath))
            {
                var actual = await fixture.Client.Sessions.AnalyzeImpact(file, "super-duper-sale", "sales", DateTimeOffset.Parse("2016-11-25 -0:00"), DateTimeOffset.Parse("2016-12-25 -0:00"));
                Assert.NotNull(actual.SessionId);
            }
        }

        [Fact]
        public async Task StartImpactWithDataDirectlyStartsNewSession()
        {
            var dataSet = DataSetGenerator.Run(DateTime.Parse("2016-08-01"), DateTime.Parse("2017-03-26"), "instances");
            var actual = await fixture.Client.Sessions.AnalyzeImpact(dataSet, $"charlie-delta-{DateTime.UtcNow:s}", "instances", DateTimeOffset.Parse("2016-11-26"), DateTimeOffset.Parse("2016-12-25") );
            Assert.NotNull(actual.SessionId);
        }

        [Fact]
        public async Task StartImpactFromSavedDataSetStartsNewSession()
        {
            var dataSetName = $"testDataSet-{DateTime.UtcNow:s}";
            var dataSet = DataSetGenerator.Run(DateTime.Parse("2016-08-01"), DateTime.Parse("2017-03-26"), "instances");
            await fixture.Client.DataSets.Create(dataSetName, dataSet);

            var actual = await fixture.Client.Sessions.CreateForecast(dataSetName, "instances", DateTimeOffset.Parse("2017-03-26"), DateTimeOffset.Parse("2017-04-25") );
            Assert.NotNull(actual.SessionId);
        }

        [Fact]
        public async Task GetSessionListHasItems()
        {
            var sessions = await fixture.Client.Sessions.List();
            Assert.True(sessions.Count > 0);
        }
/*
        [Fact]
        public async Task GetSessionResultsHasResults()
        {
            var dataSet = DataSetGenerator.Run(DateTime.Parse("2016-08-01"), DateTime.Parse("2017-03-26"), "instances");
            var session = await fixture.Client.Sessions.CreateForecast(dataSet, "instances", DateTimeOffset.Parse("2017-03-26"), DateTimeOffset.Parse("2017-04-25") );

            var totalDelay = 0;
            while (session.Status != SessionStatus.Completed || session.Status != SessionStatus.Failed || totalDelay > 30_000)
            {
                await Task.Delay(5000);
                totalDelay += 5000;

                session = await fixture.Client.Sessions.Get(session.SessionId);
            }

            var results = await fixture.Client.Sessions.GetResults(session.SessionId);

            Assert.NotNull(results);
            Assert.True(results.Data.Count > 0);
        }

        [Fact]
        public async Task GetSessionResultsWillWriteFile()
        {
            var sessions = await fixture.Client.Sessions.List();
            var session = sessions.FirstOrDefault(s => s.Status == SessionStatus.Completed);

            var filename = Path.Combine(AppContext.BaseDirectory, $"test-ouput-{DateTime.UtcNow:yyyyMMddhhmmss}.csv");
            try
            {
                using (var output = new StreamWriter(File.OpenWrite(filename)))
                {
                    await fixture.Client.Sessions.GetResults(session.SessionId, output);
                }

                var results = File.ReadAllText(filename);

                Assert.True(results.Length > 0);
                Assert.StartsWith("timestamp,", results);
            }
            finally
            {
                if (File.Exists(filename))
                    File.Delete(filename);
            }
        }
        */

        [Fact]
        public async Task DeletingSessionThenQueryingReturns404()
        {
            var dataSet = DataSetGenerator.Run(DateTime.Parse("2016-08-01"), DateTime.Parse("2017-03-26"), "instances");
            var actual = await fixture.Client.Sessions.AnalyzeImpact(dataSet, $"charlie-delta-{DateTime.UtcNow:s}", "instances", DateTimeOffset.Parse("2016-11-26"), DateTimeOffset.Parse("2016-12-25") );

            await fixture.Client.Sessions.Remove(actual.SessionId);
            var exception = await Assert.ThrowsAsync<NexosisClientException>(async () => await fixture.Client.Sessions.Get(actual.SessionId));

            Assert.Equal(exception.StatusCode, HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task CheckingSessionStatusReturnsExpcetedValue()
        {
            var dataSet = DataSetGenerator.Run(DateTime.Parse("2016-08-01"), DateTime.Parse("2017-03-26"), "instances");
            var actual = await fixture.Client.Sessions.EstimateImpact(dataSet, $"charlie-delta-{DateTime.UtcNow:s}", "instances", DateTimeOffset.Parse("2016-11-26"), DateTimeOffset.Parse("2016-12-25") );

            var status = await fixture.Client.Sessions.GetStatus(actual.SessionId);

            Assert.Equal(actual.Status, status.Status);
        }

        [Fact]
        public async Task CanRemoveMultipleSessions()
        {
            var dataSet = DataSetGenerator.Run(DateTime.Parse("2016-08-01"), DateTime.Parse("2017-03-26"), "instances");
            var first = await fixture.Client.Sessions.AnalyzeImpact(dataSet, "juliet-juliet-echo-1", "instances", DateTimeOffset.Parse("2016-11-26"), DateTimeOffset.Parse("2016-12-25") );
            var second = await fixture.Client.Sessions.AnalyzeImpact(dataSet, "juliet-juliet-echo-2", "instances", DateTimeOffset.Parse("2016-11-26"), DateTimeOffset.Parse("2016-12-25") );

            await fixture.Client.Sessions.Remove(null, "juliet-juliet-echo-", SessionType.Impact);

            var exceptionTheFirst = await Assert.ThrowsAsync<NexosisClientException>(async () => await fixture.Client.Sessions.Get(first.SessionId));
            var exceptionTheSecond = await Assert.ThrowsAsync<NexosisClientException>(async () => await fixture.Client.Sessions.Get(second.SessionId));

            Assert.Equal(exceptionTheFirst.StatusCode, HttpStatusCode.NotFound);
            Assert.Equal(exceptionTheSecond.StatusCode, HttpStatusCode.NotFound);
        }

    }
}