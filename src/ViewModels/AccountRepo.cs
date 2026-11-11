using Avalonia.Media;

namespace SourceGitPlus.ViewModels
{
    public class AccountRepo
    {
        public string Name { get; }
        public string CloneUrl { get; }
        public Geometry Icon { get; }

        public AccountRepo(string name, string cloneUrl, Geometry icon)
        {
            Name = name;
            CloneUrl = cloneUrl;
            Icon = icon;
        }
    }
}
