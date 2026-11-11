using System;

namespace SourceGitPlus.Models
{
    [Flags]
    public enum DirtyState
    {
        None = 0,
        HasLocalChanges = 1 << 0,
        HasPendingPullOrPush = 1 << 1,
    }
}
