using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SourceGitPlus.Views
{
    public partial class UpdateToken : ChromelessWindow
    {
        public UpdateToken() : this(null)
        {
        }

        public UpdateToken(ViewModels.Account account)
        {
            _account = account;
            CloseOnESC = true;
            InitializeComponent();

            _oauth = account?.AuthType == "OAuth";
            TitleText.Text = $"Update token for {account?.Username}";
            if (_oauth)
            {
                // OAuth accounts re-authorize through the device flow: no token to type.
                TokenLabel.IsVisible = false;
                TokenBox.IsVisible = false;
                SaveButton.Content = "Re-authorize";
                SaveButton.IsEnabled = true;
            }
        }

        private void OnTokenChanged(object _, RoutedEventArgs e)
        {
            SaveButton.IsEnabled = !string.IsNullOrEmpty(TokenBox.Text);
        }

        private async void OnSave(object _, RoutedEventArgs e)
        {
            e.Handled = true;
            SaveButton.IsEnabled = false;

            try
            {
                string token = _oauth ? await AuthorizeAsync() : TokenBox.Text.Trim();

                // Bitbucket (cloud) tokens are "email:api-token": keep the account's email.
                if (_account.Provider == "Bitbucket" && !token.Contains(':'))
                    token = $"{_account.Token.Split(':')[0]}:{token}";

                await _account.UpdateTokenAsync(token, _cts.Token);
                Close();
            }
            catch (OperationCanceledException)
            {
                // Window closed.
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Update failed: {ex.Message}";
                StatusText.IsVisible = true;
                SaveButton.IsEnabled = _oauth || !string.IsNullOrEmpty(TokenBox.Text);
            }
        }

        private async Task<string> AuthorizeAsync()
        {
            return await Models.GitHubApi.AuthorizeAsync(Models.GitHubApi.OAuthClientId, async code =>
            {
                if (Clipboard != null)
                    await Clipboard.SetTextAsync(code.UserCode);
                Native.OS.OpenBrowser(code.VerificationUri);
                StatusText.Text = $"Enter the code {code.UserCode} at {code.VerificationUri} (copied to the clipboard). Waiting for authorization...";
                StatusText.IsVisible = true;
            }, _cts.Token);
        }

        protected override void OnClosed(EventArgs e)
        {
            _cts.Cancel();
            base.OnClosed(e);
        }

        private void OnCancel(object _, RoutedEventArgs e)
        {
            Close();
            e.Handled = true;
        }

        private readonly ViewModels.Account _account;
        private readonly bool _oauth;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    }
}
