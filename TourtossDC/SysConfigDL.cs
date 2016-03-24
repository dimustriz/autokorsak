using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using System.Xml.Serialization;
using System.Reflection;

using System.Net;

using Tourtoss.BE;

namespace Tourtoss.DL
{

    public class SysConfigDL: BaseDL
    {

        private SysConfig ImportSysConfig(string url)
        {
            WebRequest request;

            XmlSerializer serializer = new XmlSerializer(typeof(SysConfig));
            var result = new SysConfig();

            try
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                using (WebResponse response = request.GetResponse())
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        result = (SysConfig)serializer.Deserialize(reader);
                    }
                }
            }
            catch (Exception)
            {
            }

            return result;
        }

        public SysConfig ImportSysConfig()
        {
            SysConfig result = null;
            string urlBase = @"http://kfgo.org.ua/autokorsak/";
            result = ImportSysConfig(urlBase + "ar_sys.xml?stamp=" + DateTime.Now.Ticks.ToString());

            return result;
        }
    
    }

}
