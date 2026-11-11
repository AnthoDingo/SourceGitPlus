using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SourceGit.Models
{
    // Shared HTTP plumbing for the git hosting providers (authenticated GET, JSON).
    public static class ApiHttp
    {
        public static readonly HttpClient Client = CreateClient();

        public static async Task<JsonDocument> GetJsonAsync(string provider, string url, string token, CancellationToken ct)
        {
            using HttpResponseMessage response = await SendAsync(provider, url, token, ct);
            return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        }

        public static async Task<string> GetTextAsync(string provider, string url, string token, CancellationToken ct)
        {
            using HttpResponseMessage response = await SendAsync(provider, url, token, ct);
            return (await response.Content.ReadAsStringAsync(ct)).Trim();
        }

        private static async Task<HttpResponseMessage> SendAsync(string provider, string url, string token, CancellationToken ct)
        {
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
            Authorize(provider, token, request);

            HttpResponseMessage response = await Client.SendAsync(request, ct);
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (Exception)
            {
                response.Dispose();
                throw;
            }

            return response;
        }

        private static void Authorize(string provider, string token, HttpRequestMessage request)
        {
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            switch (provider)
            {
                case "GitHub":
                case "GitHub Enterprise Server":
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
                    break;
                case "GitLab":
                case "GitLab Server":
                    request.Headers.Add("PRIVATE-TOKEN", token);
                    break;
                case "Gitea":
                    request.Headers.Authorization = new AuthenticationHeaderValue("token", token);
                    break;
                case "Bitbucket": // token is "email:api-token"
                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(token)));
                    break;
                default: // Bitbucket Server: personal access token
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    break;
            }
        }

        private static HttpClient CreateClient()
        {
            HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SourceGit");
            return client;
        }
    }
}
