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

    public class RatingSystemDL: BaseDL
    {

        public static string LocalDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\AutoKorsak\";


        public string GetFileName(RtKind kind)
        {
            string result = null;

            switch (kind)
            {
                case RtKind.ua:
                    result = LocalDataFolder + "ar_rs_ua.xml";
                    break;
                case RtKind.ru:
                    result = LocalDataFolder + "ar_rs_ru.xml";
                    break;
                case RtKind.eu:
                    result = LocalDataFolder + "ar_ra_eu.xml";
                    break;
            }

            return result;
        }

        private void OnLoad(RatingSystem rts)
        {
            foreach (var item in rts.Persons)
            {
                item.ClubsLink = rts.Clubs;
            }
        }

        public RatingSystem Load(RtKind kind)
        {
            string fileName = GetFileName(kind);

            XmlSerializer serializer = new XmlSerializer(typeof(RatingSystem));
            var result = new RatingSystem();
            Stream stream = null;
            try
            {
                stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                result = (RatingSystem)serializer.Deserialize(stream);
            }
            catch (Exception ex)
            {/* ... */
                ex.ToString();
            }
            finally
            {
                if (null != stream)
                    stream.Close();
            }
            result.Kind = kind;

            OnLoad(result);

            return result;
        }

        public bool Save(RatingSystem rs)
        {
            bool result = false;

            string fileName = GetFileName(rs.Kind);
            string filePath = System.IO.Path.GetDirectoryName(fileName);

            if (!Directory.Exists(filePath))
                try
                {
                    Directory.CreateDirectory(filePath);
                }
                catch (Exception) { }

            if (!string.IsNullOrEmpty(fileName))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(RatingSystem));
                TextWriter writer = new StreamWriter(fileName);
                try
                {
                    serializer.Serialize(writer, rs);
                    result = true;
                }
                catch {/* ... */}
                finally
                {
                    writer.Close();
                }
            }
            return result;
        }

        private RatingSystem ImportRatingSystem(string url)
        {
            WebRequest request;

            XmlSerializer serializer = new XmlSerializer(typeof(RatingSystem));
            var result = new RatingSystem();

            try
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                using (WebResponse response = request.GetResponse())
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        result = (RatingSystem)serializer.Deserialize(reader);
                    }
                }
            }
            catch (Exception)
            {
            }

            return result;
        }

        public RatingSystem ImportRatingSystem(RtKind kind)
        {
            RatingSystem result = null;
            string urlBase = @"http://kfgo.org.ua/autokorsak/rating/";
            switch (kind)
            {
                case RtKind.ua:
                    {
                        result = ImportRatingSystem(urlBase + "ar_rs_ua.xml?stamp=" + DateTime.Now.Ticks.ToString());
                        break;
                    }
                case RtKind.ru:
                    {
                        result = ImportRatingSystem(urlBase + "ar_rs_ru.xml?stamp=" + DateTime.Now.Ticks.ToString());
                        break;
                    }
            }

            OnLoad(result);

            return result;
        }
    
    }

}
