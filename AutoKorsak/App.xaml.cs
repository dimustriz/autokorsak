using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

using System.Reflection;
using System.IO;
using System.Globalization;
using System.Windows.Markup;

using Tourtoss.BE;

namespace AutoKorsak
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)   
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnResolveAssembly;
            DispatcherUnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);
            AppDomain.CurrentDomain.UnhandledException +=
                  new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
                         new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag))); 
            base.OnStartup(e);
        }

        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            ProceedException(e.Exception);
            //MessageBox.Show(e.Exception.ToString(), LangResources.LR.Warning);
            e.Handled = true;
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ProceedException(e.ExceptionObject as Exception);
        }

        void ProceedException(Exception ex)
        {
            if (string.IsNullOrEmpty(_error))
            {
                if (ex is CustomHandledException)
                {
                    _error = ex.Message;
                    _isCustomError = true;
                }
                if (ex is TargetInvocationException && ex.InnerException != null)
                {
                    _error = ex.InnerException.Message;
                    _isCustomError = true;
                }
                else
                {
                    _error = ex.ToString();
                    _isCustomError = false;
                }

                StartExecutionTimer();
            }
        }

        private readonly object _initLock = new object();
        private System.Timers.Timer _executionTimer;
        private static string _error = string.Empty;
        private static bool _isCustomError = false;

        private void ExecutionTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            StopExecutionTimer();
            ShowErrorWindow();
        }

        private void StartExecutionTimer()
        {
            lock (_initLock)
            {
                if (_executionTimer == null)
                {
                    _executionTimer = new System.Timers.Timer(2000);
                    _executionTimer.Elapsed += ExecutionTimer_Elapsed;
                }
                _executionTimer.Enabled = false;
                _executionTimer.Enabled = true;
            }
        }

        private void StopExecutionTimer()
        {
            lock (_initLock)
            {
                if (_executionTimer != null)
                {
                    _executionTimer.Elapsed -= ExecutionTimer_Elapsed;
                    _executionTimer.Enabled = false;
                    _executionTimer.Dispose();
                    _executionTimer = null;
                }
            }
        }

        private void ShowErrorWindow()
        {
            this.Dispatcher.BeginInvoke(new MethodInvoker(delegate
                {
                    try
                    {
                        MessageBoxResult r = 0;
                        if (_isCustomError)
                        {
                            r = DialogWindow.Show(MainWindow,
                            _error,
                            LangResources.LR.Warning, MessageBoxButton.OK, 
                            MessageBoxImage.Error, null, 200);
                        }
                        else
                        {
                            r = DialogWindow.Show(MainWindow,
                            LangResources.LR.UnexpectedError + "\n\r" + _error,
                            LangResources.LR.Warning, MessageBoxButton.ExitCopyCancel,
                            MessageBoxImage.Stop, 500, 400);
                        }
                        switch (r)
                        {
                            case MessageBoxResult.Exit:
                                if (MainWindow != null)
                                    MainWindow.Close();
                                else
                                    Shutdown();
                                break;
                            case MessageBoxResult.Send:
                                SendErrorEmail(_error);
                                break;
                            case MessageBoxResult.Copy:
                                CopyToClipboard(_error);
                                break;
                        }

                        _error = string.Empty;
                    }
                    catch (Exception ex)
                    {
                        ex.ToString();
                        StartExecutionTimer();
                    }
                }));
        }


        private void CopyToClipboard(string error)
        {
            Clipboard.SetDataObject(error);
        }

        private void SendErrorEmail(string error)
        {
            ShellLib.ShellExecute shellExecute = new ShellLib.ShellExecute();
            shellExecute.Path = LangResources.LR.ContactsEmail;

            shellExecute.Path +=
                "?subject=" + LangResources.LR.UnexpectedError +
                "&body=" + error.Replace("&", "%26");

            //Clipboard.SetText(error);
            
            if (shellExecute.Path != string.Empty)
                shellExecute.Execute();
        }

        public static Assembly OnResolveAssembly(object sender, ResolveEventArgs args)
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            AssemblyName assemblyName = new AssemblyName(args.Name);

            string path = assemblyName.Name + ".dll";
            if (assemblyName.CultureInfo != null && assemblyName.CultureInfo.Equals(CultureInfo.InvariantCulture) == false)
            {
                path = String.Format(@"{0}\{1}", assemblyName.CultureInfo, path);
            }

            using (Stream stream = executingAssembly.GetManifestResourceStream(path))
            {
                if (stream == null)
                    return null;

                byte[] assemblyRawBytes = new byte[stream.Length];
                stream.Read(assemblyRawBytes, 0, assemblyRawBytes.Length);
                return Assembly.Load(assemblyRawBytes);
            }
        }
    }
}
