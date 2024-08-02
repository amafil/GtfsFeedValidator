using System.Net;
using System.Net.Http.Headers;

namespace GtfsFeedValidator.Test.TestLib
{
    internal static class WebApi
    {
        internal static async Task<HttpResponseMessage> StartValidationAsync(this HttpClient client, string path)
        {
            var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");

            var multipartContent = new MultipartFormDataContent
            {
                { streamContent, "file", Path.GetFileName(path) }
            };

            return await client.PostAsync("start-validation", multipartContent); ;
        }

        internal static async Task<HttpResponseMessage> GetValidationResultAsync(this HttpClient client, string gtfsFeedValidationId)
        {
            return await client.GetAsync($"validation-result/{gtfsFeedValidationId}");
        }

        internal static async Task EnsureApiIsReadyAsync(this HttpClient client)
        {
            int attempt = 0;

            HttpResponseMessage response = await client.GetAsync("/api-status");
            string responseString = await response.Content.ReadAsStringAsync();

            while (response.StatusCode != HttpStatusCode.OK)
            {
                await Task.Delay(1000);
                response = await client.GetAsync("/api-status");
                responseString = await response.Content.ReadAsStringAsync();

                if (attempt > 5) {
                    throw new Exception("API did not start in time");
                }
            }
        }
    }
}
