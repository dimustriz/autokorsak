using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Windows;
using System.Windows.Threading;
using Tourtoss.BC;
using Tourtoss.BE;

namespace AutoKorsak
{
    public class TournamentView : INotifyPropertyChanged
    {
        private Tournament _tournament;
        TournamentBC _bc = new TournamentBC();

        private void TournamentLoadHandler(object sender, EventArgs e)
        {
            var tournament = sender as Tournament;
            if (tournament != null)
            {
                Tournament.RSystem = RSystem;
                currentPage = 1;
            }
        }

        public TournamentView()
        {
            TournamentBC.OnTournamentLoad += TournamentLoadHandler;
        }

        public static string g_appLiteral = "AutoKorsak";
        public static string AppName = GetVersion();

        private static string GetVersion()
        {
            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            return g_appLiteral + " " + version.Major + "." + version.Minor;
        }

        public string FileName { get { return _tournament != null ? _tournament.FileName : string.Empty; } }
        public string StatusBarMessage 
        {
            get { return FileName; } 
        }

        public string TitleMessage
        {
            get
            {
                return _tournament != null
                    ? !string.IsNullOrEmpty(_tournament.Name)
                    ? AppName + " - " + _tournament.Name + " - " + _bc.GetTourName(_tournament)
                    : AppName + " - " + _bc.GetTourName(_tournament)
                    : AppName;
            }
        }

        public bool IsTournamentEnabled
        {
            get { return Tournament != null; }
        }

        public Tournament Tournament
        {
            get { return _tournament; }
            set
            {
                _tournament = value;
                
                if (_tournament != null)
                {
                    _tournament.IsCreated = true;
                    Tournament.UseTransliteration = UseTransliteration;
                }

                UpdateTournamentProps();
                UpdatePairings();
            }
        }

        public Tournament GetNewTournament() 
        {
            var result = new Tournament()
            {
                Name = "",
                CurrentRoundNumber = 1,
                Boardsize = 19,
                ExportEncoding = "UTF-8",
                VersionCreated = AppName,
                VersionSaved = AppName,
                HalfScoreGroupsRoundDown = true,
                RatingDeterminesStartScore = true,

                RatingLowestOneDanRating = 2050,
                LowerMacMahonBarLevel = "30k",
                UpperMacMahonBarLevel = "8d",
                UpperMacMahonBarRating = 0,
                HandicapAdjustmentValue = 0,
                HandicapLimitValue = 9,
                HandicapBelowLevel = "1d",
                MakePairingTopGroupSeedingByRatingRound = 2,
                MakePairingOutsideTopGroupStrengthDifferenceValue = 5,
                MakePairingTopGroupByNumberOfPlayers = 0,

                PairingsFontsize = 12,
                PairingsBlackColumnWidth = 15,
                PairingsWhiteColumnWidth = 15,

                PrintWalllistFont = "Courier New",
                PrintWalllistFontsize = 12,
                PrintWalllistIndentationTop = 0,
                PrintWalllistIndentationLeft = 0,
                PrintWalllistColumnDistance = 2,
                PrintWalllistAbbreviateNameLength = 20,
                PrintPairingsFont = "Courier New",
                PrintPairingsFontsize = 12,
                PrintPairingsIndentationTop = 0,
                PrintPairingsIndentationLeft = 0,
                PrintPairingsColumnDistance = 3,
                PrintPairingsAbbreviateNameLength = 35
            };

            result.Walllist.SortCriterion.Add(new SortCriterionDescriptior() { Id = Entity.Score });
            result.Walllist.SortCriterion.Add(new SortCriterionDescriptior() { Id = Entity.SOS });
            result.Walllist.SortCriterion.Add(new SortCriterionDescriptior() { Id = Entity.SODOS });
            result.Walllist.SortCriterion.Add(new SortCriterionDescriptior() { Id = Entity.SOSOS });

            if (result.Countries == null || result.Countries.Count == 0)
            {
                var countries = _bc.LoadCountries();
                if (countries != null)
                {
                    result.Countries = countries.Items;
                }
            }

            if (result.Countries != null)
            {
                result.Countries.Sort();
                result.Countries.Insert(0, new Country());
            }

            if (result.Walllist.Columns.Count == 0)
                foreach (var item in result.EntitiesStd)
                    result.Walllist.Columns.Add(item); 


            return result;
        }

        public int NumberOfRounds 
        {
            get { return _tournament == null ? 0 : _tournament.NumberOfRounds; }
            set 
            {
                if (_tournament != null)
                    _tournament.NumberOfRounds = value;
                UpdateTournamentProps();
                UpdateTournamentTables();
            }
        }

        public int CurrentRoundNumber
        {
            get 
            {
                if (_tournament == null)
                    return 0;
                if (_tournament.CurrentRoundNumber > _tournament.Tours.Count)
                    return _tournament.Tours.Count;
                if (_tournament.CurrentRoundNumber > 0)
                    return _tournament.CurrentRoundNumber;
                else
                    return 1;
            }
            set 
            { 
                if (_tournament != null)
                    _tournament.CurrentRoundNumber = value;
                UpdateStatusBar();
                UpdateTournamentTables();

                OnPropertyChanged("RoundCanNext");
                OnPropertyChanged("RoundCanPrev");
            }
        }

        #region page

        int pageSize = 100;

        public int PageSize
        {
            get
            {
                return pageSize;
            }
            set
            {
                pageSize = value;
            }
        }

        public Visibility PagingVisibility
        {
            get { return Players != null && Players.Count > PageSize ? Visibility.Visible : Visibility.Collapsed; }
        }

        private int currentPage = 1;
        private int pageCount = 1;

        public int PageCount
        {
            get
            {
                return pageCount;
            }
        }

        public void UpdatePageCount()
        {
            if (this.Players == null)
            {
                pageCount = 1;
                return;
            }

            int teamsCount = _bc.GetTeams(this.Players).Count;
            if (teamsCount > 0)
            {
                //According to displaying
                teamsCount += 2;
            }

            pageCount = (int)Math.Ceiling((double)(this.Players.Count + teamsCount) / this.PageSize);
        }

        public bool PagerCanPrev
        {
            get
            {
                return this.CurrentPage > 1;
            }
        }

        public string PagerText
        {
            get
            {
                return this.CurrentPage + "/" + PageCount;
            }
        }

        public bool PagerCanNext
        {
            get
            {
                return this.Players != null ? this.CurrentPage < this.PageCount : false;
            }
        }

        public int CurrentPage
        {
            get
            {
                return currentPage;
            }
            set 
            {
                currentPage = value;
                UpdateWallListPage();
                OnPropertyChanged("PagerCanNext");
                OnPropertyChanged("PagerCanPrev");
            }
        }

        public void UpdatePager()
        {
            UpdatePageCount();
            if (this.PageCount < this.CurrentPage)
            {
                this.CurrentPage = this.PageCount;
            }
        }

        #endregion

        public void UpdateTournamentProps()
        {
            OnPropertyChanged("Tournament");
            OnPropertyChanged("NumberOfRounds");
            OnPropertyChanged("TourIDs");
            OnPropertyChanged("CurrentRoundNumber");
            OnPropertyChanged("TournamentName");
            OnPropertyChanged("IsTournamentEnabled");

            OnPropertyChanged("RoundCanNext");
            OnPropertyChanged("RoundCanPrev");

            UpdatePager();
            UpdateWallList();
            UpdatePairings();
            UpdatePlayers();
        }

        public void UpdatePlayers()
        {
            OnPlayersUpdateBegin();
            OnPropertyChanged("Players");
            OnPlayersChanged();
            OnPlayersUpdateEnd();
        }

        public void UpdateStatusBar()
        {
            OnPropertyChanged("StatusBarMessage");
        }

        public void UpdateWallList()
        {
            OnWallListUpdateBegin();
            OnPropertyChanged("WallList");
            OnPropertyChanged("PagingVisibility");
            UpdatePager();
            OnWallListUpdateEnd();
        }

        public void UpdateWallListPage()
        {
            OnWallListUpdateBegin();
            OnPropertyChanged("WallList");
            OnWallListUpdateEnd();
        }

        public void UpdatePairings()
        {
            OnPairingUpdateBegin();
            OnPropertyChanged("Pairings");
            OnPairingUpdateEnd();
        }

        public void UpdateTournamentTables()
        {
            UpdateWallList();
            UpdatePairings();
        }

        public PlayerList Players
        {
            get
            {
                if (_tournament == null)
                    return null;

                return _tournament.Players;
            }
        }

        private bool IsPlayerInPair(int playerId, Pair pair)
        {
            return playerId == pair.FirstPlayerId || playerId == pair.SecondPlayerId;
        }

        public bool AlreadyPlayed(int player1, int player2, int tourNumber)
        {
            if (_tournament == null || _tournament.Tours.Count < tourNumber)
                return false;

            for (int i = 0; i < tourNumber; i++)
            { 
                Tour tour = _tournament.Tours[i];
                if (tour.Pairs.Find(pl => pl != null &&
                    (
                    pl.FirstPlayerId == player1 && pl.SecondPlayerId == player2 || 
                    pl.FirstPlayerId == player2 && pl.SecondPlayerId == player1)
                ) != null)
                    return true;
            }

            return false;
        }

        public bool StartNumberDuplicated(Player player)
        {
            foreach (var item in _tournament.Players)
            {
                if (item.StartNumber == player.StartNumber && item.Id != player.Id)
                    return true;
            }

            return false;
        }

        public PlayerList GetFreePlayers(Pair rootPair, Pair bakPair, bool first)
        {
            if (_tournament == null)
                return null;
            if (rootPair == null)
                return null;
            if (rootPair.TourId < 0 || rootPair.TourId >= _tournament.Tours.Count)
                return null;

            bool takeCurrentRoundInAccount = _tournament.TakeCurrentRoundInAccount;
            _tournament.TakeCurrentRoundInAccount = false;

            try
            {

                Tour tour = _tournament.Tours[rootPair.TourId];

                var result = new PlayerList();
                foreach (var player in _tournament.Players)
                {
                    if (!player.NotPlayingInRound.Contains(rootPair.TourId + 1) &&
                        !(first && rootPair.SecondPlayerId == player.Id) &&
                        !(!first && rootPair.FirstPlayerId == player.Id) &&
                        ((first && rootPair.FirstPlayerId == player.Id || !first && rootPair.SecondPlayerId == player.Id) ||
                        IsPlayerInPair(player.Id, bakPair) && !IsPlayerInPair(player.Id, rootPair) ||
                        tour.Pairs.Find(player.Id) == null)
                        )
                    {
                        player.CurrentScore = player.GetCoef(_tournament, Entity.Score, rootPair.TourId);
                        result.Add(player);
                    }
                }
                result.Sort(delegate(Player player1, Player player2)
                {
                    return _bc.ComparePlayers(_tournament, rootPair.TourId, player1, player2);
                });

                return result;
            }
            finally
            {
                _tournament.TakeCurrentRoundInAccount = takeCurrentRoundInAccount;
            }
        }

        public Player GetPlayer(int id)
        {
            Players.FillIDs();
            Player player = Players.Find(pl => pl.Id == id);
            if (player == null)
            {
                player = new Player(this.Tournament);
                Players.Add(player);
            }
            return player;
        }

        public Pair GetPair(int boardNumber, int tourNumber)
        {
            if (tourNumber >= _tournament.Tours.Count)
                return null;

            Pair pair = Tournament.Tours[tourNumber].Pairs.Find(pl => pl.BoardNumber == boardNumber);
            if (pair == null)
            {
                pair = new Pair();
                Tournament.Tours[tourNumber].Pairs.Add(pair);
            }
            pair.TourId = tourNumber;

            return pair;
        }

        public Pair GetPair(int tourNumber, int firstPlayerId, int secondPlayerId)
        {
            if (tourNumber >= _tournament.Tours.Count)
                return null;

            Pair pair = Tournament.Tours[tourNumber].Pairs.Find(pl => 
                pl.FirstPlayerId == firstPlayerId && pl.SecondPlayerId == secondPlayerId || 
                pl.FirstPlayerId == secondPlayerId && pl.SecondPlayerId == firstPlayerId 
                );
            if (pair == null)
            {
                pair = new Pair();
                Tournament.Tours[tourNumber].Pairs.Add(pair);
            }
            pair.TourId = tourNumber;

            return pair;
        }

        public void ApplyPlayer(Player player)
        {
            if (player == null)
                return;
            var basePlayer = GetPlayer(player.Id);
            if (player.Id == 0)
                player.Id = basePlayer.Id;

            player.CopyTo(basePlayer);
            basePlayer.Update();
        }

        public void ApplyPair(Pair pair)
        {
            if (pair == null)
                return;
            var basePair = GetPair(pair.TourId, pair.FirstPlayerId, pair.SecondPlayerId);

            if (pair.TourId < _tournament.Tours.Count)
            {
                _bc.UpdatePairProps(_tournament, pair, _tournament.Players);
                if (pair != basePair)
                    pair.CopyTo(basePair);
                Tournament.Tours[pair.TourId].Pairs.FillIDs(true);
                if (pair.BoardNumber == 0)
                    pair.BoardNumber = basePair.BoardNumber;
                pair.Update();
            }

            basePair.Update();
        }

        public ObservableCollection<string> ResultKinds
        {
            get
            {
                return _tournament == null ? null : _tournament.ResultKinds;
            }
        }

        public CountryList Countries
        {
            get
            {
                return _tournament == null ? null : _tournament.Countries;
            }
        }

        public void UpdateClub(string countryCode, string clubCode) //from RGD search
        {
            if (Countries != null)
            {
                var country = Countries.Find(c => c != null && c.InternetCode == countryCode);
                if (country != null)
                {
                    var club = country.Clubs.Find(c => c != null && (c.Name == clubCode || c.EGDName == clubCode));
                    if (club == null)
                    { 
                        country.Clubs.Add(new Club() {Name = clubCode, EGDName = clubCode});
                    }
                }
            }
        }

        public void UpdateClub(string countryCode, Club club)
        {
            if (Countries != null)
            {
                var country = Countries.Find(c => c != null && c.InternetCode == countryCode);
                if (country != null)
                {
                    var cl = country.Clubs.Find(c => c != null && (c.Name == club.Name || c.EGDName == club.EGDName));
                    if (cl == null)
                    {
                        country.Clubs.Add(new Club() { Name = club.Name, EGDName = club.EGDName, NameEn = club.NameEn, NameUa = club.NameUa });
                    }
                }
            }
        }

        public string WallListSortColumn { get; set; }
        public ListSortDirection? WallListSortDirection { get; set; }
        public string PairingSortColumn { get; set; }
        public ListSortDirection? PairingSortDirection { get; set; }       
        public string PlayersSortColumn { get; set; }
        public ListSortDirection? PlayersSortDirection { get; set; }

        public DataTable WallList
        {
            get
            {
                if (_tournament == null)
                    return null;

                int readRecords = 0;
                int skipRecords = 0;

                if (PagingVisibility == Visibility.Visible)
                {
                    readRecords = this.PageSize;
                    skipRecords = (this.CurrentPage - 1) * this.PageSize;
                }

                var result = _bc.GetTournamentDataTable(_tournament, CurrentRoundNumber - 1, _tournament.Walllist.Columns, skipRecords, readRecords, false).Table;

                OnPropertyChanged("PagerCanPrev");
                OnPropertyChanged("PagerText");
                OnPropertyChanged("PagerCanNext");

                return result;
            }
        }

        public PairList Pairings
        {
            get
            {
                if (_tournament != null && _tournament.Tours.Count > 0 && CurrentRoundNumber <= _tournament.Tours.Count)
                {
                    bool takeCurrentRoundInAccount = _tournament.TakeCurrentRoundInAccount;
                    _tournament.TakeCurrentRoundInAccount = false;

                    try
                    {
                        var tour = _tournament.Tours[CurrentRoundNumber - 1];
                        _bc.FillPairsData(_tournament, tour, _tournament.Players);
                        return tour.Pairs;
                    }
                    finally
                    {
                        _tournament.TakeCurrentRoundInAccount = takeCurrentRoundInAccount;
                    }
                }
                else
                    return null;
            }
        }

        public event PropertyChangedEventHandler PlayersChanged;

        private void OnPlayersChanged()
        {
            Tournament.Changed = true;
            if (null != this.PlayersChanged)
                try
                {
                    this.PlayersChanged(this, null);
                }
                catch (Exception) { }
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
            catch(Exception) {}
        }

        public event EventHandler WallListUpdateBegin;

        public void OnWallListUpdateBegin()
        {
            if (null != this.WallListUpdateBegin)
                try
                {
                    this.WallListUpdateBegin(this, null);
                }
                catch (Exception) { }
        }

        public event EventHandler WallListUpdateEnd;

        public void OnWallListUpdateEnd()
        {

            if (null != this.WallListUpdateEnd)
                try
                {
                    this.WallListUpdateEnd(this, null);
                }
                catch (Exception) { }
        }

        public event EventHandler PairingUpdateBegin;

        public void OnPairingUpdateBegin()
        {
            if (null != this.PairingUpdateBegin)
                try
                {
                    this.PairingUpdateBegin(this, null);
                }
                catch (Exception) { }
        }

        public event EventHandler PairingUpdateEnd;

        public void OnPairingUpdateEnd()
        {
            if (null != this.PairingUpdateEnd)
                try
                {
                    this.PairingUpdateEnd(this, null);
                }
                catch (Exception) { }
        }

        public event EventHandler PlayersUpdateBegin;

        public void OnPlayersUpdateBegin()
        {
            if (null != this.PlayersUpdateBegin)
                try
                {
                    this.PlayersUpdateBegin(this, null);
                }
                catch (Exception) { }
        }

        public event EventHandler PlayersUpdateEnd;

        public void OnPlayersUpdateEnd()
        {
            if (null != this.PlayersUpdateEnd)
                try
                {
                    this.PlayersUpdateEnd(this, null);
                }
                catch (Exception) { }
        }

        public bool AutoPairing()
        {
            bool result = _bc.AutoPairing(_tournament, CurrentRoundNumber - 1);
            UpdateTournamentTables();
            return result;
        }

        public void GenerateStartNumbers()
        {
            _bc.AutoRandomStartNumbers(_tournament.Players, _tournament.TournamentSystemScheveningen);
            UpdateTournamentTables();
        }

        public bool CleanPairs()
        {
            bool result = _bc.CleanPairs(_tournament, CurrentRoundNumber - 1);
            UpdateTournamentTables();
            return result;
        }

        public bool RoundCanPrev
        {
            get
            {
                if (_tournament == null)
                    return false;
                if (_tournament.CurrentRoundNumber <= 1)
                    return false;
                return true;
            }
        }

        public bool RoundCanNext
        {
            get
            {
                if (_tournament == null)
                    return false;
                if (_tournament.CurrentRoundNumber >= _tournament.Tours.Count)
                    return false;
                return true;
            }
        }

        public int ActualTab
        {
            get;
            set;
        }

        private bool _playerCanPrev;
        private bool _playerCanNext;

        public bool PlayerCanPrev
        {
            get { return _playerCanPrev; }
            set { _playerCanPrev = value; OnPropertyChanged("PlayerCanPrev"); }
        }

        public bool PlayerCanNext
        {
            get { return _playerCanNext; }
            set { _playerCanNext = value; OnPropertyChanged("PlayerCanNext"); }
        }

        private bool _pairCanPrev;
        private bool _pairCanNext;

        public bool PairCanPrev
        {
            get { return _pairCanPrev; }
            set { _pairCanPrev = value; OnPropertyChanged("PairCanPrev"); }
        }

        public bool PairCanNext
        {
            get { return _pairCanNext; }
            set { _pairCanNext = value; OnPropertyChanged("PairCanNext"); }
        }

        public LangResources Capt
        {
            get { return LangResources.LR; }
        }

        private RecentFiles _recentFiles = new RecentFiles();

        public RecentFiles RecentTournaments { get { return _recentFiles; } }

        private RtKind _localPlayerDbKind;

        public RtKind LocalPlayerDbKind
        {
            get { return _localPlayerDbKind; }
            set { _localPlayerDbKind = value; OnPropertyChanged("RSystem"); }
        }

        private bool _localPlayerDbUsage;

        public bool LocalPlayerDbUsage {
            get { return _localPlayerDbUsage; }
            set { _localPlayerDbUsage = value; OnPropertyChanged("LocalPlayerDbUsage"); }
        }

        private bool _useTransliteration;

        public bool UseTransliteration
        {
            get { return _useTransliteration; }
            set 
            { 
                _useTransliteration = value;
                if (_tournament != null)
                    Tournament.UseTransliteration = value;
                OnPropertyChanged("UseTransliteration");
                UpdateTournamentProps();
            }
        }

        public bool TakeCurrentRoundInAccount
        {
            get { return _tournament != null ? _tournament.TakeCurrentRoundInAccount : false; }
            set
            {
                if (_tournament != null)
                {
                    _tournament.TakeCurrentRoundInAccount = value;

                    OnPropertyChanged("TakeCurrentRoundInAccount");
                    UpdateTournamentProps();
                }
            }
        }

        private ConfigInfo _cfgInfo;

        public ConfigInfo CfgInfo
        {
            get
            {
                if (_cfgInfo == null)
                {
                    var bc = new RatingListBC();
                    _cfgInfo = bc.LoadConfigInfo();
                }
                return _cfgInfo;
            }
            set
            {
                _cfgInfo = value;
            }
        }

        private RatingList _rList;

        public RatingList RList 
        { 
            get 
            {
                if (!LocalPlayerDbUsage)
                    return null;
                if (_rList == null || LocalPlayerDbKind != _rList.Kind)
                {
                    var bc = new RatingListBC();
                    _rList = bc.LoadRatingList(LocalPlayerDbKind);
                    if (_rList != null)
                        _rList.Sort(RtItemSortOrder.Surname);
                    //OnPropertyChanged("RListUpdated");
                    OnPropertyChanged("RSystem");
                }
                return _rList;
            }
            set 
            { 
                _rList = value;
                if (_rList != null)
                    _rList.Sort(RtItemSortOrder.Surname);
                //OnPropertyChanged("RListUpdated");
            }
        }

        private RatingSystem _rSystem;

        public RatingSystem RSystem
        {
            get
            {
                if (!LocalPlayerDbUsage)
                    return null;
                if (_rSystem == null || LocalPlayerDbKind != _rSystem.Kind)
                {
                    var bc = new RatingSystemBC();
                    _rSystem = bc.Load(LocalPlayerDbKind);
                    //OnPropertyChanged("RSystemUpdated");
                }
                return _rSystem;
            }
            set
            {
                _rSystem = value;
                Tournament.RSystem = _rSystem;

                //OnPropertyChanged("RSystemUpdated");
            }
        }

        public void UpdateRList()
        {
            OnPropertyChanged("RList");
        }

        public string RListUaUpdated
        {
            get 
            {
                if (string.IsNullOrEmpty(CfgInfo.RatingUaDate))
                    return Capt.UkrainianRatingSystem + " (" + Capt.NotLoaded + ")";
                else
                    return Capt.UkrainianRatingSystem + " " + Capt.ActualDate + " " + CfgInfo.RatingUaDate;
            }
        }

        public string RListRuUpdated
        {
            get
            {
                if (string.IsNullOrEmpty(CfgInfo.RatingRuDate))
                    return Capt.RussianRatingSystem + " (" + Capt.NotLoaded + ")";
                else
                    return Capt.RussianRatingSystem + " " + Capt.ActualDate + " " + CfgInfo.RatingRuDate;
            }
        }

        private SysConfig _sysConfig;

        public SysConfig SysConfig
        {
            get
            {
                if (_sysConfig == null)
                {
                    var bc = new SysConfigBC();
                    _sysConfig = bc.Load();
                    //OnPropertyChanged("SysConfigUpdated");
                }
                return _sysConfig;
            }
            set
            {
                _sysConfig = value;
                //OnPropertyChanged("SysConfigUpdated");
            }
        }
    }

}
