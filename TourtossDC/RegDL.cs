using System;
using System.IO;
using System.Net;
using Microsoft.Win32;

namespace Tourtoss.DL
{

    public class RegDL: BaseDL
    {

        public const string RegPath = @"Software\AutoKorsak.exe\";

        public void SaveRegProp(string propName, string propValue)
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(RegPath);
            if (key != null)
                key.SetValue(propName, propValue == null ? string.Empty : propValue);
        }

        public string LoadRegProp(string propName)
        {
            string result = string.Empty;
            RegistryKey key = Registry.CurrentUser.OpenSubKey(RegPath);
            if (key != null)
            {
                var obj = key.GetValue(propName);
                if (obj != null)
                    result = obj.ToString();
            }
            return result;
        }


    }

}
