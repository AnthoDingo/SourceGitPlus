using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SourceGit.ViewModels
{
    public class Account : ObservableObject
    {
        public string Provider
        {
            get;
        }

        public Geometry Icon
        {
            get;
        }

        public string Username
        {
            get;
        }

        public string Url
        {
            get;
        }

        public string AuthType
        {
            get;
        }

        // Bitbucket (cloud) stores "email:api-token".
        public string Token
        {
            get => _token;
            private set => SetProperty(ref _token, value);
        }

        public bool EnableNotifications
        {
            get => _enableNotifications;
            set => SetProperty(ref _enableNotifications, value);
        }

        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        public IBrush StatusBrush
        {
            get => _statusBrush;
            private set => SetProperty(ref _statusBrush, value);
        }

        public Bitmap Avatar
        {
            get => _avatar;
            private set => SetProperty(ref _avatar, value);
        }

        public bool IsLoadingRepositories
        {
            get => _isLoadingRepositories;
            private set => SetProperty(ref _isLoadingRepositories, value);
        }

        public string Filter
        {
            get => _filter;
            set
            {
                if (SetProperty(ref _filter, value))
                    OnPropertyChanged(nameof(FilteredGroups));
            }
        }

        public IEnumerable<AccountRepoGroup> FilteredGroups
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_filter))
                    return _groups;

                string filter = _filter.Trim();
                return _groups
                    .Select(g => new AccountRepoGroup(g.Owner, g.Repositories.Where(r => r.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList()))
                    .Where(g => g.Repositories.Count > 0)
                    .ToList();
            }
        }

        public Account(string provider, Geometry icon, string username, string url, string authType, string token, bool enableNotifications = true)
        {
            Provider = provider;
            Icon = icon;
            Username = username;
            Url = url;
            AuthType = authType;
            _token = token;
            _enableNotifications = enableNotifications;
            _statusText = "Checking...";
        }

        // Validates the new token (it must belong to this account) before replacing the current one.
        public async Task UpdateTokenAsync(string token, CancellationToken ct)
        {
            (string login, string _) = await Models.GitProviderApi.GetUserAsync(Provider, Url, token, ct);
            if (!string.Equals(login, Username, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"This token belongs to '{login}', not '{Username}'.");

            Token = token;
            _ = RefreshAsync();
        }

        public void OpenUrl()
        {
            Native.OS.OpenBrowser(Url);
        }

        public async Task RefreshAsync()
        {
            if (_refreshing)
                return;

            _refreshing = true;
            IsLoadingRepositories = true;

            try
            {
                (string _, string avatarUrl) = await Models.GitProviderApi.GetUserAsync(Provider, Url, Token, CancellationToken.None);
                StatusText = "Online";
                StatusBrush = Brushes.LimeGreen;

                if (!string.IsNullOrEmpty(avatarUrl))
                {
                    try
                    {
                        byte[] bytes = await Models.GitHubApi.GetBytesAsync(avatarUrl, CancellationToken.None);
                        Avatar = new Bitmap(new MemoryStream(bytes));
                    }
                    catch (Exception)
                    {
                        // The avatar is cosmetic.
                    }
                }

                List<Models.RemoteRepo> repos = await Models.GitProviderApi.GetRepositoriesAsync(Provider, Url, Token, CancellationToken.None);
                _groups = repos
                    .GroupBy(r => r.Owner)
                    .OrderBy(g => g.Key.Equals(Username, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                    .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(g => new AccountRepoGroup(g.Key, g.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase).Select(r => new AccountRepo(r.Name, r.CloneUrl, Icon)).ToList()))
                    .ToList();
                OnPropertyChanged(nameof(FilteredGroups));
            }
            catch (Exception)
            {
                StatusText = "Offline";
                StatusBrush = Brushes.IndianRed;
            }
            finally
            {
                IsLoadingRepositories = false;
                _refreshing = false;
            }
        }

        private string _token;
        private bool _enableNotifications;
        private string _statusText;
        private IBrush _statusBrush = Brushes.Gray;
        private Bitmap _avatar;
        private bool _isLoadingRepositories;
        private bool _refreshing;
        private string _filter = string.Empty;
        private List<AccountRepoGroup> _groups = [];
    }
}
