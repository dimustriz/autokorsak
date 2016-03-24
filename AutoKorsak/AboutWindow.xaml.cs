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
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        private TournamentView _tournamentView;
        private WindowHelper.ResultHandler OnResult;
        
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

        private void InitLangs()
        {
            this.Title = TournamentView.AppName + " - " + LangResources.LR.About;
            
            lblContactsText.Inlines.Clear();
            lblContactsText.Inlines.Add(new Run(LangResources.LR.ContactsText));

            Inline rMail = new Run(LangResources.LR.ContactsEmailTxt);
            rMail.Cursor = Cursors.Hand;
            rMail.MouseDown += new MouseButtonEventHandler(rMail_MouseDown);

            lblContactsText.Inlines.Add(new Underline(rMail));

            lblContactsText2.Inlines.Clear();
            lblContactsText2.Inlines.Add(new Run(LangResources.LR.ContactsText2));

            Inline rUrl = new Run(LangResources.LR.ContactsUrlTxt);
            rUrl.Cursor = Cursors.Hand;
            rUrl.MouseDown += new MouseButtonEventHandler(rUrl_MouseDown);

            lblContactsText2.Inlines.Add(new Underline(rUrl));

        }

        void rUrl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ShellLib.ShellExecute shellExecute = new ShellLib.ShellExecute();
            shellExecute.Path = LangResources.LR.ContactsUrl;
            if (shellExecute.Path != string.Empty)
                shellExecute.Execute();
        }

        void rMail_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ShellLib.ShellExecute shellExecute = new ShellLib.ShellExecute();
            shellExecute.Path = LangResources.LR.ContactsEmail;
            if (shellExecute.Path != string.Empty)
                shellExecute.Execute();
        }

        public void SetContext(TournamentView view)
        {
            _tournamentView = view;
            DataContext = _tournamentView;

            _tournamentView.Capt.LanguageChanged += new EventHandler(Capt_LanguageChanged);
        }

        public AboutWindow(TournamentView view, WindowHelper.ResultHandler onResult)
        {
            InitializeComponent();
            InitPalette();
            SetContext(view);
            InitLangs();
            OnResult = onResult;
        }

        void Capt_LanguageChanged(object sender, EventArgs e)
        {
            InitLangs();
        }
        
        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (OnResult != null)
                OnResult(ReturnResult.Ok, null);
            Close();
        }

        private void about_Initialized(object sender, EventArgs e)
        {
            this.LoadSettings(true);
        }

        private void about_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.SaveSettings();
        }

    }
}
