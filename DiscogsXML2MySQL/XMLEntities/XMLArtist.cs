using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using CDR.Logging;

namespace DiscogsXML2MySQL
{
    public class XMLArtist
    {
        // -----------------------------------------------------------------------------------------------------------
        // This are needed to not do duplicate inserts (they have there own ID's in de xml so we honer them)
        private static List<int> MEMBER_IDs = new List<int>();
        private static List<int> GROUP_IDs = new List<int>();

        private static StreamWriter swARTIST = null;
        private static StreamWriter swNAMEVARIATION = null;
        private static StreamWriter swALIAS = null;
        private static StreamWriter swMEMBER = null;
        private static StreamWriter swGROUP = null;
        private static StreamWriter swURL = null;
        private static StreamWriter swIMAGE = null;
        // -----------------------------------------------------------------------------------------------------------

        #region Data definition

        public int ARTIST_ID = -1;
        public string NAME = "";
        public string REALNAME = "";
        public string PROFILE = "";
        public string DATA_QUALITY = "";

        public List<Image> IMAGES = new List<Image>();
        public List<string> URLS = new List<string>();
        public List<string> NAMEVARIATIONS = new List<string>();
        public List<Alias> ALIASES = new List<Alias>();
        public List<Member> MEMBERS = new List<Member>();
        public List<Group> GROUPS = new List<Group>();

        public class Image
        {
            public int HEIGHT = -1;
            public int WIDTH = -1;
            public string TYPE = "";
            public string URI = "";
            public string URI150 = "";
        }

        public class Alias
        {
            public int ARTIST_ID = -1;
            public string NAME = "";
        }


        public class Member
        {
            public int ARTIST_ID = -1;
            public string NAME = "";
        }

        public class Group
        {
            public int ARTIST_ID = -1;
            public string NAME = "";
        }

        #endregion

        #region Parse XML

        public static XMLArtist ParseXML(XmlElement xArtist)
        {
            // -------------------------------------------------------------------------
            System.Globalization.NumberFormatInfo nfi = null;
            System.Globalization.CultureInfo culture = null;

            nfi = new System.Globalization.CultureInfo("en-US", false).NumberFormat;
            nfi.CurrencySymbol = "€";
            nfi.CurrencyDecimalDigits = 2;
            nfi.CurrencyDecimalSeparator = ".";
            nfi.NumberGroupSeparator = "";
            nfi.NumberDecimalSeparator = ".";

            culture = new System.Globalization.CultureInfo("en-US");
            // -------------------------------------------------------------------------


            XMLArtist artist = new XMLArtist();

            artist.ARTIST_ID = Convert.ToInt32(xArtist.GetElementsByTagName("id")[0].InnerText);
            artist.NAME = xArtist["name"].InnerText;
            if (xArtist["realname"] != null)
            {
                artist.REALNAME = xArtist["realname"].InnerText;
            }
            if (xArtist["profile"] != null)
            {
                artist.PROFILE = xArtist["profile"].InnerText;
            }
            artist.DATA_QUALITY = xArtist["data_quality"].InnerText;

            if (xArtist.GetElementsByTagName("images")[0] != null)
            {
                foreach (XmlNode xn in xArtist.GetElementsByTagName("images")[0].ChildNodes)
                {
                    XmlElement xImage = (XmlElement)xn;
                    Image image = new Image();
                    image.HEIGHT = Convert.ToInt32(xImage.Attributes["height"].Value);
                    image.WIDTH = Convert.ToInt32(xImage.Attributes["width"].Value);
                    image.TYPE = xImage.Attributes["type"].Value;
                    image.URI = xImage.Attributes["uri"].Value;
                    image.URI150 = xImage.Attributes["uri150"].Value;
                    artist.IMAGES.Add(image);
                } //foreach
            }

            if (xArtist.GetElementsByTagName("urls")[0] != null)
            {
                foreach (XmlNode xn in xArtist.GetElementsByTagName("urls")[0].ChildNodes)
                {
                    XmlElement xUrl = (XmlElement)xn;
                    if (!string.IsNullOrEmpty(xUrl.InnerText))
                    {
                        artist.URLS.Add(xUrl.InnerText.Trim());
                    }
                } //foreach
            }

            if (xArtist.GetElementsByTagName("namevariations")[0] != null)
            {
                foreach (XmlNode xn in xArtist.GetElementsByTagName("namevariations")[0].ChildNodes)
                {
                    XmlElement xName = (XmlElement)xn;
                    if (!string.IsNullOrEmpty(xName.InnerText))
                    {
                        artist.NAMEVARIATIONS.Add(xName.InnerText.Trim());
                    }
                } //foreach
            }

            if (xArtist.GetElementsByTagName("aliases")[0] != null)
            {
                foreach (XmlNode xn in xArtist.GetElementsByTagName("aliases")[0].ChildNodes)
                {
                    XmlElement xAlias = (XmlElement)xn;
                    Alias alias = new Alias();
                    if (!string.IsNullOrEmpty(xAlias.InnerText))
                    {
                        alias.ARTIST_ID = Convert.ToInt32(xAlias.Attributes["id"].Value);
                        alias.NAME = xAlias.InnerText;
                        artist.ALIASES.Add(alias);
                    }
                } //foreach
            }

            if (xArtist.GetElementsByTagName("members")[0] != null)
            {
                XmlElement xMembers = (XmlElement)xArtist.GetElementsByTagName("members")[0];

                foreach (XmlNode xn in xMembers.GetElementsByTagName("name"))
                {
                    XmlElement xMember = (XmlElement)xn;
                    Member member = new Member();
                    if (!string.IsNullOrEmpty(xMember.InnerText))
                    {
                        member.ARTIST_ID = Convert.ToInt32(xMember.Attributes["id"].Value);
                        member.NAME = xMember.InnerText;
                        artist.MEMBERS.Add(member);
                    }
                } //foreach
            }

            if (xArtist.GetElementsByTagName("groups")[0] != null)
            {
                foreach (XmlNode xn in xArtist.GetElementsByTagName("groups")[0].ChildNodes)
                {
                    XmlElement xMember = (XmlElement)xn;
                    Group group = new Group();
                    if (!string.IsNullOrEmpty(xMember.InnerText))
                    {
                        group.ARTIST_ID = Convert.ToInt32(xMember.Attributes["id"].Value);
                        group.NAME = xMember.InnerText;
                        artist.GROUPS.Add(group);
                    }
                } //foreach
            }

            return artist;
        }

        #endregion

        #region Export to file

        /// <summary>
        /// Write data to diffenrent tab seperated files, which kan be imported using local MySQL functions (this is way faster!)
        /// </summary>
        public void StoreInTAB()
        {
            if (swARTIST == null)
            {
                CreateTABSeperatedFiles();
            }

            swARTIST.WriteLine($"{this.ARTIST_ID}\t{CDR.DB_Helper.EscapeMySQL(this.NAME)}\t{CDR.DB_Helper.EscapeMySQL(this.REALNAME)}\t{CDR.DB_Helper.EscapeMySQL(this.PROFILE)}\t{CDR.DB_Helper.EscapeMySQL(this.DATA_QUALITY)}");

            foreach (Image image in this.IMAGES)
            {
                string uri = "\\N";
                if (!string.IsNullOrEmpty(image.URI))
                {
                    uri = CDR.DB_Helper.EscapeMySQL(image.URI);
                }
                string uri150 = "\\N";
                if (!string.IsNullOrEmpty(image.URI150))
                {
                    uri150 = CDR.DB_Helper.EscapeMySQL(image.URI150);
                }
                swIMAGE.WriteLine($"{this.ARTIST_ID}\t{image.HEIGHT}\t{image.WIDTH}\t{CDR.DB_Helper.EscapeMySQL(image.TYPE)}\t{uri}\t{uri150}");
            } //foreach

            foreach (string name in this.NAMEVARIATIONS)
            {
                swNAMEVARIATION.WriteLine($"{this.ARTIST_ID}\t{CDR.DB_Helper.EscapeMySQL(name)}");
            } //foreach

            foreach (Alias alias in this.ALIASES)
            {
                swALIAS.WriteLine($"{alias.ARTIST_ID}\t{CDR.DB_Helper.EscapeMySQL(alias.ARTIST_ID)}");
            } //foreach

            foreach (Member member in this.MEMBERS)
            {
                if (!MEMBER_IDs.Contains(member.ARTIST_ID))
                {
                    swMEMBER.WriteLine($"{member.ARTIST_ID}\t{this.ARTIST_ID}\t{CDR.DB_Helper.EscapeMySQL(member.ARTIST_ID)}");
                }
            } //foreach

            foreach (Group group in this.GROUPS)
            {
                if (!GROUP_IDs.Contains(group.ARTIST_ID))
                {
                    swGROUP.WriteLine($"{group.ARTIST_ID}\t{this.ARTIST_ID}\t{CDR.DB_Helper.EscapeMySQL(group.ARTIST_ID)}");
                }
            } //foreach

            foreach (string url in this.URLS)
            {
                swURL.WriteLine($"{this.ARTIST_ID}\t{CDR.DB_Helper.EscapeMySQL(url)}");
            } //foreach
        }

        public void CreateTABSeperatedFiles()
        {
            if (swARTIST == null)
            {
                CloseTABSeperatedFiles();

                Encoding utf8WithoutBom = new UTF8Encoding(false);

                swARTIST = new System.IO.StreamWriter(@"ARTIST.TAB", false, utf8WithoutBom);
                swNAMEVARIATION = new System.IO.StreamWriter(@"NAMEVARIATION.TAB", false, utf8WithoutBom);
                swALIAS = new System.IO.StreamWriter(@"ALIAS.TAB", false, utf8WithoutBom);
                swMEMBER = new System.IO.StreamWriter(@"MEMBER.TAB", false, utf8WithoutBom);
                swGROUP = new System.IO.StreamWriter(@"GROUP.TAB", false, utf8WithoutBom);
                swURL = new System.IO.StreamWriter(@"URL.TAB", false, utf8WithoutBom);
                swIMAGE = new System.IO.StreamWriter(@"IMAGE.TAB", false, utf8WithoutBom);

                GC.WaitForPendingFinalizers();
            }
        }

        public static void CloseTABSeperatedFiles()
        {
            CloseStreamWriter(ref swARTIST);
            CloseStreamWriter(ref swNAMEVARIATION);
            CloseStreamWriter(ref swALIAS);
            CloseStreamWriter(ref swMEMBER);
            CloseStreamWriter(ref swGROUP);
            CloseStreamWriter(ref swURL);
            CloseStreamWriter(ref swIMAGE);
        }

        private static void CloseStreamWriter(ref StreamWriter sw)
        {
            if (sw != null)
            {
                sw.Close();
                sw = null;
            }
        }

        /// <summary>
        /// Remove tab files, to save space
        /// </summary>
        public static void CleanUpConvertedArtistsFiles(string tabFolder)
        {
            DeleteFile(Path.Combine(tabFolder, "ARTIST.TAB"));
            DeleteFile(Path.Combine(tabFolder, "NAMEVARIATION.TAB"));
            DeleteFile(Path.Combine(tabFolder, "ALIAS.TAB"));
            DeleteFile(Path.Combine(tabFolder, "MEMBER.TAB"));
            DeleteFile(Path.Combine(tabFolder, "GROUP.TAB"));
            DeleteFile(Path.Combine(tabFolder, "IMAGE.TAB"));
            DeleteFile(Path.Combine(tabFolder, "URL.TAB"));
        }

        private static void DeleteFile(string filename)
        {
            if (File.Exists(filename))
            {
                try
                {
                    File.Delete(filename);
                }
                catch { }
            }
        }

        #endregion

        #region Import into MySQL

        public static void ImportArtistsData(string tabFolder)
        {
            using (MySql.Data.MySqlClient.MySqlConnection conn = CDR.DB_Helper.NewMySQLConnection())
            {
                CDR.DB_Helper.LOAD_DATA_LOCAL_INFILE(conn, Path.Combine(tabFolder, "ARTIST.TAB"), "ARTIST", "ARTIST_ID, `NAME`, REALNAME, `PROFILE`, DATA_QUALITY", "");
                CDR.DB_Helper.LOAD_DATA_LOCAL_INFILE(conn, Path.Combine(tabFolder, "NAMEVARIATION.TAB"), "NAMEVARIATION", "ARTIST_ID, `NAME`", "NAMEVARIATION_ID");
                CDR.DB_Helper.LOAD_DATA_LOCAL_INFILE(conn, Path.Combine(tabFolder, "ALIAS.TAB"), "ALIAS", "MAIN_ARTIST_ID, ALIAS_ARTIST_ID", "ALIAS_ID");
                CDR.DB_Helper.LOAD_DATA_LOCAL_INFILE(conn, Path.Combine(tabFolder, "MEMBER.TAB"), "MEMBER", "MAIN_ARTIST_ID, MEMBER_ARTIST_ID", "MEMBER_ID");
                CDR.DB_Helper.LOAD_DATA_LOCAL_INFILE(conn, Path.Combine(tabFolder, "GROUP.TAB"), "GROUP", "MAIN_ARTIST_ID, GROUP_ARTIST_ID", "GROUP_ID");
                CDR.DB_Helper.LOAD_DATA_LOCAL_INFILE(conn, Path.Combine(tabFolder, "IMAGE.TAB"), "IMAGE", "ARTIST_ID, HEIGHT, WIDTH, `TYPE`, URI, URI150", "IMAGE_ID");
                CDR.DB_Helper.LOAD_DATA_LOCAL_INFILE(conn, Path.Combine(tabFolder, "URL.TAB"), "URL", "ARTIST_ID, URL", "URL_ID");
            } //using
        }

        #endregion

        /// <summary>
        /// Clear static storage
        /// </summary>
        public static void Clear()
        {
            MEMBER_IDs = new List<int>();
            GROUP_IDs = new List<int>();
            CloseTABSeperatedFiles();
        }
    }


    public class Artist
    {
        public int ARTIST_ID = -1;
        public string NAME = "";
        public string ANV = "";
        public string JOIN = "";
        public string ROLE = "";
        public bool EXTRA_ARTIST = false;

        public object Clone()
        {
            Artist artist = new Artist();
            artist.ARTIST_ID = this.ARTIST_ID;
            artist.NAME = this.NAME;
            artist.ANV = this.ANV;
            artist.JOIN = this.JOIN;
            artist.ROLE = this.ROLE;
            artist.EXTRA_ARTIST = this.EXTRA_ARTIST;

            return artist;
        }

        public static List<Artist> Clone(List<Artist> list)
        {
            List<Artist> newList = new List<Artist>();
            foreach (Artist a in list)
            {
                Artist artist = new Artist();
                artist.ARTIST_ID = a.ARTIST_ID;
                artist.NAME = a.NAME;
                artist.ANV = a.ANV;
                artist.JOIN = a.JOIN;
                artist.ROLE = a.ROLE;
                artist.EXTRA_ARTIST = a.EXTRA_ARTIST;
                newList.Add(artist);
            }

            return newList;
        }

        public static List<Artist> ParseArtists(XmlElement xRelease)
        {
            List<Artist> artists = new List<Artist>();

            if (xRelease.GetElementsByTagName("artists")[0] != null)
            {
                foreach (XmlNode xn in xRelease.GetElementsByTagName("artists")[0].ChildNodes)
                {
                    XmlElement xArtist = (XmlElement)xn;
                    Artist artist = new Artist();
                    artist.ARTIST_ID = Convert.ToInt32(xArtist["id"].InnerText);
                    artist.NAME = xArtist["name"].InnerText;
                    artist.ANV = xArtist["anv"].InnerText;
                    artist.JOIN = xArtist["join"].InnerText;
                    artist.ROLE = xArtist["role"].InnerText;
                    artist.EXTRA_ARTIST = false;

                    artists.Add(artist);
                } //foreach
            }
            if (xRelease.GetElementsByTagName("extraartists")[0] != null)
            {
                foreach (XmlNode xn in xRelease.GetElementsByTagName("extraartists")[0].ChildNodes)
                {
                    XmlElement xArtist = (XmlElement)xn;
                    Artist artist = new Artist();
                    artist.ARTIST_ID = Convert.ToInt32(xArtist["id"].InnerText);
                    artist.NAME = xArtist["name"].InnerText;
                    artist.ANV = xArtist["anv"].InnerText;
                    artist.JOIN = xArtist["join"].InnerText;
                    artist.ROLE = xArtist["role"].InnerText;
                    artist.EXTRA_ARTIST = true;

                    artists.Add(artist);
                } //foreach
            }

            return artists;
        }

    }

}
