using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;
using Microsoft.Win32;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = System.IO.Path;
using RageCoop.Core;

namespace RageCoop.Client.Installer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Choose();
        }

        private void Choose()
        {
            var od = new OpenFileDialog()
            {
                Filter = "GTA 5 executable |GTA5.exe;PlayGTAV.exe",
                Title = "Select you GTAV executable"
            };
            if (od.ShowDialog() ?? false == true)
            {
                Task.Run(() =>
                {
                    try
                    {
                        Install(Directory.GetParent(od.FileName).FullName);
                    }
                    catch (Exception ex)
                    {
                        Log("Installation failed: " + ex);
                        MessageBox.Show("Installation failed: " + ex.ToString());
                        Environment.Exit(1);
                    }
                });
            }
            else
            {
                Environment.Exit(0);
            }
        }

        private void Install(string root)
        {
            UpdateStatus("Checking requirements");
            var shvPath = Path.Combine(root, "ScriptHookV.dll");
            var shvdnPath = Path.Combine(root, "ScriptHookVDotNet3.dll");
            var scriptsPath = Path.Combine(root, "Scripts");
            var lemonPath = Path.Combine(scriptsPath, "LemonUI.SHVDN3.dll");
            var installPath = Path.Combine(scriptsPath, "RageCoop");
            if (Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName == installPath)
            {
                throw new InvalidOperationException("The installer is not meant to be run in the game folder, please extract the zip to somewhere else and run again.");
            }
            Directory.CreateDirectory(installPath);
            Log("Installer started. Game folder: " + root + ". OS: " + Environment.OSVersion + ". Installer version: " + TryVer(Assembly.GetExecutingAssembly().Location));

            // One-click setup: fetch anything that is missing or too old
            // instead of telling the user to go and find it.
            EnsurePrerequisites(root, shvPath, shvdnPath, scriptsPath, lemonPath);

            UpdateStatus("Removing old versions");

            foreach (var f in Directory.GetFiles(scriptsPath, "RageCoop.*", SearchOption.AllDirectories))
            {
                if (f.EndsWith("RageCoop.Client.Settings.xml")) { continue; }
                File.Delete(f);
            }
            foreach (var f in Directory.GetFiles(installPath, "*.dll", SearchOption.AllDirectories))
            {
                File.Delete(f);
            }

            if (File.Exists("RageCoop.Core.dll") && File.Exists("RageCoop.Client.dll"))
            {
                UpdateStatus("Installing...");
                CoreUtils.CopyFilesRecursively(new DirectoryInfo(Directory.GetCurrentDirectory()), new DirectoryInfo(installPath));
                Finish();
            }
            else
            {
                throw new Exception("Required files are missing, please re-download the zip from official website");
            }

            void Finish()
            {

            checkKeys:
                UpdateStatus("Checking conflicts");
                var menyooConfig = Path.Combine(root, @"menyooStuff\menyooConfig.ini");
                var settingsPath = Path.Combine(installPath, @"Data\RageCoop.Client.Settings.xml");
                Settings settings = null;
                try
                {
                    settings = Util.ReadSettings(settingsPath);
                }
                catch
                {
                    settings = new Settings();
                }
                if (File.Exists(menyooConfig))
                {
                    var lines = File.ReadAllLines(menyooConfig).Where(x => !x.StartsWith(";") && x.EndsWith(" = " + (int)settings.MenuKey));
                    if (lines.Any())
                    {
                        if (MessageBox.Show("Following menyoo config value will conflict with RAGECOOP menu key\n" +
                            string.Join("\n", lines)
                            + "\nDo you wish to change the Menu Key?", "Warning!", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            var ae = new AutoResetEvent(false);
                            UpdateStatus("Press the key you wish to change to");
                            Dispatcher.BeginInvoke(new Action(() =>
                            KeyDown += (s, e) =>
                            {
                                settings.MenuKey = (Keys)KeyInterop.VirtualKeyFromKey(e.Key);
                                ae.Set();
                            }));
                            ae.WaitOne();
                            if (!Util.SaveSettings(settingsPath, settings))
                            {
                                MessageBox.Show("Error occurred when saving settings");
                                Environment.Exit(1);
                            }
                            MessageBox.Show("Menu key changed to " + settings.MenuKey);
                            goto checkKeys;
                        }
                    }
                }

                UpdateStatus("Checking ZeroTier");
                try
                {
                    ZeroTierHelper.Check();
                }
                catch
                {
                    if (MessageBox.Show("You can't join ZeroTier server unless ZeroTier is installed, do you want to download and install it?", "Install ZeroTier", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        var url = "https://download.zerotier.com/dist/ZeroTier%20One.msi";
                        UpdateStatus("Downloading ZeroTier from " + url);
                        try
                        {
                            HttpHelper.DownloadFile(url, "ZeroTier.msi", (p) => UpdateStatus("Downloading ZeroTier " + p + "%"));
                            UpdateStatus("Installing ZeroTier");
                            Process.Start("ZeroTier.msi").WaitForExit();
                        }
                        catch
                        {
                            MessageBox.Show("Failed to download ZeroTier, please download it from officail website");
                            Process.Start(url);
                        }
                    }
                }

                UpdateStatus("Completed!");
                MessageBox.Show("Installation sucessful!");
                Environment.Exit(0);
            }
        }

        // Minimum SHVDN the client was built against (bundled libs version).
        // Anything older breaks script loading (e.g. 3.7.0.110 throws a
        // NullReferenceException in GTA.Script..ctor for every script).
        private static readonly Version MinShvdnVersion = new Version(3, 7, 0, 188);
        private static readonly Version MinLemonVersion = new Version(2, 2, 0, 0);

        // Installer log, next to the installer exe (NOT in the game folder,
        // so it survives the old-files cleanup). Every status update and
        // every decision (versions found, downloads, copies) is recorded
        // here, so a bad install can be diagnosed from the log alone
        // instead of guessing from the game folder afterwards.
        private static readonly object LogLock = new object();
        private static readonly string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RageCoop.Client.Installer.log");

        private static void Log(string msg)
        {
            try
            {
                lock (LogLock)
                {
                    File.AppendAllText(LogPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + msg + "\r\n");
                }
            }
            catch { /* logging must never break the install */ }
        }

        private static string TryVer(string path)
        {
            try { return File.Exists(path) ? FileVersionInfo.GetVersionInfo(path).FileVersion : "(missing)"; }
            catch (Exception ex) { return "(unreadable: " + ex.Message + ")"; }
        }

        // Game build -> "legacy_enhanced" part of the ScriptHookV zip name
        // on dev-c.com, e.g. game 1.0.3889.0 -> ScriptHookV_3889.0_1158.13.zip.
        // Add a row here when a new game build ships; unknown builds fall
        // back to opening the download page instead of guessing a URL.
        private static readonly Dictionary<string, string> ScriptHookVDownloads = new Dictionary<string, string>
        {
            { "3889", "3889.0_1158.13" }
        };

        private void EnsurePrerequisites(string root, string shvPath, string shvdnPath, string scriptsPath, string lemonPath)
        {
            bool needDotNet = !IsNet48OrLater();
            bool needShv = !File.Exists(shvPath) || !ScriptHookMatchesGame(root, shvPath);
            bool needShvdn = !File.Exists(shvdnPath) || GetVer(shvdnPath) < MinShvdnVersion;
            bool needLemon = !File.Exists(lemonPath) || GetVer(lemonPath) < MinLemonVersion;

            Log("Found: .NET 4.8 present=" + !needDotNet + ", ScriptHookV=" + TryVer(shvPath) + ", SHVDN=" + TryVer(shvdnPath) + ", LemonUI=" + TryVer(lemonPath));
            Log("Need: dotnet=" + needDotNet + ", shv=" + needShv + ", shvdn=" + needShvdn + ", lemon=" + needLemon);

            if (!needDotNet && !needShv && !needShvdn && !needLemon)
            {
                EnsureScriptTimeout(root);
                return;
            }

            var missing = new List<string>();
            if (needDotNet) { missing.Add(".NET Framework 4.8 runtime"); }
            if (needShv) { missing.Add("ScriptHookV (matching your game build)"); }
            if (needShvdn) { missing.Add("ScriptHookVDotNet nightly"); }
            if (needLemon) { missing.Add("LemonUI 2.2"); }
            if (MessageBox.Show("RAGECOOP needs the following first:\n\n- " + string.Join("\n- ", missing) +
                "\n\nDownload and install them automatically?", "One-click setup", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            {
                Log("User declined automatic download.");
                MessageBox.Show("Install them manually and run this installer again. The manual steps are in TESTING.md in the repo.");
                Environment.Exit(1);
            }

            if (needDotNet) { InstallDotNet48(); }
            if (needShv) { InstallScriptHookV(root, shvPath); }
            if (needShvdn) { InstallShvdnNightly(root); }
            if (needLemon)
            {
                UpdateStatus("Installing LemonUI 2.2");
                Directory.CreateDirectory(scriptsPath);
                File.WriteAllBytes(lemonPath, getLemon());
                Log("Wrote embedded LemonUI 2.2 to " + lemonPath);
            }
            EnsureScriptTimeout(root);
        }

        private static bool IsNet48OrLater()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"))
                {
                    if (key == null) { return false; }
                    var release = key.GetValue("Release") as int?;
                    return release.HasValue && release.Value >= 528040;
                }
            }
            catch { return false; }
        }

        private void InstallDotNet48()
        {
            const string url = "https://go.microsoft.com/fwlink/?LinkId=2085155"; // .NET Framework 4.8 web installer
            UpdateStatus("Downloading .NET Framework 4.8");
            var installer = Path.Combine(Path.GetTempPath(), "ndp48-web.exe");
            try
            {
                HttpHelper.DownloadFile(url, installer, (p) => UpdateStatus("Downloading .NET Framework 4.8 " + p + "%"));
                UpdateStatus("Installing .NET Framework 4.8 (follow the prompts)");
                Process.Start(installer).WaitForExit();
                Log(".NET Framework 4.8 installer finished. Present now: " + IsNet48OrLater());
            }
            catch (Exception ex)
            {
                Log("Failed to download/install .NET Framework 4.8: " + ex.Message);
                MessageBox.Show("Could not download .NET Framework 4.8. Get it from https://dotnet.microsoft.com/download/dotnet-framework/net48 and run this installer again.");
                Process.Start("https://dotnet.microsoft.com/download/dotnet-framework/net48");
                Environment.Exit(1);
            }
            if (!IsNet48OrLater())
            {
                MessageBox.Show(".NET Framework 4.8 is still not installed. Install it, then run this installer again.");
                Environment.Exit(1);
            }
        }

        private bool ScriptHookMatchesGame(string root, string shvPath)
        {
            try
            {
                var game = Version.Parse(FileVersionInfo.GetVersionInfo(Path.Combine(root, "GTA5.exe")).FileVersion);
                return GetVer(shvPath).Build == game.Build;
            }
            catch { return false; }
        }

        private void InstallScriptHookV(string root, string shvPath)
        {
            var game = Version.Parse(FileVersionInfo.GetVersionInfo(Path.Combine(root, "GTA5.exe")).FileVersion);
            if (!ScriptHookVDownloads.TryGetValue(game.Build.ToString(), out var suffix))
            {
                MessageBox.Show($"No automatic ScriptHookV download for game build {game.Build}. Download it from https://dev-c.com/gtav/scripthookv/ (it must match your game build), copy ScriptHookV.dll and dinput8.dll into the game folder, then run this installer again.");
                Process.Start("https://dev-c.com/gtav/scripthookv/");
                Environment.Exit(1);
            }
            var url = $"https://dev-c.com/files/ScriptHookV_{suffix}.zip";
            UpdateStatus("Downloading ScriptHookV for game build " + game.Build);
            Log("Game build " + game.Build + ", downloading ScriptHookV from " + url);
            var work = NewTempDir();
            try
            {
                var zip = Path.Combine(work, "shv.zip");
                // dev-c.com rejects hotlinked downloads: without a Referer
                // from its own download page it 302s to the homepage, and
                // WebClient silently saves that HTML as the .zip — which
                // then fails to extract ("End of Central Directory record
                // could not be found"). The Referer is what makes this a
                // real zip; the IsZipFile check is the safety net.
                HttpHelper.DownloadFile(url, zip, (p) => UpdateStatus("Downloading ScriptHookV " + p + "%"), "https://dev-c.com/gtav/scripthookv/");
                Log("ScriptHookV download: " + new FileInfo(zip).Length + " bytes, valid zip: " + HttpHelper.IsZipFile(zip));
                if (!HttpHelper.IsZipFile(zip)) { DownloadFailedManual("ScriptHookV", "https://dev-c.com/gtav/scripthookv/"); }
                try { ZipFile.ExtractToDirectory(zip, work); }
                catch (InvalidDataException) { DownloadFailedManual("ScriptHookV", "https://dev-c.com/gtav/scripthookv/"); }
                CopyIfExists(Path.Combine(work, "ScriptHookV.dll"), Path.Combine(root, "ScriptHookV.dll"));
                CopyIfExists(Path.Combine(work, "dinput8.dll"), Path.Combine(root, "dinput8.dll"));
                if (!File.Exists(shvPath)) { throw new Exception("ScriptHookV.dll was not in the downloaded archive."); }
            }
            finally { TryDeleteDir(work); }
        }

        // The automatic download didn't produce a usable file. Tell the user
        // exactly what to do by hand instead of showing a stack trace.
        private void DownloadFailedManual(string name, string pageUrl)
        {
            Log("Download failed validation for " + name + " (not a usable file). Sent user to " + pageUrl + ".");
            MessageBox.Show("The automatic " + name + " download didn't work (the site returned something that isn't the file). Download it yourself from " + pageUrl + ", copy the files into the game folder, then run this installer again.");
            Process.Start(pageUrl);
            Environment.Exit(1);
        }

        private void InstallShvdnNightly(string root)
        {
            UpdateStatus("Finding latest ScriptHookVDotNet nightly");
            string zipUrl = null;
            try
            {
                var json = HttpHelper.DownloadString("https://api.github.com/repos/scripthookvdotnet/scripthookvdotnet-nightly/releases/latest");
                var m = Regex.Match(json, "\"browser_download_url\"\\s*:\\s*\"(?<url>https://[^\"]+\\.zip)\"");
                if (m.Success) { zipUrl = m.Groups["url"].Value; }
            }
            catch { /* fall through to the manual fallback below */ }
            if (zipUrl == null)
            {
                MessageBox.Show("Could not find the latest ScriptHookVDotNet nightly. Download it from https://github.com/scripthookvdotnet/scripthookvdotnet-nightly/releases and copy ScriptHookVDotNet3.dll and the loader .asi into the game folder, then run this installer again.");
                Process.Start("https://github.com/scripthookvdotnet/scripthookvdotnet-nightly/releases");
                Environment.Exit(1);
            }
            UpdateStatus("Downloading ScriptHookVDotNet nightly");
            Log("Latest SHVDN nightly: " + zipUrl);
            var work = NewTempDir();
            try
            {
                var zip = Path.Combine(work, "shvdn.zip");
                HttpHelper.DownloadFile(zipUrl, zip, (p) => UpdateStatus("Downloading ScriptHookVDotNet " + p + "%"));
                Log("SHVDN download: " + new FileInfo(zip).Length + " bytes, valid zip: " + HttpHelper.IsZipFile(zip));
                if (!HttpHelper.IsZipFile(zip)) { DownloadFailedManual("ScriptHookVDotNet nightly", "https://github.com/scripthookvdotnet/scripthookvdotnet-nightly/releases"); }
                try { ZipFile.ExtractToDirectory(zip, work); }
                catch (InvalidDataException) { DownloadFailedManual("ScriptHookVDotNet nightly", "https://github.com/scripthookvdotnet/scripthookvdotnet-nightly/releases"); }
                CopyIfExists(Path.Combine(work, "ScriptHookVDotNet3.dll"), Path.Combine(root, "ScriptHookVDotNet3.dll"));
                CopyIfExists(Path.Combine(work, "ScriptHookVDotNet2.dll"), Path.Combine(root, "ScriptHookVDotNet2.dll"));
                // Loader .asi — new nightlies call it ScriptHookVDotNet-Loader.asi.
                // Copy whichever name the zip has, then make sure only one
                // loader remains: having both .asi files loads SHVDN twice
                // and breaks every script (NullReferenceException in
                // GTA.Script..ctor, garbled console).
                CopyIfExists(Path.Combine(work, "ScriptHookVDotNet-Loader.asi"), Path.Combine(root, "ScriptHookVDotNet-Loader.asi"));
                CopyIfExists(Path.Combine(work, "ScriptHookVDotNet.asi"), Path.Combine(root, "ScriptHookVDotNet.asi"));
                if (File.Exists(Path.Combine(root, "ScriptHookVDotNet-Loader.asi")) && File.Exists(Path.Combine(root, "ScriptHookVDotNet.asi")))
                {
                    File.Delete(Path.Combine(root, "ScriptHookVDotNet.asi"));
                    Log("Deleted old ScriptHookVDotNet.asi (both loaders were present).");
                }
                // .ini only if the user doesn't have one — never overwrite
                // their settings (the timeout tweak is applied below).
                var srcIni = Path.Combine(work, "ScriptHookVDotNet.ini");
                var dstIni = Path.Combine(root, "ScriptHookVDotNet.ini");
                if (File.Exists(srcIni) && !File.Exists(dstIni)) { File.Copy(srcIni, dstIni); }
                if (!File.Exists(Path.Combine(root, "ScriptHookVDotNet3.dll"))) { throw new Exception("ScriptHookVDotNet3.dll was not in the downloaded archive."); }
            }
            finally { TryDeleteDir(work); }
        }

        private void EnsureScriptTimeout(string root)
        {
            const string key = "ScriptTimeoutThreshold";
            var ini = Path.Combine(root, "ScriptHookVDotNet.ini");
            try
            {
                if (!File.Exists(ini))
                {
                    File.WriteAllText(ini, key + "=15000\r\n");
                    return;
                }
                var lines = File.ReadAllLines(ini).ToList();
                var found = false;
                for (var i = 0; i < lines.Count; i++)
                {
                    var t = lines[i].Trim();
                    if (t.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        if (int.TryParse(t.Substring(key.Length + 1), out var v) && v >= 15000) { return; }
                        lines[i] = key + "=15000";
                    }
                }
                if (!found) { lines.Add(key + "=15000"); }
                File.WriteAllLines(ini, lines);
            }
            catch { /* timeout tweak is best-effort; a missing/broken ini must not block install */ }
        }

        private static string NewTempDir()
        {
            var dir = Path.Combine(Path.GetTempPath(), "ragecoop-install-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        private static void TryDeleteDir(string dir)
        {
            try { if (Directory.Exists(dir)) { Directory.Delete(dir, true); } } catch { }
        }

        private static void CopyIfExists(string src, string dst)
        {
            if (File.Exists(src)) { File.Copy(src, dst, true); Log("Copied " + Path.GetFileName(src) + " -> " + dst); }
            else { Log("Skipped copy (not in download): " + Path.GetFileName(src)); }
        }

        private void UpdateStatus(string status)
        {
            Log(status);
            Dispatcher.BeginInvoke(new Action(() => Status.Content = status));
        }

        private Version GetVer(string location)
        {
            return Version.Parse(FileVersionInfo.GetVersionInfo(location).FileVersion);
        }

        private byte[] getLemon()
        {
            return (byte[])Resource.ResourceManager.GetObject("LemonUI_SHVDN3");
        }

    }
}
