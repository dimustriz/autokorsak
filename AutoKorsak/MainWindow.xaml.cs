using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Tourtoss.BC;
//using FirstFloor.ModernUI.Windows.Controls;

using Tourtoss.BE;
using System.Net;

namespace AutoKorsak
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TournamentView _tournamentView = new TournamentView();
        private TournamentBC _bc = new TournamentBC();
        private AppUpdateBC _appBc = new AppUpdateBC();

        public static RoutedCommand AutoPairingCmd = new RoutedCommand();
        public static RoutedCommand CleanPairsCmd = new RoutedCommand();
        public static RoutedCommand AddPlayerCmd = new RoutedCommand();
        public static RoutedCommand RemovePreliminaryPlayersCmd = new RoutedCommand();
        public static RoutedCommand FinalizePreliminaryPlayersCmd = new RoutedCommand();
        public static RoutedCommand AddPairCmd = new RoutedCommand();
        public static RoutedCommand ExportWallListCmd = new RoutedCommand();
        public static RoutedCommand ExportWallListRoundRobinCmd = new RoutedCommand();
        public static RoutedCommand ExportWallListForRatingCmd = new RoutedCommand();
        public static RoutedCommand ExportWallListForRatingEngCmd = new RoutedCommand();
        public static RoutedCommand ExportPairingCmd = new RoutedCommand();
        public static RoutedCommand ImportPlayersCmd = new RoutedCommand();
        public static RoutedCommand PropertiesCmd = new RoutedCommand();
        public static RoutedCommand SaveToCmd = new RoutedCommand();
        public static RoutedCommand PrintPairingCmd = new RoutedCommand();
        public static RoutedCommand ExitCmd = new RoutedCommand();

        public static RoutedCommand SetThemeAeroCmd = new RoutedCommand();
        public static RoutedCommand SetThemeLunaCmd = new RoutedCommand();
        public static RoutedCommand SetThemeRoyaleCmd = new RoutedCommand();
        public static RoutedCommand SetThemeClassicCmd = new RoutedCommand();
        public static RoutedCommand SetThemeDefaultCmd = new RoutedCommand();

        public static RoutedCommand SetLocalPlayerDbUsageCmd = new RoutedCommand();
        public static RoutedCommand SetLocalPlayerDbRuCmd = new RoutedCommand();
        public static RoutedCommand SetLocalPlayerDbUaCmd = new RoutedCommand();
        public static RoutedCommand DatabaseImportCmd = new RoutedCommand();

        public static RoutedCommand RefreshWallListCmd = new RoutedCommand();
        public static RoutedCommand AboutCmd = new RoutedCommand();

        public static RoutedCommand NextRoundCmd = new RoutedCommand();
        public static RoutedCommand PrevRoundCmd = new RoutedCommand();

        public static RoutedCommand UseTransliterationCmd = new RoutedCommand();
        public static RoutedCommand GenerateStartNumbersCmd = new RoutedCommand();
        public static RoutedCommand TakeCurrentRoundInAccountCmd = new RoutedCommand();

        private void NextRoundCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            int wlSelection = GetWallListSelection();
            _tournamentView.CurrentRoundNumber++;
            SetWallListSelection(wlSelection);
            InitTourMenu();

            //Update all subscribed windows
            LangResources.LR.UpdateSettings();
        }

        private void PrevRoundCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            int wlSelection = GetWallListSelection();
            _tournamentView.CurrentRoundNumber--;
            SetWallListSelection(wlSelection);
            InitTourMenu();

            //Update all subscribed windows
            LangResources.LR.UpdateSettings();
        }

        private void SetThemeAeroCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            CurrentTheme = Theme.Aero;
        }

        private void SetThemeLunaCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            CurrentTheme = Theme.Luna;
        }

        private void SetThemeClassicCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            CurrentTheme = Theme.Classic;
        }

        private void SetThemeRoyaleCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            CurrentTheme = Theme.Royale;
        }

        private void SetThemeDefaultCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            CurrentTheme = Theme.Default;
        }

        private void UseTransliterationCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            UseTransliteration = !UseTransliteration;
            OnTournamentUpdate(_tournamentView.Tournament);
            _tournamentView.UpdateTournamentProps();
        }

        private void SetLocalPlayerDbUsageCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            LocalPlayerDbUsage = !LocalPlayerDbUsage;
            OnTournamentUpdate(_tournamentView.Tournament);
            _tournamentView.UpdateTournamentProps();
        }

        private void SetLocalPlayerDbRuCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            LocalPlayerDbKind = RtKind.ru;
            OnTournamentUpdate(_tournamentView.Tournament);
            _tournamentView.UpdateTournamentProps();
        }

        private void SetLocalPlayerDbUaCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            LocalPlayerDbKind = RtKind.ua;
            OnTournamentUpdate(_tournamentView.Tournament);
            _tournamentView.UpdateTournamentProps();
        }

        private void AutoPairingCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            new Thread(delegate()
            {
                Thread.CurrentThread.IsBackground = true;

                //int wSelected = GetWallListSelection();

                this.Dispatcher.BeginInvoke(new MethodInvoker(delegate
                {
                    Cursor = Cursors.Wait;
                    try
                    {
                        bool b = _tournamentView.AutoPairing();
                        if (!b)
                            DialogWindow.Show(this, LangResources.LR.ImpossibleToPair, LangResources.LR.Warning, MessageBoxButton.OK, MessageBoxImage.Stop);
                    }
                    finally
                    {
                        Cursor = Cursors.Arrow;
                    }
                }));

                //SetWallListSelection(wSelected);

            }).Start();
        }

        private void AutoPairingCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _tournamentView != null && _tournamentView.IsTournamentEnabled;
        }

        private void TakeCurrentRoundInAccountCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _tournamentView != null && _tournamentView.IsTournamentEnabled;
        }

        private void GenerateStartNumbersCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _tournamentView.GenerateStartNumbers();
        }

        private void TakeCurrentRoundInAccountCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _tournamentView.TakeCurrentRoundInAccount = !_tournamentView.TakeCurrentRoundInAccount;
            mnTakeCurrentRoundInAccount.IsChecked = _tournamentView.TakeCurrentRoundInAccount;
            UpdateTitle();
        }

        private void GenerateStartNumbersCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _tournamentView != null && _tournamentView.IsTournamentEnabled && (_tournamentView.Tournament.TournamentSystemScheveningen || _tournamentView.Tournament.TournamentSystemRound);
        }

        private void CleanPairsCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            int wSelected = GetWallListSelection();
            _tournamentView.CleanPairs();
            SetWallListSelection(wSelected);
            LangResources.LR.UpdateSettings();
        }

        private void RefreshWallListCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _tournamentView != null && _tournamentView.IsTournamentEnabled;
        }

        private void RefreshWallListCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            int wSelected = GetWallListSelection();
            _tournamentView.UpdateWallList();
            SetWallListSelection(wSelected);
        }

        private void RemovePreliminaryPlayersCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _tournamentView != null && _tournamentView.IsTournamentEnabled;
        }

        private void RemovePreliminaryPlayersCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            int wSelected = GetWallListSelection();
            _tournamentView.Players.RemovePreliminaryRegistered();
            _tournamentView.UpdateWallList();
            _tournamentView.UpdatePlayers();
            SetWallListSelection(wSelected);
        }

        private void FinalizePreliminaryPlayersCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _tournamentView != null && _tournamentView.IsTournamentEnabled;
        }

        private void FinalizePreliminaryPlayersCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            int wSelected = GetWallListSelection();
            _tournamentView.Players.FinalizePreliminaryRegistered();
            _tournamentView.UpdateWallList();
            _tournamentView.UpdatePlayers();
            SetWallListSelection(wSelected);
        }

        private void CleanPairsCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _tournamentView != null && _tournamentView.IsTournamentEnabled;
        }

        private void PrintText(string text)
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                string[] arr = text.Split(Convert.ToChar(10));
                var paginator = new PrintPaginator(arr,
                  new Size(printDialog.PrintableAreaWidth,
                    printDialog.PrintableAreaHeight));
                printDialog.PrintDocument(paginator, _tournamentView.StatusBarMessage);
            }
        }

        private void PrintCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            PrintText(_bc.GetTournamentHtmlText(_tournamentView.Tournament, _tournamentView.CurrentRoundNumber, true));
        }

        private void PrintCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _tournamentView != null && _tournamentView.IsTournamentEnabled;
        }

        private void ExitCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        private void ExitCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void PrintPairingCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            PrintText(_bc.GetPairingHtmlText(_tournamentView.Tournament, _tournamentView.CurrentRoundNumber, true));
        }

        private void PrintPairingCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _tournamentView != null && _tournamentView.IsTournamentEnabled;
        }

        private void SaveToCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var fileSaveDlg = new Microsoft.Win32.SaveFileDialog();
            fileSaveDlg.AddExtension = true;
            fileSaveDlg.Filter = "Tournament files (*.xml)|*.xml";
            fileSaveDlg.DefaultExt = "xml";
            fileSaveDlg.FileName = Path.GetFileNameWithoutExtension(_tournamentView.FileName) + "." + fileSaveDlg.DefaultExt;
            if (fileSaveDlg.ShowDialog() == true)
                SaveTournament(_tournamentView.Tournament, fileSaveDlg.FileName);
        }

        private void SaveToCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _tournamentView != null && _tournamentView.IsTournamentEnabled;
        }

        private void AddPlayerCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ExecutePlayerWindow(new Player(_tournamentView.Tournament) { IsCreated = false });
        }

        void NewCmdExecuted(object target, ExecutedRoutedEventArgs e)
        {
            var tournament = _tournamentView.GetNewTournament();
            tournament.IsCreated = true;
            tournament.NumberOfRounds = 5;
            tournament.IsCreated = false;

            ExecuteTournamentPropertiesWindow(tournament);
        }

        private void PropertiesCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ExecuteTournamentPropertiesWindow(_tournamentView.Tournament);
        }

        private void UpdateDownloadedHandler(object sender, EventArgs e)
        {
            var bc = new AppUpdateBC();
            var state = bc.GetState();

            if (state == AppUpdateState.Downloaded)
            {
                this.Dispatcher.BeginInvoke(new MethodInvoker(delegate
                {
                    var r = DialogWindow.Show(this, LangResources.LR.RestartMessage, LangResources.LR.Warning, MessageBoxButton.RestartCancel, MessageBoxImage.Question);
                    if (r == MessageBoxResult.Restart)
                    {
                        bool needRestart;
                        bc.CheckUpdateState(out needRestart);
                        Application.Current.Shutdown();
                    }
                }));
            }
        }

        private int GetWallListSelection()
        {
            var grid = grdWallList;

            var rowView = grid.SelectedItem as DataRowView;
            if (rowView != null)
                return Convert.ToInt32(rowView.Row.ItemArray[0]);
            else
                return -1;
        }

        private List<int> GetWallListSelectionList()
        {
            var grid = grdWallList;

            var rowViewList = grid.SelectedItems as IList;
            if (rowViewList != null)
            {
                var result = new List<int>();
                foreach (var item in rowViewList)
                {
                    var it = item as DataRowView;
                    if (it != null)
                        result.Add(Convert.ToInt32(it.Row.ItemArray[0]));
                }
                return result;
            }
            else
                return null;
        }

        private int GetPlayerSelection()
        {
            var grid = grdPlayers;

            var player = grid.SelectedItem as Player;
            if (player != null)
                return player.Id;
            else
                return -1;
        }

        private List<int> GetPlayerSelectionList()
        {
            var grid = grdPlayers;

            var rowViewList = grid.SelectedItems as IList;
            if (rowViewList != null)
            {
                var result = new List<int>();
                foreach (var item in rowViewList)
                {
                    var it = item as Player;
                    if (it != null)
                        result.Add(it.Id);
                }
                return result;
            }
            else
                return null;
        }

        private List<int> GetPairsSelectionList()
        {
            var grid = grdPairings;

            var rowViewList = grid.SelectedItems as IList;
            if (rowViewList != null)
            {
                var result = new List<int>();
                foreach (var item in rowViewList)
                {
                    var it = item as Pair;
                    if (it != null)
                        result.Add(it.BoardNumber);
                }

                result.Sort();
                return result;
            }
            else
                return null;
        }

        private void SetWallListSelection(int id)
        {
            var grid = grdWallList;

            foreach (var row in grid.Items)
            {
                var rowView = row as DataRowView;
                if (rowView != null)
                {
                    var row_id = Convert.ToInt32(rowView.Row.ItemArray[0]);
                    if (row_id == id)
                    {
                        grid.SelectedItem = row;
                        break;
                    }
                }
            }
        }

        private void SetPlayerListSelection(Player player)
        {
            var grid = grdPlayers;

            foreach (var item in grid.Items)
            {
                var gridPlayer = item as Player;
                if (gridPlayer != null)
                {
                    if (gridPlayer.NameExt == player.NameExt && gridPlayer.Id == player.Id)
                    {
                        grid.SelectedItem = gridPlayer;
                        break;
                    }
                }
            }
        }

        private void SetPairListSelection(Pair pair, bool setFocus = false)
        {
            var grid = grdPairings;

            foreach (var item in grid.Items)
            {
                var gridPair = item as Pair;
                if (gridPair != null)
                {
                    if (gridPair.FirstPlayerId == pair.FirstPlayerId && gridPair.SecondPlayerId == pair.SecondPlayerId)
                    {
                        grid.SelectedItem = gridPair;

                        if (setFocus)
                        {
                            //grid.Focus();
                        }

                        break;
                    }
                }
            }
        }

        private Player GetWallListPrev(Player player)
        {
            var grid = grdWallList;

            int prev_id = -1;

            foreach (var row in grid.Items)
            {
                var rowView = row as DataRowView;
                if (rowView != null)
                {
                    var row_id = Convert.ToInt32(rowView.Row.ItemArray[0]);
                    if (row_id == player.Id)
                        return _tournamentView.Players.Find(item => item != null && item.Id == prev_id);
                    prev_id = row_id;
                }
            }

            return null;
        }

        private Player GetWallListNext(Player player)
        {
            var grid = grdWallList;

            int prev_id = -1;

            foreach (var row in grid.Items)
            {
                var rowView = row as DataRowView;
                if (rowView != null)
                {
                    var row_id = Convert.ToInt32(rowView.Row.ItemArray[0]);
                    if (prev_id == player.Id)
                        return _tournamentView.Players.Find(item => item != null && item.Id == row_id);
                    prev_id = row_id;
                }
            }

            return null;
        }

        private Player GetPlayerListPrev(Player player)
        {
            var grid = grdPlayers;

            Player prev = null;

            foreach (var item in grid.Items)
            {
                var gridPlayer = item as Player;
                if (gridPlayer != null)
                {
                    if (gridPlayer.NameExt == player.NameExt && gridPlayer.Id == player.Id)
                        return prev;
                    prev = gridPlayer;
                }
            }

            return null;
        }

        private Player GetPlayerListNext(Player player)
        {
            var grid = grdPlayers;

            Player prev = null;

            foreach (var item in grid.Items)
            {
                var gridPlayer = item as Player;
                if (gridPlayer != null)
                {
                    if (prev != null && prev.NameExt == player.NameExt && prev.Id == player.Id)
                    {
                        return gridPlayer;
                    }
                    prev = gridPlayer;
                }
            }

            return null;
        }

        private Pair GetPairListPrev(Pair pair)
        {
            if (pair == null)
            {
                return null;
            }

            Pair prev = null;
            IList items = null;

            if (pair.TourId != _tournamentView.CurrentRoundNumber)
            {
                if (_tournamentView.Tournament != null && pair.TourId >= 0 && pair.TourId < _tournamentView.Tournament.Tours.Count)
                items = _tournamentView.Tournament.Tours[pair.TourId].Pairs;
            }
            else
            {
                items = grdPairings.Items;
            }

            if (items != null)
            {
                foreach (var item in items)
                {
                    var gridPair = item as Pair;
                    if (gridPair != null)
                    {
                        if (gridPair.FirstPlayerId == pair.FirstPlayerId && gridPair.SecondPlayerId == pair.SecondPlayerId)
                        {
                            if (prev != null)
                                prev.TourId = pair.TourId;
                            return prev;
                        }
                        prev = gridPair;
                    }
                }
            }

            return prev;
        }

        private Pair GetPairListNext(Pair pair)
        {
            Pair prev = null;
            IList items = null;

            if (pair.TourId != _tournamentView.CurrentRoundNumber)
            {
                if (_tournamentView.Tournament != null && pair.TourId >= 0 && pair.TourId < _tournamentView.Tournament.Tours.Count)
                items = _tournamentView.Tournament.Tours[pair.TourId].Pairs;
            }
            else
            {
                items = grdPairings.Items;
            }

            if (items != null)
            {
                foreach (var item in items)
                {
                    var gridPair = item as Pair;
                    if (gridPair != null)
                    {
                        if (prev != null && prev.FirstPlayerId == pair.FirstPlayerId && prev.SecondPlayerId == pair.SecondPlayerId)
                        {
                            gridPair.TourId = pair.TourId;
                            return gridPair;
                        }
                        prev = gridPair;
                    }
                }
            }
            
            return null;
        }

        bool OnAboutWindowReturn(ReturnResult ret, object value)
        {
            return true;
        }

        void PlayerCancelInplaceEditiong()
        {
            var grid = grdPlayers;

            try
            {
                // Bingo! It works.
                grid.CancelEdit(DataGridEditingUnit.Row);
                grid.Items.Refresh();
                grid.UpdateLayout();
            }
            catch
            {
                // TODO: analyze why it leads to TargetInvocationException/InvalidOperationException when AddNew or EditItem transaction.
            }
        }

        bool OnPlayerWindowReturn(ReturnResult ret, object value)
        {
            if (_tournamentView.Players == null)
                return false;

            var player = value as Player;
            switch (ret)
            {
                case ReturnResult.Ok:
                case ReturnResult.Apply:
                    {
                        PlayerCancelInplaceEditiong();

                        bool createNew = false;
                        if (player != null && !player.IsCreated)
                        {
                            player.IsCreated = true;
                            createNew = true;
                        }
                        _tournamentView.ApplyPlayer(player);
                        _tournamentView.UpdatePlayers();

                        _tournamentView.UpdateTournamentTables();

                        LangResources.LR.UpdateSettings();

                        if (player != null)
                            SetWallListSelection(player.Id);

                        if (createNew)
                            ExecutePlayerWindow(new Player(_tournamentView.Tournament) { IsCreated = false });

                        break;
                    }
                case ReturnResult.Delete:
                    {
                        if (player != null)
                        {
                            var p = _tournamentView.Players.Find(pr => pr != null && pr.FirstName == player.FirstName && pr.Surname == player.Surname);
                            if (p != null)
                            {
                                _tournamentView.Players.Remove(p);
                                _tournamentView.Tournament.RemoveBrokenPairs();
                                _tournamentView.UpdateTournamentTables();
                                _tournamentView.UpdatePlayers();
                            }
                        }
                        break;
                    }
                case ReturnResult.Prev:
                    {
                        if (player != null)
                        {
                            Player pl = null;
                            if (_tournamentView.ActualTab == 0)
                            {
                                pl = GetWallListPrev(player);
                                if (pl != null)
                                    SetWallListSelection(pl.Id);
                                else
                                    SetWallListSelection(player.Id);
                            }
                            else
                            {
                                pl = GetPlayerListPrev(player);
                                if (pl != null)
                                    SetPlayerListSelection(pl);
                                else
                                    SetPlayerListSelection(player);
                            }

                            if (pl != null)
                            {
                                ExecutePlayerWindow(pl.Clone() as Player);
                            }
                        }
                        break;
                    }
                case ReturnResult.Next:
                    {
                        if (player != null)
                        {
                            Player pl = null;
                            if (_tournamentView.ActualTab == 0)
                            {
                                pl = GetWallListNext(player);
                                if (pl != null)
                                    SetWallListSelection(pl.Id);
                                else
                                    SetWallListSelection(player.Id);
                            }
                            else
                            {
                                pl = GetPlayerListNext(player);
                                if (pl != null)
                                    SetPlayerListSelection(pl);
                                else
                                    SetPlayerListSelection(player);
                            }

                            if (pl != null)
                            {
                                ExecutePlayerWindow(pl.Clone() as Player);
                            }
                        }
                        break;
                    }
            }
            return true;
        }

        bool OnPairWindowReturn(ReturnResult ret, object value)
        {
            if (_tournamentView.Pairings == null)
                return false;

            var pair = value as Pair;
            switch (ret)
            {
                case ReturnResult.Ok:
                case ReturnResult.Apply:
                    {
                        bool createNew = false;
                        if (pair != null && !pair.IsCreated)
                        {
                            pair.IsCreated = true;
                            createNew = true;
                        }

                        int wlSelection = GetWallListSelection();

                        if (pair != null && !pair.IsCreated)
                        {
                            pair.IsCreated = true;
                            _tournamentView.ApplyPair(pair);
                            _tournamentView.UpdateTournamentTables();
                        }
                        else
                        {
                            _tournamentView.ApplyPair(pair);
                            _tournamentView.UpdateWallList();

                            _tournamentView.UpdatePairings();

                            LangResources.LR.UpdateSettings();
                        }

                        SetWallListSelection(wlSelection);


                        if (createNew)
                            ExecutePairWindow(new Pair() { TourId = pair.TourId, ForcedPairing = true, IsCreated = false });

                        break;
                    }
                case ReturnResult.Delete:
                    {
                        if (pair != null)
                        {
                            var p = _tournamentView.GetPair(pair.BoardNumber, pair.TourId);
                            if (p != null /*&& p.FirstPlayerId == pair.FirstPlayerId && p.SecondPlayerId == pair.SecondPlayerId*/)
                            {
                                _tournamentView.Tournament.Tours[pair.TourId].Pairs.Remove(p);
                                _tournamentView.UpdateTournamentTables();
                                LangResources.LR.UpdateSettings();
                            }
                        }
                        break;
                    }
                case ReturnResult.Prev:
                    {
                        if (pair != null)
                        {
                            Pair pl = null;

                            pl = GetPairListPrev(pair);
                            if (pl != null)
                                SetPairListSelection(pl);
                            else
                                SetPairListSelection(pair);

                            if (pl != null)
                            {
                                ExecutePairWindow(pl.Clone() as Pair);
                            }
                        }
                        break;
                    }
                case ReturnResult.Next:
                    {
                        if (pair != null)
                        {
                            Pair pl = null;

                            pl = GetPairListNext(pair);
                            if (pl != null)
                                SetPairListSelection(pl);
                            else
                                SetPairListSelection(pair);

                            if (pl != null)
                            {
                                ExecutePairWindow(pl.Clone() as Pair);
                            }
                        }
                        break;
                    }
            }
            return true;
        }

        bool OnTournamentPropertiesWindowReturn(ReturnResult ret, object value)
        {
            var tournament = value as Tournament;
            switch (ret)
            {
                case ReturnResult.Ok:
                    {
                        if (tournament != null && !tournament.IsCreated)
                        {
                            tournament.IsCreated = true;
                            AssignTournament(tournament);
                        }
                        else
                        {
                            int wlSelection = GetWallListSelection();
                            OnTournamentUpdate(tournament);
                            _tournamentView.UpdateTournamentProps();
                            SetWallListSelection(wlSelection);
                        }
                        break;
                    }

            }
            return true;
        }

        private void AboutCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            AboutWindow dlg = App.GetOpenedWindow(typeof(AboutWindow)) as AboutWindow;
            if (dlg == null)
                dlg = new AboutWindow(_tournamentView, OnAboutWindowReturn);
            else
                dlg.SetContext(_tournamentView);
            dlg.ShowWindow();
        }

        private void ExecuteTournamentPropertiesWindow(Tournament tournament)
        {
            TournamentPropertiesWindow dlg = App.GetOpenedWindow(typeof(TournamentPropertiesWindow)) as TournamentPropertiesWindow;
            if (dlg == null)
                dlg = new TournamentPropertiesWindow(tournament, OnTournamentPropertiesWindowReturn);
            else
                dlg.SetContext(tournament);
            dlg.ShowWindow();
        }

        private void ExecutePlayerWindow(Player player)
        {
            _tournamentView.PlayerCanPrev = GetWallListPrev(player) != null;
            _tournamentView.PlayerCanNext = GetWallListNext(player) != null;

            PlayerWindow dlg = App.GetOpenedWindow(typeof(PlayerWindow)) as PlayerWindow;
            if (dlg == null)
                dlg = new PlayerWindow(_tournamentView, player, OnPlayerWindowReturn);
            else
                dlg.SetContext(_tournamentView, player);
            dlg.ShowWindow();
        }


        private void ExecutePairWindow(Pair pair)
        {
            _tournamentView.PairCanPrev = GetPairListPrev(pair) != null;
            _tournamentView.PairCanNext = GetPairListNext(pair) != null;

            PairWindow dlg = App.GetOpenedWindow(typeof(PairWindow)) as PairWindow;
            if (dlg == null)
                dlg = new PairWindow(_tournamentView, pair, OnPairWindowReturn);
            else
                dlg.SetContext(_tournamentView, pair);
            dlg.ShowWindow();
        }

        private void AddPlayerCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _tournamentView != null && _tournamentView.IsTournamentEnabled;
        }

        private void AddPairCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ExecutePairWindow(new Pair() { IsCreated = false, ForcedPairing = true, TourId = _tournamentView.CurrentRoundNumber - 1 });
        }

        private void AddPairCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _tournamentView != null && _tournamentView.IsTournamentEnabled;
        }

        private void UpdateTournamentSettings()
        {
            Tournament.UseTransliteration = this.UseTransliteration;
            Tournament.RSystem = _tournamentView.RSystem;
        }

        private void OnTournamentUpdate(Tournament t)
        {
            //Player list
            //Name
            grdPlayers.Columns[0].Visibility = t != null && _tournamentView.UseTransliteration ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            //InternationalName
            grdPlayers.Columns[1].Visibility = t != null && !_tournamentView.UseTransliteration ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

            //AdjustedStartScores
            grdPlayers.Columns[2].Visibility = t != null && t.UseMacMahonStartScores ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            grdPlayers.Columns[2].IsReadOnly = t != null && !t.UseMacMahonStartScoresManually;
            //StartNumber
            grdPlayers.Columns[3].Visibility = t != null && (t.TournamentSystemRound || t.TournamentSystemScheveningen) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            //Rating
            grdPlayers.Columns[4].IsReadOnly = t != null && t.RatingDeterminesRank;


            //Pairing list
            grdPairings.Columns[6].Visibility = t != null && !t.HandicapDisplayInKomi ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            grdPairings.Columns[7].Visibility = t != null && t.HandicapDisplayInKomi ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

            InitTourMenu();
            InitLangs();

            mnGenerateStartNumbers.Visibility = t != null && (t.TournamentSystemRound || t.TournamentSystemScheveningen) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            mnExportRoundRobin.Visibility = t != null && (t.TournamentSystemRound || t.TournamentSystemScheveningen) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            mnTakeCurrentRoundInAccount.IsChecked = t != null && t.TakeCurrentRoundInAccount;

            UpdateTournamentSettings();
            _bc.OnUpdateTournament(t);

            //Update all subscribed windows
            LangResources.LR.UpdateSettings();
        }

        private void PropertiesCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _tournamentView != null && _tournamentView.Tournament != null;
        }

        private void ExportWallListCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var fileSaveDlg = new Microsoft.Win32.SaveFileDialog();
            fileSaveDlg.AddExtension = true;
            fileSaveDlg.Filter = "Text files (*.txt)|*.txt|HTML files (*.htm)|*.htm|Excel files (*.xlsx)|*.xlsx";
            fileSaveDlg.DefaultExt = "txt";
            fileSaveDlg.FileName = Path.GetFileNameWithoutExtension(_tournamentView.FileName) + "." + fileSaveDlg.DefaultExt;
            if (fileSaveDlg.ShowDialog() == true)
            {
                try
                {
                    _bc.ExportWallList(_tournamentView.Tournament, fileSaveDlg.FileName);
                }
                catch (IOException ex)
                {
                    DialogWindow.Show(this, LangResources.LR.UnexpectedError + "\n\r" + ex.Message,
                        LangResources.LR.Warning, MessageBoxButton.OK, MessageBoxImage.Error, null, 200);
                }
            }
        }

        private void ExportWallListRoundRobinCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var fileSaveDlg = new Microsoft.Win32.SaveFileDialog();
            fileSaveDlg.AddExtension = true;
            fileSaveDlg.Filter = "Text files (*.txt)|*.txt|HTML files (*.htm)|*.htm";
            fileSaveDlg.DefaultExt = "txt";
            fileSaveDlg.FileName = Path.GetFileNameWithoutExtension(_tournamentView.FileName) + "." + fileSaveDlg.DefaultExt;
            if (fileSaveDlg.ShowDialog() == true)
            {
                try
                {
                    _bc.ExportRoundRobinWallList(_tournamentView.Tournament, fileSaveDlg.FileName);
                }
                catch (IOException ex)
                {
                    DialogWindow.Show(this, LangResources.LR.UnexpectedError + "\n\r" + ex.Message,
                        LangResources.LR.Warning, MessageBoxButton.OK, MessageBoxImage.Error, null, 200);
                }
            }
        }

        private void ExportWallListCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _tournamentView != null && _tournamentView.Tournament != null;
        }

        private void ExportWallListRoundRobinCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _tournamentView != null && _tournamentView.Tournament != null;
        }

        private void ExportWallListForRatingCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var fileSaveDlg = new Microsoft.Win32.SaveFileDialog();
            fileSaveDlg.AddExtension = true;
            fileSaveDlg.Filter = "Text files (*.txt)|*.txt";
            fileSaveDlg.DefaultExt = "txt";
            fileSaveDlg.FileName = Path.GetFileNameWithoutExtension(_tournamentView.FileName) + "." + fileSaveDlg.DefaultExt;
            if (fileSaveDlg.ShowDialog() == true)
            {
                _bc.ExportWallListForRating(_tournamentView.Tournament, fileSaveDlg.FileName, true);
            }
        }

        private void ExportWallListForRatingEngCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var fileSaveDlg = new Microsoft.Win32.SaveFileDialog();
            fileSaveDlg.AddExtension = true;
            fileSaveDlg.Filter = "Text files (*.txt)|*.txt";
            fileSaveDlg.DefaultExt = "txt";
            fileSaveDlg.FileName = Path.GetFileNameWithoutExtension(_tournamentView.FileName) + "." + fileSaveDlg.DefaultExt;
            if (fileSaveDlg.ShowDialog() == true)
            {
                _bc.ExportWallListForRating(_tournamentView.Tournament, fileSaveDlg.FileName, false);
            }
        }

        private void ExportWallListForRatingCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _tournamentView != null && _tournamentView.Tournament != null;
        }

        private void ExportPairingCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var fileSaveDlg = new Microsoft.Win32.SaveFileDialog();
            fileSaveDlg.AddExtension = true;
            fileSaveDlg.Filter = "Text files (*.txt)|*.txt|HTML files (*.htm)|*.htm|Excel files (*.xlsx)|*.xlsx";
            fileSaveDlg.DefaultExt = "txt";
            fileSaveDlg.FileName = Path.GetFileNameWithoutExtension(_tournamentView.FileName) + "." + fileSaveDlg.DefaultExt;
            if (fileSaveDlg.ShowDialog() == true)
            {
                try
                {
                    _bc.ExportPairing(_tournamentView.Tournament, fileSaveDlg.FileName);
                }
                catch (IOException ex)
                {
                    DialogWindow.Show(this, LangResources.LR.UnexpectedError + "\n\r" + ex.Message,
                        LangResources.LR.Warning, MessageBoxButton.OK, MessageBoxImage.Error, null, 200);
                }
            }
        }

        private void ExportPairingCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _tournamentView != null && _tournamentView.Tournament != null;
        }

        private void ImportPlayersCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var fileOpenDlg = new Microsoft.Win32.OpenFileDialog();
            fileOpenDlg.Filter = "Tournament files (*.xml)|*.xml|MacMahon 2.* files (*.tur)|*.tur|Plain tournament tables (*.txt)|*.txt|Excel tournament tables (*.xlsx)|*.xlsx|All files (*.*)|*.*";
            if (fileOpenDlg.ShowDialog() == true)
            {
                ImportPlayers(fileOpenDlg.FileName);
            };
        }

        private void ImportPlayersCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _tournamentView != null && _tournamentView.Tournament != null;
        }

        private void DatabaseImportCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ImportRating(LocalPlayerDbKind);
        }

        private void InitPalette()
        {
            Brush body = ValueFromStyleExtension.BodyBrush;
            Brush border = ValueFromStyleExtension.BorderBrush;

            grdMain.Background = body;
        }

        private void InitLangs()
        {
            foreach (var item in grdWallList.Columns)
            {
                string s = _bc.GetColumnNameByKey(item.SortMemberPath);
                if (s != null)
                    item.Header = s;
            }

            foreach (var item in grdPairings.Columns)
            {
                switch (item.HeaderStringFormat)
                {
                    case "Black":
                        if (_tournamentView.Tournament != null && _tournamentView.Tournament.FirstMoveWhite)
                            item.Header = Translator.Translate("Common", "White");
                        else
                            item.Header = Translator.Translate("Common", "Black");
                        break;
                    case "White":
                        if (_tournamentView.Tournament != null && _tournamentView.Tournament.FirstMoveWhite)
                            item.Header = Translator.Translate("Common", "Black");
                        else
                            item.Header = Translator.Translate("Common", "White");
                        break;
                    default:
                        {
                            item.Header = Translator.Translate("Common", item.HeaderStringFormat);
                            break;
                        }
                }
            }
            foreach (var item in grdPlayers.Columns)
            {
                switch (item.HeaderStringFormat)
                {
                    case "InternationalName":
                        item.Header = Translator.Translate("Common", "Name");
                        break;
                    case "StartNumber":
                        item.Header = Translator.Translate("Common", "Start number");
                        break;
                    default:
                        {
                            item.Header = Translator.Translate("Common", item.HeaderStringFormat);
                            break;
                        }
                }
            }

            UpdateTitle();

            //Update DatabaseSource labels
            LocalPlayerDbKind = LocalPlayerDbKind;

            if (_tournamentView.Tournament != null && (_tournamentView.Tournament.TournamentSystemRound || _tournamentView.Tournament.TournamentSystemScheveningen))
            {
                mnAutoPairing.Header = LangResources.LR.AutoPairing + " " + LangResources.LR.AllRounds;
                mnCleanPairs.Header = LangResources.LR.CleanPairs + " " + LangResources.LR.AllRounds;
            }
            else
            {
                mnAutoPairing.Header = LangResources.LR.AutoPairing;
                mnCleanPairs.Header = LangResources.LR.CleanPairs;
            }
        }

        private void CleanThemesChecking()
        {
            mnThemeAero.IsChecked = false;
            mnThemeLuna.IsChecked = false;
            mnThemeClassic.IsChecked = false;
            mnThemeAero.IsChecked = false;
            mnThemeRoyale.IsChecked = false;
        }

        private Theme _currentTheme = Theme.Aero;

        public Theme CurrentTheme
        {
            get { return _currentTheme; }
            set
            {
                _currentTheme = value;

                CleanThemesChecking();

                switch (_currentTheme)
                {
                    case Theme.Aero: mnThemeAero.IsChecked = true; break;
                    case Theme.Classic: mnThemeClassic.IsChecked = true; break;
                    case Theme.Royale: mnThemeRoyale.IsChecked = true; break;
                    case Theme.Luna: mnThemeLuna.IsChecked = true; break;
                }

                var app = App.Current as App;
                app.ChangeTheme(_currentTheme);
            }
        }

        private void CleanLocalPlayerDbChecking()
        {
            mnDatabaseUkrainian.IsChecked = false;
            mnDatabaseRussian.IsChecked = false;
        }

        public RtKind LocalPlayerDbKind
        {
            get { return _tournamentView.LocalPlayerDbKind; }
            set
            {
                _tournamentView.LocalPlayerDbKind = value;

                CleanLocalPlayerDbChecking();

                switch (_tournamentView.LocalPlayerDbKind)
                {
                    case RtKind.ua: mnDatabaseUkrainian.IsChecked = true; break;
                    case RtKind.ru: mnDatabaseRussian.IsChecked = true; break;
                }

                mnDatabaseRussian.Header = _tournamentView.RListRuUpdated;
                mnDatabaseUkrainian.Header = _tournamentView.RListUaUpdated;
            }
        }

        public bool IsTournamentEnabled
        {
            get { return _tournamentView.IsTournamentEnabled; }
        }

        public bool LocalPlayerDbUsage
        {
            get { return _tournamentView.LocalPlayerDbUsage; }
            set
            {
                _tournamentView.LocalPlayerDbUsage = value;

                mnDatabaseUsing.IsChecked = value;
            }
        }

        public bool UseTransliteration
        {
            get { return _tournamentView.UseTransliteration; }
            set
            {
                _tournamentView.UseTransliteration = value;

                mnUseTransliteration.IsChecked = value;
            }
        }

        public bool TakeCurrentRoundInAccount
        {
            get { return _tournamentView.TakeCurrentRoundInAccount; }
            set
            {
                _tournamentView.TakeCurrentRoundInAccount = value;

                mnTakeCurrentRoundInAccount.IsChecked = value;
            }
        }

        private string _currentLanguage = "en";

        public string CurrentLanguage
        {
            get { return _currentLanguage; }
            set
            {
                _currentLanguage = value;
                new Translator(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ak_"), _currentLanguage);
                LangResources.LR.Update();
                foreach (MenuItem item in mnLangs.Items)
                    item.IsChecked = item.Name == _currentLanguage;
                InitLangs();
                if (_tournamentView != null)
                {
                    if (_tournamentView.Players != null)
                        _tournamentView.Players.RefreshLang();
                    if (_tournamentView.Pairings != null)
                        _tournamentView.Pairings.RefreshLang();

                    _tournamentView.UpdateWallList();
                    _tournamentView.UpdatePlayers();
                }
            }
        }


        private void LangItem_Click(object sender, RoutedEventArgs e)
        {
            var mnItem = sender as MenuItem;
            CurrentLanguage = mnItem.Name;
        }

        private void TourItem_Click(object sender, RoutedEventArgs e)
        {
            var mnItem = sender as MenuItem;
            if (mnItem != null)
            {
                int tourNumber = Convert.ToInt16(mnItem.Name.Substring(1));
                int wlSelection = GetWallListSelection();
                _tournamentView.CurrentRoundNumber = tourNumber;
                SetWallListSelection(wlSelection);
                InitTourMenu();

                //Update all subscribed windows
                LangResources.LR.UpdateSettings();
            }
        }

        private void RecentFileItem_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckChanged())
                return;

            var mnItem = sender as MenuItem;
            if (mnItem != null)
            {
                int itemNumber = Convert.ToInt16(mnItem.Name.Substring(1));
                string fileName = _tournamentView.RecentTournaments.List[itemNumber].FileName;
                LoadTournament(fileName);
            }
        }

        private MenuItem AddLangMenu(string lang)
        {
            string langName = lang;

            var ci = CultureInfo.GetCultureInfo(lang.ToLower());
            if (ci != null)
                langName = ci.NativeName;

            var mn = new MenuItem();
            mn.Header = langName;
            mn.Name = lang;
            mn.IsCheckable = true;
            mn.Click += LangItem_Click;

            return mn;
        }

        private MenuItem AddTourMenu(int tourNumber, string tourText)
        {
            var mn = new MenuItem();
            mn.Header = tourText;
            mn.Name = "_" + tourNumber.ToString();
            mn.IsCheckable = true;
            mn.IsChecked = _tournamentView.CurrentRoundNumber == tourNumber;
            mn.Click += TourItem_Click;

            return mn;
        }

        private MenuItem AddRecentMenu(int itemNumber)
        {
            var mn = new MenuItem();
            mn.Header = _tournamentView.RecentTournaments.List[itemNumber].DisplayName.Replace("_", "__");
            mn.Name = "_" + itemNumber.ToString();
            mn.Click += RecentFileItem_Click;

            return mn;
        }

        private void InitLangMenu()
        {
            var langs = Translator.GetLangList(AppDomain.CurrentDomain.BaseDirectory);
            bool en = false;
            foreach (string item in langs)
            {
                string lang = item.Substring(item.Length - 6, 2);

                if (lang.Equals("en", StringComparison.OrdinalIgnoreCase))
                    en = true;

                mnLangs.Items.Add(AddLangMenu(lang));
            }

            if (!en)
                mnLangs.Items.Insert(0, AddLangMenu("en"));

        }

        private void InitTourMenu()
        {
            mnTours.Items.Clear();

            if (_tournamentView.Tournament == null)
                return;

            var tours = _tournamentView.Tournament.TourIDs;

            for (int i = 0; i < tours.Count; i++)
                mnTours.Items.Add(AddTourMenu(i + 1, tours[i]));

            UpdateTitle();
        }

        private void InitRecentMenu(bool saveMenu)
        {
            mnRecent.Items.Clear();

            var list = _tournamentView.RecentTournaments.List;
            for (int i = 0; i < list.Count; i++)
                mnRecent.Items.Add(AddRecentMenu(i));

            if (saveMenu)
                _tournamentView.RecentTournaments.Save();
        }

        public MainWindow()
        {
            InitializeComponent();
            InitLangMenu();

            AppUpdateBC.OnUpdateDownloaded += UpdateDownloadedHandler;

            _tournamentView.PlayersUpdateBegin += new EventHandler(_tournamentView_PlayersUpdateBegin);
            _tournamentView.PlayersUpdateEnd += new EventHandler(_tournamentView_PlayersUpdateEnd);
            _tournamentView.PairingUpdateBegin += new EventHandler(_tournamentView_PairingUpdateBegin);
            _tournamentView.PairingUpdateEnd += new EventHandler(_tournamentView_PairingUpdateEnd);
            _tournamentView.WallListUpdateBegin += new EventHandler(_tournamentView_WallListUpdateBegin);
            _tournamentView.WallListUpdateEnd += new EventHandler(_tournamentView_WallListUpdateEnd);

            InitCommands();

            CurrentTheme = WindowHelper.LoadTheme();
            CurrentLanguage = WindowHelper.LoadLanguage();

            LocalPlayerDbUsage = WindowHelper.LoadPlayerDbUsage();
            LocalPlayerDbKind = WindowHelper.LoadPlayerDbKind();
            UseTransliteration = WindowHelper.LoadUseTransliteration();

            _tournamentView.RecentTournaments.Load();
            InitRecentMenu(false);

            InitPalette();
            AssignTournament(null);

            grdPlayers.BeginningEdit += new EventHandler<DataGridBeginningEditEventArgs>(grdPlayers_BeginningEdit);
            grdPlayers.CellEditEnding += new EventHandler<DataGridCellEditEndingEventArgs>(grdPlayers_CellEditEnding);
            grdPlayers.Sorting += new DataGridSortingEventHandler(grdPlayers_Sorting);

            grdPairings.BeginningEdit += new EventHandler<DataGridBeginningEditEventArgs>(grdPairs_BeginningEdit);
            grdPairings.CellEditEnding += new EventHandler<DataGridCellEditEndingEventArgs>(grdPairing_CellEditEnding);

            AddPlayerCmd.InputGestures.Add(new KeyGesture(Key.R, ModifierKeys.Control));
            AddPairCmd.InputGestures.Add(new KeyGesture(Key.G, ModifierKeys.Control));
        }

        private void UpdateTitle()
        {
            this.Title = _tournamentView.TitleMessage;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            Tournament.Changed = true;
            if (null != this.PropertyChanged)
                try
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
                catch (Exception) { }
        }

        private void AssignTournament(Tournament t)
        {
            _tournamentView.WallListSortColumn = string.Empty;
            _tournamentView.PlayersSortColumn = string.Empty; 
            _tournamentView.PairingSortColumn = string.Empty;

            _tournamentView.Tournament = t;
            DataContext = _tournamentView;
            dgResultKindCombo.ItemsSource = _tournamentView.ResultKinds;
            dgCountryCombo.ItemsSource = _tournamentView.Countries;

            OnTournamentUpdate(t);

            _tournamentView.UpdateStatusBar();

            OnPropertyChanged("IsTournamentEnabled");
            Tournament.Changed = false;
        }

        public class PlayerListSort : IComparer
        {
            public delegate int SortDelegate(Player arg1, Player arg2);
            SortDelegate _compare;
            public PlayerListSort(ListSortDirection direction, DataGridColumn column)
            {
                int dir = (direction == ListSortDirection.Ascending) ? 1 : -1;
                switch ((string)column.SortMemberPath)
                {
                    case "RankExt":
                        _compare = (x, y) => x == null || y == null ? 0 : PlayerInfo.GetRatingByRank(x.RankExt).CompareTo(PlayerInfo.GetRatingByRank(y.RankExt)) * dir;
                        break;
                }
            }
            int IComparer.Compare(object X, object Y)
            {
                return _compare((Player)X, (Player)Y);
            }
        }

        void grdPlayers_Sorting(object sender, DataGridSortingEventArgs e)
        {
            if (e.Handled) return;

            if (e.Column.SortMemberPath == "RankExt")
            {
                e.Handled = true;

                ListSortDirection direction =
                (e.Column.SortDirection != ListSortDirection.Ascending) ?
                    ListSortDirection.Ascending :
                    ListSortDirection.Descending;
                e.Column.SortDirection = direction;
                ListCollectionView lcv = CollectionViewSource.GetDefaultView(grdPlayers.ItemsSource) as ListCollectionView;
                PlayerListSort sortLogic = new PlayerListSort(direction, e.Column);
                if (lcv != null)
                    lcv.CustomSort = sortLogic;
            }

        }

        void grdPairs_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            _pairIsEditing = true;
        }

        void grdPairing_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            _tournamentView.UpdateWallList();
            _tournamentView.UpdatePairings();
            _pairIsEditing = false;
        }

        void grdPlayers_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            _playerIsEditing = true;
        }

        void grdPlayers_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            _tournamentView.UpdateTournamentTables();
            _playerIsEditing = false;
        }

        void CmdExecuted(object target, ExecutedRoutedEventArgs e)
        {
            String command, targetobj;
            command = ((RoutedCommand)e.Command).Name;
            targetobj = ((FrameworkElement)target).Name;

            MessageBox.Show("The " + command + " command has been invoked on target object " + targetobj);
        }

        private bool SaveTournament(Tournament tournament, string fileName)
        {
            string ext = Path.GetExtension(fileName).ToLower();
            if (ext == ".tur" || ext == ".txt" || ext == ".xlsx")
            {
                DialogWindow.Show(this, LangResources.LR.ImpossibleExtention, LangResources.LR.Warning, MessageBoxButton.OK, MessageBoxImage.Stop);
                return false;
            }

            tournament.VersionSaved = TournamentView.AppName;
            _bc.Save(tournament, fileName);
            _tournamentView.RecentTournaments.Add(fileName);
            InitRecentMenu(true);
            _tournamentView.UpdateStatusBar();
            Tournament.Changed = false;
            return true;
        }

        private bool SaveTournament()
        {
            bool result = false;
            if (!string.IsNullOrEmpty(_tournamentView.FileName))
                result = SaveTournament(_tournamentView.Tournament, _tournamentView.FileName);

            if (!result)
            {
                var fileSaveDlg = new Microsoft.Win32.SaveFileDialog();
                fileSaveDlg.AddExtension = true;
                fileSaveDlg.Filter = "Tournament files (*.xml)|*.xml";
                fileSaveDlg.DefaultExt = "xml";
                fileSaveDlg.FileName = Path.GetFileNameWithoutExtension(_tournamentView.FileName) + "." + fileSaveDlg.DefaultExt;
                if (fileSaveDlg.ShowDialog() == true)
                    result = SaveTournament(_tournamentView.Tournament, fileSaveDlg.FileName);

            }
            return result;
        }

        private void SaveCmdExecuted(object target, ExecutedRoutedEventArgs e)
        {
            SaveTournament();
        }

        private void LoadTournament(string fileName)
        {
            new Thread(delegate()
            {
                Thread.CurrentThread.IsBackground = true;

                Tournament tournament;

                this.Dispatcher.BeginInvoke(new MethodInvoker(delegate
                {
                    Cursor = Cursors.Wait;
                    try
                    {
                        string ext = Path.GetExtension(fileName).ToLower();
                        if (ext == ".tur" || ext == ".txt" || ext == ".xlsx")
                            tournament = _bc.Import(fileName);
                        else
                            tournament = _bc.Load(fileName);

                        AssignTournament(tournament);
                        _tournamentView.RecentTournaments.Add(fileName);
                        InitRecentMenu(true);
                    }
                    finally
                    {
                        Cursor = Cursors.Arrow;
                    }
                }));
            }).Start();
        }


        private readonly object _initLock = new object();
        private System.Timers.Timer _executionTimer;

        private void ExecutionTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            StopExecutionTimer();
            CheckNewVersion();
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

        private void CheckNewVersion()
        {
            new Thread(delegate()
            {
                Thread.CurrentThread.IsBackground = true;

                var sysConfig = _tournamentView.SysConfig;

                if (sysConfig != null)
                {
                    string curVer = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    string newVer = sysConfig.Version;
                    if (SysConfig.CompareBuilds(newVer, curVer) > 0)
                    {
                        var bc = new SysConfigBC();
                        string s = bc.GetNotesText(sysConfig, curVer, _currentLanguage);

                        this.Dispatcher.BeginInvoke(new MethodInvoker(delegate
                        {
                            var r = DialogWindow.Show(this, s, LangResources.LR.Warning, MessageBoxButton.DownloadCancel, MessageBoxImage.Question, height: 300);
                            switch (r)
                            {
                                case MessageBoxResult.Download:
                                    _appBc.StartDownloadNewBuild();
                                    break;
                            }
                        }));
                    }
                }

            }).Start();
        }

        private void ImportPlayers(string fileName)
        {
            new Thread(delegate()
            {
                Thread.CurrentThread.IsBackground = true;

                Tournament tournament;

                this.Dispatcher.BeginInvoke(new MethodInvoker(delegate
                {
                    string ext = Path.GetExtension(fileName).ToLower();
                    if (ext == ".tur" || ext == ".txt" || ext == ".xlsx")
                        tournament = _bc.Import(fileName);
                    else
                        tournament = _bc.Load(fileName);

                    if (tournament != null)
                    {
                        var r = DialogWindow.Show(this, LangResources.LR.CalculateNewRatingForPlayers, LangResources.LR.Import, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                        switch (r)
                        {
                            case MessageBoxResult.Yes:
                                {
                                    _bc.CalculateRatingInfo(tournament, tournament.Tours.Count);
                                    foreach (var player in tournament.Players)
                                        player.Rating = (int)Math.Round(player.GetNewRating(tournament, tournament.Tours.Count - 1), 0);
                                    break;
                                }
                            case MessageBoxResult.No:
                                break;
                            default:
                                return;
                        }

                        foreach (Player item in tournament.Players)
                        {
                            item.PreliminaryRegistration = true;
                            _tournamentView.Tournament.Players.Add(item);

                            if (!string.IsNullOrEmpty(item.Club))
                            {
                                var clubs = tournament.Countries.GetClubs(item.Country);
                                if (clubs != null)
                                {
                                    var club = clubs.Find(cl => cl != null && cl.Name == item.Club);
                                    if (club != null)
                                    {
                                        if (!_bc.CheckClub(_tournamentView.Tournament, item.Country, club.EGDName))
                                            _tournamentView.UpdateClub(item.Country, club);
                                        item.Club = club.Name;
                                    }
                                }
                            }
                        }
                    }

                    int wSelected = GetWallListSelection();
                    _tournamentView.UpdatePlayers();
                    _tournamentView.UpdateWallList();
                    SetWallListSelection(wSelected);
                }));
            }).Start();
        }

        void ImportRating(RtKind kind)
        {
            new Thread(delegate()
            {
                Thread.CurrentThread.IsBackground = true;

                this.Dispatcher.BeginInvoke(new MethodInvoker(delegate
                {
                    Cursor = Cursors.Wait;
                    try
                    {
                        var bcs = new RatingSystemBC();
                        var bcl = new RatingListBC();

                        var rs = bcs.ImportRatingSystem(kind);
                        if (rs != null && rs.Persons.Count > 0)
                        {

                            bcs.Save(rs);
                            _tournamentView.RSystem = rs;
                        }

                        var rl = bcl.ImportRatingList(kind);
                        if (rl != null && rl.Items.Count > 0)
                        {
                            bcl.SaveRatingList(rl);
                            _tournamentView.RList = rl;
                        }

                        bool changed = false;

                        if (rl != null)
                        {
                            switch (kind)
                            {
                                case RtKind.ru:
                                    _tournamentView.CfgInfo.RatingRuDate = rl.Date; break;
                                case RtKind.ua:
                                    _tournamentView.CfgInfo.RatingUaDate = rl.Date; break;
                            }

                            changed = true;
                        }

                        if (rs != null)
                        {
                            switch (kind)
                            {
                                case RtKind.ru:
                                    _tournamentView.CfgInfo.RsRuDate = rs.Date; break;
                                case RtKind.ua:
                                    _tournamentView.CfgInfo.RsUaDate = rs.Date; break;
                            }

                            changed = true;
                        }

                        if (changed)
                        {
                            bcl.SaveConfigInfo(_tournamentView.CfgInfo);

                            rs = bcs.MergeWithRatingList(kind);

                            if (rs != null)
                            {
                                string countryCode = kind.ToString();
                                foreach (var item in rs.Persons)
                                {
                                    switch (kind)
                                    {
                                        case RtKind.ru:
                                            if (string.IsNullOrEmpty(item.FirstNameEn))
                                                item.FirstNameEn = Translit.Transliterate(countryCode, item.FirstName);
                                            if (string.IsNullOrEmpty(item.LastNameEn))
                                                item.LastNameEn = Translit.Transliterate(countryCode, item.LastName);
                                            break;
                                        case RtKind.ua:
                                            if (string.IsNullOrEmpty(item.FirstNameEn))
                                                item.FirstNameEn = Translit.Transliterate(countryCode, item.FirstNameUa);
                                            if (string.IsNullOrEmpty(item.LastNameEn))
                                                item.LastNameEn = Translit.Transliterate(countryCode, item.LastNameUa);
                                            break;
                                    }
                                }
                                foreach (var item in rs.Clubs)
                                {
                                    switch (kind)
                                    {
                                        case RtKind.ru:
                                            if (string.IsNullOrEmpty(item.NameEn))
                                                item.NameEn = Translit.Transliterate(countryCode, item.Name);
                                            break;
                                        case RtKind.ua:
                                            if (string.IsNullOrEmpty(item.NameEn))
                                                item.NameEn = Translit.Transliterate(countryCode, item.NameUa);
                                            break;
                                    }
                                }
                                bcs.Save(rs);
                                _tournamentView.RSystem = rs;
                                _tournamentView.UpdateTournamentTables();
                                _tournamentView.UpdatePlayers();
                            }

                            //Update menu captions
                            LocalPlayerDbKind = LocalPlayerDbKind;
                        }
                        else
                            DialogWindow.Show(this, LangResources.LR.ImpossibleToSynchronize, LangResources.LR.Warning, MessageBoxButton.OK, MessageBoxImage.Stop);
                    }
                    catch (WebException ex)
                    {
                        throw new CustomHandledException(string.Format(LangResources.LR.ResourceIsUnavailable, ex.Response.ResponseUri.OriginalString, ex.Message));
                    }
                    finally
                    {
                        Cursor = Cursors.Arrow;
                    }
                }));
            }).Start();
        }

        void OpenCmdExecuted(object target, ExecutedRoutedEventArgs e)
        {
            var fileOpenDlg = new Microsoft.Win32.OpenFileDialog();
            fileOpenDlg.Filter = "Tournament files (*.xml)|*.xml|MacMahon 2.* files (*.tur)|*.tur|Plain tournament tables (*.txt)|*.txt|Excel tournament tables (*.xlsx)|*.xlsx|All files (*.*)|*.*";
            if (fileOpenDlg.ShowDialog() == true)
            {
                LoadTournament(fileOpenDlg.FileName);
            };
        }

        private bool CheckChanged()
        {
            if (Tournament.Changed && _tournamentView.Tournament != null)
            {
                var r = DialogWindow.Show(this, LangResources.LR.SaveChanges, LangResources.LR.Warning, MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                switch (r)
                {
                    case MessageBoxResult.None:
                    case MessageBoxResult.Cancel:
                        return false;
                    case MessageBoxResult.Yes:
                        if (!SaveTournament())
                            return false;
                        break;
                }

            }

            return true;
        }

        void CloseCmdExecuted(object target, ExecutedRoutedEventArgs e)
        {
            if (!CheckChanged())
                return;

            AssignTournament(null);
        }

        void NewCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        void OpenCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        void SaveCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _tournamentView.IsTournamentEnabled;
        }

        void CloseCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _tournamentView.IsTournamentEnabled;
        }

        private void InitCommands()
        {
            // Creating CommandBinding and attaching an Executed and CanExecute handler

            CommandBinding NewCmdBinding = new CommandBinding(
                ApplicationCommands.New,
                NewCmdExecuted,
                NewCmdCanExecute);
            this.CommandBindings.Add(NewCmdBinding);

            CommandBinding OpenCmdBinding = new CommandBinding(
                ApplicationCommands.Open,
                CmdExecuted,
                OpenCmdCanExecute);
            this.CommandBindings.Add(OpenCmdBinding);

            CommandBinding SaveCmdBinding = new CommandBinding(
                ApplicationCommands.Open,
                CmdExecuted,
                SaveCmdCanExecute);
            this.CommandBindings.Add(SaveCmdBinding);

            CommandBinding CloseCmdBinding = new CommandBinding(
                ApplicationCommands.Close,
                CmdExecuted,
                CloseCmdCanExecute);
            this.CommandBindings.Add(CloseCmdBinding);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            App.SetFocusToWindow(new WindowInteropHelper(this).Handle);
        }

        private void grdPlayers_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_tournamentView.Tournament == null)
                return;

            var grid = sender as DataGrid;

            if (grid.CurrentCell == null || grid.CurrentCell.Column == null || grid.CurrentCell.Column.SortMemberPath == "Country")
                return;

            if (!App.GridCheckDblClick(e))
                return;

            var player = grid.SelectedItem as Player;

            if (player == null)
            {
                player = new Player();
                player.IsCreated = false;
            }

            player.RootTournament = _tournamentView.Tournament;
            _tournamentView.Players.FillIDs();

            ExecutePlayerWindow(player.Clone() as Player);

        }

        private void grdPairing_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_tournamentView.Tournament == null)
                return;

            var grid = sender as DataGrid;
            if (grid.CurrentCell == null || grid.CurrentCell.Column == null || grid.CurrentCell.Column.SortMemberPath == "Result")
                return;

            if (!App.GridCheckDblClick(e))
                return;

            var pair = grid.SelectedItem as Pair;
            Pair editPair = null;
            if (pair == null)
            {
                pair = new Pair();
                pair.IsCreated = false;
                pair.ForcedPairing = true;
                editPair = pair;
            }
            else
                editPair = pair.Clone() as Pair;

            editPair.TourId = _tournamentView.CurrentRoundNumber - 1;

            ExecutePairWindow(editPair);
        }

        bool needRestart;

        private void tour_Initialized(object sender, EventArgs e)
        {
            this.LoadSettings();

            // Perform restarting if needed
            _appBc.CheckUpdateState(out needRestart);
            if (needRestart)
            {
                Application.Current.Shutdown();
            }

            this.StartExecutionTimer();

            //RatingSystemBC rbc = new RatingSystemBC();
            //rbc.ImportFromRatingLists(RtKind.ua);
            //rbc.MergeWithRatingList(RtKind.ua);
            /*
            RatingListBC rbl = new RatingListBC();
            rbl.CombineDifferentLangs();
            */
        }

        private void tour_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!CheckChanged())
            {
                e.Cancel = true;
                return;
            }

            if (!needRestart)
            {
                WindowHelper.SaveLanguage(CurrentLanguage);

                WindowHelper.SaveTheme(CurrentTheme);
                WindowHelper.SavePlayerDbKind(LocalPlayerDbKind);
                WindowHelper.SavePlayerDbUsage(LocalPlayerDbUsage);
                WindowHelper.SaveUseTransliteration(UseTransliteration);
                this.SaveSettings();
            }

            Application.Current.Shutdown();
        }

        #region WallList operation

        private void grdWallList_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            e.Column.Header = Translator.Translate("Common", e.PropertyName);

            switch (e.PropertyName)
            {
                case "ID":
                case "PL":
                case "SB":
                case "PR":
                case "RC":
                case "RN":
                case "NL":
                    e.Column.Visibility = System.Windows.Visibility.Hidden;
                    break;

                case "Place":
                    {
                        e.Column.SortMemberPath = "PL";
                        e.Column.CellStyle = Resources["CenterCellStyle"] as Style;
                        break;
                    }
                case "Name":
                    break;
                case "SuperBarMember":
                    {
                        if (_tournamentView.Tournament.UpperMacMahonBar || _tournamentView.Tournament.LowerMacMahonBar)
                        {
                            DgTemplateColumn col = new DgTemplateColumn();
                            col.ColumnName = e.PropertyName;  // so it knows from which column to get MyData
                            col.CellTemplate = FindResource("SuperBarColumnCellTemplate") as DataTemplate;
                            e.Column = col;
                            e.Column.Header = string.Empty;
                            e.Column.Width = 4;
                        }
                        else
                            e.Column.Visibility = System.Windows.Visibility.Hidden;

                        break;
                    }

                case "Rank":
                    {
                        e.Column.SortMemberPath = "RC";
                        e.Column.CellStyle = Resources["CenterCellStyle"] as Style;
                        break;
                    }

                case "NR":
                    {

                        DgTemplateColumn col = new DgTemplateColumn();
                        col.ColumnName = e.PropertyName;
                        col.CellTemplate = FindResource("NewRatingColumnCellTemplate") as DataTemplate;
                        e.Column = col;

                        e.Column.SortMemberPath = "RN";
                        e.Column.Header = Translator.Translate("Common", "Rating₂");

                        e.Column.CellStyle = Resources["RightCellStyle"] as Style;
                        break;
                    }

                case "Group":
                case "Points":
                case "Rating":
                case "Score":
                case "ScoreX":
                case "SOS":
                case "SOSOS":
                case "SODOS":
                case "SOUD":
                case "SORP":
                    e.Column.CellStyle = Resources["RightCellStyle"] as Style;
                    break;

                case "Country":
                case "City":
                case "Club":
                case "Team":
                case "Coach":
                case "Grade":
                    e.Column.CellStyle = Resources["CenterCellStyle"] as Style;
                    break;

                default:
                    {
                        DgTemplateColumn col = new DgTemplateColumn();
                        col.ColumnName = e.PropertyName;
                        col.CellTemplate = FindResource("ResultColumnCellTemplate") as DataTemplate;
                        e.Column = col;
                        e.Column.Header = e.PropertyName;

                        col.HeaderStyle = Resources["CenterColumnHeaderStyle"] as Style;

                        e.Column.CellStyle = Resources["CenterCellStyle"] as Style;
                        break;
                    }
            }

            //if (_tournamentView.WallListSortColumn == e.Column.SortMemberPath)
            //{
            //    e.Column.SortDirection = _tournamentView.WallListSortDirection;
            //}
        }

        private void grdWallList_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            var row = (e.Row.Item as System.Data.DataRowView).Row;
            var id = row.ItemArray[0];
            var pl = row.ItemArray[1];
            var sb = row.ItemArray[2];
            var pr = row.ItemArray[3];
            var nl = row.ItemArray[6];
            if (id == null)
                return;
            bool superBarMember = Convert.ToBoolean(sb);
            bool prelimintaryRegistered = Convert.ToBoolean(pr);
            bool nonListed = Convert.ToBoolean(nl);
            //Player player = _tournamentView.Tournament.Players.GetById(Convert.ToInt16(id));

            if (Convert.ToInt32(id) < 0) //team
            {
                e.Row.Background = new SolidColorBrush(Colors.NavajoWhite);
                return;
            }

            if (_tournamentView.UseTransliteration && _tournamentView.LocalPlayerDbUsage && nonListed /*!player.PresentInRSystem*/
                && _tournamentView.Tournament.NonDatabasePlayersCount < _tournamentView.Tournament.Players.Count / 2)
            {
                e.Row.FontStyle = FontStyles.Italic;
                e.Row.Foreground = new SolidColorBrush(Colors.Green);
            }

            if (prelimintaryRegistered /*player.PreliminaryRegistration*/)
            {
                e.Row.FontStyle = FontStyles.Italic;
                e.Row.Background = new SolidColorBrush(Colors.Snow);
            }
        }

        private void grdWallList_AutoGeneratedColumns(object sender, EventArgs e)
        {
            //DataGrid grid = sender as DataGrid;
            //grid.Columns[0].Visibility = System.Windows.Visibility.Hidden;
        }

        #endregion

        private void grdWallList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_tournamentView.Tournament == null)
                return;

            var grid = sender as DataGrid;
            if (grid.CurrentCell == null || grid.CurrentCell.Column == null)
                return;

            if (!App.GridCheckDblClick(e))
                return;

            bool showPlayer = false;
            switch (grid.CurrentCell.Column.SortMemberPath)
            {
                case "Name":
                case "Group":
                case "Points":
                case "Rating":
                case "Rank":
                case "Grade":
                case "Score":
                case "ScoreX":
                case "SOS":
                case "SOSOS":
                case "SODOS":
                case "SOUD":
                case "SORP":
                case "Place":
                case "PL":
                case "RC":
                case "Country":
                case "City":
                case "Club":
                case "Team":
                case "Coach":
                case "RN":
                    showPlayer = true;
                    break;
            }
            if (showPlayer)
            {
                var rowView = grid.CurrentItem as DataRowView;
                var id = Convert.ToInt32(rowView.Row.ItemArray[0]);
                if (id < 0) //team
                    return;
                var editPlayer = _tournamentView.GetPlayer(id).Clone() as Player;
                ExecutePlayerWindow(editPlayer);
            }
            else
            {
                int tourId = -1;
                for (int i = 0; i < _tournamentView.Tournament.TourIDs.Count; i++)
                    if (_tournamentView.Tournament.TourIDs[i] == grid.CurrentCell.Column.Header.ToString())
                    {
                        tourId = i;
                        break;
                    }
                if (tourId > -1)
                {
                    var rowView = grid.CurrentItem as DataRowView;
                    var id = Convert.ToInt32(rowView.Row.ItemArray[0]);
                    Pair pair = _tournamentView.Tournament.Tours[tourId].Pairs.Find(item => id == item.FirstPlayerId || id == item.SecondPlayerId);
                    if (pair != null)
                    {
                        pair.TourId = tourId;
                        ExecutePairWindow(pair.Clone() as Pair);
                    }
                    else
                    {
                        Player editPlayer = _tournamentView.GetPlayer(id).Clone() as Player;

                        if (editPlayer.NotPlayingInRound.Contains(tourId + 1))
                            ExecutePlayerWindow(editPlayer);
                        else
                        {
                            pair = new Pair() { FirstPlayerId = id, ForcedPairing = true };
                            pair.TourId = tourId;
                            ExecutePairWindow(pair.Clone() as Pair);
                        }
                    }

                }
            }

        }

        private void grdPlayers_LoadingRow(object sender, DataGridRowEventArgs e)
        {

            Player player = e.Row.Item as Player;
            if (player == null)
                return;

            if (player.Id == 0)
                _bc.PreparePlayers(_tournamentView.Tournament);

            if (_tournamentView.UseTransliteration && _tournamentView.LocalPlayerDbUsage && !player.PresentInRSystem
                && _tournamentView.Tournament.NonDatabasePlayersCount < _tournamentView.Tournament.Players.Count / 2)
            {
                e.Row.FontStyle = FontStyles.Italic;
                e.Row.Foreground = new SolidColorBrush(Colors.Green);
            }

            if (player.PreliminaryRegistration)
            {
                e.Row.FontStyle = FontStyles.Italic;
                e.Row.Background = new SolidColorBrush(Colors.Snow);
                return;
            }

        }

        private void tcMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _tournamentView.ActualTab = tcMain.SelectedIndex;

        }

        private void grdPairings_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            int cSuspictionDiff = 200;

            Pair pair = e.Row.Item as Pair;
            if (pair == null)
                return;

            //col.CellTemplate = FindResource("PairingMemberFirstColumnCellTemplate") as DataTemplate;
            if (pair.Handicap == 0 && pair.FirstPlayer != null && pair.SecondPlayer != null)
            {
                int diff = PlayerInfo.GetRatingByRank(pair.SecondPlayer.Rank) - PlayerInfo.GetRatingByRank(pair.FirstPlayer.Rank);
                if (pair.IsFirstWon && (diff >= cSuspictionDiff) ||
                    pair.IsSecondWon && (-diff >= cSuspictionDiff))
                {
                    e.Row.FontStyle = FontStyles.Italic;
                    e.Row.Foreground = new SolidColorBrush(Colors.Red);
                    //e.Row.Background = new SolidColorBrush(Colors.Snow);
                    //return;
                }
            }
        }

        private void RemovePairs(IList<int> ids)
        {
            var grid = grdPairings;

            for (int i = ids.Count - 1; i >= 0; i--)
            {
                int id = ids[i];
                var p = _tournamentView.GetPair(id, _tournamentView.CurrentRoundNumber - 1);
                if (p != null /*&& p.FirstPlayerId == pair.FirstPlayerId && p.SecondPlayerId == pair.SecondPlayerId*/)
                {
                    _tournamentView.Tournament.Tours[_tournamentView.CurrentRoundNumber - 1].Pairs.Remove(p);

                }
            }
            _tournamentView.UpdateTournamentTables();
            LangResources.LR.UpdateSettings();
        }

        private void mnPairDelete_Click(object sender, RoutedEventArgs e)
        {
            List<int> idLst = GetPairsSelectionList();
            if (idLst == null)
                return;
            var r = DialogWindow.Show(this, LangResources.LR.Delete + " " + idLst.Count + " " + LangResources.GetRecordsStr(idLst.Count) + "?" +
                "\n" + LangResources.LR.DataWillNot, LangResources.LR.Warning,
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r == MessageBoxResult.Yes)
                RemovePairs(idLst);
        }

        private void mnPairSwapPlayers_Click(object sender, RoutedEventArgs e)
        {
            Pair pair = grdPairings.SelectedItem as Pair;
            if (pair == null)
                return;

            int swap = pair.SecondPlayerId;
            pair.SecondPlayerId = pair.FirstPlayerId;
            pair.FirstPlayerId = swap;

            if (pair.GameResult == 1)
                pair.GameResult = 2;
            else
                if (pair.GameResult == 2)
                    pair.GameResult = 1;

            pair.Update();
            _tournamentView.UpdateTournamentTables();
            SetPairListSelection(pair, true);
        }

        private void mnPlayerEdit_Click(object sender, RoutedEventArgs e)
        {
            int id = GetPlayerSelection();
            if (id == -1)
                return;
            grdPlayers_MouseDoubleClick(grdPlayers, null);
        }

        private void RemovePlayer(List<int> ids)
        {
            _tournamentView_WallListUpdateBegin(null, null);
            _tournamentView_PairingUpdateBegin(null, null);
            _tournamentView_PlayersUpdateBegin(null, null);

            new Thread(delegate()
            {
                Thread.CurrentThread.IsBackground = true;
                Thread.Sleep(200);

                this.Dispatcher.BeginInvoke(new MethodInvoker(delegate
                {
                    Cursor = Cursors.Wait;
                    try
                    {
                        foreach (int id in ids)
                        {
                            Player player = _tournamentView.Tournament.Players.Find(p => p.Id == id);
                            if (player != null)
                            {
                                _tournamentView.Tournament.Players.Remove(player);
                            }
                        }

                        _tournamentView.Tournament.RemoveBrokenPairs();
                        _tournamentView.UpdateTournamentTables();
                        _tournamentView.UpdatePlayers();
                    }
                    finally
                    {
                        Cursor = Cursors.Arrow;
                    }
                }));

            }).Start();
        }

        private void mnPlayerDelete_Click(object sender, RoutedEventArgs e)
        {
            List<int> idLst = GetPlayerSelectionList();
            if (idLst == null)
                return;
            var r = DialogWindow.Show(this, LangResources.LR.Delete + " " + idLst.Count + " " + LangResources.GetRecordsStr(idLst.Count) + "?" +
                "\n" + LangResources.LR.DataWillNot, LangResources.LR.Warning,
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r == MessageBoxResult.Yes)
                RemovePlayer(idLst);
        }

        private void mnWallListEdit_Click(object sender, RoutedEventArgs e)
        {
            int id = GetWallListSelection();
            if (id == -1)
                return;
            grdWallList_MouseDoubleClick(grdWallList, null);
        }

        private void mnWallListDelete_Click(object sender, RoutedEventArgs e)
        {
            List<int> idLst = GetWallListSelectionList();
            if (idLst == null)
                return;
            var r = DialogWindow.Show(this, LangResources.LR.Delete + " " + idLst.Count + " " + LangResources.GetRecordsStr(idLst.Count) + "?" +
                "\n" + LangResources.LR.DataWillNot, LangResources.LR.Warning,
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r == MessageBoxResult.Yes)
            {
                RemovePlayer(idLst);
            }
        }

        private void grdPairings_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && !_pairIsEditing)
            {
                mnPairDelete_Click(null, null);
                e.Handled = true;
            }
        }

        private bool _playerIsEditing = false;
        private bool _pairIsEditing = false;

        private void grdPlayers_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && !_playerIsEditing)
            {
                mnPlayerDelete_Click(null, null);
                e.Handled = true;
            }
        }

        private void grdWallList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                mnWallListDelete_Click(null, null);
                e.Handled = true;
            }
        }

        void _tournamentView_PlayersUpdateBegin(object sender, EventArgs e)
        {
            if (grdPlayers.SelectedIndex > -1)
            {
                _playersSelectedId = grdPlayers.SelectedIndex;
            }
        }

        void _tournamentView_PlayersUpdateEnd(object sender, EventArgs e)
        {
            new Thread(delegate()
            {
                Thread.CurrentThread.IsBackground = true;

                this.Dispatcher.BeginInvoke(new MethodInvoker(delegate
                {
                    var grid = grdPlayers;

                    PlayerCancelInplaceEditiong();

                    if (!string.IsNullOrEmpty(_tournamentView.PairingSortColumn))
                    {
                        foreach (var item in grid.Columns)
                        {
                            if (_tournamentView.PlayersSortColumn == item.SortMemberPath)
                            {
                                item.SortDirection = _tournamentView.PlayersSortDirection;
                                SortPlayers();
                            }
                        }
                    }

                    int id = _playersSelectedId;

                    if (id > -1 && id < grid.Items.Count)
                    {
                        grid.SelectedItem = grid.Items[id];
                        //grid.CurrentItem = grid.Items[id];
                        grid.CurrentCell = new DataGridCellInfo(grid.SelectedItem, grid.Columns[1]);
                        //grid.Focus();
                    }

                }));

            }).Start();
        }

        void _tournamentView_PairingUpdateBegin(object sender, EventArgs e)
        {
            if (grdPairings.SelectedIndex > -1)
            {
                //_pairingsSelectedId = grdPairings.SelectedIndex;
            }

            var grid = grdPairings;

            //int id = 0;
            //foreach (var item in grid.Items)
            //{
            //    if (item == grdPairings.CurrentItem)
            //    {
            //        _pairingsSelectedId = id;
            //    }

            //    id++;
            //}
            
            _pairingsSelected = grdPairings.CurrentItem as Pair;
            _pairingsSelectedPrev = GetPairListPrev(_pairingsSelected);
        }

        void _tournamentView_PairingUpdateEnd(object sender, EventArgs e)
        {
            new Thread(delegate()
            {
                Thread.CurrentThread.IsBackground = true;

                this.Dispatcher.BeginInvoke(new MethodInvoker(delegate
                {
                    var grid = grdPairings;

                    // Bingo! It works.
                    grid.CancelEdit(DataGridEditingUnit.Row);
                    grid.Items.Refresh();
                    grid.UpdateLayout();

                    if (!string.IsNullOrEmpty(_tournamentView.PairingSortColumn))
                    {
                        foreach (var item in grid.Columns)
                        {
                            if (_tournamentView.PairingSortColumn == item.SortMemberPath)
                            {
                                item.SortDirection = _tournamentView.PairingSortDirection;
                                SortPairing();
                            }
                        }
                    }

                    Pair current = _pairingsSelected;
                    var prev = _pairingsSelectedPrev;
                    if (prev != null)
                    {
                        grid.SelectedItem = prev;
                        grid.CurrentItem = prev;
                        grid.Focus();
                    }
                    
                    //grid.UnselectAll();
                    //grid.SelectedItems.Clear();

                    //var id = _pairingsSelectedId;
                    //if (id > 0 && id < grid.Items.Count)
                    //{
                    //    grid.SelectedItem = grid.Items[id];
                    //    grid.CurrentItem = grid.Items[id];
                    //    //grid.Focus();
                    //}
                }));

            }).Start();
        }

        private int _wallListSelectedId;
        private Pair _pairingsSelected;
        //private int _pairingsSelectedId;
        private Pair _pairingsSelectedPrev;
        private int _playersSelectedId;

        void _tournamentView_WallListUpdateBegin(object sender, EventArgs e)
        {
            if (grdWallList.SelectedIndex > -1)
            {
                _wallListSelectedId = grdWallList.SelectedIndex;
            }
        }

        void _tournamentView_WallListUpdateEnd(object sender, EventArgs e)
        {
            new Thread(delegate()
            {
                Thread.CurrentThread.IsBackground = true;

                this.Dispatcher.BeginInvoke(new MethodInvoker(delegate
                {
                    var grid = grdWallList;
                    
                    grid.Items.Refresh();
                    grid.UpdateLayout();

                    if (!string.IsNullOrEmpty(_tournamentView.WallListSortColumn))
                    {
                        foreach (var item in grid.Columns)
                        {
                            if (_tournamentView.WallListSortColumn == item.SortMemberPath)
                            {
                                item.SortDirection = _tournamentView.WallListSortDirection;
                                SortWallList();
                            }
                        }
                    }

                    int id = _wallListSelectedId;

                    if (id > -1 && id < grid.Items.Count)
                    {
                        grid.SelectedItem = grid.Items[id];
                        //grid.Focus();
                    }
                }));

            }).Start();
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            _tournamentView.CurrentPage = _tournamentView.CurrentPage + 1;
        }

        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            _tournamentView.CurrentPage = _tournamentView.CurrentPage - 1;
        }

        private void btnFirst_Click(object sender, RoutedEventArgs e)
        {
            _tournamentView.CurrentPage = 1;
        }

        private void btnLast_Click(object sender, RoutedEventArgs e)
        {
            _tournamentView.CurrentPage = _tournamentView.PageCount;
        }

        public static string GetSortMemberPath(DataGridColumn column)
        {
            // find the sortmemberpath
            string sortPropertyName = column.SortMemberPath;
            if (string.IsNullOrEmpty(sortPropertyName))
            {
                DataGridBoundColumn boundColumn = column as DataGridBoundColumn;
                if (boundColumn != null)
                {
                    Binding binding = boundColumn.Binding as Binding;
                    if (binding != null)
                    {
                        if (!string.IsNullOrEmpty(binding.XPath))
                        {
                            sortPropertyName = binding.XPath;
                        }
                        else if (binding.Path != null)
                        {
                            sortPropertyName = binding.Path.Path;
                        }
                    }
                }
            }

            return sortPropertyName;
        }

        public static int FindSortDescription(SortDescriptionCollection sortDescriptions, string sortPropertyName)
        {
            int index = -1;
            int i = 0;
            foreach (SortDescription sortDesc in sortDescriptions)
            {
                if (string.Compare(sortDesc.PropertyName, sortPropertyName) == 0)
                {
                    index = i;
                    break;
                }
                i++;
            }

            return index;
        }

        private void DataGrid_Standard_Sorting(object sender, DataGridSortingEventArgs e)
        {
            DataGrid dataGrid = sender as DataGrid;

            string sortPropertyName = GetSortMemberPath(e.Column);
            if (!string.IsNullOrEmpty(sortPropertyName))
            {
                // sorting is cleared when the previous state is Descending
                if (e.Column.SortDirection.HasValue && e.Column.SortDirection.Value == ListSortDirection.Descending)
                {
                    int index = FindSortDescription(dataGrid.Items.SortDescriptions, sortPropertyName);
                    if (index != -1)
                    {
                        e.Column.SortDirection = null;

                        // remove the sort description
                        dataGrid.Items.SortDescriptions.RemoveAt(index);
                        dataGrid.Items.Refresh();

                        if ((Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift)
                        {
                            // clear any other sort descriptions for the multisorting case
                            dataGrid.Items.SortDescriptions.Clear();
                            dataGrid.Items.Refresh();
                        }

                        // stop the default sort
                        e.Handled = true;
                    }
                }
            }
        }

        private void grdWallList_Sorting(object sender, DataGridSortingEventArgs e)
        {
            if (e.Column != null)
            {
                _tournamentView.WallListSortColumn = e.Column.SortMemberPath;
                _tournamentView.WallListSortDirection = !e.Column.SortDirection.HasValue ? ListSortDirection.Ascending :
                    e.Column.SortDirection.Value == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
            }
            else
            {
                _tournamentView.WallListSortColumn = string.Empty;
                _tournamentView.WallListSortDirection = null;
            }

            DataGrid_Standard_Sorting(sender, e);
        }

        private void SortWallList()
        {
            if (!string.IsNullOrEmpty(_tournamentView.WallListSortColumn))
            {
                //create a collection view for the datasource binded with grid
                ICollectionView dataView = CollectionViewSource.GetDefaultView(grdWallList.ItemsSource);
                dataView.SortDescriptions.Clear();
                var sortOrder = _tournamentView.WallListSortDirection.HasValue ? _tournamentView.WallListSortDirection.Value : ListSortDirection.Ascending;
                dataView.SortDescriptions.Add(new SortDescription(_tournamentView.WallListSortColumn, sortOrder));

                //refresh the view which in turn refresh the grid
                dataView.Refresh();
            }
        }

        private void SortPairing()
        {
            if (!string.IsNullOrEmpty(_tournamentView.PairingSortColumn))
            {
                //create a collection view for the datasource binded with grid
                ICollectionView dataView = CollectionViewSource.GetDefaultView(grdPairings.ItemsSource);
                dataView.SortDescriptions.Clear();
                var sortOrder = _tournamentView.PairingSortDirection.HasValue ? _tournamentView.PairingSortDirection.Value : ListSortDirection.Ascending;
                dataView.SortDescriptions.Add(new SortDescription(_tournamentView.PairingSortColumn, sortOrder));

                //refresh the view which in turn refresh the grid
                dataView.Refresh();
            }
        }

        private void SortPlayers()
        {
            if (!string.IsNullOrEmpty(_tournamentView.PlayersSortColumn))
            {
                //create a collection view for the datasource binded with grid
                ICollectionView dataView = CollectionViewSource.GetDefaultView(grdPlayers.ItemsSource);
                dataView.SortDescriptions.Clear();
                var sortOrder = _tournamentView.PlayersSortDirection.HasValue ? _tournamentView.PlayersSortDirection.Value : ListSortDirection.Ascending;
                dataView.SortDescriptions.Add(new SortDescription(_tournamentView.PlayersSortColumn, sortOrder));

                //refresh the view which in turn refresh the grid
                dataView.Refresh();
            }
        }

        private void grdPairings_Sorting(object sender, DataGridSortingEventArgs e)
        {
            if (e.Column != null)
            {
                _tournamentView.PairingSortColumn = e.Column.SortMemberPath;
                _tournamentView.PairingSortDirection = !e.Column.SortDirection.HasValue ? ListSortDirection.Ascending :
                    e.Column.SortDirection.Value == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
            }
            else
            {
                _tournamentView.PairingSortColumn = string.Empty;
                _tournamentView.PairingSortDirection = null;
            }

            DataGrid_Standard_Sorting(sender, e);
        }

        private void grdPlayers_Sorting_1(object sender, DataGridSortingEventArgs e)
        {
            if (e.Column != null)
            {
                _tournamentView.PlayersSortColumn = e.Column.SortMemberPath;
                _tournamentView.PlayersSortDirection = !e.Column.SortDirection.HasValue ? ListSortDirection.Ascending :
                    e.Column.SortDirection.Value == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
            }
            else
            {
                _tournamentView.PlayersSortColumn = string.Empty;
                _tournamentView.PlayersSortDirection = null;
            }

            DataGrid_Standard_Sorting(sender, e);
        }
    }
}
    
  
