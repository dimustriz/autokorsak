using System;
using System.IO;
using System.Net;
using System.IO.Compression;

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
            var urlBase = @"http://kfgo.org.ua/autokorsak/";
            var arcName = "AutoKorsak.zip";
            var appName = "AutoKorsak.exe";
            var path = Path.GetDirectoryName(fileName);
            var target = Path.Combine(path, arcName);
            var result = ImportApp(urlBase + arcName, target);

            if (!result) return false;

            try
            {

                using (ZipArchive archive = ZipFile.OpenRead(target))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entry.FullName == appName)
                        {
                            // Here is the App
                            entry.ExtractToFile(Path.Combine(path, Path.GetFileName(fileName)), true);
                        }
                        else
                        {
                            // Rest files if present
                            entry.ExtractToFile(Path.Combine(path, entry.FullName), true);
                        }
                    }
                }

                // Remove downloaded archive
                File.Delete(target);
            }
            catch (Exception)
            {
                return false;
            }

            return result;

        }

    }

}
