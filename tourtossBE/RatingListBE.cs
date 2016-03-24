using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using Tourtoss.BE.Helper;

namespace Tourtoss.BE
{

    public enum RtKind { ua, ru, eu }

    public enum RtItemSortOrder
    {
        Surname,
        FirstName,
        City,
        Place,
        Rating
    }

    [Serializable]
    public class RtItem
    {
        [XmlIgnore]
        public int Place { get; set; }
        [XmlAttribute(AttributeName = "fn")]
        public string FirstName { get; set; }
        [XmlAttribute(AttributeName = "ln")]
        public string LastName { get; set; }
        [XmlAttribute(AttributeName = "ct")]
        public string City { get; set; }
        [XmlAttribute(AttributeName = "fnu")]
        public string FirstNameUa { get; set; }
        [XmlAttribute(AttributeName = "lnu")]
        public string LastNameUa { get; set; }
        [XmlAttribute(AttributeName = "ctu")]
        public string CityUa { get; set; }
        [XmlAttribute(AttributeName = "fne")]
        public string FirstNameEn { get; set; }
        [XmlAttribute(AttributeName = "lne")]
        public string LastNameEn { get; set; }
        [XmlAttribute(AttributeName = "cte")]
        public string CityEn { get; set; }
        [XmlAttribute(AttributeName = "rt")]
        public int Rating { get; set; }
        [XmlAttribute(AttributeName = "d")]
        public string Date { get; set; }
        [XmlAttribute(AttributeName = "rk")]
        public string Rank { get; set; }
        [XmlAttribute(AttributeName = "gr")]
        public string Grade { get; set; }
        [XmlAttribute(AttributeName = "cm")]
        public string Comment { get; set; }

        [XmlIgnore]
        public string DisplayName
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(LastName).Append(" ").Append(FirstName).Append(", ").Append(City).Append(" (").Append(Rating).Append(")");
                return sb.ToString();

            }
        }

        [XmlIgnore]
        public string DisplayNameUa
        {
            get
            {
                if (string.IsNullOrEmpty(FirstNameUa) && string.IsNullOrEmpty(LastNameUa))
                    return DisplayName;

                StringBuilder sb = new StringBuilder();
                sb.Append(LastNameUa).Append(" ").Append(FirstNameUa).Append(", ").Append(CityUa).Append(" (").Append(Rating).Append(")");
                return sb.ToString();

            }
        }

        [XmlIgnore]
        public string DisplayNameEn
        {
            get
            {
                if (string.IsNullOrEmpty(FirstNameEn) && string.IsNullOrEmpty(LastNameEn))
                    return DisplayName;

                StringBuilder sb = new StringBuilder();
                sb.Append(LastNameUa).Append(" ").Append(FirstNameEn).Append(", ").Append(CityEn).Append(" (").Append(Rating).Append(")");
                return sb.ToString();

            }
        }
    }

    [Serializable]
    public class ConfigInfo
    {
        public string RatingUaDate { get; set; }
        public string RatingRuDate { get; set; }
        public string RsUaDate { get; set; }
        public string RsRuDate { get; set; }
    }

    [Serializable]
    public class RatingList
    {
        [XmlAttribute(AttributeName = "kind")]
        public RtKind Kind { get; set; }
        [XmlAttribute(AttributeName = "date")]
        public string Date { get; set; }

        [XmlElement(ElementName = "item")]
        public List<RtItem> Items = new List<RtItem>();

        public static string GetActualUrl(ref string text)
        {
            string result = null;

            if (!string.IsNullOrEmpty(text))
            {
                int p = text.IndexOf("<ul type=\"disc\">");
                if (p >= 0)
                {
                    int p2 = text.IndexOf("href=", p);
                    if (p2 > p)
                    {
                        int p3 = text.IndexOf("\">", p2);

                        if (p3 > p2)
                            result = text.Substring(p2 + 6, p3 - p2 - 6);
                    }
                }
            }

            return result;
        }

        public static string StripHTML(string source)
        {
            try
            {
                string result;

                // Remove HTML Development formatting
                // Replace line breaks with space
                // because browsers inserts space
                result = source.Replace("\r", " ");
                // Replace line breaks with space
                // because browsers inserts space
                result = result.Replace("\n", " ");
                // Remove step-formatting
                result = result.Replace("\t", string.Empty);
                // Remove repeating spaces because browsers ignore them
                result = System.Text.RegularExpressions.Regex.Replace(result,
                                                                      @"( )+", " ");

                // Remove the header (prepare first by clearing attributes)
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*head([^>])*>", "<head>",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"(<( )*(/)( )*head( )*>)", "</head>",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         "(<head>).*(</head>)", string.Empty,
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // remove all scripts (prepare first by clearing attributes)
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*script([^>])*>", "<script>",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"(<( )*(/)( )*script( )*>)", "</script>",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                //result = System.Text.RegularExpressions.Regex.Replace(result,
                //         @"(<script>)([^(<script>\.</script>)])*(</script>)",
                //         string.Empty,
                //         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"(<script>).*(</script>)", string.Empty,
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // remove all styles (prepare first by clearing attributes)
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*style([^>])*>", "<style>",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"(<( )*(/)( )*style( )*>)", "</style>",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         "(<style>).*(</style>)", string.Empty,
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // insert tabs in spaces of <td> tags
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*td([^>])*>", "\t",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // insert line breaks in places of <BR> and <LI> tags
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*br( )*>", "\r",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*li( )*>", "\r",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // insert line paragraphs (double line breaks) in place
                // if <P>, <DIV> and <TR> tags
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*div([^>])*>", "\r\r",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*tr([^>])*>", "\r\r",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*p([^>])*>", "\r\r",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // Remove remaining tags like <a>, links, images,
                // comments etc - anything that's enclosed inside < >
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<[^>]*>", string.Empty,
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // replace special characters:
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @" ", " ",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&bull;", " * ",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&lsaquo;", "<",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&rsaquo;", ">",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&trade;", "(tm)",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&frasl;", "/",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&lt;", "<",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&gt;", ">",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&copy;", "(c)",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&reg;", "(r)",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&nbsp;", " ",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                // Remove all others. More can be added, see
                // http://hotwired.lycos.com/webmonkey/reference/special_characters/
                /*
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&(.{2,6});", string.Empty,
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                */

                // for testing
                //System.Text.RegularExpressions.Regex.Replace(result,
                //       this.txtRegex.Text,string.Empty,
                //       System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // make line breaking consistent
                result = result.Replace("\n", "\r");

                // Remove extra line breaks and tabs:
                // replace over 2 breaks with 2 and over 4 tabs with 4.
                // Prepare first to remove any whitespaces in between
                // the escaped characters and remove redundant tabs in between line breaks
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         "(\r)( )+(\r)", "\r\r",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         "(\t)( )+(\t)", "\t\t",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         "(\t)( )+(\r)", "\t\r",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         "(\r)( )+(\t)", "\r\t",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                // Remove redundant tabs
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         "(\r)(\t)+(\r)", "\r\r",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                // Remove multiple tabs following a line break with just one tab
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         "(\r)(\t)+", "\r\t",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                // Initial replacement target string for line breaks
                string breaks = "\r\r\r";
                // Initial replacement target string for tabs
                string tabs = "\t\t\t\t\t";
                for (int index = 0; index < result.Length; index++)
                {
                    result = result.Replace(breaks, "\r\r");
                    result = result.Replace(tabs, "\t\t\t\t");
                    breaks = breaks + "\r";
                    tabs = tabs + "\t";
                }

                // That's it.

                return result.Trim();
            }
            catch (System.ArgumentException)
            {
                return source;
            }
        }

        private static string GetStringFromHtm(string text)
        {
            StringBuilder sb = new StringBuilder();
            
            int tag = 0;

            foreach (char c in text)
            {
                switch (c)
                {
                    case '<': tag++; break;
                    case '>': tag--; break;
                }

                if (tag == 0) 
                    sb.Append(c);
            }

            return sb.ToString();
        }

        private static bool RepairApostrof(string value, out string result)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (value.IndexOf("&#8217;") > -1)
                {
                    result = value.Replace("&#8217;", "’");
                    return true;
                }
                else
                {
                    result = value;
                }
            }
            
            result = string.Empty;

            return false;
        }

        public static RatingList Parse(ref string text)
        {
            var result = new RatingList();


            if (!string.IsNullOrEmpty(text))
            {

                //int p = text.IndexOf("<tbody>");
                int p = text.IndexOf("<table");
                if (p >= 0)
                {
                    int place = 1;
                    //int pEnd = text.IndexOf("</tbody>", p);
                    int pEnd = text.IndexOf("</table>", p);
                    int p2 = text.IndexOf("<tr>", p);

                    do
                    {
                        if (p2 > p)
                        {
                            int p2End = text.IndexOf("</tr>", p2);

                            if (p2End > p2)
                            {
                                RtItem rec = new RtItem();
                                for (int i = 0; i < 7; i++)
                                {
                                    string s;
                                    int p3 = text.IndexOf("<td", p2);
                                    if (p3 > -1)
                                    {
                                        int p3End = text.IndexOf(">", p3);
                                        int p4 = text.IndexOf(@"</td>", p3End);
                                        if (p4 > p3End)
                                        {
                                            switch (i)
                                            {
                                                case 0: //Place
                                                    rec.Place = place++;
                                                    break;
                                                case 1: //Name
                                                    s = StripHTML(text.Substring(p3End + 1, p4 - p3End - 1));
                                                    int j = s.IndexOf(' ');
                                                    if (j > -1)
                                                    {
                                                        rec.LastName = s.Substring(0, j).Trim();
                                                        rec.FirstName = s.Substring(j + 1).Trim();
                                                        if (rec.LastName.LastIndexOf(',') == rec.LastName.Length - 1)
                                                            rec.LastName = rec.LastName.Remove(rec.LastName.Length - 1);
                                                    }
                                                    else
                                                        rec.LastName = s;
                                                    break;
                                                case 2: //City
                                                    rec.City = StripHTML(text.Substring(p3End + 1, p4 - p3End - 1));
                                                    if (string.IsNullOrWhiteSpace(rec.City))
                                                        rec.City = string.Empty;
                                                    break;
                                                case 3: //Rating
                                                    s = text.Substring(p3End + 1, p4 - p3End - 1);
                                                    int val = 0;
                                                    if (int.TryParse(s, out val))
                                                        rec.Rating = val;
                                                    break;
                                                case 4: //Rank
                                                    rec.Rank = StripHTML(text.Substring(p3End + 1, p4 - p3End - 1));
                                                    if (string.IsNullOrWhiteSpace(rec.Rank))
                                                        rec.Rank = string.Empty;
                                                    break;
                                                case 5: //Grade
                                                    rec.Grade = StripHTML(text.Substring(p3End + 1, p4 - p3End - 1));
                                                    if (string.IsNullOrWhiteSpace(rec.Grade))
                                                        rec.Grade = string.Empty;
                                                    break;
                                                case 6: //Comment
                                                    rec.Comment = StripHTML(text.Substring(p3End + 1, p4 - p3End - 1));
                                                    if (string.IsNullOrWhiteSpace(rec.Comment))
                                                        rec.Comment = string.Empty;
                                                    break;
                                            }
                                            p2 = p4 + 1;
                                        }
                                        else
                                            break;

                                    }
                                }

                                string corrected;

                                if (RepairApostrof(rec.City, out corrected))
                                {
                                    rec.City = corrected;
                                }

                                if (RepairApostrof(rec.FirstName, out corrected))
                                {
                                    rec.FirstName = corrected;
                                }

                                if (RepairApostrof(rec.LastName, out corrected))
                                {
                                    rec.LastName = corrected;
                                }

                                if (RepairApostrof(rec.Rank, out corrected))
                                {
                                    rec.Rank = corrected;
                                }

                                if (RepairApostrof(rec.Grade, out corrected))
                                {
                                    rec.Grade = corrected;
                                }

                                if (RepairApostrof(rec.Comment, out corrected))
                                {
                                    rec.Comment = corrected;
                                }

                                result.Items.Add(rec);
                            }
                            else
                                break;

                            p2 = text.IndexOf("<tr>", p2);

                        }
                        else
                            break;
                    } while (p2 < pEnd || p2 == -1);
                }
            }

            return result;
        }

        public static RatingList ParseRfg(ref string text)
        {
            var result = new RatingList();

            DateTime actualDate = CoreConvert.EmptyDate; 

            if (!string.IsNullOrEmpty(text))
            {
                string datePattern = "<h2 id=\"rating-name\">Текущий рейтинг лист.<br /> Рейтинг–лист по состоянию на ";
                int d = text.IndexOf(datePattern);
                if (d < 0)
                {
                    datePattern = "Рейтинг-лист по состоянию на ";
                    d = text.IndexOf(datePattern);
                }
                if (d < 0)
                {
                    datePattern = "Рейтинг–лист по состоянию на ";
                    d = text.IndexOf(datePattern);
                }
                
                if (d < 0)
                {
                    datePattern = @"<div id=""leftcol"">";
                    d = text.IndexOf(datePattern);
                }
                /*
                if (d > 0)
                {
                    int dEnd = text.IndexOf(" года</h2>", d);
                    if (dEnd > d + datePattern.Length)
                    {
                        string date = text.Substring(d + datePattern.Length, dEnd - (d + datePattern.Length));
                        if (!string.IsNullOrEmpty(date))
                        {
                            string[] arr = date.Split();
                            if (arr.Length > 1)
                            {
                                string m = null;
                                switch (arr[1])
                                {
                                    case "января": m = "01"; break;
                                    case "февраля": m = "02"; break;
                                    case "марта": m = "03"; break;
                                    case "апреля": m = "04"; break;
                                    case "мая": m = "05"; break;
                                    case "июня": m = "06"; break;
                                    case "июля": m = "07"; break;
                                    case "августа": m = "08"; break;
                                    case "сентября": m = "09"; break;
                                    case "октября": m = "10"; break;
                                    case "ноября": m = "11"; break;
                                    case "декабря": m = "12"; break;
                                }

                                if (!string.IsNullOrEmpty(m))
                                    result.Date = arr[0] + "." + m + "." + arr[2];
                            }
                        }
                    }
                }
                */
                int p = text.IndexOf("<table", d);
                if (p >= 0)
                {
                    int place = 1;
                    int p2 = text.IndexOf("<td>1</td>", p);
                    int pEnd = text.IndexOf("</tbody>", p2);

                    do
                    {
                        if (p2 > p)
                        {
                            int p2End = text.IndexOf("</tr>", p2);

                            if (p2End > p2)
                            {
                                RtItem rec = new RtItem();
                                for (int i = 0; i < 6; i++)
                                {
                                    string s;
                                    int val;
                                    int p3 = text.IndexOf("<td", p2);
                                    if (p3 > -1)
                                    {
                                        int p3End = text.IndexOf(">", p3);
                                        int p4 = text.IndexOf(@"</td>", p3End);
                                        if (p4 > p3End)
                                        {
                                            switch (i)
                                            {
                                                case 0: //Place
                                                    rec.Place = place++;
                                                    break;
                                                case 1: //Name
                                                    int p5 = text.IndexOf("<a", p3End);
                                                    int p5a = text.IndexOf(">", p5);
                                                    int p5End = text.IndexOf("</a>", p5);
                                                    s = text.Substring(p5a + 1, p5End - p5a - 1);
                                                    int j = s.IndexOf(' ');
                                                    if (j > -1)
                                                    {
                                                        rec.LastName = s.Substring(0, j);
                                                        rec.FirstName = s.Substring(j + 1);
                                                    }
                                                    else
                                                        rec.LastName = s;
                                                    break;
                                                case 2: //City
                                                    s = text.Substring(p3End + 1, p4 - p3End - 1);
                                                    val = 0;
                                                    //Avoid mistakes in the table
                                                    if (int.TryParse(s, out val))
                                                        rec.Rating = val;
                                                    else
                                                        rec.City = s;
                                                    break;
                                                case 3: //Rating
                                                    if (rec.Rating == 0)
                                                    {
                                                        s = text.Substring(p3End + 1, p4 - p3End - 1);
                                                        val = 0;
                                                        if (int.TryParse(s, out val))
                                                            rec.Rating = val;
                                                    }
                                                    break;
                                                case 4: //Diff
                                                    break;
                                                case 5: //Date
                                                    s = text.Substring(p3End + 1, p4 - p3End - 1);
                                                    var date = CoreConvert.ToDate(s);
                                                    rec.Date = CoreConvert.ToDateString(date);

                                                    if (DateTime.Compare(date, actualDate) > 0)
                                                    {
                                                        actualDate = date;
                                                    }
                                                    break;
                                            }
                                            p2 = p4 + 1;
                                        }
                                        else
                                            break;

                                    }
                                }
                                result.Items.Add(rec);
                            }
                            else
                                break;

                            p2 = text.IndexOf("<tr>", p2);

                        }
                        else
                            break;
                    } while (p2 < pEnd || p2 == -1);
                }
            }

            result.Date = CoreConvert.ToDateString(actualDate);

            return result;
        }

        private static int CompareRtItems(RtItem item1, RtItem item2, RtItemSortOrder order)
        {
            if (item1 == null && item1 == null)
                return 0;
            else
                if (item1 == null)
                    return -1;
                else
                    if (item1 == null)
                        return 1;
            switch (order)
            {
                case RtItemSortOrder.Surname:
                    if (!string.IsNullOrEmpty(item1.LastName) &&
                        !string.IsNullOrEmpty(item2.LastName))
                        return item1.LastName.CompareTo(item2.LastName);
                    break;
                case RtItemSortOrder.FirstName:
                    if (!string.IsNullOrEmpty(item1.FirstName) &&
                        !string.IsNullOrEmpty(item2.FirstName))
                        return item1.FirstName.CompareTo(item2.FirstName);
                    break;
                case RtItemSortOrder.City:
                    if (!string.IsNullOrEmpty(item1.City) &&
                        !string.IsNullOrEmpty(item2.City))
                        return item1.City.CompareTo(item2.City);
                    break;
                case RtItemSortOrder.Rating:
                    return Math.Sign(item2.Rating - item1.Rating);
            }

            return Math.Sign(item1.Place - item2.Place);
        }

        public void Sort(RtItemSortOrder order)
        {
            if (order != RtItemSortOrder.Place)
                Items.Sort(delegate(RtItem item1, RtItem item2)
                {
                    return CompareRtItems(item1, item2, order);
                });
        }
    }
}
