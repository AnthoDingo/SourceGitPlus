using System;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace SourceGitPlus.Views
{
    public partial class LoginAccount : ChromelessWindow
    {
        private readonly string _provider;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private string _manageUrlPath = "";

        public LoginAccount() : this("", null)
        {
        }

        public LoginAccount(string provider, Geometry icon)
        {
            CloseOnESC = true;
            InitializeComponent();

            _provider = provider;
            Title = $"Log in to {provider}";
            TitleText.Text = Title;
            ProviderIcon.Data = icon;

            // Login options per provider.
            if (provider == "Bitbucket")
            {
                MethodBox.ItemsSource = new string[] { "Log in on Bitbucket.org (OAuth)", "API Token" };
                MethodBox.SelectedIndex = 0;
                MethodBox.IsVisible = true;
                UserLabel.Text = "Email:";
                UserBox.Watermark = "Email";
                UserTokenLabel.Text = "API Token:";
                // The user form only shows for the API Token method (see OnInputChanged).
            }
            else if (provider == "GitHub")
            {
                MethodBox.ItemsSource = new string[] { "Log in on GitHub.com (OAuth)", "Personal Access Token" };
                MethodBox.SelectedIndex = 0;
                MethodBox.IsVisible = true;
                UserForm.IsVisible = true;
            }
            else if (provider == "Bitbucket Server")
            {
                ServerForm.IsVisible = true;
                _manageUrlPath = "/plugins/servlet/access-tokens/manage";
            }
            else if (provider == "GitHub Enterprise Server")
            {
                ServerForm.IsVisible = true;
                ServerBox.Watermark = "https://github.yourcompany.com";
                TokenBox.Watermark = "Token";
                _manageUrlPath = "/settings/tokens";
            }
            else if (provider == "GitLab" || provider == "GitLab Server")
            {
                Title = "Log in to GitLab";
                TitleText.Text = Title;
                ServerForm.IsVisible = true;
                TokenBox.Watermark = "Token with 'api write_repository' scopes";
                ManageText.Foreground = this.FindResource("Brush.Link") as IBrush;
                _manageUrlPath = "/-/user_settings/personal_access_tokens";

                ServerBox.Text = "https://gitlab.com";
                if (provider == "GitLab")
                {
                    // gitlab.com only: no server field
                    ServerLabel.IsVisible = false;
                    ServerBox.IsVisible = false;
                }
            }
            else if (provider == "Gitea")
            {
                Title = "Log in to Gitea server";
                TitleText.Text = Title;
                ServerForm.IsVisible = true;
                ServerBox.Watermark = "https://gitea.yourcompany.com";
                TokenBox.Watermark = "Token with 'read user, read and write repository' scopes";
                ManageText.Text = "access tokens";
                _manageUrlPath = "/user/settings/applications";
            }
        }

        private void OnInputChanged(object _, RoutedEventArgs e)
        {
            if (_provider == "GitHub" || _provider == "Bitbucket")
            {
                bool usePat = MethodBox.SelectedIndex == 1;
                UserTokenLabel.IsVisible = usePat;
                UserTokenBox.IsVisible = usePat;

                if (_provider == "Bitbucket")
                {
                    // Bitbucket Cloud API tokens are used with the account email.
                    UserForm.IsVisible = usePat;
                    SignInButton.IsEnabled = MethodBox.SelectedItem != null &&
                        (!usePat || (!string.IsNullOrWhiteSpace(UserBox.Text) && !string.IsNullOrEmpty(UserTokenBox.Text)));
                }
                else
                {
                    SignInButton.IsEnabled = MethodBox.SelectedItem != null && (!usePat || !string.IsNullOrEmpty(UserTokenBox.Text));
                }
            }
            else
            {
                SignInButton.IsEnabled = (!ServerBox.IsVisible || !string.IsNullOrWhiteSpace(ServerBox.Text)) && !string.IsNullOrEmpty(TokenBox.Text);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _cts.Cancel();
            base.OnClosed(e);
        }

        private void ShowStatus(string text)
        {
            StatusText.Text = text;
            StatusText.IsVisible = true;
        }

        private async void OnSignIn(object _, RoutedEventArgs e)
        {
            e.Handled = true;

            string url = Models.GitProviderApi.GetDefaultUrl(_provider);
            if (url.Length == 0)
                url = ServerBox.Text.Trim().TrimEnd('/');

            bool oauth = _provider == "GitHub" && MethodBox.SelectedIndex == 0;
            string token;

            SignInButton.IsEnabled = false;
            try
            {
                if (_provider == "Bitbucket" && MethodBox.SelectedIndex == 0)
                {
                    // Bitbucket Cloud OAuth needs an OAuth consumer with a redirect URL: not available here.
                    ShowStatus("Bitbucket OAuth is not available in SourceGitPlus. Use an API Token instead.");
                    return;
                }

                if (oauth)
                {
                    token = await Models.GitHubApi.AuthorizeAsync(Models.GitHubApi.OAuthClientId, async code =>
                    {
                        if (Clipboard != null)
                            await Clipboard.SetTextAsync(code.UserCode);
                        Native.OS.OpenBrowser(code.VerificationUri);
                        ShowStatus($"Enter the code {code.UserCode} at {code.VerificationUri} (copied to the clipboard). Waiting for authorization...");
                    }, _cts.Token);
                }
                else if (_provider == "Bitbucket")
                {
                    token = $"{UserBox.Text.Trim()}:{UserTokenBox.Text.Trim()}";
                }
                else if (_provider == "GitHub")
                {
                    token = UserTokenBox.Text.Trim();
                }
                else
                {
                    token = TokenBox.Text.Trim();
                }

                ShowStatus("Signing in...");
                (string login, string _) = await Models.GitProviderApi.GetUserAsync(_provider, url, token, _cts.Token);
                if (_provider == "GitHub" && !string.IsNullOrWhiteSpace(UserBox.Text) &&
                    !string.Equals(UserBox.Text.Trim(), login, StringComparison.OrdinalIgnoreCase))
                {
                    ShowStatus($"This token belongs to '{login}', not '{UserBox.Text.Trim()}'.");
                    return;
                }

                string auth = oauth ? "OAuth" : (_provider == "Bitbucket" ? "API Token" : "Personal Access Token");
                ViewModels.Accounts.Instance.AddOrReplace(new ViewModels.Account(_provider, ProviderIcon.Data, login, url, auth, token));
                Close();
            }
            catch (OperationCanceledException)
            {
                // Window closed.
            }
            catch (Exception ex)
            {
                ShowStatus($"Sign in failed: {ex.Message}");
            }
            finally
            {
                if (!_cts.IsCancellationRequested)
                    OnInputChanged(null, null);
            }
        }

        private void OnManageTokens(object _, PointerPressedEventArgs e)
        {
            string server = ServerBox.Text?.Trim().TrimEnd('/');
            if (!string.IsNullOrEmpty(server))
                Native.OS.OpenBrowser($"{server}{_manageUrlPath}");
        }

        private void OnCancel(object _, RoutedEventArgs e)
        {
            Close();
            e.Handled = true;
        }
    }
}
