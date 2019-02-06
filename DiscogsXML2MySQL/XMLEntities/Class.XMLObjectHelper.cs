using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace DiscogsXML2MySQL
{
    /// <summary>
    /// Helper class voor de XML objecten parsen
    /// </summary>
    public static class XMLObjectHelper
    {
        /// <summary>
        /// Verwijder @ en / uit een string. De @ wordt vervangen door een spaties
        /// omdat de codering voor medewerker die vast plakt!
        /// </summary>
        /// <param name="pvalue"></param>
        /// <returns></returns>
        public static string FilterPicaSymbolsMedewerker(string pvalue)
        {
            string result = "";

            pvalue = pvalue.Replace("@@", "\xFDE8");
            pvalue = pvalue.Replace("//", "\xFDE7");
            pvalue = pvalue.Replace("##", "\xFDE6");
            // Hardgecodere aanpassing. (Makkelijker)
            pvalue = pvalue.Replace("#DJ#", "DJ");
            pvalue = pvalue.Replace("#MC#", "MC");

            int cnt = 1;
            foreach (char ch in pvalue)
            {
                switch (ch)
                {
                    case '@':
                        if (cnt > 1)
                        {
                            result += " ";
                        }
                        break;
                    case '/':
                        result += " ";
                        break;

                    default:
                        result += ch;
                        break;
                } //switch

                cnt++;
            }

            result = result.Replace('\xFDE8', '@');
            result = result.Replace('\xFDE7', '/');
            result = result.Replace('\xFDE6', '#');

            return result;
        }


        /// <summary>
        /// Verwijder @ uit een string. Er wordt verder niks gedaan
        /// </summary>
        /// <param name="pvalue"></param>
        /// <returns></returns>
        public static string FilterPicaSymbolsTitel(string pvalue)
        {
            string result = "";
            // encode @@ tijdelijk
            pvalue = pvalue.Replace("@@", "\xFDE8");

            foreach (char ch in pvalue)
            {
                if (ch != '@')
                {
                    result += ch;
                }
            }

            // Zet @@ om naar @
            result = result.Replace('\xFDE8', '@');

            return result;
        }

        /// <summary>
        /// Code 0x0B mag niet doorgegeven worden aan een XELement want dan gaat het fout, beetje vreemd
        /// maar het is zo, dus we filteren alle characters <= 26 eruit.
        /// </summary>
        /// <param name="s"></param>
        public static string FilterIllegalCharsForXML(string s, bool filterCRLF = true)
        {
            StringBuilder sb = new StringBuilder(s.Length);
            foreach (char ch in s)
            {
                if (!filterCRLF && ch == '\r')
                {
                    sb.Append(ch);
                }
                else if (!filterCRLF && ch == '\n')
                {
                    sb.Append(ch);
                }
                else if (Convert.ToInt32(ch) > 26)
                {
                    sb.Append(ch);
                }
            } //foreach


            return sb.ToString();
        }

        /// <summary>
        ///  try to detect if a text is a wikipeid a article.
        ///  return true if so else false
        /// </summary>
        public static bool IsWikipediaText(string text)
        {
            return (text.Contains("'''") ||
                text.Contains("[[Bestand:") ||
                text.Contains("[[File:") ||
                text.Contains("{{Infobox") ||
                text.Contains("{{"));
        }

        public delegate bool ParseXMLPiece(XmlDocument xmlDoc);

        /// <summary>
        /// Deze functie help bij het parsen van grote XML files door ze in kleine brokken te hakken op het 2de
        /// tag niveau (de eerste tag na de root:
        /// bv:
        /// <?xml version="1.0" encoding="utf-8"?>
        /// <root>
        ///   <album>
        ///     blablabla blablabla blablabla blablabla
        ///   </album>
        ///   <album>
        ///     blablabla blablabla blablabla blablabla
        ///   </album>
        /// </root>
        ///
        /// Er worden bij dit voorbeeld 2 calls naar de delgate gedaan. Deze delegate bevat een XmlDocument waarbij de
        /// root naam wordt gekopieerd uit de hoofdfile xml en er maar 1 tweede niveau (bij dit voorbeeld <album>)
        /// aanwezig is.
        /// Door aan de delgate false terug te geven wordt de scan voortijdig gestopt.
        ///
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="dParseXMLPiece"></param>
        public static void ParseXMLFile(string filename, ParseXMLPiece dParseXMLPiece)
        {
            // Dan valt er weinig te doen
            if (dParseXMLPiece == null)
            {
                return;
            }

            using (XmlTextReader reader = new XmlTextReader(new FileStream(filename, FileMode.Open, FileAccess.Read)))
            {
                reader.WhitespaceHandling = WhitespaceHandling.None;

                // Ga naar positie in xml bestand waar de eerste xml tag begint
                reader.MoveToContent();
                string rootStr = reader.Name;

                // We hebben de root tag. Nu naar het eerst volgende element. Deze komt 0, 1 of meer keer voor en gaan
                // we in die stukken opdelen en doorgegeven voor parsing.
                reader.Read();

                // Loop door alle 2de niveau (dus na de root tag) tags totdat we aan het eind van de xml file zijn
                while (!reader.EOF)
                {
                    string xmlStr = string.Empty;

                    using (XmlTextWriter writer = new XmlTextWriter(new MemoryStream(), Encoding.UTF8)) //encoding utf-8
                    {
                        writer.Formatting = Formatting.Indented;
                        writer.WriteStartDocument();
                        writer.WriteStartElement(rootStr);

                        // Haal "plukje" uit grote xml bestand
                        writer.WriteRaw(reader.ReadOuterXml());

                        writer.WriteEndElement();
                        writer.WriteEndDocument();
                        writer.Flush(); // belangrijk anders wordt er niks naar de stream geschreven (bij een te kleine xml)

                        // Ken gebruikte stream aan eigen var toe zodat we resultaat kunnen gebruiken.
                        MemoryStream stream = (MemoryStream)writer.BaseStream;
                        // Jump to the start position of the stream
                        stream.Seek(0, SeekOrigin.Begin);

                        using (StreamReader sr = new StreamReader(stream))
                        {
                            xmlStr = sr.ReadToEnd();
                        } //using
                    } //using closes XmlTextWriter


                    // Nu de tag <album> verwerken.
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(xmlStr);
                    if (!dParseXMLPiece(xmlDoc))
                    {
                        // verzoek tot stoppen met parse van xml file.
                        break;
                    }
                } //while
            } //using XmlTextReader
        }


        public static void RetrieveRootInfo(string xmlFile, out string rootBegin, out string rootEnd)
        {
            rootBegin = string.Empty;
            rootEnd = string.Empty;

            using (XmlTextReader reader = new XmlTextReader(new FileStream(xmlFile, FileMode.Open, FileAccess.Read)))
            {
                reader.WhitespaceHandling = WhitespaceHandling.None;

                // Ga naar positie in xml bestand waar de eerste xml tag begint
                reader.MoveToContent();
                rootBegin = "<" + reader.Name;
                rootEnd = "</" + reader.Name + ">";

                if (reader.HasAttributes)
                {
                    for (int i = 0; i < reader.AttributeCount; i++)
                    {
                        reader.MoveToAttribute(i);
                        rootBegin += string.Format(" {0}=\"{1}\"", reader.Name, reader.Value);
                    }
                }

                rootBegin += ">";
            } //using
        }

        /// <summary>
        /// doorzoekt xml file naar eerste "rootbegin" text en vervangt dan "count"
        /// door nieuwe aantal. De tekst wordt "hard" overschreven. We gaan ervan uit dat we
        /// 00000000000 dus 11 cijfers kunnen wegschrijven!
        /// </summary>
        /// <param name="xmlFile"></param>
        /// <param name="rootBegin"></param>
        /// <param name="count"></param>
        public static void OverwriteCOUNTAttribute(string xmlFile, string rootBegin, int count)
        {
            long countPos = -1;
            using (TextPositionFileReader textReader = new TextPositionFileReader(xmlFile))
            {
                while (!textReader.EOF)
                {
                    long linePosition;
                    string line = textReader.ReadLine(out linePosition);
                    if (line.Length > 0)
                    {
                        int index = line.IndexOf(rootBegin);
                        if (index >= 0)
                        {
                            // gevonden root
                            // Nu count opzoeken
                            index = line.IndexOf("count=\"");
                            if (index >= 0)
                            {
                                countPos = linePosition + index + 7;
                                // We zijn klaar
                                break;
                            }
                        }
                    }
                } //while
            } //using

            if (countPos >= 0)
            {
                using (FileStream fs = new FileStream(xmlFile, FileMode.Open, FileAccess.ReadWrite))
                {
                    fs.Seek(countPos, SeekOrigin.Begin);
                    byte[] data = System.Text.Encoding.ASCII.GetBytes(string.Format("{0:00000000000}", count));
                    if (data.Length != 11)
                    {
                        // MOET 11 posities zijn!
                        return;
                    }
                    fs.Write(data, 0, data.Length);
                } //using
            }
        }


        public static string ReadXMLEntry(string xmlFile, XMLLocationEntry entry)
        {
            StringBuilder sb = new StringBuilder();

            using (FileStream fs = new FileStream(xmlFile, FileMode.Open, FileAccess.Read))
            {
                byte[] data = new byte[entry.Length];
                fs.Seek(entry.Location, SeekOrigin.Begin);
                int read = fs.Read(data, 0, data.Length);
                sb.Append(System.Text.Encoding.UTF8.GetString(data));
            }

            return sb.ToString();
        }


        public delegate bool XMLPieceCompleted(XmlDocument xmlDoc, XMLLocationEntry entry);

        public static bool ScanXMLFile(string xmlFile, string startText, string endText, out List<XMLLocationEntry> locationList)
        {
            return ScanXMLFile(xmlFile, startText, endText, out locationList, null);
        }

        public static bool ScanXMLFile(string xmlFile, string startText, string endText, out List<XMLLocationEntry> locationList, XMLPieceCompleted dXMLPieceCompleted)
        {
            bool result = false;
            locationList = new List<XMLLocationEntry>();

            try
            {
                using (TextPositionFileReader textReader = new TextPositionFileReader(xmlFile))
                {
                    XMLLocationEntry entry = new XMLLocationEntry();
                    StringBuilder textEntry = new StringBuilder(1024 * 50);

                    while (!textReader.EOF)
                    {
                        long linePosition;
                        string line = textReader.ReadLine(out linePosition);
                        if (line.Length > 0)
                        {
                            if (entry.Location < 0)
                            {
                                // We zoeken een start
                                int startIndex = line.IndexOf(startText);
                                if (startIndex >= 0)
                                {
                                    // We hebben een start tag gevonden!
                                    entry.Location = linePosition + startIndex;

                                    // voeg data toe aan string
                                    textEntry.Append(line.Substring(startIndex));
                                }
                            }
                            else
                            {
                                // We zoeken een "eind punt"
                                int startIndex = line.IndexOf(endText);
                                if (startIndex >= 0)
                                {
                                    // We hebben een end tag gevonden!
                                    entry.Length = (linePosition + startIndex + endText.Length) - entry.Location;

                                    textEntry.Append(line.Substring(0, startIndex + endText.Length));

                                    // Kijk of we het "stuk naar xml kunnen converten (zoja, dan zal het een correct
                                    // stuk zijn)
                                    bool addEntry = true;
                                    XmlDocument xmlDoc = new XmlDocument();
                                    try
                                    {
                                        xmlDoc.LoadXml(textEntry.ToString());
                                        // Nu event afvuren zodat we wat mee gedaan kan worden
                                        if (dXMLPieceCompleted != null)
                                        {
                                            addEntry = dXMLPieceCompleted(xmlDoc, entry);
                                        }
                                    }
                                    catch
                                    {
                                        addEntry = false;
                                    }

                                    if (addEntry)
                                    {
                                        locationList.Add(entry);
                                    }

                                    // We gaan verder en begin met zoeken naar nieuwe begin tag
                                    entry = new XMLLocationEntry();
                                    textEntry = new StringBuilder(1024 * 50);
                                }
                                else
                                {
                                    // We zitten ergens tussen begin en eind tag
                                    textEntry.Append(line);
                                }
                            }
                        }
                    } //while;
                } //using

                result = true;
            }
            catch { }

            return result;
        }

        public static string MEDEWERKSOORT2performerType(string medewerkersoort)
        {
            switch (medewerkersoort.ToUpper())
            {
                case "CORPORATIE":
                    return "GROUP";
                case "PERSOON":
                    return "PERSON";
                case "COMPONIST":
                    return "COMPOSER";
                case "VERZAMEL":
                    return "COLLECTION";
                default:
                    return "UNKNOWN";
            } //switch
        }

        public static string performerType2MEDEWERKSOORT(string performerType)
        {
            switch (performerType.ToUpper())
            {
                case "GROUP":
                    return "CORPORATIE";
                case "PERSON":
                    return "PERSOON";
                case "COMPOSER":
                    return "COMPONIST";
                case "COLLECTION":
                    return "VERZAMEL";
                default:
                    return "ONBEKEND";
            } //switch
        }

        public static short ToInt16(string value, short defaultValue)
        {
            if (value.Length > 0)
            {
                try
                {
                    return Convert.ToInt16(value);
                }
                catch { }
            }
            return defaultValue;
        }

        public static int ToInt32(string value, int defaultValue)
        {
            if (value.Length > 0)
            {
                try
                {
                    return Convert.ToInt32(value);
                }
                catch { }
            }
            return defaultValue;
        }
    }



    public class TextPositionFileReader : IDisposable
    {
        // Track whether Dispose has been called.
        private bool disposed = false;

        private EndOfLineEncoding eolEncoding;
        private FileStream fs;
        private byte[] data = new byte[1024];
        private bool eof;

        public TextPositionFileReader(string filename, EndOfLineEncoding eolEncoding)
        {
            this.eolEncoding = eolEncoding;

            fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
            eof = false;
        }

        public TextPositionFileReader(string filename)
            : this(filename, EndOfLineEncoding.CrLf)
        {
        }

        #region IDispose implementation
        /// <summary>
        /// Implement IDisposable.
        /// Do not make this method virtual.
        /// A derived class should not be able to override this method.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose(bool disposing) executes in two distinct scenarios.
        /// If disposing equals true, the method has been called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be disposed.
        /// If disposing equals false, the method has been called by the
        /// runtime from inside the finalizer and you should not reference
        /// other objects. Only unmanaged resources can be disposed.
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                try
                {
                    if (fs != null)
                    {
                        fs.Close();
                        fs = null;
                    }
                }
                catch
                {
                }
            }

            disposed = true;
        }

        /// <summary>
        /// Use C# destructor syntax for finalization code.
        /// This destructor will run only if the Dispose method
        /// does not get called.
        /// It gives your base class the opportunity to finalize.
        /// Do not provide destructors in types derived from this class.
        /// </summary>
        ~TextPositionFileReader()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }
        #endregion

        public string ReadLine(out long fileposition)
        {
            StringBuilder sb = new StringBuilder();

            fileposition = fs.Position;
            long count = 0;
            while (true)
            {
                int read = fs.Read(data, 0, data.Length);
                for (int i = 0; i < read; i++)
                {
                    bool addToString = true;

                    switch (eolEncoding)
                    {
                        case EndOfLineEncoding.CrLf:
                            // Hebben we een cr character?
                            if (data[i] == '\r')
                            {
                                if ((i + 1) < read)
                                {
                                    // we kunnen 2 characters vergelijken
                                    if (data[i + 1] == '\n')
                                    {
                                        // We hebben een regel gelezen!
                                        // Zet nieuwe file positie voor volgende regel
                                        fs.Seek(fileposition + count + 2, SeekOrigin.Begin);
                                        return sb.ToString();
                                    }
                                }
                                else
                                {
                                    // Het kan een crlf zijn maar we zitten precies op een
                                    // boundery. We gaan dit character dus opnieuw lezen +
                                    // meer behalve als we natuurlijk op het einde van de file zijn!
                                    if (read != data.Length)
                                    {
                                        // We zijn op het einde van de file! de Cr hoort bij de regel
                                        // dus voeg toe en geef resultaat terug
                                        sb.Append(Convert.ToChar(data[i]));
                                        eof = true;
                                        return sb.ToString();
                                    }

                                    // Er is meer data, dus file positie 1 terug en lees buffer opnieuw in
                                    fs.Seek(-1, SeekOrigin.Current);
                                    eof = false;
                                    addToString = false;
                                }
                            }
                            break;

                        case EndOfLineEncoding.Lf:
                        case EndOfLineEncoding.Cr:
                            throw new Exception("end of line type not supported");
                    } //switch

                    // Alleen toevoegen als we dat mogen
                    if (addToString)
                    {
                        sb.Append(Convert.ToChar(data[i]));
                        count++;
                    }
                } //for i


                if (read <= 0)
                {
                    // eof bereikt
                    eof = true;
                    break;
                }
            } //while

            // Geeft terug wat we hebben
            return sb.ToString();
        }


        public bool EOF
        {
            get
            {
                return eof;
            }
        }

    }


    public enum EndOfLineEncoding
    {
        CrLf,
        Lf,
        Cr
    }


    public class XMLLocationEntry
    {
        public long Location = -1;
        public long Length = 0;
        public string PrivateTag = string.Empty;
    }

}
