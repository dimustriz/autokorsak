// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Translator.cs" company="">
//   
// </copyright>
// <summary>
//   The translator.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Tourtoss.BE
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.Linq;
    using System.IO;
    using System.Threading;
    using System.Xml;
    using System.ComponentModel;

    using System.Xml.Serialization;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Text;

    /*
	 * Translator using Qt compatible language files.
	 */

    /// <summary>
    /// The translator.
    /// </summary>
    public class Translator
    {
        #region Constants and Fields

        /// <summary>
        /// The s_lang.
        /// </summary>
        private static string s_lang = "en";

        /// <summary>
        /// The s_region.
        /// </summary>
        private static string s_region = "US";

        /// <summary>
        /// The translator.
        /// </summary>
        private static Hashtable translator;

        #endregion

        /*
		 * Instantiate the translator. The language will be retrieved from the
		 * environment variable "LANG". If it is not set, the systems lanuage
		 * setting will be used. In case no matching language file can be found
		 * the user interface falls back to the encoded english texts.
		 * Use this class from the GUI thread only - it is not threadsafe.
		 * 
		 * Application programmers must include in their GUI classes a method 
		 * with the signature
		 * - string tr(string text) {Translator.Translate("context", text);}
		 * where "context" is replaced by the classes context.
		 * 
		 * @param filename Path and first part of the filename. The filename will
		 * be completed appending the two letter language code and the extension ".ts".
		 */
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Translator"/> class.
        /// </summary>
        /// <param name="filename">
        /// The filename.
        /// </param>
        public Translator(string filename)
        {
            this.Init(filename, Environment.GetEnvironmentVariable("LANG"));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Translator"/> class.
        /// </summary>
        /// <param name="filename">
        /// The filename.
        /// </param>
        /// <param name="_lang">
        /// The _lang.
        /// </param>
        public Translator(string filename, string _lang)
        {
            this.Init(filename, _lang);
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets Language.
        /// </summary>
        public static string Language
        {
            get
            {
                return s_lang;
            }
        }

        #endregion

        /*
		 * Returns the ranslated text using the given context. In case no
		 * translation is found the input text is returned.
		 */
        #region Public Methods and Operators

        /// <summary>
        /// The translate.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <param name="text">
        /// The text.
        /// </param>
        /// <returns>
        /// The translate.
        /// </returns>
        public static string Translate(string context, string text)
        {
            if (translator == null || string.IsNullOrEmpty(text))
                return text;

            var ht = (Hashtable)translator[context];
            if (ht == null)
                return text;

            var rtext = (string)ht[text];
            if (rtext == null)
                return text;

            if (rtext.Length == 0)
                return text;

            return rtext;
        }

        /// <summary>
        /// The set file.
        /// </summary>
        /// <param name="file">
        /// The file.
        /// </param>
        public void SetFile(string file)
        {
            if (translator != null)
            {
                translator.Clear();
                translator = null;
            }

            translator = new Hashtable();

            try
            {
                Init(file);
            }
            catch (Exception)
            {
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// The assign file.
        /// </summary>
        /// <param name="filename">
        /// The filename.
        /// </param>
        /// <param name="_lang">
        /// The _lang.
        /// </param>
        /// <returns>
        /// The assign file.
        /// </returns>
        private static string AssignFile(string filename, string _lang)
        {
            string _fileName = filename + _lang + ".tsl";
            if (!File.Exists(_fileName))
            {

                Assembly assembly;
                assembly = Assembly.GetExecutingAssembly();

                string resName = "Tourtoss.BE." + Path.GetFileName(_fileName);
                var info = assembly.GetManifestResourceInfo(resName);
                if (info != null)
                    return resName;

                _fileName = filename + "en.tsl";
                if (!File.Exists(_fileName))
                {
                    return string.Empty;
                }
            }

            return _fileName;
        }

        /// <summary>
        /// The init.
        /// </summary>
        /// <param name="filename">
        /// The filename.
        /// </param>
        private static void Init(string filename)
        {
            var xd = new XmlDocument();

            if (filename.StartsWith("Tourtoss.BE."))
            {
                Assembly assembly;
                assembly = Assembly.GetExecutingAssembly();

                Stream stream = null;
                try
                {
                    stream = assembly.GetManifestResourceStream(filename);

                    var reader = new XmlTextReader(stream);
                    xd.Load(reader);
                    reader.Close();
                }
                catch (Exception)
                {
                }
                finally
                {
                    if (null != stream)
                        stream.Close();
                }
            }
            else
            {
                var reader = new XmlTextReader(filename);
                xd.Load(reader);
                reader.Close();
            }

            string contextName = string.Empty;

            foreach (XmlNode node in xd.DocumentElement.ChildNodes)
            {
                var group = new Hashtable();
                foreach (XmlNode n in node.ChildNodes)
                {
                    try
                    {
                        if (n.Name == "name")
                        {
                            contextName = n.InnerText;
                            if (translator.Contains(n.InnerText))
                            {
                                group = translator[n.InnerText] as Hashtable;
                            }
                            else
                            {
                                translator.Add(n.InnerText, group);
                            }
                        }
                        else if (n.Name == "message")
                        {
                            if (group.Contains(n.ChildNodes[0].InnerText))
                            {
                                continue;
                            }
                            else
                            {
                                group.Add(n.ChildNodes[0].InnerText, n.ChildNodes[1].InnerText);
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        /// <summary>
        /// The init.
        /// </summary>
        /// <param name="filename">
        /// The filename.
        /// </param>
        /// <param name="_lang">
        /// The _lang.
        /// </param>
        private void Init(string filename, string _lang)
        {
            s_lang = _lang;
            s_region = _lang;

            switch (s_region)
            {
                case "en": s_region = "US"; break;
                case "uk": s_region = "UA"; break;
                case "ru": s_region = "RU"; break;
            }

            if (translator == null)
                translator = new Hashtable();
            else
                translator.Clear();

            if ((s_lang == null) || (s_lang.Length == 0))
            {
                s_lang = Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName;
            }
            else if (s_lang.Length == 2)
            {
                try
                {
                    Thread.CurrentThread.CurrentCulture = new CultureInfo(s_lang + "-" + s_region);
                }
                catch (Exception)
                {
                }
            }
            else
            {
                try
                {
                    Thread.CurrentThread.CurrentCulture = new CultureInfo(s_lang);
                }
                catch (Exception)
                {
                }
            }

            string _fileName = AssignFile(filename, s_lang);
            if (_fileName.Length == 0)
            {
                return;
            }

            try
            {
                Init(_fileName);
            }
            catch (Exception)
            {
            }
        }

        public static ArrayList GetLangList(string path)
        {
            var result = new ArrayList();

            var fls = Directory.EnumerateFiles(path, "ak_??.tsl");

            var enr = fls.GetEnumerator();
            enr.MoveNext();
            while (!string.IsNullOrEmpty(enr.Current))
            {
                result.Add(enr.Current);
                enr.MoveNext();
            }

            Assembly assembly;
            assembly = Assembly.GetExecutingAssembly();

            var resNames =
                assembly.GetManifestResourceNames();
            foreach (string item in resNames)
            {
                if (item.EndsWith(".tsl") && item.Length >= 9)
                {
                    string s = item.Substring(item.Length - 9, 9);
                    if (!result.Contains(s))
                        result.Add(s);
                }
            }

            return result;
        }

        #endregion
    }

    public class LangResources : INotifyPropertyChanged
    {
        #region translations

        public string Tournament { get { return this.tr("Tournament"); } }
        public string New { get { return this.tr("New"); } }
        public string Open { get { return this.tr("Open ..."); } }
        public string Save { get { return this.tr("Save"); } }
        public string SaveAs { get { return this.tr("Save as ..."); } }
        public string Properties { get { return this.tr("Properties"); } }
        public string Close { get { return this.tr("Close"); } }
        public string Exit { get { return this.tr("Exit"); } }
        public string Players { get { return this.tr("Players"); } }
        public string PlayersAdd { get { return this.tr("Add"); } }
        public string Round { get { return this.tr("Round"); } }
        public string Before { get { return this.tr("Before"); } }
        public string After { get { return this.tr("After"); } }
        public string Round2 { get { return this.tr("round"); } }
        public string Round3 { get { return this.tr("round "); } }
        public string CurrentRound { get { return this.tr("Go to round"); } }
        public string CleanPairs { get { return this.tr("Clean pairs"); } }
        public string AutoPairing { get { return this.tr("Auto pairing"); } }
        public string AddPair { get { return this.tr("Add pair"); } }
        public string WallList { get { return this.tr("Wall List"); } }
        public string Pairing { get { return this.tr("Pairing"); } }
        public string File { get { return this.tr("File"); } }

        public string Rating { get { return this.tr("Rating"); } }
        public string ScoreAdjustment { get { return this.tr("Adjust by"); } }
        public string Name { get { return this.tr("Name"); } }
        public string Rank { get { return this.tr("Rank"); } }
        public string BoardNumber { get { return this.tr("Board"); } }
        public string FirstPlayer { get { return this.tr("Black"); } }
        public string SecondPlayer { get { return this.tr("White"); } }
        public string Result { get { return this.tr("Result"); } }
        public string Handicap { get { return this.tr("Handicap"); } }
        public string StartScores { get { return this.tr("Start scores"); } }
        public string StartNumber { get { return this.tr("Start number"); } }
        public string GenerateStartNumbers { get { return this.tr("Generate random start numbers"); } }
        
        public string AllRounds { get { return this.tr("for all rounds"); } }
        public string CurrentRounds { get { return this.tr("for current round"); } }
        
        public string Ok { get { return this.tr("OK"); } }
        public string Apply { get { return this.tr("Apply"); } }
        public string SaveAndCreateNew { get { return this.tr("Save and create new"); } }
        public string Cancel { get { return this.tr("Cancel"); } }
        public string Data { get { return this.tr("Data"); } }
        public string SearchResult { get { return this.tr("Search result"); } }
        public string FirstName { get { return this.tr("First name"); } }
        public string Surname { get { return this.tr("Surname"); } }
        public string Country { get { return this.tr("Country"); } }
        public string Club { get { return this.tr("Club"); } }
        public string CreateСlub { get { return this.tr("Create club"); } }
        public string EditСlub { get { return this.tr("Edit club"); } }
        public string Nationality { get { return this.tr("Nationality"); } }
        public string Team { get { return this.tr("Team"); } }
        public string Registration { get { return this.tr("Registration"); } }
        public string Preliminary { get { return this.tr("Preliminary"); } }
        public string Finally { get { return this.tr("Finally"); } }
        public string PlayingInRounds { get { return this.tr("Playing in rounds"); } }
        public string SearchInEGD { get { return this.tr("Search in EGD"); } }
        public string ClearFields { get { return this.tr("Clear fields"); } }
        public string CreateClub { get { return this.tr("Create club"); } }
        public string Comments { get { return this.tr("Comments"); } }
        public string Select { get { return this.tr("Select"); } }

        public string Main { get { return this.tr("Main"); } }
        public string PlacementCriteria { get { return this.tr("Criteria"); } }
        public string PairingRestrictions { get { return this.tr("Pairing"); } }

        public string TournamentProperties { get { return this.tr("Tournament properties"); } }
        public string TournamentName { get { return this.tr("Tournament name"); } }
        public string Description { get { return this.tr("Description"); } }
        public string BoardSize { get { return this.tr("Board size"); } }
        public string NumberOfRounds { get { return this.tr("Number of rounds"); } }
        public string Optimum { get { return this.tr("optimum"); } }
        public string Maximum { get { return this.tr("maximum"); } }
        public string Minimum { get { return this.tr("minimum"); } }
        public string NumberOfPlayers { get { return this.tr("Number of players"); } }
        public string NumberOfPrizes { get { return this.tr("Number of prizes"); } }
        public string NumberOfTeamPlayers { get { return this.tr("Number of team players"); } }
        public string AllowJigo { get { return this.tr("Allow jigo (give ½ komi otherwise)"); } }
        public string RatingDeterminesRank { get { return this.tr("Rating determines rank"); } }
        public string UpperMacMahonBar { get { return this.tr("Upper MacMahon bar"); } }
        public string LowerMacMahonBar { get { return this.tr("Lower MacMahon bar"); } }
        public string MacMahonStartScores { get { return this.tr("Use MacMahon start scores"); } }
        public string StartScoresWithoutGaps { get { return this.tr("Assing start scores without gaps"); } }
        public string SetStartScoresManually { get { return this.tr("Set start scores manually"); } }
        public string TopGroup { get { return this.tr("top group"); } }

        public string Hint { get { return this.tr("Use Drag&Drop to change order"); } }
        public string Hint2 { get { return this.tr("These parameters are not used for the top group member"); } }
        public string Active { get { return this.tr("Active"); } }
        public string Available { get { return this.tr("Available"); } }
        public string DoNotPair { get { return this.tr("Do not pair players"); } }

        public string SameCountry { get { return this.tr("from same country"); } }
        public string SameClub { get { return this.tr("from same club"); } }
        public string SameNationality { get { return this.tr("with same nationality"); } }
        public string SameTeam { get { return this.tr("of same team"); } }
        public string SameCoach { get { return this.tr("of same coach"); } }

        public string HandicapCalculationCase { get { return this.tr("Handicap calculation base"); } }
        public string CalculateAdditionalKomi { get { return this.tr("Calculate additional komi"); } }
        public string PlayersRanks { get { return this.tr("Players' rank"); } }
        public string PlayersPoints { get { return this.tr("Players' current points"); } }
        public string PlayersRatings { get { return this.tr("Players' rating"); } }
        public string BelowLevel { get { return this.tr("Below level"); } }
        public string AdjustBy { get { return this.tr("Adjust by (stones)"); } }
        public string HandicapLimit { get { return this.tr("Limit handicap to (stones)"); } }
        public string IncludeHandicap { get { return this.tr("Include handicap in the calculation of the breakers (SOS, SOSOS, etc.)"); } }
        public string UseHandicap { get { return this.tr("Use handicap"); } }

        public string UseSupergroup { get { return this.tr("Use manually managed Super bar"); } }
        public string SupergroupMember { get { return this.tr("Super bar member"); } }

        public string Board { get { return this.tr("Board"); } }
        public string Black { get { return this.tr("Black"); } }
        public string White { get { return this.tr("White"); } }
        public string BoardNumberIsFixed { get { return this.tr("Board number is fixed"); } }
        public string SwapPlayers { get { return this.tr("Swap players"); } }
        public string GameResult { get { return this.tr("Game result"); } }
        public string ForcePairing { get { return this.tr("Force pairing"); } }
        public string ResultByRefereeDecision { get { return this.tr("Result by referee decision"); } }
        public string Stones { get { return this.tr("stones"); } }
        public string Stones234 { get { return this.tr("stones "); } }
        public string Stone { get { return this.tr("stone"); } }
        public string Points567 { get { return this.tr("points"); } }
        public string Points234 { get { return this.tr("points "); } }
        public string Point { get { return this.tr("point"); } }
        public string Levels567 { get { return this.tr("levels"); } }
        public string Levels234 { get { return this.tr("levels "); } }
        public string Level { get { return this.tr("level"); } }
        public string Delete { get { return this.tr("Delete"); } }
        public string DataWillNot { get { return this.tr("The data will not be able to restore."); } }
        public string PointsForWinner { get { return this.tr("Points for winner or black in drawn game. Used for SORP."); } }

        public string Pair { get { return this.tr("Pair"); } }
        public string RefreshWallList { get { return this.tr("Refresh Wall List"); } }
        public string Player { get { return this.tr("Player"); } }
        public string EgdSearchError { get { return this.tr("EGD Search Error"); } }
        public string Edit { get { return this.tr("Edit"); } }
        public string Record { get { return this.tr("record"); } }
        public string Records234 { get { return this.tr("records"); } }
        public string Records567 { get { return this.tr("records "); } }
        public string Player1 { get { return this.tr("player"); } }
        public string Players234 { get { return this.tr("players"); } }
        public string Players567 { get { return this.tr("players "); } }

        public string ImpossibleToPair { get { return this.tr("It is impossible to pair. Check the participants amount and the tournament settings."); } }
        public string ImpossibleToSynchronize { get { return this.tr("It is impossible to syhchronize. Check the Internet connection."); } }
        public string Warning { get { return this.tr("Warning!"); } }
        public string AlreadyPlayed { get { return this.tr("Players were already matched"); } }
        public string AlreadyNumbered { get { return this.tr("The number is duplicated"); } }

        public string PlaceDescr { get { return this.tr("Place based on criteria"); } }
        public string NameDescr { get { return this.tr("Player (or team) name"); } }
        public string CountryDescr { get { return this.tr("Country name"); } }
        public string CityDescr { get { return this.tr("City name"); } }
        public string ClubDescr { get { return this.tr("Club name"); } }
        public string TeamDescr { get { return this.tr("Team name"); } }
        public string RatingDescr { get { return this.tr("Registered rating"); } }
        public string RankDescr { get { return this.tr("Oriental dan/kyu gradation"); } }
        public string NewRatingDescr { get { return this.tr("Calculated rating"); } }
        public string GroupDescr { get { return this.tr("MacMahon start scores"); } }
        public string ToursDescr { get { return this.tr("Pairing and game result"); } }
        public string CriteriasDescr { get { return this.tr("Fields from \"Criteria\" tab"); } }
        public string CoachDescr { get { return this.tr("Coach name"); } }
        public string GradeDescr { get { return this.tr("Grade by State sports classification"); } }

        public string ScoreDescr { get { return this.tr("Calculated MacMahon score"); } }
        public string ScoreXDescr { get { return this.tr("Score without missed rounds"); } }
        public string PointsDescr { get { return this.tr("Number of wins"); } }
        public string SodosDescr { get { return this.tr("Sum of defeated oppenent scores"); } }
        public string SosDescr { get { return this.tr("Sum of opponent score"); } }
        public string SososDescr { get { return this.tr("Sum of oppenent SOS"); } }
        public string SorpDescr { get { return this.tr("Sum of optional Result points"); } }
        public string SoudDescr { get { return this.tr("Sum of Up/Down (+1/-1) pairing balance"); } }
        public string PgrcDescr { get { return this.tr("Personal game result comparing"); } }

        public string Pl { get { return this.tr("Pl."); } }
        public string Place { get { return this.tr("Place"); } }
        public string Num { get { return this.tr("#"); } }
        public string City { get { return this.tr("City"); } }
        public string Group { get { return this.tr("Group"); } }
        public string Points { get { return this.tr("Points"); } }
        public string Score { get { return this.tr("Score"); } }
        public string ScoreX { get { return this.tr("ScoreX"); } }
        public string SODOS { get { return this.tr("SODOS"); } }
        public string SOSOS { get { return this.tr("SOSOS"); } }
        public string SOS { get { return this.tr("SOS"); } }
        public string SORP { get { return this.tr("SORP"); } }
        public string SOUD { get { return this.tr("SOUD"); } }
        public string PGRC { get { return this.tr("PGRC"); } }

        public string Tours { get { return this.tr("Rounds"); } }
        public string Column { get { return this.tr("Column"); } }
        public string ResultPoints { get { return this.tr("Result points"); } }
        public string HideEmptyColumns { get { return this.tr("Hide columns with empty data"); } }
        public string NoData { get { return this.tr("No data"); } }

        public string Dan { get { return this.tr("dan"); } }
        public string Kyu { get { return this.tr("kyu"); } }
        public string HalfKomi { get { return this.tr("½ komi"); } }
        public string FullKomi { get { return this.tr("komi"); } }
        public string Komi { get { return this.tr("Komi"); } }

        private string _grade = "Grade";

        public string GradeHonoredMaster { get { return this.tr("HM", _grade); } }
        public string GradeInternationalMaster { get { return this.tr("IM", _grade); } }
        public string GradeMaster { get { return this.tr("M", "Grade"); } }
        public string GradeCandidate { get { return this.tr("C", _grade); } }
        public string Grade1 { get { return this.tr("1", _grade); } }
        public string Grade2 { get { return this.tr("2", _grade); } }
        public string Grade3 { get { return this.tr("3", _grade); } }
        public string GradeJnr1 { get { return this.tr("1 jnr", _grade); } }
        public string GradeJnr2 { get { return this.tr("2 jnr", _grade); } }
        public string GradeJnr3 { get { return this.tr("3 jnr", _grade); } }

        public string GradeHonoredMasterDescr { get { return this.tr("Honored Master", _grade); } }
        public string GradeInternationalMasterDescr { get { return this.tr("International Master", _grade); } }
        public string GradeMasterDescr { get { return this.tr("Master", _grade); } }
        public string GradeCandidateDescr { get { return this.tr("Candidate to Master", _grade); } }
        public string Grade1Descr { get { return this.tr("Grade 1", _grade); } }
        public string Grade2Descr { get { return this.tr("Grade 2", _grade); } }
        public string Grade3Descr { get { return this.tr("Grade 3", _grade); } }
        public string GradeJnr1Descr { get { return this.tr("Grade 1 junior", _grade); } }
        public string GradeJnr2Descr { get { return this.tr("Grade 2 junior", _grade); } }
        public string GradeJnr3Descr { get { return this.tr("Grade 3 junior", _grade); } }

        public string EgdName { get { return this.tr("EGD name (up to 4 characters)"); } }

        public string Reports { get { return this.tr("Reports"); } }
        public string PrintWallList { get { return this.tr("Print Wall List ..."); } }
        public string PrintPairingTable { get { return this.tr("Print pairing table ..."); } }
        public string ExportWallList { get { return this.tr("Export Wall List ..."); } }
        public string ExportWallListRoundRobin { get { return this.tr("Export Round-robin Wall List..."); } }
        public string ExportWallListForRating { get { return this.tr("Export data for Rating system ..."); } }
        public string ExportWallListForRatingEng { get { return this.tr("Export data for Rating system (eng.) ..."); } }
        public string ExportPairing { get { return this.tr("Export pairing table ..."); } }
        public string Import { get { return this.tr("Import"); } }
        public string History { get { return this.tr("History"); } }
        public string AbnormalGrowth { get { return this.tr("abnormal growth"); } }
        public string NewRating { get { return this.tr("Rating₂"); } }
        public string CalculateNewRatingForPlayers { get { return this.tr("Calculate new rating for the players to be imported?"); } }
        public string ShowColors { get { return this.tr("Show colors"); } }

        public string Help { get { return this.tr("Help"); } }
        public string Theme { get { return this.tr("Theme"); } }
        public string CleanTheme { get { return this.tr("Clean theme"); } }
        public string About { get { return this.tr("About"); } }
        public string Language { get { return this.tr("Language"); } }

        public string PreliminaryRegisteredPlayers { get { return this.tr("Preliminary registered players"); } }
        public string FinalizeAll { get { return this.tr("Finalize all"); } }
        public string DeleteAll { get { return this.tr("Delete all"); } }

        public string Licence { get { return this.tr("Licence"); } }
        public string Build { get { return this.tr("Build"); } }
        public string SaveChanges { get { return this.tr("Do you want to save changes?"); } }
        public string RecentFiles { get { return this.tr("Recent files"); } }
        
        public string Contacts { get { return this.tr("Contacts"); } }
        public string Yes { get { return this.tr("Yes"); } }
        public string No { get { return this.tr("No"); } }

        public string SkippingTheRounds { get { return this.tr("Skipping the rounds"); } }
        public string Coach { get { return this.tr("Coach"); } }
        public string Grade { get { return this.tr("Grade"); } }
        public string InternationalName { get { return this.tr("Name (eng.)"); } }
        public string InternationalSurname { get { return this.tr("Surname (eng.)"); } }
        public string Transliterate { get { return this.tr("Transliterate"); } }
        public string TransliterationSupport { get { return this.tr("Transliteration support"); } }
        public string ByLevel { get { return this.tr("By level"); } }
        public string ByAmount { get { return this.tr("By amount"); } }
        public string ImpossibleExtention { get { return this.tr("It is impossible to save the tournament with current extention."); } }
        public string DisplayHadicapInComi { get { return this.tr("Display handicap in komi (in stones otherwise)"); } }
        

        public string Groups { get { return this.tr("Groups"); } }

        public string UseLocalPlayerDatabase { get { return this.tr("Use local player database"); } }
        public string DatabaseSource { get { return this.tr("Database source"); } }
        public string UkrainianRatingSystem { get { return this.tr("UFGO rating-list"); } }
        public string RussianRatingSystem { get { return this.tr("RFG rating-list"); } }
        public string ImportData { get { return this.tr("Synchronize"); } }
        public string ActualDate { get { return this.tr("from"); } }
        public string NotLoaded { get { return this.tr("not synchronized"); } }
        public string NextRound { get { return this.tr("Next round"); } }
        public string PreviousRound { get { return this.tr("Previous round"); } }
        public string TournamentSystem { get { return this.tr("Tournament system"); } }
        public string Swiss { get { return this.tr("Swiss"); } }
        public string Scheveningen { get { return this.tr("Scheveningen"); } }
        public string RoundRobin { get { return this.tr("Round-robin"); } }
        public string McMahon { get { return this.tr("McMahon"); } }
        public string FirsMoveColor { get { return this.tr("First move color"); } }
        public string Calculation { get { return this.tr("Calculation"); } }
        public string CustomizeCalculation { get { return this.tr("Customize calculation"); } }
        public string Assistant { get { return this.tr("Assistant"); } }
        public string AssistantInfo { get { return this.tr("Enter awaited amount of players and planned quantity of rounds."); } }
        public string RecommendedSystem { get { return this.tr("Recommended system:"); } }
        public string PressOKOrApply { get { return this.tr("Press OK or Apply to implement the recommendation to the tournament settings."); } }
        
        public string ForRoundsSkipping { get { return this.tr("Accounting for skipped rounds"); } }
        public string AddZeroToSelfCoefs { get { return this.tr("Add zero to own coefficients"); } }
        public string AddMaximumToCompetitorCoefs { get { return this.tr("Add maximum to competitors coefficients"); } }
        public string AddNothing { get { return this.tr("Add nothing"); } }
        public string AddHalfPoint { get { return this.tr("Add 0.5 points"); } }
        public string AlternatePoints { get { return this.tr("Alternate zero and one point"); } }
        public string StandardCalculation { get { return this.tr("Standard calculation"); } }
        public string TopGroupOnly { get { return this.tr("Top group only"); } }

        public string NewVersion { get { return this.tr("New version {0} from {1} is available!"); } }
        public string WhatsNew { get { return this.tr("What's new:"); } }
        public string VersionInfo { get { return this.tr("Version {0} from {1} news:"); } }

        public string TakeCurrentRoundInAccount { get { return this.tr("Take current round in account"); } }
        public string Send { get { return this.tr("Send a report"); } }
        public string UnexpectedError { get { return this.tr("Unexpected error appeared!"); } }
        public string Copy { get { return this.tr("Copy to Clipboard"); } }
        public string RestartMessage { get { return this.tr("The application update is ready to be installed. Press \"Restart the application\" to finish or \"Cancel\" to postpone updating process."); } }
        public string Restart { get { return this.tr("Restart the application"); } }

        public string ContactsEmail { get { return "mailto:dmitry@korolev.kiev.ua"; } }
        public string ContactsUrl { get { return "http://kfgo.org.ua/autokorsak/"; } }
        public string ContactsEmailTxt { get { return "dmitry@korolev.kiev.ua"; } }
        public string ContactsUrlTxt { get { return "kfgo.org.ua/autokorsak/"; } }
        public string AppUpdateUrl { get { return "http://kfgo.org.ua/autokorsak/autokorsak.exe"; } }

        public string ResourceIsUnavailable { get { return this.tr("Resource {0} is unavailable: {1}"); } }

        public string LicenceText 
        {
            get
            {
                string result = "This software is provided as is with no warranty. Licence is granted to use the software at tournament of all kinds without charge.";
                switch (Translator.Language)
                {
                    case "ru": result = "Данное программное обеспечение поставляется \"как есть\", без какой-либо гарантии. Лицензия позволяет использовать программное обеспечение на турнирах любых видов без оплаты."; break;
                    case "uk": result = "Дане програмне забезпечення поставляється \"як є\", без жодної гарантії. Ліцензія дозволяє використовувати програмне забезпечення на турнірах будь-яких видів без оплати."; break;
                }
                return result;
            }
        }

        public string AboutText
        {
            get
            {
                string result = "The software provides with pairing algorithm based on Vladimir Korsak grouping method. It is a variation of McMahon pairing system. Main idea is in separating one McMahon group to higher and lower parts and than pairing the first players from these parts while the group has more than one player. (Nevertheless the last round is paired by \"'the first againsg the last\" schema.) Unpaired player should be paired with middle player from next McMahon group. ";
                switch (Translator.Language)
                {
                    case "ru": result = "Программное обеспечение осуществляет алгоритм жеребьёвки, основанный на методе группировок Владимира Корсака. Данный метод является вариацией системы жеребьёвки Макмагона. Главная идея состоит в разделении группы Макмагона на верхнюю и нижнюю части и последующего сведения первых игроков из этих частей до тех пор, пока группа имеет более одного игрока. (Однако в последнем туре применяется схема \"первый с последним\".) Оставшийся без пары игрок должен быть сведён со средним игроком из следующей группы Макмагона."; break;
                    case "uk": result = "Програмне забезпечення здійснює алгоритм жеребкування, заснований на методі угруповань Володимира Корсака. Даний метод є варіацією системи жеребкування Макмагона. Головна ідея полягає в поділі групи Макмагона на верхню та нижню частини і подальшого зведення перших гравців цих частин до тих пір, поки група має більше одного гравця. (Проте в останньому турі застосовується схема \"перший з останнім\".) Гравець, що залишився без пари, повинен бути зведений із середнім гравцем з наступної групи Макмагона."; break;
                }
                return result;
            }
        }

        public string ContactsText
        {
            get
            {
                string result = "For any questions and suggestions: ";
                switch (Translator.Language)
                {
                    case "ru": result = "По всем вопросам и предложениям: "; break;
                    case "uk": result = "За будь-яких питань чи пропозицій: "; break;
                }
                return result;
            }
        }

        public string ContactsText2
        {
            get
            {
                string result = "The software official site: ";
                switch (Translator.Language)
                {
                    case "ru": result = "Официальный сайт программы: "; break;
                    case "uk": result = "Офіційний сайт програми: "; break;
                }
                return result;
            }
        }

        public string Copyright
        {
            get
            {
                string result = "© 2013 ";
                switch (Translator.Language)
                {
                    case "ru": result += "Дмитрий Королёв"; break;
                    case "uk": result += "Дмитро Корольов"; break;
                    default: result += "Dmitry Korolev"; break;
                }
                return result;
            }
        }

        
        // no translations
        public string D { get { return "d"; } }
        public string K { get { return "k"; } }
        public string B { get { return "b"; } }
        public string W { get { return "w"; } }

        #endregion

        public static string GetStoneStr(int stones)
        {
            string result;
            stones = Math.Abs(stones);
            if (stones % 100 > 10 && stones % 100 < 20)
                stones = 0;
            switch (stones % 10)
            {
                case 1: result = LangResources.LR.Stone; break;
                case 2:
                case 3:
                case 4:
                    result = LangResources.LR.Stones234; break;
                default:
                    result = LangResources.LR.Stones; break;
            }

            return result.Trim();
        }

        public static string GetPointsStr(double points)
        {
            string result;
            points = Math.Abs(points);

            if (points - Math.Floor(points) > 0)
                points = Math.Floor(points);
            
            if (points % 100 > 10 && points % 100 < 20)
            {
                points = 0;
            }

            switch ((int)points % 10)
            {
                case 1: result = LangResources.LR.Point; break;
                case 2:
                case 3:
                case 4:
                    result = LangResources.LR.Points234; break;
                default:
                    result = LangResources.LR.Points567; break;
            }

            return result.Trim();
        }

        public static string GetLevelsStr(int levels)
        {
            string result;
            levels = Math.Abs(levels);
            if (levels % 100 > 10 && levels % 100 < 20)
                levels = 0;
            switch (levels % 10)
            {
                case 1: result = LangResources.LR.Level; break;
                case 2:
                case 3:
                case 4:
                    result = LangResources.LR.Levels234; break;
                default:
                    result = LangResources.LR.Levels567; break;
            }

            return result.Trim();
        }

        public static string GetRecordsStr(int records)
        {
            string result;
            records = Math.Abs(records);
            if (records % 100 > 10 && records % 100 < 20)
                records = 0;
            switch (records % 10)
            {
                case 1: result = LangResources.LR.Record; break;
                case 2:
                case 3:
                case 4:
                    result = LangResources.LR.Records234; break;
                default:
                    result = LangResources.LR.Records567; break;
            }

            return result.Trim();
        }

        public static string GetPlayersStr(int players)
        {
            string result;
            players = Math.Abs(players);
            if (players % 100 > 10 && players % 100 < 20)
                players = 0;
            switch (players % 10)
            {
                case 1: result = LangResources.LR.Player1; break;
                case 2:
                case 3:
                case 4:
                    result = LangResources.LR.Players234; break;
                default:
                    result = LangResources.LR.Players567; break;
            }

            return result.Trim();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler LanguageChanged;
        public event EventHandler SettingsChanged;

        public void Update()
        {
            if (null != this.PropertyChanged)
                foreach (var item in this.GetType().GetProperties())
                    this.PropertyChanged(this, new PropertyChangedEventArgs(item.Name));
            if (null != this.LanguageChanged)
                this.LanguageChanged(this, new EventArgs());
        }

        public void UpdateSettings()
        {
            if (null != this.SettingsChanged)
                this.SettingsChanged(this, new EventArgs());
        }

        private string tr(string text)
        {
            return Translator.Translate("Common", text);
        }

        private string tr(string text, string section)
        {
            return Translator.Translate(section, text);
        }

        private static LangResources _lr;

        private static LangResources GetLangResouces()
        {
            return _lr ?? (_lr = new LangResources());
        }

        public static LangResources LR
        {
            get { return GetLangResouces(); }
        }

    }

    public class TranslitItem
    {
        #region Constants and Fields

        private string _region = "us";

        private Hashtable translator;

        #endregion

        #region Constructors and Destructors

        public TranslitItem(string filename, string region)
        {
            this.Init(filename, region);
        }

        #endregion

        #region Public Properties

        public string Region
        {
            get
            {
                return _region;
            }
        }

        #endregion

        #region Public Methods and Operators

        public string Translate(string context, string text)
        {
            if (translator == null || text == null)
                return string.Empty;

            if (text.Length == 1)
            {
                char c = text[0];
                if (c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '-' || c == ' ' || c == '.' || c == '_')
                    return text;
            }

            var ht = translator[context] as Hashtable;
            if (ht == null)
                return string.Empty;

            var rtext = ht[text] as string;
            if (rtext == null)
                return string.Empty;

            if (rtext.Length == 0)
                return string.Empty;

            return rtext;
        }

        public Hashtable GetContext(string context)
        {
            if (translator == null)
                return null;
            return (Hashtable)translator[context];
        }

        public void SetFile(string file)
        {
            if (translator != null)
            {
                translator.Clear();
                translator = null;
            }

            translator = new Hashtable();

            try
            {
                Init(file);
            }
            catch (Exception)
            {
            }
        }

        #endregion

        #region Methods

        private void Init(string filename)
        {
            Assembly assembly;
            assembly = Assembly.GetExecutingAssembly();

            var xd = new XmlDocument();
            Stream stream = null;
            try
            {
                stream = // new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    assembly.GetManifestResourceStream("Tourtoss.BE." + filename);
                if (stream != null)
                {
                    var reader = new XmlTextReader(stream);
                    xd.Load(reader);
                    reader.Close();
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                if (null != stream)
                    stream.Close();
            }

            /*
            var reader = new XmlTextReader(filename);
            var xd = new XmlDocument();
            xd.Load(reader);
            reader.Close();
            */
            string contextName = string.Empty;

            if (xd.DocumentElement != null)
            {
                foreach (XmlNode node in xd.DocumentElement.ChildNodes)
                {
                    var group = new Hashtable();
                    foreach (XmlNode n in node.ChildNodes)
                    {
                        try
                        {
                            if (n.Name == "name")
                            {
                                contextName = n.InnerText;
                                if (translator.Contains(n.InnerText))
                                {
                                    group = translator[n.InnerText] as Hashtable;
                                }
                                else
                                {
                                    translator.Add(n.InnerText, group);
                                }
                            }
                            else if (n.Name == "l")
                            {
                                if (group.Contains(n.Attributes[0].InnerText))
                                {
                                    continue;
                                }
                                else
                                {
                                    group.Add(n.Attributes[0].InnerText, n.Attributes[1].InnerText);
                                }
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
        }

        private void Init(string filename, string region)
        {
            _region = region;

            if (translator == null)
                translator = new Hashtable();
            else
                translator.Clear();

            try
            {
                Init(filename);
            }
            catch (Exception)
            {
            }
        }

        #endregion
    }

    public class Translit
    { 
        private static List<TranslitItem> items = new List<TranslitItem>();

        public static string Transliterate(string countryCode, string text)
        {
            StringBuilder result = new StringBuilder();
            if (!string.IsNullOrEmpty(text))
            {
                string region = string.IsNullOrEmpty(countryCode) ? string.Empty : countryCode.ToLower();
                TranslitItem tr = items.Find(item => item.Region == region);
                if (tr == null)
                {
                    items.Add(new TranslitItem("ak_" + region + ".tsx", region));
                    tr = items[items.Count - 1];
                }

                string r = tr.Translate("Translate", text);
                if (r != string.Empty)
                    return r;

                Hashtable context = tr.GetContext("Macro");
                if (context != null)
                    foreach (DictionaryEntry item in context)
                        if (text.IndexOf((string)item.Key) > -1)
                            text = text.Replace((string)item.Key, (string)item.Value);

                for (int i = 0; i < text.Length; i++)
                    result.Append(tr.Translate("Translit", text[i].ToString()));
            }
            return result.ToString();
        }
    }
}