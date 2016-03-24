using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml;

using System.Web.Script.Serialization;

namespace Tourtoss.BE
{
    public class EgdPlayer
    {
        public int Pin_Player { get; set; }
        public int AGAID { get; set; }
        public string Last_Name { get; set; }
        public string Name { get; set; }
        public string Country_Code { get; set; }
        public string Club { get; set; }
        public string Grade { get; set; }
        public int Grade_n { get; set; }
        public int EGF_Placement { get; set; }
        public int Gor { get; set; }
        public int DGor { get; set; }
        public string Proposed_Grade { get; set; }
        public int Tot_Tournaments { get; set; }
        public string Last_Appearance { get; set; }
        public string Elab_Date { get; set; }
        public int Hidden_History { get; set; }
        public string Real_Last_Name { get; set; }
        public string Real_Name { get; set; }
    }

    public class EgdSearchResult
    {
        public EgdSearchResult() { players = new List<EgdPlayer>(); }
        public string retcode { get; set; }
        public List<EgdPlayer> players { get; set; }

        public static EgdSearchResult Parse(string text)
        {
            JavaScriptSerializer ser = new JavaScriptSerializer();
            return ser.Deserialize<EgdSearchResult>(text);
        }
    }

}
