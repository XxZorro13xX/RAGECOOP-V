using System;
using System.IO;
using System.Net;
using System.Threading;

namespace RageCoop.Core
{
    internal static class HttpHelper
    {
        public static void DownloadFile(string url, string destination, Action<int> progressCallback, string referer = null)
        {
            if (File.Exists(destination)) { File.Delete(destination); }
            AutoResetEvent ae = new AutoResetEvent(false);
            WebClient client = new WebClient();
            client.Headers.Add("User-Agent", "RAGECOOP-Installer");
            if (!string.IsNullOrEmpty(referer)) { client.Headers.Add("Referer", referer); }

            // TLS only
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            client.DownloadProgressChanged += (s, e1) => progressCallback?.Invoke(e1.ProgressPercentage);
            client.DownloadFileCompleted += (s, e2) =>
            {
                ae.Set();
            };
            client.DownloadFileAsync(new Uri(url), destination);
            ae.WaitOne();
        }

        /// <summary>
        /// True if the file looks like a real zip (PK magic, non-trivial size).
        /// Servers that reject hotlinking return an HTML page instead of the
        /// file; catching that here beats a cryptic "End of Central Directory"
        /// error from ZipFile.ExtractToDirectory.
        /// </summary>
        public static bool IsZipFile(string path)
        {
            try
            {
                var info = new FileInfo(path);
                if (!info.Exists || info.Length < 100) { return false; }
                using (var s = File.OpenRead(path))
                {
                    return s.ReadByte() == (byte)'P' && s.ReadByte() == (byte)'K';
                }
            }
            catch { return false; }
        }

        public static string DownloadString(string url)
        {
            // TLS only
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 |
                                                   SecurityProtocolType.Tls11 |
                                                   SecurityProtocolType.Tls;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            WebClient client = new WebClient();
            client.Headers.Add("User-Agent", "RAGECOOP-Installer"); // GitHub API rejects requests without one
            return client.DownloadString(url);
        }
    }
}
