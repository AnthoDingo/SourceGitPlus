using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SourceGitPlus.Models
{
    public static class GitHubApi
    {
        // Public OAuth App Client ID (Device Flow enabled). It is not a secret; the device flow needs no client secret.
        // SOURCEGIT_GITHUB_CLIENT_ID overrides it, e.g. to test with another OAuth App.
        public static string OAuthClientId
        {
            get => Environment.GetEnvironmentVariable("SOURCEGIT_GITHUB_CLIENT_ID") is { Length: > 0 } env ? env : "Ov23li35At9NVFCrqE9d";
        }

        public const string DEFAULT_URL = "https://github.com";

        public static async Task<GitHubDeviceCode> RequestDeviceCodeAsync(string clientId, CancellationToken ct)
        {
            using JsonDocument doc = await PostFormAsync("https://github.com/login/device/code",
                new Dictionary<string, string> { ["client_id"] = clientId, ["scope"] = "repo read:org" }, ct);
            JsonElement root = doc.RootElement;
            if (root.TryGetProperty("error", out JsonElement error))
                throw new InvalidOperationException(root.TryGetProperty("error_description", out JsonElement d) ? d.GetString() : error.GetString());

            return new GitHubDeviceCode(
                root.GetProperty("device_code").GetString(),
                root.GetProperty("user_code").GetString(),
                root.GetProperty("verification_uri").GetString(),
                root.GetProperty("interval").GetInt32(),
                root.GetProperty("expires_in").GetInt32());
        }

        public static async Task<string> PollAccessTokenAsync(string clientId, GitHubDeviceCode code, CancellationToken ct)
        {
            int interval = Math.Max(code.Interval, 1);
            DateTime deadline = DateTime.UtcNow.AddSeconds(code.ExpiresIn);

            while (DateTime.UtcNow < deadline)
            {
                await Task.Delay(TimeSpan.FromSeconds(interval), ct);

                using JsonDocument doc = await PostFormAsync("https://github.com/login/oauth/access_token",
                    new Dictionary<string, string>
                    {
                        ["client_id"] = clientId,
                        ["device_code"] = code.DeviceCode,
                        ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code",
                    }, ct);

                JsonElement root = doc.RootElement;
                if (root.TryGetProperty("access_token", out JsonElement token))
                    return token.GetString();

                string error = root.TryGetProperty("error", out JsonElement e) ? e.GetString() : "unknown_error";
                if (error == "slow_down")
                    interval += 5;
                else if (error != "authorization_pending")
                    throw ToAuthorizationException(error);
            }

            throw new TimeoutException("The device code has expired. Please try again.");
        }

        // Full device flow: requests a code, hands it to the UI (to show/copy it), then waits for the user to authorize.
        public static async Task<string> AuthorizeAsync(string clientId, Func<GitHubDeviceCode, Task> onCode, CancellationToken ct)
        {
            GitHubDeviceCode code = await RequestDeviceCodeAsync(clientId, ct);
            await onCode(code);
            return await PollAccessTokenAsync(clientId, code, ct);
        }

        private static Exception ToAuthorizationException(string error)
        {
            return error switch
            {
                "access_denied" => new InvalidOperationException("Authorization was denied on GitHub."),
                "expired_token" => new TimeoutException("The device code has expired. Please try again."),
                "device_flow_disabled" => new InvalidOperationException("Device Flow is not enabled for this GitHub OAuth App."),
                "incorrect_client_credentials" => new InvalidOperationException("The OAuth App Client ID is not valid."),
                _ => new InvalidOperationException($"GitHub authorization failed: {error}"),
            };
        }

        public static async Task<(string Login, string AvatarUrl)> GetUserAsync(string webUrl, string token, CancellationToken ct)
        {
            using JsonDocument doc = await ApiHttp.GetJsonAsync("GitHub", $"{GetApiBase(webUrl)}/user", token, ct);
            return (doc.RootElement.GetProperty("login").GetString(), doc.RootElement.GetProperty("avatar_url").GetString());
        }

        public static async Task<List<RemoteRepo>> GetRepositoriesAsync(string webUrl, string token, CancellationToken ct)
        {
            List<RemoteRepo> repos = [];
            for (int page = 1; ; page++)
            {
                using JsonDocument doc = await ApiHttp.GetJsonAsync("GitHub",
                    $"{GetApiBase(webUrl)}/user/repos?per_page=100&page={page}&sort=full_name", token, ct);

                int count = 0;
                foreach (JsonElement repo in doc.RootElement.EnumerateArray())
                {
                    repos.Add(new RemoteRepo(repo.GetProperty("owner").GetProperty("login").GetString(), repo.GetProperty("name").GetString(), repo.GetProperty("clone_url").GetString()));
                    count++;
                }

                if (count < 100)
                    return repos;
            }
        }

        public static async Task<byte[]> GetBytesAsync(string url, CancellationToken ct)
        {
            return await ApiHttp.Client.GetByteArrayAsync(url, ct);
        }

        private static string GetApiBase(string webUrl)
        {
            return webUrl == DEFAULT_URL ? "https://api.github.com" : webUrl.TrimEnd('/') + "/api/v3";
        }

        private static async Task<JsonDocument> PostFormAsync(string url, Dictionary<string, string> form, CancellationToken ct)
        {
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(form) };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using HttpResponseMessage response = await ApiHttp.Client.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        }
    }
}
