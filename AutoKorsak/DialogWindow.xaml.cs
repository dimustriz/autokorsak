using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using System.Net;
using System.IO;

using Tourtoss.BE;

namespace AutoKorsak
{

    // Summary:
    //     Specifies the buttons that are displayed on a message box. Used as an argument
    //     of the Overload:System.Windows.MessageBox.Show method.
    public enum MessageBoxButton
    {
        // Summary:
        //     The message box displays an OK button.
        OK = 0,
        //
        // Summary:
        //     The message box displays OK and Cancel buttons.
        OKCancel = 1,
        //
        // Summary:
        //     The message box displays Yes, No, and Cancel buttons.
        YesNoCancel = 3,
        //
        // Summary:
        //     The message box displays Yes and No buttons.
        YesNo = 4,
        //
        // Summary:
        //     The message box displays Download and Cancel buttons.
        DownloadCancel = 5,
        //
        // Summary:
        //     The message box displays Exit, Send and Cancel buttons.
        ExitSendCancel = 6,
        //
        // Summary:
        //     The message box displays Exit, Copy and Cancel buttons.
        ExitCopyCancel = 7,
        //
        // Summary:
        //     The message box displays Restart and Cancel buttons.
        RestartCancel = 8,
    }

    // Summary:
    //     Specifies which message box button that a user clicks. System.Windows.MessageBoxResult
    //     is returned by the Overload:System.Windows.MessageBox.Show method.
    public enum MessageBoxResult
    {
        // Summary:
        //     The message box returns no result.
        None = 0,
        //
        // Summary:
        //     The result value of the message box is OK.
        OK = 1,
        //
        // Summary:
        //     The result value of the message box is Cancel.
        Cancel = 2,
        //
        // Summary:
        //     The result value of the message box is Yes.
        Yes = 6,
        //
        // Summary:
        //     The result value of the message box is No.
        No = 7,
        //
        // Summary:
        //     The result value of the message box is Download.
        Download = 4,
        //
        // Summary:
        //     The result value of the message box is Exit.
        Exit = 3,
        //
        // Summary:
        //     The result value of the message box is Send.
        Send = 5,
        //
        // Summary:
        //     The result value of the message box is Copy.
        Copy = 8,
        //
        // Summary:
        //     The result value of the message box is Restart.
        Restart = 8,
    }

    /// <summary>
    /// Interaction logic for DialogWindow.xaml
    /// </summary>
    public partial class DialogWindow : Window
    {
        public class MBoxView 
        {
            public MessageBoxImage MessageBoxImage {get; set; }
            public LangResources Capt
            {
                get { return LangResources.LR; }
            }
        }
        
        private void InitPalette()
        {
            Brush body = ValueFromStyleExtension.BodyBrush;
            Brush border = ValueFromStyleExtension.BorderBrush;

            grdMain.Background = body;
            pnlButtons.Background = body;
            pnlButtons.BorderBrush = border;
            pnlInnerBody.BorderBrush = border;
            pnlInnerBody.Background = body;
        }

        public void SetContext(MessageBoxImage image)
        {
            DataContext = new MBoxView() { MessageBoxImage = image};
        }

        public MessageBoxResult ModalResult = MessageBoxResult.None;

        private BitmapImage GetImage(string imgName)
        {
            var imguri = new Uri("/AutoKorsak;Component/Images/" + imgName, UriKind.Relative);
            BitmapImage ni = new BitmapImage(imguri);
            return ni;
        }
        
        public DialogWindow(string text, string caption, MessageBoxButton buttons, MessageBoxImage image)
        {
            InitializeComponent();
            InitPalette();
            SetContext(image);

            btnOk.Visibility = System.Windows.Visibility.Collapsed;
            btnYes.Visibility = System.Windows.Visibility.Collapsed;
            btnNo.Visibility = System.Windows.Visibility.Collapsed;
            btnCancel.Visibility = System.Windows.Visibility.Collapsed;
            btnDownload.Visibility = System.Windows.Visibility.Collapsed;
            btnExit.Visibility = System.Windows.Visibility.Collapsed;
            btnSend.Visibility = System.Windows.Visibility.Collapsed;
            btnCopy.Visibility = System.Windows.Visibility.Collapsed;
            btnRestart.Visibility = System.Windows.Visibility.Collapsed;

            switch (buttons)
            {
                case MessageBoxButton.OK: 
                    btnOk.Visibility = System.Windows.Visibility.Visible;
                    btnOk.Focus();
                    break;
                case MessageBoxButton.OKCancel: 
                    btnOk.Visibility = System.Windows.Visibility.Visible; 
                    btnCancel.Visibility = System.Windows.Visibility.Visible;
                    btnOk.Focus();
                    break;
                case MessageBoxButton.YesNo:
                    btnYes.Visibility = System.Windows.Visibility.Visible;
                    btnNo.Visibility = System.Windows.Visibility.Visible;
                    btnYes.Focus();
                    break;
                case MessageBoxButton.YesNoCancel:
                    btnYes.Visibility = System.Windows.Visibility.Visible;
                    btnNo.Visibility = System.Windows.Visibility.Visible;
                    btnCancel.Visibility = System.Windows.Visibility.Visible;
                    btnYes.Focus();
                    break;
                case MessageBoxButton.DownloadCancel:
                    btnDownload.Visibility = System.Windows.Visibility.Visible;
                    btnCancel.Visibility = System.Windows.Visibility.Visible;
                    btnDownload.Focus();
                    break;
                case MessageBoxButton.ExitSendCancel:
                    btnExit.Visibility = System.Windows.Visibility.Visible;
                    btnSend.Visibility = System.Windows.Visibility.Visible;
                    btnCancel.Visibility = System.Windows.Visibility.Visible;
                    btnSend.Focus();
                    break;
                case MessageBoxButton.ExitCopyCancel:
                    btnExit.Visibility = System.Windows.Visibility.Visible;
                    btnCopy.Visibility = System.Windows.Visibility.Visible;
                    btnCancel.Visibility = System.Windows.Visibility.Visible;
                    btnCopy.Focus();
                    break;
                case MessageBoxButton.RestartCancel:
                    btnRestart.Visibility = System.Windows.Visibility.Visible;
                    btnCancel.Visibility = System.Windows.Visibility.Visible;
                    btnRestart.Focus();
                    break;
            }

            switch (image)
            { 
                case MessageBoxImage.Question:
                    imgWarning.Source = GetImage("questionmark.png");
                    break;
            }

            Title = caption;
            lblText.Text = text;
        }
        
        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            ModalResult = MessageBoxResult.OK;
            Close();
        }

        private void btnNo_Click(object sender, RoutedEventArgs e)
        {
            ModalResult = MessageBoxResult.No;
            Close();
        }

        private void btnYes_Click(object sender, RoutedEventArgs e)
        {
            ModalResult = MessageBoxResult.Yes;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            ModalResult = MessageBoxResult.Cancel;
            Close();
        }

        private void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            ModalResult = MessageBoxResult.Download;
            Close();
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            ModalResult = MessageBoxResult.Send;
            Close();
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            ModalResult = MessageBoxResult.Exit;
            Close();
        }

        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            ModalResult = MessageBoxResult.Copy;
            Close();
        }
        
        private void btnRestart_Click(object sender, RoutedEventArgs e)
        {
            ModalResult = MessageBoxResult.Restart;
            Close();
        }
        
        private void dialog_Initialized(object sender, EventArgs e)
        {
            //this.LoadSettings(true);
        }

        private void dialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //this.SaveSettings();
        }

        public static MessageBoxResult Show(Window owner, string text, string caption, MessageBoxButton buttons, MessageBoxImage image, int? width = null, int? height = null)
        {
            var dlg = new DialogWindow(text, caption, buttons, image);
            
            if (width.HasValue)
                dlg.Width = width.Value;
            if (height.HasValue)
                dlg.Height = height.Value;

            dlg.Owner = owner;
            dlg.ShowDialog();
            return dlg.ModalResult;
        }

        public static void ShowWindow(Window owner, string text, string caption, MessageBoxButton buttons, MessageBoxImage image, int? width = null, int? height = null)
        {
            var dlg = new DialogWindow(text, caption, buttons, image);

            if (width.HasValue)
                dlg.Width = width.Value;
            if (height.HasValue)
                dlg.Height = height.Value;

            dlg.Owner = owner;
            dlg.Show();

        }

    }
}
