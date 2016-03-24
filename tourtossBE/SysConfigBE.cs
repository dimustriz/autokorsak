namespace Tourtoss.BE
{
    using System;
    using System.Collections.Generic;
    using System.Xml;
    using System.Xml.Serialization;

    [Serializable]
    public class SysNote
    {
        [XmlAttribute(AttributeName = "lang")]
        public string Lang { get; set; }
        [XmlElement(ElementName = "Text")]
        public string Text { get; set; }
    }

    [Serializable]
    public class SysRelease
    {
        [XmlAttribute(AttributeName = "ver")]
        public string Version { get; set; }
        [XmlAttribute(AttributeName = "date")]
        public string DateStr { get; set; }
        
        [XmlElement(ElementName = "Notes")]
        public List<SysNote> Notes { get; set; }
    }

    [Serializable]
    public class SysConfig
    {
        public SysConfig()
        {
            this.ReleaseList = new List<SysRelease>();
        }

        [XmlIgnore]
        public string Version { get; set; }
        [XmlIgnore]
        public string DateStr { get; set; }

        [XmlIgnore]
        public DateTime Date
        {
            get
            {
                DateTime result = DateTime.MinValue;
                if (!string.IsNullOrEmpty(this.DateStr))
                {
                    string[] arr = this.DateStr.Split('.');
                    if (arr.Length == 3)
                    {
                        int year;
                        int month;
                        int day;
                        if (int.TryParse(arr[0], out day) & int.TryParse(arr[1], out month) & int.TryParse(arr[2], out year))
                        {
                            result = new DateTime(year, month, day);
                        }
                    }
                }

                return result;
            }
        }
        
        [XmlElement(ElementName = "Release")]
        public List<SysRelease> ReleaseList { get; set; }

        public static int CompareBuilds(string v1, string v2)
        {
            if (string.IsNullOrEmpty(v1) && string.IsNullOrEmpty(v2))
            {
                return 0;
            }

            if (string.IsNullOrEmpty(v2))
            {
                return 1;
            }

            if (string.IsNullOrEmpty(v1))
            {
                return -1;
            }

            int result = 0;

            string[] a1 = v1.Split('.');
            string[] a2 = v2.Split('.');

            int i = 0;

            int d1;
            int d2;

            while (result == 0 && i < a1.Length && i < a2.Length)
            { 
                if (int.TryParse(a1[i], out d1) && int.TryParse(a2[i], out d2))
                {
                    result = Math.Sign(d1 - d2);
                }

                i++;
            }
               
            return result;
        }
    }
}
