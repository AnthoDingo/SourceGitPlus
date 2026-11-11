using System.Collections.Generic;

namespace SourceGitPlus.ViewModels
{
    public class AccountRepoGroup
    {
        public string Owner { get; }
        public List<AccountRepo> Repositories { get; }

        public AccountRepoGroup(string owner, List<AccountRepo> repositories)
        {
            Owner = owner;
            Repositories = repositories;
        }
    }
}
