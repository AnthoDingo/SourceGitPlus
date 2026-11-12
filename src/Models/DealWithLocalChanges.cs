namespace SourceGitPlus.Models
{
    public enum DealWithLocalChanges
    {
        DoNothing = 0,
        StashAndReapply,
        Discard,
    }
}
