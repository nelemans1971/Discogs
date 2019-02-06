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
    public class XMLMaster
    {
        // -----------------------------------------------------------------------------------------------------------
        private static StreamWriter swMASTER = null;
        private static StreamWriter swIMAGE = null;
        private static StreamWriter swMASTER_ARTIST = null;
        private static StreamWriter swMASTER_GENRE = null;
        private static StreamWriter swMASTER_STYLE = null;
        private static StreamWriter swMASTER_VIDEO = null;
        // -----------------------------------------------------------------------------------------------------------

        #region Data definition

        public int MASTER_ID = -1;
        public int MAIN_RELEASE_ID = -1;
        public string TITLE = "";
        public DateTime RELEASED = DateTime.MinValue; // only year is valid when it is a master record
        public string NOTES = "";
        public string DATA_QUALITY = "";

        public List<Image> IMAGES = new List<Image>();
        public List<Artist> ARTISTS = new List<Artist>(); // Note: no <extraartists> tag!
        public List<string> GENRES = new List<string>();
        public List<string> STYLES = new List<string>();
        public List<Video> VIDEOS = new List<Video>();

        public class Image
        {
            public int HEIGHT = -1;
            public int WIDTH = -1;
            public string TYPE = "";
            public string URI = "";
            public string URI150 = "";
        }

        public class Video
        {
            public bool EMBED = false;
            public int DURATION_IN_SEC = 0;
            public string SRC = "";
            public string TITLE = "";
            public string DESCRIPTION = "";
        }

        #endregion

        #region Parse XML

        public static XMLMaster ParseXML(XmlElement xMaster)
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


            XMLMaster master = new XMLMaster();

            master.MASTER_ID = Convert.ToInt32(xMaster.Attributes["id"].Value);
            master.TITLE = xMaster["title"].InnerText;
            if (xMaster["year"] != null)
            {
                DateTime.TryParse($"{xMaster["year"].InnerText}-01-01", out master.RELEASED);
            }
            if (xMaster["notes"] != null)
            {
                master.NOTES = xMaster["notes"].InnerText;
            }
            if (xMaster["main_release"] != null)
            {
                master.MAIN_RELEASE_ID = Convert.ToInt32(xMaster["main_release"].InnerText);
            }
            if (xMaster["data_quality"] != null)
            {
                master.DATA_QUALITY = xMaster["data_quality"].InnerText;
            }

            if (xMaster.GetElementsByTagName("images")[0] != null)
            {
                foreach (XmlNode xn in xMaster.GetElementsByTagName("images")[0].ChildNodes)
                {
                    XmlElement xImage = (XmlElement)xn;
                    Image image = new Image();
                    image.HEIGHT = Convert.ToInt32(xImage.Attributes["height"].Value);
                    image.WIDTH = Convert.ToInt32(xImage.Attributes["width"].Value);
                    image.TYPE = xImage.Attributes["type"].Value;
                    image.URI = xImage.Attributes["uri"].Value;
                    image.URI150 = xImage.Attributes["uri150"].Value;
                    master.IMAGES.Add(image);
                } //foreach
            }

            master.ARTISTS = Artist.ParseArtists(xMaster);

            if (xMaster.GetElementsByTagName("genres")[0] != null)
            {
                foreach (XmlNode xn in xMaster.GetElementsByTagName("genres")[0].ChildNodes)
                {
                    XmlElement xGenre = (XmlElement)xn;
                    master.GENRES.Add(xGenre.InnerText);
                } //foreach
            }
            if (xMaster.GetElementsByTagName("styles")[0] != null)
            {
                foreach (XmlNode xn in xMaster.GetElementsByTagName("styles")[0].ChildNodes)
                {
                    XmlElement xStyle = (XmlElement)xn;
                    master.STYLES.Add(xStyle.InnerText);
                } //foreach
            }

            if (xMaster.GetElementsByTagName("videos")[0] != null)
            {
                foreach (XmlNode xn in xMaster.GetElementsByTagName("videos")[0].ChildNodes)
                {
                    XmlElement xVideo = (XmlElement)xn;
                    Video video = new Video();
                    video.EMBED = (xVideo.Attributes["embed"].Value == "true");
                    video.DURATION_IN_SEC = Convert.ToInt32(xVideo.Attributes["duration"].Value);
                    video.SRC = xVideo.Attributes["src"].Value;
                    video.TITLE = xVideo["title"].InnerText;
                    video.DESCRIPTION = xVideo["description"].InnerText;

                    master.VIDEOS.Add(video);
                } //foreach
            }

            return master;
        }

        #endregion

        #region Export to file

        /// <summary>
        /// Write data to diffenrent tab seperated files, which kan be imported using local MySQL functions (this is way faster!)
        /// </summary>
        public void StoreInTAB()
        {
            if (swMASTER == null)
            {
                CreateTABSeperatedFiles();
            }
            string notes = "\\N";
            if (!string.IsNullOrEmpty(this.NOTES))
            {
                notes = CDR.DB_Helper.EscapeMySQL(this.NOTES);
            }

            swMASTER.WriteLine($"{this.MASTER_ID}\t{this.MAIN_RELEASE_ID}\t {CDR.DB_Helper.EscapeMySQL(this.TITLE)}\t{this.RELEASED:yyyy-MM-dd}\t{notes}\t{CDR.DB_Helper.EscapeMySQL(this.DATA_QUALITY)}");

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
                swIMAGE.WriteLine($"{this.MASTER_ID}\t{image.HEIGHT}\t{image.WIDTH}\t{CDR.DB_Helper.EscapeMySQL(image.TYPE)}\t{uri}\t{uri150}");
            } //foreach

            foreach (Artist artist in this.ARTISTS)
            {
                swMASTER_ARTIST.WriteLine($"{this.MASTER_ID}\t{artist.ARTIST_ID}\t{CDR.DB_Helper.EscapeMySQL(artist.ANV)}\t{CDR.DB_Helper.EscapeMySQL(artist.JOIN)}\t{CDR.DB_Helper.EscapeMySQL(artist.ROLE)}\t{CDR.DB_Helper.EscapeMySQL(artist.NAME)}");
            } //foreach

            foreach (string genre in this.GENRES)
            {
                swMASTER_GENRE.WriteLine($"{this.MASTER_ID}\t{CDR.DB_Helper.EscapeMySQL(genre)}");
            } //foreach

            foreach (string style in this.STYLES)
            {
                swMASTER_STYLE.WriteLine($"{this.MASTER_ID}\t{CDR.DB_Helper.EscapeMySQL(style)}");
            } //foreach

            foreach (Video video in this.VIDEOS)
            {
                swMASTER_VIDEO.WriteLine($"{this.MASTER_ID}\t{Convert.ToInt16(video.EMBED)}\t{video.DURATION_IN_SEC}\t{CDR.DB_Helper.EscapeMySQL(video.SRC)}\t{CDR.DB_Helper.EscapeMySQL(video.TITLE)}\t{CDR.DB_Helper.EscapeMySQL(video.DESCRIPTION)}");
            } //foreach
        }

        public void CreateTABSeperatedFiles()
        {
            if (swMASTER == null)
            {
                CloseTABSeperatedFiles();

                Encoding utf8WithoutBom = new UTF8Encoding(false);

                swMASTER = new System.IO.StreamWriter(@"MASTER.TAB", false, utf8WithoutBom);
                swIMAGE = new System.IO.StreamWriter(@"MASTER-IMAGE.TAB", false, utf8WithoutBom);
                swMASTER_ARTIST = new System.IO.StreamWriter(@"MASTER-ARTIST.TAB", false, utf8WithoutBom);
                swMASTER_GENRE = new System.IO.StreamWriter(@"MASTER-GENRE.TAB", false, utf8WithoutBom);
                swMASTER_STYLE = new System.IO.StreamWriter(@"MASTER-STYLE.TAB", false, utf8WithoutBom);
                swMASTER_VIDEO = new System.IO.StreamWriter(@"MASTER-VIDEO.TAB", false, utf8WithoutBom);

                GC.WaitForPendingFinalizers();
            }
        }

        public static void CloseTABSeperatedFiles()
        {
            CloseStreamWriter(ref swMASTER);
            CloseStreamWriter(ref swIMAGE);
            CloseStreamWriter(ref swMASTER_ARTIST);
            CloseStreamWriter(ref swMASTER_GENRE);
            CloseStreamWriter(ref swMASTER_STYLE);
            CloseStreamWriter(ref swMASTER_VIDEO);
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
        public static void CleanUpConvertedMastersFiles(string tabFolder)
        {
            DeleteFile(Path.Combine(tabFolder, "MASTER.TAB"));
            DeleteFile(Path.Combine(tabFolder, "MASTER-IMAGE.TAB"));
            DeleteFile(Path.Combine(tabFolder, "MASTER-ARTIST.TAB"));
            DeleteFile(Path.Combine(tabFolder, "MASTER-GENRE.TAB"));
            DeleteFile(Path.Combine(tabFolder, "MASTER-STYLE.TAB"));
            DeleteFile(Path.Combine(tabFolder, "MASTER-VIDEO.TAB"));
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

        public static void ImportMastersData(string tabFolder)
        {
            using (MySql.Data.MySqlClient.MySqlConnection conn = CDR.DB_Helper.NewMySQLConnection())
            {
                CDR.DB_Helper.LOAD_DATA_LOCAL_INFILE(conn, Path.Combine(tabFolder, "MASTER.TAB"), "MASTER", "MASTER_ID, MAIN_RELEASE_ID, TITLE, RELEASED, NOTES, DATA_QUALITY", "");
                CDR.DB_Helper.LOAD_DATA_LOCAL_INFILE(conn, Path.Combine(tabFolder, "MASTER-IMAGE.TAB"), "IMAGE", "MASTER_ID, HEIGHT, WIDTH, `TYPE`, URI, URI150", "IMAGE_ID");
                CDR.DB_Helper.LOAD_DATA_LOCAL_INFILE(conn, Path.Combine(tabFolder, "MASTER-ARTIST.TAB"), "MASTER_ARTIST", "MASTER_ID, ARTIST_ID, ANV, `JOIN`, `NAME`, ROLE", "MASTER_ARTIST_ID");
                CDR.DB_Helper.LOAD_DATA_LOCAL_INFILE(conn, Path.Combine(tabFolder, "MASTER-GENRE.TAB"), "GENRE", "MASTER_ID, GENRE_NAME", "GENRE_ID");
                CDR.DB_Helper.LOAD_DATA_LOCAL_INFILE(conn, Path.Combine(tabFolder, "MASTER-STYLE.TAB"), "STYLE", "MASTER_ID, STYLE_NAME", "STYLE_ID");
                CDR.DB_Helper.LOAD_DATA_LOCAL_INFILE(conn, Path.Combine(tabFolder, "MASTER-VIDEO.TAB"), "VIDEO", "MASTER_ID, EMBED, DURATION_IN_SEC, SRC, TITLE, `DESCRIPTION`", "VIDEO_ID");
            } //using
        }

        #endregion

        /// <summary>
        /// Clear static storage
        /// </summary>
        public static void Clear()
        {
            CloseTABSeperatedFiles();
        }
    }
}
