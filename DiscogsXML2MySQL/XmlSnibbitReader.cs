using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CDR.Extensions;

namespace DiscogsXML2MySQL
{
    public class XmlSnibbitReader
    {
        public StreamReader stream = null;
        public StringBuilder sbLeft = new StringBuilder();

        public bool OpenFile(string xmlFilename)
        {
            try
            {
                stream = new StreamReader(new FileStream(xmlFilename, FileMode.Open, FileAccess.Read));
                sbLeft.Clear();
                return true;
            }
            catch { }

            return false;
        }

        public void Close()
        {
            if (stream != null)
            {
                stream.Close();
                stream = null;
                sbLeft.Clear();
            }
        }

        public string GetXMLSnibbit(string tag)
        {
            int lastSearchPos = 0;
            int tagBeginPos = -1;
            int tagEndPos = -1;

            string line;
            while ((line = stream.ReadLine()) != null)
            {
                // filter illegal xml chars
                line = FixXML(line);

                sbLeft.Append(line);
                if (tagBeginPos == -1)
                {
                    // Search for begin tag
                    int tmpTagbeginPos1 = sbLeft.IndexOf($"<{tag} ", lastSearchPos, false);
                    int tmpTagbeginPos2 = sbLeft.IndexOf($"<{tag}>", lastSearchPos, false);
                    int tmpTagbeginPos = -1;
                    if (tmpTagbeginPos1 >= 0 || tmpTagbeginPos2 >= 0)
                    {
                        if (tmpTagbeginPos1 >= 0 && tmpTagbeginPos2 >= 0)
                        {
                            tmpTagbeginPos = (tmpTagbeginPos1 > tmpTagbeginPos2) ? tmpTagbeginPos2 : tmpTagbeginPos1;
                        }
                        else
                        {
                            tmpTagbeginPos = (tmpTagbeginPos1 >= 0) ? tmpTagbeginPos1 : tmpTagbeginPos2;
                        }
                    }

                    if (tmpTagbeginPos >= 0)
                    {
                        lastSearchPos = tmpTagbeginPos + tag.Length + 2;
                        tagBeginPos = tmpTagbeginPos;
                    }
                }
                if (tagBeginPos >= 0)
                {
redoEndTagSearch:
                    // Search end tag, /> not taken into account (noty needed so far for discogs xml export)
                    tagEndPos = sbLeft.IndexOf($"</{tag}>", lastSearchPos, false);
                    if (tagEndPos >= 0)
                    {
                        // Is there another opening tag with the same name?
                        int tmpTagbeginPos1 = sbLeft.IndexOf($"<{tag} ", lastSearchPos, false);
                        int tmpTagbeginPos2 = sbLeft.IndexOf($"<{tag}>", lastSearchPos, false);
                        int tmpTagbeginPos = -1;
                        if (tmpTagbeginPos1 >= 0 || tmpTagbeginPos2 >= 0)
                        {
                            if (tmpTagbeginPos1 >= 0 && tmpTagbeginPos2 >= 0)
                            {
                                tmpTagbeginPos = (tmpTagbeginPos1 > tmpTagbeginPos2) ? tmpTagbeginPos2 : tmpTagbeginPos1;
                            }
                            else
                            {
                                tmpTagbeginPos = (tmpTagbeginPos1 >= 0) ? tmpTagbeginPos1 : tmpTagbeginPos2;
                            }
                        }

                        if (tmpTagbeginPos >= 0 && tagEndPos > tmpTagbeginPos)
                        {
                            // We have case of <tag> <tag></tag> </tag>
                            lastSearchPos = tagEndPos + tag.Length + 3;
                            tagEndPos = -1;
                            goto redoEndTagSearch;
                        }

                        tagEndPos += tag.Length + 3;
                    }
                }

                if (tagBeginPos >= 0 && tagEndPos >= 0)
                {
                    string result = sbLeft.Substring(tagBeginPos, tagEndPos - tagBeginPos);
                    string left = sbLeft.Substring(tagEndPos, sbLeft.Length - tagEndPos);
                    sbLeft.Clear();
                    sbLeft.Append(left);

                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// delete any byte that's between 0x00 and 0x1F except 0x09 (tab), 0x0A (LF), and 0x0D (CR).
        /// tab,lf and cr are escaped for mysql
        /// </summary>
        static public string FixXML(string xml)
        {
            StringBuilder sb = new StringBuilder(xml.Length);
            // delete any byte that's between 0x00 and 0x1F except 0x09 (tab), 0x0A (LF), and 0x0D (CR).
            foreach (char c in xml)
            {
                if (c == '\t' || c == '\n' || c == '\r')
                {
                    // escape char
                    switch (c)
                    {
                        case '\t':
                            sb.Append("\\t");
                            break;
                        case '\n':
                            sb.Append("\\n");
                            break;
                        case '\r':
                            sb.Append("\\r");
                            break;
                    } //switch
                }
                else if ((ushort)c > 0x1f)
                {
                    sb.Append(c);
                }
            } //foreach

            return sb.ToString();
        }
    } //class

}
