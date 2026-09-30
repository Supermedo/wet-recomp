using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Wet.Launcher
{
    public sealed class GitHubRelease
    {
        public string Tag;
        public string Version;
        public string ZipUrl;
        public string Name;
    }

    public static class GitHubUpdater
    {
        public const string Owner = "Supermedo";
        public const string Repo = "wet-recomp";
        public const string LatestApi =
            "https://api.github.com/repos/Supermedo/wet-recomp/releases/latest";

        public static string CurrentVersion()
        {
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            if (v == null)
                return "0.1.0";
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}", v.Major, v.Minor, v.Build);
        }

        public static GitHubRelease FetchLatest()
        {
            EnableTls12();
            using (var client = new WebClient())
            {
                client.Headers.Add("User-Agent", "WetLauncher");
                client.Headers.Add("Accept", "application/vnd.github+json");
                string json = client.DownloadString(LatestApi);
                string tag = JsonString(json, "tag_name");
                if (string.IsNullOrEmpty(tag))
                    throw new InvalidOperationException("GitHub did not return a release tag.");

                string zip = FindZipUrl(json);
                if (string.IsNullOrEmpty(zip))
                    throw new InvalidOperationException("The latest GitHub release has no Windows zip.");

                return new GitHubRelease
                {
                    Tag = tag,
                    Version = NormalizeVersion(tag),
                    ZipUrl = zip,
                    Name = JsonString(json, "name") ?? tag
                };
            }
        }

        public static bool IsNewer(string remoteVersion)
        {
            return CompareVersions(remoteVersion, CurrentVersion()) > 0;
        }

        public static void Download(string url, string destFile, Action<int> progress)
        {
            EnableTls12();
            using (var client = new WebClient())
            {
                client.Headers.Add("User-Agent", "WetLauncher");
                var done = new System.Threading.ManualResetEvent(false);
                Exception error = null;
                client.DownloadProgressChanged += (s, e) =>
                {
                    if (progress != null)
                        progress(e.ProgressPercentage);
                };
                client.DownloadFileCompleted += (s, e) =>
                {
                    error = e.Error;
                    done.Set();
                };
                client.DownloadFileAsync(new Uri(url), destFile);
                done.WaitOne();
                if (error != null)
                    throw error;
            }
        }

        public static string ExtractZip(string zipPath, string destDir)
        {
            if (Directory.Exists(destDir))
                Directory.Delete(destDir, true);
            Directory.CreateDirectory(destDir);

            var tar = new ProcessStartInfo
            {
                FileName = "tar",
                Arguments = "-xf \"" + zipPath + "\" -C \"" + destDir + "\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
            };
            using (Process proc = Process.Start(tar))
            {
                if (proc != null)
                {
                    proc.WaitForExit();
                    if (proc.ExitCode == 0)
                        return FindPayload(destDir);
                }
            }

            var ps = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -Command \"Expand-Archive -LiteralPath '" +
                            zipPath.Replace("'", "''") + "' -DestinationPath '" +
                            destDir.Replace("'", "''") + "' -Force\"",
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using (Process proc = Process.Start(ps))
            {
                if (proc == null)
                    throw new InvalidOperationException("Could not extract the update zip.");
                proc.WaitForExit();
                if (proc.ExitCode != 0)
                    throw new InvalidOperationException("Could not extract the update zip.");
            }
            return FindPayload(destDir);
        }

        public static void ApplyAndRestart(string installDir, string payloadDir)
        {
            string script = Path.Combine(Path.GetTempPath(), "wet-recomp-apply.cmd");
            string target = Path.GetFullPath(installDir).TrimEnd(Path.DirectorySeparatorChar);
            string source = Path.GetFullPath(payloadDir).TrimEnd(Path.DirectorySeparatorChar);
            int pid = Process.GetCurrentProcess().Id;

            var sb = new StringBuilder();
            sb.AppendLine("@echo off");
            sb.AppendLine("setlocal EnableExtensions");
            sb.AppendLine("set WAITPID=" + pid.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("set \"TARGET=" + target + "\"");
            sb.AppendLine("set \"SOURCE=" + source + "\"");
            sb.AppendLine(":waitloop");
            sb.AppendLine("ping -n 2 127.0.0.1 >nul");
            sb.AppendLine("tasklist /FI \"PID eq %WAITPID%\" | findstr /I /C:\"%WAITPID%\" >nul");
            sb.AppendLine("if not errorlevel 1 goto waitloop");
            sb.AppendLine("ping -n 2 127.0.0.1 >nul");
            sb.AppendLine("if not exist \"%SOURCE%\\WetLauncher.exe\" if not exist \"%SOURCE%\\wet.exe\" goto launch");
            sb.AppendLine("robocopy \"%SOURCE%\" \"%TARGET%\" /E /IS /IT /R:8 /W:1 /XD game /NFL /NDL /NJH /NJS /nc /ns /np");
            sb.AppendLine("if %ERRORLEVEL% GEQ 8 (");
            sb.AppendLine("  for /f \"delims=\" %%F in ('dir /b /a-d \"%SOURCE%\" 2^>nul') do copy /Y \"%SOURCE%\\%%F\" \"%TARGET%\\\" >nul");
            sb.AppendLine("  for /f \"delims=\" %%D in ('dir /b /ad \"%SOURCE%\" 2^>nul') do (");
            sb.AppendLine("    if /I not \"%%D\"==\"game\" xcopy /E /Y /I \"%SOURCE%\\%%D\" \"%TARGET%\\%%D\\\" >nul");
            sb.AppendLine("  )");
            sb.AppendLine(")");
            sb.AppendLine(":launch");
            sb.AppendLine("start \"\" \"%TARGET%\\WetLauncher.exe\"");
            sb.AppendLine("rmdir /S /Q \"%SOURCE%\" 2>nul");
            sb.AppendLine("del \"%~f0\" 2>nul");
            File.WriteAllText(script, sb.ToString(), Encoding.ASCII);

            Process.Start(new ProcessStartInfo
            {
                FileName = script,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            });
        }

        public static bool GameProcessRunning()
        {
            Process[] procs = Process.GetProcessesByName("wet");
            return procs != null && procs.Length > 0;
        }

        private static string FindPayload(string unpack)
        {
            if (File.Exists(Path.Combine(unpack, "WetLauncher.exe")) ||
                File.Exists(Path.Combine(unpack, "wet.exe")))
                return unpack;

            foreach (string dir in Directory.GetDirectories(unpack))
            {
                if (File.Exists(Path.Combine(dir, "WetLauncher.exe")) ||
                    File.Exists(Path.Combine(dir, "wet.exe")))
                    return dir;
            }
            return unpack;
        }

        private static string FindZipUrl(string json)
        {
            var matches = Regex.Matches(json, "\"browser_download_url\"\\s*:\\s*\"([^\"]+)\"");
            string fallback = null;
            foreach (Match match in matches)
            {
                string url = Unescape(match.Groups[1].Value);
                if (url.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    string lower = url.ToLowerInvariant();
                    if (lower.Contains("windows") || lower.Contains("win") || lower.Contains("x64"))
                        return url;
                    if (fallback == null)
                        fallback = url;
                }
            }
            return fallback;
        }

        private static string JsonString(string json, string key)
        {
            var match = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"((?:\\\\.|[^\"\\\\])*)\"");
            if (!match.Success)
                return null;
            return Unescape(match.Groups[1].Value);
        }

        private static string Unescape(string value)
        {
            return value.Replace("\\/", "/").Replace("\\\"", "\"").Replace("\\\\", "\\");
        }

        public static string NormalizeVersion(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return "0.0.0";
            string t = tag.Trim();
            if (t.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                t = t.Substring(1);
            int cut = t.IndexOf('-');
            if (cut > 0)
                t = t.Substring(0, cut);
            return t;
        }

        public static int CompareVersions(string a, string b)
        {
            int[] left = ParseParts(a);
            int[] right = ParseParts(b);
            int n = Math.Max(left.Length, right.Length);
            for (int i = 0; i < n; i++)
            {
                int l = i < left.Length ? left[i] : 0;
                int r = i < right.Length ? right[i] : 0;
                if (l != r)
                    return l.CompareTo(r);
            }
            return 0;
        }

        private static int[] ParseParts(string version)
        {
            string[] bits = NormalizeVersion(version).Split('.');
            var parts = new int[bits.Length];
            for (int i = 0; i < bits.Length; i++)
            {
                int n;
                parts[i] = int.TryParse(bits[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out n) ? n : 0;
            }
            return parts;
        }

        private static void EnableTls12()
        {
            try
            {
                ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;
            }
            catch
            {
            }
        }
    }
}
