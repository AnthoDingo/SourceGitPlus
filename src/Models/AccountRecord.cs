namespace SourceGit.Models
{
    // Persisted form of an account (accounts.json). The token is stored protected, never in clear text on Windows.
    public class AccountRecord
    {
        public string Provider { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string AuthType { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public bool EnableNotifications { get; set; } = true;
    }
}
