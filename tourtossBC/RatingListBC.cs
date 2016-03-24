using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Globalization;
using System.Threading;

using Tourtoss.BE;
using Tourtoss.DL;

namespace Tourtoss.BC
{
    public class RatingListBC: BaseBC<RatingListDL>
    {
        #region Import UFGO & RFG Rating

        public ConfigInfo LoadConfigInfo()
        {
            return GetDL().LoadConfigInfo();
        }

        public bool SaveConfigInfo(ConfigInfo cfg)
        {
            return GetDL().SaveConfigInfo(cfg);
        }

        public RatingList LoadRatingList(RtKind kind, string fileName = null)
        {
            return GetDL().LoadRatingList(kind, fileName);
        }

        public bool SaveRatingList(RatingList rl)
        {
            return GetDL().SaveRatingList(rl);
        }

        public RatingList ImportRatingList(RtKind kind)
        {
            var fullList = GetDL().ImportRatingList(kind, false);
            var uaList = GetDL().ImportRatingList(kind, true);

            CombineLists(fullList, uaList);

            return fullList;
        }

        public string GetFileName(RtKind kind)
        {
            return GetDL().GetRatingListFileName(kind);
        }

        private void CombineLists(RatingList fullList, RatingList uaList)
        {
            if (fullList != null && uaList != null)
            {
                for (int i = 0; i < uaList.Items.Count; i++)
                {
                    string surname = uaList.Items[i].LastName;
                    string firstname = uaList.Items[i].FirstName;

                    var ritem = fullList.Items.Find(item =>
                        (
                            (string.Compare(item.LastName, surname) == 0) && (string.Compare(item.FirstName, firstname) == 0) ||
                            (string.Compare(item.LastNameUa, surname) == 0) && (string.Compare(item.FirstNameUa, firstname) == 0)
                        ));

                    if (ritem != null)
                    {
                        ritem.Rating = uaList.Items[i].Rating;
                        ritem.CityUa = uaList.Items[i].City;
                        ritem.Rank = uaList.Items[i].Rank;
                        ritem.Grade = uaList.Items[i].Grade;
                        ritem.Comment = uaList.Items[i].Comment;
                    }
                    else
                        fullList.Items.Add(uaList.Items[i]);
                }
                fullList.Date = uaList.Date;
            }
        }

        //Is temporary used for RL generating
        /*
        public RatingList CombineDifferentLangs()
        {
            var rlUaRu = GetDL().LoadRatingList(RtKind.ua, RatingListDL.LocalDataFolder + "ar_rl_ua_ru.xml");
            var rlUaUa = GetDL().LoadRatingList(RtKind.ua, RatingListDL.LocalDataFolder + "ar_rl_ua_ua.xml");

            if (rlUaRu.Items.Count == rlUaUa.Items.Count)
            {
                for (int i = 0; i < rlUaRu.Items.Count; i++)
                {
                    rlUaRu.Items[i].LastNameUa =
                        rlUaUa.Items[i].LastName;
                    rlUaRu.Items[i].FirstNameUa =
                        rlUaUa.Items[i].FirstName;
                    rlUaRu.Items[i].CityUa =
                        rlUaUa.Items[i].City;

                    rlUaRu.Items[i].Rating =
                        rlUaUa.Items[i].Rating;
                }
            }

            GetDL().SaveRatingList(rlUaRu);

            return rlUaRu;
        }
        */
        #endregion
    }

}
