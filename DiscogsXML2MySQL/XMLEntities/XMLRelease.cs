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
    public class XMLRelease
    {
        // -----------------------------------------------------------------------------------------------------------
        private static int SERIAL_FORMAT_ID = 0;
        private static int SERIAL_TRACK_ID = 0; // must self generate!


        private static StreamWriter swRELEASE = null;
        private static StreamWriter swIMAGE = null;
        private static StreamWriter swRELEASE_ARTIST = null;
        private static StreamWriter swRELEASE_GENRE = null;
        private static StreamWriter swRELEASE_STYLE = null;
        private static StreamWriter swFORMAT = null;
        private static StreamWriter swFORMAT_DESCRIPTION = null;
        private static StreamWriter swRELEASE_LABEL = null;
        private static StreamWriter swTRACK = null;
        private static StreamWriter swTRACK_ARTIST = null;
        private static StreamWriter swIDENTIFIER = null;
        private static StreamWriter swRELEASE_VIDEO = null;
        private static StreamWriter swCOMPANY = null;
        // -----------------------------------------------------------------------------------------------------------

        #region Data definition

        public int RELEASE_ID = -1;
        public int? MASTER_ID = null;
        public bool IS_MAIN_RELEASE = false;
        public string STATUS = "";
        public string TITLE = "";
        public string COUNTRY = "";
        public DateTime RELEASED = DateTime.MinValue;
        public string NOTES = "";
        public string DATA_QUALITY = "";

        public List<Image> IMAGES = new List<Image>();
        public List<Artist> ARTISTS = new List<Artist>();
        public List<string> GENRES = new List<string>();
        public List<string> STYLES = new List<string>();
        public List<Format> FORMATS = new List<Format>();
        public List<ReleaseLabel> RELEASELABELS = new List<ReleaseLabel>();
        public List<Track> TRACKS = new List<Track>();
        public List<Identifier> IDENTIFIERS = new List<Identifier>();
        public List<Video> VIDEOS = new List<Video>();
        public List<Company> COMPANIES = new List<Company>();

        public class Image
        {
            public int HEIGHT = -1;
            public int WIDTH = -1;
            public string TYPE = "";
            public string URI = "";
            public string URI150 = "";
        }

        public class Format
        {
            public int FORMAT_ID = -1; // must self generate!
            public string FORMAT_NAME = "";
            public string FORMAT_TEXT = "";
            public int QUANTITY = 1;
            public List<FormatDescription> FORMAT_DESCRIPTIONS = new List<FormatDescription>();

            public class FormatDescription
            {
                public string DESCRIPTION = "";
                public int DESCRIPTION_ORDER = 1;
            }
        }

        public class ReleaseLabel
        {
            public int LABEL_ID = -1;
            public string NAME = "";
            public string CATNO = "";
        }

        public class Track
        {
            public int TRACK_ID = -1; // must self generate!
            public int? MAIN_TRACK_ID = null; // if not null points to track where this is a subtrack of otherwise null
            public bool HAS_SUBTRACKS = false;
            public bool IS_SUBTRACK = false;
            public int TRACKNUMBER = 1;
            public string TITLE = "";
            public string SUBTRACK_TITLE = "";
            public string POSITION = "";
            public int DURATION_IN_SEC = 0;

            public List<Artist> ARTISTS = new List<Artist>();
        }

        public class Identifier
        {
            public string DESCRIPTION = "";
            public string TYPE = "";
            public string VALUE = "";
        }

        public class Video
        {
            public bool EMBED = false;
            public int DURATION_IN_SEC = 0;
            public string SRC = "";
            public string TITLE = "";
            public string DESCRIPTION = "";
        }

        public class Company
        {
            public int COMPANY_ID = -1;
            public string NAME = "";
            public string CATNO = "";
            public int ENTITY_TYPE = 0;
            public string ENTITY_TYPE_NAME = "";
            public string RESOURCE_URL = "";
        }

        #endregion

        #region Parse XML

        public static XMLRelease ParseXML(XmlElement xRelease)
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


            XMLRelease release = new XMLRelease();

            release.RELEASE_ID = Convert.ToInt32(xRelease.Attributes["id"].Value);
            release.STATUS = xRelease.Attributes["status"].Value;
            release.TITLE = xRelease["title"].InnerText;
            if (xRelease["country"] != null)
            {
                release.COUNTRY = xRelease["country"].InnerText;
            }
            if (xRelease["released"] != null)
            {
                string[] tmp = xRelease["released"].InnerText.Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < tmp.Length; i++)
                {
                    int v;
                    if (int.TryParse(tmp[i], out v))
                    {
                        if (v == 0)
                        {
                            tmp[i] = "01";
                            if (i == 0)
                            {
                                tmp[i] = "0001";
                            }
                        }
                    }
                    else
                    {
                        tmp[i] = "01";
                        if (i == 0)
                        {
                            tmp[i] = "0001";
                        }
                    }
                }

                switch (tmp.Length)
                {
                    case 0:
                        release.RELEASED = DateTime.Parse("0001-01-01");
                        break;
                    case 1:
                        DateTime.TryParse($"{tmp[0]}-01-01", out release.RELEASED);
                        break;
                    case 2:
                        DateTime.TryParse($"{tmp[0]}-{tmp[1]}-01", out release.RELEASED);
                        break;
                    case 3:
                    default:
                        DateTime.TryParse($"{tmp[0]}-{tmp[1]}-{tmp[2]}", out release.RELEASED);
                        break;
                } //switch
            }
            if (xRelease["notes"] != null)
            {
                release.NOTES = xRelease["notes"].InnerText;
            }
            release.MASTER_ID = null;
            release.IS_MAIN_RELEASE = false;
            if (xRelease["master_id"] != null)
            {
                release.MASTER_ID = Convert.ToInt32(xRelease["master_id"].InnerText);
                if (release.MASTER_ID == 0)
                {
                    release.MASTER_ID = null;
                }
                release.IS_MAIN_RELEASE = (xRelease["master_id"].Attributes["is_main_release"].Value == "true");
            }
            if (xRelease["data_quality"] != null)
            {
                release.DATA_QUALITY = xRelease["data_quality"].InnerText;
            }

            if (xRelease.GetElementsByTagName("images")[0] != null)
            {
                foreach (XmlNode xn in xRelease.GetElementsByTagName("images")[0].ChildNodes)
                {
                    XmlElement xImage = (XmlElement)xn;
                    Image image = new Image();
                    image.HEIGHT = Convert.ToInt32(xImage.Attributes["height"].Value);
                    image.WIDTH = Convert.ToInt32(xImage.Attributes["width"].Value);
                    image.TYPE = xImage.Attributes["type"].Value;
                    image.URI = xImage.Attributes["uri"].Value;
                    image.URI150 = xImage.Attributes["uri150"].Value;
                    release.IMAGES.Add(image);
                } //foreach
            }

            release.ARTISTS = Artist.ParseArtists(xRelease);

            if (xRelease.GetElementsByTagName("genres")[0] != null)
            {
                foreach (XmlNode xn in xRelease.GetElementsByTagName("genres")[0].ChildNodes)
                {
                    XmlElement xGenre = (XmlElement)xn;
                    release.GENRES.Add(xGenre.InnerText);
                } //foreach
            }
            if (xRelease.GetElementsByTagName("styles")[0] != null)
            {
                foreach (XmlNode xn in xRelease.GetElementsByTagName("styles")[0].ChildNodes)
                {
                    XmlElement xStyle = (XmlElement)xn;
                    release.STYLES.Add(xStyle.InnerText);
                } //foreach
            }

            if (xRelease.GetElementsByTagName("formats")[0] != null)
            {
                foreach (XmlNode xn1 in xRelease.GetElementsByTagName("formats")[0].ChildNodes)
                {
                    XmlElement xformat = (XmlElement)xn1;
                    Format format = new Format();

                    XMLRelease.SERIAL_FORMAT_ID++;
                    format.FORMAT_ID = XMLRelease.SERIAL_FORMAT_ID;
                    format.FORMAT_NAME = xformat.Attributes["name"].Value;
                    format.FORMAT_TEXT = xformat.Attributes["text"].Value;
                    if (xformat.Attributes["qty"] != null)
                    {
                        int.TryParse(xformat.Attributes["qty"].Value, out format.QUANTITY);
                    }

                    if (xformat.GetElementsByTagName("descriptions")[0] != null)
                    {
                        int cnt = 1;
                        foreach (XmlNode xn2 in xRelease.GetElementsByTagName("descriptions")[0].ChildNodes)
                        {
                            XmlElement xDescription = (XmlElement)xn2;
                            Format.FormatDescription formatDescription = new Format.FormatDescription();
                            formatDescription.DESCRIPTION = xDescription.InnerText;
                            formatDescription.DESCRIPTION_ORDER = cnt;
                            format.FORMAT_DESCRIPTIONS.Add(formatDescription);
                            cnt++;
                        } //foreach
                    }

                    release.FORMATS.Add(format);
                } //foreach
            }

            if (xRelease.GetElementsByTagName("labels")[0] != null)
            {
                foreach (XmlNode xn in xRelease.GetElementsByTagName("labels")[0].ChildNodes)
                {
                    XmlElement xLabel = (XmlElement)xn;
                    ReleaseLabel releaseLabel = new ReleaseLabel();
                    releaseLabel.LABEL_ID = Convert.ToInt32(xLabel.Attributes["id"].Value);
                    releaseLabel.NAME = xLabel.Attributes["name"].Value;
                    releaseLabel.CATNO = xLabel.Attributes["catno"].Value;

                    release.RELEASELABELS.Add(releaseLabel);
                } //foreach
            }

            if (xRelease.GetElementsByTagName("tracklist")[0] != null)
            {
                int cnt = 1;
                foreach (XmlNode xn in xRelease.GetElementsByTagName("tracklist")[0].ChildNodes)
                {
                    XmlElement xTrack = (XmlElement)xn;
                    if (xTrack["title"] != null)
                    {
                        Track track = new Track();
                        track.TRACKNUMBER = cnt;
                        SERIAL_TRACK_ID++;
                        track.TRACK_ID = SERIAL_TRACK_ID;
                        track.POSITION = track.TRACKNUMBER.ToString();
                        if (xTrack["position"] != null)
                        {
                            track.POSITION = xTrack["position"].InnerText;
                        }
                        track.TITLE = xTrack["title"].InnerText;

                        if (xTrack["duration"] != null)
                        {
                            track.DURATION_IN_SEC = ParseDuration(xTrack["duration"].InnerText);
                        }

                        track.ARTISTS = Artist.ParseArtists(xTrack);
                        if (track.ARTISTS.Count == 0)
                        {
                            // Copy Artists from release album
                            track.ARTISTS = Artist.Clone(release.ARTISTS);
                        }

                        // Only when there are no subtracks do we use the "main" track info
                        release.TRACKS.Add(track);
                        cnt++;

                        // Are there sub tracks? (then we ignore the main track!)
                        if (xTrack.GetElementsByTagName("sub_tracks")[0] != null)
                        {
                            track.HAS_SUBTRACKS = true;

                            foreach (XmlNode xn2 in xTrack.GetElementsByTagName("sub_tracks")[0].ChildNodes)
                            {
                                XmlElement xSubTrack = (XmlElement)xn2;
                                Track subTrack = new Track();
                                SERIAL_TRACK_ID++;
                                subTrack.TRACK_ID = SERIAL_TRACK_ID;
                                subTrack.MAIN_TRACK_ID = track.TRACK_ID;
                                subTrack.TRACKNUMBER = cnt;

                                subTrack.IS_SUBTRACK = true;
                                subTrack.POSITION = subTrack.TRACKNUMBER.ToString();
                                if (xSubTrack["position"] != null)
                                {
                                    subTrack.POSITION = xSubTrack["position"].InnerText;
                                }
                                subTrack.TITLE = track.TITLE + " / " + xSubTrack["title"].InnerText;
                                subTrack.SUBTRACK_TITLE = xSubTrack["title"].InnerText;
                                if (xSubTrack["duration"] != null)
                                {
                                    subTrack.DURATION_IN_SEC = ParseDuration(xSubTrack["duration"].InnerText);
                                }

                                // I don't believe this exists for sub_tracks but it doesn't hurt
                                subTrack.ARTISTS = Artist.ParseArtists(xSubTrack);
                                if (subTrack.ARTISTS.Count == 0)
                                {
                                    // Copy Artists from "Main" track album
                                    subTrack.ARTISTS = Artist.Clone(track.ARTISTS);
                                }

                                release.TRACKS.Add(subTrack);
                                cnt++;
                            } //foreach subtrack
                        }
                    }
                } //foreach
            }

            if (xRelease.GetElementsByTagName("identifiers")[0] != null)
            {
                foreach (XmlNode xn in xRelease.GetElementsByTagName("identifiers")[0].ChildNodes)
                {
                    XmlElement xIdentifier = (XmlElement)xn;
                    Identifier identifier = new Identifier();
                    if (xIdentifier.Attributes["value"] != null)
                    {
                        if (xIdentifier.Attributes["description"] != null)
                        {
                            identifier.DESCRIPTION = xIdentifier.Attributes["description"].Value;
                        }
                        if (xIdentifier.Attributes["type"] != null)
                        {
                            identifier.TYPE = xIdentifier.Attributes["type"].Value;
                        }
                        identifier.VALUE = xIdentifier.Attributes["value"].Value;

                        release.IDENTIFIERS.Add(identifier);
                    }
                } //foreach
            }

            if (xRelease.GetElementsByTagName("videos")[0] != null)
            {
                foreach (XmlNode xn in xRelease.GetElementsByTagName("videos")[0].ChildNodes)
                {
                    XmlElement xVideo = (XmlElement)xn;
                    Video video = new Video();
                    video.EMBED = (xVideo.Attributes["embed"].Value == "true");
                    video.DURATION_IN_SEC = Convert.ToInt32(xVideo.Attributes["duration"].Value);
                    video.SRC = xVideo.Attributes["src"].Value;
                    video.TITLE = xVideo["title"].InnerText;
                    video.DESCRIPTION = xVideo["description"].InnerText;

                    release.VIDEOS.Add(video);
                } //foreach
            }

            if (xRelease.GetElementsByTagName("companies")[0] != null)
            {
                foreach (XmlNode xn in xRelease.GetElementsByTagName("companies")[0].ChildNodes)
                {
                    XmlElement xCompany = (XmlElement)xn;
                    Company company = new Company();
                    company.COMPANY_ID = Convert.ToInt32(xCompany["id"].InnerText);
                    company.NAME = xCompany["name"].InnerText;
                    company.CATNO = xCompany["catno"].InnerText;
                    company.ENTITY_TYPE = Convert.ToInt32(xCompany["entity_type"].InnerText);
                    company.ENTITY_TYPE_NAME = xCompany["entity_type_name"].InnerText;
                    company.RESOURCE_URL = xCompany["resource_url"].InnerText;

                    release.COMPANIES.Add(company);
                } //foreach
            }

            return release;
        }

        /// <summary>
        /// returns duration in seconds
        /// </summary>
        private static int ParseDuration(string d)
        {
            if (d.Contains(":"))
            {
                string[] duration = d.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                int min = 0;
                if (duration.Length >= 1)
                {
                    int.TryParse(duration[0], out min);
                }
                int sec = 0;
                if (duration.Length >= 2)
                {
                    int.TryParse(duration[1], out sec);
                }
                return min * 60 + sec;
            }

            return 0;
        }

        #endregion

        #region Export to file

        /// <summary>
        /// Write data to diffenrent tab seperated files, which kan be imported using local MySQL functions (this is way faster!)
        /// </summary>
        public void StoreInTAB()
        {
            if (swRELEASE == null)
            {
                CreateTABSeperatedFiles();
            }

            string MASTER_ID = "\\N";
            if (this.MASTER_ID != null)
            {
                MASTER_ID = this.MASTER_ID.ToString();
            }
            string IS_MAIN_RELEASE = "0";
            if (this.IS_MAIN_RELEASE)
            {
                IS_MAIN_RELEASE = "1";
            }
            string notes = "\\N";
            if (!string.IsNullOrEmpty(this.NOTES))
            {
                notes = CDR.DB_Helper.EscapeMySQL(this.NOTES);
            }
            swRELEASE.WriteLine($"{this.RELEASE_ID}\t{MASTER_ID}\t{CDR.DB_Helper.EscapeMySQL(this.STATUS)}\t{CDR.DB_Helper.EscapeMySQL(this.TITLE)}\t" +
                                $"{CDR.DB_Helper.EscapeMySQL(this.COUNTRY)}\t{this.RELEASED:yyyy-MM-dd}\t{notes}\t{CDR.DB_Helper.EscapeMySQL(this.DATA_QUALITY)}\t{IS_MAIN_RELEASE}");

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
                swIMAGE.WriteLine($"{this.RELEASE_ID}\t{image.HEIGHT}\t{image.WIDTH}\t{CDR.DB_Helper.EscapeMySQL(image.TYPE)}\t{uri}\t{uri150}");
            } //foreach

            foreach (Artist artist in this.ARTISTS)
            {
                string EXTRA_ARTIST = "0";
                if (artist.EXTRA_ARTIST)
                {
                    EXTRA_ARTIST = "1";
                }
                swRELEASE_ARTIST.WriteLine($"{this.RELEASE_ID}\t{artist.ARTIST_ID}\t{CDR.DB_Helper.EscapeMySQL(artist.ANV)}\t{CDR.DB_Helper.EscapeMySQL(artist.JOIN)}\t{CDR.DB_Helper.EscapeMySQL(artist.ROLE)}\t{CDR.DB_Helper.EscapeMySQL(artist.NAME)}\t{EXTRA_ARTIST}");
            } //foreach

            foreach (Track track in this.TRACKS)
            {
                string MAIN_TRACK_ID = "\\N";
                if (track.MAIN_TRACK_ID != null)
                {
                    MAIN_TRACK_ID = track.MAIN_TRACK_ID.ToString();
                }
                int HAS_SUBTRACKS = Convert.ToInt32(track.HAS_SUBTRACKS);
                int IS_SUBTRACK = Convert.ToInt32(track.IS_SUBTRACK);
                swTRACK.WriteLine($"{track.TRACK_ID}\t{this.RELEASE_ID}\t{MAIN_TRACK_ID}\t{HAS_SUBTRACKS}\t{IS_SUBTRACK}\t{track.TRACKNUMBER}\t{CDR.DB_Helper.EscapeMySQL(track.TITLE)}\t{CDR.DB_Helper.EscapeMySQL(track.SUBTRACK_TITLE)}\t{CDR.DB_Helper.EscapeMySQL(track.POSITION)}\t{track.DURATION_IN_SEC}");

                foreach (Artist artist in track.ARTISTS)
                {
                    string EXTRA_ARTIST = "0";
                    if (artist.EXTRA_ARTIST)
                    {
                        EXTRA_ARTIST = "1";
                    }
                    swTRACK_ARTIST.WriteLine($"{track.TRACK_ID}\t{artist.ARTIST_ID}\t{CDR.DB_Helper.EscapeMySQL(artist.ANV)}\t{CDR.DB_Helper.EscapeMySQL(artist.JOIN)}\t{CDR.DB_Helper.EscapeMySQL(artist.ROLE)}\t{CDR.DB_Helper.EscapeMySQL(artist.NAME)}\t{EXTRA_ARTIST}");
                } //foreach
            } //foreach

            foreach (string genre in this.GENRES)
            {
                swRELEASE_GENRE.WriteLine($"{this.RELEASE_ID}\t{CDR.DB_Helper.EscapeMySQL(genre)}");
            } //foreach

            foreach (string style in this.STYLES)
            {
                swRELEASE_STYLE.WriteLine($"{this.RELEASE_ID}\t{CDR.DB_Helper.EscapeMySQL(style)}");
            } //foreach

            foreach (Format format in this.FORMATS)
            {
                swFORMAT.WriteLine($"{format.FORMAT_ID}\t{this.RELEASE_ID}\t{CDR.DB_Helper.EscapeMySQL(format.FORMAT_NAME)}\t{CDR.DB_Helper.EscapeMySQL(format.FORMAT_TEXT)}\t{format.QUANTITY}");

                foreach (Format.FormatDescription formatDescription in format.FORMAT_DESCRIPTIONS)
                {
                    swFORMAT_DESCRIPTION.WriteLine($"{format.FORMAT_ID}\t{CDR.DB_Helper.EscapeMySQL(formatDescription.DESCRIPTION)}\t{CDR.DB_Helper.EscapeMySQL(formatDescription.DESCRIPTION_ORDER)}");
                } //foreach
            } //foreach

            foreach (ReleaseLabel releaseLabel in this.RELEASELABELS)
            {
                swRELEASE_LABEL.WriteLine($"{this.RELEASE_ID}\t{releaseLabel.LABEL_ID}\t{CDR.DB_Helper.EscapeMySQL(releaseLabel.CATNO)}");
            } //foreach

            foreach (Identifier identifier in this.IDENTIFIERS)
            {
                 swIDENTIFIER.WriteLine($"{this.RELEASE_ID}\t{CDR.DB_Helper.EscapeMySQL(identifier.DESCRIPTION)}\t{CDR.DB_Helper.EscapeMySQL(identifier.TYPE)}\t{CDR.DB_Helper.EscapeMySQL(identifier.VALUE)}");
            } //foreach

            foreach (Video video in this.VIDEOS)
            {
                swRELEASE_VIDEO.WriteLine($"{this.RELEASE_ID}\t{Convert.ToInt16(video.EMBED)}\t{video.DURATION_IN_SEC}\t{CDR.DB_Helper.EscapeMySQL(video.SRC)}\t{CDR.DB_Helper.EscapeMySQL(video.TITLE)}\t{CDR.DB_Helper.EscapeMySQL(video.DESCRIPTION)}");
            } //foreach

            foreach (Company company in this.COMPANIES)
            {
                swCOMPANY.WriteLine($"{company.COMPANY_ID}\t{this.RELEASE_ID}\t{CDR.DB_Helper.EscapeMySQL(company.NAME)}\t{CDR.DB_Helper.EscapeMySQL(company.CATNO)}\t{company.ENTITY_TYPE}\t{CDR.DB_Helper.EscapeMySQL(company.ENTITY_TYPE_NAME)}\t{CDR.DB_Helper.EscapeMySQL(company.RESOURCE_URL)}");
            } //foreach
        }

        public void CreateTABSeperatedFiles()
        {
            if (swRELEASE == null)
            {
                CloseTABSeperatedFiles();

                Encoding utf8WithoutBom = new UTF8Encoding(false);

                swRELEASE = new System.IO.StreamWriter(@"RELEASE.TAB", false, utf8WithoutBom);
                swIMAGE = new System.IO.StreamWriter(@"RELEASE-IMAGE.TAB", false, utf8WithoutBom);
                swRELEASE_ARTIST = new System.IO.StreamWriter(@"RELEASE-ARTIST.TAB", false, utf8WithoutBom);
                swRELEASE_GENRE = new System.IO.StreamWriter(@"RELEASE-GENRE.TAB", false, utf8WithoutBom);
                swRELEASE_STYLE = new System.IO.StreamWriter(@"RELEASE-STYLE.TAB", false, utf8WithoutBom);
                swTRACK_ARTIST = new System.IO.StreamWriter(@"TRACK-ARTIST.TAB", false, utf8WithoutBom);
                swFORMAT = new System.IO.StreamWriter(@"FORMAT.TAB", false, utf8WithoutBom);
                swFORMAT_DESCRIPTION = new System.IO.StreamWriter(@"FORMAT-DESCRIPTION.TAB", false, utf8WithoutBom);
                swRELEASE_LABEL = new System.IO.StreamWriter(@"RELEASE-LABEL.TAB", false, utf8WithoutBom);
                swTRACK = new System.IO.StreamWriter(@"TRACK.TAB", false, utf8WithoutBom);
                swIDENTIFIER = new System.IO.StreamWriter(@"IDENTIFIER.TAB", false, utf8WithoutBom);
                swRELEASE_VIDEO = new System.IO.StreamWriter(@"RELEASE-VIDEO.TAB", false, utf8WithoutBom);
                swCOMPANY = new System.IO.StreamWriter(@"COMPANY.TAB", false, utf8WithoutBom);

                GC.WaitForPendingFinalizers();
            }
        }

        public static void CloseTABSeperatedFiles()
        {
            CloseStreamWriter(ref swRELEASE);
            CloseStreamWriter(ref swIMAGE);
            CloseStreamWriter(ref swRELEASE_ARTIST);
            CloseStreamWriter(ref swRELEASE_GENRE);
            CloseStreamWriter(ref swRELEASE_STYLE);
            CloseStreamWriter(ref swTRACK_ARTIST);
            CloseStreamWriter(ref swFORMAT);
            CloseStreamWriter(ref swFORMAT_DESCRIPTION);
            CloseStreamWriter(ref swRELEASE_LABEL);
            CloseStreamWriter(ref swTRACK);
            CloseStreamWriter(ref swIDENTIFIER);
            CloseStreamWriter(ref swRELEASE_VIDEO);
            CloseStreamWriter(ref swCOMPANY);
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
        public static void CleanUpConvertedReleasesFiles(string tabFolder)
        {
            DeleteFile(Path.Combine(tabFolder, "RELEASE.TAB"));
            DeleteFile(Path.Combine(tabFolder, "RELEASE-IMAGE.TAB"));
            DeleteFile(Path.Combine(tabFolder, "RELEASE-ARTIST.TAB"));
            DeleteFile(Path.Combine(tabFolder, "RELEASE-GENRE.TAB"));
            DeleteFile(Path.Combine(tabFolder, "RELEASE-STYLE.TAB"));
            DeleteFile(Path.Combine(tabFolder, "FORMAT.TAB"));
            DeleteFile(Path.Combine(tabFolder, "FORMAT-DESCRIPTION.TAB"));
            DeleteFile(Path.Combine(tabFolder, "RELEASE-LABEL.TAB"));
            DeleteFile(Path.Combine(tabFolder, "TRACK.TAB"));
            DeleteFile(Path.Combine(tabFolder, "TRACK-ARTIST.TAB"));
            DeleteFile(Path.Combine(tabFolder, "IDENTIFIER.TAB"));
            DeleteFile(Path.Combine(tabFolder, "RELEASE-VIDEO.TAB"));
            DeleteFile(Path.Combine(tabFolder, "COMPANY.TAB"));
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

        public static void ImportReleasesData(string tabFolder)
        {
            using (MySql.Data.MySqlClient.MySqlConnection conn = CDR.DB_Helper.NewMySQLConnection())
            {
                CDR.DB_Helper.LOAD_DATA_LOCAL_INFILE(conn, Path.Combine(tabFolder, "RELEASE.TAB"), "RELEASE", "RELEASE_ID, MASTER_ID, STATUS, TITLE, COUNTRY, RELEASED, NOTES, DATA_QUALITY, IS_MAIN_RELEASE", "");
                CDR.DB_Helper.LOAD_DATA_LOCAL_INFILE(conn, Path.Combine(tabFolder, "RELEASE-IMAGE.TAB"), "IMAGE", "RELEASE_ID, HEIGHT, WIDTH, `TYPE`, URI, URI150", "IMAGE_ID");
                CDR.DB_Helper.LOAD_DATA_LOCAL_INFILE(conn, Path.Combine(tabFolder, "RELEASE-ARTIST.TAB"), "RELEASE_ARTIST", "RELEASE_ID, ARTIST_ID, ANV, `JOIN`, ROLE, `NAME`, EXTRA_ARTIST", "RELEASE_ARTIST_ID");
                CDR.DB_Helper.LOAD_DATA_LOCAL_INFILE(conn, Path.Combine(tabFolder, "RELEASE-GENRE.TAB"), "GENRE", "RELEASE_ID, GENRE_NAME", "GENRE_ID");
                CDR.DB_Helper.LOAD_DATA_LOCAL_INFILE(conn, Path.Combine(tabFolder, "RELEASE-STYLE.TAB"), "STYLE", "RELEASE_ID, STYLE_NAME", "STYLE_ID");
                CDR.DB_Helper.LOAD_DATA_LOCAL_INFILE(conn, Path.Combine(tabFolder, "FORMAT.TAB"), "FORMAT", "FORMAT_ID, RELEASE_ID, FORMAT_NAME, FORMAT_TEXT, QUANTITY", "");
                CDR.DB_Helper.LOAD_DATA_LOCAL_INFILE(conn, Path.Combine(tabFolder, "FORMAT-DESCRIPTION.TAB"), "FORMAT_DESCRIPTION", "FORMAT_ID, `DESCRIPTION`, DESCRIPTION_ORDER", "FORMAT_DESCRIPTION_ID");
                CDR.DB_Helper.LOAD_DATA_LOCAL_INFILE(conn, Path.Combine(tabFolder, "RELEASE-LABEL.TAB"), "RELEASE_LABEL", "RELEASE_ID, LABEL_ID, CATNO", "RELEASE_LABEL_ID");
                CDR.DB_Helper.LOAD_DATA_LOCAL_INFILE(conn, Path.Combine(tabFolder, "TRACK.TAB"), "TRACK", "TRACK_ID, RELEASE_ID, MAIN_TRACK_ID, HAS_SUBTRACKS, IS_SUBTRACK, TRACKNUMBER, TITLE, SUBTRACK_TITLE, POSITION, DURATION_IN_SEC", "");
                CDR.DB_Helper.LOAD_DATA_LOCAL_INFILE(conn, Path.Combine(tabFolder, "TRACK-ARTIST.TAB"), "TRACK_ARTIST", "TRACK_ID, ARTIST_ID, ANV, `JOIN`, ROLE, `NAME`, EXTRA_ARTIST", "TRACK_ARTIST_ID");
                CDR.DB_Helper.LOAD_DATA_LOCAL_INFILE(conn, Path.Combine(tabFolder, "IDENTIFIER.TAB"), "IDENTIFIER", "RELEASE_ID, `DESCRIPTION`, TYPE, VALUE", "IDENTIFIER_ID");
                CDR.DB_Helper.LOAD_DATA_LOCAL_INFILE(conn, Path.Combine(tabFolder, "RELEASE-VIDEO.TAB"), "VIDEO", "RELEASE_ID, EMBED, DURATION_IN_SEC, SRC, TITLE, `DESCRIPTION`", "VIDEO_ID");
                CDR.DB_Helper.LOAD_DATA_LOCAL_INFILE(conn, Path.Combine(tabFolder, "COMPANY.TAB"), "COMPANY", "COMPANY_ID, RELEASE_ID, `NAME`, CATNO, ENTITY_TYPE, ENTITY_TYPE_NAME, RESOURCE_URL", "");
            } //using
        }

        #endregion

        public static void Clear()
        {
            SERIAL_FORMAT_ID = 0;
            SERIAL_TRACK_ID = 0;
            CloseTABSeperatedFiles();
        }
    }
}
