using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Tourtoss.BE
{

    public enum TournamentKind
    {
        Swiss,
        McMahon,
        Round,
        Scheveningen,
        None,
    }

    [Serializable]
    public class PlayerInfo
    {
        [XmlAttribute(AttributeName = "typeversion")]
        public int TypeVersion = 1;

        public bool AsianName { get; set; }

        [XmlIgnore]
        public bool AlternativeNameIfEmpty = true;

        [XmlIgnore]
        public string FirstName { get; set; }
        [XmlElement(ElementName = "FirstName")]
        public XmlNode[] FirstNameCData
        {
            get
            {
                var dummy = new XmlDocument();
                return new XmlNode[] { dummy.CreateCDataSection(FirstName) };
            }
            set
            {
                if (value == null)
                {
                    FirstName = null;
                    return;
                }

                if (value.Length != 1)
                {
                    throw new InvalidOperationException(
                        String.Format(
                            "Invalid array length {0}", value.Length));
                }

                var node0 = value[0];
                var cdata = node0 as XmlCDataSection;
                if (cdata == null)
                {
                    throw new InvalidOperationException(
                        String.Format(
                            "Invalid node type {0}", node0.NodeType));
                }

                FirstName = cdata.Data;
            }
        }

        [XmlIgnore]
        public string Surname { get; set; }
        [XmlElement(ElementName = "Surname")]
        public XmlNode[] SurnameCData
        {
            get
            {
                var dummy = new XmlDocument();
                return new XmlNode[] { dummy.CreateCDataSection(Surname) };
            }
            set
            {
                if (value == null)
                {
                    Surname = null;
                    return;
                }

                if (value.Length != 1)
                {
                    throw new InvalidOperationException(
                        String.Format(
                            "Invalid array length {0}", value.Length));
                }

                var node0 = value[0];
                var cdata = node0 as XmlCDataSection;
                if (cdata == null)
                {
                    throw new InvalidOperationException(
                        String.Format(
                            "Invalid node type {0}", node0.NodeType));
                }

                Surname = cdata.Data;
            }
        }

        [XmlIgnore]
        private string _localFirstName { get; set; }
        [XmlIgnore]
        public string LocalFirstName
        {
            get { return AlternativeNameIfEmpty && string.IsNullOrEmpty(_localFirstName) ? FirstName : _localFirstName; }
            set { _localFirstName = value; }
        }
        [XmlElement(ElementName = "LocalFirstName")]
        public XmlNode[] LocalFirstNameCData
        {
            get
            {
                var dummy = new XmlDocument();
                return new XmlNode[] { dummy.CreateCDataSection(LocalFirstName) };
            }
            set
            {
                if (value == null)
                {
                    LocalFirstName = null;
                    return;
                }

                if (value.Length != 1)
                {
                    throw new InvalidOperationException(
                        String.Format(
                            "Invalid array length {0}", value.Length));
                }

                var node0 = value[0];
                var cdata = node0 as XmlCDataSection;
                if (cdata == null)
                {
                    throw new InvalidOperationException(
                        String.Format(
                            "Invalid node type {0}", node0.NodeType));
                }

                LocalFirstName = cdata.Data;
            }
        }

        [XmlIgnore]
        private string _localSurname { get; set; }
        [XmlIgnore]
        public string LocalSurname
        {
            get { return AlternativeNameIfEmpty && string.IsNullOrEmpty(_localSurname) ? Surname : _localSurname; }
            set { _localSurname = value; }
        }
        [XmlElement(ElementName = "LocalSurname")]
        public XmlNode[] LocalSurnameCData
        {
            get
            {
                var dummy = new XmlDocument();
                return new XmlNode[] { dummy.CreateCDataSection(LocalSurname) };
            }
            set
            {
                if (value == null)
                {
                    LocalSurname = null;
                    return;
                }

                if (value.Length != 1)
                {
                    throw new InvalidOperationException(
                        String.Format(
                            "Invalid array length {0}", value.Length));
                }

                var node0 = value[0];
                var cdata = node0 as XmlCDataSection;
                if (cdata == null)
                {
                    throw new InvalidOperationException(
                        String.Format(
                            "Invalid node type {0}", node0.NodeType));
                }

                LocalSurname = cdata.Data;
            }
        }

        [XmlIgnore]
        private string _rank;
        [XmlIgnore]
        private string _rankExt;
        [XmlIgnore]
        public Tournament RootTournament;

        [XmlIgnore]
        public bool IsRankEnabled
        {
            get
            {
                return !(RootTournament != null && RootTournament.RatingDeterminesRank);
            }
        }

        private int _levelFromRank;

        [XmlIgnore]
        public int LevelFromRank
        {
            get
            {
                return _levelFromRank;
            }
        }

        [XmlElement(ElementName = "GoLevel")]
        public string Rank
        {
            get
            {
                return IsRankEnabled ? _rank : GetRankFromRating(Rating);
            }
            set
            {
                int l = GetLevelByRank(value);
                int r = GetRatingByLevel(l);
                _levelFromRank = l;
                _rank = GetRankFromRating(r);
                _rankExt = GetRankFromRating(r, true);

                if (IsRankEnabled && Rating == 0)
                    Rating = r;
            }
        }

        [XmlIgnore]
        public string RankExt
        {
            get
            {
                return IsRankEnabled ? _rankExt : GetRankFromRating(Rating, true);
            }
            set
            {
                int l = GetLevelByRank(value);
                int r = GetRatingByLevel(l);
                _levelFromRank = l;
                _rankExt = GetRankFromRating(r, true);
                _rank = GetRankFromRating(r, false);

                if (IsRankEnabled && Rating == 0)
                    Rating = r;
            }
        }

        public string Country { get; set; }
        public string Club { get; set; }
        public string Nationality { get; set; }
        public string City { get; set; }
        public string Team { get; set; }
        public string Coach { get; set; }
        public int Grade { get; set; }
        
        public static string GetRankFromRating(int rating, bool fullLiteral = false)
        {
            string result;
            int r;
            bool kyu = true;

            if (rating < 0)
                rating = 0;
            if (rating >= 100)
            {
                r = rating / 100;
                if (r > 20)
                {
                    result = (r - 20).ToString();
                    kyu = false;
                }
                else
                    result = (21 - r).ToString();
            }
            else
            {
                r = rating / 10;
                result = (30 - r).ToString();
            }

            string literal = fullLiteral ? " " + (kyu ? LangResources.LR.Kyu : LangResources.LR.Dan) : (kyu ? LangResources.LR.K : LangResources.LR.D);
            return result + literal;
        }

        public static int GetLevelByRank(string rank) // >0 - dan, else - kyu
        {
            int result;

            string s = string.IsNullOrEmpty(rank) ? string.Empty : rank.Trim().ToLower();
            string Rank = string.Empty;
            int i = 0;
            while (i < s.Length)
            {
                if (s[i] >= '0' && s[i] <= '9')
                    Rank += s[i];
                else
                    break;
                i++;
            }
            int r = 0;
            int.TryParse(Rank, out r);
            if (r == 0)
                r = 30;

            string kind = s.Substring(Rank.Length).Trim();
            bool dan = (kind == "d" || kind == "dan" || kind == "д" || kind == "дан" || kind == LangResources.LR.D || kind == LangResources.LR.Dan);

            if (dan)
                result = r;
            else
                result = -r;

            return result;
        }

        /// <summary>
        /// Get flat rating for handicap comparing
        /// </summary>
        /// <param name="rating">Rating</param>
        /// <returns>Comparable int for handicap calculation</returns>
        public static int GetHandicupBaseByRating(int rating)
        {
            int result;

            if (rating >= 100)
                result = rating;
            else
                result = 100 - (100 - rating) * 10;

            return result;
        }

        public static int GetRatingByLevel(int level)
        {
            int result;

            bool dan = level > 0;
            if (dan)
                result = 2000 + level * 100;
            else
                if (-level <= 20)
                    result = 2000 - (-level - 1) * 100;
                else
                    result = 0 + (30 + level) * 10;

            if (result < 10)
                result = 0;

            return result;
        }

        public static int GetRatingByRank(string rank)
        {
            int level = GetLevelByRank(rank);
            return GetRatingByLevel(level);
        }
        
        private int _rating;

        public int Rating
        {
            get
            {
                return _rating;
            }
            set
            {
                _rating = value;
            }
        }
        public int EgdPin { get; set; }

        [XmlIgnore]
        public int Order { get; set; }

        public PlayerInfo(Tournament tournament)
        {
            RootTournament = tournament;
        }

        public PlayerInfo()
        {
        }
    }

    public class TourInfo : ICloneable
    {
        public double Rating;
        public double Diff;
        public Player Competitor;
        public int CompetitorId;

        public double MMS;
        public double MMSX;
        public double MMSM; //max
        public double SOS;
        public double SOSOS;
        public double SODOS;
        public double SOUD;
        public double Points;

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    [Serializable]
    public class Player : INotifyPropertyChanged, ICloneable
    {
        [XmlAttribute(AttributeName = "typeversion")]
        public int TypeVersion = 1;

        public int Id { get; set; }

        public bool Joker { get; set; }
        public bool PreliminaryRegistration { get; set; }
        [XmlIgnore]
        public bool FinalRegistration { get { return !PreliminaryRegistration; } set { PreliminaryRegistration = !value; } }

        public bool SuperBarMember { get; set; }

        [XmlIgnore]
        public bool HiGroupMember { get; set; } //calculated Upper bar
        [XmlIgnore]
        public bool LoGroupMember { get; set; } //calculated Lower bar

        [XmlIgnore]
        public bool TopGroupMember { get { return HiGroupMember || SuperBarMember; } } //calculated Upper + Super bars

        [XmlIgnore]
        public bool AlternativeNameIfEmpty
        {
            get { return Info.AlternativeNameIfEmpty; }
            set { Info.AlternativeNameIfEmpty = value; }
        }

        [XmlIgnore]
        public string FirstName
        {
            get { return Info.FirstName; }
            set { Info.FirstName = value; }
        }
        [XmlIgnore]
        public string Surname
        {
            get { return Info.Surname; }
            set { Info.Surname = value; }
        }
        [XmlIgnore]
        public string InternationalName
        {
            get
            {
                return !string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(Surname)
                    ? Surname + ", " + FirstName : !string.IsNullOrEmpty(FirstName)
                    ? FirstName : Surname;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    FirstName = string.Empty;
                    Surname = string.Empty;
                }
                else
                {
                    int i = value.IndexOf(", ");
                    if (i < 0)
                    {
                        Surname = value;
                        FirstName = string.Empty;
                    }
                    else
                    {
                        Surname = value.Substring(0, i);
                        FirstName = value.Substring(i + 2);
                    }
                }
            }
        }

        [XmlIgnore]
        public string ExportName
        {
            get
            {
                return GetExportLocalName(true);
            }
        }

        private string GetExportLocalName(bool eng)
        {
            string name = GetName(eng);
            string surname = string.Empty;
            string firstName = string.Empty;

            int i = name.IndexOf(", ");
            if (i < 0)
            {
                surname = name;
                firstName = string.Empty;
            }
            else
            {
                surname = name.Substring(0, i);
                firstName = name.Substring(i + 2);
            }

            surname = surname.Replace(' ', '_');
            firstName = firstName.Replace(' ', '_');
            if (string.IsNullOrEmpty(surname))
                surname = "?";
            if (string.IsNullOrEmpty(firstName))
                firstName = "?";
            return surname + " " + firstName;
        }

        [XmlIgnore]
        public string ExportLocalName
        {
            get
            {
                return GetExportLocalName(false);
            }
        }

        bool _presentInRSystem;
        [XmlIgnore]
        public bool PresentInRSystem { get { return _presentInRSystem; } }

        public string GetName(bool eng = false)
        {
            string name = null;
            _presentInRSystem = false;

            if (!Tournament.UseTransliteration)
            {
                name = this.InternationalName;
            }

            if (string.IsNullOrEmpty(name))
            {
                if (Tournament.RSystem != null && !(string.IsNullOrEmpty(this.FirstName) && string.IsNullOrEmpty(this.Surname)))
                {
                    var person = Tournament.RSystem.Persons.Find(p =>
                        p.FirstName == this.FirstName && p.LastName == this.Surname ||
                        p.FirstNameUa == this.FirstName && p.LastNameUa == this.Surname ||
                        p.FirstNameEn == this.FirstName && p.LastNameEn == this.Surname ||
                        (!(string.IsNullOrEmpty(this.LocalFirstName) && string.IsNullOrEmpty(this.LocalSurname)) &&
                        p.FirstNameUa == this.LocalFirstName && p.LastNameUa == this.LocalSurname ||
                        p.FirstName == this.LocalFirstName && p.LastName == this.LocalSurname)
                        );
                    if (person != null)
                    {
                        if (eng)
                        {
                            name = person.NameEn;
                        }
                        else
                        {
                            switch (Translator.Language)
                            {
                                case "uk": name = person.NameUa; break;
                                case "en": name = person.NameEn; break;
                                default: name = person.Name; break;
                            }
                        }
                        _presentInRSystem = true;
                    }
                }
            }

            if (string.IsNullOrEmpty(name))
            {
                if (Tournament.UseTransliteration)
                {
                    if (eng)
                    {
                        name = this.InternationalName;
                    }
                    else
                    {
                        switch (Translator.Language)
                        {
                            case "en": name = this.InternationalName; break;
                            default: name = this.LocalName; break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(name))
                    name = this.InternationalName;
                if (string.IsNullOrEmpty(name))
                    name = this.LocalName;

                return !string.IsNullOrEmpty(name) ? name : string.Empty;

            }

            return !string.IsNullOrEmpty(name) ? name : string.Empty;
        }

        public string GetClubName()
        {
            if (Tournament.RSystem != null && !string.IsNullOrEmpty(Info.Club))
            {
                var club = Tournament.RSystem.Clubs.Find(cl => cl.Name == Info.Club || cl.NameUa == Info.Club || cl.NameUa == Info.Club || cl.NameEn == Info.Club || cl.EGDName == Info.Club);
                if (club != null)
                {
                    string clName = null;
                    switch (Translator.Language)
                    {
                        case "uk": clName = club.NameUa; break;
                        case "en": clName = club.NameEn; break;
                        default: clName = club.Name; break;
                    }

                    if (!string.IsNullOrEmpty(clName) && string.IsNullOrEmpty(Country))
                    {
                        string cl = Info.Club;
                        switch (Tournament.RSystem.Kind)
                        {
                            case RtKind.ru: Country = "ru"; break;
                            case RtKind.ua: Country = "ua"; break;
                        }
                        Info.Club = cl;
                    }

                    if (string.IsNullOrEmpty(clName))
                        clName = Info.Club;

                    return !string.IsNullOrEmpty(clName) ? clName : string.Empty;


                }
            }

            return !string.IsNullOrEmpty(Info.Club) ? Info.Club : string.Empty;
        }

        [XmlIgnore]
        public string Name
        {
            get
            {
                return GetName();//!string.IsNullOrEmpty(LocalName) ? LocalName : InternationalName; 
            }
            set { LocalName = value; OnPropertyChanged("Name"); }
        }

        [XmlIgnore]
        public double CurrentScore { get; set; } //For pair displayling

        [XmlIgnore]
        public string NameExt
        {
            get { return string.IsNullOrEmpty(Rank) ? Name : Name + " (" + RankExt + ")"; }
        }

        [XmlIgnore]
        public string NameExt2
        {
            get { return string.IsNullOrEmpty(Rank) ? Name : Name + " (" + CurrentScore + " " + LangResources.GetPointsStr(CurrentScore) + ", " + RankExt + ")"; }
        }

        [XmlIgnore]
        public string InternationalNameExt
        {
            get { return string.IsNullOrEmpty(Rank) ? InternationalName : InternationalName + " (" + RankExt + ")"; }
        }

        [XmlIgnore]
        public string InternationalNameExt2
        {
            get { return string.IsNullOrEmpty(Rank) ? InternationalName : InternationalName + " (" + CurrentScore + " " + LangResources.GetPointsStr(CurrentScore) + ", " + RankExt + ")"; }
        }
        
        [XmlIgnore]
        public string LocalNameExt
        {
            get { return string.IsNullOrEmpty(Rank) ? LocalName : LocalName + " (" + RankExt + ")"; }
        }

        [XmlIgnore]
        public string LocalFirstName
        {
            get { return Info.LocalFirstName; }
            set { Info.LocalFirstName = value; }
        }
        [XmlIgnore]
        public string LocalSurname
        {
            get { return Info.LocalSurname; }
            set { Info.LocalSurname = value; }
        }
        [XmlIgnore]
        public string LocalName
        {
            get
            {
                return !string.IsNullOrEmpty(LocalFirstName) && !string.IsNullOrEmpty(LocalSurname)
                  ? LocalSurname + ", " + LocalFirstName : !string.IsNullOrEmpty(LocalFirstName)
                  ? LocalFirstName : LocalSurname;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    LocalFirstName = string.Empty;
                    LocalSurname = string.Empty;
                }
                else
                {
                    int i = value.IndexOf(", ");
                    if (i < 0)
                    {
                        LocalSurname = value;
                        LocalFirstName = string.Empty;
                    }
                    else
                    {
                        LocalSurname = value.Substring(0, i);
                        LocalFirstName = value.Substring(i + 2);
                    }
                }
            }
        }

        [XmlIgnore]
        public LangResources Capt
        {
            get { return LangResources.LR; }
        }

        [XmlIgnore]
        public string Comment { get; set; }
        [XmlElement(ElementName = "Comment")]
        public XmlNode[] CommentCData
        {
            get
            {
                var dummy = new XmlDocument();
                return new XmlNode[] { dummy.CreateCDataSection(Comment) };
            }
            set
            {
                if (value == null)
                {
                    Comment = null;
                    return;
                }

                if (value.Length != 1)
                {
                    throw new InvalidOperationException(
                        String.Format(
                            "Invalid array length {0}", value.Length));
                }

                var node0 = value[0];
                var cdata = node0 as XmlCDataSection;
                if (cdata == null)
                {
                    throw new InvalidOperationException(
                        String.Format(
                            "Invalid node type {0}", node0.NodeType));
                }

                Comment = cdata.Data;
            }
        }

        [XmlIgnore]
        private double _startScores { get; set; }
        public double StartScores { get { return _startScores; } set { _startScores = value; OnPropertyChanged("StartScores"); } }

        [XmlIgnore]
        private double _startNumber { get; set; }
        public double StartNumber { get { return _startNumber; } set { _startNumber = value; OnPropertyChanged("StartNumber"); } }

        [XmlIgnore]
        private bool _scoreAdjustment { get; set; }
        public bool ScoreAdjustment { get { return _scoreAdjustment; } set { _scoreAdjustment = value; OnPropertyChanged("ScoreAdjustment"); } }

        private double _scoreAdjustmentValue;
        [XmlIgnore]
        public double ScoreAdjustmentValue
        {
            get { return _scoreAdjustmentValue; }
            set { _scoreAdjustmentValue = value; }
        }
        [XmlIgnore]
        public double ScoreAdjustmentValueEx
        {
            get { return ScoreAdjustment ? _scoreAdjustmentValue : 0; }
        }
        [XmlElement(ElementName = "ScoreAdjustmentValue")]
        public int ExportScoreAdjustmentValue
        {
            get { return Convert.ToInt32(ScoreAdjustmentValue * 4); }
            set
            {
                ScoreAdjustmentValue = (double)value / 4;
            }
        }

        public double GetAdjustedStartScores()
        {
            return _startScores + _scoreAdjustmentValue;
        }

        [XmlIgnore]
        public double AdjustedStartScores
        {
            get { return StartScores + ScoreAdjustmentValueEx; }
            set { StartScores = value - ScoreAdjustmentValueEx; }
        }
        
        [XmlIgnore]
        public string Country
        {
            get { return Info.Country; }
            set
            {
                if (value != "Tourtoss.BE.Country")
                {
                    if (Info.Country != value)
                    {
                        Info.Club = string.Empty;
                        OnPropertyChanged("Club");
                    }
                    Info.Country = value;
                    OnPropertyChanged("IsCountrySet");
                }
            }
        }

        [XmlIgnore]
        public string Club
        {
            get { return GetClubName(); }
            set
            {
                if (value != null)
                    Info.Club = value;
                else
                    Info.Club = string.Empty;
                OnPropertyChanged("Club");
            }
        }

        [XmlIgnore]
        public bool IsCountrySet { get { return !string.IsNullOrEmpty(Country); } }

        [XmlIgnore]
        public string Nationality { get { return Info.Nationality; } set { Info.Nationality = value; } }

        [XmlIgnore]
        public string City { get { return Info.City; } set { Info.City = value; OnPropertyChanged("City"); } }

        [XmlIgnore]
        public string Team { get { return Info.Team; } set { Info.Team = value; OnPropertyChanged("Team"); } }

        [XmlIgnore]
        public string Coach { get { return Info.Coach; } set { Info.Coach = value; OnPropertyChanged("Coach"); } }

        [XmlIgnore]
        public int Grade { get { return Info.Grade; } set { Info.Grade = value; if (!IsCalculating) { OnPropertyChanged("Grade"); } } }

        [XmlIgnore]
        public string Rank { get { return Info.Rank; } set { Info.Rank = value; if (!IsCalculating) { OnPropertyChanged("Rating"); } } }

        [XmlIgnore]
        public string RankExt { get { return Info.RankExt; } set { Info.RankExt = value; if (!IsCalculating) { OnPropertyChanged("Rating"); } } }

        [XmlIgnore]
        public bool IsRankEnabled { get { return Info.IsRankEnabled; } }

        [XmlIgnore]
        public int Rating { get { return Info.Rating; } set { Info.Rating = value; if (!IsCalculating) { OnPropertyChanged("Rank"); OnPropertyChanged("RankExt"); } } }

        [XmlIgnore]
        public List<TourInfo> TourInfoList = new List<TourInfo>();

        [XmlIgnore]
        public double RatingC { get; set; }
        [XmlIgnore]
        public double RatingBonus { get; set; }
        [XmlIgnore]
        public bool RatingAbnormal { get; set; }

        [XmlIgnore]
        public double DefaultScore
        {
            get
            {
                return GetDefaultScore(Info.LevelFromRank, ScoreAdjustmentValueEx);
            }
        }

        public static double GetDefaultScore(int level, double adjust = 0)
        {
            return PlayerInfo.GetRatingByLevel(level) / 10 + adjust * 10 - 10;
        }

        [XmlIgnore]
        public int Order { get { return Info.Order; } set { Info.Order = value; } }

        [XmlElement]
        public List<int> NotPlayingInRound { get; set; }

        [XmlIgnore]
        public string NotPlayingInRoundStr
        {
            get
            {
                var sb = new StringBuilder();
                foreach (int item in NotPlayingInRound)
                {
                    if (sb.Length != 0)
                        sb.Append(" ");
                    sb.Append(Tour.ToRoman(item));
                }
                return sb.ToString();
            }
        }

        [XmlIgnore]
        public int EgdPin { get { return Info.EgdPin; } set { Info.EgdPin = value; } }

        [XmlElement(ElementName = "GoPlayer")]
        public PlayerInfo Info { get; set; }

        [XmlIgnore]
        public bool IsCreated { get; set; }

        [XmlIgnore]
        public bool IsCalculating { get; set; }

        [XmlIgnore]
        public bool SharedPlace { get; set; }

        [XmlIgnore]
        public string PlaceStr { get; set; }

        #region methods

        public double GetCoef(Tournament tournament, Entity coef, int tourNumber, bool manageTourNumber = true)
        {
            double result = 0;

            if (manageTourNumber && !tournament.TakeCurrentRoundInAccount)
            {
                tourNumber--;
            }

            switch (coef)
            {
                case Entity.Points: result = GetPoints(tournament, tourNumber); break;
                case Entity.Score: result = GetMMS(tournament, tourNumber); break;
                case Entity.ScoreX: result = GetMMS(tournament, tourNumber, ScoreKind.ScoreX); break;
                case Entity.ScoreM: result = GetMMS(tournament, tourNumber, ScoreKind.ScoreMax); break;
                case Entity.SODOS: result = GetSodos(tournament, tourNumber); break;
                case Entity.SOSOS: result = GetSosos(tournament, tourNumber); break;
                case Entity.SORP: result = GetSorp(tournament, tourNumber); break;
                case Entity.SOS: result = GetSos(tournament, tourNumber); break;
                case Entity.SOUD: result = GetSoud(tournament, tourNumber); break;
                case Entity.NewRating: result = GetNewRating(tournament, tourNumber); break;
            }

            return result;
        }

        private enum ScoreKind
        { 
            Score,
            ScoreX,
            ScoreMax
        }

        private double GetMMS(Tournament tournament, int tourNumber, ScoreKind kind = ScoreKind.Score)
        {
            if (tourNumber > -1 && this.TourInfoList.Count > tourNumber)
            {
                switch (kind)
                {
                    case ScoreKind.ScoreX:
                        if (this.TourInfoList[tourNumber].MMSX > -1)
                            return this.TourInfoList[tourNumber].MMSX;
                        break;
                    case ScoreKind.ScoreMax:
                        if (this.TourInfoList[tourNumber].MMSM > -1)
                            return this.TourInfoList[tourNumber].MMSM;
                        break;
                    case ScoreKind.Score:
                        if (this.TourInfoList[tourNumber].MMS > -1)
                            return this.TourInfoList[tourNumber].MMS;
                        break;
                }
            }

            double result = StartScores + ScoreAdjustmentValueEx;

            bool isOddSkipping = false;

            for (int i = 0; i <= tourNumber; i++)
            {
                if (i < tournament.Tours.Count && tournament.Tours[i].Pairs != null)
                {
                    var pair = tournament.Tours[i].Pairs.Find(this.Id);
                    if (pair != null)
                    {
                        int competitorId = pair.FirstPlayerId == Id ? pair.SecondPlayerId : pair.FirstPlayerId;
                        switch (competitorId)
                        {
                            case -1: //Bye
                                if (kind != ScoreKind.ScoreX) result += 1;
                                break;
                            default:
                                if (pair.FirstPlayerId == this.Id)
                                {
                                    switch (pair.GameResult)
                                    {
                                        case 1: result++; break;
                                    }
                                    break;
                                }
                                else
                                    if (pair.SecondPlayerId == this.Id)
                                    {
                                        switch (pair.GameResult)
                                        {
                                            case 2: result++; break;
                                        }
                                        break;
                                    }
                                break;
                        }
                    }
                    else //no pair
                    {
                        if (kind == ScoreKind.ScoreMax)
                        {
                            result += 0.5f;
                        } 
                        else
                        if (tournament.CustomizeCalculation && !(tournament.CustomizeForTopGroupOnly && !this.TopGroupMember))
                        {
                            if (tournament.CalculationScoreAddHalf)
                            { 
                                result += 0.5f; 
                            }
                            else
                                if (tournament.CalculationScoreAddAlternate)
                                {
                                    if (isOddSkipping)
                                    { 
                                        result += 1; 
                                    }
                                    isOddSkipping = !isOddSkipping;
                                }
                        }
                        else // Default calculation
                        {                            
                            if (!this.NotPlayingInRound.Contains(i + 1))
                            {
                                //is not paired
                                result += 0.5f;
                            }
                            else
                            {
                                //does not participate a round
                                result += 0.5f;
                            }
                        }
                    }
                }
            }

            return result;
        }

        public double GetPoints(Tournament tournament, int tourNumber)//Number of wins
        {
            double result = 0;

            if (tourNumber > -1 && this.TourInfoList.Count > tourNumber && this.TourInfoList[tourNumber].Points > -1)
                return this.TourInfoList[tourNumber].Points;

            for (int i = 0; i <= tourNumber; i++)
            {
                if (i < tournament.Tours.Count && tournament.Tours[i].Pairs != null)
                {
                    var pair = tournament.Tours[i].Pairs.Find(this.Id);
                    if (pair != null)
                    {
                        if (pair.FirstPlayerId <= 0 || pair.SecondPlayerId <= 0)
                        {
                            //bye
                            result++;
                        }
                        else
                            if (pair.FirstPlayerId == this.Id)
                            {
                                switch (pair.GameResult)
                                {
                                    case 1: result++; break;
                                    //case 2: result--; break;
                                }
                            }
                            else
                                if (pair.SecondPlayerId == this.Id)
                                {
                                    switch (pair.GameResult)
                                    {
                                        //case 1: result--; break;
                                        case 2: result++; break;
                                    }
                                }

                    }
                }
            }
            return result;
        }

        public double GetSoud(Tournament tournament, int tourNumber)//Up-down balance
        {
            int result = 0;

            if (tourNumber > -1 && this.TourInfoList.Count > tourNumber && this.TourInfoList[tourNumber].SOUD > -1)
                return this.TourInfoList[tourNumber].SOUD;

            if (this.Id > -1)
                for (int i = 0; i <= tourNumber; i++)
                {
                    if (i < tournament.Tours.Count)
                    {
                        var pair = tournament.Tours[i].Pairs.Find(this.Id);
                        if (pair != null)
                        {
                            if (pair.FirstPlayerId == this.Id || pair.SecondPlayerId == this.Id)
                            {
                                int competitorId = pair.FirstPlayerId == Id ? pair.SecondPlayerId : pair.FirstPlayerId;
                                switch (competitorId)
                                {
                                    case -2: break;
                                    case -1: break;
                                    default:
                                        if (pair.FirstPlayerId == this.Id)
                                        {
                                            result += pair.UpDownBalance;
                                            break;
                                        }
                                        else
                                            if (pair.SecondPlayerId == this.Id)
                                            {
                                                result -= pair.UpDownBalance;
                                                break;
                                            }
                                        break;
                                }
                            }
                        }
                    }
                }
            return result;
        }

        public double GetSos(Tournament tournament, int tourNumber)
        {
            double result = 0;

            if (tourNumber > -1 && this.TourInfoList.Count > tourNumber && this.TourInfoList[tourNumber].SOS > -1)
                return this.TourInfoList[tourNumber].SOS;

            for (int i = 0; i <= tourNumber; i++)
            {
                bool noPair = false;
                bool withBye = false;
                bool participate = !this.NotPlayingInRound.Contains(i + 1);

                if (participate && i < tournament.Tours.Count)
                {
                    var pair = tournament.Tours[i].Pairs.Find(this.Id);
                    int competitorId = -1;
                    if (pair == null)
                    {
                        noPair = true;
                    }
                    else
                    {
                        competitorId = pair.FirstPlayerId == Id ? pair.SecondPlayerId : pair.FirstPlayerId;
                        if (competitorId <= 0)
                            withBye = true;

                        if (!withBye)
                        {
                            var competitor = tournament.Players.GetById(competitorId);
                            if (competitor != null)
                            {
                                if (tournament.CustomizeCalculation && tournament.CalculationScoreMaxToCompetitorCoefs
                                    && !(tournament.CustomizeForTopGroupOnly && !competitor.TopGroupMember))
                                {
                                    result += competitor.GetMMS(tournament, tourNumber, ScoreKind.ScoreMax);
                                }
                                else // Default calculation
                                {
                                    result += competitor.GetMMS(tournament, tourNumber);
                                }
                                if (tournament.HandicapIncludeInTieBreakers)
                                {
                                    if (pair.FirstPlayerId == this.Id)
                                        result -= pair.Handicap;
                                    else
                                        result += pair.Handicap;
                                }
                            }
                        }
                    }

                }

                if (!participate || noPair)
                {
                    if (tournament.CustomizeCalculation && tournament.CalculationScoreZeroToOwnCoefs
                        && !(tournament.CustomizeForTopGroupOnly && !this.TopGroupMember))
                    {
                        // add zero
                    }
                    else // Default calculation
                    {
                        //incomplete pair - add start points of itself
                        var player = tournament.Players.GetById(this.Id);
                        if (player != null)
                            result += player.GetMMS(tournament, -1);
                    }
                } else
                if (withBye)
                {
                    //incomplete pair - add MMS of itself mines 1
                    var player = tournament.Players.GetById(this.Id);
                    if (player != null)
                        result += player.GetMMS(tournament, tourNumber) - 1;
                }
            }
            return result;
        }

        public double GetSodos(Tournament tournament, int tourNumber)
        {
            double result = 0;

            if (tourNumber > -1 && this.TourInfoList.Count > tourNumber && this.TourInfoList[tourNumber].SODOS > -1)
                return this.TourInfoList[tourNumber].SODOS;

            for (int i = 0; i <= tourNumber; i++)
            {
                bool withBye = false;
                bool participate = !this.NotPlayingInRound.Contains(i + 1);
                bool isWin = true;

                if (participate && i < tournament.Tours.Count)
                {
                    var pair = tournament.Tours[i].Pairs.Find(this.Id);
                    int competitorId = -1;
                    if (pair != null)
                    {
                        competitorId = pair.FirstPlayerId == Id ? pair.SecondPlayerId : pair.FirstPlayerId;
                        if (competitorId <= 0)
                            withBye = true;

                        if ((pair.FirstPlayerId == Id && pair.GameResult != 1) |
                            (pair.SecondPlayerId == Id && pair.GameResult != 2)
                            )
                            isWin = false;

                        if (!withBye && isWin)
                        {
                            var competitor = tournament.Players.Find(delegate(Player item) { return item != null && item.Id == competitorId; });
                            if (competitor != null)
                            {
                                if (tournament.CustomizeCalculation && tournament.CalculationScoreMaxToCompetitorCoefs
                                    && !(tournament.CustomizeForTopGroupOnly && !competitor.TopGroupMember))
                                {
                                    result += competitor.GetMMS(tournament, tourNumber, ScoreKind.ScoreMax);
                                }
                                else // Default calculation
                                {
                                    result += competitor.GetMMS(tournament, tourNumber);
                                }
                                if (tournament.HandicapIncludeInTieBreakers)
                                {
                                    if (pair.FirstPlayerId == this.Id)
                                        result -= pair.Handicap;
                                    else
                                        result += pair.Handicap;
                                }
                            }
                        }
                    }

                }

                if (withBye)
                {
                    //incomplete pair - add MMS of itself mines 1
                    var player = tournament.Players.GetById(this.Id);
                    if (player != null)
                        result += player.GetMMS(tournament, tourNumber) - 1;
                }

            }
            return result;
        }

        public double GetSosos(Tournament tournament, int tourNumber)
        {
            double result = 0;

            if (tourNumber > -1 && this.TourInfoList.Count > tourNumber && this.TourInfoList[tourNumber].SOSOS > -1)
                return this.TourInfoList[tourNumber].SOSOS;

            for (int i = 0; i <= tourNumber; i++)
            {
                bool noPair = false;
                bool withBye = false;
                bool participate = !this.NotPlayingInRound.Contains(i + 1);

                if (participate && i < tournament.Tours.Count)
                {
                    var pair = tournament.Tours[i].Pairs.Find(this.Id);
                    int competitorId = -1;
                    if (pair == null)
                    {
                        noPair = true;
                    }
                    else
                    {
                        competitorId = pair.FirstPlayerId == Id ? pair.SecondPlayerId : pair.FirstPlayerId;
                        if (competitorId <= 0)
                            withBye = true;
                        else
                        {
                            var competitor = tournament.Players.Find(delegate(Player item) { return item != null && item.Id == competitorId; });
                            if (competitor != null)
                                result += competitor.GetSos(tournament, tourNumber);
                        }
                    }
                }

                if (!participate || noPair)
                {
                    if (tournament.CustomizeCalculation && tournament.CalculationScoreZeroToOwnCoefs
                        && !(tournament.CustomizeForTopGroupOnly && !this.TopGroupMember))
                    {
                    }
                    else // Default calculation
                    {
                        //incomplete pair - add som of own starting scores
                        var player = tournament.Players.Find(delegate(Player item) { return item != null && item.Id == this.Id; });
                        if (player != null)
                            result += player.GetMMS(tournament, -1) * (tourNumber + 1);
                    }
                }
                else
                {
                    if (withBye)
                    {
                        //playing with bye - add points of itself
                        var player = tournament.Players.Find(delegate(Player item) { return item != null && item.Id == this.Id; });
                        if (player != null)
                            result += player.GetSos(tournament, tourNumber);
                    }
                }
            }

            return result;
        }

        public double GetSorp(Tournament tournament, int tourNumber)
        {
            double result = 0;

            for (int i = 0; i <= tourNumber; i++)
            {
                if (i < tournament.Tours.Count)
                {
                    var pair = tournament.Tours[i].Pairs.Find(this.Id);
                    if (pair != null)
                    {

                        if ((pair.FirstPlayerId == Id && pair.GameResult != 1) |
                            (pair.SecondPlayerId == Id && pair.GameResult != 2)
                            )
                            continue;

                        result += pair.ResultPoints;
                    }
                }
            }
            return result;
        }

        public double GetNewRating(Tournament tournament, int tourNumber)
        {
            double result = 0;

            if (TourInfoList.Count > 0)
            {
                if (tourNumber >= TourInfoList.Count)
                    tourNumber = TourInfoList.Count - 1;
                if (tourNumber < TourInfoList.Count)
                {
                    while (tourNumber >= 0 && result == 0)
                    {
                        result = TourInfoList[tourNumber].Rating;
                        tourNumber--;
                    }
                }
            }

            if (result == 0)
                result = Rating;

            return result;
        }

        private Tournament _tournament;

        [XmlIgnore]
        public Tournament RootTournament
        {
            get { return _tournament; }
            set { _tournament = value; if (Info != null) Info.RootTournament = _tournament; }
        }

        public Player(Tournament tournament)
        {
            _tournament = tournament;
            Info = new PlayerInfo(tournament);
            NotPlayingInRound = new List<int>();
            PreliminaryRegistration = false;
            IsCreated = true;
        }

        public Player()
        {
            Info = new PlayerInfo();
            NotPlayingInRound = new List<int>();
            PreliminaryRegistration = false;
            IsCreated = true;
        }

        public void CopyTo(Player result)
        {
            result.RootTournament = RootTournament;
            result.Id = Id;
            result.FirstName = FirstName;
            result.Surname = Surname;
            result.LocalFirstName = LocalFirstName;
            result.LocalSurname = LocalSurname;
            result.Comment = Comment;
            result.Country = Country;
            result.Club = Club;
            result.Nationality = Nationality;
            result.ExportScoreAdjustmentValue = ExportScoreAdjustmentValue;
            result.NotPlayingInRound.Clear();
            result.NotPlayingInRound.AddRange(NotPlayingInRound);
            result.Order = Order;
            result.Rank = Rank;
            result.RankExt = RankExt;
            result.Rating = Rating;
            result.RatingC = RatingC;
            result.RatingAbnormal = RatingAbnormal;
            result.RatingBonus = RatingBonus;
            result.ScoreAdjustment = ScoreAdjustment;
            result.ScoreAdjustmentValue = ScoreAdjustmentValue;
            result.StartScores = StartScores;
            result.StartNumber = StartNumber;
            result.Team = Team;
            result.Coach = Coach;
            result.Grade = Grade;

            result.PreliminaryRegistration = PreliminaryRegistration;
            result.Joker = Joker;
            result.SuperBarMember = SuperBarMember;
            result.HiGroupMember = HiGroupMember;
            result.LoGroupMember = LoGroupMember;

            result.TourInfoList.AddRange(TourInfoList);
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public override bool Equals(object obj)
        {
            var player = obj as Player;
            if (player == null)
            {
                return false;
            }

            if (
                player.RootTournament != RootTournament ||
                player.Id != Id ||
                player.FirstName != FirstName ||
                player.Surname != Surname ||
                player.LocalFirstName != LocalFirstName ||
                player.LocalSurname != LocalSurname ||
                player.Comment != Comment ||
                player.Country != Country ||
                player.Club != Club ||
                player.Nationality != Nationality ||
                player.ExportScoreAdjustmentValue != ExportScoreAdjustmentValue ||
                player.Order != Order ||
                player.Rank != Rank ||
                player.RankExt != RankExt ||
                player.Rating != Rating ||
                player.RatingC != RatingC ||
                player.RatingAbnormal != RatingAbnormal ||
                player.RatingBonus != RatingBonus ||
                player.ScoreAdjustment != ScoreAdjustment ||
                player.ScoreAdjustmentValue != ScoreAdjustmentValue ||
                player.StartScores != StartScores ||
                player.StartNumber != StartNumber ||
                player.Team != Team ||
                player.Coach != Coach ||
                player.Grade != Grade ||

                player.PreliminaryRegistration != PreliminaryRegistration ||
                player.Joker != Joker ||
                player.SuperBarMember != SuperBarMember ||
                player.HiGroupMember != HiGroupMember ||
                player.LoGroupMember != LoGroupMember
                )
            {
                return false;
            }
            else
            {
                if (player.NotPlayingInRound.Count != NotPlayingInRound.Count)
                    return false;
                for (int i = 0; i < player.NotPlayingInRound.Count; i++)
                {
                    if (player.NotPlayingInRound[i] != NotPlayingInRound[i])
                        return false;
                }
                return true;
            }
        }

        public object Clone()
        {
            var result = new Player(_tournament);

            CopyTo(result);

            return result;
        }

        public void Update()
        {
            OnPropertyChanged("Name");
            OnPropertyChanged("Surname");
            OnPropertyChanged("FirstName");
            OnPropertyChanged("LocalName");
            OnPropertyChanged("LocalSurname");
            OnPropertyChanged("LocalFirstName");
            OnPropertyChanged("StartScores");
            OnPropertyChanged("SuperBarMember");
            OnPropertyChanged("Rank");
            OnPropertyChanged("RankExt");
            OnPropertyChanged("Rating");
            OnPropertyChanged("Country");
            OnPropertyChanged("Club");
            OnPropertyChanged("Team");
            OnPropertyChanged("Coach");
            OnPropertyChanged("Grade");
            OnPropertyChanged("NotPlayingInRoundStr");
            OnPropertyChanged("Comment");
            OnPropertyChanged("PreliminaryRegistration");
        }

        public void RefreshLang()
        {
            RankExt = RankExt;
            Update();
        }

        #endregion

        #region events

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            Tournament.Changed = true;
            if (null != this.PropertyChanged)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

    }



    // Written by JvanLangen
    public class BufferedObservableCollection<T> : ObservableCollection<T>
    {
        // the last action used
        public NotifyCollectionChangedAction? _lastAction = null;
        // the items to be buffered
        public List<T> _itemBuffer = new List<T>();

        // constructor registeres on the CollectionChanged
        public BufferedObservableCollection()
        {
            base.CollectionChanged += new NotifyCollectionChangedEventHandler(ObservableCollectionUpdate_CollectionChanged);
        }

        // When the collection changes, buffer the actions until the 'user' changes action or flushes it.
        // This will batch add and remove actions.
        private void ObservableCollectionUpdate_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // if we have a lastaction, check if it is changed and should be flush else only change the lastaction
            if (_lastAction.HasValue)
            {
                if (_lastAction != e.Action)
                {
                    Flush();
                    _lastAction = e.Action;
                }
            }
            else
                _lastAction = e.Action;

            _itemBuffer.AddRange(e.NewItems.Cast<T>());
        }

        // Raise the new event.
        protected void RaiseCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (this.CollectionChanged != null)
                CollectionChanged(sender, e);
        }

        // Don't forget to flush the list when your ready with your action or else the last actions will not be 'raised'
        public void Flush()
        {
            if (_lastAction.HasValue && (_itemBuffer.Count > 0))
            {
                RaiseCollectionChanged(this, new NotifyCollectionChangedEventArgs(_lastAction.Value, _itemBuffer));
                _itemBuffer.Clear();
                _lastAction = null;
            }
        }

        // new event
        public override event NotifyCollectionChangedEventHandler CollectionChanged;
    }

    public class ObservableList<T> : ObservableCollection<T>
    {
        public void Sort(Comparison<T> comparison)
        {
            for (int i = this.Count - 1; i >= 0; i--)
            {
                for (int j = 1; j <= i; j++)
                {
                    var x = this.ElementAt(j - 1);
                    var y = this.ElementAt(j);
                    if (comparison(x, y) > 0)
                    {
                        this.Move(j - 1, j);
                    }
                }
            }
        }

        public int FindIndex(Func<T, bool> predicate)
        {
            int result = -1;
            for (int i = 0; i < Count; i++)
            {
                if (predicate(this[i]))
                {
                    result = i;
                    break;
                }
            }
            return result;
        }

        public T Find(Predicate<T> match)
        {
            T result = default(T);
            for (int i = 0; i < Count; i++)
            {
                if (match(this[i]))
                {
                    result = this[i];
                    break;
                }
            }
            return result;
        }

        public void AddRange(List<T> items)
        {
            foreach (var item in items)
                base.Add(item);
        }
    }

    public class PlayerList : ObservableList<Player>
    {
        private int GetMaxId()
        {
            int maxId = 0;
            foreach (var player in this)
            {
                if (player.Id > maxId)
                    maxId = player.Id;
            }
            return maxId;
        }

        public void FillIDs()
        {
            int maxId = 0;
            int i = 0;
            int[] ids = new int[Count];

            foreach (var player in this)
            {
                if (player != null)
                {
                    if (player.Id == 0 || Array.IndexOf(ids, player.Id) > -1)
                    {
                        if (maxId == 0)
                            maxId = GetMaxId();
                        maxId++;
                        player.Id = maxId;
                    }
                    ids[i++] = player.Id;
                }
            }
        }

        public new void Insert(int index, Player item)
        {
            base.Insert(index, item);
            FillIDs();
        }

        public new void Add(Player item)
        {
            base.Add(item);
            FillIDs();
        }

        public void Add(Player item, bool noUpdate)
        {
            base.Add(item);

            if (!noUpdate)
            {
                FillIDs();
            }
        }

        public void AddRange(PlayerList items)
        {
            foreach (var item in items)
            {
                base.Add(item);
            }
            FillIDs();
        }

        public void RemovePreliminaryRegistered()
        {
            for (int i = Count - 1; i >= 0; i--)
                if (this[i].PreliminaryRegistration)
                    this.RemoveAt(i);
        }

        public void FinalizePreliminaryRegistered()
        {
            foreach (var item in this)
                if (item.PreliminaryRegistration)
                    item.PreliminaryRegistration = false;
        }

        public void RefreshLang()
        {
            foreach (var item in this)
            {
                item.RankExt = item.RankExt;
                item.Update();
            }
        }

        public Player GetById(int Id)
        {
            int i = FindIndex(delegate(Player item) { return item.Id == Id; });
            return i > -1 ? this[i] : null;
        }

        public bool HasAnyBarMember(Tournament tournament)
        {
            bool result = false;

            if (tournament.UseMacMahonStartScores)
                foreach (Player player in this)
                {
                    if ((tournament.LowerMacMahonBar && player.LoGroupMember) ||
                     (tournament.UpperMacMahonBar && player.HiGroupMember) ||
                     (tournament.UseMacMahonSuperBar && player.SuperBarMember))
                    {
                        result = true;
                        break;
                    }
                    if (tournament.UpperMacMahonBar && PlayerInfo.GetRatingByRank(player.Rank) >= PlayerInfo.GetRatingByRank(tournament.UpperMacMahonBarLevel))
                    {
                        result = true;
                        break;
                    }
                    if (tournament.LowerMacMahonBar && PlayerInfo.GetRatingByRank(player.Rank) <= PlayerInfo.GetRatingByRank(tournament.LowerMacMahonBarLevel))
                    {
                        result = true;
                        break;
                    }
                }
            return result;
        }

        public bool HasDifferentStartScores(Tournament tournament)
        {
            double? old = null;

            foreach (Player player in this)
            {
                if (!old.HasValue)
                {
                    old = player.StartScores;
                }
                if (old.Value != player.StartScores)
                {
                    return true;
                }
            }
            return false;
        }

    }

    public class PairList : ObservableList<Pair>
    {

        private Hashtable Index = new Hashtable();

        private void UpdateIndex()
        {
            Index.Clear();
            int i = 0;
            foreach (var item in this)
            {
                if (!Index.ContainsKey(item.FirstPlayerId))
                    Index.Add(item.FirstPlayerId, i);
                if (!Index.ContainsKey(item.SecondPlayerId))
                    Index.Add(item.SecondPlayerId, i);
                i++;
            }
        }

        public Pair Find(int playerId)
        {
            Pair result = null;

            if (Index.Count == 0)
                UpdateIndex();
            if (Index.ContainsKey(playerId))
            {
                int? i = Index[playerId] as int?;
                if (i.HasValue && i < this.Count && i >= 0)
                    result = this[i.Value];
            }

            //result = base.Find(item => item != null && (item.FirstPlayerId == playerId || item.SecondPlayerId == playerId));
            return result;
        }

        public new void Remove(Pair pair)
        {
            base.Remove(pair);
            UpdateIndex();
        }

        public new void RemoveAt(int i)
        {
            base.RemoveAt(i);
            UpdateIndex();
        }

        private int GetMaxId()
        {
            int maxId = 0;
            foreach (var pair in this)
            {
                if (pair.BoardNumber > maxId)
                    maxId = pair.BoardNumber;
            }
            return maxId;
        }

        private bool IsAccessibeId(int id)
        {
            foreach (var pair in this)
            {
                if (pair.BoardNumber == id)
                {
                    return false;
                }
            }
            return true;
        }

        public void FillIDs(bool sort = false)
        {
            if (!this.sorting)
            {
                if (sort)
                {
                    Sort();
                }

                foreach (var pair in this)
                {
                    if (pair != null && !pair.BoardNumberFixed)
                        pair.BoardNumber = 0;
                }

                int id = 1;
                foreach (var pair in this)
                {
                    if (pair != null && pair.BoardNumber == 0)
                    {
                        while (!IsAccessibeId(id))
                        {
                            id++;
                        }
                        pair.BoardNumber = id;

                        if (!this.sorting)
                        {
                            pair.OnPropertyChanged("BoardNumber");
                        }
                    }
                }

                if (sort)
                {
                    this.Sort(CompareOrder);
                    UpdateIndex();
                }
            }
        }

        public void Add(Pair item, bool sort = true)
        {
            base.Add(item);
            FillIDs(sort);
        }

        public new void Add(Pair item)
        {
            Add(item, true);
        }

        public new void Clear()
        {
            base.Clear();
            UpdateIndex();
        }

        private int CompareStrength(Pair item1, Pair item2)
        {
            if (item1 == null && item2 == null)
                return 0;
            else
                if (item2 == null)
                    return 1;
                else
                    if (item1 == null)
                        return -1;

            int result = (int)(item2.Score - item1.Score);

            if (result == 0)
            {
                result = item2.Rating - item1.Rating;
            }

            if (result == 0)
            {
                int r2 = (item2.FirstPlayer != null ? item2.FirstPlayer.Rating : 0) +
                    (item2.SecondPlayer != null ? item2.SecondPlayer.Rating : 0);
                int r1 = (item1.FirstPlayer != null ? item1.FirstPlayer.Rating : 0) +
                    (item1.SecondPlayer != null ? item1.SecondPlayer.Rating : 0);
                result = r2 - r1;
            }
            if (result == 0)
            {
                if (item2.FirstPlayer != null && item1.FirstPlayer != null)
                    result = item2.FirstPlayer.Rating - item1.FirstPlayer.Rating;
            }
            if (result == 0)
            {
                if (item2.SecondPlayer != null && item1.SecondPlayer != null)
                    result = item2.SecondPlayer.Rating - item1.SecondPlayer.Rating;
            }
            if (result == 0)
            {
                result = item2.FirstPlayerId - item1.FirstPlayerId;
            }

            return result;
        }

        private int CompareOrder(Pair item1, Pair item2)
        {
            if (item1 == null && item2 == null)
                return 0;
            else
                if (item2 == null)
                    return 1;
                else
                    if (item1 == null)
                        return -1;

            int result = item1.BoardNumber - item2.BoardNumber;

            if (result == 0)
            {
                result = CompareStrength(item1, item2);
            }

            return result;
        }

        private bool sorting = false;

        public void Sort()
        {
            this.sorting = true;
            this.Sort(CompareStrength);
            this.sorting = false;
            UpdateIndex();
        }

        public void RefreshLang()
        {
            foreach (var item in this)
            {
                item.Update();
            }
        }

    }

    public class Group : ICloneable
    {
        public PlayerList Players;
        public Sequence Sequence;
        public double MMS { get; set; }
        public bool IsLast { get; set; }

        public Group()
        {
            Players = new PlayerList();
            Update();
        }

        public bool Reorder()
        {
            return this.Sequence.Next();
        }

        public void Update()
        {
            this.Sequence = new Sequence(0, this.Players.Count - 1);
        }

        public bool CanMixParts { get; set; }

        public Player getMiddle()
        {
            int i = Players.Count / 2;
            if (i * 2 < Players.Count)
            {
                return Players[i];
            }
            return null;
        }

        private static bool IsMiddle(int idx, int count)
        {
            bool result = false;
            int i = count / 2;
            i *= 2;
            if (i != count && idx == count / 2)
                result = true;

            return result;
        }

        private static int ComparePlayersForPairing(Tournament tournament, int tourId, Player player1, Player player2, PlayerList players, Sequence sequence)
        {
            double result = 0;

            if (sequence != null)
            {
                var sp1Idx = sequence.Figures.FindIndex(
                    delegate(Figure item) { return player1.Order == item.Value; }
                    );
                var sp2Idx = sequence.Figures.FindIndex(
                    delegate(Figure item) { return player2.Order == item.Value; }
                    );

                result = sp1Idx - sp2Idx;

                if (IsMiddle(sp2Idx, sequence.Figures.Count))
                {
                    double soud = -player2.GetCoef(tournament, Entity.SOUD, tourId) + player1.GetCoef(tournament, Entity.SOUD, tourId);
                    if (soud != 0)
                        result = soud;
                }
            }

            if (result == 0)
                result = player2.GetCoef(tournament, Entity.Score, tourId) - player1.GetCoef(tournament, Entity.Score, tourId);

            if (result == 0)
            {
                if (IsMiddle(player2.Order, players.Count))
                {
                    double soud = -player2.GetCoef(tournament, Entity.SOUD, tourId) + player1.GetCoef(tournament, Entity.SOUD, tourId);
                    if (soud != 0)
                        result = soud;
                }
            }

            if (result == 0)
                result = player2.Rating - player1.Rating;

            if (result == 0)
            {
                if (player2.Id < 0 && player1.Id > 0)
                    result = -1;
                else
                    if (player1.Id < 0 && player2.Id > 0)
                        result = 1;
                    else
                        result = player1.Id - player2.Id;
            }
            return Math.Sign(result);
        }

        public void Sort(Tournament tournament, int tourNumber)
        {
            Players.Sort(
                delegate(Player player1, Player player2)
                {
                    return ComparePlayersForPairing(tournament, tourNumber, player1, player2, Players, Sequence);
                });
        }

        public void CopyTo(Group result)
        {
            result.MMS = MMS;
            result.IsLast = IsLast;
            result.CanMixParts = CanMixParts;
            result.Players.AddRange(Players);
            result.Update();
        }

        public object Clone()
        {
            var result = new Group();

            CopyTo(result);

            return result;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append("Group MMS: ").Append(MMS).Append(" Count: ").Append(Players.Count).Append(" Items: ");
            bool first = true;
            foreach (var item in Players)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(", ");
                }
                sb.Append(item.Id);
            }

            return sb.ToString();
        }
    }

    public class Team
    {
        public PlayerList Players;
        public string Name { get; set; }
        public double Score { get; set; }
        public double Points { get; set; }
        public double Sos { get; set; }
        public double Sodos { get; set; }
        public double Sosos { get; set; }
        public int Soud { get; set; }
        public int Sorp { get; set; }
        public double ScoreX { get; set; }
        public bool SharedPlace { get; set; }
        public Team() { Players = new PlayerList(); }
    }

    public enum Entity
    {
        Num,
        Place,
        Name,
        Country,
        City,
        Club,
        Team,
        Coach,
        Grade,
        Rating,
        NewRating,
        Rank,
        Group,
        Tours,
        Criterias,
        Points,
        Score,
        ScoreX,
        ScoreM,
        SOS,
        SODOS,
        SOSOS,
        SOUD,
        SORP,
        PGRC
    }

    public enum Restriction
    {
        AlreadyPlayed,
        AlreadyPairedDown,
        SameCountry,
        SameNationality,
        SameCity,
        SameClub,
        SameTeam,
        SameCoach
    }

    [Serializable]
    public class Club : INotifyPropertyChanged, ICloneable
    {
        [XmlAttribute(AttributeName = "typeversion")]
        public int TypeVersion = 1;

        public string Name { get; set; }

        private string _nameUa;
        [XmlIgnore]
        public string NameUa
        {
            get { return string.IsNullOrEmpty(_nameUa) ? Name : _nameUa; }
            set { _nameUa = value; }
        }

        private string _nameEn;
        [XmlIgnore]
        public string NameEn
        {
            get { return string.IsNullOrEmpty(_nameEn) ? Name : _nameEn; }
            set { _nameEn = value; }
        }

        public string EGDName { get; set; }

        [XmlIgnore]
        public string DisplayName { get { return !string.IsNullOrEmpty(EGDName) ? Name + " (" + EGDName + ")" : string.Empty; } }

        [XmlIgnore]
        public string DisplayNameUa { get { return !string.IsNullOrEmpty(EGDName) ? NameUa + " (" + EGDName + ")" : string.Empty; } }

        [XmlIgnore]
        public string DisplayNameEn { get { return !string.IsNullOrEmpty(EGDName) ? NameEn + " (" + EGDName + ")" : string.Empty; } }

        [XmlIgnore]
        public LangResources Capt
        {
            get { return LangResources.LR; }
        }

        public void CopyTo(Club result)
        {
            result.EGDName = EGDName;
            result.NameUa = NameUa;
            result.NameEn = NameEn;
            result.Name = Name;
        }

        public object Clone()
        {
            var result = new Club();

            CopyTo(result);

            return result;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            Tournament.Changed = true;
            if (null != this.PropertyChanged)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void Update()
        {
            OnPropertyChanged("Name");
            OnPropertyChanged("NameEn");
            OnPropertyChanged("NameUa");
            OnPropertyChanged("EGDName");
        }

    }

    [Serializable]
    public class Countries
    {
        [XmlAttribute(AttributeName = "typeversion")]
        public int TypeVersion = 1;

        [XmlElement(ElementName = "Country")]
        public CountryList Items { get; set; }
    }

    [Serializable]
    public class Country : INotifyPropertyChanged
    {
        [XmlAttribute(AttributeName = "typeversion")]
        public int TypeVersion = 1;

        public string InternetCode { get; set; }
        public string Name { get; set; }

        [XmlElement(ElementName = "Club")]
        public List<Club> Clubs { get; set; }

        [XmlIgnore]
        public string DisplayName { get { return !string.IsNullOrEmpty(Name) ? InternetCode + " (" + Name + ")" : string.Empty; } }

        public Country()
        {
            Clubs = new List<Club>();
            Clubs.Add(new Club());
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            Tournament.Changed = true;
            if (null != this.PropertyChanged)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class CountryList : ObservableList<Country>
    {
        private int CompareCountry(Country item1, Country item2)
        {
            if (item1 == null && item2 == null)
                return 0;
            else
                if (item2 == null)
                    return 1;
                else
                    if (item1 == null)
                        return -1;

            return string.Compare(item1.DisplayName, item2.DisplayName);
        }

        public List<Club> GetClubs(string countryCode)
        {
            if (string.IsNullOrEmpty(countryCode))
                return null;
            List<Club> result = null;
            var country = this.Find(item => item != null && item.InternetCode == countryCode);
            if (country != null)
                result = country.Clubs;

            return result;
        }

        public void Sort()
        {
            this.Sort(CompareCountry);
        }

        public void AddRange(CountryList items)
        {
            if (items != null)
                foreach (var item in items)
                    base.Add(item);
        }
    }

    [Serializable]
    public class Grade : INotifyPropertyChanged
    {
        protected static int GradeCount = 10;

        [XmlAttribute(AttributeName = "typeversion")]
        public int TypeVersion = 1;

        public int Code { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }

        public Grade()
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            Tournament.Changed = true;
            if (null != this.PropertyChanged)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public static string GetShortName(int code, string language = null)
        {
            switch (code)
            {
                case 10: return LangResources.LR.GradeHonoredMaster;
                case 9: return LangResources.LR.GradeInternationalMaster;
                case 8: return LangResources.LR.GradeMaster;
                case 7: return LangResources.LR.GradeCandidate;
                case 6: return LangResources.LR.Grade1;
                case 5: return LangResources.LR.Grade2;
                case 4: return LangResources.LR.Grade3;
                case 3: return LangResources.LR.GradeJnr1;
                case 2: return LangResources.LR.GradeJnr2;
                case 1: return LangResources.LR.GradeJnr3;
            }

            return string.Empty;
        }

        public static int Parse(string shortName)
        {
            var arr = new string[] { "3 юн. сп. р.", "2 юн. сп. р.", "1 юн. сп. р.", "3 сп. р.", "2 сп. р.", "1 сп. р.", "КМСУ", "МСУ", "МСУМК", "ЗМСУ" };
            for (int i = 0; i < GradeCount; i++)
            {
                if (string.Compare(arr[i], shortName, true) == 0)
                {
                    return i + 1;
                }
            }

            return 0;
        }
    }

    public class GradeList : ObservableList<Grade>
    {
        private int CompareGrade(Grade item1, Grade item2)
        {
            if (item1 == null && item2 == null)
                return 0;
            else
                if (item2 == null)
                    return 1;
                else
                    if (item1 == null)
                        return -1;

            return string.Compare(item1.Name, item2.Name);
        }

        public void Sort()
        {
            this.Sort(CompareGrade);
        }

        public void AddRange(GradeList items)
        {
            if (items != null)
                foreach (var item in items)
                    base.Add(item);
        }

        public void FillByDefault()
        {
            Add(new Grade());
            Add(new Grade() { Code = 10, ShortName = LangResources.LR.GradeHonoredMaster, Name = LangResources.LR.GradeHonoredMasterDescr });
            Add(new Grade() { Code = 9, ShortName = LangResources.LR.GradeInternationalMaster, Name = LangResources.LR.GradeInternationalMasterDescr });
            Add(new Grade() { Code = 8, ShortName = LangResources.LR.GradeMaster, Name = LangResources.LR.GradeMasterDescr });
            Add(new Grade() { Code = 7, ShortName = LangResources.LR.GradeCandidate, Name = LangResources.LR.GradeCandidateDescr });
            Add(new Grade() { Code = 6, ShortName = LangResources.LR.Grade1, Name = LangResources.LR.Grade1Descr });
            Add(new Grade() { Code = 5, ShortName = LangResources.LR.Grade2, Name = LangResources.LR.Grade2Descr });
            Add(new Grade() { Code = 4, ShortName = LangResources.LR.Grade3, Name = LangResources.LR.Grade3Descr });
            Add(new Grade() { Code = 3, ShortName = LangResources.LR.GradeJnr1, Name = LangResources.LR.GradeJnr1Descr });
            Add(new Grade() { Code = 2, ShortName = LangResources.LR.GradeJnr2, Name = LangResources.LR.GradeJnr2Descr });
            Add(new Grade() { Code = 1, ShortName = LangResources.LR.GradeJnr3, Name = LangResources.LR.GradeJnr3Descr });
        }
    }

    [Serializable]
    public class Pair : INotifyPropertyChanged, ICloneable
    {
        [XmlAttribute(AttributeName = "typeversion")]
        public int TypeVersion = 1;

        [XmlIgnore]
        public LangResources Capt
        {
            get { return LangResources.LR; }
        }

        [XmlIgnore]
        public int TourId { get; set; }

        [XmlIgnore]
        public int Rating { get; set; }

        [XmlIgnore]
        public double Score { get; set; }

        [XmlIgnore]
        private bool _forcedPairing { get; set; }
        public bool ForcedPairing
        {
            get { return _forcedPairing; }
            set
            {
                _forcedPairing = value;
                OnPropertyChanged("ForcedPairing");
                OnPropertyChanged("CanDelete");
                OnPropertyChanged("CanEditBoard");
            }
        }

        public bool PairingWithBye { get; set; }

        [XmlIgnore]
        public int FirstPlayerId { get; set; }
        public string Black
        {
            get
            {
                return FirstPlayerId == -1 ? SecondPlayerId.ToString() : FirstPlayerId.ToString();
            }
            set
            {
                int i = 0;
                if (int.TryParse(value, out i))
                {
                    FirstPlayerId = i;
                }
                else
                {
                    SecondPlayerId = -1; // Bye
                }
            }
        }
        [XmlIgnore]
        public int SecondPlayerId { get; set; }
        public string White
        {
            get
            {
                return (SecondPlayerId == -1 || FirstPlayerId == -1) ? null : SecondPlayerId.ToString();
            }
            set 
            {
                int i = 0;
                if (int.TryParse(value, out i))
                {
                    SecondPlayerId = i;
                }
                else
                {
                    SecondPlayerId = -1; // Bye
                }
            }
        }

        public int Handicap { get; set; }
        public int AdditionalKomi { get; set; }

        [XmlIgnore]
        public string HandicapText
        {
            get
            {
                bool showAddKomi = true;
                bool showKomi = (AllowJigo || showAddKomi);

                string addKomi = AdditionalKomi != 0 ? AdditionalKomi.ToString() + " " : string.Empty;

                string komiStr = showKomi && ((Handicap != 0) || AdditionalKomi != 0) ? addKomi + (!AllowJigo ? LangResources.LR.HalfKomi : AdditionalKomi != 0 ? LangResources.LR.FullKomi : string.Empty) : string.Empty;

                string handStr = (Handicap < 2 && Handicap > -2) ? string.Empty : Handicap + " " + LangResources.GetStoneStr(Handicap);

                return string.IsNullOrEmpty(komiStr) || string.IsNullOrEmpty(handStr) ?
                    handStr + komiStr : handStr + ", " + komiStr;
            }
        }

        [XmlIgnore]
        public string HandicapInKomiText
        {
            get
            {
                int komi = ((Handicap < 2 && Handicap > -2) ? 0 : Handicap) * 15 + AdditionalKomi;
                bool showAddKomi = komi != 0;
                bool halfKomi = !AllowJigo && (Handicap != 0);
                bool showKomi = (halfKomi || showAddKomi);

                string addKomi = showAddKomi ? komi.ToString() + " " : string.Empty;

                return showKomi ? addKomi + (halfKomi ? LangResources.LR.HalfKomi : LangResources.LR.FullKomi) : string.Empty;
            }
        }

        public bool AllowJigo { get; set; }

        public int UpDownBalance { get; set; }// 0 - no changes, 1 - first player is down, -1 - second player down
        [XmlIgnore]
        public bool UpDownBalanceCompensedFirst { get; set; }
        [XmlIgnore]
        public bool UpDownBalanceCompensedSecond { get; set; }

        [XmlIgnore]
        public int GameResult { get; set; }
        //0 - no result; 1 - first player, 2 - second player, 3 - even game

        public string Result
        {
            get
            {
                switch (GameResult)
                {
                    case 1: return "1-0";
                    case 2: return "0-1";
                    case 3: return "1-1";
                    case 4: return "0-0";
                    case 5: return "½-½";
                }
                return "?-?";
            }
            set
            {
                GameResult = 0;
                switch (value)
                {
                    case "1-0": GameResult = 1; break;
                    case "0-1": GameResult = 2; break;
                    case "1-1": GameResult = 3; break;
                    case "0-0": GameResult = 4; break;
                    case "½-½":
                    case "S-S": GameResult = 5; break;
                }
            }
        }

        public string GetGameResultText(int playerId, bool showColors, bool useHandicap, bool showZeroHandicap, bool useExclamation = true)
        {
            var bldr = new StringBuilder();

            switch (GameResult)
            {
                case 0:
                    bldr.Append("?");
                    break;
                case 1:
                    bldr.Append(FirstPlayerId == playerId ? "+" : "-");
                    break;
                case 2:
                    bldr.Append(FirstPlayerId == playerId ? "-" : "+");
                    break;
                case 3:
                case 5:
                    bldr.Append("+");
                    if (useExclamation)
                        bldr.Append("!");
                    break;
                case 4:
                    bldr.Append("-");
                    if (useExclamation)
                        bldr.Append("!");
                    break;
            }

            if (showColors || useHandicap && (showZeroHandicap || Handicap != 0))
            {
                bldr.Append("/");
                if (showColors || useHandicap && Handicap != 0)
                {
                    bldr.Append((FirstPlayerId == playerId) ? Capt.B : Capt.W);
                }
                if (showZeroHandicap || Handicap != 0)
                    bldr.Append(Handicap);
            }

            return bldr.ToString();
        }

        public string GetGameResultRounRobinText(int playerId, bool useExclamation)
        {
            var bldr = new StringBuilder();

            switch (GameResult)
            {
                case 0:
                    bldr.Append(".");
                    break;
                case 1:
                    bldr.Append(FirstPlayerId == playerId ? "1" : "0");
                    break;
                case 2:
                    bldr.Append(FirstPlayerId == playerId ? "0" : "1");
                    break;
                case 3:
                case 5:
                    bldr.Append("1");
                    if (useExclamation)
                        bldr.Append("!");
                    break;
                case 4:
                    bldr.Append("0");
                    if (useExclamation)
                        bldr.Append("!");
                    break;
            }

            return bldr.ToString();
        }

        public bool ResultByReferee { get; set; }
        public int ResultPoints { get; set; }
        public int BoardNumber { get; set; }

        [XmlIgnore]
        private bool _boardNumberFixed { get; set; }
        public bool BoardNumberFixed
        {
            get { return _boardNumberFixed; }
            set { _boardNumberFixed = value; OnPropertyChanged("BoardNumberFixed"); OnPropertyChanged("CanEditBoard"); }
        }

        [XmlIgnore]
        public string Comment { get; set; }
        [XmlElement(ElementName = "Comment")]
        public XmlNode[] CommentCData
        {
            get
            {
                var dummy = new XmlDocument();
                return new XmlNode[] { dummy.CreateCDataSection(Comment) };
            }
            set
            {
                if (value == null)
                {
                    Comment = null;
                    return;
                }

                if (value.Length != 1)
                {
                    throw new InvalidOperationException(
                        String.Format(
                            "Invalid array length {0}", value.Length));
                }

                var node0 = value[0];
                var cdata = node0 as XmlCDataSection;
                if (cdata == null)
                {
                    throw new InvalidOperationException(
                        String.Format(
                            "Invalid node type {0}", node0.NodeType));
                }

                Comment = cdata.Data;
            }
        }
        [XmlIgnore]
        public bool IsCreated { get; set; }

        [XmlIgnore]
        public bool CanDelete { get { return IsCreated && ForcedPairing; } }
        [XmlIgnore]
        public bool CanEditBoard { get { return ForcedPairing && BoardNumberFixed; } }

        [XmlIgnore]
        public bool IsFirstWon { get { return GameResult == 1 || GameResult == 3; } }
        [XmlIgnore]
        public bool IsSecondWon { get { return GameResult == 2 || GameResult == 3; } }

        public string Link { get; set; }

        public Pair()
        {
            IsCreated = true;
            ResultPoints = 0;
            BoardNumber = 0;
            //BoardNumberFixed = true;

            FirstPlayerId = -1;
            SecondPlayerId= -1;
        }

        //Fields for view. Should be filled by external code.
        [XmlIgnore]
        public Player FirstPlayer { get; set; }
        [XmlIgnore]
        public Player SecondPlayer { get; set; }
        [XmlIgnore]
        public string FirstPlayerName { get; set; }
        [XmlIgnore]
        public string SecondPlayerName { get; set; }

        public int GetUpDownBalance(int playerId)
        {
            if (playerId == SecondPlayerId)
                return -UpDownBalance;
            else
                return UpDownBalance;
        }

        public bool GetUpDownCompensed(int playerId)
        {
            if (playerId == SecondPlayerId)
                return UpDownBalanceCompensedSecond;
            else
                return UpDownBalanceCompensedFirst;
        }

        public void SetUpDownCompensed(int playerId, bool value)
        {
            if (playerId == SecondPlayerId)
                UpDownBalanceCompensedSecond = value;
            else
                UpDownBalanceCompensedFirst = value;
        }

        public void CopyTo(Pair result)
        {
            result.AllowJigo = AllowJigo;
            result.TourId = TourId;
            result.BoardNumber = BoardNumber;
            result.BoardNumberFixed = BoardNumberFixed;
            result.Comment = Comment;
            result.Link = Link;
            result.IsCreated = IsCreated;
            result.UpDownBalance = UpDownBalance;
            result.FirstPlayer = FirstPlayer;
            result.FirstPlayerId = FirstPlayerId;
            result.ForcedPairing = ForcedPairing;
            result.GameResult = GameResult;
            result.Handicap = Handicap;
            result.AdditionalKomi = AdditionalKomi;
            result.PairingWithBye = PairingWithBye;
            result.Rating = Rating;
            result.Score = Score;
            result.Result = Result;
            result.ResultByReferee = ResultByReferee;
            result.ResultPoints = ResultPoints;
            result.SecondPlayer = SecondPlayer;
            result.SecondPlayerId = SecondPlayerId;
        }

        public override int GetHashCode()
        {
            return (TourId << 8 + FirstPlayerId) << 8 + SecondPlayerId;
        }

        public override bool Equals(object obj)
        {
            var pair = obj as Pair;
            if (pair == null)
            {
                return false;
            }

            if (
                pair.AllowJigo != AllowJigo ||
                pair.TourId != TourId ||
                pair.BoardNumber != BoardNumber ||
                pair.BoardNumberFixed != BoardNumberFixed ||
                pair.Comment != Comment ||
                pair.Link != Link ||
                pair.IsCreated != IsCreated ||
                pair.UpDownBalance != UpDownBalance ||
                pair.FirstPlayer != FirstPlayer ||
                pair.FirstPlayerId != FirstPlayerId ||
                pair.ForcedPairing != ForcedPairing ||
                pair.GameResult != GameResult ||
                pair.Handicap != Handicap ||
                pair.AdditionalKomi != AdditionalKomi ||
                pair.PairingWithBye != PairingWithBye ||
                pair.Rating != Rating ||
                pair.Score != Score ||
                pair.Result != Result ||
                pair.ResultByReferee != ResultByReferee ||
                pair.ResultPoints != ResultPoints ||
                pair.SecondPlayer != SecondPlayer ||
                pair.SecondPlayerId != SecondPlayerId
                )
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public void Update()
        {
            OnPropertyChanged("AllowJigo");
            OnPropertyChanged("TourId");
            OnPropertyChanged("BoardNumber");
            OnPropertyChanged("BoardNumberFixed");
            OnPropertyChanged("CanDelete");
            OnPropertyChanged("FirstPlayer");
            OnPropertyChanged("FirstPlayerId");
            OnPropertyChanged("FirstPlayerName");
            OnPropertyChanged("ForcedPairing");
            OnPropertyChanged("GameResult");
            OnPropertyChanged("Handicap");
            OnPropertyChanged("HandicapText");
            OnPropertyChanged("Rating");
            OnPropertyChanged("Result");
            OnPropertyChanged("ResultByReferee");
            OnPropertyChanged("ResultPoints");
            OnPropertyChanged("SecondPlayer");
            OnPropertyChanged("SecondPlayerId");
            OnPropertyChanged("SecondPlayerName");
            OnPropertyChanged("Comment");
        }

        public object Clone()
        {
            var result = new Pair();

            CopyTo(result);

            return result;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            Tournament.Changed = true;
            if (null != this.PropertyChanged)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }

    [Serializable]
    public class Tour
    {
        [XmlAttribute(AttributeName = "typeversion")]
        public int TypeVersion = 1;

        public int RoundNumber { get; set; } //just for XML

        [XmlElement(ElementName = "Pairing")]
        public PairList Pairs { get; set; }

        public Tour()
        {
            Pairs = new PairList();
            Pairs.CollectionChanged += new NotifyCollectionChangedEventHandler(Pairs_CollectionChanged);
        }

        void Pairs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Pairs.FillIDs(false);
        }

        public Tour(int aRoundNumber)
            : this()
        {
            RoundNumber = aRoundNumber;
        }

        public static string ToRoman(int digits)
        {
            int[] a_num = { 1, 4, 5, 9, 10, 40, 50, 90, 100, 400, 500, 900, 1000 };
            string[] r_str = { "I", "IV", "V", "IX", "X", "XL", "L", "XC", "C", "CD", "D", "CM", "M" };
            int idx = a_num.Length - 1;
            string resullt = "";
            while (digits > 0)
            {
                while (digits >= a_num[idx])
                {
                    digits -= a_num[idx];
                    resullt += r_str[idx];
                }
                idx--;
            }
            return resullt;
        }
    }

    public class WallListColumns : List<WallListMemberDescriptior>
    {
        public bool Contains(Entity entity)
        {
            return this.Find(item => item.Id == entity) != null;
        }

        public void Add(Entity entity)
        {
            base.Add(new WallListMemberDescriptior() { Id = entity });
        }
    }

    public class WallListMemberDescriptior
    {
        [XmlAttribute(AttributeName = "typeversion")]
        public int Typeversion = 2;
        [XmlIgnore]
        public Entity Id { get; set; }
        [XmlIgnore]
        public bool Active { get; set; }
        [XmlIgnore]
        public bool Enabled { get; set; }
        public string ShortName
        {
            get
            {
                switch (Id)
                {
                    case Entity.Num: return "Num";
                    case Entity.Place: return "Place";
                    case Entity.Name: return "Name";
                    case Entity.Country: return "Country";
                    case Entity.City: return "City";
                    case Entity.Club: return "Club";
                    case Entity.Coach: return "Coach";
                    case Entity.Grade: return "Grade";
                    case Entity.Team: return "Team";
                    case Entity.Rating: return "Rating";
                    case Entity.NewRating: return "NewRating";
                    case Entity.Rank: return "Rank";
                    case Entity.Group: return "Group";
                    case Entity.Tours: return "Tours";
                    case Entity.Criterias: return "Criterias";
                    case Entity.Score: return "Score";
                    case Entity.ScoreX: return "ScoreX";
                    case Entity.Points: return "Points";
                    case Entity.SODOS: return "SODOS";
                    case Entity.SOS: return "SOS";
                    case Entity.SOSOS: return "SOSOS";
                    case Entity.SORP: return "SORP";
                    case Entity.SOUD: return "SOUD";
                    case Entity.PGRC: return "PGRC";
                }
                return "";
            }
            set
            {
                Id = 0;
                switch (value)
                {
                    case "Num": Id = Entity.Num; break;
                    case "Place": Id = Entity.Place; break;
                    case "Name": Id = Entity.Name; break;
                    case "Country": Id = Entity.Country; break;
                    case "City": Id = Entity.City; break;
                    case "Club": Id = Entity.Club; break;
                    case "Team": Id = Entity.Team; break;
                    case "Coach": Id = Entity.Coach; break;
                    case "Grade": Id = Entity.Grade; break;
                    case "Rating": Id = Entity.Rating; break;
                    case "NewRating": Id = Entity.NewRating; break;
                    case "Rank": Id = Entity.Rank; break;
                    case "Group": Id = Entity.Group; break;
                    case "Tours": Id = Entity.Tours; break;
                    case "Criterias": Id = Entity.Criterias; break;
                    case "Score": Id = Entity.Score; break;
                    case "ScoreX": Id = Entity.ScoreX; break;
                    case "Points": Id = Entity.Points; break;
                    case "SODOS": Id = Entity.SODOS; break;
                    case "SOS": Id = Entity.SOS; break;
                    case "SOSOS": Id = Entity.SOSOS; break;
                    case "SORP": Id = Entity.SORP; break;
                    case "SOUD": Id = Entity.SOUD; break;
                    case "PGRC": Id = Entity.PGRC; break;
                }
            }
        }

        public string DisplayName
        {
            get
            {
                switch (Id)
                {
                    case Entity.Place: return LangResources.LR.Place;
                    case Entity.Name: return LangResources.LR.Name;
                    case Entity.Country: return LangResources.LR.Country;
                    case Entity.City: return LangResources.LR.City;
                    case Entity.Club: return LangResources.LR.Club;
                    case Entity.Team: return LangResources.LR.Team;
                    case Entity.Coach: return LangResources.LR.Coach;
                    case Entity.Grade: return LangResources.LR.Grade;
                    case Entity.Rating: return LangResources.LR.Rating;
                    case Entity.NewRating: return LangResources.LR.NewRating;
                    case Entity.Rank: return LangResources.LR.Rank;
                    case Entity.Group: return LangResources.LR.Group;
                    case Entity.Tours: return LangResources.LR.Tours;
                    case Entity.Criterias: return LangResources.LR.PlacementCriteria;
                    case Entity.Score: return LangResources.LR.Score;
                    case Entity.ScoreX: return LangResources.LR.ScoreX;
                    case Entity.Points: return LangResources.LR.Points;
                    case Entity.SODOS: return LangResources.LR.SODOS;
                    case Entity.SOS: return LangResources.LR.SOS;
                    case Entity.SOSOS: return LangResources.LR.SOSOS;
                    case Entity.SORP: return LangResources.LR.SORP;
                    case Entity.SOUD: return LangResources.LR.SOUD;
                    case Entity.PGRC: return LangResources.LR.PGRC;
                }
                return "";
            }
        }

        public string Name
        {
            get
            {
                switch (Id)
                {
                    case Entity.Place: return LangResources.LR.PlaceDescr;
                    case Entity.Name: return LangResources.LR.NameDescr;
                    case Entity.Country: return LangResources.LR.CountryDescr;
                    case Entity.City: return LangResources.LR.CityDescr;
                    case Entity.Club: return LangResources.LR.ClubDescr;
                    case Entity.Team: return LangResources.LR.TeamDescr;
                    case Entity.Coach: return LangResources.LR.CoachDescr;
                    case Entity.Grade: return LangResources.LR.GradeDescr;
                    case Entity.Rating: return LangResources.LR.RatingDescr;
                    case Entity.NewRating: return LangResources.LR.NewRatingDescr;
                    case Entity.Rank: return LangResources.LR.RankDescr;
                    case Entity.Group: return LangResources.LR.GroupDescr;
                    case Entity.Tours: return LangResources.LR.ToursDescr;
                    case Entity.Criterias: return LangResources.LR.CriteriasDescr;
                    case Entity.Score: return LangResources.LR.ScoreDescr;
                    case Entity.ScoreX: return LangResources.LR.ScoreXDescr;
                    case Entity.Points: return LangResources.LR.PointsDescr;
                    case Entity.SODOS: return LangResources.LR.SodosDescr;
                    case Entity.SOS: return LangResources.LR.SosDescr;
                    case Entity.SOSOS: return LangResources.LR.SososDescr;
                    case Entity.SORP: return LangResources.LR.SorpDescr;
                    case Entity.SOUD: return LangResources.LR.SoudDescr;
                    case Entity.PGRC: return LangResources.LR.PgrcDescr;
                }
                return "";
            }
        }

    }

    public class SortCriterionDescriptior : WallListMemberDescriptior
    {
    }

    public class Wall : ICloneable
    {
        [XmlAttribute(AttributeName = "typeversion")]
        public int TypeVersion = 3;

        public bool ShowCountry { get; set; }
        public bool ShowClub { get; set; }
        public bool ShowClubEgdName { get; set; }
        public bool ShowClubAbbreviateName { get; set; }
        public bool ShowLevels { get; set; }
        public bool ShortNotationForLevel { get; set; }
        public bool ShowBarMembership { get; set; }
        public bool ShowRatings { get; set; }
        public bool MarkPreliminaryRegisteredParticipants { get; set; }
        public bool MarkResultsByReferree { get; set; }
        public bool ShowColors { get; set; }
        public bool ShowHandicap { get; set; }
        public bool ShowWarningMissingPairing { get; set; }
        public int Fontsize { get; set; }
        public int NameColumnWidth { get; set; }
        public int ClubColumnWidth { get; set; }
        public bool ClubColumnAlignmentCenter { get; set; }
        public int WeakSortCriteria { get; set; }
        [XmlElement(ElementName = "SortCriterionDescriptor")]
        public List<SortCriterionDescriptior> SortCriterion { get; set; }
        [XmlElement(ElementName = "WallListMemberDescriptor")]
        public WallListColumns Columns { get; set; }
        public bool HideEmptyColumns { get; set; }

        private void Init()
        {
            ShowClubAbbreviateName = true;
            ShowLevels = true;
            ShowRatings = true;
            MarkPreliminaryRegisteredParticipants = true;
            MarkResultsByReferree = true;
            ShowHandicap = true;
            ShowWarningMissingPairing = true;
            Fontsize = 16;
            NameColumnWidth = 238;
            ClubColumnWidth = 200;
            ClubColumnAlignmentCenter = true;
            SortCriterion = new List<SortCriterionDescriptior>();
            Columns = new WallListColumns();
            HideEmptyColumns = true;
        }

        public Wall()
        {
            Init();
        }

        public void CopyTo(Wall result)
        {
            result = (Wall)this.MemberwiseClone();
            result.Init();
            result.SortCriterion.AddRange(this.SortCriterion);
            result.Columns.AddRange(this.Columns);
        }

        public object Clone()
        {
            var result = new Wall();

            CopyTo(result);

            return result;
        }

    }

    public class TournamentScheme : INotifyPropertyChanged, ICloneable
    {
        [XmlAttribute(AttributeName = "typeversion")]
        public int TypeVersion = 1;

        private int _participantsAmount;
        private int _prizesAmount = 3;
        private int _roundsAmount;

        public int ParticipantsAmount 
        {
            get
            {
                return _participantsAmount;
            }
            set 
            {
                _participantsAmount = value;
                UpdateValues();
                OnPropertyChanged("RoundsBoundsText");
            }
        }

        public int PrizesAmount
        {
            get
            {
                return _prizesAmount;
            }
            set
            {
                _prizesAmount = value;
                UpdateValues();
                OnPropertyChanged("RoundsBoundsText");
            }
        }
        
        public int RoundsAmount
        {
            get
            {
                return _roundsAmount;
            }
            set
            {
                _roundsAmount = value;
                UpdateValues();
            }
        }

        public string RecommendedScheme
        {
            get
            {
                var sb = new StringBuilder();
                switch (PrefferableSystem)
                { 
                    case TournamentKind.McMahon:
                        sb.Append(Capt.McMahon);
                        break;
                    case TournamentKind.Round:
                        sb.Append(Capt.RoundRobin);
                        break;
                    case TournamentKind.Swiss:
                        sb.Append(Capt.Swiss);
                        break;
                    case TournamentKind.Scheveningen:
                        sb.Append(Capt.Scheveningen);
                        break;
                    default:
                        sb.Append(Capt.NoData);
                        break;
                }

                if (TopGroupParticipantsAmount > 0)
                {
                    sb.Append(", ").Append(Capt.TopGroup).Append(" - ").Append(TopGroupParticipantsAmount.ToString()).Append(" ").
                        Append(LangResources.GetPlayersStr(TopGroupParticipantsAmount));
                }
                sb.Append(".");

                return sb.ToString();
            }
        }

        private int GetMinimum()
        {
            int p = ParticipantsAmount;

            // Set even players amount
            if (p % 2 == 1)
            {
                p++;
            }

            int max = p - 1;
            int min = (int)Math.Ceiling((decimal)p / 3);

            if (max < 0) max = 0;
            if (min < 0) min = 0;
            if (min > max) min = max;

            return min;
        }
        
        private int GetMaximum()
        {
            int p = ParticipantsAmount;

            // Set even players amount
            if (p % 2 == 1)
            {
                p++;
            }

            int max = p - 1;

            if (max < 0) max = 0;

            return max;
        }

        private int GetOptimum()
        {
            int p = ParticipantsAmount;
            int z = PrizesAmount - 1;

            // Set even players amount
            if (p % 2 == 1)
            {
                p++;
            }

            // Set well-ordered places more than 1
            if (z <= 1)
            {
                z++;
            }

            int opt = (int)Math.Ceiling(Math.Log(p, 2) + Math.Log(z, 2)) + 1;
            int max = p - 1;

            if (max < 0) max = 0;
            if (opt < 0) opt = 0;
            if (opt > max) opt = max;

            return opt;
        }

        public string RoundsBoundsText
        {
            get
            {
                var sb = new StringBuilder();

                sb.Append(Capt.NumberOfRounds).Append(" (").
                    //Append(Capt.Minimum).Append(" - ").Append(GetMinimum().ToString()).
                    //Append(", ").
                    Append(Capt.Optimum).Append(" - ").Append(GetOptimum().ToString()).
                    Append(", ").
                    Append(Capt.Maximum).Append(" - ").Append(GetMaximum().ToString()).
                    Append(")");

                return sb.ToString();
            }
        }
        
        public TournamentKind PrefferableSystem { get; set; }
        public int TopGroupParticipantsAmount { get; set; }

        object[] scheme = new object[] {
            //players, rounds, system, topGroup
            new object[] { 5, 4, TournamentKind.Round, 0 },
            new object[] { 6, 4, TournamentKind.Swiss, 0 },
            new object[] { 6, 5, TournamentKind.Round, 0 },
            new object[] { 7, 4, TournamentKind.Swiss, 0 },
            new object[] { 7, 5, TournamentKind.Swiss, 0 },
            new object[] { 7, 6, TournamentKind.Round, 0 },
            new object[] { 8, 4, TournamentKind.Swiss, 0 },
            new object[] { 8, 5, TournamentKind.Swiss, 0 },
            new object[] { 8, 6, TournamentKind.Swiss, 0 },
            new object[] { 8, 7, TournamentKind.Round, 0 },
            new object[] { 16, 4, TournamentKind.McMahon, 8 },
            new object[] { 16, 5, TournamentKind.McMahon, 12 },
            new object[] { 16, 6, TournamentKind.Swiss, 0 },
            new object[] { 16, 7, TournamentKind.Swiss, 0 },
            new object[] { 24, 4, TournamentKind.McMahon, 8 },
            new object[] { 24, 5, TournamentKind.McMahon, 12 },
            new object[] { 24, 6, TournamentKind.McMahon, 16 },
            new object[] { 24, 7, TournamentKind.Swiss, 0 },
            new object[] { 32, 4, TournamentKind.McMahon, 8 },
            new object[] { 32, 5, TournamentKind.McMahon, 12 },
            new object[] { 32, 6, TournamentKind.McMahon, 16 },
            new object[] { 32, 7, TournamentKind.Swiss, 0 },
            new object[] { 40, 4, TournamentKind.McMahon, 8 },
            new object[] { 40, 5, TournamentKind.McMahon, 12 },
            new object[] { 40, 6, TournamentKind.McMahon, 16 },
            new object[] { 40, 7, TournamentKind.McMahon, 24},
        };

        private void UpdateValues()
        {
            int p = ParticipantsAmount;
            int r = RoundsAmount;

            // Set even players amount
            if (p % 2 == 1) 
            {
                p++;
            }

            // No chance to pair
            if (p < r + 1)
            {
                PrefferableSystem = TournamentKind.None;
                TopGroupParticipantsAmount = 0;
                OnPropertyChanged("RecommendedScheme");
                return;
            }

            // Only round-Robbin
            if (p == r + 1)
            {
                PrefferableSystem = TournamentKind.Round;
                TopGroupParticipantsAmount = 0;
                OnPropertyChanged("RecommendedScheme");
                return;
            }

            // Swiss
            if (r >= GetOptimum())
            {
                PrefferableSystem = TournamentKind.Swiss;
                TopGroupParticipantsAmount = 0;
                OnPropertyChanged("RecommendedScheme");
                return;
            }

            // McMahon
            PrefferableSystem = TournamentKind.McMahon;
            TopGroupParticipantsAmount = p / 2 > r * 2 ? r * 2 : 0;

            OnPropertyChanged("RecommendedScheme");
            return;
            /*
            if (p > 40)
            {
                if (r < 4)
                {
                    TopGroupParticipantsAmount = 0;
                }
                else
                    if (r == 4)
                    {
                        TopGroupParticipantsAmount = 8;
                    }
                    else
                        if (r == 5)
                        {
                            TopGroupParticipantsAmount = 12;
                        }
                        else
                            if (r == 6)
                            {
                                TopGroupParticipantsAmount = 16;
                            }
                            else
                                TopGroupParticipantsAmount = 24;
            }
            else
            {
                TopGroupParticipantsAmount = 0;
            }
            OnPropertyChanged("RecommendedScheme");
            return;
            */

            //int i = 0;
            //bool found = false;

            //foreach (var item in scheme)
            //{
            //    var arr = item as object[];
            //    bool playerCountOk = false;
            //    if (ParticipantsAmount <= (int)arr[0])
            //    {
            //        playerCountOk = true;
            //        PrefferableSystem = (TournamentKind)arr[2];
            //        TopGroupParticipantsAmount = (int)arr[3];
            //    }

            //    if (playerCountOk && RoundsAmount <= (int)arr[1])
            //    {
            //        PrefferableSystem = (TournamentKind)arr[2];
            //        TopGroupParticipantsAmount = (int)arr[3];
            //        found = true;
            //        break;
            //    }

            //    i++;
            //}

            //if (!found)
            //{
            //    if (ParticipantsAmount < RoundsAmount + 1)
            //    {
            //        PrefferableSystem = TournamentKind.None;
            //        TopGroupParticipantsAmount = 0;
            //    }
            //    else
            //        if (ParticipantsAmount == RoundsAmount + 1)
            //        {
            //            PrefferableSystem = TournamentKind.Round;
            //            TopGroupParticipantsAmount = 0;
            //        }
            //        else
            //            if (ParticipantsAmount < RoundsAmount * 2)
            //            {
            //                PrefferableSystem = TournamentKind.Swiss;
            //                TopGroupParticipantsAmount = 0;
            //            }
            //            else
            //            {
            //                PrefferableSystem = TournamentKind.McMahon;
            //                if (ParticipantsAmount > 40)
            //                {
            //                    if (RoundsAmount < 4)
            //                    {
            //                        TopGroupParticipantsAmount = 0;
            //                    }
            //                    else
            //                    if (RoundsAmount == 4)
            //                    {
            //                        TopGroupParticipantsAmount = 8;
            //                    }
            //                    else
            //                        if (RoundsAmount == 5)
            //                        {
            //                            TopGroupParticipantsAmount = 12;
            //                        }
            //                        else
            //                            if (RoundsAmount == 6)
            //                            {
            //                                TopGroupParticipantsAmount = 16;
            //                            }
            //                            else
            //                                TopGroupParticipantsAmount = 24;
            //                }
            //                else
            //                {
            //                    TopGroupParticipantsAmount = 0;
            //                }
            //            }
            //}

            //OnPropertyChanged("RecommendedScheme");
        }

        private void Init()
        {
        }

        public TournamentScheme()
        {
            Init();
        }

        public void CopyTo(TournamentScheme result)
        {
            result = (TournamentScheme)this.MemberwiseClone();
            result.Init();
        }

        public object Clone()
        {
            var result = new TournamentScheme();

            CopyTo(result);

            return result;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            Tournament.Changed = true;
            if (null != this.PropertyChanged)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        [XmlIgnore]
        public LangResources Capt
        {
            get { return LangResources.LR; }
        }
    }

    [Serializable]
    public class Tournament : INotifyPropertyChanged, ICloneable
    {
        //Flag for exit dialog
        public static bool Changed = false;

        //Flag for exit dialog
        [XmlIgnore]
        public int NonDatabasePlayersCount = 0;

        [XmlAttribute(AttributeName = "typeversion")]
        public int TypeVersion = 6;

        [XmlIgnore]
        public string FileName { get; set; }

        [XmlIgnore]
        public string Name { get; set; }
        [XmlElement(ElementName = "Name")]
        public XmlNode[] NameCData
        {
            get
            {
                var dummy = new XmlDocument();
                return new XmlNode[] { dummy.CreateCDataSection(Name) };
            }
            set
            {
                if (value == null)
                {
                    Name = null;
                    return;
                }

                if (value.Length != 1)
                {
                    throw new InvalidOperationException(
                        String.Format(
                            "Invalid array length {0}", value.Length));
                }

                var node0 = value[0];
                var cdata = node0 as XmlCDataSection;
                if (cdata == null)
                {
                    throw new InvalidOperationException(
                        String.Format(
                            "Invalid node type {0}", node0.NodeType));
                }

                Name = cdata.Data;
            }
        }

        [XmlIgnore]
        public string Description { get; set; }
        [XmlElement(ElementName = "Description")]
        public XmlNode[] DescriptionCData
        {
            get
            {
                var dummy = new XmlDocument();
                return new XmlNode[] { dummy.CreateCDataSection(Description) };
            }
            set
            {
                if (value == null)
                {
                    Description = null;
                    return;
                }

                if (value.Length != 1)
                {
                    throw new InvalidOperationException(
                        String.Format(
                            "Invalid array length {0}", value.Length));
                }

                var node0 = value[0];
                var cdata = node0 as XmlCDataSection;
                if (cdata == null)
                {
                    throw new InvalidOperationException(
                        String.Format(
                            "Invalid node type {0}", node0.NodeType));
                }

                Description = cdata.Data;
            }
        }

        [XmlIgnore]
        public bool IsCreated { get; set; }

        public int NumberOfRounds
        {
            get
            {
                return Tours.Count;
            }
            set
            {
                if (IsCreated)
                {
                    for (int i = Tours.Count; i <= value; i++)
                        Tours.Add(new Tour());
                    for (int i = Tours.Count; i > value; i--)
                        Tours.RemoveAt(i - 1);

                }
                UpdateTourIDs();
            }
        }

        private int _numberOfPlayers = -1;

        private int GetRoundsAmountByPlayers(int playersAmount)
        {
            return playersAmount + playersAmount % 2 - 1;
        }

        private int GetPlayersAmountByRounds(int roundsAmount)
        {
            var result = roundsAmount + 1;
            return roundsAmount + roundsAmount % 2;
        }

        [XmlIgnore]
        public int NumberOfPlayers
        {
            get
            {
                if (GetRoundsAmountByPlayers(_numberOfPlayers) != NumberOfRounds)
                {
                    _numberOfPlayers = GetPlayersAmountByRounds(NumberOfRounds);
                }
                return _numberOfPlayers;
            }
            set
            {
                _numberOfPlayers = value;
                NumberOfRounds = GetRoundsAmountByPlayers(_numberOfPlayers);
            }
        }

        [XmlIgnore]
        public int NumberOfTeamPlayers
        {
            get
            {
                return NumberOfRounds;
            }
            set
            {
                NumberOfRounds = value;
            }
        }

        [XmlIgnore]
        public string BoardSizeText
        {
            get
            {
                return Boardsize == 0 ? "NxN" : Boardsize.ToString() + "x" + Boardsize.ToString();
            }
            set
            {
                Boardsize = 0;
                int p = value.IndexOf("x");
                if (p > 0)
                {
                    string s = value.Substring(0, p);
                    int i;
                    if (int.TryParse(s, out i))
                        Boardsize = i;
                }
            }
        }

        public void UpdateTourIDs()
        {
            if (CurrentRoundNumber < 1)
                CurrentRoundNumber = 1;

            if (CurrentRoundNumber > Tours.Count)
                CurrentRoundNumber = Tours.Count;
            /*
            TakeCurrentRoundInAccount =
                CurrentRoundNumber == Tours.Count;
            */
            _tourIDs.Clear();
            for (int i = 0; i < Tours.Count; i++)
            {
                Tours[i].RoundNumber = i + 1;
                _tourIDs.Add(Tour.ToRoman(i + 1));
            }
        }

        public void RemoveBrokenPairs()
        {
            foreach (var tour in Tours)
            {
                for (int i = tour.Pairs.Count - 1; i >= 0; i--)
                {
                    var pair = tour.Pairs[i];
                    if (pair.FirstPlayerId > 0 && this.Players.GetById(pair.FirstPlayerId) == null ||
                        pair.SecondPlayerId > 0 && this.Players.GetById(pair.SecondPlayerId) == null)
                    {
                        tour.Pairs.RemoveAt(i);
                    }
                }
            }
        }

        [XmlIgnore]
        private TournamentKind _kind { get; set; }

        [XmlIgnore]
        public TournamentKind Kind
        {
            get { return _kind; }
            set
            {
                _kind = value;
                OnPropertyChanged("TournamentSystemScheveningen");
                OnPropertyChanged("TournamentSystemSwiss");
                OnPropertyChanged("TournamentSystemRound");
                OnPropertyChanged("TournamentSystemMcMahon");
                OnPropertyChanged("ManualRoundAmountAllowed");
                OnPropertyChanged("ManualPlayersAmountAllowed");
                OnPropertyChanged("ManualTeamPlayersAmountAllowed");
            }
        }

        [XmlElement(ElementName = "Kind")]
        public int TournamentKindInt
        {
            get
            {
                switch (Kind)
                {
                    case TournamentKind.Round: return 1;
                    case TournamentKind.Swiss: return 2;
                    case TournamentKind.Scheveningen: return 3;
                    default: return 0;
                }
            }
            set
            {
                switch (value)
                {
                    case 1: Kind = TournamentKind.Round; break;
                    case 2: Kind = TournamentKind.Swiss; break;
                    case 3: Kind = TournamentKind.Scheveningen; break;
                    default: Kind = TournamentKind.McMahon; break;
                }
            }
        }

        [XmlIgnore]
        public bool TournamentSystemSwiss
        {
            get { return Kind == TournamentKind.Swiss; }
            set { if (value) { Kind = TournamentKind.Swiss; UseMacMahonStartScores = false; } }
        }

        [XmlIgnore]
        public bool TournamentSystemScheveningen
        {
            get { return Kind == TournamentKind.Scheveningen; }
            set { if (value) { Kind = TournamentKind.Scheveningen; UseMacMahonStartScores = false; } }
        }

        [XmlIgnore]
        public bool TournamentSystemMcMahon
        {
            get { return Kind == TournamentKind.McMahon; }
            set { if (value) { Kind = TournamentKind.McMahon; UseMacMahonStartScores = true; } }
        }

        [XmlIgnore]
        public bool TournamentSystemRound
        {
            get { return Kind == TournamentKind.Round; }
            set { if (value) { Kind = TournamentKind.Round; UseMacMahonStartScores = false; } }
        }

        [XmlIgnore]
        public bool ManualRoundAmountAllowed
        {
            get { return !ManualPlayersAmountAllowed && !ManualTeamPlayersAmountAllowed; }
        }

        [XmlIgnore]
        public bool ManualPlayersAmountAllowed
        {
            get { return (TournamentSystemRound); }
        }

        [XmlIgnore]
        public bool ManualTeamPlayersAmountAllowed
        {
            get { return (TournamentSystemScheveningen); }
        }

        public int FirstMoveColor { get; set; } //0 = black, 1 = white, 2 = random

        [XmlIgnore]
        public bool FirstMoveBlack
        {
            get { return FirstMoveColor == 0; }
            set { if (value) { FirstMoveColor = 0; } }
        }

        [XmlIgnore]
        public bool FirstMoveWhite
        {
            get { return FirstMoveColor == 1; }
            set { if (value) { FirstMoveColor = 1; } }
        }

        [XmlIgnore]
        public bool FirstMoveRandom
        {
            get { return FirstMoveColor == 2; }
            set { if (value) { FirstMoveColor = 2; } }
        }

        public int CurrentRoundNumber { get; set; }

        public bool TakeCurrentRoundInAccount { get; set; }
        public bool DefaultAsianName { get; set; }
        public bool RatingAllowed { get; set; }
        public int RatingLowestOneDanRating { get; set; }
        public bool RatingDeterminesRank { get; set; }
        public bool RankDeterminesRating { get; set; }
        public bool RatingDeterminesStartScore { get; set; }

        [XmlIgnore]
        private bool _lowerMacMahonBar { get; set; }
        [XmlIgnore]
        private bool _upperMacMahonBar { get; set; }
        [XmlIgnore]
        private bool _upperMacMahonBarByAmount { get; set; }
        [XmlIgnore]
        private bool _useMacMahonStartScores { get; set; }

        public bool LowerMacMahonBar
        {
            get { return _lowerMacMahonBar; }
            set
            {
                if (value) UseMacMahonStartScores = true;
                UseMacMahonStartScoresManually = false;
                _lowerMacMahonBar = value;
                OnPropertyChanged("LowerMacMahonBar");
                OnPropertyChanged("UseMacMahonStartScores");
                OnPropertyChanged("UseMacMahonStartScoresAutoCalc");
            }
        }

        [XmlIgnore]
        private string _lowerMacMahonBarLevel;
        public string LowerMacMahonBarLevel { get { return !string.IsNullOrEmpty(_lowerMacMahonBarLevel) ? _lowerMacMahonBarLevel : "30k"; } set { _lowerMacMahonBarLevel = value; } }

        private int _upperMacMahonBarAmount;
        public int UpperMacMahonBarAmount 
        {
            get { return _upperMacMahonBarAmount; }
            set
            {
                _upperMacMahonBarAmount = value;
                OnPropertyChanged("UpperMacMahonBarAmount");
            }
        }

        public bool UpperMacMahonBarByAmount
        {
            get { return _upperMacMahonBarByAmount; }
            set
            {
                _upperMacMahonBarByAmount = value;
                OnPropertyChanged("UpperMacMahonBarByAmount");
                OnPropertyChanged("UpperMacMahonBarByLevel");
            }
        }

        [XmlIgnore]
        public bool UpperMacMahonBarByLevel
        {
            get { return !_upperMacMahonBarByAmount; }
            set
            {
                _upperMacMahonBarByAmount = !value;
                OnPropertyChanged("UpperMacMahonBarByLevel");
                OnPropertyChanged("UpperMacMahonBarByAmount");
            }
        }

        [XmlIgnore]
        public string LowerMacMahonBarLevelExt
        {
            get
            {
                int r = PlayerInfo.GetRatingByRank(LowerMacMahonBarLevel);
                return PlayerInfo.GetRankFromRating(r, true);
            }
            set
            {
                int r = PlayerInfo.GetRatingByRank(value);
                LowerMacMahonBarLevel = PlayerInfo.GetRankFromRating(r);
                OnPropertyChanged("LowerMacMahonBarLevelExt");
            }
        }

        [XmlIgnore]
        public string UpperMacMahonBarLevelExt
        {
            get
            {
                int r = PlayerInfo.GetRatingByRank(UpperMacMahonBarLevel);
                return PlayerInfo.GetRankFromRating(r, true);
            }
            set
            {
                int r = PlayerInfo.GetRatingByRank(value);
                UpperMacMahonBarLevel = PlayerInfo.GetRankFromRating(r);
                OnPropertyChanged("UpperMacMahonBarLevelExt");
            }
        }

        public bool UpperMacMahonBar
        {
            get { return _upperMacMahonBar; }
            set
            {
                if (value) UseMacMahonStartScores = true;
                UseMacMahonStartScoresManually = false;
                _upperMacMahonBar = value;
                OnPropertyChanged("UpperMacMahonBar");
                OnPropertyChanged("UseMacMahonStartScores");
                OnPropertyChanged("UseMacMahonStartScoresAutoCalc");
            }
        }

        [XmlIgnore]
        private string _upperMacMahonBarLevel { get; set; }
        public string UpperMacMahonBarLevel { get { return !string.IsNullOrEmpty(_upperMacMahonBarLevel) ? _upperMacMahonBarLevel : "8d"; } set { _upperMacMahonBarLevel = value; } }

        public int UpperMacMahonBarRating { get; set; }
        public bool HalfScoreGroupsRoundDown { get; set; }
        public bool HalfScoreGroupsRoundDownNotJigo { get; set; }
        public bool PreliminaryDefaultRegistration { get; set; }

        public bool UseMacMahonStartScores
        {
            get { return _useMacMahonStartScores; }
            set
            {
                if (_useMacMahonStartScores != value)
                {
                    _useMacMahonStartScores = value;
                    OnPropertyChanged("UseMacMahonStartScores");

                    if (Kind == TournamentKind.Swiss || Kind == TournamentKind.McMahon)
                    {
                        if (value)
                            Kind = TournamentKind.McMahon;
                        else
                            if (Kind == TournamentKind.McMahon)
                                Kind = TournamentKind.Swiss;
                    }
                }
            }
        }
        public bool UseMacMahonStartScoresManually { get; set; }

        public bool UseMacMahonStartScoresWithoutGapsInSequence { get; set; }
        public bool UseMacMahonSuperBar { get; set; }

        public bool OnlineEgdSupport { get; set; }
        public bool OnlineEgdSupportByPin { get; set; }

        [XmlIgnore]
        private bool _handicapUsed { get; set; }
        [XmlIgnore]
        private bool _handicapBelow { get; set; }
        [XmlIgnore]
        private bool _handicapByPoints { get; set; }
        [XmlIgnore]
        private bool _handicapByLevel { get; set; }
        [XmlIgnore]
        private bool _handicapByRating { get; set; }
        [XmlIgnore]
        private bool _handicapAdjustment { get; set; }
        [XmlIgnore]
        private bool _handicapLimit { get; set; }

        [XmlIgnore]
        private bool _calculationScoreAddHalf { get; set; }
        [XmlIgnore]
        private bool _calculationScoreAddAlternate { get; set; }
        [XmlIgnore]
        private bool _calculationScoreAddNothing { get; set; }
        [XmlIgnore]
        private bool _calculationScoreZeroToSelfCoefs { get; set; }
        [XmlIgnore]
        private bool _calculationScoreMaxToCompetitorCoefs { get; set; }
        [XmlIgnore]
        private bool _customizeCalculation { get; set; }
        [XmlIgnore]
        private bool _customizeForTopGroupOnly { get; set; }

        public bool HandicapUsed { get { return _handicapUsed; } set { _handicapUsed = value; OnPropertyChanged("HandicapUsed"); } }
        public bool HandicapBelow { get { return _handicapBelow; } set { _handicapBelow = value; OnPropertyChanged("HandicapBelow"); } }

        public bool CustomizeCalculation { get { return _customizeCalculation; } set { _customizeCalculation = value; OnPropertyChanged("CustomizeCalculation"); } }
        public bool CalculationScoreAddHalf { get { return _calculationScoreAddHalf || !_calculationScoreAddAlternate && !_calculationScoreAddNothing; } set { _calculationScoreAddHalf = value; OnPropertyChanged("CalculationScoreAddHalf"); } }
        public bool CalculationScoreAddAlternate { get { return _calculationScoreAddAlternate; } set { _calculationScoreAddAlternate = value; OnPropertyChanged("CalculationScoreAddAlternate"); } }
        public bool CalculationScoreAddNothing { get { return _calculationScoreAddNothing; } set { _calculationScoreAddNothing = value; OnPropertyChanged("CalculationScoreAddNothing"); } }
        public bool CalculationScoreZeroToOwnCoefs { get { return _calculationScoreZeroToSelfCoefs; } set { _calculationScoreZeroToSelfCoefs = value; OnPropertyChanged("CalculationScoreZeroToSelfCoefs"); } }
        public bool CalculationScoreMaxToCompetitorCoefs { get { return _calculationScoreMaxToCompetitorCoefs; } set { _calculationScoreMaxToCompetitorCoefs = value; OnPropertyChanged("CalculationScoreMaxToCompetitorCoefs"); } }
        public bool CustomizeForTopGroupOnly { get { return _customizeForTopGroupOnly; } set { _customizeForTopGroupOnly = value; OnPropertyChanged("CustomizeForTopGroupOnly"); } }

        public bool ByeShouldResultInZeroSOSetc { get; set; }

        [XmlIgnore]
        private string _handicapBelowLevel { get; set; }
        public string HandicapBelowLevel { get { return !string.IsNullOrEmpty(_handicapBelowLevel) ? _handicapBelowLevel : "1d"; } set { _handicapBelowLevel = value; } }

        private void ClearHandicapBy()
        {
            _handicapByLevel = false; _handicapByRating = false; _handicapByPoints = false;
        }

        private void UpdateHandicapBy()
        {
            OnPropertyChanged("HandicapByLevel"); OnPropertyChanged("HandicapByPoints"); OnPropertyChanged("HandicapByRating");
        }

        public bool HandicapByLevel { get { return _handicapByPoints == false && _handicapByRating == false ? true : _handicapByLevel; } set { if (value) ClearHandicapBy(); _handicapByLevel = value; UpdateHandicapBy(); } }
        public bool HandicapByPoints { get { return _handicapByPoints; } set { if (value) ClearHandicapBy(); _handicapByPoints = value; UpdateHandicapBy(); } }
        public bool HandicapByRating { get { return _handicapByRating; } set { if (value) ClearHandicapBy(); _handicapByRating = value; UpdateHandicapBy(); } }

        public bool HandicapAdjustment { get { return _handicapAdjustment; } set { _handicapAdjustment = value; OnPropertyChanged("HandicapAdjustment"); } }
        public int HandicapAdjustmentValue { get; set; }
        public bool HandicapLimit { get { return _handicapLimit; } set { _handicapLimit = value; OnPropertyChanged("HandicapLimit"); } }
        public int HandicapLimitValue { get; set; }
        public bool HandicapIncludeInTieBreakers { get; set; }
        public bool HandicapAdditionalKomi { get; set; }
        public bool HandicapDisplayInKomi { get; set; }

        public bool AllowJigo { get; set; }
        public int Boardsize { get; set; }

        public bool PairingsMarkMissingResults { get; set; }
        public bool PairingsMarkWinner { get; set; }
        public bool PairingsShowLevels { get; set; }
        public bool PairingsShortNotationForLevel { get; set; }
        public bool PairingsShowScores { get; set; }
        public bool PairingsShowHandicaps { get; set; }
        public int PairingsFontsize { get; set; }
        public int PairingsBlackColumnWidth { get; set; }
        public int PairingsWhiteColumnWidth { get; set; }

        [XmlIgnore]
        public string ExportColumnDelimiter { get; set; }
        [XmlElement(ElementName = "ExportColumnDelimiter")]
        public XmlNode[] ExportColumnDelimiterCData
        {
            get
            {
                var dummy = new XmlDocument();
                return new XmlNode[] { dummy.CreateCDataSection(ExportColumnDelimiter) };
            }
            set
            {
                if (value == null)
                {
                    ExportColumnDelimiter = null;
                    return;
                }

                if (value.Length != 1)
                {
                    throw new InvalidOperationException(
                        String.Format(
                            "Invalid array length {0}", value.Length));
                }

                var node0 = value[0];
                var cdata = node0 as XmlCDataSection;
                if (cdata == null)
                {
                    throw new InvalidOperationException(
                        String.Format(
                            "Invalid node type {0}", node0.NodeType));
                }

                ExportColumnDelimiter = cdata.Data;
            }
        }

        public bool ExportWalllistShowTournamentTitle { get; set; }
        public string ExportEncoding { get; set; }
        public string VersionCreated { get; set; }
        public string VersionSaved { get; set; }

        public bool MakePairingTopGroup { get; set; }
        public bool MakePairingTopGroupEverywhere { get; set; }
        public bool MakePairingTopGroupStrictlyByTopBar { get; set; }
        public bool MakePairingTopGroupByNumberOfPlayersAuto { get; set; }
        public int MakePairingTopGroupByNumberOfPlayers { get; set; }
        public bool MakePairingTopGroupSeeding { get; set; }
        [XmlElement]
        public SortCriterionDescriptior MakePairingSeedingPlacementCriterion { get; set; }
        public bool MakePairingTopGroupSeedingByRating { get; set; }
        public int MakePairingTopGroupSeedingByRatingRound { get; set; }
        public bool MakePairingOutsideTopGroupSameCountry { get; set; }
        public bool MakePairingOutsideTopGroupSameClub { get; set; }
        public bool MakePairingOutsideTopGroupSameNationality { get; set; }
        public bool MakePairingOutsideTopGroupStrengthDifference { get; set; }
        public int MakePairingOutsideTopGroupStrengthDifferenceValue { get; set; }
        public bool MakePairingOutsideTopGroupColorBalance { get; set; }
        public bool MakePairingOutsideTopGroupWeakOddMan { get; set; }
        public bool MakePairingOutsideTopGroupSeeding { get; set; }

        public bool MakePairingOutsideTopGroupSameTeam { get; set; }
        public bool MakePairingOutsideTopGroupSameCoach { get; set; }

        public string PrintWalllistFont { get; set; }
        public int PrintWalllistFontsize { get; set; }
        public int PrintWalllistIndentationTop { get; set; }
        public int PrintWalllistIndentationLeft { get; set; }
        public int PrintWalllistColumnDistance { get; set; }
        public bool PrintWalllistAbbreviateName { get; set; }
        public int PrintWalllistAbbreviateNameLength { get; set; }
        public string PrintPairingsFont { get; set; }
        public int PrintPairingsFontsize { get; set; }
        public int PrintPairingsIndentationTop { get; set; }
        public int PrintPairingsIndentationLeft { get; set; }
        public int PrintPairingsColumnDistance { get; set; }
        public bool PrintPairingsAbbreviateName { get; set; }
        public int PrintPairingsAbbreviateNameLength { get; set; }


        [XmlIgnore]
        private static bool HideTransliteration { get; set; }
        [XmlIgnore]
        public static bool UseTransliteration { get { return !HideTransliteration; } set { HideTransliteration = !value; } }
        [XmlIgnore]
        public static RatingSystem RSystem { get; set; }

        public Wall Walllist { get; set; }

        [XmlElement(ElementName = "Country")]
        public CountryList Countries { get; set; }
        [XmlElement(ElementName = "IndividualParticipant")]
        public PlayerList Players { get; set; }
        [XmlElement(ElementName = "TournamentRound")]
        public List<Tour> Tours { get; set; }

        private void Init()
        {
            Walllist = new Wall();
            Players = new PlayerList();
            Tours = new List<Tour>();

            _tourIDs = new ObservableCollection<string>();
            ResultKinds = new ObservableCollection<string>();
            ResultKinds.Add("?-?");
            ResultKinds.Add("1-0");
            ResultKinds.Add("0-1");
            ResultKinds.Add("1-1");
            ResultKinds.Add("0-0");
            ResultKinds.Add("½-½");

            BoardSizes = new ObservableCollection<string>();
            BoardSizes.Add("19x19");
            BoardSizes.Add("13x13");
            BoardSizes.Add("9x9");
            BoardSizes.Add("NxN");
        }

        public Tournament()
        {
            Init();
        }

        [XmlIgnore]
        private ObservableCollection<string> _tourIDs { get; set; }

        [XmlIgnore]
        public ObservableCollection<string> TourIDs
        {
            get
            {
                return _tourIDs;
            }
        }

        [XmlIgnore]
        public ObservableCollection<string> BoardSizes { get; set; }

        [XmlIgnore]
        public List<Entity> EntitiesAll = new List<Entity>()
        {
            Entity.Place
            ,Entity.Name
            ,Entity.Country
            ,Entity.City
            ,Entity.Club
            ,Entity.Team
            ,Entity.Coach
            ,Entity.Rank
            ,Entity.Rating
            ,Entity.Grade
            ,Entity.Group
            ,Entity.Tours
            ,Entity.Points
            ,Entity.Score
            ,Entity.SOS
            ,Entity.SODOS
            ,Entity.SOSOS
            ,Entity.SOUD
            ,Entity.NewRating
        };

        [XmlIgnore]
        public List<Entity> EntitiesCriteria = new List<Entity>()
        {
             Entity.Points
            ,Entity.Score
            ,Entity.ScoreX
            ,Entity.SOS
            ,Entity.SODOS
            ,Entity.SOSOS
            ,Entity.SORP
            ,Entity.SOUD
            ,Entity.PGRC
        };

        [XmlIgnore]
        public List<Entity> EntitiesStd = new List<Entity>()
        {
            Entity.Place
            ,Entity.Name
            ,Entity.Country
            ,Entity.City
            ,Entity.Club
            ,Entity.Team
            ,Entity.Coach
            ,Entity.Grade
            ,Entity.Rank
            ,Entity.Rating
            ,Entity.Group
            ,Entity.Tours
            ,Entity.Criterias
            ,Entity.NewRating

        };

        [XmlIgnore]
        public List<Entity> EntitiesMin = new List<Entity>()
        {
            Entity.Name
        };

        [XmlIgnore]
        public List<Entity> EntitiesHidden = new List<Entity>()
        {
            Entity.City
        };

        [XmlIgnore]
        public List<Entity> EntitiesOutOfWallist = new List<Entity>()
        {
            Entity.Num,
            Entity.ScoreM,
        };

        [XmlIgnore]
        public List<Restriction> ImpossiblePairs = new List<Restriction>()
        {
            Restriction.AlreadyPlayed,
            Restriction.SameTeam
        };

        [XmlIgnore]
        public ObservableCollection<string> ResultKinds { get; set; }

        public void CopyTo(Tournament result)
        {
            result = (Tournament)this.MemberwiseClone();
            result.Init();
            result.Walllist = (Wall)this.Walllist.Clone();
            result.Tours.AddRange(Tours);
            result.Players.AddRange(Players);
        }

        public object Clone()
        {
            var result = new Tournament();
            CopyTo(result);

            return result;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            Tournament.Changed = true;
            if (null != this.PropertyChanged)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        [XmlIgnore]
        public LangResources Capt
        {
            get { return LangResources.LR; }
        }
    }

}
