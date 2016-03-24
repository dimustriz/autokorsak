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

    public class RatingListDL: BaseDL
    {

        public static string LocalDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\AutoKorsak\";

        private RatingList DownloadUfgoRatingList(string url)
        {
            WebRequest request;

            XmlSerializer serializer = new XmlSerializer(typeof(RatingList));
            var result = new RatingList();

            try
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                using (WebResponse response = request.GetResponse())
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        result = (RatingList)serializer.Deserialize(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                string s = ex.Message;
            }

            return result;
        }

        private RatingList ImportUfgoRatingList()
        {
            WebRequest request;
            string text;
            string urlBase = @"http://ufgo.org";
            string url = urlBase + @"/rating-list/";
            //string urlBase = @"http://kfgo.org.ua/autokorsak/test/rating-list";
            //string url = urlBase + @"/rating-list.htm";

            RatingList result = null;
            try
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                using (WebResponse response = request.GetResponse())
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        text = reader.ReadToEnd();

                        url = RatingList.GetActualUrl(ref text);
                        if (!string.IsNullOrEmpty(url))
                        {
                            request = (HttpWebRequest)WebRequest.Create(url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ? url : urlBase + url);
                            using (WebResponse response2 = request.GetResponse())
                            {
                                using (StreamReader reader2 = new StreamReader(response2.GetResponseStream()))
                                {
                                    text = reader2.ReadToEnd();
                                    var ratingData = RatingList.Parse(ref text);
                                    if (ratingData != null)
                                    {
                                        result = ratingData;

                                        string d = url.Substring(url.Length - 11, 10);
                                        if (!string.IsNullOrEmpty(d))
                                        {
                                            string[] arr = d.Split('-');
                                            if (arr.Length > 1)
                                            {
                                                if (arr[2][0] == '0')
                                                    arr[2] = arr[2][1].ToString();
                                                result.Date = arr[2] + "." + arr[1] + "." + arr[0];
                                            }
                                        }
                                    }
                                }
                            }
                        }

                    }
                }
            }
            catch (WebException ex)
            {
                throw ex;
            }
            catch (Exception)
            {
            }

            return result;
        }

        private RatingList ImportRfgRatingList()
        {
            WebRequest request;
            string text;
            //string url = @"http://gofederation.ru/ratings/61";
            //string url = @"http://gofederation.ru/ratings/real_rating";
            string url = @"http://gofederation.ru/players/";
        

            RatingList result = null;
            try
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                using (WebResponse response = request.GetResponse())
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        text = reader.ReadToEnd();

                        var ratingData = RatingList.ParseRfg(ref text);
                        if (ratingData != null)
                            result = ratingData;

                    }
                }
            }
            catch (Exception)
            {
            }

            return result;
        }

        public RatingList ImportRatingList(RtKind kind, bool useUfgoSite = false)
        {
            string urlBase = @"http://kfgo.org.ua/autokorsak/rating/";

            var result = new RatingList();

            switch (kind)
            {
                case RtKind.ua:
                    {
                        if (useUfgoSite)
                            result = ImportUfgoRatingList();
                        else
                            result = DownloadUfgoRatingList(urlBase + "ar_rl_ua.xml?stamp=" + DateTime.Now.Ticks.ToString());
                        break;
                    }
                case RtKind.ru:
                    {
                        result = ImportRfgRatingList();
                        break;
                    }
            }

            if (result != null)
                result.Kind = kind;

            return result;
        }

        public string GetRatingListFileName(RtKind kind)
        {
            string result = null;

            switch (kind)
            {
                case RtKind.ua:
                    result = LocalDataFolder + "ar_rl_ua.xml";
                    break;
                case RtKind.ru:
                    result = LocalDataFolder + "ar_rl_ru.xml";
                    break;
                case RtKind.eu:
                    result = LocalDataFolder + "ar_rl_eu.xml";
                    break;
            }

            return result;
        }

        private string GetRatingSystemFileName()
        {
            return LocalDataFolder + "ar_rs.xml";
        }

        private string GetConfigInfo()
        {
            return LocalDataFolder + "ar_cfg.xml";
        }

        public RatingList LoadRatingList(RtKind kind, string fileName = null)
        {
            if (fileName == null)
                fileName = GetRatingListFileName(kind);

            XmlSerializer serializer = new XmlSerializer(typeof(RatingList));
            var result = new RatingList();
            Stream stream = null;
            try
            {
                stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                result = (RatingList)serializer.Deserialize(stream);
                for (int i = 0; i < result.Items.Count; i++)
                    result.Items[i].Place = i + 1;
                result.Kind = kind;
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
            return result;
        }
        
        public bool SaveRatingList(RatingList rl)
        {
            bool result = false;

            string fileName = GetRatingListFileName(rl.Kind);
            string filePath = System.IO.Path.GetDirectoryName(fileName);
            
            if (!Directory.Exists(filePath))
                try
                {
                    Directory.CreateDirectory(filePath);
                }
            catch (Exception) {}

            if (!string.IsNullOrEmpty(fileName))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(RatingList));
                var writer = new StreamWriter(fileName);
                try
                {
                    serializer.Serialize(writer, rl);
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

        public ConfigInfo LoadConfigInfo()
        {
            string fileName = GetConfigInfo();

            XmlSerializer serializer = new XmlSerializer(typeof(ConfigInfo));
            var result = new ConfigInfo();
            Stream stream = null;
            try
            {
                stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                result = (ConfigInfo)serializer.Deserialize(stream);
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
            return result;
        }

        public bool SaveConfigInfo(ConfigInfo rl)
        {
            bool result = false;

            string fileName = GetConfigInfo();
            string filePath = System.IO.Path.GetDirectoryName(fileName);

            if (!Directory.Exists(filePath))
                try
                {
                    Directory.CreateDirectory(filePath);
                }
                catch (Exception) { }

            if (!string.IsNullOrEmpty(fileName))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ConfigInfo));
                var writer = new StreamWriter(fileName);
                try
                {
                    serializer.Serialize(writer, rl);
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


    }

}
