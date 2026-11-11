using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace SourceGit.Views
{
    public partial class Accounts : ChromelessWindow
    {
        public Accounts()
        {
            CloseOnESC = true;
            DataContext = ViewModels.Accounts.Instance;
            InitializeComponent();
            ViewModels.Accounts.Instance.RefreshAll();
        }

        private async void OnAddAccount(object _, RoutedEventArgs e)
        {
            await new AddAccount().ShowDialog(this);
            e.Handled = true;
        }

        private void OnRemoveAccount(object _, RoutedEventArgs e)
        {
            ViewModels.Accounts.Instance.RemoveSelected();
            e.Handled = true;
        }

        private void OnOpenAccountUrl(object _, PointerPressedEventArgs e)
        {
            ViewModels.Accounts.Instance.Selected?.OpenUrl();
            e.Handled = true;
        }

        private async void OnUpdateToken(object _, RoutedEventArgs e)
        {
            ViewModels.Account account = ViewModels.Accounts.Instance.Selected;
            if (account != null)
                await new UpdateToken(account).ShowDialog(this);

            e.Handled = true;
        }

        private void OnCloneRepo(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (sender is not Button { DataContext: ViewModels.AccountRepo repo } || string.IsNullOrEmpty(repo.CloneUrl))
                return;

            if (!ViewModels.Preferences.Instance.IsGitConfigured())
            {
                Models.Notification.Send(null, App.Text("NotConfigured"), true);
                return;
            }

            ViewModels.LauncherPage page = App.GetLauncher()?.ActivePage;
            if (page == null || !page.CanCreatePopup())
                return;

            page.Popup = new ViewModels.Clone(page.Node.Id) { Remote = repo.CloneUrl };
            Close(); // the clone popup lives in the main window
        }

        private void OnClose(object _, RoutedEventArgs e)
        {
            Close();
            e.Handled = true;
        }
    }
}
