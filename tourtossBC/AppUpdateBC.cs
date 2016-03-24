using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;
using Tourtoss.BE;
using Tourtoss.DL;

namespace Tourtoss.BC
{
    public class AppUpdateBC : BaseBC<AppUpdateDL>
    {
        private RegBC _regBc = new RegBC();

        public static EventHandler OnUpdateDownloaded;

        public bool ImportApp(string fileName)
        {
            return GetDL().ImportApp(fileName);
        }

        public void StartDownloadNewBuild()
        {
            new Thread(delegate()
            {
                Thread.CurrentThread.IsBackground = true;

                var assembly = System.Reflection.Assembly.GetEntryAssembly();
                string path = Path.GetDirectoryName(assembly.Location);

                string fileName = path + "\\AutoKorsak_update.exe";

                _regBc.SaveRegProp("UpdTarget", Path.GetFullPath(assembly.Location));
                _regBc.SaveRegProp("UpdSource", fileName);

                if (ImportApp(fileName))
                {
                    SetState(AppUpdateState.Downloaded);

                    if (OnUpdateDownloaded != null)
                    {
                        OnUpdateDownloaded(null, null);
                    }
                }

            }).Start();
        }

        public void SetState(AppUpdateState state)
        {
            switch (state)
            {
                case AppUpdateState.Downloaded: 
                    _regBc.SaveRegProp("UpdState", "1"); 
                    break;
                case AppUpdateState.Updated: 
                    _regBc.SaveRegProp("UpdState", "2"); 
                    break;
                case AppUpdateState.Finished: 
                    _regBc.SaveRegProp("UpdState", "3"); 
                    break;
                default: 
                    _regBc.SaveRegProp("UpdState", "0"); 
                    break;
            }
        }

        public AppUpdateState GetState()
        {
            AppUpdateState result = AppUpdateState.None;
            string state = _regBc.LoadRegProp("UpdState");
            switch (state)
            {
                case "1": 
                    result = AppUpdateState.Downloaded; 
                    break;
                case "2": 
                    result = AppUpdateState.Updated; 
                    break;
                case "3": 
                    result = AppUpdateState.Finished; 
                    break;
            }
            return result;
        }

        public void CheckUpdateState(out bool needRestart)
        {
            needRestart = false;
            var state = GetState();
            if (state != AppUpdateState.None)
            {
                bool done = false;
                int i = 20;
                while (!done && i > 0)
                {
                    switch (state)
                    {
                        case AppUpdateState.Downloaded:
                            done = ExecuteDownloadedApp();
                            if (done)
                            {
                                needRestart = true;
                            }
                            break;
                        case AppUpdateState.Updated:
                            done = CopyAppFromUpdate();
                            if (done)
                            {
                                needRestart = true;
                            }
                            break;
                        case AppUpdateState.Finished: 
                            done = RemoveAppUpdate(); 
                            break;
                        default: 
                            done = true; 
                            break;
                    }
                    if (!done)
                    {
                        Thread.Sleep(100);
                    }
                    i--;
                }

                if (!done) // All tries fault
                {
                    SetState(AppUpdateState.None);
                }
            }
        }

        
        private bool ExecuteDownloadedApp()
        {
            string fileName = _regBc.LoadRegProp("UpdSource");

            SetState(AppUpdateState.Updated);

            System.Diagnostics.Process.Start(fileName);

            return true;
        }

        private bool CopyAppFromUpdate()
        {
            string targetName = _regBc.LoadRegProp("UpdTarget");
            string sourceName = _regBc.LoadRegProp("UpdSource");

            try
            {
                File.Copy(sourceName, targetName, true);
            }
            catch (Exception)
            {
                // DialogWindow.Show(null, LangResources.LR.UnexpectedError + "\n" + ex.Message, LangResources.LR.Warning, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            SetState(AppUpdateState.Finished);

            System.Diagnostics.Process.Start(targetName);

            return true;
        }

        private bool RemoveAppUpdate()
        {
            string fileName = _regBc.LoadRegProp("UpdSource");
            
            try
            {
                File.Delete(fileName);
            }
            catch (Exception)
            {
                // App.SetState(AppUpdateState.None);
                // DialogWindow.Show(null, LangResources.LR.UnexpectedError + "\n" + ex.Message, LangResources.LR.Warning, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            SetState(AppUpdateState.None);
            return true;
        }


    }
}
