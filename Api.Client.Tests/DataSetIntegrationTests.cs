﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Nexosis.Api.Client;
using Nexosis.Api.Client.Model;
using Xunit;

namespace Api.Client.Tests
{
    [Collection("Integration")]
    public class DataSetIntegrationTests
    {
        private readonly IntegrationTestFixture fixture;

        public DataSetIntegrationTests(IntegrationTestFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task CanSaveDataSet()
        {
            var data = DataSetGenerator.Run(DateTime.Parse("2017-01-01"), DateTime.Parse("2017-03-31"), "xray");

            var result = await fixture.Client.DataSets.Create("mike", data);

            Assert.Equal("mike", result.DataSetName);
        }
        
        [Fact]
        public async Task CanSaveDataSetWithAssumedTimestampColumn()
        {
            var data = DataSetGenerator.Run(DateTime.Parse("2017-01-01"), DateTime.Parse("2017-03-31"), "foxtrot", implicitTimestamp: true);

            var result = await fixture.Client.DataSets.Create("whiskey", data);

            Assert.Equal("whiskey", result.DataSetName);
        }

        [Fact]
        public async Task GettingDataSetGivesBackLinks()
        {
            var result = await fixture.Client.DataSets.Get("whiskey");

            Assert.Equal(1, result.Links.Count);
            Assert.Equal(new [] { "sessions"}, result.Links.Select(l => l.Rel));
            Assert.Equal("https://api.dev.nexosisdev.com/api/sessions?dataSetName=whiskey", result.Links[0].Href);
        }

        [Fact]
        public async Task CanGetDataSetThatHasBeenSaved()
        {
            var data = DataSetGenerator.Run(DateTimeOffset.Parse("2017-01-01 0:00 -0:00"), DateTimeOffset.Parse("2017-03-31 0:00 -0:00"), "india juliet");

            await fixture.Client.DataSets.Create("zulu yankee", data);

            var result = await fixture.Client.DataSets.Get("zulu yankee");

            Assert.Equal(DateTimeOffset.Parse("2017-01-01 0:00 -0:00"), DateTimeOffset.Parse(result.Data.First()["time"]));
            Assert.True(result.Data.First().ContainsKey("india juliet"));
        }

        [Fact]
        public async Task CanPutMoreDataToSameDataSet()
        {
            var data = DataSetGenerator.Run(DateTimeOffset.Parse("2017-01-01 0:00 -0:00"), DateTimeOffset.Parse("2017-01-31 0:00 -0:00"), "golf hotel");
            await fixture.Client.DataSets.Create("alpha bravo", data);

            var moreData = DataSetGenerator.Run(DateTimeOffset.Parse("2017-02-01 0:00 -0:00"), DateTimeOffset.Parse("2017-03-01 0:00 -0:00"), "golf hotel");
            await fixture.Client.DataSets.Create("alpha bravo", moreData);

            var result = await fixture.Client.DataSets.Get("alpha bravo");

            var orderedData = result.Data.Select(d => DateTimeOffset.Parse(d["time"])).OrderBy(it => it);
            Assert.Equal(DateTimeOffset.Parse("2017-01-01 0:00 -0:00"), orderedData.First());
            Assert.Equal(DateTimeOffset.Parse("2017-02-28 0:00 -0:00"), orderedData.Last());
        }

        [Fact]
        public async Task ListsDataSets()
        {
            var list = await fixture.Client.DataSets.List();

            Assert.True(list.Count > 0);
        }

        [Fact]
        public async Task CanRemoveDataSet()
        {
            var id = Guid.NewGuid().ToString("N");

            var data = DataSetGenerator.Run(DateTime.Parse("2017-01-01"), DateTime.Parse("2017-03-31"), "hotel");

            await fixture.Client.DataSets.Create(id, data);
            await fixture.Client.DataSets.Remove(id, DataSetDeleteOptions.None);

            var exception = await Assert.ThrowsAsync<NexosisClientException>(async () => await fixture.Client.DataSets.Get(id));

            Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        }

        [Fact(Skip = "Only run if changing the API key used.")]
        public async Task PopulateDataForTesting()
        {
            // loads a data set and creates a forecast so we can query it when running the tests
            string dataSet = fixture.SavedDataSet;

            using (var file = File.OpenText("..\\..\\..\\CsvFiles\\producttest.csv"))
            {
                await fixture.Client.DataSets.Create(dataSet, file);
            }
            await fixture.Client.Sessions.CreateForecast(dataSet, "sales", DateTimeOffset.Parse("2017-03-25 0:00:00 -0:00"), DateTimeOffset.Parse("2017-04-24 0:00:00 -0:00"), ResultInterval.Day);

            var names = String.Join(", ", fixture.Client.DataSets.List(dataSet).GetAwaiter().GetResult().Select(ds => ds.DataSetName));
            Console.WriteLine(names);
        }

    }
}
