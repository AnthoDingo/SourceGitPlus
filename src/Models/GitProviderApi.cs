using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SourceGitPlus.Models
{
    // Per-provider "current user" and "my repositories" calls.
    public static class GitProviderApi
    {
        public static string GetDefaultUrl(string provider)
        {
            return provider switch
            {
                "GitHub" => GitHubApi.DEFAULT_URL,
                "GitLab" => "https://gitlab.com",
                "Bitbucket" => "https://bitbucket.org",
                _ => string.Empty,
            };
        }

        public static async Task<(string Login, string AvatarUrl)> GetUserAsync(string provider, string url, string token, CancellationToken ct)
        {
            string root = url.TrimEnd('/');
            switch (provider)
            {
                case "GitHub":
                case "GitHub Enterprise Server":
                    return await GitHubApi.GetUserAsync(url, token, ct);
                case "GitLab":
                case "GitLab Server":
                    {
                        using JsonDocument doc = await ApiHttp.GetJsonAsync(provider, $"{root}/api/v4/user", token, ct);
                        return (doc.RootElement.GetProperty("username").GetString(), GetString(doc.RootElement, "avatar_url"));
                    }
                case "Gitea":
                    {
                        using JsonDocument doc = await ApiHttp.GetJsonAsync(provider, $"{root}/api/v1/user", token, ct);
                        return (doc.RootElement.GetProperty("login").GetString(), GetString(doc.RootElement, "avatar_url"));
                    }
                case "Bitbucket":
                    {
                        using JsonDocument doc = await ApiHttp.GetJsonAsync(provider, "https://api.bitbucket.org/2.0/user", token, ct);
                        JsonElement user = doc.RootElement;
                        string login = GetString(user, "nickname") ?? user.GetProperty("display_name").GetString();
                        string avatar = user.TryGetProperty("links", out JsonElement links) && links.TryGetProperty("avatar", out JsonElement a) ? GetString(a, "href") : null;
                        return (login, avatar);
                    }
                default: // Bitbucket Server
                    {
                        string name = await ApiHttp.GetTextAsync(provider, $"{root}/plugins/servlet/applinks/whoami", token, ct);
                        if (string.IsNullOrEmpty(name))
                            throw new InvalidOperationException("The server did not recognize this token.");

                        return (name, null);
                    }
            }
        }

        public static async Task<List<RemoteRepo>> GetRepositoriesAsync(string provider, string url, string token, CancellationToken ct)
        {
            string root = url.TrimEnd('/');
            List<RemoteRepo> repos = [];

            switch (provider)
            {
                case "GitHub":
                case "GitHub Enterprise Server":
                    return await GitHubApi.GetRepositoriesAsync(url, token, ct);

                case "GitLab":
                case "GitLab Server":
                    for (int page = 1; ; page++)
                    {
                        using JsonDocument doc = await ApiHttp.GetJsonAsync(provider,
                            $"{root}/api/v4/projects?membership=true&simple=true&per_page=100&page={page}", token, ct);
                        int count = 0;
                        foreach (JsonElement p in doc.RootElement.EnumerateArray())
                        {
                            repos.Add(new RemoteRepo(p.GetProperty("namespace").GetProperty("full_path").GetString(), p.GetProperty("path").GetString(), p.GetProperty("http_url_to_repo").GetString()));
                            count++;
                        }

                        if (count < 100)
                            return repos;
                    }

                case "Gitea":
                    for (int page = 1; ; page++)
                    {
                        using JsonDocument doc = await ApiHttp.GetJsonAsync(provider, $"{root}/api/v1/user/repos?limit=50&page={page}", token, ct);
                        int count = 0;
                        foreach (JsonElement r in doc.RootElement.EnumerateArray())
                        {
                            repos.Add(new RemoteRepo(r.GetProperty("owner").GetProperty("login").GetString(), r.GetProperty("name").GetString(), r.GetProperty("clone_url").GetString()));
                            count++;
                        }

                        if (count < 50)
                            return repos;
                    }

                case "Bitbucket":
                    {
                        string next = "https://api.bitbucket.org/2.0/repositories?role=member&pagelen=100";
                        while (next != null)
                        {
                            using JsonDocument doc = await ApiHttp.GetJsonAsync(provider, next, token, ct);
                            foreach (JsonElement r in doc.RootElement.GetProperty("values").EnumerateArray())
                            {
                                repos.Add(new RemoteRepo(
                                    r.GetProperty("workspace").GetProperty("slug").GetString(),
                                    r.GetProperty("slug").GetString(),
                                    FindCloneUrl(r.GetProperty("links"), "https")));
                            }

                            next = GetString(doc.RootElement, "next");
                        }

                        return repos;
                    }

                default: // Bitbucket Server
                    {
                        int start = 0;
                        while (true)
                        {
                            using JsonDocument doc = await ApiHttp.GetJsonAsync(provider, $"{root}/rest/api/1.0/repos?limit=100&start={start}", token, ct);
                            foreach (JsonElement r in doc.RootElement.GetProperty("values").EnumerateArray())
                            {
                                repos.Add(new RemoteRepo(
                                    r.GetProperty("project").GetProperty("key").GetString(),
                                    r.GetProperty("slug").GetString(),
                                    FindCloneUrl(r.GetProperty("links"), "http")));
                            }

                            if (doc.RootElement.GetProperty("isLastPage").GetBoolean())
                                return repos;

                            start = doc.RootElement.GetProperty("nextPageStart").GetInt32();
                        }
                    }
            }
        }

        private static string FindCloneUrl(JsonElement links, string protocol)
        {
            if (links.TryGetProperty("clone", out JsonElement clones))
            {
                foreach (JsonElement clone in clones.EnumerateArray())
                {
                    if (clone.GetProperty("name").GetString() == protocol)
                        return clone.GetProperty("href").GetString();
                }
            }

            return string.Empty;
        }

        private static string GetString(JsonElement element, string name)
        {
            return element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        }
    }
}
