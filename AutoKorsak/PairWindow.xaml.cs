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
using Tourtoss.BC;

namespace AutoKorsak
{
    /// <summary>
    /// Interaction logic for PairWindow.xaml
    /// </summary>
    public partial class PairWindow : Window
    {
        private TournamentView _tournamentView;
        private Pair _pairBak;
        private Pair _pair;
        private TournamentBC _bc = new TournamentBC();
        private bool _isActivated = false;

        private WindowHelper.ResultHandler OnResult;

        private void InitPalette()
        {
            Brush body = ValueFromStyleExtension.BodyBrush;
            Brush border = ValueFromStyleExtension.BorderBrush;

            grdMain.Background = body;
            pnlMainProps.BorderBrush = border;
            pnlButtons.Background = body;
            pnlButtons.BorderBrush = border;
            pnlInnerBody.BorderBrush = border;
            pnlInnerBody.Background = body;
            pnlInnerButtons.Background = body;
            pnlInnerButtons.BorderBrush = border;
        }

        private void InitLangs()
        {
            this.Title = TournamentView.AppName + " - " + LangResources.LR.Round + " " + Tour.ToRoman(_pair.TourId + 1) + " - " + LangResources.LR.Pair;
            if (_pair.BoardNumber != 0)
                this.Title += " - " + _pair.BoardNumber;

            if (_tournamentView.Tournament != null && _tournamentView.Tournament.FirstMoveBlack)
            {
                txtFirstPlayer.Text = LangResources.LR.Black;
                txtSecondPlayer.Text = LangResources.LR.White;
            }
            else
            {
                txtFirstPlayer.Text = LangResources.LR.White;
                txtSecondPlayer.Text = LangResources.LR.Black;
            }

            btnApply.Content = _pair.IsCreated ? LangResources.LR.Apply : LangResources.LR.SaveAndCreateNew;
        }

        private void UpdatePlayerComboboxes()
        {
            bool act = _isActivated;
            _isActivated = false;
            try
            {
                cmbFirstPlayer.DisplayMemberPath = Tournament.UseTransliteration ? "NameExt2" : "InternationalNameExt2";
                cmbSecondPlayer.DisplayMemberPath = Tournament.UseTransliteration ? "NameExt2" : "InternationalNameExt2";

                cmbFirstPlayer.ItemsSource = _tournamentView.GetFreePlayers(_pair, _pairBak, true);
                cmbFirstPlayer.SelectedValue = _pair.FirstPlayerId;
                cmbSecondPlayer.ItemsSource = _tournamentView.GetFreePlayers(_pair, _pairBak, false);
                cmbSecondPlayer.SelectedValue = _pair.SecondPlayerId;


                if (_tournamentView.AlreadyPlayed(_pair.FirstPlayerId, _pair.SecondPlayerId, _pair.TourId))
                    pnlAlreadyPlayed.Visibility = System.Windows.Visibility.Visible;
                else
                    pnlAlreadyPlayed.Visibility = System.Windows.Visibility.Collapsed;
            }
            finally
            {
                _isActivated = act;
            }
        }

        public void SetContext(TournamentView view, Pair pair)
        {
            _tournamentView = view;
            _pair = pair;
            _pairBak = new Pair();
            _pair.CopyTo(_pairBak);
            DataContext = _pair;

            _pair.Capt.LanguageChanged += new EventHandler(Capt_LanguageChanged);
            _pair.Capt.SettingsChanged += new EventHandler(Capt_SettingsChanged);
            InitLangs();

            cmbGameResult.ItemsSource = _tournamentView.ResultKinds;
            cmbGameResult.SelectedValue = pair.Result;

            UpdatePlayerComboboxes();
            UpdateGameResult();

            //Additional buttons
            btnPrev.DataContext = _tournamentView;
            btnNext.DataContext = _tournamentView;

            btnDeletePair.IsEnabled = _pair.BoardNumber > 0;
        }

        public PairWindow(TournamentView view, Pair pair, WindowHelper.ResultHandler onResult)
        {
            InitializeComponent();
            InitPalette();
            OnResult = onResult;
            SetContext(view, pair);
            _isActivated = true;
        }

        void Capt_LanguageChanged(object sender, EventArgs e)
        {
            InitLangs();
            UpdatePlayerComboboxes();
            TextBox_TextChanged(null, null);
            txtKomi_TextChanged(null, null);
        }

        void Capt_SettingsChanged(object sender, EventArgs e)
        {
            UpdatePlayerComboboxes();
            Capt_LanguageChanged(null, null);
        }

        private void UpdatePairPlayers()
        {
            _pair.FirstPlayerId = cmbFirstPlayer.SelectedValue == null ? -1 : (int)cmbFirstPlayer.SelectedValue;
            _pair.SecondPlayerId = cmbSecondPlayer.SelectedValue == null ? -1 : (int)cmbSecondPlayer.SelectedValue;

            if (_pair.FirstPlayerId <= 0)
            {
                
                if (_pair.GameResult == 1)
                    _pair.GameResult = 2;
                else
                    if (_pair.GameResult == 2)
                        _pair.GameResult = 1;

                _pair.FirstPlayerId = _pair.SecondPlayerId;
                _pair.SecondPlayerId = -1;
            }

            if (_pair.SecondPlayerId == 0)
                _pair.SecondPlayerId = -1;

            _pair.PairingWithBye = _pair.SecondPlayerId == -1 || _pair.FirstPlayerId == -1;
        }

        private void UpdateHandicap()
        {
            int id1 = cmbFirstPlayer.SelectedValue == null ? -1 : (int)cmbFirstPlayer.SelectedValue;
            int id2 = cmbSecondPlayer.SelectedValue == null ? -1 : (int)cmbSecondPlayer.SelectedValue;
            int komi;
            bool swap;
            _pair.Handicap = _bc.GetHandicap(_tournamentView.Tournament, _tournamentView.CurrentRoundNumber, id1, id2, out komi, out swap);
            _pair.AdditionalKomi = komi;
            
            if (swap)
            {
                btnSwapPlayers_Click(null, null);
            }

            _pair.OnPropertyChanged("Handicap");
            _pair.OnPropertyChanged("AdditionalKomi");
        }

        private void Apply(bool closeWindow)
        {
            UpdatePairPlayers();

            if (OnResult != null)
                OnResult(closeWindow ? ReturnResult.Ok : ReturnResult.Apply, _pair);

            _pair.CopyTo(_pairBak);

            if (!closeWindow)
                SetContext(_tournamentView, _pair);
        }
        
        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            Apply(true);
            Close();
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            Apply(false);
        }
        
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (OnResult != null)
                OnResult(ReturnResult.Cancel, null);
            Close();
        }

        private void btnDeletePair_Click(object sender, RoutedEventArgs e)
        {
            if (OnResult != null)
                OnResult(ReturnResult.Delete, _pairBak);
            Close();
        }

        private void player_Initialized(object sender, EventArgs e)
        {
            this.LoadSettings();
        }                                           

        private void player_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!CheckSaved())
            {
                e.Cancel = true;
                return;
            }
            this.SaveSettings();
        }

        private void btnSwapPlayers_Click(object sender, RoutedEventArgs e)
        {
            _pair.SecondPlayerId = cmbFirstPlayer.SelectedValue == null ? -1 : (int)cmbFirstPlayer.SelectedValue;
            _pair.FirstPlayerId = cmbSecondPlayer.SelectedValue == null ? -1 : (int)cmbSecondPlayer.SelectedValue;

            if (_pair.GameResult == 1)
                _pair.GameResult = 2;
            else
                if (_pair.GameResult == 2)
                    _pair.GameResult = 1;
            
            UpdatePlayerComboboxes();
            UpdateGameResult();

            _pair.Update();
        }

        private void UpdateGameResult()
        {
            imgFirstWon.Visibility = _pair.IsFirstWon ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            imgSecondWon.Visibility = _pair.IsSecondWon ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        private void cmbFirstPlayer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isActivated)
            {
                UpdatePairPlayers();
                UpdatePlayerComboboxes();
                UpdateHandicap();
            }
        }

        private void cmbSecondPlayer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isActivated)
            {
                UpdatePairPlayers();
                UpdatePlayerComboboxes();
                UpdateHandicap();
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int stones = _pair.Handicap;
            int.TryParse(txtHandicap.Text, out stones);
            lblHandicap.Text = LangResources.GetStoneStr(stones);
        }

        private void txtKomi_TextChanged(object sender, TextChangedEventArgs e)
        {
            int stones = _pair.Handicap;
            int.TryParse(txtKomi.Text, out stones);
            lblKomi.Text = LangResources.GetPointsStr(stones);
        }

        private bool CheckSaved()
        {
            bool result = true;
            if (!_pairBak.Equals(_pair))
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

        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckSaved())
                return;
            if (OnResult != null)
                OnResult(ReturnResult.Prev, _pair);
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckSaved())
                return;
            if (OnResult != null)
                OnResult(ReturnResult.Next, _pair);
        }

        private void cmbGameResult_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateGameResult();
        }

    }
}
