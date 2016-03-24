using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Tourtoss.BC;
using Tourtoss.BE;

namespace AutoKorsak
{

    /// <summary>
    /// Interaction logic for PlayerWindow.xaml
    /// </summary>
    public partial class PlayerWindow : Window
    {
        private TournamentView _tournamentView;
        private TournamentBC _bc = new TournamentBC();
        private RegBC _regBc = new RegBC();
        private Player _playerBak;
        private Player _player;

        private readonly object _initLock = new object();
        private System.Timers.Timer _surnameTimer;

        private WindowHelper.ResultHandler OnResult;

        private void InitPalette()
        {
            Brush body = ValueFromStyleExtension.BodyBrush;
            Brush border = ValueFromStyleExtension.BorderBrush;

            grdMain.Background = body;
            pnlButtons.Background = body;
            pnlButtons.BorderBrush = border;
            pnlSearchResultButtons.Background = body;
            pnlSearchResultButtons.BorderBrush = border;
            pnlSearchResult.BorderBrush = border;
            pnlMainProps.BorderBrush = border;
            
        }

        private void InitTitle()
        {
            string name = _tournamentView.UseTransliteration ? _player.Name : _player.InternationalName;
            if (string.IsNullOrEmpty(name))
                this.Title = TournamentView.AppName + " - " + LangResources.LR.Player;
            else
                this.Title = TournamentView.AppName + " - " + LangResources.LR.Player + " - " + name;
        }

        private void RefreshPlayerList()
        {
            ComboBox cmb = GetSurnameCmb();
            switch (Translator.Language)
            {
                case "uk": cmb.DisplayMemberPath = "DisplayNameUa"; break;
                case "en": cmb.DisplayMemberPath = "DisplayNameEn"; break;
                default: cmb.DisplayMemberPath = "DisplayName"; break;
            }

        }

        private void SelectClub(string clubName)
        {
            foreach (var item in cmbClub.Items)
            {
                Club club = item as Club;
                if (club != null)
                {
                    if (!string.IsNullOrEmpty(clubName) && (clubName == club.Name || clubName == club.NameEn || clubName == club.NameEn || clubName == club.NameUa || clubName == club.EGDName))
                    {
                        cmbClub.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        private void InitLangs()
        {
            Club selClub = GetSelectedClub();
            
            foreach (var item in grdSearhchResult.Columns)
                item.Header = Translator.Translate("Common", item.HeaderStringFormat);

            InitTitle();

            btnApply.Content = _player.IsCreated ? LangResources.LR.Apply : LangResources.LR.SaveAndCreateNew;

            RefreshPlayerList();

            _player.RefreshLang();
            
            RefreshClubs(selClub);
            RefreshGrade();
        }

        private void InitBars()
        {
            //TranslitBar
            bool useTransliteration = _tournamentView.UseTransliteration;
            System.Windows.Visibility hidden = System.Windows.Visibility.Collapsed;
            System.Windows.Visibility visible = System.Windows.Visibility.Visible;
            System.Windows.Visibility visibility = useTransliteration ? visible : hidden;

            lblLocalFirstName.Visibility = visibility;
            txtLocalFirstName.Visibility = visibility;
            lblLocalSurname.Visibility = visibility;

            if (!useTransliteration)
            {
                txtLocalSurname.Visibility = hidden;
                cmbLocalSurname.Visibility = hidden;
            }
            else 
            {
                if (_tournamentView.LocalPlayerDbUsage)
                {
                    txtLocalSurname.Visibility = hidden;
                    cmbLocalSurname.Visibility = visible;
                }
                else
                {
                    txtLocalSurname.Visibility = visible;
                    cmbLocalSurname.Visibility = hidden;
                }
            }

            if (!useTransliteration && _tournamentView.LocalPlayerDbUsage)
            {
                txtSurname.Visibility = hidden;
                cmbSurname.Visibility = visible;
            }
            else
            {
                txtSurname.Visibility = visible;
                cmbSurname.Visibility = hidden;
            }

            pnlTransliterate.Visibility = visibility;

            lblFirstName.Text = useTransliteration ? LangResources.LR.InternationalName : LangResources.LR.FirstName;
            lblSurname.Text = useTransliteration ? LangResources.LR.InternationalSurname : LangResources.LR.Surname;

            //SuperBar
            bool useSuperBar = _tournamentView.Tournament != null && _tournamentView.Tournament.UseMacMahonStartScores && _tournamentView.Tournament.UseMacMahonSuperBar;
            cbSuperBarMember.Visibility = useSuperBar ? visible : hidden;

            //Start scores and numbers
            bool useStartScores = _tournamentView.Tournament != null && _tournamentView.Tournament.UseMacMahonStartScores && _tournamentView.Tournament.UseMacMahonStartScoresManually;
            bool useStartNumbers = _tournamentView.Tournament != null && (_tournamentView.Tournament.TournamentSystemRound || _tournamentView.Tournament.TournamentSystemScheveningen);

            lblStartScores.Visibility = useStartScores ? visible : hidden;
            txtStartScores.Visibility = useStartScores ? visible : hidden;
            lblStartNumber.Visibility = useStartNumbers ? visible : hidden;
            txtStartNumber.Visibility = useStartNumbers ? visible : hidden;

            UpdateRatingTab();
        }

        public void SetContext(TournamentView view, Player player)
        {
            _tournamentView = view;
            _player = player;
            _player.AlternativeNameIfEmpty = false;
            string clubName = _player.Club;

            var countries = new CountryList();
            countries.AddRange(_tournamentView.Countries);
            cmbCountry.ItemsSource = countries;
            cmbNationaity.ItemsSource = countries;

            var grades = new GradeList();
            grades.FillByDefault();

            cmbGrade.ItemsSource = grades;

            if (!_player.IsCreated)
            {
                _player.Country = _regBc.LoadRegProp("Default Country");
                _player.Club = _regBc.LoadRegProp("Default Club");
                bool preliminary;
                if (bool.TryParse(_regBc.LoadRegProp("Default Preliminary"), out preliminary))
                    _player.PreliminaryRegistration = preliminary;
                else
                    _player.PreliminaryRegistration = false;
            }

            DataContext = player;

            _playerBak = new Player();
            _player.CopyTo(_playerBak);

            _player.Capt.LanguageChanged += new EventHandler(Capt_LanguageChanged);
            _player.Capt.SettingsChanged += new EventHandler(Capt_SettingsChanged);
            InitBars();
            InitLangs();

            wpRounds.Children.Clear();
            for (int i = 1; i <= _tournamentView.NumberOfRounds; i++)
            {
                var ctl = new CheckBox() { Content = Tour.ToRoman(i) };
                ctl.IsChecked = _player.NotPlayingInRound.FindIndex(round => round == i) == -1;
                ctl.Margin = new Thickness(2, 2, 10, 2);
                wpRounds.Children.Add(ctl);
            }

            //Team combobox
            var items = new List<string>();
            items.Clear();
            foreach (var item in _tournamentView.Players)
            {
                bool found = false;
                for (int i = 0; i < items.Count; i++)
                    if (items[i] != null && items[i].ToString() == item.Team)
                    {
                        found = true;
                        break;
                    }
                if (!found)
                    items.Add(item.Team);
            }
            items.Sort();
            string s = _player.Team;
            cmbTeam.Items.Clear();
            foreach (var item in items)
                cmbTeam.Items.Add(item);
            _player.Team = s;
            cmbTeam.Text = _player.Team;

            //Coach combobox
            items = new List<string>();
            items.Clear();
            foreach (var item in _tournamentView.Players)
            {
                bool found = false;
                for (int i = 0; i < items.Count; i++)
                    if (items[i] != null && items[i].ToString() == item.Coach)
                    {
                        found = true;
                        break;
                    }
                if (!found)
                    items.Add(item.Coach);
            }
            items.Sort();
            s = _player.Coach;
            cmbCoach.Items.Clear();
            foreach (var item in items)
                cmbCoach.Items.Add(item);
            _player.Coach = s;
            cmbCoach.Text = _player.Coach;

            //Additional buttons
            btnPrev.DataContext = _tournamentView;
            btnNext.DataContext = _tournamentView;

            //Local Players Database support
            UpdateSurnameCombo();

            txtStartNumber_TextChanged(null, null);

            //Restore club that was changed on several event handlers
            new Thread(delegate()
            {
                Thread.CurrentThread.IsBackground = true;

                this.Dispatcher.BeginInvoke(new MethodInvoker(delegate
                {
                    _player.Club = clubName;
                    SelectClub(clubName);
                }));
            }).Start();

        }

        public PlayerWindow(TournamentView view, Player player, WindowHelper.ResultHandler onResult)
        {
            InitializeComponent();

            InitPalette();
            OnResult = onResult;

            SetContext(view, player);
        }

        void Capt_LanguageChanged(object sender, EventArgs e)
        {
            InitLangs();
            txtScoreAdjustment_TextChanged(null, null);
        }

        void Capt_SettingsChanged(object sender, EventArgs e)
        {
            Club selClub = GetSelectedClub();
            InitTitle();
            InitBars();
            RefreshPlayerList();
            RefreshClubs(selClub);
        }

        private void GetEGgdRequest(string firstname, string surname)
        {
            WebRequest request;
            string text;
            string url = "http://www.europeangodatabase.eu/EGD/GetPlayerDataByData.php?"+
                "lastname=" + surname + 
                "&name=" + firstname;

            this.Cursor = Cursors.Wait;
            try
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                using (WebResponse response = request.GetResponse())
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        text = reader.ReadToEnd();
                        var egdData = EgdSearchResult.Parse(text);
                        if (egdData != null)
                        {
                            //egdData.retcode == "Ok"
                            grdSearhchResult.ItemsSource = egdData.players;
                            tabSearch.Visibility = System.Windows.Visibility.Visible;
                            tabSearch.Focus();
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Arrow;
                DialogWindow.Show(this, ex.Message, LangResources.LR.EgdSearchError, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            this.Cursor = Cursors.Arrow;
        }
        
        private void btnFindInEGD_Click(object sender, RoutedEventArgs e)
        {
            GetEGgdRequest(_player.FirstName, _player.Surname);
        }

        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            EgdPlayer sel = grdSearhchResult.SelectedValue as EgdPlayer;
            if (sel != null)
            {
                _player.Surname = sel.Last_Name;
                _player.FirstName = sel.Name;
                _player.Rank = sel.Grade;
                _player.Rating = sel.Gor;
                _player.Country = sel.Country_Code.ToLower();
                _player.EgdPin = sel.Pin_Player;

                if (!string.IsNullOrEmpty(sel.Club))
                {
                    var clubs = _tournamentView.Countries.GetClubs(_player.Country);
                    if (clubs != null)
                    {
                        var club = clubs.Find(cl => cl != null && cl.Name == _player.Club);
                        if (club != null)
                        {
                            if (!_bc.CheckClub(_tournamentView.Tournament, _player.Country, club.EGDName))
                            {
                                _tournamentView.UpdateClub(_player.Country, club);
                                RefreshClubs();
                            }
                            _player.Club = club.Name;
                        }
                    }
                }

                _player.Update();
            }
            tabMain.Focus();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            _player.Surname = string.Empty;
            _player.FirstName = string.Empty;
            _player.LocalSurname = string.Empty;
            _player.LocalFirstName = string.Empty;
            _player.Rating = 0;
            _player.Country = string.Empty;
            _player.Club = string.Empty; 
            _player.Team = string.Empty;
            _player.Coach = string.Empty;
            _player.Grade = 0;
            _player.EgdPin = 0;

            UpdateRatingTab();

            _player.Update();
        }

        private void grdSearhchResult_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var grid = sender as DataGrid;
            if (grid.CurrentCell == null || grid.CurrentCell.Column == null || grid.CurrentCell.Column.SortMemberPath == "Result")
                return;

            if (!App.GridCheckDblClick(e))
                return;
            
            btnSelect_Click(null, null);
            e.Handled = true;
        }

        private void Apply(bool closeWindow)
        {
            _player.NotPlayingInRound.Clear();
            for (int i = 1; i <= wpRounds.Children.Count; i++)
            {
                var ctl = wpRounds.Children[i - 1] as CheckBox;
                if (ctl.IsChecked == false)
                    _player.NotPlayingInRound.Add(i);
            }

            _player.CopyTo(_playerBak);

            _regBc.SaveRegProp("Default Country", _player.Country);
            _regBc.SaveRegProp("Default Club", _player.Club);
            _regBc.SaveRegProp("Default Preliminary", _player.PreliminaryRegistration.ToString());

            if (OnResult != null)
                OnResult(closeWindow ? ReturnResult.Ok : ReturnResult.Apply, _player);
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            Apply(false);
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            Apply(true);
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void player_Initialized(object sender, EventArgs e)
        {
            this.LoadSettings();
        }

        private bool CheckSaved()
        {
            bool result = true;
            if (!_playerBak.Equals(_player))
            {
                var r = DialogWindow.Show(this, LangResources.LR.SaveChanges, LangResources.LR.Warning, MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                switch (r)
                {
                    case MessageBoxResult.None:
                    case MessageBoxResult.Cancel:
                        return false;
                    case MessageBoxResult.Yes:
                        Apply(false);
                        return true;
                }
            }
            return result;
        }
        
        private void player_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!CheckSaved())
            {
                e.Cancel = true;
                return;
            } 
            
            this.SaveSettings();

            StopSurnameTimer();

        }

        private Club GetSelectedClub()
        {
            Club sel = cmbClub.SelectedItem as Club;
            return sel != null ? sel.Clone() as Club : null;
        }

        private void RefreshClubs(Club selectedClub = null)
        {
            if (selectedClub == null) 
                selectedClub = GetSelectedClub();

            string selectedEGDName = selectedClub != null ? selectedClub.EGDName : null;

            cmbClub.Items.Clear();

            List<Club> clubs = new List<Club>();
            
            List<RatingSystem.Club> rClubs = null;
            List<Club> cClubs = null;

            if (_tournamentView.Countries != null)
            {
                cClubs = _tournamentView.Countries.GetClubs(_player.Country);
            }

            if (_tournamentView.RSystem != null && (string.IsNullOrEmpty(_player.Country) || _tournamentView.RSystem.Kind.ToString() == _player.Country))
                rClubs = _tournamentView.RSystem.Clubs;

            if (cClubs != null)
                clubs.AddRange(cClubs);

            if (rClubs != null)
            {
                foreach (var item in rClubs)
                {
                    if (!string.IsNullOrEmpty(item.EGDName) && clubs.Find(p => p.Name == item.Name) == null)
                    {
                        clubs.Add(new Club()
                        {
                            Name = item.Name,
                            NameUa = item.NameUa,
                            NameEn = item.NameEn,
                            EGDName = item.EGDName
                        });
                    }
                }

            }

            var cmb = cmbClub;
            switch (Translator.Language)
            {
                case "uk":
                    clubs.Sort((x, y) => string.Compare(x.DisplayNameUa, y.DisplayNameUa));
                    cmb.DisplayMemberPath = "DisplayNameUa"; 
                    break;
                case "en":
                    clubs.Sort((x, y) => string.Compare(x.DisplayNameEn, y.DisplayNameEn));
                    cmb.DisplayMemberPath = "DisplayNameEn"; 
                    break;
                default:
                    clubs.Sort((x, y) => string.Compare(x.DisplayName, y.DisplayName));
                    cmb.DisplayMemberPath = "DisplayName";
                    break;
            }

            foreach (var item in clubs)
            {
                cmb.Items.Add(item);
            } 

            SelectClub(selectedEGDName);
        }

        private void RefreshGrade()
        {
            int i = cmbGrade.SelectedIndex;
            var grades = new GradeList();
            grades.FillByDefault();

            cmbGrade.ItemsSource = grades;
            cmbGrade.SelectedIndex = i;

        }

        private bool DrawLines(List<RatingSystem.RatingRec> items, Polyline line, Canvas canvas, string footer1, string footer2)
        {
            bool result = false;

            //Clear drawing
            line.Points.Clear();
            while (canvas.Children.Count > 1)
                canvas.Children.RemoveAt(canvas.Children.Count - 1);

            if (items.Count == 1)
                items.Add(new RatingSystem.RatingRec() { Rating = items[0].Rating, Date = items[0].Date });

            if (items != null && items.Count > 0)
            {
                double minRating = items[0].Rating;
                double maxRating = items[0].Rating;

                foreach (var item in items)
                {
                    if (minRating > item.Rating)
                        minRating = item.Rating;
                    if (maxRating < item.Rating)
                        maxRating = item.Rating;
                }

                int steps = 10;
                double step = (double)(maxRating - minRating) / steps;

                //Draw coordinates
                int N = 2;
                if (items != null && items.Count > 0)
                    for (int i = 0; i < N; i++)
                    {
                        if (footer1 == null && footer2 == null && items[0].PersonId == -1)
                            break;

                        TextBlock textBlock = new TextBlock();
                        if (i == 0)
                        {
                            if (footer1 != null)
                                textBlock.Text = footer1;
                            else
                                textBlock.Text = items[items.Count - 1].DateStr;
                            Canvas.SetLeft(textBlock, 10);
                        }
                        else
                        {
                            if (footer2 != null)
                                textBlock.Text = footer2;
                            else
                                textBlock.Text = items[0].DateStr;
                            Canvas.SetLeft(textBlock, canvas.Width - 110);
                            textBlock.TextAlignment = TextAlignment.Right;
                            textBlock.Width = 100;
                        }

                        //textBlock.Foreground = ValueFromStyleExtension.BorderBrush;
                        textBlock.FontSize = 12;
                        Canvas.SetTop(textBlock, canvas.Height - 20);
                        canvas.Children.Add(textBlock);

                    }

                double dY = (canvas.Height - 50) / steps;
                double j = minRating;
                for (int i = 0; i <= steps; i++)
                {
                    TextBlock textBlock = new TextBlock();
                    textBlock.Text = ((int)j).ToString();
                    //textBlock.Foreground = ValueFromStyleExtension.BorderBrush;
                    textBlock.FontSize = 12;
                    Canvas.SetLeft(textBlock, 4);
                    Canvas.SetTop(textBlock, canvas.Height - i * dY - 45);

                    textBlock.TextAlignment = TextAlignment.Right;
                    textBlock.Width = 32;

                    canvas.Children.Add(textBlock);
                    j += step;
                }

                //draw rating
                for (int i = 0; i < items.Count; i++)
                {
                    int id = items.Count - i - 1;
                    double x = (canvas.Width - 54) / (items.Count - 1) * i + 44;
                    double d = items[id].Rating - minRating;
                    double dMax = maxRating - minRating;
                    if (dMax == 0)
                    {
                        d = 1;
                        dMax = 2;
                    }

                    double y = canvas.Height - d * (canvas.Height - 50) / dMax - 40;
                    line.Points.Add(new Point(x, y));
                }
            }
            else 
            { 
                TextBlock textBlock = new TextBlock();
                textBlock.Text = LangResources.LR.NoData;
                //textBlock.Foreground = ValueFromStyleExtension.BorderBrush;
                textBlock.FontSize = 12;
                Canvas.SetLeft(textBlock, (canvas.Width - 100) / 2);
                Canvas.SetTop(textBlock, canvas.Height/2);

                textBlock.TextAlignment = TextAlignment.Center;
                textBlock.Width = 100;

                canvas.Children.Add(textBlock);
            }
  
            return result;
        }

        private bool CreatePoints(RatingSystem system, RatingSystem.Person person)
        {
            bool result = false;

            result &= system != null && DrawLines(system.Ratings.FindAll(item => person != null && item.PersonId == person.Id), polyLine, cvRating, null, null);

            string footer1 = null;
            string footer2 = null;
            var tourRating = _bc.GetRatingGrowth(_tournamentView.Tournament, _tournamentView.CurrentRoundNumber, _player.Id);
            if (tourRating.Count > 0)
            {
                double delta = tourRating[0].Rating - _player.Rating;
                double dAbs = delta >= 0 ? delta : - delta;
                footer1 = _player.Capt.NewRating + ": " + Math.Round(tourRating[0].Rating, 2).ToString() + 
                    " (" + (delta < 0 ? "-" : "+") + Math.Round(dAbs, 2).ToString() + ")" +
                    (_player.RatingAbnormal ? " - " + LangResources.LR.AbnormalGrowth : string.Empty);
            }

            footer2 = _player.Capt.Round + " " + Tour.ToRoman(_tournamentView.CurrentRoundNumber);

            result &= DrawLines(tourRating, newLine, cvTournamentRating, footer1, footer2);

            return result;
        }

        private void UpdateRatingTab()
        {
            if (_tournamentView.RSystem != null && _tournamentView.RSystem.Persons.Count > 0)
            {
                string surname = _tournamentView.UseTransliteration ? _player.LocalSurname : _player.Surname;
                string firstname = _tournamentView.UseTransliteration ? _player.LocalFirstName : _player.FirstName;
                var ritem = _tournamentView.RSystem.Persons.Find(item => 
                    (
                        (string.Compare(item.LastName, surname, true) == 0) && (string.Compare(item.FirstName, firstname, true) == 0) ||
                        (string.Compare(item.LastNameUa, surname, true) == 0) && (string.Compare(item.FirstNameUa, firstname, true) == 0) ||
                        (string.Compare(item.LastNameEn, surname, true) == 0) && (string.Compare(item.FirstNameEn, firstname, true) == 0)
                    ));
                CreatePoints(_tournamentView.RSystem, ritem);
                tabRatingHistory.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                CreatePoints(null, null);
                tabRatingHistory.Visibility = System.Windows.Visibility.Collapsed;
            }
            if (tabRatingHistory.Visibility != System.Windows.Visibility.Visible)
                tcRating.SelectedIndex = 1;
        }

        private void cmbCountry_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshClubs();
            UpdateRatingTab();
        }

        bool OnClubWindowReturn(ReturnResult ret, object value)
        {
            var club = value as Club;
            switch (ret)
            {
                case ReturnResult.Delete:
                    {
                        var country = _tournamentView.Countries.Find(c => c != null && c.InternetCode == _player.Country);
                        if (country != null)
                        {
                            var currentClub = country.Clubs.Find(c => c != null && c.Name == _player.Club);
                            if (currentClub != null)
                            {
                                int count = 0;
                                foreach (var item in _tournamentView.Players)
                                    if (item.Club == currentClub.Name)
                                        count++;

                                var r = count == 0 ? MessageBoxResult.Yes :
                                    DialogWindow.Show(this, LangResources.LR.Delete + " " + count + " " + LangResources.GetRecordsStr(count) + "?" +
                                    "\n" + LangResources.LR.DataWillNot, LangResources.LR.Warning,
                                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                                if (r == MessageBoxResult.Yes)
                                {
                                    foreach (var item in _tournamentView.Players)
                                        if (item.Club == currentClub.Name)
                                            item.Club = string.Empty;
                                    country.Clubs.Remove(currentClub);

                                    _player.Club = string.Empty;

                                    RefreshClubs();
                                }
                                else
                                    return false;
                            }
                            else
                            { 
                            }
                        }
                        else 
                        { 
                        }

                        _player.Update();
                        _tournamentView.UpdatePlayers();
                        _tournamentView.UpdateWallList();
                        break;
                    }
                case ReturnResult.Yes: //edit
                    {
                        var country = _tournamentView.Countries.Find(c => c != null && c.InternetCode == _player.Country);
                        if (country != null)
                        {
                            var currentClub = country.Clubs.Find(c => c != null && c.Name == _player.Club);
                            if (currentClub != null)
                            {
                                foreach (var item in _tournamentView.Players)
                                    if (item.Club == currentClub.Name)
                                        item.Club = club.Name;
                                club.CopyTo(currentClub);

                                RefreshClubs();

                                _player.Club = club.Name;
                            }
                        }

                        _player.Update();
                        _tournamentView.UpdatePlayers();
                        _tournamentView.UpdateWallList();
                        break;
                    }
                case ReturnResult.Ok: //create
                    {
                        var country = _tournamentView.Countries.Find(c => c != null && c.InternetCode == _player.Country);
                        if (country != null)
                        {
                            if (country.Clubs.Find(c => c != null && c.Name == club.Name) == null)
                            {
                                country.Clubs.Add(club);

                                RefreshClubs();

                                _player.Club = club.Name;

                            }
                        }

                        _player.Update();
                        break;
                    }
            }

            return true;
        }

        private void ExecuteClubWindow(TournamentView view, Club club, string countryCode, bool editMode)
        {
            ClubWindow dlg = App.GetOpenedWindow(typeof(ClubWindow)) as ClubWindow;
            if (dlg == null)
                dlg = new ClubWindow(view, club, countryCode, OnClubWindowReturn, editMode);
            else
                dlg.SetContext(view, club, countryCode, editMode);
            dlg.ShowWindow();
        }

        private void btnCreateClub_Click(object sender, RoutedEventArgs e)
        {
            var club = new Club();
            ExecuteClubWindow(_tournamentView, club, 
                !string.IsNullOrEmpty(_player.Nationality) ? _player.Nationality : _player.Country, 
                false);
        }

        private void btnEditClub_Click(object sender, RoutedEventArgs e)
        {
            var country = _tournamentView.Countries.Find(c => c != null && c.InternetCode == _player.Country);
            if (country != null)
            {
                var club = country.Clubs.Find(c => c != null && c.Name == _player.Club);
                
                if (!string.IsNullOrEmpty(_player.Club) && club == null)
                { 
                    if (_tournamentView.RSystem != null)
                    {
                        var rSystemclub = _tournamentView.RSystem.Clubs.Find(p => p.Name == _player.Club || p.NameUa == _player.Club || p.NameEn == _player.Club);
                        if (rSystemclub != null)
                            club = new Club()
                            {
                                EGDName = rSystemclub.EGDName,
                                Name = rSystemclub.Name,
                                NameEn = rSystemclub.NameEn,
                                NameUa = rSystemclub.NameUa
                            };
                    }
                }

                if (club != null)
                    ExecuteClubWindow(_tournamentView, club.Clone() as Club, 
                        !string.IsNullOrEmpty(_player.Nationality) ? _player.Nationality : _player.Country,
                        true);
            }

        }

        private void btnTransliterate_Click(object sender, RoutedEventArgs e)
        {
            var region = string.IsNullOrEmpty(_player.Nationality) ? _player.Country : _player.Nationality;
            
            if (string.IsNullOrEmpty(region))
            {
                if (Translator.Language == "uk")
                    region = "ua";
                else
                    region = Translator.Language;
            };

            _player.Surname = Translit.Transliterate(region, _player.LocalSurname);
            _player.FirstName = Translit.Transliterate(region, _player.LocalFirstName);

            if (string.IsNullOrEmpty(_player.Surname) && !string.IsNullOrEmpty(_player.LocalSurname))
                _player.Surname = BinaryAnalysis.UnidecodeSharp.Unidecoder.Unidecode(_player.LocalSurname);
            if (string.IsNullOrEmpty(_player.FirstName) && !string.IsNullOrEmpty(_player.LocalFirstName))
                _player.FirstName = BinaryAnalysis.UnidecodeSharp.Unidecoder.Unidecode(_player.LocalFirstName);

            _player.Update();
        }

        private void txtScoreAdjustment_TextChanged(object sender, TextChangedEventArgs e)
        {
            double levels = 0;
            double.TryParse(txtScoreAdjustment.Text, out levels);
            if ((int)levels != levels)
                levels = levels * 10;
            lblLevels.Text = LangResources.GetLevelsStr((int)levels);
        }

        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckSaved())
                return;
            if (OnResult != null)
                OnResult(ReturnResult.Prev, _player);
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckSaved())
                return;
            if (OnResult != null)
                OnResult(ReturnResult.Next, _player);
        }

        RatingSystem.Person _selectedPerson = null;

        private void cmbLocalSurname_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var selection = e.AddedItems[0] as RatingSystem.Person;
                if (selection != null)
                    _selectedPerson = selection;
            }
        }

        private void cmbLocalSurname_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_selectedPerson != null)
            {
                new Thread(delegate()
                {
                    Thread.CurrentThread.IsBackground = true;

                    this.Dispatcher.BeginInvoke(new MethodInvoker(delegate
                    {
                        if (_selectedPerson != null)
                        {
                            string firstname = string.Empty;
                            string lastname = string.Empty;

                            switch (Translator.Language)
                            {
                                case "uk":
                                    {
                                        lastname = !string.IsNullOrEmpty(_selectedPerson.LastNameUa) ? _selectedPerson.LastNameUa : _selectedPerson.LastName;
                                        firstname = !string.IsNullOrEmpty(_selectedPerson.FirstNameUa) ? _selectedPerson.FirstNameUa : _selectedPerson.FirstName;
                                        break;
                                    }
                                default:
                                    {
                                        lastname = _selectedPerson.LastName;
                                        firstname = _selectedPerson.FirstName;
                                        break;
                                    }
                            }

                            if (cmbLocalSurname.Visibility != System.Windows.Visibility.Visible)
                            {
                                _player.Surname = lastname;
                                _player.FirstName = firstname;
                            }
                            else
                            {
                                _player.LocalSurname = lastname;
                                _player.LocalFirstName = firstname;
                            }

                            _player.Rating = _selectedPerson.Rating;
                            
                            _player.Rank = _selectedPerson.Rank;
                            _player.Grade = _selectedPerson.Grade;
                            _player.Comment = _selectedPerson.Comment;

                            if (_tournamentView.RList.Kind == RtKind.ru)
                                _player.Country = "ru";
                            else
                                if (_tournamentView.RList.Kind == RtKind.ua)
                                    _player.Country = "ua";

                            if (!string.IsNullOrEmpty(_selectedPerson.FirstNameEn) && !string.IsNullOrEmpty(_selectedPerson.LastNameEn))
                            {
                                _player.FirstName = _selectedPerson.FirstNameEn;
                                _player.Surname = _selectedPerson.LastNameEn;
                            }

                            Club club = null;
                            if (_selectedPerson.ClubsLink != null)
                            {
                                var cl = _selectedPerson.ClubsLink.Find(c => c.Id == _selectedPerson.ClubId);
                                if (cl != null)
                                {
                                    club = new Club()
                                    {
                                        EGDName = cl.EGDName,
                                        Name = cl.Name,
                                        NameEn = cl.NameEn,
                                        NameUa = cl.NameUa
                                    };
                                }
                            }

                            UpdateRatingTab();

                            _player.Update();
                            RefreshClubs(club);

                            _selectedPerson = null;
                        }
                    }));
                }).Start();
            }

        }

        private List<RatingSystem.Person> GetFilterItems(string context)
        {
            var items = new List<RatingSystem.Person>();

            if (string.IsNullOrEmpty(context))
                return items;

            string[] arr = context.Split(' ');
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i].EndsWith(","))
                    arr[i] = arr[i].Remove(arr[i].Length - 1);
            }

            int cnt = 0;

            if (_tournamentView.RSystem != null && _tournamentView.RSystem.Persons != null)
            {
                foreach (var item in _tournamentView.RSystem.Persons)
                {
                    bool fnd;

                    //Find in default language
                    fnd = !string.IsNullOrEmpty(item.LastName) && item.LastName.IndexOf(arr[0], StringComparison.OrdinalIgnoreCase) == 0;
                    if (fnd && arr.Length > 1)
                        fnd = !string.IsNullOrEmpty(item.FirstName) && item.FirstName.IndexOf(arr[1], StringComparison.OrdinalIgnoreCase) == 0;

                    //Find in additional language
                    if (!fnd)
                    {
                        fnd = !string.IsNullOrEmpty(item.LastNameUa) && item.LastNameUa.IndexOf(arr[0], StringComparison.OrdinalIgnoreCase) == 0;
                        if (fnd && arr.Length > 1)
                            fnd = !string.IsNullOrEmpty(item.FirstNameUa) && item.FirstNameUa.IndexOf(arr[1], StringComparison.OrdinalIgnoreCase) == 0;
                    }

                    //Find in English
                    if (!fnd)
                    {
                        fnd = !string.IsNullOrEmpty(item.LastNameEn) && item.LastNameEn.IndexOf(arr[0], StringComparison.OrdinalIgnoreCase) == 0;
                        if (fnd && arr.Length > 1)
                            fnd = !string.IsNullOrEmpty(item.FirstNameEn) && item.FirstNameEn.IndexOf(arr[1], StringComparison.OrdinalIgnoreCase) == 0;
                    }

                    if (fnd)
                    {
                        /*
                        var person = _tournamentView.RSystem.Persons.Find(p => p != null && item != null &&
                            (
                                string.Compare(p.LastName, item.LastName, true) == 0 && string.Compare(p.FirstName, item.FirstName, true) == 0
                            ));

                        if (person != null)
                        {
                            item.FirstNameEn = person.FirstNameEn;
                            item.LastNameEn = person.LastNameEn;
                        }
                        */
                        items.Add(item);
                        cnt++;
                        if (cnt == 20)
                            break;
                    }
                }
            }

            return items;
        }

        private ComboBox GetSurnameCmb()
        {
            return cmbLocalSurname.Visibility == System.Windows.Visibility.Visible ? cmbLocalSurname : cmbSurname;
        }

        private void UpdateSurnameCombo(bool shoDropdown = false)
        {
            new Thread(delegate()
            {
                Thread.CurrentThread.IsBackground = true;

                this.Dispatcher.BeginInvoke(new MethodInvoker(delegate
                {
                    ComboBox cmb = GetSurnameCmb();
                    string s = cmb.Text;

                    List<RatingSystem.Person> items = GetFilterItems(s);

                    int start = 0;
                    int length = 0;

                    TextBox tb = (TextBox)cmbLocalSurname.Template.FindName("PART_EditableTextBox", cmb);
                    if (tb != null)
                    {
                        start = tb.SelectionStart;
                        length = tb.SelectionLength;
                    }

                    cmb.ItemsSource = items;
                    cmb.IsDropDownOpen = shoDropdown && !string.IsNullOrEmpty(s) && items.Count > 0;
                    cmb.Text = s;

                    if (tb != null)
                    {
                        tb.SelectionStart = start;
                        tb.SelectionLength = length;
                    }
                }));
            }).Start();
        }

        private void SurnameTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            StopSurnameTimer();
            UpdateSurnameCombo(true);
        }

        private void StartSurnameTimer()
        {
            lock (_initLock)
            {
                if (_surnameTimer == null)
                {
                    _surnameTimer = new System.Timers.Timer(100);
                    _surnameTimer.Elapsed += SurnameTimer_Elapsed;
                }
                _surnameTimer.Enabled = false;
                _surnameTimer.Enabled = true;
            }
        }

        private void StopSurnameTimer()
        {
            lock (_initLock)
            {
                if (_surnameTimer != null)
                {
                    _surnameTimer.Elapsed -= SurnameTimer_Elapsed;
                    _surnameTimer.Enabled = false;
                    _surnameTimer.Dispose();
                    _surnameTimer = null;
                }
            }
        }

        private void cmbLocalSurname_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key >= Key.A && e.Key <= Key.Z || e.Key == Key.Decimal || e.Key == Key.Back || e.Key == Key.Delete)
            {
                StartSurnameTimer();
            }
            if (e.Key == Key.Enter)
            {
                cmbLocalSurname_LostFocus(sender, null);
            }
        }

        private void cmbLocalSurname_DropDownClosed(object sender, EventArgs e)
        {
            cmbLocalSurname_LostFocus(sender, null);
        }

        private void txtStartNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtStartNumber.Visibility == System.Windows.Visibility.Visible)
            {
                string s = txtStartNumber.Text;
                int i = 0;
                if (int.TryParse(s, out i))
                    _player.StartNumber = i;

                if (_tournamentView.StartNumberDuplicated(_player))
                    lblStartNumberError.Visibility = System.Windows.Visibility.Visible;
                else
                    lblStartNumberError.Visibility = System.Windows.Visibility.Collapsed;
            } 
            else
                lblStartNumberError.Visibility = System.Windows.Visibility.Collapsed;
        }

    }
}
