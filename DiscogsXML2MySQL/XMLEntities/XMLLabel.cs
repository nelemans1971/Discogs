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
    public class XMLLabel
    {
        // -----------------------------------------------------------------------------------------------------------
        private static StreamWriter swLABEL = null;
        private static StreamWriter swIMAGE = null;
        private static StreamWriter swURL = null;
        private static StreamWriter swSUBLABEL = null;
        // -----------------------------------------------------------------------------------------------------------

        public int LABEL_ID = -1;
        public string NAME = "";
        public string CONTACTINFO = "";
        public string PROFILE = "";
        public string DATA_QUALITY = "";
        public SubLabel PARENTLABEL = null;

        public List<Image> IMAGES = new List<Image>();
        public List<string> URLS = new List<string>();
        public List<SubLabel> SUBLABELS = new List<SubLabel>();

        public class Image
        {
            public int HEIGHT = -1;
            public int WIDTH = -1;
            public string TYPE = "";
            public string URI = "";
            public string URI150 = "";
        }

        public class SubLabel
        {
            public int LABEL_ID = -1;
            public string NAME = "";
        }

        #region Parse XML

        public static XMLLabel ParseXML(XmlElement xLabel)
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

            XMLLabel label = new XMLLabel();

            label.LABEL_ID = Convert.ToInt32(xLabel.GetElementsByTagName("id")[0].InnerText);
            label.NAME = xLabel["name"].InnerText;
            if (xLabel["contactinfo"] != null)
            {
                label.CONTACTINFO = xLabel["contactinfo"].InnerText;
            }
            if (xLabel["profile"] != null)
            {
                label.PROFILE = xLabel["profile"].InnerText;
            }
            if (xLabel["data_quality"] != null)
            {
                label.DATA_QUALITY = xLabel["data_quality"].InnerText;
            }
            if (xLabel["parentLabel"] != null)
            {
                label.PARENTLABEL = new SubLabel();
                label.PARENTLABEL.LABEL_ID = Convert.ToInt32(xLabel["parentLabel"].Attributes["id"].Value);
                label.PARENTLABEL.NAME = xLabel["parentLabel"].InnerText;
            }

            if (xLabel.GetElementsByTagName("images")[0] != null)
            {
                foreach (XmlNode xn in xLabel.GetElementsByTagName("images")[0].ChildNodes)
                {
                    XmlElement xImage = (XmlElement)xn;
                    Image image = new Image();
                    image.HEIGHT = Convert.ToInt32(xImage.Attributes["height"].Value);
                    image.WIDTH = Convert.ToInt32(xImage.Attributes["width"].Value);
                    image.TYPE = xImage.Attributes["type"].Value;
                    image.URI = xImage.Attributes["uri"].Value;
                    image.URI150 = xImage.Attributes["uri150"].Value;
                    label.IMAGES.Add(image);
                } //foreach
            }

            if (xLabel.GetElementsByTagName("urls")[0] != null)
            {
                foreach (XmlNode xn in xLabel.GetElementsByTagName("urls")[0].ChildNodes)
                {
                    XmlElement xUrl = (XmlElement)xn;
                    if (!string.IsNullOrEmpty(xUrl.InnerText))
                    {
                        label.URLS.Add(xUrl.InnerText.Trim());
                    }
                } //foreach
            }

            if (xLabel.GetElementsByTagName("sublabels")[0] != null)
            {
                foreach (XmlNode xn in xLabel.GetElementsByTagName("sublabels")[0].ChildNodes)
                {
                    XmlElement xSublabel = (XmlElement)xn;
                    SubLabel subLabel = new SubLabel();
                    if (!string.IsNullOrEmpty(xSublabel.InnerText))
                    {
                        subLabel.LABEL_ID = Convert.ToInt32(xSublabel.Attributes["id"].Value);
                        subLabel.NAME = xSublabel.InnerText;
                        label.SUBLABELS.Add(subLabel);
                    }
                } //foreach
            }


            return label;
        }

        #endregion

        /// <summary>
        /// Write data to diffenrent tab seperated files, which kan be imported using local MySQL functions (this is way faster!)
        /// </summary>
        public void StoreInTAB()
        {
            if (swLABEL == null)
            {
                CreateTABSeperatedFiles();
            }

            string parentlabel = "\\N";
            if (this.PARENTLABEL != null)
            {
                parentlabel = this.PARENTLABEL.LABEL_ID.ToString();
            }

            swLABEL.WriteLine($"{this.LABEL_ID}\t{CDR.DB_Helper.EscapeMySQL(this.NAME)}\t{CDR.DB_Helper.EscapeMySQL(this.CONTACTINFO)}\t{CDR.DB_Helper.EscapeMySQL(this.PROFILE)}\t{CDR.DB_Helper.EscapeMySQL(this.DATA_QUALITY)}\t{parentlabel}");

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
                swIMAGE.WriteLine($"{this.LABEL_ID}\t{image.HEIGHT}\t{image.WIDTH}\t{CDR.DB_Helper.EscapeMySQL(image.TYPE)}\t{uri}\t{uri150}");
            } //foreach

            foreach (string url in this.URLS)
            {
                swURL.WriteLine($"{this.LABEL_ID}\t{CDR.DB_Helper.EscapeMySQL(url)}");
            } //foreach

            foreach (SubLabel subLabel in this.SUBLABELS)
            {
                swSUBLABEL.WriteLine($"{this.LABEL_ID}\t{subLabel.LABEL_ID}");
            } //foreach
        }

        public static void CreateTABSeperatedFiles()
        {
            if (swLABEL == null)
            {
                CloseTABSeperatedFiles();

                Encoding utf8WithoutBom = new UTF8Encoding(false);

                swLABEL = new System.IO.StreamWriter(@"LABEL.TAB", false, utf8WithoutBom);
                swIMAGE = new System.IO.StreamWriter(@"LABEL-IMAGE.TAB", false, utf8WithoutBom);
                swURL = new System.IO.StreamWriter(@"LABEL-URL.TAB", false, utf8WithoutBom);
                swSUBLABEL = new System.IO.StreamWriter(@"SUBLABEL.TAB", false, utf8WithoutBom);
                GC.WaitForPendingFinalizers();
            }
        }

        public static void CloseTABSeperatedFiles()
        {
            CloseStreamWriter(ref swLABEL);
            CloseStreamWriter(ref swIMAGE);
            CloseStreamWriter(ref swURL);
            CloseStreamWriter(ref swSUBLABEL);
        }

        private static void CloseStreamWriter(ref StreamWriter sw)
        {
            if (sw != null)
            {
                sw.Close();
                sw = null;
            }
        }

        public static void ImportLabelsData(string tabFolder)
        {
            using (MySql.Data.MySqlClient.MySqlConnection conn = CDR.DB_Helper.NewMySQLConnection())
            {
                CDR.DB_Helper.LOAD_DATA_LOCAL_INFILE(conn, Path.Combine(tabFolder, "LABEL.TAB"), "LABEL", "LABEL_ID, `NAME`, CONTACTINFO, `PROFILE`, DATA_QUALITY, PARENT_LABEL_ID", "");
                CDR.DB_Helper.LOAD_DATA_LOCAL_INFILE(conn, Path.Combine(tabFolder, "LABEL-IMAGE.TAB"), "IMAGE", "LABEL_ID, HEIGHT, WIDTH, `TYPE`, URI, URI150", "IMAGE_ID");
                CDR.DB_Helper.LOAD_DATA_LOCAL_INFILE(conn, Path.Combine(tabFolder, "LABEL-URL.TAB"), "URL", "LABEL_ID, URL", "URL_ID");
                CDR.DB_Helper.LOAD_DATA_LOCAL_INFILE(conn, Path.Combine(tabFolder, "SUBLABEL.TAB"), "SUBLABEL", "MAIN_LABEL_ID, CHILD_LABEL_ID", "SUBLABEL_ID");
            } //using
        }

        /// <summary>
        /// Remove tab files, to save space
        /// </summary>
        public static void CleanUpConvertedLabelsFiles(string tabFolder)
        {
            DeleteFile(Path.Combine(tabFolder, "LABEL.TAB"));
            DeleteFile(Path.Combine(tabFolder, "LABEL-IMAGE.TAB"));
            DeleteFile(Path.Combine(tabFolder, "LABEL-URL.TAB"));
            DeleteFile(Path.Combine(tabFolder, "SUBLABEL.TAB"));
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

        /// <summary>
        /// Clear static storage
        /// </summary>
        public static void Clear()
        {
            CloseTABSeperatedFiles();
        }

    }
}
