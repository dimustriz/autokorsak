using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using Microsoft.Win32;
using Tourtoss.BE;
using Tourtoss.DL;

namespace Tourtoss.BC
{
    public class RegBC : BaseBC<RegDL>
    {
        public string RegPath { get { return RegDL.RegPath; } }

        public RegistryKey OpenKey(string path)
        {
            return Registry.CurrentUser.OpenSubKey(RegPath + path);
        }

        public RegistryKey CreateKey(string path)
        {
            return Registry.CurrentUser.CreateSubKey(RegPath + path);
        }

        public string LoadRegProp(string propName)
        {
            return GetDL().LoadRegProp(propName);
        }

        public void SaveRegProp(string propName, string propValue)
        {
            GetDL().SaveRegProp(propName, propValue);
        }
    }
}
