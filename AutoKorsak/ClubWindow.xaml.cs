using System;
using System.Windows;
using System.Windows.Media;
using Tourtoss.BE;

namespace AutoKorsak
{
    /// <summary>
    /// Interaction logic for ClubWindow.xaml
    /// </summary>
    public partial class ClubWindow : Window
    {
        private TournamentView _tournamentView;
        private Club _club;
        private string _countryCode;
        private bool _editMode;
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

        private void InitBars()
        {
            //TranslitBar
            bool useTransliteration = _tournamentView.UseTransliteration;
            System.Windows.Visibility hidden = System.Windows.Visibility.Collapsed;
            System.Windows.Visibility visible = System.Windows.Visibility.Visible;
            System.Windows.Visibility visibility = useTransliteration ? visible : hidden;

            lblNameEng.Visibility = visibility;
            txtNameEng.Visibility = visibility;

            pnlTransliterate.Visibility = visibility;
        }

        private void InitLangs()
        {
            if (string.IsNullOrEmpty(_club.Name))
                this.Title = TournamentView.AppName + " - " + LangResources.LR.Club;
            else
                this.Title = TournamentView.AppName + " - " + LangResources.LR.Club + " - " + _club.Name;
        }

        public void SetContext(TournamentView view, Club club, string countryCode, bool editMode)
        {
            _tournamentView = view;
            _club = club;
            _countryCode = countryCode;
            _editMode = editMode;
            DataContext = club;

            _club.Capt.LanguageChanged += new EventHandler(Capt_LanguageChanged);
            _club.Capt.SettingsChanged += new EventHandler(Capt_SettingsChanged);
        }

        public ClubWindow(TournamentView view, Club club, string countryCode, WindowHelper.ResultHandler onResult, bool editMode)
        {
            InitializeComponent();
            InitPalette();
            OnResult = onResult;
            SetContext(view, club, countryCode, editMode);
            InitLangs();
            InitBars();
        }

        void Capt_LanguageChanged(object sender, EventArgs e)
        {
            InitLangs();
        }

        void Capt_SettingsChanged(object sender, EventArgs e)
        {
            InitLangs();
            InitBars();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (OnResult != null)
                OnResult(_editMode ? ReturnResult.Yes : ReturnResult.Ok, _club);
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (OnResult != null)
                OnResult(ReturnResult.Cancel, null); 
            Close();
        }

        private void club_Initialized(object sender, EventArgs e)
        {
            this.LoadSettings();
        }

        private void club_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.SaveSettings();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (OnResult != null)
            {
                if (!OnResult(ReturnResult.Delete, _club))
                    return;
            }
            Close();
        }

        private void btnTransliterate_Click(object sender, RoutedEventArgs e)
        {
            string translit = Translit.Transliterate(_countryCode, _club.Name);

            if (string.IsNullOrEmpty(translit) && !string.IsNullOrEmpty(_club.Name))
                translit = BinaryAnalysis.UnidecodeSharp.Unidecoder.Unidecode(_club.Name);

            _club.NameEn = translit;
            
            if (!string.IsNullOrEmpty(translit))
            {
                _club.EGDName = translit.Length <= 4 ? translit : translit.Substring(0, 4);
            }
            
            _club.Update();
        }

    }
}
