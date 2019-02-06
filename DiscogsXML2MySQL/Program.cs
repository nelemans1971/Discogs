using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CDR.Console2File;
using CDR.Logging;
using IniParser;
using IniParser.Model;

namespace DiscogsXML2MySQL
{
    class Program
    {
        public static bool forcedownload = false;
        public static bool forceUpdate = false;
        public static bool onlyTab = false;
        public static bool useExistingFiles = false;

        /// <summary>
        /// Schema reference
        /// https://github.com/alvare/discogs2pg/blob/master/sql/tables.sql
        /// AND
        /// https://github.com/coder11/discogs-db/blob/master/schema.sql
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            string AppName = System.IO.Path.GetFileName(System.IO.Path.ChangeExtension(System.Reflection.Assembly.GetExecutingAssembly().Location, null));
            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            string versionType = "";
#if DEBUG
            versionType = " [DEBUG]";
#endif
            RenameINIFile();
            ReadIniEMailSettings();

            string title = String.Format("{0} v{1:0}.{2:00}{3}", AppName, version.Major, version.Minor, versionType);
            Console.Title = title;
            ConsoleLogger.WriteLine(title);
            ConsoleLogger.WriteLine();

            Console.WriteLine("Parameters:");
            Console.WriteLine("/forcedownload    - (re)download files from discogs even when they are allready exist localy");
            Console.WriteLine("/forceupdate      - import database even when database is allready up to date");
            Console.WriteLine("/onlytab          - don't import data into database only create tab seperated files");
            Console.WriteLine("/useexistingfiles - use the xml files in the data directory (use this for old versions)");
            Console.WriteLine();
            foreach (string arg in args)
            {
                if (arg.ToLower() == "/forcedownload")
                {
                    forcedownload = true;
                }
                else if (arg.ToLower() == "/forceupdate")
                {
                    forceUpdate = true;
                }
                else if (arg.ToLower() == "/onlytab")
                {
                    onlyTab = true;
                }
                else if (arg.ToLower() == "/useexistingfiles")
                {
                    useExistingFiles = true;
                }
            } //foreach

            // When only exporting to file no database is needed so don't connect to it.
            if (!onlyTab)
            {
                ReadIniDBConnection();
                // Now test database connection before continuing
                Import.CheckDBExistElseCreate();
            }

            ConsoleLogger.WriteLine($"Using (xml) data stored in '{DataPath}'.");
            ConsoleLogger.WriteLine();

            try
            {
                DiscogsDataDownloader downloader = new DiscogsDataDownloader();
                // Get three dates from different locations
                string discogsLocalLastDate = downloader.DiscogsLocalLastDate();
                string discogsRemoteLastDate = discogsLocalLastDate;
                string dbLastDate = null;
                // When only exporting to file no database is needed so don't connect to it.
                if (!onlyTab)
                {
                    Import.SETTING_S("DBLASTDATE", out dbLastDate);
                }

                bool downloadResult = true;
                if (!Program.useExistingFiles)
                {
                    discogsRemoteLastDate = downloader.DiscogsRemoteLastDate();

                    if (forcedownload || string.IsNullOrEmpty(discogsLocalLastDate) || discogsRemoteLastDate != discogsLocalLastDate)
                    {
                        if (!downloader.DownloadDiscogsNewData())
                        {
                            // No local files available, we can't do anything
                            ConsoleLogger.WriteLine("Discogs download failed.");
                            ConsoleLogger.WriteLine("Try manual download and placing them in de data direcotry.");
                            ConsoleLogger.WriteLine("Exiting...");
                            Environment.Exit(1);
                        }
                    }
                }

                // xml should be available at this point.
                // --------------------------------------
                Import import = new Import();
                if (!string.IsNullOrEmpty(dbLastDate) && dbLastDate.Length == 8)
                {
                    // Check to see if we're downloading the same version as allready is in the database
                    // when that is the case exit and do nothing!
                    if (!forcedownload && discogsRemoteLastDate != null && discogsRemoteLastDate.Length == 8)
                    {
                        if (dbLastDate == discogsRemoteLastDate && !forceUpdate)
                        {
                            ConsoleLogger.WriteLine($"The last discogs export ({discogsRemoteLastDate}) is allready in the database. Exiting...");
                            Environment.Exit(0);
                        }
                    }
                }

                if (downloadResult)
                {
                    import.Run();
                }
                else
                {
                    ConsoleLogger.WriteLine("No discogs xml files found.");
                }
            }
            catch (Exception e)
            {
                CDRLogger.Logger.LogError(e);
            }


            if (CDRLogger.MemoryLogger.Count > 0)
            {
#if DEBUG
                Console.WriteLine();
                CDRLogger.WriteToConsole();
                Console.WriteLine("Press enter to close program.");
                Console.ReadLine();
#else
                CDRLogger.WriteToConsole();
                CDRLogger.SendSmtpOfMemoryLog(CDRLogger.emailAdress, "");
#endif

                // exit with error 1 that something is gonne wrong
                Environment.Exit(1);
            }
#if DEBUG
            Console.WriteLine("Press enter to close program.");
            Console.ReadLine();
#endif
        }

        #region Ini file stuff

        public static string IniFilename
        {
            get
            {
                return System.IO.Path.ChangeExtension(System.Reflection.Assembly.GetExecutingAssembly().Location, ".ini");
            }
        }

        /// <summary>
        /// Use the correct INI file, used for development!
        /// </summary>
        private static void RenameINIFile()
        {
            string iniFilename = IniFilename;
#if DEBUG
            string releaseFilename = iniFilename.Replace(".ini", ".DEBUG.ini");
#else
            string releaseFilename = iniFilename.Replace(".ini", ".RELEASE.ini");
#endif
            if (System.IO.File.Exists(releaseFilename))
            {
                try
                {
                    System.IO.File.Delete(iniFilename);
                }
                catch
                {
                }
                GC.WaitForPendingFinalizers();
                System.IO.File.Move(releaseFilename, iniFilename);
                GC.WaitForPendingFinalizers();
            }
        }

        public static void ReadIniEMailSettings()
        {
            try
            {
                FileIniDataParser parser = new FileIniDataParser();
                IniData data = parser.ReadFile(IniFilename);

                CDRLogger.smtpServer = data["Program"]["smtpServer"];
                CDRLogger.emailAdress = data["Program"]["email"];
            }
            catch { }
        }

        /// <summary>
        /// Read database settings
        /// </summary>
        public static void ReadIniDBConnection()
        {
            try
            {
                FileIniDataParser parser = new FileIniDataParser();
                IniData data = parser.ReadFile(IniFilename);

                CDR.DB_Helper.mysqlServer = data["DBConnnection"]["mysqlServer"];
                CDR.DB_Helper.mysqlDB = data["DBConnnection"]["mysqlDB"];
                CDR.DB_Helper.mysqlUser = data["DBConnnection"]["mysqlUser"];
                CDR.DB_Helper.mysqlPassword = data["DBConnnection"]["mysqlPassword"];
                int.TryParse(data["DBConnnection"]["mysqlServerPort"], out CDR.DB_Helper.mysqlServerPort);
            }
            catch (Exception e)
            {
                CDRLogger.Logger.LogError(e);
                ConsoleLogger.WriteLine("Error reading Database connection settings.");
                Environment.Exit(1);
            }
        }

        private static string dataPath = "";
        public static string DataPath
        {
            get
            {
                if (string.IsNullOrEmpty(dataPath))
                {
                    FileIniDataParser parser = new FileIniDataParser();
                    IniData data = parser.ReadFile(Program.IniFilename);

                    string tmpS = data["Program"]["DataPath"];
                    if (string.IsNullOrEmpty(tmpS))
                    {
                        tmpS = @".\";
                    }
                    dataPath = Path.GetFullPath(tmpS);
                    if (!Directory.Exists(dataPath))
                    {
                        Directory.CreateDirectory(dataPath);
                    }
                }

                return dataPath;
            }
        }

        #endregion

        #region Embedded resources

        /// <summary>
        /// Extracts an embedded file out of a given assembly.
        /// http://www.csharper.net/blog/getting_an_embedded_resource_file_out_of_an_assembly.aspx
        /// </summary>
        /// <param name="assemblyName">The namespace of you assembly.</param>
        /// <param name="fileName">The name of the file to extract.</param>
        /// <returns>A stream containing the file data.</returns>
        private static Stream GetEmbeddedFile(string assemblyName, string fileName)
        {
            try
            {
                System.Reflection.Assembly a = System.Reflection.Assembly.Load(assemblyName);
                Stream str = a.GetManifestResourceStream(assemblyName + "." + fileName);

                if (str == null)
                    throw new Exception("Could not locate embedded resource '" + fileName + "' in assembly '" + assemblyName + "'");
                return str;
            }
            catch (Exception e)
            {
                throw new Exception(assemblyName + ": " + e.Message);
            }
        }
        private static Stream GetEmbeddedFile(System.Reflection.Assembly assembly, string fileName)
        {
            string assemblyName = assembly.GetName().Name;
            return GetEmbeddedFile(assemblyName, fileName);
        }
        private static Stream GetEmbeddedFile(Type type, string fileName)
        {
            string assemblyName = type.Assembly.GetName().Name;
            return GetEmbeddedFile(assemblyName, fileName);
        }

        /// <summary>
        /// Reads Embedded resources returns it as one "big" string
        /// </summary>
        public static string ExtractEmbededTextFile(string filename)
        {
            try
            {
                string AppName = System.IO.Path.GetFileName(System.IO.Path.ChangeExtension(System.Reflection.Assembly.GetExecutingAssembly().Location, null));
                Stream stream = GetEmbeddedFile(AppName, "Resources." + filename);
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
            catch { }

            return null;
        }

        #endregion


    }
}
