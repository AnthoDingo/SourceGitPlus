using System;
using System.Security.Cryptography;
using System.Text;

namespace SourceGitPlus.Models
{
    public static class TokenProtector
    {
        // Windows: encrypted with DPAPI for the current user.
        // ponytail: other OSes store the token base64-only (like the AI api key in preferences); use libsecret/Keychain if needed.
        public static string Protect(string token)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(token);
            if (OperatingSystem.IsWindows())
                return "dpapi:" + Convert.ToBase64String(ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser));

            return "plain:" + Convert.ToBase64String(bytes);
        }

        public static string Unprotect(string stored)
        {
            try
            {
                if (stored.StartsWith("dpapi:", StringComparison.Ordinal) && OperatingSystem.IsWindows())
                    return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(stored[6..]), null, DataProtectionScope.CurrentUser));
                if (stored.StartsWith("plain:", StringComparison.Ordinal))
                    return Encoding.UTF8.GetString(Convert.FromBase64String(stored[6..]));
            }
            catch (Exception)
            {
                // Unreadable (other user/machine): treated as no token.
            }

            return null;
        }
    }
}
