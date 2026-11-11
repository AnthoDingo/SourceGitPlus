namespace SourceGitPlus.Models
{
    public record GitHubDeviceCode(string DeviceCode, string UserCode, string VerificationUri, int Interval, int ExpiresIn);
}
