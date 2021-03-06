﻿using System;
using System.Net.Http;
using System.Threading.Tasks;
using Nexosis.Api.Client.Model;
using Xunit;

namespace Api.Client.Tests.SessionTests
{
    public class RemoveTests : NexosisClient_TestsBase
    {
        public RemoveTests() : base(new {})
        {
        }

        [Fact]
        public async Task HandlerDoesNotIncludeOptionalArgsIfTheyAreNotSet()
        {
            await target.Sessions.Remove();

            Assert.Equal(HttpMethod.Delete, handler.Request.Method);
            Assert.Equal(new Uri(baseUri, "sessions"), handler.Request.RequestUri);
        }

        [Fact]
        public async Task HandlerIncludesOptionalArgsIfTheyAreSet()
        {
            await target.Sessions.Remove("data-set-name", "event-name", SessionType.Forecast);

            Assert.Equal(HttpMethod.Delete, handler.Request.Method);
            Assert.Equal(new Uri(baseUri, "sessions?dataSetName=data-set-name&eventName=event-name&type=Forecast"), handler.Request.RequestUri);
        }

        [Fact]
        public async Task IncludesDatesInUrlWhenGiven()
        {
            await target.Sessions.Remove(null, null, null, DateTimeOffset.Parse("2017-02-02 20:20:12 -0:00"), DateTimeOffset.Parse("2017-02-22 21:12 -0:00"));

            Assert.Equal(HttpMethod.Delete, handler.Request.Method);
            Assert.Equal(new Uri(baseUri, "sessions?requestedAfterDate=2017-02-02T20:20:12.0000000%2B00:00&requestedBeforeDate=2017-02-22T21:12:00.0000000%2B00:00"), handler.Request.RequestUri);
        }

        [Fact]
        public async Task IdIsUsedInUrl()
        {
            var sessionId = Guid.NewGuid(); 
            await target.Sessions.Remove(sessionId);

            Assert.Equal(HttpMethod.Delete, handler.Request.Method);
            Assert.Equal(new Uri(baseUri, $"sessions/{sessionId}"), handler.Request.RequestUri);
        }
    }
}
