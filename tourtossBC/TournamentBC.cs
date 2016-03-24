using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Globalization;
using System.Threading;

using DocumentFormat.OpenXml;

using Tourtoss.BE;
using Tourtoss.DL;

namespace Tourtoss.BC
{
    public class TournamentBC : BaseBC<TournamentDL>
    {
        public static TournamentBC Instance = new TournamentBC();

        #region Tournament events

        public static EventHandler OnTournamentLoad;

        public void OnUpdateTournament(Tournament tournament)
        {
            if (tournament != null)
            {
                UpdateHandicaps(tournament);
            }
        }

        #endregion

        #region File operations

        public void Save(Tournament tournament, string fileName)
        {
            tournament.UpdateTourIDs();

            //Compatibility with MacMahon
            tournament.RatingAllowed = true;

            RemoveEmptyCountries(tournament);

            UpdatePairs(tournament);

            DL.SaveTournament(fileName, tournament);

            AddEmptyCountries(tournament);

            tournament.FileName = fileName;
            Tournament.Changed = false;
        }

        private void AddEmptyCountries(Tournament tournament)
        {
            if (tournament.Countries != null)
            {
                if (tournament.Countries.Count > 0 && !(tournament.Countries[0] == null || string.IsNullOrEmpty(tournament.Countries[0].Name)))
                    tournament.Countries.Insert(0, new Country());
                foreach (var item in tournament.Countries)
                {
                    if (item.Clubs.Count == 0 || item.Clubs[0] != null && !string.IsNullOrEmpty(item.Clubs[0].Name))
                        item.Clubs.Insert(0, new Club());
                }
            }

        }

        public bool CheckClub(Tournament tournament, string countryCode, string clubCode)
        {
            bool result = false;
            if (tournament.Countries != null)
            {
                var country = tournament.Countries.Find(c => c != null && c.InternetCode == countryCode);
                if (country != null)
                {
                    var club = country.Clubs.Find(c => c != null && c.Name == clubCode);
                    if (club == null)
                    {
                        return false;
                    }
                }
            }
            return result;
        }

        private void RemoveEmptyCountries(Tournament tournament)
        {
            if (tournament.Countries != null)
            {
                if (tournament.Countries.Count > 0 && (tournament.Countries[0] == null || string.IsNullOrEmpty(tournament.Countries[0].Name)))
                    tournament.Countries.RemoveAt(0);
                foreach (var item in tournament.Countries)
                {
                    while (item.Clubs.Count > 0 && (item.Clubs[0] == null || string.IsNullOrEmpty(item.Clubs[0].Name)))
                        item.Clubs.RemoveAt(0);
                }
            }
        }

        private void PostLoad(Tournament tournament)
        {
            if (tournament.Countries == null || tournament.Countries.Count == 0)
            {
                var countries = LoadCountries();
                if (countries != null)
                    tournament.Countries = countries.Items;
            }

            if (tournament.Countries != null && Tournament.RSystem != null)
            {
                string country = Tournament.RSystem.Kind.ToString();
                foreach (var item in tournament.Countries)
                {
                    if (!string.IsNullOrEmpty(item.InternetCode) && item.InternetCode.CompareTo(country) == 0)
                    {
                        foreach (var rClub in Tournament.RSystem.Clubs)
                        {
                            if (!string.IsNullOrEmpty(rClub.EGDName))
                            {
                                var cl = item.Clubs.Find(c => c.EGDName == rClub.EGDName || c.Name == rClub.EGDName);
                                if (cl == null)
                                {
                                    item.Clubs.Add(
                                        new Club()
                                        {
                                            EGDName = rClub.EGDName,
                                            Name = rClub.Name,
                                            NameEn = rClub.NameEn,
                                            NameUa = rClub.NameUa
                                        });
                                }
                                else
                                {
                                    cl.EGDName = rClub.EGDName;
                                    cl.Name = rClub.Name;
                                    cl.NameUa = rClub.NameUa;
                                    cl.NameEn = rClub.NameEn;
                                }
                            }
                        }
                    }
                }
            }

            RemoveEmptyCountries(tournament);
            AddEmptyCountries(tournament);

            foreach (var player in tournament.Players)
            {
                player.RootTournament = tournament;
                player.Rank = player.Rank;

                //check missing clubs and countries
                if (!string.IsNullOrEmpty(player.Country) && tournament.Countries != null)
                {
                    var country = tournament.Countries.Find(item => item != null &&
                        (item.Name != null && item.Name.Equals(player.Country, StringComparison.OrdinalIgnoreCase) ||
                        (item.InternetCode != null && item.InternetCode.Equals(player.Country, StringComparison.OrdinalIgnoreCase))));
                    if (country != null)
                    {
                        if (player.Country != country.InternetCode)
                        {
                            string saveClub = player.Club;
                            player.Country = country.InternetCode;
                            player.Club = saveClub;
                        }

                        if (!string.IsNullOrEmpty(player.Club))
                        {
                            var club = country.Clubs.Find(item => item != null &&
                                (item.Name != null && item.Name.Equals(player.Club, StringComparison.OrdinalIgnoreCase) ||
                                item.NameEn != null && item.NameEn.Equals(player.Club, StringComparison.OrdinalIgnoreCase) ||
                                item.NameUa != null && item.NameUa.Equals(player.Club, StringComparison.OrdinalIgnoreCase) ||
                                item.EGDName != null && item.EGDName.Equals(player.Club, StringComparison.OrdinalIgnoreCase)));
                            if (club != null)
                            {
                                if (player.Club != club.Name)
                                    player.Club = club.Name;
                            }
                            else
                            {
                                string egdName = player.Club.Length > 4 ? player.Club.Substring(0, 4) : player.Club;
                                country.Clubs.Add(new Club() { Name = player.Club, EGDName = egdName });
                            }
                        }
                    }
                }
            }

            tournament.IsCreated = true;
            tournament.UpdateTourIDs();


            if (tournament.Walllist.Columns.Count == 0)
                foreach (var item in tournament.EntitiesStd)
                {
                    if (
                        !(item == Entity.Rank && tournament.Players.All(pl => pl.Rank == PlayerInfo.GetRankFromRating(0))) &&
                        !(item == Entity.Rating && tournament.Players.All(pl => pl.Rating == 0)) &&
                        !(item == Entity.NewRating && tournament.Players.All(pl => pl.Rating == 0))
                        )
                        tournament.Walllist.Columns.Add(item);
                }

            Tournament.Changed = false;
        }

        public Tournament Load(string fileName)
        {
            Tournament tournament = null;
            try
            {
                tournament = DL.Load(fileName);

                tournament.FileName = fileName;

                RemoveBye(tournament.Players);

                if (OnTournamentLoad != null)
                    OnTournamentLoad(tournament, null);

                PostLoad(tournament);
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
            return tournament;
        }

        public Tournament ImportFromTur(string fileName)
        {
            Tournament tournament = null;

            try
            {
                var tf = new TurFile();
                if (!tf.Load(fileName))
                    return null;

                tournament = new Tournament();

                tournament.Name = tf.Name;
                tournament.Description = tf.Descr;
                tournament.VersionCreated = tf.CreatedBy;
                tournament.VersionSaved = tf.ModifiedBy;
                tournament.NumberOfRounds = tf.NumberofRounds;
                tournament.CurrentRoundNumber = tf.CurrentRound;
                tournament.UseMacMahonStartScores = tf.UseMacMahon;
                tournament.LowerMacMahonBarLevel = tf.LowMcMahonBarString;
                tournament.UpperMacMahonBarLevel = tf.TopMcMahonBarString;

                tournament.TakeCurrentRoundInAccount = true;

                tournament.HandicapUsed = tf.HandicapBelow;
                tournament.HandicapBelow = tf.HandicapBelow;
                tournament.HandicapBelowLevel = tf.HandicapBelowLevel;
                if (tournament.HandicapBelow)
                {
                    tournament.HandicapLimit = true;
                    tournament.HandicapLimitValue = 9;
                }

                foreach (var item in tf.Criteria)
                {
                    var sc = new SortCriterionDescriptior();
                    switch (item)
                    {
                        case 1: sc.Id = Entity.Score; break;
                        case 2: sc.Id = Entity.SOS; break;
                        case 3: sc.Id = Entity.SOSOS; break;
                        case 4: sc.Id = Entity.SODOS; break;
                        case 5: sc.Id = Entity.ScoreX; break; //not sure
                        case 6: sc.Id = Entity.SORP; break; //not sure
                        case 7: sc.Id = Entity.Points; break;
                        default: continue;
                    }
                    tournament.Walllist.SortCriterion.Add(sc);
                }

                //Add players
                for (int i = 0; i < tf.NumberOfPlayers; i++)
                {
                    var tp = tf.Players[i];
                    tournament.Players.Add(new Player(tournament)
                    {
                        Id = i + 1,
                        Club = tf.Clubs[tp.ClubId - 1] != "?" ? tf.Clubs[tp.ClubId - 1] : string.Empty,
                        Country = tf.Countries[tp.CountryId - 1] != "?" ? tf.Countries[tp.CountryId - 1] : string.Empty,
                        PreliminaryRegistration = tp.IsPreliminary,
                        FirstName = tp.FirstName,
                        Surname = tp.Surname,
                        Rank = tp.Rank,
                        Rating = tp.Rating,
                        Comment = tp.Comment
                    });
                }

                //Add tours data
                for (int j = 0; j < tf.NumberofRounds; j++)
                {
                    tournament.Tours.Add(new Tour(j));
                    var pairs = tournament.Tours[j].Pairs;

                    for (int i = 0; i < tf.NumberOfPlayers; i++)
                    {
                        var tp = tf.Players[i];
                        var tr = tp.RoundInfo[j];

                        int playerId = i + 1;
                        int competitorId = tr.P1_CompetitorId + 1;

                        if (pairs.Find(playerId) != null)
                            continue;

                        if (tr.P1_CompetitorId >= 0)
                        {
                            var pair = new Pair();

                            if (tr.P4_Color < 30)
                            {
                                pair.FirstPlayerId = playerId;
                                pair.SecondPlayerId = competitorId;
                            }
                            else
                            {
                                pair.FirstPlayerId = competitorId;
                                pair.SecondPlayerId = playerId;
                            }

                            pair.BoardNumberFixed = tr.P7_Board > 0;
                            pair.BoardNumber = tr.P7_Board;

                            //pair.Handicap = tr.P5_Handicap; -- unbelivable figures
                            if (tr.P5_Handicap > 0)
                            {
                                int additionalKomi;
                                bool swap;
                                pair.Handicap = GetHandicap(tournament, i, pair.FirstPlayerId, pair.SecondPlayerId, out additionalKomi, out swap);
                                pair.AdditionalKomi = additionalKomi;
                            }

                            switch (tr.P3_GameResult)
                            {
                                case 0: pair.GameResult = 0; break; // no result

                                case 129: pair.GameResult = 0; break; // ?-?
                                case 130: pair.GameResult = 1; break; // 1-0
                                case 132: pair.GameResult = 2; break; // 0-1
                                case 136: pair.GameResult = 4; break; // 0-0

                                case 192:
                                    pair.GameResult = 1;  // free - 1-0!
                                    pair.FirstPlayerId = tp.Id;
                                    pair.SecondPlayerId = -1;
                                    break;
                                case 160: pair.GameResult = 2; break; // 0-1!
                                case 144: pair.GameResult = 4; break; // 0-0!
                                case 128: pair.GameResult = 3; break; // 1-1!
                            }

                            pairs.Add(pair);
                        }
                    }
                }
            }
            catch
            {
            }

            return tournament;
        }

        public Tournament ImportFromTxt(string fileName)
        {
            return DL.ImportFromTxt(fileName);
        }

        public Tournament ImportFromExcel(string fileName)
        {
            return DL.ImportFromExcel(fileName);
        }

        public Tournament Import(string fileName)
        {
            Tournament tournament = null;
            try
            {
                string ext = System.IO.Path.GetExtension(fileName).ToLower();
                if (ext == ".tur")
                    tournament = ImportFromTur(fileName);
                else
                    if (ext == ".txt")
                        tournament = ImportFromTxt(fileName);
                    else
                        if (ext == ".xlsx")
                            tournament = ImportFromExcel(fileName);

                if (tournament != null)
                    tournament.FileName = fileName;

                if (OnTournamentLoad != null)
                    OnTournamentLoad(tournament, null);

                PostLoad(tournament);
            }
            catch
            {
            }
            return tournament;
        }

        public Countries LoadCountries()
        {
            Countries countries = null;
            try
            {
                countries = DL.LoadCountries("countries.xml");
                if (countries != null && countries.Items != null)
                    countries.Items.Sort();
            }
            catch
            {
            }
            return countries;
        }

        #endregion

        #region Player operations

        public void PreparePlayers(Tournament tournament)
        {
            tournament.Players.FillIDs();

            // Map player pairs

            int tourId = 0;

            PrepareTourInfo(tournament);

            tourId = 0;

            foreach (Tour tour in tournament.Tours)
            {
                if (tour.Pairs != null)
                {
                    foreach (Pair pair in tour.Pairs)
                    {
                        var firstPlayer = tournament.Players.GetById(pair.FirstPlayerId);
                        var secondPlayer = tournament.Players.GetById(pair.SecondPlayerId);

                        pair.FirstPlayer = firstPlayer;
                        pair.SecondPlayer = secondPlayer;

                        if (firstPlayer != null)
                        {
                            firstPlayer.TourInfoList[tourId].Competitor = secondPlayer;
                            firstPlayer.TourInfoList[tourId].CompetitorId = pair.SecondPlayerId;
                        }

                        if (secondPlayer != null)
                        {
                            secondPlayer.TourInfoList[tourId].Competitor = firstPlayer;
                            secondPlayer.TourInfoList[tourId].CompetitorId = pair.FirstPlayerId;
                        }
                    }
                }
                tourId++;
            }
        }

        private void PrepareImpossiblePairs(Tournament tournament)
        {
            tournament.ImpossiblePairs.Clear();

            tournament.ImpossiblePairs.Add(Restriction.AlreadyPlayed);

            if (tournament.MakePairingOutsideTopGroupSameCountry)
            {
                tournament.ImpossiblePairs.Add(Restriction.SameCountry);
            }

            if (tournament.MakePairingOutsideTopGroupSameClub)
            {
                tournament.ImpossiblePairs.Add(Restriction.SameClub);
            }

            if (tournament.MakePairingOutsideTopGroupSameNationality)
            {
                tournament.ImpossiblePairs.Add(Restriction.SameNationality);
            }

            if (tournament.MakePairingOutsideTopGroupSameTeam)
            {
                tournament.ImpossiblePairs.Add(Restriction.SameTeam);
            }

            if (tournament.MakePairingOutsideTopGroupSameCoach)
            {
                tournament.ImpossiblePairs.Add(Restriction.SameCoach);
            }
        }

        private bool IsImpossble(Tournament tournament, int tourNumber, Player player1, Player player2)
        {
            var restrictions = tournament.ImpossiblePairs;
            bool result = false;

            int i;
            for (i = 0; i < restrictions.Count; i++)
            {
                switch (restrictions[i])
                {
                    case Restriction.AlreadyPlayed:
                        result = IsAlreadyPlayed(tournament, player1, player2);
                        break;
                    case Restriction.AlreadyPairedDown:
                        bool aPairedDown = GetPairedDown(tournament, tourNumber, player1, player2);
                        result = aPairedDown && (
                            player1.GetSoud(tournament, tourNumber) < 0 ||
                            player2.GetSoud(tournament, tourNumber) > 0);
                        break;
                    case Restriction.SameClub:
                        result = player1.Club != null && player1.Club == player2.Club && !player1.TopGroupMember && !player2.TopGroupMember;
                        break;
                    case Restriction.SameCity:
                        result = player1.City != null && player1.City == player2.City && !player1.TopGroupMember && !player2.TopGroupMember;
                        break;
                    case Restriction.SameCountry:
                        result = player1.Country != null && player1.Country == player2.Country && !player1.TopGroupMember && !player2.TopGroupMember;
                        break;
                    case Restriction.SameTeam:
                        result = !string.IsNullOrEmpty(player1.Team) && player1.Team == player2.Team && !player1.TopGroupMember && !player2.TopGroupMember;
                        break;
                    case Restriction.SameCoach:
                        result = !string.IsNullOrEmpty(player1.Coach) && player1.Coach == player2.Coach && !player1.TopGroupMember && !player2.TopGroupMember;
                        break;
                    case Restriction.SameNationality:
                        result = player1.Nationality != null && player1.Nationality == player2.Nationality && !player1.TopGroupMember && !player2.TopGroupMember;
                        break;
                }
                if (result == true)
                    break;
            }
            return result;
        }

        private bool IsImpossbleRestrictions(Tournament tournament, int tourNumber, PlayerList players)
        {
            List<Restriction> restrictions = tournament.ImpossiblePairs;

            bool result = false;
            if (players.Count > 0)
            {
                for (int i = 0; i < restrictions.Count; i++)
                {
                    result = true;
                    switch (restrictions[i])
                    {
                        case Restriction.SameClub:
                            for (int j = 1; j < players.Count; j++)
                                if (players[j].Club != players[0].Club && !players[j].TopGroupMember && !players[0].TopGroupMember)
                                {
                                    result = false;
                                    break;
                                }
                            break;
                        case Restriction.SameCity:
                            for (int j = 1; j < players.Count; j++)
                                if (players[j].City != players[0].City && !players[j].TopGroupMember && !players[0].TopGroupMember)
                                {
                                    result = false;
                                    break;
                                }
                            break;
                        case Restriction.SameCountry:
                            for (int j = 1; j < players.Count; j++)
                                if (players[j].Country != players[0].Country && !players[j].TopGroupMember && !players[0].TopGroupMember)
                                {
                                    result = false;
                                    break;
                                }
                            break;
                        case Restriction.SameTeam:
                            for (int j = 1; j < players.Count; j++)
                                if (players[j].Team != players[0].Team && !players[j].TopGroupMember && !players[0].TopGroupMember)
                                {
                                    result = false;
                                    break;
                                }
                            break;
                        case Restriction.SameCoach:
                            for (int j = 1; j < players.Count; j++)
                                if (players[j].Coach != players[0].Coach && !players[j].TopGroupMember && !players[0].TopGroupMember)
                                {
                                    result = false;
                                    break;
                                }
                            break;
                        case Restriction.SameNationality:
                            for (int j = 1; j < players.Count; j++)
                                if (players[j].Nationality != players[0].Nationality && !players[j].TopGroupMember && !players[0].TopGroupMember)
                                {
                                    result = false;
                                    break;
                                }
                            break;
                        default:
                            result = false;
                            break;
                    }
                    if (result == true)
                        break;
                }
            }
            return result;
        }

        public bool IsAlreadyPlayed(Tournament tournament, Player player1, Player player2)
        {
            int tourId = 0;
            foreach (Tour tour in tournament.Tours)
            {
                if (
                    (player1.TourInfoList[tourId].CompetitorId == -1 && player2.Id == -1)
                || (player2.TourInfoList[tourId].CompetitorId == -1 && player1.Id == -1)
                || (player1.TourInfoList[tourId].Competitor == player2)
                    )
                {
                    return true;
                }
                tourId++;
            }

            return false;
        }

        public void SkipTour(Tournament tournament, int playerId, params int[] tourNumbers)
        {
            var player = tournament.Players.Find(
                delegate(Player item) { return item.Id == playerId; });

            if (player != null)
            {
                player.NotPlayingInRound.Clear();
                foreach (int tourId in tourNumbers)
                {
                    player.NotPlayingInRound.Add(tourId + 1);
                }
            }
        }


        public void FillPairsData(Tournament tournament, Tour tour, PlayerList players)
        {
            for (int i = 0; i < tour.Pairs.Count; i++)
            {
                var pair = tour.Pairs[i];
                pair.TourId = tour.RoundNumber - 1;
                UpdatePairProps(tournament, pair, players);
            }
            tour.Pairs.FillIDs(true);
        }

        public void UpdatePairProps(Tournament tournament, Pair pair, PlayerList players)
        {
            var firstPlayer = players.GetById(pair.FirstPlayerId);
            var secondPlayer = players.GetById(pair.SecondPlayerId);

            int rating = 0;
            double score = 0;

            if (firstPlayer != null)
            {
                pair.FirstPlayer = firstPlayer;
                firstPlayer.CurrentScore = firstPlayer.GetCoef(tournament, Entity.Score, pair.TourId);
                pair.FirstPlayerName = Tournament.UseTransliteration ? firstPlayer.NameExt2 : firstPlayer.InternationalNameExt2;
                rating += firstPlayer.Rating;
                score += firstPlayer.CurrentScore;
            }
            if (secondPlayer != null)
            {
                pair.SecondPlayer = secondPlayer;
                secondPlayer.CurrentScore = secondPlayer.GetCoef(tournament, Entity.Score, pair.TourId);
                pair.SecondPlayerName = Tournament.UseTransliteration ? secondPlayer.NameExt2 : secondPlayer.InternationalNameExt2;
                rating += secondPlayer.Rating;
                score += secondPlayer.CurrentScore;
            }
            pair.Rating = rating;
            pair.Score = score;
        }

        public void UpdatePairs(Tournament tournament)
        {
            foreach (var tour in tournament.Tours)
            {
                foreach (var pair in tour.Pairs)
                {
                    if (pair.SecondPlayerId == -1 || pair.FirstPlayerId == -1)
                        pair.PairingWithBye = true;
                }
            }
        }

        public int GetHandicap(Tournament tournament, int tourNumber, int FirstPlayerId, int SecondPlayerId, out int additionalKomi, out bool swapNeeded)
        {
            int result = 0;
            additionalKomi = 0;
            swapNeeded = false;

            if (tournament.HandicapUsed)
            {
                var player1 = tournament.Players.Find(p => p != null && p.Id == FirstPlayerId && FirstPlayerId != -1);
                var player2 = tournament.Players.Find(p => p != null && p.Id == SecondPlayerId && SecondPlayerId != -1);

                if (player1 != null && player2 != null)
                {
                    if (tournament.HandicapBelow)
                    {
                        int r = PlayerInfo.GetRatingByRank(tournament.HandicapBelowLevel);
                        if (PlayerInfo.GetRatingByRank(player1.Rank) > r ||
                            PlayerInfo.GetRatingByRank(player2.Rank) > r)
                            return result;
                    }

                    if (tournament.HandicapByLevel || tournament.HandicapByRating)
                    {
                        int r1 = player1.Rating;
                        int r2 = player2.Rating;

                        if (tournament.HandicapByLevel)
                        {
                            r1 = PlayerInfo.GetRatingByRank(player1.Rank);
                            r2 = PlayerInfo.GetRatingByRank(player2.Rank);
                        }

                        int l1 = PlayerInfo.GetHandicupBaseByRating(r1);
                        int l2 = PlayerInfo.GetHandicupBaseByRating(r2);

                        int d = (l1 - l2) / 100;
                        swapNeeded = d > 0;
                        result = Math.Abs(d);
                    }
                    else
                    {
                        int d = (int)(player1.GetCoef(tournament, Entity.Score, tourNumber) - player2.GetCoef(tournament, Entity.Score, tourNumber));
                        swapNeeded = d > 0;
                        result = Math.Abs(d);
                    }

                    if (tournament.HandicapAdjustment)
                        result += tournament.HandicapAdjustmentValue;

                    if (result < 0)
                    {
                        result = -result;
                        swapNeeded = true;
                    }

                    if (tournament.HandicapLimit && result > tournament.HandicapLimitValue)
                    {
                        if (tournament.HandicapAdditionalKomi)
                            additionalKomi = (result - tournament.HandicapLimitValue) * 15;
                        result = tournament.HandicapLimitValue;
                    }

                }
            }

            return result;
        }

        public void UpdateHandicaps(Tournament tournament)
        {
            if (tournament == null)
                return;

            for (int i = 0; i < tournament.Tours.Count; i++)
            {
                var tour = tournament.Tours[i];

                foreach (var pair in tour.Pairs)
                {
                    pair.AllowJigo = tournament.AllowJigo;
                    if (!pair.ForcedPairing)
                    {
                        int additionalKomi;
                        bool swap;
                        pair.Handicap = GetHandicap(tournament, i, pair.FirstPlayerId, pair.SecondPlayerId, out additionalKomi, out swap);
                        pair.AdditionalKomi = additionalKomi;
                        if (swap)
                            PerformColorBalance(tournament, i, pair, true);
                    }
                }
            }
        }

        #endregion

        #region Pairing

        private double GetPairingMMS(Tournament tournament, int tourNumber, Player player)
        {
            return player == null ? 0 : player.GetCoef(tournament, Entity.Score, tourNumber - 1, false);
        }

        private int GetUpDownBalance(Tournament tournament, int tourNumber, Player player1, Player player2)
        {
            var Mms1 = GetPairingMMS(tournament, tourNumber, player1);
            var Mms2 = GetPairingMMS(tournament, tourNumber, player2);

            return Math.Sign(Mms2 - Mms1);
        }

        private bool GetPairedDown(Tournament tournament, int tourNumber, Player player1, Player player2)
        {
            return GetUpDownBalance(tournament, tourNumber, player1, player2) > 0;
        }


        public bool AutoPairing(Tournament tournament, int tourNumber)
        {
            bool isLastTour = tournament.CurrentRoundNumber == tournament.NumberOfRounds;
            bool takeCurrentRoundInAccount = tournament.TakeCurrentRoundInAccount;
            tournament.TakeCurrentRoundInAccount = false;

            try
            {
                PreparePlayers(tournament);

                if (tournament.TournamentSystemRound)
                    return AutoPairingRoundRobin(tournament);
                if (tournament.TournamentSystemScheveningen)
                    return AutoPairingScheveningenSystem(tournament);

                //Pairing for Swiss and McMahon tournament systems

                UpdateStartScores(tournament);

                PrepareImpossiblePairs(tournament);

                if (tourNumber < tournament.Tours.Count && tourNumber > -1)
                {
                    var tourPlayers = GetSortedPlayers(tournament, tourNumber, true, false);
                    var pairs = tournament.Tours[tourNumber].Pairs;

                    AddBye(tournament, tourPlayers, true);

                    ExcludePairedPlayers(pairs, tourPlayers);
                    UpdateUpDownBalance(tournament, tourPlayers, tourNumber);
                    CalculateScoreInfo(tournament, tourNumber, tourPlayers);

                    var groups = GetGroups(tournament, tourNumber, tourPlayers);

                    bool lastGroupProcessed = false;


                    // Main group cycle
                    while (groups.Count > 0)
                    {
                        var group = groups[0];
                        bool combineUp = false;

                        // try to find pair for the last group
                        if (isLastTour && !lastGroupProcessed)
                        {
                            group = groups[groups.Count - 1];
                            lastGroupProcessed = true;
                            combineUp = true;
                        }

                        SetPlayersOrderByMmsAndRating(tournament, group.Players, tourNumber);

                        Group combinedGroup = null;

                        bool canPair = false;
                        do
                        {
                            // try to pair with sort order changing
                            if (group.Players.Count != 1)
                            {
                                group.IsLast = groups.Count == 1;
                                canPair = SortPairingPlayers(tournament, tourNumber, group, null);
                            }

                            if (!canPair)
                            {
                                if (pairs.Count == 0 && groups.Count == 1)
                                    return false; // cannot pair at all
                                else
                                {

                                    combinedGroup = GetGroupToCombinePairs(tournament, tourNumber, groups, group, !combineUp);

                                    // if it is possible - move the group members and continue pairing
                                    if (combinedGroup != null)
                                    {

                                        if (combineUp)
                                        {
                                            ApplyGroupCombining(combinedGroup, group);
                                        }
                                        else
                                            ApplyGroupCombining(group, combinedGroup);

                                        canPair = true;
                                    }
                                    else
                                    {
                                        if (combineUp)
                                            break;
                                        else
                                            ReArrangeGroups(tournament, tourNumber, groups, pairs, tourPlayers);
                                    }

                                }
                            }
                        }
                        while (!canPair);

                        while (group.Players.Count > 0)
                        {

                            group.IsLast = combineUp && combinedGroup != null || groups.Count == 1;

                            var pair = GetTopPair(tournament, tourNumber, isLastTour, group);

                            if (pair != null)
                                AddPair(tournament, tourNumber, pair, pairs, group.Players);
                            else
                            {
                                if (combineUp && combinedGroup != null)
                                    ApplyGroupCombining(group, combinedGroup);
                                else
                                    ReArrangeGroups(tournament, tourNumber, groups, pairs, tourPlayers);

                                break;
                            }
                        }

                        if (group.Players.Count == 0)
                            groups.Remove(group);
                    }

                    pairs.Sort();
                }
                return true;
            }
            finally
            {
                RemoveBye(tournament.Players);
                tournament.TakeCurrentRoundInAccount = takeCurrentRoundInAccount;
            }
        }

        private bool CanPairGroup(Tournament tournament, int tourNumber, Group group, Group shifted)
        {
            if (IsImpossbleRestrictions(tournament, tourNumber, group.Players))
                return false;

            if (shifted != null)
            {
                var mixed = new Group();
                mixed.CanMixParts = group.CanMixParts;
                mixed.IsLast = group.IsLast;
                mixed.Players.AddRange(shifted.Players);
                mixed.Players.AddRange(group.Players);
                mixed.MMS = group.MMS;

                int topPlayersCount = shifted.Players.Count;

                while (topPlayersCount > 0 && group.Players.Count > 0)
                {
                    int i = GetSecondPlayerIdx(tournament, tourNumber, mixed);
                    if (i > 0)
                    {
                        mixed.Players.RemoveAt(i);
                        mixed.Players.RemoveAt(0);
                        topPlayersCount--;
                    }
                    else
                        return false;
                }
            }
            else
            {
                while (group.Players.Count > 1)
                {
                    int i = GetSecondPlayerIdx(tournament, tourNumber, group);
                    if (i > 0)
                    {
                        group.Players.RemoveAt(i);
                        group.Players.RemoveAt(0);
                    }
                    else
                        return false;
                }
            }

            return true;
        }

        private Pair GetTopPair(Tournament tournament, int tourNumber, bool isLastTour, Group group)
        {
            Pair result = null;

            var players = group.Players;

            for (int i = 0; i < players.Count / 2; i++)
            {
                int upDownBalance = 0;
                int secondIdx = GetSecondPlayerIdx(tournament, tourNumber, group);
                if (secondIdx > 0)
                {
                    var first = players[0];
                    var second = players[secondIdx];
                    upDownBalance = GetPairedDown(tournament, tourNumber, first, second) ? -1 : 0;

                    int rating =
                        first.Rating +
                        second.Rating;

                    double score =
                        first.GetCoef(tournament, Entity.Score, tourNumber, false) +
                        second.GetCoef(tournament, Entity.Score, tourNumber, false);

                    result = new Pair()
                        {
                            FirstPlayerId = first.Id,
                            SecondPlayerId = second.Id,
                            FirstPlayer = first,
                            SecondPlayer = second,
                            UpDownBalance = upDownBalance,
                            PairingWithBye = (second.Id == -1),
                            Rating = rating,
                            Score = score
                        };

                    break;
                }
            }
            return result;
        }

        private int FindSecondPlayerIdx(Tournament tournament, int tourNumber, Group group, int start, int finish)
        {
            var result = -1;
            var players = group.Players;

            int i = start;
            int step = start < finish ? 1 : -1;

            while (i != finish)
            {
                if (i > 0 && i < players.Count &&
                    //players[i].Id != -1 && -- commented because of cannot pair with Bye
                    !IsImpossble(tournament, tourNumber, players[0], players[i]))
                {
                    return i;
                }

                i += step;
            }

            return result;
        }

        private int GetSecondPlayerIdx(Tournament tournament, int tourNumber, Group group)
        {
            var players = group.Players;
            bool isLastTour = tournament.CurrentRoundNumber == tournament.NumberOfRounds;
            bool isLastGroup = group.IsLast;

            if (players.Count > 1)
            {
                // Check if top player came from higher group 
                bool isShifted = players[0].GetCoef(tournament, Entity.Score, tourNumber) > group.MMS;

                int middle = players.Count / 2;
                if (middle * 2 < players.Count)
                {
                    middle++;
                }

                int start = middle;
                int finish = players.Count;

                if (group.CanMixParts /*&& !isShifted*/)
                {
                    start = 1;
                }

                if (isLastTour /*&& !isShifted*/)
                {
                    start = players.Count;
                    finish = middle - 1;
                }

                if (isShifted)
                {
                    //
                }

                int i = FindSecondPlayerIdx(tournament, tourNumber, group, start, finish);
                if (i > -1)
                    return i;

                // another trying
                if (isShifted)
                {
                    finish = 2;
                    start = middle - 1;

                    i = FindSecondPlayerIdx(tournament, tourNumber, group, start, finish);
                    if (i > -1)
                        return i;
                }
            }

            return -1;
        }

        private List<Group> GetGroups(Tournament tournament, int tourNumber, PlayerList players)
        {
            var groups = new List<Group>();
            if (players.Count > 1)
            {
                foreach (var player in players)
                {
                    var MMS = GetPairingMMS(tournament, tourNumber, player);
                    var group = groups.Find(
                                delegate(Group item) { return MMS == item.MMS; }
                                );
                    if (group == null)
                    {
                        groups.Add(new Group() { MMS = MMS });
                        group = groups[groups.Count - 1];
                    }
                    group.Players.Add(player, true);
                }
            }
            return groups;
        }

        private bool SortPairingPlayers(Tournament tournament, int tourNumber, Group group, Group topGroup)
        {
            bool result = false;
            bool needSort = true;

            int maxIterations = 1000;

            group.Update();

            int i = 0;
            while (needSort && i < maxIterations)
            {

                group.Sort(tournament, tourNumber);

                var newGroup = (Group)group.Clone();

                if (CanPairGroup(tournament, tourNumber, newGroup, topGroup))
                {
                    needSort = false;
                    result = true;
                }
                else
                {
                    //Change sort order
                    //(n-1)!
                    if (IsImpossbleRestrictions(tournament, tourNumber, newGroup.Players) || !group.Reorder())
                    {
                        needSort = false;
                        result = false;
                    }
                }

                i++;
            }

            return result;
        }

        private void ReArrangeGroups(Tournament tournament, int tourNumber, List<Group> groups, PairList pairs, PlayerList tourPlayers)
        {
            var group = groups[0];
            var players = group.Players;

            //cannot do pairing
            if (groups.Count > 1 && players.Count > 0)
            {
                //move player down
                Player downPlayer = players[0];
                groups[1].Players.Insert(0, downPlayer);
                players.Remove(downPlayer);

                SetPlayersOrderByMmsAndRating(tournament, groups[1].Players, tourNumber);
            }
            else
            {
                if (!group.CanMixParts)
                {
                    //Fill free to pair all group members between each other as far as the last group is incomplete
                    group.CanMixParts = true;
                }
                else
                {
                    //the last group - unpair the last pair
                    for (int j = pairs.Count - 1; j >= 0; j--)
                    {
                        var pair = pairs[j];

                        if (pair.SecondPlayerId == -2) continue;

                        var FirstPlayer = tourPlayers.Find(
                            delegate(Player player) { return player.Id == pair.FirstPlayerId; }
                            );
                        var SecondPlayer = tourPlayers.Find(
                            delegate(Player player) { return player.Id == pair.SecondPlayerId; }
                            );

                        players.Add(FirstPlayer, true);
                        players.Add(SecondPlayer, true);
                        pairs.RemoveAt(pairs.Count - 1);

                        SetPlayersOrderByMmsAndRating(tournament, players, tourNumber);
                        break;
                    }

                }
            }
        }

        private int GetPersonalGameComparing(Tournament tournament, int tourNumber, Player player1, Player player2)
        {
            int result = 0;

            for (int i = 0; i < tourNumber; i++)
            {
                var tour = tournament.Tours[i];
                var pair = tour.Pairs.Find(
                    delegate(Pair item)
                    {
                        return item.FirstPlayerId == player1.Id && item.SecondPlayerId == player2.Id ||
                               item.FirstPlayerId == player2.Id && item.SecondPlayerId == player1.Id;
                    }
                );
                if (pair != null)
                {
                    if (pair.FirstPlayerId == player1.Id)
                    {
                        switch (pair.GameResult)
                        {
                            case 1: result++; break;
                            case 2: result--; break;
                        }
                        break;
                    }
                    else
                        switch (pair.GameResult)
                        {
                            case 1: result--; break;
                            case 2: result++; break;
                        }
                    break;
                }

            }
            return result;
        }

        public int ComparePlayers(Tournament tournament, int tourNumber, Player player1, Player player2, bool forPlaceSharing = false)
        {
            if (player1 == null && player2 == null || player1 == player2)
                return 0;
            else
                if (player2 == null)
                    return -1;
                else
                    if (player1 == null)
                        return 1;
            double result = 0;

            foreach (var criteria in tournament.Walllist.SortCriterion)
            {
                switch (criteria.Id)
                {
                    case Entity.Points:
                    case Entity.Score:
                    case Entity.ScoreX:
                    case Entity.SODOS:
                    case Entity.SORP:
                    case Entity.SOS:
                    case Entity.SOSOS:
                        result = player2.GetCoef(tournament, criteria.Id, tourNumber) - player1.GetCoef(tournament, criteria.Id, tourNumber);
                        break;
                    case Entity.PGRC:
                        result = GetPersonalGameComparing(tournament, tourNumber, player2, player1);
                        break;
                }
                if (result != 0)
                    break;
            }

            if (result == 0)
            {
                if (forPlaceSharing)
                    return 0;
            }

            if (result == 0)
                result = player2.Rating - player1.Rating;
            if (result == 0)
            {
                if (player2.Id < 0)
                    result = -1;
                else
                    if (player1.Id < 0)
                        result = 1;
                    else
                        result = player1.Id - player2.Id;
            }
            return Math.Sign(result);
        }

        private int CompareTeams(Tournament tournament, Team team1, Team team2, bool forPlaceSharing = false)
        {
            if (team1 == null && team2 == null || team1 == team2)
                return 0;
            else
                if (team2 == null)
                    return -1;
                else
                    if (team1 == null)
                        return 1;
            double result = 0;

            foreach (var criteria in tournament.Walllist.SortCriterion)
            {
                switch (criteria.Id)
                {
                    case Entity.Points:
                        result = team2.Points - team1.Points;
                        break;
                    case Entity.Score:
                        result = team2.Score - team1.Score;
                        break;
                    case Entity.ScoreX:
                        result = team2.ScoreX - team1.ScoreX;
                        break;
                    case Entity.SODOS:
                        result = team2.Sodos - team1.Sodos;
                        break;
                    case Entity.SORP:
                        result = team2.Sorp - team1.Sorp;
                        break;
                    case Entity.SOS:
                        result = team2.Sos - team1.Sos;
                        break;
                    case Entity.SOSOS:
                        result = team2.Sosos - team1.Sosos;
                        break;
                    case Entity.PGRC:
                        //result = player2.PGRC - team1.PGRC;
                        break;
                }
                if (result != 0)
                    break;
            }

            if (result == 0)
            {
                if (forPlaceSharing)
                    return 0;
            }

            if (result == 0)
                result = string.Compare(team1.Name, team2.Name, true);

            return Math.Sign(result);
        }

        private static int ComparePlayersByRating(Tournament tournament, Player player1, Player player2)
        {
            if (player1 == null && player2 == null)
                return 0;
            else
                if (player2 == null)
                    return -1;
                else
                    if (player1 == null)
                        return 1;

            return Math.Sign(player2.Rating - player1.Rating);
        }

        private static int ComparePlayersByMmsAndRating(Tournament tournament, int tourNumber, Player player1, Player player2)
        {
            if (player1 == null && player2 == null)
                return 0;
            else
                if (player2 == null)
                    return -1;
                else
                    if (player1 == null)
                        return 1;

            double result = player2.GetCoef(tournament, Entity.Score, tourNumber) - player1.GetCoef(tournament, Entity.Score, tourNumber);

            if (result == 0)
            {
                result = player2.Rating - player1.Rating;
            }

            return Math.Sign(result);
        }

        private static int ComparePlayersByStartNumber(Player player1, Player player2, bool useTeam)
        {
            if (player1 == null && player2 == null)
                return 0;
            else
                if (player2 == null)
                    return -1;
                else
                    if (player1 == null)
                        return 1;

            double result = 0;
            if (useTeam)
                result = string.Compare(player1.Team, player2.Team, true);

            if (result == 0)
                result = player1.StartNumber - player2.StartNumber;

            if (result == 0)
            {
                int p1 = player1.Id;
                int p2 = player2.Id;
                if (p1 < 0)
                    p1 = int.MaxValue;
                if (p2 < 0)
                    p2 = int.MaxValue;
                result = p1 - p2;
            }

            return Math.Sign(result);
        }

        private static int ComparePlayersByOrder(Player player1, Player player2, bool useTeam = false)
        {
            if (player1 == null && player2 == null)
                return 0;
            else
                if (player2 == null)
                    return -1;
                else
                    if (player1 == null)
                        return 1;

            double result = 0;
            if (useTeam)
                result = string.Compare(player1.Team, player2.Team, true);

            if (result == 0)
                result = player1.Order - player2.Order;

            if (result == 0)
            {
                int p1 = player1.Id;
                int p2 = player2.Id;
                if (p1 < 0)
                    p1 = int.MaxValue;
                if (p2 < 0)
                    p2 = int.MaxValue;
                result = p1 - p2;
            }

            return Math.Sign(result);
        }

        private static int ComparePlayersForStartScoreCalculation(Tournament tournament, Player player1, Player player2)
        {
            if (player1 == null && player2 == null)
                return 0;
            else
                if (player2 == null)
                    return -1;
                else
                    if (player1 == null)
                        return 1;

            double result = player2.DefaultScore - player1.DefaultScore;

            return Math.Sign(result);
        }

        private int ComparePlayersForAutoGaming(Tournament tournament, int tourNumber, Player player1, Player player2)
        {
            float result = player2.Rating - player1.Rating;
            if (result == 0)
                result = player1.Id - player2.Id;
            return Math.Sign(result);
        }

        public void AutoSetStartScores(Tournament tournament, int[] topLevels, int lowLevel, int topGroupAmount)
        {
            PreparePlayers(tournament);
            var players = GetSortedPlayers(tournament, -1, false, false, true);

            double shift = 0;
            bool prevLow = false;
            for (int i = players.Count - 1; i >= 0; i--)
            {
                var player = players[i];

                double defScore = player.DefaultScore;
                bool lowGroup = defScore <= lowLevel;

                if (lowGroup)
                    shift = 1;
                else
                    if (prevLow)
                    {
                        shift = shift - defScore / 10;
                    }

                player.StartScores = lowGroup ? 0 : defScore / 10 + shift - players[i].ScoreAdjustmentValueEx;
                player.HiGroupMember = player.SuperBarMember;
                player.LoGroupMember = lowGroup;
                prevLow = lowGroup;
            }

            double max = 0;

            if (tournament.UpperMacMahonBar && topLevels.Length > 0)
            {
                if (tournament.UpperMacMahonBarByAmount && topGroupAmount > 0)
                {
                    int amount = 0;
                    double score = 0;
                    foreach (Player player in players)
                    {
                        if (amount >= topGroupAmount)
                            break;
                        else
                        {
                            player.HiGroupMember = true;
                            score = Math.Ceiling(player.DefaultScore / 10);
                        }
                        amount++;
                    }

                    if (amount < players.Count)
                    {
                        double grp = players[amount].StartScores + players[amount].ScoreAdjustmentValueEx;
                        if (score <= grp)
                            score = Math.Truncate(grp + 1);
                    }

                    for (int i = 0; i < amount; i++)
                        players[i].StartScores = score - players[i].ScoreAdjustmentValueEx;

                    max = score;
                }

                if (tournament.UpperMacMahonBarByLevel)
                {
                    Array.Sort(topLevels);

                    foreach (Player player in players)
                    {
                        for (int i = topLevels.Length - 1; i >= 0; i--)
                        {
                            if (player.DefaultScore >= topLevels[i])
                            {
                                int score = topLevels[i] / 10;
                                player.StartScores = score - player.ScoreAdjustmentValueEx;
                                player.HiGroupMember = true;
                                if (max < score)
                                    max = score;
                                break;
                            }
                        }
                    }

                }
            }
            else
            {
                if (tournament.UseMacMahonSuperBar)
                    foreach (Player player in players)
                        if (!player.SuperBarMember)
                        {
                            double score = player.StartScores + player.ScoreAdjustmentValueEx;
                            if (max < score)
                                max = score;
                        }
            }

            if (tournament.UseMacMahonSuperBar)
                foreach (Player player in players)
                    if (player.SuperBarMember)
                        player.StartScores = max + 1 - player.ScoreAdjustmentValueEx;

            if (tournament.UseMacMahonStartScoresWithoutGapsInSequence)
            {
                List<Group> groups = new List<Group>();

                for (int i = players.Count - 1; i >= 0; i--)
                {
                    var player = players[i];

                    var group = groups.Find(
                                delegate(Group item) { return player.StartScores + players[i].ScoreAdjustmentValueEx == item.MMS; }
                                );
                    if (group == null)
                    {
                        groups.Add(new Group() { MMS = player.StartScores - players[i].ScoreAdjustmentValueEx });
                        group = groups[groups.Count - 1];
                    }
                    group.Players.Add(player, true);
                }

                for (int i = 0; i < groups.Count; i++)
                {
                    for (int j = 0; j < groups[i].Players.Count; j++)
                    {
                        var player = groups[i].Players[j];
                        player.StartScores = i - player.ScoreAdjustmentValueEx;
                    }
                }
            }
        }

        public void AutoRandomGaming(Tournament tournament, int tourNumber)
        {
            if (tourNumber < tournament.Tours.Count)
            {
                var pairs = tournament.Tours[tourNumber].Pairs;

                var random = new Random();

                foreach (Pair pair in pairs)
                {
                    if (pair.GameResult == 0 &&
                        !(pair.FirstPlayerId < 0 || pair.SecondPlayerId < 0))
                    {
                        var firstPlayer = tournament.Players.Find(
                            delegate(Player item) { return pair.FirstPlayerId == item.Id; }
                            );
                        var secondPlayer = tournament.Players.Find(
                            delegate(Player item) { return pair.SecondPlayerId == item.Id; }
                            );
                        int r = ComparePlayersForAutoGaming(tournament, tourNumber, secondPlayer, firstPlayer);
                        switch (r)
                        {
                            case 1:
                                pair.GameResult = 1;
                                break;
                            case -1:
                                pair.GameResult = 2;
                                break;
                            default:
                                pair.GameResult = random.Next(1, 3);
                                break;
                        }
                    }
                }
            }
        }

        private void RemoveBye(PlayerList players)
        {
            for (int i = players.Count - 1; i >= 0; i--)
            {
                var player = players[i];
                if (player.Id == -1)
                    players.RemoveAt(i);
            }
        }

        private void AddBye(Tournament tournament, PlayerList players, bool ifNeeded)
        {
            if (!ifNeeded || players.Count % 2 == 1)
            {
                var player = players.Find(p => p.Id == -1);
                if (player == null)
                {
                    Player bye = new Player(tournament) { Id = -1, Surname = "Bye", Name = "+-", Joker = true };
                    players.Add(bye, true);
                }
            }
        }

        private void SetPlayersOrderByRating(Tournament tournament, PlayerList players)
        {
            players.Sort(delegate(Player player1, Player player2)
            {
                return ComparePlayersByRating(tournament, player1, player2);
            });
            for (int i = 0; i < players.Count; i++)
                players[i].Order = i;
        }

        private void SetPlayersOrderByMmsAndRating(Tournament tournament, PlayerList players, int tourNumber)
        {
            players.Sort(delegate(Player player1, Player player2)
            {
                return ComparePlayersByMmsAndRating(tournament, tourNumber, player1, player2);
            });
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] != null)
                {
                    players[i].Order = i;
                }
            }
        }

        private void SetPlayersOrderByStartNumber(PlayerList players, bool useTeam)
        {
            players.Sort(delegate(Player player1, Player player2)
            {
                return ComparePlayersByStartNumber(player1, player2, useTeam);
            });
            for (int i = 0; i < players.Count; i++)
                players[i].Order = i;
        }

        private PlayerList GetSortedPlayers(Tournament tournament, int tourNumber, bool skipUnplayed, bool isPairingMode, bool isStartScoreCalculating = false)
        {
            var players = new PlayerList();
            PairList pairs = null;
            if (tournament.Tours != null &&
                tourNumber >= 0 &&
                tourNumber < tournament.Tours.Count)
                pairs = tournament.Tours[tourNumber].Pairs;

            if (!skipUnplayed || pairs == null)
            {
                players.AddRange(tournament.Players);
            }
            else
            {
                foreach (var player in tournament.Players)
                {
                    if (!player.NotPlayingInRound.Contains(tourNumber + 1))
                        players.Add(player, true);
                }
            }

            // Hide players without name
            // Set PlaceStr to empty
            for (int i = players.Count - 1; i >= 0; i--)
            {
                if (string.IsNullOrEmpty(players[i].Name))
                {
                    players.RemoveAt(i);
                }
                else
                {
                    players[i].PlaceStr = i.ToString();
                }
            }

            if (isPairingMode)
            {
                SetPlayersOrderByRating(tournament, players);
            }
            else
            {

                foreach (var item in players)
                    item.SharedPlace = false;

                players.Sort(delegate(Player player1, Player player2)
                {
                    if (isStartScoreCalculating)
                        return ComparePlayersForStartScoreCalculation(tournament, player1, player2);
                    else
                        return ComparePlayers(tournament, tourNumber, player1, player2);
                });

                bool shared = false;
                int sharedStart = 1;
                for (int i = 1; i < players.Count; i++)
                {
                    if (ComparePlayers(tournament, tourNumber, players[i], players[i - 1], true) == 0)
                    {
                        players[i].SharedPlace = true;
                        if (!shared)
                        {
                            sharedStart = i;
                            shared = true;
                        }
                    }
                    else
                    {
                        if (shared)
                        {
                            for (int j = sharedStart - 1; j < i; j++)
                            {
                                players[j].PlaceStr = sharedStart.ToString() + "-" + i.ToString();
                            }
                            shared = false;
                        }
                    }
                }

                if (shared)
                {
                    for (int j = sharedStart - 1; j < players.Count; j++)
                    {
                        players[j].PlaceStr = sharedStart.ToString() + "-" + players.Count.ToString();
                    }
                }
            }

            return players;
        }

        public bool CleanPairs(Tournament tournament, int tourNumber)
        {
            if (tournament.TournamentSystemRound || tournament.TournamentSystemScheveningen)
            {
                foreach (var items in tournament.Tours)
                    items.Pairs.Clear();
                return true;
            }

            bool result = false;

            if (tournament.Tours != null &&
                tourNumber >= 0 &&
                tourNumber < tournament.Tours.Count)
            {
                var pairs = tournament.Tours[tourNumber].Pairs;
                pairs.Clear();
                result = true;
            }

            return result;
        }

        private void ExcludePairedPlayers(PairList pairs, PlayerList players)
        {
            for (int j = pairs.Count - 1; j >= 0; j--)
            {
                var pair = pairs[j];

                if (pair.SecondPlayerId == -2) continue;

                var FirstPlayer = players.Find(
                    delegate(Player player) { return player.Id == pair.FirstPlayerId; }
                    );
                var SecondPlayer = players.Find(
                    delegate(Player player) { return player.Id == pair.SecondPlayerId; }
                    );

                players.Remove(FirstPlayer);
                players.Remove(SecondPlayer);
            }
        }

        private void PerformColorBalance(Tournament tournament, int TourID, Pair pair, bool swapDirect)
        {
            if (pair == null || pair.FirstPlayerId == -1 || pair.SecondPlayerId == -1)
                return;

            bool needSwapColor = false;

            if (swapDirect)
            {
                needSwapColor = true;
            }
            else
            {
                int firstBalance = 0;
                int secondBalance = 0;

                int firstBalanceCurrentTour = 0;
                int secondBalanceCurrentTour = 0;

                for (int i = 0; i < TourID; i++)
                {
                    if (tournament.Tours != null &&
                        i >= 0 &&
                        i < tournament.Tours.Count)
                    {
                        var pairs = tournament.Tours[i].Pairs;

                        if (
                            null != pairs.Find(
                                delegate(Pair item)
                                { return item.FirstPlayerId == pair.FirstPlayerId; }
                                )
                            )
                        {
                            firstBalance++;
                            if (i == TourID - 1)
                                firstBalanceCurrentTour++;
                        }
                        if (
                            null != pairs.Find(
                                delegate(Pair item)
                                { return item.SecondPlayerId == pair.FirstPlayerId; }
                                )
                            )
                        {
                            firstBalance--;
                            if (i == TourID - 1)
                                firstBalanceCurrentTour--;
                        }
                        if (
                            null != pairs.Find(
                                delegate(Pair item)
                                { return item.FirstPlayerId == pair.SecondPlayerId; }
                                )
                            )
                        {
                            secondBalance++;
                            if (i == TourID - 1)
                                secondBalanceCurrentTour++;
                        }
                        if (
                            null != pairs.Find(
                                delegate(Pair item)
                                { return item.SecondPlayerId == pair.SecondPlayerId; }
                                )
                            )
                        {
                            secondBalance--;
                            if (i == TourID - 1)
                                secondBalanceCurrentTour--;
                        }

                    }
                }

                needSwapColor = firstBalance > secondBalance || (firstBalance == secondBalance && firstBalanceCurrentTour > 0);
                if (!needSwapColor)
                    needSwapColor = firstBalance == secondBalance && firstBalanceCurrentTour == 0 && new Random().Next(10) > 5;
            }

            if (needSwapColor)
            {
                int j = pair.FirstPlayerId;
                pair.FirstPlayerId = pair.SecondPlayerId;
                pair.SecondPlayerId = j;
                pair.UpDownBalance = -pair.UpDownBalance;
            }
        }

        public void UpdateSuperBarMember(Tournament tournament)
        {
            if (tournament.UseMacMahonStartScores && tournament.UseMacMahonStartScoresManually)
            {
                double maxGroup = 0;
                foreach (var player in tournament.Players)
                {
                    var score = player.StartScores + player.ScoreAdjustmentValueEx;
                    if (score > maxGroup)
                        maxGroup = score;
                }

                if (maxGroup > 0)
                {
                    foreach (var player in tournament.Players)
                    {
                        if (player.StartScores + player.ScoreAdjustmentValueEx == maxGroup)
                            player.HiGroupMember = true;
                    }
                }
            }
        }

        public void UpdateStartScores(Tournament tournament)
        {
            if (tournament == null) return;
            if (!tournament.UseMacMahonStartScores)
            {
                var players = tournament.Players;
                foreach (var player in players)
                {
                    player.StartScores = 0;
                    player.HiGroupMember = false;
                }
            }
            else
                if (!tournament.UseMacMahonStartScoresManually)
                {
                    int[] arr = new int[1];
                    arr[0] = -1;
                    int lowLevel = -1;
                    int topAmount = -1;

                    if (tournament.UpperMacMahonBar)
                    {
                        if (tournament.UpperMacMahonBarByLevel)
                        {
                            int topLevel = (int)Player.GetDefaultScore(PlayerInfo.GetLevelByRank(tournament.UpperMacMahonBarLevel));
                            if (topLevel < 0)
                            {
                                topLevel = 0;
                            }

                            arr[0] = topLevel;
                        }
                        else
                            topAmount = tournament.UpperMacMahonBarAmount;

                    }

                    if (tournament.LowerMacMahonBar)
                    {
                        lowLevel = (int)Player.GetDefaultScore(PlayerInfo.GetLevelByRank(tournament.LowerMacMahonBarLevel));
                    }


                    AutoSetStartScores(tournament, arr, lowLevel, topAmount);
                }
                else
                    UpdateSuperBarMember(tournament);
        }

        private void AddPair(Tournament tournament, int tourNumber, Pair pair, PairList pairs, PlayerList players)
        {
            pair.AllowJigo = tournament.AllowJigo;

            int komi;
            bool swap;
            pair.Handicap = GetHandicap(tournament, tourNumber, pair.FirstPlayerId, pair.SecondPlayerId, out komi, out swap);
            pair.AdditionalKomi = komi;
            pairs.Add(pair, false);

            PerformColorBalance(tournament, tourNumber, pair, swap);

            players.Remove(pair.FirstPlayer);
            players.Remove(pair.SecondPlayer);
        }

        public void AutoRandomStartNumbers(PlayerList players, bool useTeam)
        {
            var rnd = new Random();
            foreach (var item in players)
                item.Order = rnd.Next(players.Count);

            var list = new PlayerList();
            list.AddRange(players);

            list.Sort(delegate(Player player1, Player player2)
            {
                return ComparePlayersByOrder(player1, player2, useTeam);
            });

            string team = string.Empty;
            int j = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (useTeam)
                {
                    if (team != list[i].Team)
                        j = 0;
                    team = list[i].Team;
                }
                list[i].StartNumber = j + 1;
                j++;
            }
        }

        private bool AutoPairingRoundRobin(Tournament tournament)
        {
            var players = new PlayerList();
            players.AddRange(tournament.Players);
            AddBye(tournament, players, true);

            SetPlayersOrderByStartNumber(players, false);

            foreach (var item in tournament.Tours)
                item.Pairs.Clear();

            int N = tournament.NumberOfPlayers;

            for (int i = 1; i <= N; i++)
                for (int j = 1; j <= N; j++)
                {
                    if (i == j) continue;

                    int id1 = i - 1;
                    int id2 = j - 1;
                    int sum = i + j;
                    bool swap = false;
                    int tourN = 0;

                    if (i != N && j != N)
                    {
                        if (sum <= N)
                            tourN = sum - 1;
                        else
                            tourN = sum - N;
                        swap = sum % 2 == 0;
                    }
                    else
                    {
                        if (i == N)
                        {
                            sum = j * 2;
                            swap = j >= N / 2;
                        }
                        else
                        {
                            sum = i * 2;
                            swap = i >= N / 2;
                        }

                        if (sum <= N)
                            tourN = sum - 1;
                        else
                            tourN = sum - N;

                    }

                    if (swap)
                    {
                        id1 = j - 1;
                        id2 = i - 1;
                    }

                    if (id1 < players.Count && id2 < players.Count)
                    {
                        int rating =
                            players[id1].Rating +
                            players[id2].Rating;

                        double score =
                            players[id1].GetCoef(tournament, Entity.Score, tourN - 1, false) +
                            players[id2].GetCoef(tournament, Entity.Score, tourN - 1, false);

                        if (tournament.Tours[tourN - 1].Pairs.Find(players[id1].Id) == null && tournament.Tours[tourN - 1].Pairs.Find(players[id2].Id) == null)
                        {
                            var pair = new Pair()
                            {
                                FirstPlayerId = players[id1].Id,
                                SecondPlayerId = players[id2].Id,
                                UpDownBalance = 0,
                                PairingWithBye = (players[id2].Id == -1),
                                Rating = rating,
                                Score = score
                            };
                            int komi;
                            bool swap2;
                            pair.Handicap = GetHandicap(tournament, tourN, pair.FirstPlayerId, pair.SecondPlayerId, out komi, out swap2);
                            pair.AdditionalKomi = komi;
                            if (swap2)
                                PerformColorBalance(tournament, tourN - 1, pair, true);

                            tournament.Tours[tourN - 1].Pairs.Add(pair);
                        }
                    }
                }

            foreach (var item in tournament.Tours)
                item.Pairs.Sort();

            return true;

        }

        private bool AutoPairingScheveningenSystem(Tournament tournament)
        {
            var players = new PlayerList();
            players.AddRange(tournament.Players);

            var team1 = new PlayerList();
            var team2 = new PlayerList();
            string defTeam = null;
            string alterTeam = null;
            foreach (var item in players)
            {
                string s = string.IsNullOrEmpty(item.Team) ? string.Empty : item.Team;
                if (defTeam == null)
                    defTeam = s;
                if (s == defTeam)
                    team1.Add(item, true);
                else
                {
                    if (string.IsNullOrEmpty(alterTeam))
                        alterTeam = s;
                    team2.Add(item, true);
                }
            }

            if (team1.Count < team2.Count)
                AddBye(tournament, team1, false);
            if (team2.Count < team1.Count)
                AddBye(tournament, team2, false);

            if (team1.Count != team2.Count)
                return false;

            if (team1.Count > tournament.NumberOfTeamPlayers)
                return false;

            SetPlayersOrderByStartNumber(players, true);

            foreach (var item in tournament.Tours)
                item.Pairs.Clear();

            int N = tournament.NumberOfTeamPlayers;

            for (int i = 1; i <= N; i++)
                for (int j = N + 1; j <= N * 2; j++)
                {
                    int id1 = i + j - N - 2;
                    if (id1 >= N)
                        id1 = id1 - N;
                    int id2 = j - 1;

                    bool swap = i % 2 == 0;
                    if (swap)
                    {
                        int tmp = id1;
                        id1 = id2;
                        id2 = tmp;
                    }

                    int tourN = i;

                    if (id1 < players.Count && id2 < players.Count)
                    {
                        int rating =
                            players[id1].Rating +
                            players[id2].Rating;

                        double score =
                            players[id1].GetCoef(tournament, Entity.Score, tourN - 1, false) +
                            players[id2].GetCoef(tournament, Entity.Score, tourN - 1, false);

                        if (tournament.Tours[tourN - 1].Pairs.Find(players[id1].Id) == null && tournament.Tours[tourN - 1].Pairs.Find(players[id2].Id) == null)
                        {
                            var pair = new Pair()
                            {
                                FirstPlayerId = players[id1].Id,
                                SecondPlayerId = players[id2].Id,
                                UpDownBalance = 0,
                                PairingWithBye = (players[id2].Id == -1),
                                Rating = rating,
                                Score = score
                            };
                            int komi;
                            bool swap2;
                            pair.Handicap = GetHandicap(tournament, tourN, pair.FirstPlayerId, pair.SecondPlayerId, out komi, out swap2);
                            pair.AdditionalKomi = komi;
                            if (swap2)
                                PerformColorBalance(tournament, tourN - 1, pair, true);

                            tournament.Tours[tourN - 1].Pairs.Add(pair);
                        }
                    }
                }

            foreach (var item in tournament.Tours)
                item.Pairs.Sort();

            return true;

        }

        private Group GetGroupToCombinePairs(Tournament tournament, int tourNumber, List<Group> groups, Group group, bool downList)
        {
            Group result = null;

            // try to combine unpairing group with lower ones
            int maxShift = 2;

            int nextGroupId = !downList ? groups.Count - 2 : 1;
            int i = 0;
            while (result == null && (downList || i < maxShift) && nextGroupId < groups.Count && nextGroupId > -1)
            {
                bool canCombineGroups = SortPairingPlayers(tournament, tourNumber, groups[nextGroupId], group);
                if (canCombineGroups)
                    result = groups[nextGroupId];
                else
                    nextGroupId += !downList ? -1 : 1;
                i++;
            }

            return result;
        }

        private void ApplyGroupCombining(Group source, Group target)
        {
            target.Players.AddRange(source.Players);
            source.Players.Clear();
        }

        #endregion

        #region Rating calculation

        private double[,] _rd = 
        { 
            //SA = 1, 0 or 0.5 in case of jigo
            //GoR, con, a, SE(100)
            {  100, 116, 200, 37.8 }, 
            {  200, 110, 195, 37.5 }, 
            {  300, 105, 190, 37.1 },
            {  400, 100, 185, 36.8 },
            {  500,  95, 180, 36.5 },
            {  600,  90, 175, 36.1 }, 
            {  700,  85, 170, 35.7 }, 
            {  800,  80, 165, 35.3 }, 
            {  900,  75, 160, 34.9 }, 
            { 1000,  70, 155, 34.4 }, 
            { 1100,  65, 150, 33.9 }, 
            { 1200,  60, 145, 33.4 }, 
            { 1300,  55, 140, 32.9 }, 
            { 1400,  51, 135, 32.3 }, 
            { 1500,  47, 130, 31.7 }, 
            { 1600,  43, 125, 31.0 }, 
            { 1700,  39, 120, 30.3 }, 
            { 1800,  35, 115, 29.5 }, 
            { 1900,  31, 110, 28.7 }, 
            { 2000,  27, 105, 27.8 }, 
            { 2100,  24, 100, 26.9 }, 
            { 2200,  21,  95, 25.9 }, 
            { 2300,  18,  90, 24.8 }, 
            { 2400,  15,  85, 23.6 }, 
            { 2500,  13,  80, 22.3 }, 
            { 2600,  11,  75, 20.9 }, 
            { 2700,  10,  70, 19.3 } 
        };

        private int[] _under100_9 = { 2, 1, 2, 3 };
        private int[] _under100_13 = { 3, 2, 3, 4 };
        private int[] _under100_19 = { 5, 3, 5, 7 };

        private double GetRatingValue(double rating, int col)
        {
            double result = 0;

            int row = -1;

            for (int i = 0; i < _rd.GetLength(0); i++)
            {
                if (rating <= _rd[i, 0])
                {
                    row = i;
                    break;
                }
            }

            if (row == 0)
            {
                result = _rd[0, col];
            }
            else
            {
                if (row == -1)
                {
                    result = _rd[_rd.GetLength(0) - 1, col];
                }
                else
                {
                    result = _rd[row - 1, col] + ((_rd[row, col] - _rd[row - 1, col])) * (rating - _rd[row - 1, 0]) / (_rd[row, 0] - _rd[row - 1, 0]);
                    //result = _rd[row - 1, col] + ((_rd[row, col] - _rd[row - 1, col]) / 2);
                }
            }
            return result;
        }

        private void PrepareTourInfo(Tournament tournament)
        {
            foreach (var player in tournament.Players)
            {
                while (player.TourInfoList.Count < tournament.Tours.Count)
                    player.TourInfoList.Add(new TourInfo());
                while (player.TourInfoList.Count > tournament.Tours.Count)
                    player.TourInfoList.RemoveAt(player.TourInfoList.Count - 1);

                player.RatingC = player.Rating;
                player.RatingAbnormal = false;
                player.RatingBonus = 0;

                foreach (var info in player.TourInfoList)
                {
                    info.Rating = 0;
                    info.Diff = 0;
                    info.Competitor = null;
                    info.CompetitorId = 0;

                    info.MMS = -1;
                    info.MMSX = -1;
                    info.MMSM = -1;
                    info.Points = -1;
                    info.SODOS = -1;
                    info.SOS = -1;
                    info.SOSOS = -1;
                    info.SODOS = -1;
                    info.SOUD = -1;
                }
            }

        }

        public void CalculateScoreInfo(Tournament tournament, int tourNumber, PlayerList players = null)
        {
            int N = tourNumber;
            if (N >= tournament.Tours.Count)
                N = tournament.Tours.Count - 1;

            if (N < 0)
                return;

            if (tournament.Tours.Count == 0)
                return;

            if (players == null)
            {
                players = tournament.Players;
            }

            // Rearrange TourInfoList for each player
            foreach (var player in players)
            {
                while (player.TourInfoList.Count < tournament.Tours.Count)
                    player.TourInfoList.Add(new TourInfo());
                while (player.TourInfoList.Count > tournament.Tours.Count)
                    player.TourInfoList.RemoveAt(player.TourInfoList.Count - 1);

                foreach (var info in player.TourInfoList)
                {
                    info.MMS = -1;
                    info.MMSX = -1;
                    info.MMSM = -1;
                    info.SOS = -1;
                    info.SOSOS = -1;
                    info.SODOS = -1;
                    info.SOUD = -1;
                    info.Points = -1;
                }
            }

            // 1st level values
            for (int i = 0; i <= N; i++)
            {
                foreach (var player in players)
                {
                    var info = player.TourInfoList[i];

                    info.MMS = player.GetCoef(tournament, Entity.Score, i, false);
                    info.MMSX = player.GetCoef(tournament, Entity.ScoreX, i, false);
                    info.MMSM = player.GetCoef(tournament, Entity.ScoreM, i, false);
                    info.SOS = player.GetCoef(tournament, Entity.SOS, i, false);
                    info.Points = player.GetCoef(tournament, Entity.Points, i, false);
                    info.SOUD = player.GetCoef(tournament, Entity.SOUD, i, false);
                }
            }
            // 2nd level values
            for (int i = 0; i <= N; i++)
            {
                foreach (var player in players)
                {
                    var info = player.TourInfoList[i];

                    info.SOSOS = player.GetCoef(tournament, Entity.SOSOS, i, false);
                }
            }
            // 3rd level values
            for (int i = 0; i <= N; i++)
            {
                foreach (var player in players)
                {
                    var info = player.TourInfoList[i];

                    info.SODOS = player.GetCoef(tournament, Entity.SODOS, i, false);
                }
            }
        }

        public void CalculateRatingInfo(Tournament tournament, int tourNumber, RtKind ratingKind = RtKind.ua)
        {
            int N = tourNumber;
            if (N >= tournament.Tours.Count)
                N = tournament.Tours.Count - 1;

            if (N < 0)
                return;

            if (tournament.Tours.Count == 0)
                return;

            var players = GetSortedPlayers(tournament, N, false, false);

            foreach (var player in players)
            {
                player.IsCalculating = true;
            }

            PrepareTourInfo(tournament);

            bool hasAbnormal = false;
            int abnormalIterations = 10;

            do
            {
                for (int i = 0; i <= N; i++)
                {
                    var tour = tournament.Tours[i];

                    foreach (var player in players)
                    {

                        double epsilon = 0;

                        switch (ratingKind)
                        {
                            case RtKind.eu: epsilon = 0.016; break;
                        }

                        double Rp = i == 0 ? player.RatingC : player.TourInfoList[i - 1].Rating;
                        double Ra = player.RatingC;

                        switch (ratingKind)
                        {
                            case RtKind.ua: Ra = Rp; break;
                        }

                        var pair = tour.Pairs.Find(player.Id);
                        if (pair == null)
                        {
                            player.TourInfoList[i].Rating = Rp;
                            continue;
                        }
                        int competitorId = pair.FirstPlayerId == player.Id ? pair.SecondPlayerId : pair.FirstPlayerId;
                        if (competitorId < 0)
                        {
                            player.TourInfoList[i].Rating = Rp;
                            continue;
                        }

                        var competitor = tournament.Players.Find(p => p != null && p.Id == competitorId);
                        if (competitor == null)
                        {
                            player.TourInfoList[i].Rating = Rp;
                            continue;
                        }

                        double Rb = competitor.RatingC;

                        switch (ratingKind)
                        {
                            case RtKind.ua: if (i > 0) Rb = competitor.TourInfoList[i - 1].Rating; break;
                        }

                        double Sa = -2;

                        if (pair.FirstPlayerId == player.Id)
                        {
                            switch (pair.GameResult)
                            {
                                case 1: Sa = 1; break;
                                case 2: Sa = 0; break;
                                case 5: Sa = 0.5; break;
                            }
                        }
                        else
                            switch (pair.GameResult)
                            {
                                case 1: Sa = 0; break;
                                case 2: Sa = 1; break;
                                case 5: Sa = 0.5; break;
                            }

                        if (Sa == -2)
                        {
                            player.TourInfoList[i].Rating = Ra;
                            continue;
                        }

                        if (Rb < 100 && Ra >= 100)
                        {
                            Rb = 0;
                        }

                        //if (pair.Handicap > 0)
                        //{
                        //    double Rmax = Rb > Ra ? Rb : Ra;
                        //    Ra = Rmax;
                        //    Rb = Rmax;
                        //}

                        double Rmin = Rb < Ra ? Rb : Ra;

                        if (Rmin < 100)
                        {
                            Rmin = 100;
                        }

                        double Rbase = Ra;

                        //if (pair.Handicap > 0)
                        //{
                        //    Ra = Rmin;
                        //    Rb = Rmin;
                        //}

                        double a = 205 - Rmin / 20;

                        switch (ratingKind)
                        {
                            case RtKind.eu: a = GetRatingValue(Rmin, 2); break;
                        }

                        double d = Ra - Rb;

                        double Smin = 1 / (Math.Exp(Math.Abs(d) / a) + 1) - epsilon / 2;

                        double Se = d < 0 ? Smin : 1 - epsilon - Smin;

                        double con = GetRatingValue(Ra, 1);
                        double coef = 0;
                        double cBoard = tournament.Boardsize == 9 ? 1.0 / 3 : tournament.Boardsize == 13 ? 1.0 / 2 : 1;
                        double cHandicap = 1;

                        if (pair.Handicap > 0)
                        {
                            cHandicap = 1 - (double)pair.Handicap / 10;
                        }

                        switch (ratingKind)
                        {
                            case RtKind.ua: coef = (3100 - Ra) / 50000; break;
                        }

                        double Rn = Rbase + con * cBoard * cHandicap * (Sa - Se + coef);

                        int bonus = 0;

                        switch (ratingKind)
                        {
                            case RtKind.ua:
                                {
                                    if (Ra < 100)
                                    {
                                        if (Sa == 0)
                                            Rn = Ra + 1;
                                        else
                                            if (Sa == 1)
                                            {
                                                var bonusTable = tournament.Boardsize == 9 ? _under100_9 : tournament.Boardsize == 13 ? _under100_13 : _under100_19;

                                                Rn = Ra + bonusTable[0];
                                                if (Rb > 400)
                                                    bonus += bonusTable[3];
                                                else
                                                    if (Rb > 200)
                                                        bonus += bonusTable[2];
                                                    else
                                                        if (Rb > 100)
                                                            bonus += bonusTable[1];
                                            }
                                            else
                                                Rn = Ra;
                                        Rn += bonus;
                                    }

                                    if (player.Rating >= 100 && Rn < 100)
                                    {
                                        Rn = 100;
                                    }

                                    break;
                                }
                        }

                        switch (ratingKind)
                        {
                            //case RtKind.ua:
                            case RtKind.eu:
                                {
                                    double diff = 0;
                                    for (int j = 0; j < i; j++)
                                        diff += player.TourInfoList[j].Diff;
                                    Rn += diff;
                                    break;
                                }
                        }

                        player.TourInfoList[i].Rating = Rn;
                        player.TourInfoList[i].Diff = Rn - Rp;
                        player.RatingBonus += bonus;

                    }
                }

                hasAbnormal = false;

                if (!tournament.HandicapUsed)
                {
                    foreach (var player in players)
                    {
                        double R = player.TourInfoList[N].Rating;
                        double R0 = player.TourInfoList[0].Rating;
                        double K1 = GetRatingValue(R, 1);
                        double K2 = (3100 - R0) / 50000.0;
                        int partCnt = N + 1 - player.NotPlayingInRound.Where(x => x <= N).Count();

                        switch (ratingKind)
                        {
                            case RtKind.ua:
                                {
                                    double dRmax = 0.45 * partCnt * K1 * (1 + K2);
                                    if (tournament.Tours.Count > 2) // in another case it is nonsense
                                        if (R - R0 > dRmax)
                                        {
                                            player.RatingC = R;
                                            player.RatingAbnormal = true;
                                            hasAbnormal = true;
                                        }

                                    // Set abnormal edge depended on board size
                                    var bonusEdge = tournament.Boardsize == 9 ? 4 : tournament.Boardsize == 13 ? 6 : 9;

                                    if (player.RatingBonus > bonusEdge)
                                    {
                                        player.RatingC = R;
                                        player.RatingBonus = 0;
                                        player.RatingAbnormal = true;
                                        hasAbnormal = true;
                                    }
                                    break;
                                }
                        }
                    }
                }
            }
            while (hasAbnormal && --abnormalIterations > 0);

            foreach (var player in players)
            {
                player.IsCalculating = false;
            }
        }

        public List<RatingSystem.RatingRec> GetRatingGrowth(Tournament tournament, int tourNumber, int playerId)
        {
            List<RatingSystem.RatingRec> result = new List<RatingSystem.RatingRec>();

            if (tournament != null)
            {
                var player = tournament.Players.Find(p => p != null && p.Id == playerId);

                if (player != null)
                {
                    if (tourNumber > tournament.Tours.Count)
                        tourNumber = tournament.Tours.Count;

                    CalculateRatingInfo(tournament, tourNumber);

                    result.Insert(0, new RatingSystem.RatingRec() { Rating = player.Rating, PersonId = -1 });

                    for (int i = 0; i < tourNumber; i++)
                        result.Insert(0, new RatingSystem.RatingRec() { Rating = player.TourInfoList[i].Rating, PersonId = -1 });
                }
            }

            return result;
        }

        #endregion

        #region TournamentTable

        public string GetTourName(Tournament tournament)
        {
            if (tournament != null)
            {
                StringBuilder sb = new StringBuilder();

                if (!tournament.TakeCurrentRoundInAccount)
                    sb.Append(LangResources.LR.Before);
                else
                    sb.Append(LangResources.LR.After);
                sb.Append(" ");

                switch (Translator.Language)
                {
                    case "ru":
                    case "uk":
                        sb.Append(Tour.ToRoman(tournament.CurrentRoundNumber)).Append(" ");
                        if (!tournament.TakeCurrentRoundInAccount)
                            sb.Append(LangResources.LR.Round3);
                        else
                            sb.Append(LangResources.LR.Round2);
                        break;
                    default:
                        sb.Append(LangResources.LR.Round).Append(" ").Append(Tour.ToRoman(
                            tournament.CurrentRoundNumber > tournament.Tours.Count ? tournament.Tours.Count : tournament.CurrentRoundNumber)
                        );
                        break;
                }
                return sb.ToString();
            }
            else
                return string.Empty;
        }

        private string FloatToHtm(double value, bool isPlainText)
        {
            if (isPlainText)
                return FloatToStr(value);
            int i = (int)Math.Floor(value);
            if (value > i && i == 0)
                return "&frac12;";
            else
                return i.ToString() + (value > i ? "&frac12;" : string.Empty);
        }

        private string FloatToHtm2(double value, bool isPlainText)
        {
            if (isPlainText)
                return FloatToStr(value);
            int i = (int)Math.Floor(value);
            if (value > i && i == 0)
                return "½";
            else
                return i.ToString() + (value > i ? "½" : string.Empty);
        }

        private string FloatToStr(double value, bool forRating = false)
        {
            int i = (int)Math.Floor(value);
            string frac12 = (forRating ? "." : Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator) + "5";
            if (value > i && i == 0)
            {
                return "0" + frac12;
            }
            else
                return i.ToString() + (value > i ? frac12 : string.Empty);
        }

        public void ExportWallList(Tournament tournament, string fileName)
        {
            string ext = System.IO.Path.GetExtension(fileName).ToLower();
            string str;
            int num = tournament.CurrentRoundNumber;
            switch (ext)
            {
                case ".xlsx":
                    var table = GetTournamentDataTable(tournament, num, tournament.Walllist.Columns, 0, 0, true).Table;

                    foreach (System.Data.DataColumn item in table.Columns)
                    {
                        string s = GetColumnNameByKey(item.ColumnName);
                        if (s != null)
                            item.ColumnName = s;
                    }

                    new ExcelExport().ExportDataTable(table, fileName);

                    return;
                case ".txt":
                    str = GetTournamentHtmlText(tournament, num, true);
                    break;
                default:
                    str = GetTournamentHtmlText(tournament, num, false);
                    break;
            }

            System.IO.File.WriteAllText(fileName, str);
        }

        public void ExportRoundRobinWallList(Tournament tournament, string fileName)
        {
            string ext = System.IO.Path.GetExtension(fileName).ToLower();
            string str;

            switch (ext)
            {
                case ".txt":
                    str = GetTournamentRoundRobinHtmlText(tournament, true);
                    break;
                default:
                    str = GetTournamentRoundRobinHtmlText(tournament, false);
                    break;
            }

            System.IO.File.WriteAllText(fileName, str);
        }

        public void ExportWallListForRating(Tournament tournament, string fileName, bool useLocalNames)
        {
            string ext = System.IO.Path.GetExtension(fileName).ToLower();
            string str = GetTournamentTextForRating(tournament, useLocalNames);
            System.IO.File.WriteAllText(fileName, str);
        }

        public void ExportPairing(Tournament tournament, string fileName)
        {
            string ext = System.IO.Path.GetExtension(fileName).ToLower();
            string str;
            int num = tournament.CurrentRoundNumber - 1;

            switch (ext)
            {
                case ".xlsx":
                    var table = GetPairingDataTable(tournament, num).Table;

                    foreach (System.Data.DataColumn item in table.Columns)
                    {
                        string s = GetColumnNameByKey(item.ColumnName);
                        if (s != null)
                            item.ColumnName = s;
                    }

                    new ExcelExport().ExportDataTable(table, fileName);

                    return;
                case ".txt":
                    str = GetPairingHtmlText(tournament, num, true);
                    break;
                default:
                    str = GetPairingHtmlText(tournament, num, false);
                    break;
            }
            System.IO.File.WriteAllText(fileName, str);

        }

        private string IntToSortString(int value, int maxValue)
        {
            int length = 0;
            while (maxValue > 0)
            {
                length++;
                maxValue /= 10;
            }

            StringBuilder sb = new StringBuilder(length);
            string s = value.ToString();
            for (int i = 0; i < length - s.Length; i++)
                sb.Append('0');
            sb.Append(s);
            return sb.ToString();
        }

        public string GetClubEgdName(Tournament tournament, string countryCode, string clubName)
        {
            string result = string.Empty;
            List<Club> clubs = tournament.Countries.GetClubs(countryCode);
            if (clubs != null)
            {
                var club = clubs.Find(item => item != null &&
                    (
                        item.Name == clubName
                        || item.NameEn == clubName
                        || item.NameUa == clubName
                    ));
                if (club != null)
                    result = club.EGDName;
            }
            return result;
        }

        public void UpdateUpDownBalance(Tournament tournament, PlayerList players, int tourNum)
        {
            int j = 0;
            foreach (Tour tour in tournament.Tours)
            {
                FillPairsData(tournament, tour, tournament.Players);

                foreach (Pair pair in tour.Pairs)
                {
                    pair.UpDownBalance = GetUpDownBalance(tournament, j, pair.FirstPlayer, pair.SecondPlayer);
                    pair.UpDownBalanceCompensedFirst = false;
                    pair.UpDownBalanceCompensedSecond = false;
                }
                j++;
            }

            foreach (Player player in players)
            {
                for (int i = 0; i < tourNum; i++)
                {
                    var pair = tournament.Tours[i].Pairs.Find(player.Id);
                    if (pair != null)
                    {
                        if (pair.UpDownBalance != 0 && !pair.GetUpDownCompensed(player.Id))
                        {
                            int playerBalance = pair.GetUpDownBalance(player.Id);

                            for (int k = 0; k < i; k++)
                            {
                                var pairBefore = tournament.Tours[k].Pairs.Find(player.Id);

                                if (pairBefore != null && pairBefore.GetUpDownBalance(player.Id) == -playerBalance && !pairBefore.GetUpDownCompensed(player.Id))
                                {
                                    pair.SetUpDownCompensed(player.Id, true);
                                    pairBefore.SetUpDownCompensed(player.Id, true);
                                    break;
                                }
                            }

                        }
                    }
                }
            }
        }

        public List<Team> GetTeams(PlayerList players)
        {
            var result = new List<Team>();

            Team team = null;
            foreach (var player in players)
            {
                if (!string.IsNullOrEmpty(player.Team))
                {
                    team = result.Find(item => item.Name == player.Team);
                    if (team == null)
                    {
                        team = new Team() { Name = player.Team };
                        result.Add(team);
                    }
                    team.Players.Add(player, true);
                }
            }

            return result;
        }

        private void FillRowInfo(Tournament tournament, int tourNumber, System.Data.DataRow row, int rowNumber, Player player, WallListColumns columns, List<Team> teams, PlayerList players, int num, bool export)
        {
            double value;
            var team = teams == null ? null : teams.Find(item => item.Name == player.Team);
            foreach (var col in columns)
            {
                switch (col.Id)
                {
                    case Entity.Place:
                        row["Place"] = /*!export && */player.SharedPlace ? "(" + rowNumber.ToString() + ")" : rowNumber.ToString(); break;
                    case Entity.Name:
                        row["Name"] = player.Name; break;
                    case Entity.Country:
                        row["Country"] = player.IsCountrySet ? player.Country.ToString() : string.Empty; break;
                    case Entity.City:
                        row["City"] = player.City; break;
                    case Entity.Club:
                        row["Club"] = player.Club; break;
                    case Entity.Team:
                        row["Team"] = player.Team; break;
                    case Entity.Coach:
                        row["Coach"] = player.Coach; break;
                    case Entity.Rank:
                        row["Rank"] = player.RankExt; break;
                    case Entity.Rating:
                        row["Rating"] = player.Rating.ToString(); break;
                    case Entity.Grade:
                        row["Grade"] = Grade.GetShortName(player.Grade); break;
                    case Entity.Group:
                        {
                            row["Group"] = FloatToStr(player.StartScores + player.ScoreAdjustmentValueEx);
                            if (tournament.LowerMacMahonBar && player.LoGroupMember)
                                row["SuperBarMember"] = "2";
                            if (tournament.UpperMacMahonBar && player.HiGroupMember)
                                row["SuperBarMember"] = "1";
                            if (tournament.UseMacMahonSuperBar && player.SuperBarMember)
                                row["SuperBarMember"] = "3";
                            break;
                        }

                    case Entity.Tours:
                        {
                            for (int i = 0; i <= (tourNumber >= tournament.Tours.Count ? tournament.Tours.Count - 1 : tourNumber); i++)
                            {
                                string cellText = "--";
                                var tour = tournament.Tours[i];
                                var pair = tour.Pairs.Find(player.Id);

                                int competitorId = -1;
                                if (pair != null)
                                {
                                    competitorId = pair.FirstPlayerId == player.Id ? pair.SecondPlayerId : pair.FirstPlayerId;
                                    switch (competitorId)
                                    {
                                        case -2:
                                            cellText = "--";
                                            break;
                                        case -1:
                                            cellText = "+-";//"½";
                                            break;

                                        default:
                                            int competitorIdx = players.FindIndex(delegate(Player item) { return item.Id == competitorId; });
                                            if (competitorIdx > -1)
                                            {
                                                StringBuilder sb = new StringBuilder();
                                                cellText = string.Empty;

                                                if (!export)
                                                {
                                                    if (pair.UpDownBalance != 0)
                                                    {
                                                        if (pair.GetUpDownBalance(player.Id) < 0)
                                                            sb.Append(pair.GetUpDownCompensed(player.Id) ? "↓₌" : "↓");
                                                        else
                                                            sb.Append(pair.GetUpDownCompensed(player.Id) ? "↑₌" : "↑");
                                                    }
                                                }

                                                sb.Append(competitorIdx + 1);
                                                sb.Append(pair.GetGameResultText(player.Id, tournament.Walllist.ShowColors, tournament.HandicapUsed || pair.ForcedPairing, false, false));

                                                if (pair.ResultByReferee)
                                                    sb.Append("!");

                                                cellText = sb.ToString();
                                            }
                                            break;
                                    }
                                }
                                else
                                {
                                    if (!player.NotPlayingInRound.Contains(i + 1))
                                        cellText = "???";
                                }

                                row[Tour.ToRoman(i + 1)] = cellText;
                            }
                            break;
                        }
                    case Entity.Points:
                        value = player.GetCoef(tournament, col.Id, tourNumber);
                        if (team != null) team.Points += value;
                        row["Points"] = FloatToStr(value); break;
                    case Entity.Score:
                        value = player.GetCoef(tournament, col.Id, tourNumber);
                        if (team != null) team.Score += value;
                        row["Score"] = FloatToStr(value); break;
                    case Entity.SOS:
                        value = player.GetCoef(tournament, col.Id, tourNumber);
                        if (team != null) team.Sos += value;
                        row["SOS"] = FloatToStr(value); break;
                    case Entity.SODOS:
                        value = player.GetCoef(tournament, col.Id, tourNumber);
                        if (team != null) team.Sodos += value;
                        row["SODOS"] = FloatToStr(value); break;
                    case Entity.SOSOS:
                        value = player.GetCoef(tournament, col.Id, tourNumber);
                        if (team != null) team.Sosos += value;
                        row["SOSOS"] = FloatToStr(value); break;
                    case Entity.SOUD:
                        value = player.GetCoef(tournament, col.Id, tourNumber);
                        if (team != null) team.Soud += (int)Math.Round(Math.Round(value, 0), 0);
                        row["SOUD"] = FloatToStr(value); break;
                    case Entity.ScoreX:
                        value = player.GetCoef(tournament, col.Id, tourNumber);
                        if (team != null) team.ScoreX += Math.Round(value, 0);
                        row["ScoreX"] = FloatToStr(value); break;
                    case Entity.SORP:
                        value = player.GetCoef(tournament, col.Id, tourNumber);
                        if (team != null) team.Sorp += (int)Math.Round(value, 0);
                        row["SORP"] = FloatToStr(value); break;
                    case Entity.NewRating:
                        value = player.GetCoef(tournament, col.Id, tourNumber);
                        double delta = Math.Round(value - player.Rating, 0);
                        if (!export)
                        {
                            row["RN"] = value;
                        }
                        row["NR"] =
                            "(" +
                            (delta == 0 ? "=" : delta < 0 ? "-" + (-delta).ToString() : "+" + delta.ToString()) +
                            (player.RatingAbnormal ? "!" : string.Empty) +
                            ") " +
                            //Math.Round(value, 4);
                            FloatToStr(Math.Round(value, 0));
                        break;
                    default: continue;
                }
            }
        }

        private void FillRowInfo(System.Data.DataRow row, Team team, WallListColumns columns)
        {
            foreach (var col in columns)
            {
                switch (col.Id)
                {
                    case Entity.Points: row["Points"] = FloatToStr(team.Points); break;
                    case Entity.Score: row["Score"] = FloatToStr(team.Score); break;
                    case Entity.SOS: row["SOS"] = FloatToStr(team.Sos); break;
                    case Entity.SODOS: row["SODOS"] = FloatToStr(team.Sodos); break;
                    case Entity.SOSOS: row["SOSOS"] = FloatToStr(team.Sosos); break;
                    case Entity.SOUD: row["SOUD"] = FloatToStr(team.Soud); break;
                    case Entity.ScoreX: row["ScoreX"] = FloatToStr(team.ScoreX); break;
                    case Entity.SORP: row["SORP"] = FloatToStr(team.Sorp); break;
                    default: continue;
                }
            }
        }

        private void FillRowInfo(Tournament tournament, int tourNumber, TR row, Player player, WallListColumns columns, List<Team> teams, bool isPlainText)
        {
            double value;
            var team = teams == null ? null : teams.Find(item => item.Name == player.Team);

            string cellTxt;

            foreach (var col in columns)
            {
                value = player.GetCoef(tournament, col.Id, tourNumber);
                switch (col.Id)
                {
                    case Entity.Points:
                        if (team != null) team.Points += value;
                        cellTxt = FloatToHtm(value, isPlainText); break;
                    case Entity.Score:
                        if (team != null) team.Score += value;
                        cellTxt = FloatToHtm(value, isPlainText); break;
                    case Entity.ScoreX:
                        if (team != null) team.ScoreX += Math.Round(value, 0);
                        cellTxt = FloatToHtm(value, isPlainText); break;
                    case Entity.SOS:
                        if (team != null) team.Sos += value;
                        cellTxt = FloatToHtm(value, isPlainText); break;
                    case Entity.SODOS:
                        if (team != null) team.Sodos += value;
                        cellTxt = FloatToHtm(value, isPlainText); break;
                    case Entity.SOSOS:
                        if (team != null) team.Sosos += value;
                        cellTxt = FloatToHtm(value, isPlainText); break;
                    case Entity.SOUD:
                        if (team != null) team.Soud += (int)Math.Round(value, 0);
                        cellTxt = FloatToHtm(value, isPlainText); break;
                    case Entity.SORP:
                        if (team != null) team.Sorp += (int)Math.Round(value, 0);
                        cellTxt = FloatToHtm(value, isPlainText); break;
                    case Entity.NewRating:
                        double delta = Math.Round(value - player.Rating, 0);
                        cellTxt =
                            "(" +
                            (delta == 0 ? "=" : delta < 0 ? "-" + (-delta).ToString() : "+" + delta.ToString()) +
                            (player.RatingAbnormal ? "!" : string.Empty) +
                            ") " +
                            FloatToHtm2(Math.Round(value, 0), isPlainText);
                        break;
                    default: continue;
                }
                row.AddCol(cellTxt, HtmAlign.right);

            }
        }

        private void FillRowInfo(TR row, Team team, WallListColumns columns, bool isPlainText)
        {
            string cellTxt;
            foreach (var col in columns)
            {
                switch (col.Id)
                {
                    case Entity.Points: cellTxt = FloatToHtm(team.Points, isPlainText); break;
                    case Entity.Score: cellTxt = FloatToHtm(team.Score, isPlainText); break;
                    case Entity.SOS: cellTxt = FloatToHtm(team.Sos, isPlainText); break;
                    case Entity.SODOS: cellTxt = FloatToHtm(team.Sodos, isPlainText); break;
                    case Entity.SOSOS: cellTxt = FloatToHtm(team.Sosos, isPlainText); break;
                    case Entity.SOUD: cellTxt = FloatToHtm(team.Soud, isPlainText); break;
                    case Entity.ScoreX: cellTxt = FloatToHtm(team.ScoreX, isPlainText); break;
                    case Entity.SORP: cellTxt = FloatToHtm(team.Sorp, isPlainText); break;
                    case Entity.NewRating: cellTxt = string.Empty; break;
                    default: continue;
                }
                row.AddCol(cellTxt, HtmAlign.right);
            }
        }

        public string GetColumnNameByKey(string key)
        {
            switch (key)
            {
                case "PL":
                    return Translator.Translate("Common", "Place");
                case "RC":
                    return Translator.Translate("Common", "Rank");
                case "RN":
                case "NR":
                    return Translator.Translate("Common", "Rating₂");

                case "Place":
                case "Name":
                case "SuperBarMember":
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
                case "Country":
                case "City":
                case "Club":
                case "Team":
                case "Coach":
                    return Translator.Translate("Common", key);

                default: // do not translate tour IDs
                    {
                        break;
                    }
            }
            return null;
        }

        public DataSetView GetTournamentDataTable(Tournament tournament, int tourNumber, WallListColumns cols, int skipRecords, int readRecords, bool export = false)
        {
            var tbl = new DataSetView();

            int num = 1;
            if (tourNumber > tournament.Tours.Count)
                tourNumber = tournament.Tours.Count;

            UpdateStartScores(tournament);
            UpdateSuperBarMember(tournament);

            tournament.NonDatabasePlayersCount = 0;

            WallListColumns columns = new WallListColumns();
            foreach (var col in cols)
                if (col.Id == Entity.Criterias)
                {
                    foreach (var criteria in tournament.Walllist.SortCriterion)
                        columns.Add(new WallListMemberDescriptior() { Id = criteria.Id });
                }
                else
                    columns.Add(col);

            var columnsChecked = new List<Entity>()
            {
                Entity.City,
                Entity.Club,
                Entity.Country,
                Entity.Group,
                Entity.Team,
                Entity.Grade,
                Entity.Coach
            };
            var columnsPresent = new List<Entity>();

            CalculateScoreInfo(tournament, tourNumber);
            PlayerList players = GetSortedPlayers(tournament, tourNumber, false, false);

            bool hasAnyBarMember = players.HasAnyBarMember(tournament);
            bool hasDifferentStartScores = players.HasDifferentStartScores(tournament);

            //remove "Bye" player
            for (int i = players.Count - 1; i >= 0; i--)
            {
                var player = players[i];
                if (player.Id == -1)
                    players.RemoveAt(i);
                else
                {
                    if (!string.IsNullOrEmpty(player.City) && !columnsPresent.Contains(Entity.City))
                        columnsPresent.Add(Entity.City);
                    if (!string.IsNullOrEmpty(player.Club) && !columnsPresent.Contains(Entity.Club))
                        columnsPresent.Add(Entity.Club);
                    if (!string.IsNullOrEmpty(player.Country) && !columnsPresent.Contains(Entity.Country))
                        columnsPresent.Add(Entity.Country);
                    if ((hasDifferentStartScores || hasAnyBarMember) && !columnsPresent.Contains(Entity.Group))
                        columnsPresent.Add(Entity.Group);
                    if (!string.IsNullOrEmpty(player.Team) && !columnsPresent.Contains(Entity.Team))
                        columnsPresent.Add(Entity.Team);
                    if (player.Grade > 0 && !columnsPresent.Contains(Entity.Grade))
                        columnsPresent.Add(Entity.Grade);
                    if (!string.IsNullOrEmpty(player.Coach) && !columnsPresent.Contains(Entity.Coach))
                        columnsPresent.Add(Entity.Coach);
                }
            }

            if (tournament.Walllist.HideEmptyColumns)
            {
                for (int i = columns.Count - 1; i >= 0; i--)
                {
                    if (columnsChecked.Contains(columns[i].Id) && !columnsPresent.Contains(columns[i].Id))
                        columns.RemoveAt(i);
                }
            }

            int tourNum = tourNumber;
            if (tourNum == tournament.Tours.Count)
                tourNum--;


            //header
            System.Data.DataRow row;
            if (!export)
            {
                tbl.AddCol("ID");
                tbl.AddCol("PL");
                tbl.AddCol("SB");
                tbl.AddCol("PR");
                tbl.AddCol("RC");
                tbl.AddCol("RN", typeof(double));
                tbl.AddCol("NL");
            }

            foreach (var col in columns)
            {
                switch (col.Id)
                {
                    case Entity.Place: tbl.AddCol("Place", LangResources.LR.Place); break;
                    case Entity.Name: tbl.AddCol("Name", LangResources.LR.Name); break;
                    case Entity.Country: tbl.AddCol("Country", LangResources.LR.Country); break;
                    case Entity.City: tbl.AddCol("City", LangResources.LR.City); break;
                    case Entity.Club: tbl.AddCol("Club", LangResources.LR.Club); break;
                    case Entity.Team: tbl.AddCol("Team", LangResources.LR.Team); break;
                    case Entity.Coach: tbl.AddCol("Coach", LangResources.LR.Coach); break;
                    case Entity.Rank: tbl.AddCol("Rank", LangResources.LR.Rank); break;
                    case Entity.Rating: tbl.AddCol("Rating", LangResources.LR.Rating, typeof(int)); break;
                    case Entity.Grade: tbl.AddCol("Grade", LangResources.LR.Grade); break;
                    case Entity.Group:
                        {
                            tbl.AddCol("Group", LangResources.LR.Group, typeof(double));
                            if (hasAnyBarMember)
                            {
                                tbl.AddCol("SuperBarMember");
                            }
                            break;
                        }
                    case Entity.Tours:
                        {
                            for (int i = 0; i <= tourNum; i++)
                                tbl.AddCol(Tour.ToRoman(i + 1), typeof(string));
                            break;
                        }
                    case Entity.Points: tbl.AddCol("Points", LangResources.LR.Points, typeof(double)); break;
                    case Entity.Score: tbl.AddCol("Score", LangResources.LR.Score, typeof(double)); break;
                    case Entity.SOS: tbl.AddCol("SOS", LangResources.LR.SOS, typeof(double)); break;
                    case Entity.SODOS: tbl.AddCol("SODOS", LangResources.LR.SODOS, typeof(double)); break;
                    case Entity.SOSOS: tbl.AddCol("SOSOS", LangResources.LR.SOSOS, typeof(double)); break;
                    case Entity.SOUD: tbl.AddCol("SOUD", LangResources.LR.SOUD, typeof(int)); break;
                    case Entity.ScoreX: tbl.AddCol("ScoreX", LangResources.LR.ScoreX, typeof(double)); break;
                    case Entity.SORP: tbl.AddCol("SORP", LangResources.LR.SORP, typeof(double)); break;
                    case Entity.NewRating: tbl.AddCol("NR", LangResources.LR.NewRating, typeof(string)); break;
                }
            }

            if (columns.Contains(Entity.Tours))
                UpdateUpDownBalance(tournament, players, tourNum);


            if (columns.Contains(Entity.NewRating))
                CalculateRatingInfo(tournament, tourNumber);

            //Add team info
            var teams = GetTeams(players);

            int idx = 1;

            foreach (Player player in players)
            {
                if ((skipRecords == 0 || num > skipRecords) && (readRecords == 0 || num <= skipRecords + readRecords))
                {
                    row = tbl.AddRow();
                    if (!export)
                    {
                        row["ID"] = player.Id;
                        row["PL"] = IntToSortString(idx++, players.Count);
                        row["SB"] = player.SuperBarMember;
                        row["PR"] = player.PreliminaryRegistration;
                        row["RC"] = IntToSortString(PlayerInfo.GetRatingByRank(player.Rank), 3000);
                        row["NL"] = !player.PresentInRSystem;
                    }

                    FillRowInfo(tournament, tourNumber, row, num, player, columns, teams, players, num, export);
                }

                if (!player.PresentInRSystem)
                    tournament.NonDatabasePlayersCount++;

                num++;
            }

            //Report teams info
            idx = 1;
            num = 1;
            teams.Sort(delegate(Team team1, Team team2)
            {
                return CompareTeams(tournament, team1, team2);
            });

            for (int i = 1; i < teams.Count; i++)
                if (CompareTeams(tournament, teams[i], teams[i - 1], true) == 0)
                    teams[i].SharedPlace = true;

            foreach (var team in teams)
            {
                row = tbl.AddRow();
                if (!export)
                {
                    row["ID"] = -idx;
                    row["PL"] = IntToSortString(idx++, players.Count);
                    row["SB"] = false;
                    row["PR"] = false;
                    row["RC"] = 0;
                    row["NL"] = false;
                }

                if (columns.Contains(Entity.Place))
                    row["Place"] = team.SharedPlace ? "(" + Tour.ToRoman(num++) + ")" : Tour.ToRoman(num++);
                if (columns.Contains(Entity.Name))
                    row["Name"] = LangResources.LR.Team + " " + team.Name;


                FillRowInfo(row, team, columns);

            }

            return tbl;
        }

        public string GetTournamentHtmlText(Tournament tournament, int tourNumber, bool isPlainText)
        {
            var tbl = new HtmTable();

            WallListColumns cols = tournament.Walllist.Columns;

            WallListColumns columns = new WallListColumns();
            foreach (var col in cols)
                if (col.Id == Entity.Criterias)
                {
                    foreach (var criteria in tournament.Walllist.SortCriterion)
                        columns.Add(new WallListMemberDescriptior() { Id = criteria.Id });
                }
                else
                    columns.Add(col);

            int num = 1;
            if (tourNumber > tournament.Tours.Count)
                tourNumber = tournament.Tours.Count;

            int tourNum = tourNumber;
            if (tourNum == 0)
                tourNum++;

            var columnsChecked = new WallListColumns()
            {
                new WallListMemberDescriptior(){ Id = Entity.City },
                new WallListMemberDescriptior(){ Id = Entity.Club },
                new WallListMemberDescriptior(){ Id = Entity.Country },
                new WallListMemberDescriptior(){ Id = Entity.Group },
                new WallListMemberDescriptior(){ Id = Entity.Team },
                new WallListMemberDescriptior(){ Id = Entity.Coach },
                new WallListMemberDescriptior(){ Id = Entity.Grade }
            };

            var columnsPresent = new WallListColumns();

            PlayerList players = GetSortedPlayers(tournament, tourNumber, false, false);

            bool hasSharedPlace = false;

            //remove "Bye" player
            for (int i = players.Count - 1; i >= 0; i--)
            {
                var player = players[i];
                if (player.Id == -1)
                    players.RemoveAt(i);
                else
                {
                    if (!string.IsNullOrEmpty(player.City) && !columnsPresent.Contains(Entity.City))
                        columnsPresent.Add(Entity.City);
                    if (!string.IsNullOrEmpty(player.Club) && !columnsPresent.Contains(Entity.Club))
                        columnsPresent.Add(Entity.Club);
                    if (!string.IsNullOrEmpty(player.Country) && !columnsPresent.Contains(Entity.Country))
                        columnsPresent.Add(Entity.Country);
                    if (player.StartScores > 0 && !columnsPresent.Contains(Entity.Group))
                        columnsPresent.Add(Entity.Group);
                    if (!string.IsNullOrEmpty(player.Team) && !columnsPresent.Contains(Entity.Team))
                        columnsPresent.Add(Entity.Team);
                    if (player.Grade > 0 && !columnsPresent.Contains(Entity.Grade))
                        columnsPresent.Add(Entity.Grade);
                    if (!string.IsNullOrEmpty(player.Coach) && !columnsPresent.Contains(Entity.Coach))
                        columnsPresent.Add(Entity.Coach);

                    if (player.SharedPlace)
                        hasSharedPlace = true;
                }
            }

            if (tournament.Walllist.HideEmptyColumns)
            {
                for (int i = columns.Count - 1; i >= 0; i--)
                {
                    if (columnsChecked.Contains(columns[i].Id) && !columnsPresent.Contains(columns[i].Id))
                        columns.RemoveAt(i);
                }
            }

            //header
            var row = tbl.AddRow();

            if (columns.Contains(Entity.Place))
                row.AddCol(LangResources.LR.Pl, HtmAlign.right);
            if (columns.Contains(Entity.Name))
                row.AddCol(LangResources.LR.Name);
            if (columns.Contains(Entity.Country))
                row.AddCol(LangResources.LR.Country);
            if (columns.Contains(Entity.City))
                row.AddCol(LangResources.LR.City);
            if (columns.Contains(Entity.Club))
                row.AddCol(LangResources.LR.Club);
            if (columns.Contains(Entity.Team))
                row.AddCol(LangResources.LR.Team);
            if (columns.Contains(Entity.Coach))
                row.AddCol(LangResources.LR.Coach);
            if (columns.Contains(Entity.Rank))
                row.AddCol(LangResources.LR.Rank, HtmAlign.right);
            if (columns.Contains(Entity.Rating))
                row.AddCol(LangResources.LR.Rating, HtmAlign.right);
            if (columns.Contains(Entity.Grade))
                row.AddCol(LangResources.LR.Grade);
            if (columns.Contains(Entity.Group))
                row.AddCol(LangResources.LR.Group, HtmAlign.right);
            if (columns.Contains(Entity.Tours))
                for (int i = 0; i < tourNum; i++)
                    row.AddCol(Tour.ToRoman(i + 1), HtmAlign.center);

            foreach (var col in columns)
            {
                switch (col.Id)
                {
                    case Entity.Points: row.AddCol(LangResources.LR.Points, HtmAlign.right); break;
                    case Entity.Score: row.AddCol(LangResources.LR.Score, HtmAlign.right); break;
                    case Entity.SOS: row.AddCol(LangResources.LR.SOS, HtmAlign.right); break;
                    case Entity.SODOS: row.AddCol(LangResources.LR.SODOS, HtmAlign.right); break;
                    case Entity.SOSOS: row.AddCol(LangResources.LR.SOSOS, HtmAlign.right); break;
                    case Entity.SOUD: row.AddCol(LangResources.LR.SOUD, HtmAlign.right); break;
                    case Entity.ScoreX: row.AddCol(LangResources.LR.ScoreX, HtmAlign.right); break;
                    case Entity.SORP: row.AddCol(LangResources.LR.SORP, HtmAlign.right); break;
                    case Entity.NewRating: row.AddCol(LangResources.LR.NewRating, HtmAlign.right); break;
                }
            }

            int c = row.Cols.Count;
            row = tbl.AddRow();
            for (int i = 0; i < c; i++)
                row.AddCol(string.Empty, HtmAlign.line);

            row = new TR();
            for (int i = 0; i < c; i++)
                row.AddCol(string.Empty, HtmAlign.line);
            tbl.Rows.Insert(0, row);

            if (columns.Contains(Entity.Tours))
            {
                foreach (Tour tour in tournament.Tours)
                    FillPairsData(tournament, tour, tournament.Players);

                UpdateUpDownBalance(tournament, players, tourNum - 1);
            }

            if (columns.Contains(Entity.NewRating))
                CalculateRatingInfo(tournament, tourNumber);

            CalculateScoreInfo(tournament, tourNumber - 1);

            //Add team info
            var teams = GetTeams(players);

            foreach (Player player in players)
            {
                row = tbl.AddRow();

                if (columns.Contains(Entity.Place))
                    row.AddCol(player.SharedPlace ? "(" + num++.ToString() + ")" : num++.ToString() + (hasSharedPlace ? " " : string.Empty), HtmAlign.right);
                if (columns.Contains(Entity.Name))
                    row.AddCol(player.Name/*tournament.UseTransliteration ? player.Name : player.InternationalName*/);
                if (columns.Contains(Entity.Country))
                    row.AddCol(player.IsCountrySet ? player.Country.ToString() : string.Empty);
                if (columns.Contains(Entity.City))
                    row.AddCol(player.City);
                if (columns.Contains(Entity.Club))
                    row.AddCol(player.Club);
                if (columns.Contains(Entity.Team))
                    row.AddCol(player.Team);
                if (columns.Contains(Entity.Coach))
                    row.AddCol(player.Coach);
                if (columns.Contains(Entity.Rank))
                    row.AddCol(player.RankExt, HtmAlign.right);
                if (columns.Contains(Entity.Rating))
                    row.AddCol(player.Rating.ToString(), HtmAlign.right);
                if (columns.Contains(Entity.Grade))
                    row.AddCol(Grade.GetShortName(player.Grade), HtmAlign.center);
                if (columns.Contains(Entity.Group))
                    row.AddCol(FloatToHtm(player.StartScores + player.ScoreAdjustmentValueEx, isPlainText), HtmAlign.right);

                if (columns.Contains(Entity.Tours))
                {
                    for (int i = 0; i < tourNum; i++)
                    {
                        string cellText = "--";
                        var tour = tournament.Tours[i];

                        var pair = tour.Pairs.Find(
                            delegate(Pair item) { return item.FirstPlayerId == player.Id || item.SecondPlayerId == player.Id; }
                        );

                        int competitorId = -1;
                        if (pair != null)
                        {
                            competitorId = pair.FirstPlayerId == player.Id ? pair.SecondPlayerId : pair.FirstPlayerId;
                            switch (competitorId)
                            {
                                case -2:
                                    cellText = "--";
                                    break;
                                case -1:
                                    cellText = /*isPlainText ?*/ "+-" /*: "&frac12;"*/;
                                    break;

                                default:
                                    int competitorIdx = players.FindIndex(delegate(Player item) { return item.Id == competitorId; });
                                    if (competitorIdx > -1)
                                    {
                                        cellText = string.Empty;

                                        if (pair.UpDownBalance != 0 && !isPlainText)
                                        {
                                            if (pair.GetUpDownBalance(player.Id) < 0)
                                                cellText += pair.GetUpDownCompensed(player.Id) ? "&uarr;=" : "&darr;";
                                            else
                                                cellText += pair.GetUpDownCompensed(player.Id) ? "&uarr;=" : "&uarr;";
                                        }

                                        cellText += (competitorIdx + 1).ToString();
                                        cellText += pair.GetGameResultText(player.Id, tournament.Walllist.ShowColors, tournament.HandicapUsed || pair.ForcedPairing, false);

                                    }
                                    break;
                            }
                        }
                        row.AddCol(cellText, HtmAlign.right);
                    }

                    FillRowInfo(tournament, tourNumber - 1, row, player, columns, teams, isPlainText);
                }

            }

            //Report teams info
            num = 1;
            teams.Sort(delegate(Team team1, Team team2)
            {
                return CompareTeams(tournament, team1, team2);
            });

            hasSharedPlace = false;
            for (int i = 1; i < teams.Count; i++)
                if (CompareTeams(tournament, teams[i], teams[i - 1], true) == 0)
                {
                    teams[i].SharedPlace = true;
                    hasSharedPlace = true;
                }

            if (teams.Count > 0)
            {
                int z = row.Cols.Count;
                row = tbl.AddRow();
                for (int i = 0; i < z; i++)
                    row.AddCol("", HtmAlign.line);
            }

            foreach (var team in teams)
            {
                row = tbl.AddRow();

                if (columns.Contains(Entity.Place))
                    row.AddCol(team.SharedPlace ? "(" + Tour.ToRoman(num++) + ")" : Tour.ToRoman(num++) + (hasSharedPlace ? " " : string.Empty), HtmAlign.right);
                if (columns.Contains(Entity.Name))
                    row.AddCol(LangResources.LR.Team + " " + team.Name);

                if (columns.Contains(Entity.Country))
                    row.AddCol();
                if (columns.Contains(Entity.City))
                    row.AddCol();
                if (columns.Contains(Entity.Club))
                    row.AddCol();
                if (columns.Contains(Entity.Team))
                    row.AddCol();
                if (columns.Contains(Entity.Coach))
                    row.AddCol();
                if (columns.Contains(Entity.Rank))
                    row.AddCol();
                if (columns.Contains(Entity.Rating))
                    row.AddCol();
                if (columns.Contains(Entity.Grade))
                    row.AddCol();
                if (columns.Contains(Entity.Group))
                    row.AddCol();


                if (columns.Contains(Entity.Tours))
                {
                    for (int i = 0; i <= tourNum; i++)
                        row.AddCol();
                }

                FillRowInfo(row, team, columns, isPlainText);

            }

            var tblCapt = new HtmTable();
            if (!string.IsNullOrEmpty(tournament.Name))
            {
                row = tblCapt.AddRow();
                row.AddCol(LangResources.LR.TournamentName + ":", HtmAlign.right);
                row.AddCol(tournament.Name);
            }
            if (!string.IsNullOrEmpty(tournament.Description))
            {
                row = tblCapt.AddRow();
                row.AddCol(LangResources.LR.Description + ":", HtmAlign.right);
                row.AddCol(tournament.Description);
            }
            row = tblCapt.AddRow();
            row.AddCol(LangResources.LR.Round + ":", HtmAlign.right);
            row.AddCol(GetTourName(tournament));
            row = tblCapt.AddRow();

            if (isPlainText)
                return tblCapt.GetPlainText() +
                    tbl.GetPlainText();
            else
                return "<html>" +
                    "<header>" +
                    "<title>" + tournament.Name + "</title>" +
                    "<META HTTP-EQUIV='Content-Type' content='text/html; charset=utf-8' />" +
                    "</header><body>" +
                    tblCapt.GetPlainHtmlText() + tbl.GetPlainHtmlText() +
                    "</body></html>";
        }

        private Pair FindPair(Tournament tournament, Player player, int competitorId)
        {
            Pair result = null;

            var competitor = tournament.Players.Find(x => x.Id == competitorId);
            if (competitor == null)
            {
                return null;
            }

            for (int j = 0; j < tournament.Tours.Count; j++)
            {
                var tour = tournament.Tours[j];
                FillPairsData(tournament, tour, tournament.Players);

                result = tour.Pairs.Find(x => x.FirstPlayerId == player.Id && x.SecondPlayer == competitor ||
                    x.SecondPlayerId == player.Id && x.FirstPlayer == competitor);
                if (result != null)
                {
                    break;
                }
            }

            return result;
        }

        public string GetTournamentRoundRobinHtmlText(Tournament tournament, bool isPlainText)
        {
            var tbl = new HtmTable();

            WallListColumns cols = tournament.Walllist.Columns;

            WallListColumns columns = new WallListColumns();
            foreach (var col in cols)
                if (col.Id == Entity.Criterias)
                {
                    foreach (var criteria in tournament.Walllist.SortCriterion)
                        columns.Add(new WallListMemberDescriptior() { Id = criteria.Id });
                }
                else
                    columns.Add(col);

            int num = 1;
            int tourNumber = tournament.Tours.Count;
            int tourNum = tourNumber;

            var columnsChecked = new WallListColumns()
            {
                new WallListMemberDescriptior(){ Id = Entity.City },
                new WallListMemberDescriptior(){ Id = Entity.Club },
                new WallListMemberDescriptior(){ Id = Entity.Country },
                new WallListMemberDescriptior(){ Id = Entity.Group },
                new WallListMemberDescriptior(){ Id = Entity.Team },
                new WallListMemberDescriptior(){ Id = Entity.Coach },
                new WallListMemberDescriptior(){ Id = Entity.Grade }
            };

            var columnsPresent = new WallListColumns();

            PlayerList players = GetSortedPlayers(tournament, tourNumber, false, false);

            bool hasSharedPlace = false;

            //remove "Bye" player
            for (int i = players.Count - 1; i >= 0; i--)
            {
                var player = players[i];
                if (player.Id == -1)
                    players.RemoveAt(i);
                else
                {
                    if (!string.IsNullOrEmpty(player.City) && !columnsPresent.Contains(Entity.City))
                        columnsPresent.Add(Entity.City);
                    if (!string.IsNullOrEmpty(player.Club) && !columnsPresent.Contains(Entity.Club))
                        columnsPresent.Add(Entity.Club);
                    if (!string.IsNullOrEmpty(player.Country) && !columnsPresent.Contains(Entity.Country))
                        columnsPresent.Add(Entity.Country);
                    if (player.StartScores > 0 && !columnsPresent.Contains(Entity.Group))
                        columnsPresent.Add(Entity.Group);
                    if (!string.IsNullOrEmpty(player.Team) && !columnsPresent.Contains(Entity.Team))
                        columnsPresent.Add(Entity.Team);
                    if (player.Grade > 0 && !columnsPresent.Contains(Entity.Grade))
                        columnsPresent.Add(Entity.Grade);
                    if (!string.IsNullOrEmpty(player.Coach) && !columnsPresent.Contains(Entity.Coach))
                        columnsPresent.Add(Entity.Coach);

                    if (player.SharedPlace)
                        hasSharedPlace = true;
                }
            }

            for (int i = 0; i < players.Count; i++)
            {
                players[i].Order = i + 1;
            }

            //players.Sort(delegate(Player player1, Player player2)
            //{
            //    return ComparePlayersByStartNumber(player1, player2, false);
            //});

            if (tournament.Walllist.HideEmptyColumns)
            {
                for (int i = columns.Count - 1; i >= 0; i--)
                {
                    if (columnsChecked.Contains(columns[i].Id) && !columnsPresent.Contains(columns[i].Id))
                        columns.RemoveAt(i);
                }
            }

            //header
            var row = tbl.AddRow();

            if (columns.Contains(Entity.Place))
                row.AddCol(LangResources.LR.Num, HtmAlign.right);
            if (columns.Contains(Entity.Name))
                row.AddCol(LangResources.LR.Name);
            if (columns.Contains(Entity.Country))
                row.AddCol(LangResources.LR.Country);
            if (columns.Contains(Entity.City))
                row.AddCol(LangResources.LR.City);
            if (columns.Contains(Entity.Club))
                row.AddCol(LangResources.LR.Club);
            if (columns.Contains(Entity.Team))
                row.AddCol(LangResources.LR.Team);
            if (columns.Contains(Entity.Coach))
                row.AddCol(LangResources.LR.Coach);
            if (columns.Contains(Entity.Rank))
                row.AddCol(LangResources.LR.Rank, HtmAlign.right);
            if (columns.Contains(Entity.Rating))
                row.AddCol(LangResources.LR.Rating, HtmAlign.right);
            if (columns.Contains(Entity.Grade))
                row.AddCol(LangResources.LR.Grade);
            if (columns.Contains(Entity.Group))
                row.AddCol(LangResources.LR.Group, HtmAlign.right);
            if (columns.Contains(Entity.Tours))
                for (int i = 0; i < players.Count; i++)
                    row.AddCol((i + 1).ToString(), HtmAlign.center);

            foreach (var col in columns)
            {
                switch (col.Id)
                {
                    case Entity.Points: row.AddCol(LangResources.LR.Points, HtmAlign.right); break;
                    case Entity.Score: row.AddCol(LangResources.LR.Score, HtmAlign.right); break;
                    case Entity.SOS: row.AddCol(LangResources.LR.SOS, HtmAlign.right); break;
                    case Entity.SODOS: row.AddCol(LangResources.LR.SODOS, HtmAlign.right); break;
                    case Entity.SOSOS: row.AddCol(LangResources.LR.SOSOS, HtmAlign.right); break;
                    case Entity.SOUD: row.AddCol(LangResources.LR.SOUD, HtmAlign.right); break;
                    case Entity.ScoreX: row.AddCol(LangResources.LR.ScoreX, HtmAlign.right); break;
                    case Entity.SORP: row.AddCol(LangResources.LR.SORP, HtmAlign.right); break;
                    case Entity.NewRating: row.AddCol(LangResources.LR.NewRating, HtmAlign.right); break;
                }
            }

            //if (columns.Contains(Entity.Place))
            //    row.AddCol(LangResources.LR.Place, HtmAlign.right);

            int c = row.Cols.Count;
            row = tbl.AddRow();
            for (int i = 0; i < c; i++)
                row.AddCol(string.Empty, HtmAlign.line);

            row = new TR();
            for (int i = 0; i < c; i++)
                row.AddCol(string.Empty, HtmAlign.line);
            tbl.Rows.Insert(0, row);

            if (columns.Contains(Entity.Tours))
            {
                foreach (Tour tour in tournament.Tours)
                    FillPairsData(tournament, tour, tournament.Players);

                UpdateUpDownBalance(tournament, players, tourNum - 1);
            }

            if (columns.Contains(Entity.NewRating))
                CalculateRatingInfo(tournament, tourNumber);

            CalculateScoreInfo(tournament, tourNumber - 1);

            //Add team info
            var teams = GetTeams(players);

            foreach (Player player in players)
            {
                row = tbl.AddRow();

                if (columns.Contains(Entity.Place))
                    //row.AddCol(num++.ToString(), HtmAlign.right);
                    row.AddCol(player.SharedPlace ? "(" + num++.ToString() + ")" : num++.ToString() + (hasSharedPlace ? " " : string.Empty), HtmAlign.right);
                if (columns.Contains(Entity.Name))
                    row.AddCol(player.Name/*tournament.UseTransliteration ? player.Name : player.InternationalName*/);
                if (columns.Contains(Entity.Country))
                    row.AddCol(player.IsCountrySet ? player.Country.ToString() : string.Empty);
                if (columns.Contains(Entity.City))
                    row.AddCol(player.City);
                if (columns.Contains(Entity.Club))
                    row.AddCol(player.Club);
                if (columns.Contains(Entity.Team))
                    row.AddCol(player.Team);
                if (columns.Contains(Entity.Coach))
                    row.AddCol(player.Coach);
                if (columns.Contains(Entity.Rank))
                    row.AddCol(player.RankExt, HtmAlign.right);
                if (columns.Contains(Entity.Rating))
                    row.AddCol(player.Rating.ToString(), HtmAlign.right);
                if (columns.Contains(Entity.Grade))
                    row.AddCol(Grade.GetShortName(player.Grade), HtmAlign.center);
                if (columns.Contains(Entity.Group))
                    row.AddCol(FloatToHtm(player.StartScores + player.ScoreAdjustmentValueEx, isPlainText), HtmAlign.right);

                if (columns.Contains(Entity.Tours))
                {
                    for (int i = 0; i < players.Count; i++)
                    {
                        string cellText = "x";

                        Pair pair = players[i] == player ? null : FindPair(tournament, player, players[i].Id);

                        int competitorId = -1;
                        if (pair != null)
                        {
                            competitorId = pair.FirstPlayerId == player.Id ? pair.SecondPlayerId : pair.FirstPlayerId;
                            switch (competitorId)
                            {
                                case -2:
                                    cellText = "--";
                                    break;
                                case -1:
                                    cellText = "+-";
                                    break;

                                default:
                                    int competitorIdx = players.FindIndex(delegate(Player item) { return item.Id == competitorId; });
                                    if (competitorIdx > -1)
                                    {
                                        cellText = pair.GetGameResultRounRobinText(player.Id, false);
                                    }
                                    break;
                            }
                        }
                        row.AddCol(cellText, HtmAlign.right);
                    }

                    FillRowInfo(tournament, tourNumber - 1, row, player, columns, teams, isPlainText);

                    //if (columns.Contains(Entity.Place))
                    //    row.AddCol(player.PlaceStr, HtmAlign.center);

                }

            }

            //Report teams info
            num = 1;
            teams.Sort(delegate(Team team1, Team team2)
            {
                return CompareTeams(tournament, team1, team2);
            });

            hasSharedPlace = false;
            for (int i = 1; i < teams.Count; i++)
                if (CompareTeams(tournament, teams[i], teams[i - 1], true) == 0)
                {
                    teams[i].SharedPlace = true;
                    hasSharedPlace = true;
                }

            if (teams.Count > 0)
            {
                int z = row.Cols.Count;
                row = tbl.AddRow();
                for (int i = 0; i < z; i++)
                    row.AddCol("", HtmAlign.line);
            }

            foreach (var team in teams)
            {
                row = tbl.AddRow();

                if (columns.Contains(Entity.Place))
                    row.AddCol(team.SharedPlace ? "(" + Tour.ToRoman(num++) + ")" : Tour.ToRoman(num++) + (hasSharedPlace ? " " : string.Empty), HtmAlign.right);
                if (columns.Contains(Entity.Name))
                    row.AddCol(LangResources.LR.Team + " " + team.Name);

                if (columns.Contains(Entity.Country))
                    row.AddCol();
                if (columns.Contains(Entity.City))
                    row.AddCol();
                if (columns.Contains(Entity.Club))
                    row.AddCol();
                if (columns.Contains(Entity.Team))
                    row.AddCol();
                if (columns.Contains(Entity.Coach))
                    row.AddCol();
                if (columns.Contains(Entity.Rank))
                    row.AddCol();
                if (columns.Contains(Entity.Rating))
                    row.AddCol();
                if (columns.Contains(Entity.Grade))
                    row.AddCol();
                if (columns.Contains(Entity.Group))
                    row.AddCol();


                if (columns.Contains(Entity.Tours))
                {
                    for (int i = 0; i <= players.Count; i++)
                        row.AddCol();
                }

                FillRowInfo(row, team, columns, isPlainText);

                if (columns.Contains(Entity.Place))
                    row.AddCol();
            }

            var tblCapt = new HtmTable();
            if (!string.IsNullOrEmpty(tournament.Name))
            {
                row = tblCapt.AddRow();
                row.AddCol(LangResources.LR.TournamentName + ":", HtmAlign.right);
                row.AddCol(tournament.Name);
            }
            if (!string.IsNullOrEmpty(tournament.Description))
            {
                row = tblCapt.AddRow();
                row.AddCol(LangResources.LR.Description + ":", HtmAlign.right);
                row.AddCol(tournament.Description);
            }
            row = tblCapt.AddRow();
            row.AddCol(LangResources.LR.Round + ":", HtmAlign.right);
            row.AddCol(GetTourName(tournament));
            row = tblCapt.AddRow();

            if (isPlainText)
                return tblCapt.GetPlainText() +
                    tbl.GetPlainText();
            else
                return "<html>" +
                    "<header>" +
                    "<title>" + tournament.Name + "</title>" +
                    "<META HTTP-EQUIV='Content-Type' content='text/html; charset=utf-8' />" +
                    "</header><body>" +
                    tblCapt.GetPlainHtmlText() + tbl.GetPlainHtmlText() +
                    "</body></html>";
        }

        public string GetTournamentTextForRating(Tournament tournament, bool useLocalNames)
        {
            var tbl = new HtmTable();

            int num = 1;
            int tourNumber = tournament.Tours.Count;
            if (tourNumber <= 0)
                tourNumber = 1;

            if (tournament.Tours.Count == 0)
                return string.Empty;

            var columns = new List<Entity>()
            {
                Entity.Place,
                Entity.Name,
                Entity.Rank,
                Entity.Club,
                Entity.Country,
                Entity.Score,
                Entity.Points,
                Entity.SOS,
                Entity.SODOS,
                Entity.Tours
            };

            if (columns.Contains(Entity.Tours))
                foreach (Tour tour in tournament.Tours)
                    FillPairsData(tournament, tour, tournament.Players);

            CalculateScoreInfo(tournament, tourNumber);

            PlayerList players = GetSortedPlayers(tournament, tourNumber, false, false);
            bool hasSharedPlace = false;
            //remove "Bye" player
            for (int i = players.Count - 1; i >= 0; i--)
            {
                var player = players[i];
                if (player.Id == -1)
                    players.RemoveAt(i);
                else
                    if (player.SharedPlace)
                        hasSharedPlace = true;
            }

            int tour_num = tourNumber;
            if (tour_num == tournament.Tours.Count)
                tour_num--;
            TR row;

            foreach (Player player in players)
            {
                row = tbl.AddRow();

                if (columns.Contains(Entity.Place))
                    row.AddCol(player.SharedPlace ? "(" + num++.ToString() + ")" : num++.ToString() + (hasSharedPlace ? " " : string.Empty), HtmAlign.right);
                if (columns.Contains(Entity.Name))
                    row.AddCol(Tournament.UseTransliteration && useLocalNames ? player.ExportLocalName : player.ExportName);
                if (columns.Contains(Entity.Rank))
                    row.AddCol(player.Rank, HtmAlign.right);
                if (columns.Contains(Entity.Country))
                    row.AddCol(player.IsCountrySet ? player.Country.ToString().ToUpper() : "??");
                if (columns.Contains(Entity.Club))
                    row.AddCol(GetClubEgdName(tournament, player.Country, player.Club));

                string cellTxt;

                foreach (var col in columns)
                {
                    switch (col)
                    {
                        case Entity.Points:
                        case Entity.Score:
                        case Entity.ScoreX:
                        case Entity.SOS:
                        case Entity.SODOS:
                        case Entity.SOSOS:
                        case Entity.SOUD:
                        case Entity.SORP:
                            cellTxt = FloatToStr(player.GetCoef(tournament, col, tourNumber), true); break;
                        default: continue;
                    }
                    row.AddCol(cellTxt, HtmAlign.right);
                }

                if (columns.Contains(Entity.Tours))
                {
                    for (int i = 0; i <= tour_num; i++)
                    {
                        string cellText = "0=";
                        var tour = tournament.Tours[i];
                        var pair = tour.Pairs.Find(
                            delegate(Pair item) { return item.FirstPlayerId == player.Id || item.SecondPlayerId == player.Id; }
                        );

                        int competitorId = -1;
                        if (pair != null)
                        {
                            competitorId = pair.FirstPlayerId == player.Id ? pair.SecondPlayerId : pair.FirstPlayerId;
                            switch (competitorId)
                            {
                                case -2:
                                    cellText = "0-";
                                    break;
                                case -1:
                                    cellText = "0=";
                                    break;

                                default:
                                    int competitorIdx = players.FindIndex(delegate(Player item) { return item.Id == competitorId; });
                                    if (competitorIdx > -1)
                                    {
                                        cellText = string.Empty;
                                        cellText += (competitorIdx + 1).ToString();
                                        cellText += pair.GetGameResultText(player.Id, false, false, false, false);
                                    }
                                    break;
                            }
                            if (competitorId > -1)
                            {
                                cellText += "/";

                                if (tournament.FirstMoveBlack)
                                    cellText += (pair.FirstPlayerId == player.Id) ? "b" : "w";
                                else
                                    cellText += (pair.FirstPlayerId == player.Id) ? "w" : "b";

                                cellText += pair.Handicap.ToString();
                            }
                            else
                                cellText += "   ";
                        }
                        else
                            cellText += "   ";
                        row.AddCol(cellText, HtmAlign.right);
                    }


                }
            }

            var tblCapt = new HtmTable();
            row = tblCapt.AddRow();

            if (!string.IsNullOrEmpty(tournament.Name))
                row.AddCol("; EV[" + tournament.Name.Replace('[', '(').Replace(']', ')') + "]");

            return tblCapt.GetPlainText() +
                tbl.GetPlainText();
        }

        public DataSetView GetPairingDataTable(Tournament tournament, int tourNumber)
        {
            var tbl = new DataSetView();

            if (tourNumber >= tournament.Tours.Count)
                tourNumber = tournament.Tours.Count - 1;
            if (tourNumber < 0)
                tourNumber = 0;

            bool takeCurrentRoundInAccount = tournament.TakeCurrentRoundInAccount;
            tournament.TakeCurrentRoundInAccount = false;

            try
            {

                var tour = tournament.Tours[tourNumber];
                var pairs = tour.Pairs;
                FillPairsData(tournament, tour, tournament.Players);

                bool hasHandicap = false;
                bool hasComments = false;
                foreach (var pair in pairs)
                {
                    if (pair.Handicap != 0 || pair.AdditionalKomi != 0)
                        hasHandicap = true;
                    if (!string.IsNullOrEmpty(pair.Comment))
                        hasComments = true;
                    if (hasComments && hasHandicap)
                        break;
                }

                //header
                tbl.AddCol(LangResources.LR.Board, typeof(int));
                if (tournament.FirstMoveBlack)
                {
                    tbl.AddCol(LangResources.LR.Black);
                    tbl.AddCol(LangResources.LR.White);
                }
                else
                {
                    tbl.AddCol(LangResources.LR.White);
                    tbl.AddCol(LangResources.LR.Black);
                }

                if (hasHandicap)
                {
                    tbl.AddCol(LangResources.LR.Handicap);
                }

                tbl.AddCol(LangResources.LR.Result);

                if (hasComments)
                {
                    tbl.AddCol(LangResources.LR.Comments);
                }

                foreach (var pair in pairs)
                {
                    var row = tbl.AddRow();
                    row[0] = pair.BoardNumber;
                    row[1] = pair.FirstPlayerName;
                    row[2] = pair.SecondPlayerName;

                    row[LangResources.LR.Result] = pair.Result;
                    if (hasHandicap)
                        row[LangResources.LR.Handicap] = pair.HandicapText;
                    if (hasComments)
                        row[LangResources.LR.Comments] = pair.Comment;
                }

                return tbl;
            }
            finally
            {
                tournament.TakeCurrentRoundInAccount = takeCurrentRoundInAccount;
            }

        }

        public string GetPairingHtmlText(Tournament tournament, int tourNumber, bool isPlainText)
        {
            var tbl = new HtmTable();

            if (tourNumber >= tournament.Tours.Count)
                tourNumber = tournament.Tours.Count - 1;
            if (tourNumber < 0)
                tourNumber = 0;

            if (tournament.Tours.Count == 0)
                return string.Empty;

            bool takeCurrentRoundInAccount = tournament.TakeCurrentRoundInAccount;
            tournament.TakeCurrentRoundInAccount = false;

            try
            {

                var tour = tournament.Tours[tourNumber];
                var pairs = tour.Pairs;
                FillPairsData(tournament, tour, tournament.Players);

                //header
                var row = tbl.AddRow();
                row.AddCol("", HtmAlign.line);
                row.AddCol("", HtmAlign.line);
                row.AddCol("", HtmAlign.line);
                if (tournament.HandicapUsed)
                    row.AddCol("", HtmAlign.line);
                row.AddCol("", HtmAlign.line);

                row = tbl.AddRow();
                row.AddCol(LangResources.LR.Board, HtmAlign.left);
                if (tournament.FirstMoveBlack)
                {
                    row.AddCol(LangResources.LR.Black, HtmAlign.left);
                    row.AddCol(LangResources.LR.White, HtmAlign.left);
                }
                else
                {
                    row.AddCol(LangResources.LR.White, HtmAlign.left);
                    row.AddCol(LangResources.LR.Black, HtmAlign.left);
                }
                if (tournament.HandicapUsed)
                    row.AddCol(LangResources.LR.Handicap, HtmAlign.left);
                row.AddCol(LangResources.LR.Result, HtmAlign.left);

                row = tbl.AddRow();
                row.AddCol("", HtmAlign.line);
                row.AddCol("", HtmAlign.line);
                row.AddCol("", HtmAlign.line);
                if (tournament.HandicapUsed)
                    row.AddCol("", HtmAlign.line);
                row.AddCol("", HtmAlign.line);

                foreach (var pair in pairs)
                {
                    row = tbl.AddRow();
                    row.AddCol(pair.BoardNumber.ToString(), HtmAlign.right);
                    row.AddCol(pair.FirstPlayerName, HtmAlign.left);
                    row.AddCol(pair.SecondPlayerName, HtmAlign.left);
                    if (tournament.HandicapUsed)
                        row.AddCol(pair.HandicapText, HtmAlign.center);
                    row.AddCol(pair.Result, HtmAlign.center);
                }

                var tblCapt = new HtmTable();

                if (!string.IsNullOrEmpty(tournament.Name))
                {
                    row = tblCapt.AddRow();
                    row.AddCol(LangResources.LR.TournamentName + ":", HtmAlign.right);
                    row.AddCol(tournament.Name);
                }
                if (!string.IsNullOrEmpty(tournament.Name))
                {
                    row = tblCapt.AddRow();
                    row.AddCol(LangResources.LR.Description + ":", HtmAlign.right);
                    row.AddCol(tournament.Description);
                }
                row = tblCapt.AddRow();
                row.AddCol(LangResources.LR.Round + ":", HtmAlign.right);
                row.AddCol(Tour.ToRoman(tourNumber + 1 > tournament.Tours.Count ? tourNumber : tourNumber + 1));
                row = tblCapt.AddRow();

                if (isPlainText)
                    return tblCapt.GetPlainText() +
                        tbl.GetPlainText();
                else
                    return "<html>" +
                        "<header>" +
                        "<title>" + tournament.Name + "</title>" +
                        "<META HTTP-EQUIV='Content-Type' content='text/html; charset=utf-8' />" +
                        "</header><body>" +
                        tblCapt.GetPlainHtmlText() + tbl.GetPlainHtmlText() +
                        "</body></html>";

            }
            finally
            {
                tournament.TakeCurrentRoundInAccount = takeCurrentRoundInAccount;
            }

        }

        #endregion
    }
}
