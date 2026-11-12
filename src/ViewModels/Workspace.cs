using System.Collections.Generic;
using System.Text;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SourceGitPlus.ViewModels
{
    public class Workspace : ObservableObject
    {
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public uint Color
        {
            get => _color;
            set
            {
                if (SetProperty(ref _color, value))
                    OnPropertyChanged(nameof(Brush));
            }
        }

        public List<string> Repositories
        {
            get;
            set;
        } = new List<string>();

        public int ActiveIdx
        {
            get;
            set;
        } = 0;

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public bool RestoreOnStartup
        {
            get => _restoreOnStartup;
            set => SetProperty(ref _restoreOnStartup, value);
        }

        public string DefaultCloneDir
        {
            get => _defaultCloneDir;
            set => SetProperty(ref _defaultCloneDir, value);
        }

        // Git identity and signing overrides applied to every git command run on this workspace's repositories.
        // Empty strings and a null sign flag mean "no override": the repository setting applies.
        public string GitUserName
        {
            get => _gitUserName;
            set => SetProperty(ref _gitUserName, value);
        }

        public string GitUserEmail
        {
            get => _gitUserEmail;
            set => SetProperty(ref _gitUserEmail, value);
        }

        public string GitSigningKey
        {
            get => _gitSigningKey;
            set => SetProperty(ref _gitSigningKey, value);
        }

        public bool? GitSignCommits
        {
            get => _gitSignCommits;
            set => SetProperty(ref _gitSignCommits, value);
        }

        // "-c" options prepended to git commands run on this workspace's repositories.
        public string GitConfigArgs()
        {
            StringBuilder builder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(GitUserName))
                builder.Append("-c user.name=").Append(GitUserName.Trim().Quoted()).Append(' ');
            if (!string.IsNullOrWhiteSpace(GitUserEmail))
                builder.Append("-c user.email=").Append(GitUserEmail.Trim().Quoted()).Append(' ');
            if (!string.IsNullOrWhiteSpace(GitSigningKey))
                builder.Append("-c user.signingkey=").Append(GitSigningKey.Trim().Quoted()).Append(' ');
            if (GitSignCommits is bool sign)
                builder.Append("-c commit.gpgsign=").Append(sign ? "true" : "false").Append(' ');
            return builder.ToString();
        }

        public IBrush Brush
        {
            get => new SolidColorBrush(_color);
        }

        private string _gitUserName = string.Empty;
        private string _gitUserEmail = string.Empty;
        private string _gitSigningKey = string.Empty;
        private bool? _gitSignCommits = null;
        private string _name = string.Empty;
        private uint _color = 4278221015;
        private bool _isActive = false;
        private bool _restoreOnStartup = true;
        private string _defaultCloneDir = string.Empty;
    }
}
