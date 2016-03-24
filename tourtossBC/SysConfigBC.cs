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
    public class SysConfigBC : BaseBC<SysConfigDL>
    {
        public SysConfig Load()
        {
            SysConfig result = GetDL().ImportSysConfig();

            if (result != null)
            {
                if (result.ReleaseList.Count > 0)
                {
                    result.Version = result.ReleaseList[0].Version;
                    result.DateStr = result.ReleaseList[0].DateStr;
                }
            }

            return result;
        }


        private List<SysRelease> GetReleaseList(SysConfig sc, string ver, string lang)
        {
            if (string.IsNullOrEmpty(ver))
                return null;

            List<SysRelease> result = new List<SysRelease>();

            foreach (var item in sc.ReleaseList)
            {
                if (SysConfig.CompareBuilds(item.Version, ver) > 0)
                    result.Add(item);
                else
                    break;
            }

            return result;
        }

        public string GetNotesText(SysConfig sc, string ver, string lang)
        {
            var relList = GetReleaseList(sc, ver, lang);
            if (relList == null || relList.Count == 0)
                return null;

            StringBuilder sb = new StringBuilder();

            foreach (var item in relList)
            {

                if (sb.Length == 0)
                {
                    sb.AppendLine(string.Format(LangResources.LR.NewVersion, item.Version, item.DateStr));
                    sb.AppendLine();
                    sb.AppendLine(LangResources.LR.WhatsNew);
                }
                else
                {
                    sb.AppendLine();
                    sb.AppendLine(string.Format(LangResources.LR.VersionInfo, item.Version, item.DateStr));
                }

                foreach (var note in item.Notes)
                    if (note.Lang == lang)
                    {
                        sb.AppendLine(note.Text);
                    }
            }

            return sb.ToString();
        }

    }
}
