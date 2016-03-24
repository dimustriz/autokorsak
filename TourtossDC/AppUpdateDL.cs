using System;
using System.IO;
using System.Net;

namespace Tourtoss.DL
{

    public class AppUpdateDL: BaseDL
    {

        private bool ImportApp(string url, string fileName)
        {
            WebRequest request;

            bool result = false;

            try
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                using (WebResponse response = request.GetResponse())
                {
                    using (var reader = new BinaryReader(response.GetResponseStream()))
                    {
                        int blockSize = 1024;
                        using (var file = File.Create(fileName, blockSize))
                        {
                            using (var writer = new BinaryWriter(file))
                            {
                                int len = 0;
                                byte[] buff = new byte[blockSize];
                                do
                                {
                                    len = reader.Read(buff, 0, blockSize);
                                    if (len > 0)
                                    {
                                        writer.Write(buff, 0, len);
                                    }
                                }
                                while (len > 0);

                                writer.Close();

                                result = true;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            return result;
        }

        public bool ImportApp(string fileName)
        {
            string urlBase = @"http://kfgo.org.ua/autokorsak/";
            return ImportApp(urlBase + "AutoKorsak.exe", fileName);
        }

    }

}
