using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;

namespace SourceGitPlus.Views
{
    public partial class AddAccount : ChromelessWindow
    {
        public AddAccount()
        {
            CloseOnESC = true;
            InitializeComponent();
        }

        private async void OnLogin(object _, RoutedEventArgs e)
        {
            if (ProviderList.SelectedItem is ListBoxItem { Content: StackPanel panel } &&
                panel.Children[0] is Path icon &&
                panel.Children[1] is TextBlock name)
            {
                Window owner = Owner as Window;
                Close();
                await new LoginAccount(name.Text, icon.Data).ShowDialog(owner);
            }

            e.Handled = true;
        }

        private void OnCancel(object _, RoutedEventArgs e)
        {
            Close();
            e.Handled = true;
        }
    }
}
