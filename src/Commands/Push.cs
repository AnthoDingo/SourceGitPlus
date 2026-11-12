using System.Text;

namespace SourceGitPlus.Commands
{
    public class Push : Command
    {
        // Read-only remotes never receive a push, whatever the caller.
        public static string ReadOnlyReason(Models.Remote remote)
        {
            return remote.IsReadOnly ? App.Text("Remote.ReadOnly.Blocked", remote.Name) : null;
        }

        public Push(string repo, Models.Branch local, Models.Remote remote, Models.Branch remoteBranch, bool withTags, bool checkSubmodules, bool track, bool force, bool noVerify)
        {
            WorkingDirectory = repo;
            Context = repo;
            SSHKey = remote.PrivateSSHKey;
            BlockedReason = ReadOnlyReason(remote);

            var builder = new StringBuilder(1024);
            builder.Append("push --progress --verbose ");
            if (withTags)
                builder.Append("--tags ");
            if (checkSubmodules)
                builder.Append("--recurse-submodules=check ");
            if (track)
                builder.Append("-u ");
            if (force)
                builder.Append("--force-with-lease ");
            if (noVerify)
                builder.Append("--no-verify ");

            builder.Append(remote.Name).Append(" refs/heads/").Append(local.Name).Append(":refs/heads/").Append(remoteBranch.Name);
            Args = builder.ToString();
        }

        public Push(string repo, Models.Commit revision, Models.Remote remote, Models.Branch remoteBranch, bool force)
        {
            WorkingDirectory = repo;
            Context = repo;
            SSHKey = remote.PrivateSSHKey;
            BlockedReason = ReadOnlyReason(remote);

            var builder = new StringBuilder(1024);
            builder.Append("push --progress --verbose ");
            if (force)
                builder.Append("--force-with-lease ");

            builder.Append(remote.Name).Append(' ').Append(revision.SHA).Append(":refs/heads/").Append(remoteBranch.Name);
            Args = builder.ToString();
        }

        public Push(string repo, Models.Remote remote, string refname, bool isDelete)
        {
            WorkingDirectory = repo;
            Context = repo;
            SSHKey = remote.PrivateSSHKey;
            BlockedReason = ReadOnlyReason(remote);

            var builder = new StringBuilder(512);
            builder.Append("push ");
            if (isDelete)
                builder.Append("--delete ");
            builder.Append(remote.Name).Append(' ').Append(refname);

            Args = builder.ToString();
        }
    }
}
