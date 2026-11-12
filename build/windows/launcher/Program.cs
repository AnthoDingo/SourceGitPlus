using System;
using System.Diagnostics;
using System.IO;

namespace SourceGitPlusLauncher
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            string target = Path.Combine(AppContext.BaseDirectory, "SourceGitPlus.exe");
            if (!File.Exists(target))
            {
                Console.Error.WriteLine("SourceGitPlus.exe not found next to " + Path.GetFileName(Environment.ProcessPath));
                return 1;
            }

            ProcessStartInfo psi = new ProcessStartInfo(target) { UseShellExecute = false };
            foreach (string arg in args)
                psi.ArgumentList.Add(arg);

            // The child inherits the current directory, so "sgp ." opens the repository of the terminal.
            Process.Start(psi);
            return 0;
        }
    }
}
