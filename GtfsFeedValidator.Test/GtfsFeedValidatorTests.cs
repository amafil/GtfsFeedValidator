using GtfsFeedValidator.Test.TestLib;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using FluentAssertions;
using System.Text.Json;
using System.Net.Http.Json;
using GtfsFeedValidator.Models;

namespace GtfsFeedValidator.Test
{
    [TestClass]
    public class GtfsFeedValidatorTests
    {
        private readonly HttpClient _client;

        private const string _filePath = @"./TestData/gtfs.zip";
        private const string _uri = "http://localhost:5001";

        public GtfsFeedValidatorTests()
        {
            var _factory = new WebApplicationFactory<Program>();
            _client = _factory.CreateClient(options: new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri(_uri)
            });
        }

        [TestMethod]
        public async Task ValidateGtfsFeed()
        {
            await _client.EnsureApiIsReadyAsync();

            // start validation
            HttpResponseMessage httpResponse = await _client.StartValidationAsync(_filePath);
            httpResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

            string response = await httpResponse.Content.ReadAsStringAsync();
            response.Should().NotBeNullOrWhiteSpace();
            
            string gtfsFeedValidationId = JsonSerializer.Deserialize<string>(response);
            gtfsFeedValidationId.Should().NotBeNullOrWhiteSpace();

            // expect that validation is not completed
            httpResponse = await _client.GetValidationResultAsync(gtfsFeedValidationId);
            httpResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // wait for the validation to complete
            bool isCompleted = false;
            for(int i = 0; i < 3; i++)
            {
                await Task.Delay(Constants.WorkerMsPollingInterval);
                httpResponse = await _client.GetValidationResultAsync(gtfsFeedValidationId);
                if (httpResponse.StatusCode == HttpStatusCode.OK)
                {
                    isCompleted = true;
                    break;
                }
            }
            isCompleted.Should().BeTrue();

            GtfsValidatorResponse validatorResponse = await httpResponse.Content.ReadFromJsonAsync<GtfsValidatorResponse>();
        }

        [TestMethod]
        public async Task MassiveValidation()
        {
            await _client.EnsureApiIsReadyAsync();

            Task<HttpResponseMessage>[] tasks = new Task<HttpResponseMessage>[50];
            
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = _client.StartValidationAsync(_filePath);
            }

            Task.WaitAll(tasks);

            List<HttpResponseMessage> httpResponseMessages = new();
            foreach (var task in tasks)
            {
                httpResponseMessages.Add(await task);
            }

            httpResponseMessages.All(httpResponseMessages => httpResponseMessages.StatusCode == HttpStatusCode.Accepted).Should().BeTrue();
        }
    }
}