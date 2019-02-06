using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using CDR.Console2File;
using CDR.Extensions;
using CDR.Logging;
using IniParser;
using IniParser.Model;

namespace DiscogsXML2MySQL
{
    public class DiscogsDataDownloader
    {
        private bool s3SettingsFromDiscogsDownloaded = false;
        private string discogsDataURL = "https://data.discogs.com";
        private string BUCKET_URL = "//discogs-data.s3-us-west-2.amazonaws.com";
        private string S3B_ROOT_DIR = "data/";

        public DiscogsDataDownloader()
        {
            System.Net.ServicePointManager.Expect100Continue = true;
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            // Use SecurityProtocolType.Ssl3 if needed for compatibility reasons
            if (System.Net.ServicePointManager.ServerCertificateValidationCallback == null)
            {
                System.Net.ServicePointManager.ServerCertificateValidationCallback += ValidateRemoteCertificate;
            }

            s3SettingsFromDiscogsDownloaded = false;
            ReadIniSettings();
        }

        private static bool ValidateRemoteCertificate(object sender, System.Security.Cryptography.X509Certificates.X509Certificate cert, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors error)
        {
            return true;
        }

        /// <summary>
        /// Get date from xml files from discogs S3 storage location.
        /// null when no files could be found.
        /// </summary>
        public string DiscogsRemoteLastDate()
        {
            DownloadS3SettingsFromDiscogs();
            int year = DateTime.Now.Year;
            List<string> xmlFiles = LatestRemoteXMLFiles(year);
            if (xmlFiles == null)
            {
                year--;
                // begin of new year, try old year
                xmlFiles = LatestRemoteXMLFiles(year);
            }
            if (xmlFiles != null && xmlFiles[0].Length >= 16)
            {
                // Return remote date from filename
                return xmlFiles[0].Substring(8, 8);
            }

            return null;
        }

        /// <summary>
        /// Get date from local .xml file (not gz because we need to unzip them before we can do something with it)
        /// null when no files could be found.
        /// </summary>
        public string DiscogsLocalLastDate()
        {
            List<string> files = new List<string>();
            foreach (string s in Directory.GetFiles(Program.DataPath))
            {
                if (Path.GetExtension(s).ToLower() == ".xml")
                {
                    files.Add(s);
                }
            } //foreach

            files.Sort();
            files.Reverse(); // newest files first (date is in name)
            if (files.Count > 0 && files[0].Length >= 16)
            {
                try
                {
                    // Return date from filename
                    string date = files[0].Substring(8, 8);
                    int d;
                    if (int.TryParse(date, out d))
                    {
                        return date;
                    }
                }
                catch { }
            }

            return null;
        }

        /// <summary>
        /// Download lastest xml files from discogs. If we allready have them, don't download again!
        /// </summary>
        public bool DownloadDiscogsNewData()
        {
            DateTime dtStart = DateTime.Now;
            try
            {
                DownloadS3SettingsFromDiscogs();
                int year = DateTime.Now.Year;
                List<string> xmlFiles = LatestRemoteXMLFiles(year);
                if (xmlFiles == null)
                {
                    year--;
                    // begin of new year, try old year
                    xmlFiles = LatestRemoteXMLFiles(year);
                }
                if (xmlFiles != null)
                {
                    ConsoleLogger.WriteLine("Download xml files from discogs.");
                    // Start download the 4 files
                    foreach (string file in xmlFiles)
                    {
                        // Only download file if we dont have if compressed/uncompressed version.
                        if (!(File.Exists(Path.Combine(Program.DataPath, file)) || File.Exists(Path.Combine(Program.DataPath, file).Replace(".gz", ""))))
                        {
                            ConsoleLogger.WriteLine($"Downloading {file}.");
                            DownloadFile(year, file);
                        }
                        else
                        {
                            ConsoleLogger.WriteLine($"{file} allready downloaded.");
                        }
                    } //foreach
                    ConsoleLogger.WriteLine();

                    ConsoleLogger.WriteLine("Decompressing downloaded gz files.");
                    foreach (string file in xmlFiles)
                    {
                        if (!File.Exists(Path.Combine(Program.DataPath, file).Replace(".gz", "")))
                        {
                            ConsoleLogger.WriteLine($"Decompressing {file}.");
                            GZipDecompress(Path.Combine(Program.DataPath, file), true);
                        }
                        else
                        {
                            ConsoleLogger.WriteLine($"{file} allready decompressed.");
                        }
                    } //foreach
                    ConsoleLogger.WriteLine();

                    return true;
                }
            }
            finally
            {
                TimeSpan ts = (DateTime.Now - dtStart);
                ConsoleLogger.WriteLine();
                ConsoleLogger.WriteLine(String.Format("Elapsed index time {0:00}:{1:00}:{2:00}.{3:000}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds));
                ConsoleLogger.WriteLine();
            }

            return false;
        }

        private void ReadIniSettings()
        {
            FileIniDataParser parser = new FileIniDataParser();
            IniData data = parser.ReadFile(Program.IniFilename);

            string tmpS = data["Program"]["DiscogsDataURL"];
            if (!string.IsNullOrEmpty(tmpS))
            {
                discogsDataURL = tmpS;
            }
        }


        private bool GZipDecompress(string filename, bool removeGZipFile = false)
        {
            try
            {
                using (FileStream fileReader = File.OpenRead(filename))
                using (FileStream fileWriter = File.OpenWrite(filename.Replace(".gz", "")))
                using (GZipStream compressionStream = new GZipStream(fileReader, CompressionMode.Decompress))
                {
                    // Decompresses and reads data from stream to file
                    int readlength = 0;
                    byte[] buffer = new byte[1024];
                    do
                    {
                        readlength = compressionStream.Read(buffer, 0, buffer.Length);
                        fileWriter.Write(buffer, 0, readlength);

                    } while (readlength > 0);
                } //using
                GC.WaitForPendingFinalizers();

                if (File.Exists(filename.Replace(".gz", "")) && removeGZipFile)
                {
                    try
                    {
                        File.Delete(filename);
                    }
                    catch { }
                }

                return true;
            }
            catch { }

            return false;
        }

        private bool downloadComplete = false;

        private bool DownloadFile(int year, string filename)
        {
            string newFilename = Path.Combine(Program.DataPath, filename);
            try
            {
                if (File.Exists(newFilename))
                {
                    File.Delete(newFilename);
                }

                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                    client.Headers.Add("Accept-Encoding", "gzip, deflate");
                    client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(webClient_DownloadProgressChanged);
                    client.DownloadFileCompleted += new AsyncCompletedEventHandler(webClient_DownloadFileCompleted);
                    client.DownloadFileAsync(new System.Uri($"https:{BUCKET_URL}/data/{year}/{filename}"), newFilename);
                    while (!downloadComplete)
                    {
                        System.Threading.Thread.Sleep(100);
                    } //while
                } //using
            }
            catch
            {
                return false;
            }
            finally
            {
                downloadComplete = false;
            }

            return File.Exists(newFilename);
        }

        // Event to track the progress
        private void webClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Console.Write($"\r{e.ProgressPercentage}%");
        }
        private void webClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            downloadComplete = true;
            Console.WriteLine();
        }

        private void DownloadS3SettingsFromDiscogs()
        {
            if (!s3SettingsFromDiscogsDownloaded)
            {
                string pageContent = "";
                try
                {
                    WebClient client = new WebClient();
                    // Add a user agent header in case the
                    // requested URI contains a query.
                    client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                    Stream data = client.OpenRead(discogsDataURL);
                    StreamReader reader = new StreamReader(data);
                    pageContent = reader.ReadToEnd();
                    data.Close();
                    reader.Close();
                }
                catch { }
                int p = pageContent.IndexOf("BUCKET_URL");
                if (p >= 0)
                {
                    int p2 = pageContent.IndexOf("=", p);
                    int p3 = pageContent.IndexOf(";", p);
                    string s = pageContent.Substring(p2 + 1, p3 - p2);
                    s = s.Replace(";", "").Trim();
                    s = s.Replace("'", "").Trim();
                    if (!string.IsNullOrEmpty(s))
                    {
                        BUCKET_URL = s;
                    }
                }

                p = pageContent.IndexOf("S3B_ROOT_DIR");
                if (p >= 0)
                {
                    int p2 = pageContent.IndexOf("=", p);
                    int p3 = pageContent.IndexOf(";", p);
                    string s = pageContent.Substring(p2 + 1, p3 - p2);
                    s = s.Replace(";", "").Trim();
                    s = s.Replace("'", "").Trim();
                    if (!string.IsNullOrEmpty(s))
                    {
                        S3B_ROOT_DIR = s;
                    }
                }

                s3SettingsFromDiscogsDownloaded = true;
            }
        }

        private List<string> LatestRemoteXMLFiles(int year)
        {
            string pageContent = "";
            try
            {
                WebClient client = new WebClient();
                // Add a user agent header in case the
                // requested URI contains a query.
                client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                client.Headers.Add("Accept-Encoding", "gzip, deflate");
                Stream data = client.OpenRead($"https:{BUCKET_URL}/?delimiter=/&prefix=data/{year}/");
                StreamReader reader = new StreamReader(data);
                pageContent = reader.ReadToEnd();
                data.Close();
                reader.Close();

                // Create the instance of XmlDocument
                XmlDocument doc = new XmlDocument();
                // Loads the XML from the string
                doc.LoadXml(pageContent);

                List<string> awsFiles = new List<string>();
                XmlElement xListBucketResult = doc.DocumentElement;

                if (xListBucketResult.GetElementsByTagName("Contents") != null)
                {
                    foreach (XmlNode xn in xListBucketResult.GetElementsByTagName("Contents"))
                    {
                        XmlElement xContents = (XmlElement)xn;
                        System.Text.RegularExpressions.Regex rx = new System.Text.RegularExpressions.Regex(@"discogs_[0-9]{8}_(artists|labels|masters|releases).xml.gz",
                            System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        // Find matches.
                        System.Text.RegularExpressions.MatchCollection matches = rx.Matches(xContents["Key"].InnerText);
                        // Report on each match.
                        foreach (System.Text.RegularExpressions.Match match in matches)
                        {
                            awsFiles.Add(match.Value);
                        }
                    } //foreach
                }
                if (awsFiles.Count >= 4)
                {
                    return awsFiles.TakeLast(4).ToList();
                }
            }
            catch { }

            return null; // nothing found
        }

    }
}
