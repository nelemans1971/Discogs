using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;
using CDR.Console2File;
using CDR.Logging;

namespace DiscogsXML2MySQL
{
    class Import
    {
        private MySqlConnection conn = null;
        private string xmlFolder = @"C:\Data files\SVN\VisualStudio2017\Solutions\Discogs\Data\";
        private string exportFolder = Directory.GetCurrentDirectory();
        private bool runCreateSchema = false;

        public Import()
        {
            if (!Program.onlyTab)
            {
                conn = CDR.DB_Helper.NewMySQLConnection("mysql");
                runCreateSchema = ImportNewDBSchema(conn);
                ImportSPs(conn);
            }
        }

        /// <summary>
        /// Check if database exists if noit creates it.
        /// Also checks if db has correct character set.
        /// </summary>
        public static void CheckDBExistElseCreate()
        {
            if (!Program.onlyTab)
            {
                MySqlConnection conn = CDR.DB_Helper.NewMySQLConnection("mysql");
                if (conn != null)
                {
                    if (CreateDBIfNotExists(conn))
                    {
                        string collection;
                        if (!MySQLDBCollection(conn, out collection))
                        {
                            ConsoleLogger.WriteLine("MySQL Database must be in utf8mb4 collection!");
                            ConsoleLogger.WriteLine("Use mysql statement:");
                            ConsoleLogger.WriteLine("CREATE DATABASE IF NOT EXISTS DISCOGS CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;");
                            ConsoleLogger.WriteLine();
                            Environment.Exit(1);
                        }
                    }
                }
                else
                {
                    ConsoleLogger.WriteLine($"Can't connect to MySQL Server '{CDR.DB_Helper.mysqlServer}'.");
                    Environment.Exit(1);
                }
            }
        }

        public void Run()
        {
            // when conn == null not import into database is done
            // When Progrsam.onlyTab = true no connection is created so
            // tab files remain.

            DateTime dtStart = DateTime.Now;

            string discogsLocalLastDate = ArtistsXMLFile(xmlFolder);
            if (string.IsNullOrEmpty(discogsLocalLastDate) || discogsLocalLastDate.Length < 16)
            {
                // should never come here!
                ConsoleLogger.WriteLine("discogs xml files not found.");
                ConsoleLogger.WriteLine("Exiting...");
                Environment.Exit(1);
            }
            discogsLocalLastDate = discogsLocalLastDate.Substring(8, 8);

            ConsoleLogger.WriteLine("Exporting ARTISTS.XML data to TAB files.");
            if (ConvertArtistsXML2TAB(xmlFolder))
            {
                if (conn != null)
                {
                    ConsoleLogger.WriteLine("Importing ARTISTS TAB files into MySQL.");
                    XMLArtist.ImportArtistsData(exportFolder);
                    XMLArtist.CleanUpConvertedArtistsFiles(exportFolder);
                }
                ConsoleLogger.WriteLine("ARTIST Done.");
                ConsoleLogger.WriteLine();
            }

            // -------------------------------------------------------------------------------------------------------

            ConsoleLogger.WriteLine("Exporting LABELS.XML data to TAB files.");
            if (ConvertLabelsXML2TAB(xmlFolder))
            {
                if (conn != null)
                {
                    ConsoleLogger.WriteLine("Importing LABELS TAB files into MySQL.");
                    XMLLabel.ImportLabelsData(exportFolder);
                    XMLLabel.CleanUpConvertedLabelsFiles(exportFolder);
                }
                ConsoleLogger.WriteLine("LABELS Done.");
                ConsoleLogger.WriteLine();
            }

            // -------------------------------------------------------------------------------------------------------

            ConsoleLogger.WriteLine("Exporting RELEASES.XML data to TAB files.");
            if (ConvertReleasesXML2TAB(xmlFolder))
            {
                if (conn != null)
                {
                    ConsoleLogger.WriteLine("Importing RELEASES TAB files into MySQL.");
                    XMLRelease.ImportReleasesData(exportFolder);
                    XMLRelease.CleanUpConvertedReleasesFiles(exportFolder);
                }
                ConsoleLogger.WriteLine("RELEASES Done.");
                ConsoleLogger.WriteLine();
            }
            // -------------------------------------------------------------------------------------------------------

            ConsoleLogger.WriteLine("Exporting MASTERS.XML data to TAB files.");
            if (ConvertMastersXML2TAB(xmlFolder))
            {
                if (conn != null)
                {
                    ConsoleLogger.WriteLine("Importing MASTERS TAB files into MySQL.");
                    XMLMaster.ImportMastersData(exportFolder);
                    XMLMaster.CleanUpConvertedMastersFiles(exportFolder);
                }
                ConsoleLogger.WriteLine("MASTERS Done.");
                ConsoleLogger.WriteLine();
            }

            // if we created a schema then we can create the extra indexes (when onlyTab is set then
            // runCreateSchema = false)
            if (runCreateSchema && conn != null)
            {
                ConsoleLogger.WriteLine("Creating extra indexes.");
                ImportDBIndexes(conn);
                ConsoleLogger.WriteLine();
            }

            // Store date of imported xml in database, so we can detected if we need to update the database
            // or not.
            if (discogsLocalLastDate != null && discogsLocalLastDate.Length == 8)
            {
                SETTING_IU("DBLASTDATE", discogsLocalLastDate);
            }

            TimeSpan ts = (DateTime.Now - dtStart);
            ConsoleLogger.WriteLine();
            ConsoleLogger.WriteLine(String.Format("Elapsed index time {0:00}:{1:00}:{2:00}.{3:000}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds));
        }

        #region Discogs ARTIST

        private string ArtistsXMLFile(string xmlPath)
        {
            List<string> lArtists = new List<string>();
            foreach (string filename in System.IO.Directory.GetFiles(xmlPath))
            {
                if (Path.GetExtension(filename).ToUpper() == ".XML")
                {
                    string noExtension = Path.GetFileNameWithoutExtension(filename).ToUpper();
                    if (!(noExtension.Length > 8 && noExtension.Substring(0, 8) == "DISCOGS_"))
                    {
                        continue;
                    }
                    if (!(noExtension.Length >= 24 && noExtension.Substring(noExtension.Length - 8, 8) == "_ARTISTS"))
                    {
                        continue;
                    }
                    lArtists.Add(filename);
                }
            } //foreach

            if (lArtists.Count == 0)
            {
                return null;
            }

            lArtists.Sort();
            return lArtists[0];
        }

        public bool ConvertArtistsXML2TAB(string basePath)
        {
            string xmlFilename = ArtistsXMLFile(basePath);
            //xmlFilename = @"C:\Data files\SVN\VisualStudio2017\Solutions\Discogs\Data\ARTISTS-SMALL.XML";
            if (!string.IsNullOrEmpty(xmlFilename))
            {
                XmlSnibbitReader reader = new XmlSnibbitReader();
                if (reader.OpenFile(xmlFilename))
                {
                    string xmlBlock = "";
                    try
                    {
                        int blockCounter = 0;
                        while ((xmlBlock = reader.GetXMLSnibbit("artist")) != null)
                        {
                            XMLArtist artist = XMLArtist.ParseXML(XmlString2XmlElement(xmlBlock));
                            artist.StoreInTAB();
                            blockCounter++;
                            Console.Write($"\rXML Block: {blockCounter}");
                        } //while
                        Console.WriteLine();

                        return true;
                    }
                    catch (Exception e)
                    {
                        CDRLogger.Logger.LogError(e);
                        Console.WriteLine();
                        Console.WriteLine(xmlBlock);
                        Console.WriteLine(e.ToString());
                    }
                    finally
                    {
                        reader.Close();
                        XMLArtist.Clear();
                    }
                }
            }

            return false;
        }

        #endregion

        #region Discogs LABEL

        private string LabelsXMLFile(string xmlPath)
        {
            List<string> lLabels = new List<string>();
            foreach (string filename in System.IO.Directory.GetFiles(xmlPath))
            {
                if (Path.GetExtension(filename).ToUpper() == ".XML")
                {
                    string noExtension = Path.GetFileNameWithoutExtension(filename).ToUpper();
                    if (!(noExtension.Length > 8 && noExtension.Substring(0, 8) == "DISCOGS_"))
                    {
                        continue;
                    }
                    if (!(noExtension.Length >= 23 && noExtension.Substring(noExtension.Length - 7, 7) == "_LABELS"))
                    {
                        continue;
                    }
                    lLabels.Add(filename);
                }
            } //foreach

            if (lLabels.Count == 0)
            {
                return null;
            }

            lLabels.Sort();
            return lLabels[0];
        }

        public bool ConvertLabelsXML2TAB(string basePath)
        {
            string xmlFilename = LabelsXMLFile(basePath);
            //xmlFilename = @"C:\Data files\SVN\VisualStudio2017\Solutions\Discogs\Data\LABELS_SMALL.XML";
            if (!string.IsNullOrEmpty(xmlFilename))
            {
                XmlSnibbitReader reader = new XmlSnibbitReader();
                if (reader.OpenFile(xmlFilename))
                {
                    string xmlBlock = "";
                    try
                    {
                        int blockCounter = 0;
                        while ((xmlBlock = reader.GetXMLSnibbit("label")) != null)
                        {
                            XMLLabel label = XMLLabel.ParseXML(XmlString2XmlElement(xmlBlock));
                            label.StoreInTAB();
                            blockCounter++;
                            Console.Write($"\rXML Block: {blockCounter}");
                        } //while
                        Console.WriteLine();

                        return true;
                    }
                    catch (Exception e)
                    {
                        CDRLogger.Logger.LogError(e);
                        Console.WriteLine();
                        Console.WriteLine(xmlBlock);
                        Console.WriteLine(e.ToString());
                    }
                    finally
                    {
                        reader.Close();
                        XMLLabel.Clear();
                    }
                }
            }

            return false;
        }

        #endregion

        #region Discogs RELEASE

        private string ReleasesXMLFile(string xmlPath)
        {
            List<string> lReleases = new List<string>();
            foreach (string filename in System.IO.Directory.GetFiles(xmlPath))
            {
                if (Path.GetExtension(filename).ToUpper() == ".XML")
                {
                    string noExtension = Path.GetFileNameWithoutExtension(filename).ToUpper();
                    if (!(noExtension.Length > 8 && noExtension.Substring(0, 8) == "DISCOGS_"))
                    {
                        continue;
                    }
                    if (!(noExtension.Length >= 25 && noExtension.Substring(noExtension.Length - 9, 9) == "_RELEASES"))
                    {
                        continue;
                    }
                    lReleases.Add(filename);
                }
            } //foreach

            if (lReleases.Count == 0)
            {
                return null;
            }

            lReleases.Sort();
            return lReleases[0];
        }

        public bool ConvertReleasesXML2TAB(string basePath)
        {
            string xmlFilename = ReleasesXMLFile(basePath);
            //xmlFilename = @"C:\Data files\SVN\VisualStudio2017\Solutions\Discogs\Data\RELEASES_SMALL.XML";
            //xmlFilename = @"C:\Data files\SVN\VisualStudio2017\Solutions\Discogs\Data\RELEASES_SMALL2.XML";
            if (!string.IsNullOrEmpty(xmlFilename))
            {
                XmlSnibbitReader reader = new XmlSnibbitReader();
                if (reader.OpenFile(xmlFilename))
                {
                    XMLRelease.CleanUpConvertedReleasesFiles(exportFolder);

                    string xmlBlock = "";
                    try
                    {
                        int blockCounter = 0;
                        while ((xmlBlock = reader.GetXMLSnibbit("release")) != null)
                        {
                            XMLRelease release = XMLRelease.ParseXML(XmlString2XmlElement(xmlBlock));
                            release.StoreInTAB();
                            blockCounter++;
                            Console.Write($"\rXML Block: {blockCounter}");
                        } //while
                        Console.WriteLine();

                        return true;
                    }
                    catch (Exception e)
                    {
                        CDRLogger.Logger.LogError(e);
                        Console.WriteLine();
                        Console.WriteLine(xmlBlock);
                        Console.WriteLine(e.ToString());
                    }
                    finally
                    {
                        reader.Close();
                        XMLRelease.Clear();
                    }
                }
            }

            return false;
        }

        #endregion

        #region Discogs MASTER

        private string MastersXMLFile(string xmlPath)
        {
            List<string> lMasters = new List<string>();
            foreach (string filename in System.IO.Directory.GetFiles(xmlPath))
            {
                if (Path.GetExtension(filename).ToUpper() == ".XML")
                {
                    string noExtension = Path.GetFileNameWithoutExtension(filename).ToUpper();
                    if (!(noExtension.Length > 8 && noExtension.Substring(0, 8) == "DISCOGS_"))
                    {
                        continue;
                    }
                    if (!(noExtension.Length >= 24 && noExtension.Substring(noExtension.Length - 8, 8) == "_MASTERS"))
                    {
                        continue;
                    }
                    lMasters.Add(filename);
                }
            } //foreach

            if (lMasters.Count == 0)
            {
                return null;
            }

            lMasters.Sort();
            return lMasters[0];
        }

        public bool ConvertMastersXML2TAB(string basePath)
        {
            string xmlFilename = MastersXMLFile(basePath);
            //xmlFilename = @"C:\Data files\SVN\VisualStudio2017\Solutions\Discogs\Data\MASTERS_SMALL.XML";
            if (!string.IsNullOrEmpty(xmlFilename))
            {
                XmlSnibbitReader reader = new XmlSnibbitReader();
                if (reader.OpenFile(xmlFilename))
                {
                    XMLRelease.CleanUpConvertedReleasesFiles(exportFolder);

                    string xmlBlock = "";
                    try
                    {
                        int blockCounter = 0;
                        while ((xmlBlock = reader.GetXMLSnibbit("master")) != null)
                        {
                            XMLMaster master = XMLMaster.ParseXML(XmlString2XmlElement(xmlBlock));
                            master.StoreInTAB();
                            blockCounter++;
                            Console.Write($"\rXML Block: {blockCounter}");
                        } //while
                        Console.WriteLine();

                        return true;
                    }
                    catch (Exception e)
                    {
                        CDRLogger.Logger.LogError(e);
                        Console.WriteLine();
                        Console.WriteLine(xmlBlock);
                        Console.WriteLine(e.ToString());
                    }
                    finally
                    {
                        reader.Close();
                        XMLMaster.Clear();
                    }
                }
            }

            return false;
        }

        #endregion

        #region Helpers

        private XmlElement XmlString2XmlElement(string xml)
        {
            // Create the instance of XmlDocument
            XmlDocument doc = new XmlDocument();
            // Loads the XML from the string
            doc.LoadXml(xml);
            // Returns the XMLElement of the loaded XML String
            return doc.DocumentElement;
        }

        #endregion

        #region MySQL

        /// <summary>
        /// Get how database character set is created. MUST be utf8mb4!
        /// otherwise create database with this command:
        /// CREATE DATABASE discogs CHARACTER SET utf8mb4;
        /// </summary>
        private static bool MySQLDBCollection(MySqlConnection conn, out string collection)
        {
            collection = "";
            try
            {
                MySqlCommand command = new MySqlCommand();
                command.Connection = conn;
                command.CommandType = CommandType.Text;
                command.CommandText = "SHOW VARIABLES LIKE \"character_set_database\"";

                MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                DataSet ds = new DataSet();
                adapter.Fill(ds);
                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    collection = ds.Tables[0].Rows[0]["Value"].ToString();
                    return true;
                }
            }
            catch (Exception e)
            {
                CDRLogger.Logger.LogError(e);
                Console.WriteLine(e.ToString());
            }

            return false;
        }

        private static bool CreateDBIfNotExists(MySqlConnection conn)
        {
            try
            {
                MySqlCommand command = new MySqlCommand();
                command.Connection = conn;
                command.CommandTimeout = 0;
                command.CommandType = CommandType.Text;
                command.CommandText = Program.ExtractEmbededTextFile("dbCreate.sql").Replace("<%DATABASE%>", CDR.DB_Helper.mysqlDB);
                command.ExecuteNonQuery();
                return true;
            }
            catch (Exception e)
            {
                CDRLogger.Logger.LogError(e);
                Console.WriteLine(e.ToString());
            }

            return false;
        }

        /// <summary>
        /// Create the tables for the discogs database
        /// </summary>
        private bool ImportNewDBSchema(MySqlConnection conn)
        {
            try
            {
                MySqlCommand command = new MySqlCommand();
                command.Connection = conn;
                command.CommandType = CommandType.Text;
                command.CommandText = Program.ExtractEmbededTextFile("dbSchema.sql").Replace("<%DATABASE%>", CDR.DB_Helper.mysqlDB);
                command.ExecuteNonQuery();
                return true;
            }
            catch (Exception e)
            {
                CDRLogger.Logger.LogError(e);
                Console.WriteLine(e.ToString());
            }

            return false;
        }

        private bool ImportSPs(MySqlConnection conn)
        {
            try
            {
                string sp = Program.ExtractEmbededTextFile("dbStoredProcedures.sql").Replace("<%DATABASE%>", CDR.DB_Helper.mysqlDB);
                sp = sp.Replace("DELIMITER $$", "");
                sp = sp.Replace("DELIMITER ;", "");
                sp = sp.Replace("$$", ";");

                MySqlCommand command = new MySqlCommand();
                command.Connection = conn;
                command.CommandType = CommandType.Text;
                command.CommandText = sp;
                command.ExecuteNonQuery();

                return true;
            }
            catch (Exception e)
            {
                CDRLogger.Logger.LogError(e);
                Console.WriteLine(e.ToString());
            }

            return false;
        }

        private bool ImportDBIndexes(MySqlConnection conn)
        {
            try
            {
                MySqlCommand command = new MySqlCommand();
                command.Connection = conn;
                command.CommandTimeout = 0;
                command.CommandType = CommandType.Text;
                command.CommandText = Program.ExtractEmbededTextFile("dbIndexes.sql").Replace("<%DATABASE%>", CDR.DB_Helper.mysqlDB);
                command.ExecuteNonQuery();
                return true;
            }
            catch (Exception e)
            {
                CDRLogger.Logger.LogError(e);
                Console.WriteLine(e.ToString());
            }

            return false;
        }

        public static bool SETTING_IU(string name, string value)
        {
            try
            {
                using (MySqlConnection conn = CDR.DB_Helper.NewMySQLConnection())
                {

                    MySqlCommand command = new MySqlCommand();
                    command.Connection = conn;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "SETTING_IU";

                    command.Parameters.Add("@parNAME", MySqlDbType.VarChar, 40).Value = name;
                    command.Parameters.Add("@parVALUE", MySqlDbType.VarChar, 255).Value = value;

                    command.ExecuteNonQuery();

                    return true;
                } //using
            }
            catch (Exception e)
            {
                CDRLogger.Logger.LogError(e);
            }

            return false;
        }

        public static bool SETTING_S(string name, out string value)
        {
            value = null;
            try
            {
                using (MySqlConnection conn = CDR.DB_Helper.NewMySQLConnection())
                {
                    MySqlCommand command = new MySqlCommand();
                    command.Connection = conn;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "SETTING_S";

                    command.Parameters.Add("@parNAME", MySqlDbType.VarChar, 40).Value = name;

                    MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                    DataSet ds = new DataSet();
                    adapter.Fill(ds);
                    if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    {
                        value = ds.Tables[0].Rows[0]["VALUE"].ToString();
                    }

                    return true;
                } //using
            }
            catch { }

            return false;
        }

        #endregion
    }
}
