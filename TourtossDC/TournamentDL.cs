using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Tourtoss.BE;

namespace Tourtoss.DL
{

    public class TournamentDL: BaseDL
    {

        public static string LocalDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\AutoKorsak\";

        public void SaveTournament(string fileName, Tournament tournament)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Tournament));
            TextWriter writer = new StreamWriter(fileName);
            try
            {
                serializer.Serialize(writer, tournament);
            }
            catch {/* ... */}
            finally
            { 
                writer.Close(); 
            }
       }

        public Tournament Load(string fileName)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Tournament));
            var result = new Tournament();
            Stream stream = null;
            try
            {
                stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                result = (Tournament)serializer.Deserialize(stream);
            }
            catch {/* ... */}
            finally
            {
                if (null != stream)
                    stream.Close();
            }
            return result;
        }

        public Countries LoadCountries(string fileName)
        {

            Assembly assembly;
            assembly = Assembly.GetExecutingAssembly();
            
            XmlSerializer serializer = new XmlSerializer(typeof(Countries));
            var result = new Countries();
            Stream stream = null;
            try
            {
                stream = //new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    assembly.GetManifestResourceStream("Tourtoss.DL." + fileName);

                result = (Countries)serializer.Deserialize(stream);
            }
            catch (Exception) 
            {
            }
            finally
            {
                if (null != stream)
                    stream.Close();
            }
            return result;
        }

        private int? GetInt(string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            StringBuilder bldr = new StringBuilder();
            
            for (int i = 0; i < value.Length; i++)
                if (char.IsDigit(value[i]))
                    bldr.Append(value[i]);
                else
                    if (value[i] != '(') // for separated place
                        break;

            string s = bldr.ToString();
            if (string.IsNullOrEmpty(s))
                return null;
            else
            {
                int i = 0;
                int.TryParse(s, out i);
                return i;
            }
        }

        private bool IsInt(string value, bool strict = false)
        {
            bool result = false;
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsDigit(value[i]))
                {
                    if (!strict)
                        return true;
                    else
                        result = true;
                }
                else
                {
                    if (strict)
                        return false;
                }
            }
            return result;
        }

        private bool IsTourRecord(string value)
        {
            bool result =
                          value.Equals("--", StringComparison.OrdinalIgnoreCase) ||
                          value.Equals("+-", StringComparison.OrdinalIgnoreCase) ||
                          value.Equals("-", StringComparison.OrdinalIgnoreCase) ||
                          value.Equals("free", StringComparison.OrdinalIgnoreCase) ||
                          value.Equals("Bye", StringComparison.OrdinalIgnoreCase);

            if (!result)
            {
                if (GetInt(value) != null)
                {
                    if  (value.Contains('+') ||
                        value.Contains('-') ||
                        value.Contains('?') ||
                        value.Contains('=')
                        )
                        result = true;
                } else
                    if (value.Contains('?'))
                        result = true;
            }

            return result;
        }

        private bool IsWrongEncoding(string s) 
        {
            if (string.IsNullOrEmpty(s))
                return false;
            
            char c = s[0];
            for (int i = 1; i< s.Length; i++)
                if (c != s[i])
                    return false;

            return c == '�';
        }

        private string GetStr(Dictionary<Entity, int> columns, string[] arr, Entity entity, int shift)
        {
            if (columns.ContainsKey(entity))
            {
                int i = columns[entity] + shift;
                if (i > -1 && i < arr.Length)
                    return arr[i];
            }
            return null;
        }

        public Tournament ImportFromTextLines(string[] text, out bool unicodeError, bool unicode = true)
        {
            var result = new Tournament();
            unicodeError = false;
            try
            {
                List<string> lines = new List<string>();

                StringBuilder bldr = new StringBuilder();

                var columns = new Dictionary<Entity, int>();

                foreach (var item in text)
                {
                    bldr.Clear();

                    bool spc = false;
                    bool oldSpc = false;

                    foreach (char ch in item)
                    {
                        spc = ch == ' ';

                        if (!spc)
                        {
                            if (oldSpc && bldr.Length > 0)
                                bldr.Append(' ');

                            bldr.Append(ch);
                        }

                        oldSpc = spc;
                    }
                    lines.Add(bldr.ToString());
                }

                bool header = true;
                int id = 0;
                int lineId = -1;

                //Create players
                foreach (var line in lines)
                {
                    string[] arr = line.Split();

                    if (header &&
                        (line.StartsWith("1") ||
                         line.StartsWith("01"))
                        )
                    {
                        header = false;
                    }

                    bool hdrLineDone = false;

                    if (header && !hdrLineDone)
                    {
                        int hdrShift = 0;
                        for (int i = 0; i < arr.Length; i++)
                        {
                            string c = arr[i];

                            if (IsWrongEncoding(c))
                            {
                                //return ImportFromTxt(fileName, false);
                                unicodeError = true;
                                return null;
                            }

                            if (c.Equals(";", StringComparison.OrdinalIgnoreCase)
                                || c.Equals("R", StringComparison.OrdinalIgnoreCase))
                                hdrShift--;

                            if (c.Equals("Pl.", StringComparison.OrdinalIgnoreCase)
                                || c.Equals("Place", StringComparison.OrdinalIgnoreCase)
                                || c.Equals("№", StringComparison.OrdinalIgnoreCase)
                                || c.Equals(";N", StringComparison.OrdinalIgnoreCase)
                                || c.Equals("N", StringComparison.OrdinalIgnoreCase)
                                || c.Equals("NN", StringComparison.OrdinalIgnoreCase)
                                || c.Equals("Num", StringComparison.OrdinalIgnoreCase))
                            {
                                columns.Add(Entity.Num, i + hdrShift);
                            }

                            if (c.Equals("Pl", StringComparison.OrdinalIgnoreCase))
                            {
                                columns.Add(Entity.Place, i + hdrShift);
                                hdrShift--;
                            }

                            if (c.Equals("Name", StringComparison.OrdinalIgnoreCase) ||
                                c.Equals("Имя", StringComparison.OrdinalIgnoreCase) ||
                                c.Equals("ФИО", StringComparison.OrdinalIgnoreCase) ||
                                c.Equals("Ім'я", StringComparison.OrdinalIgnoreCase))
                            {
                                columns.Add(Entity.Name, i + hdrShift);
                            }

                            if (c.Equals("Rank", StringComparison.OrdinalIgnoreCase) ||
                                c.Equals("Grade", StringComparison.OrdinalIgnoreCase) ||
                                c.Equals("Level", StringComparison.OrdinalIgnoreCase) ||
                                c.Equals("Ранг", StringComparison.OrdinalIgnoreCase) ||
                                c.Equals("Str", StringComparison.OrdinalIgnoreCase))
                            {
                                columns.Add(Entity.Rank, i + hdrShift);
                            }

                            if (c.Equals("Rating", StringComparison.OrdinalIgnoreCase) ||
                                c.Equals("Рейт", StringComparison.OrdinalIgnoreCase) ||
                                c.Equals("Рейтинг", StringComparison.OrdinalIgnoreCase))
                            {
                                columns.Add(Entity.Rating, i + hdrShift);
                            }

                            if (c.Equals("Country", StringComparison.OrdinalIgnoreCase) ||
                                c.Equals("Cou", StringComparison.OrdinalIgnoreCase) ||
                                c.Equals("Co", StringComparison.OrdinalIgnoreCase) ||
                                c.Equals("Страна", StringComparison.OrdinalIgnoreCase) ||
                                c.Equals("Країна", StringComparison.OrdinalIgnoreCase))
                            {
                                columns.Add(Entity.Country, i + hdrShift);
                            }

                            if (c.Equals("Club", StringComparison.OrdinalIgnoreCase) ||
                                c.Equals("Cl.", StringComparison.OrdinalIgnoreCase) ||
                                c.Equals("Предс", StringComparison.OrdinalIgnoreCase) ||
                                c.Equals("Клуб", StringComparison.OrdinalIgnoreCase))
                            {
                                columns.Add(Entity.Club, i + hdrShift);
                            }

                            if (c.Equals("MMS", StringComparison.OrdinalIgnoreCase) ||
                                c.Equals("Score", StringComparison.OrdinalIgnoreCase) ||
                                c.Equals("Очки", StringComparison.OrdinalIgnoreCase))
                            {
                                columns.Add(Entity.Score, i + hdrShift);
                            }

                            if (c.Equals("Group", StringComparison.OrdinalIgnoreCase) ||
                                c.Equals("MM", StringComparison.OrdinalIgnoreCase) ||
                                c.Equals("Группа", StringComparison.OrdinalIgnoreCase) ||
                                c.Equals("Група", StringComparison.OrdinalIgnoreCase))
                            {
                                columns.Add(Entity.Group, i + hdrShift);
                            }
                        }

                        if (columns.ContainsKey(Entity.Num))
                        {
                            foreach (var c in arr)
                            {
                                if (c.Equals("Pt", StringComparison.OrdinalIgnoreCase)
                                    || c.Equals("Points", StringComparison.OrdinalIgnoreCase)
                                    || c.Equals("NbW", StringComparison.OrdinalIgnoreCase)
                                    || c.Equals("Победы", StringComparison.OrdinalIgnoreCase)
                                    || c.Equals("Перемоги", StringComparison.OrdinalIgnoreCase)
                                    )
                                    result.Walllist.SortCriterion.Add(new SortCriterionDescriptior() { Id = Entity.Points, Active = true });
                                if (c.Equals("Score", StringComparison.OrdinalIgnoreCase)
                                    || c.Equals("Очки", StringComparison.OrdinalIgnoreCase))
                                    result.Walllist.SortCriterion.Add(new SortCriterionDescriptior() { Id = Entity.Score, Active = true });
                                if (c.Equals("SOS", StringComparison.OrdinalIgnoreCase)
                                    || c.Equals("кБух", StringComparison.OrdinalIgnoreCase)
                                    || c.Equals("Бухг", StringComparison.OrdinalIgnoreCase)
                                    || c.Equals("K1", StringComparison.OrdinalIgnoreCase)
                                    )
                                    result.Walllist.SortCriterion.Add(new SortCriterionDescriptior() { Id = Entity.SOS, Active = true });
                                if (c.Equals("SODOS", StringComparison.OrdinalIgnoreCase)
                                    || c.Equals("кБерг", StringComparison.OrdinalIgnoreCase)
                                    || c.Equals("Берг", StringComparison.OrdinalIgnoreCase)
                                    || c.Equals("K2", StringComparison.OrdinalIgnoreCase)
                                    )
                                    result.Walllist.SortCriterion.Add(new SortCriterionDescriptior() { Id = Entity.SODOS, Active = true });
                                if (c.Equals("SOSOS", StringComparison.OrdinalIgnoreCase)
                                    || c.Equals("кБух₂", StringComparison.OrdinalIgnoreCase)
                                    || c.Equals("кБух?", StringComparison.OrdinalIgnoreCase)
                                    || c.Equals("кБух2", StringComparison.OrdinalIgnoreCase)
                                    || c.Equals("кБух+", StringComparison.OrdinalIgnoreCase)
                                    || c.Equals("ДБухг", StringComparison.OrdinalIgnoreCase)
                                    || c.Equals("K3", StringComparison.OrdinalIgnoreCase)
                                    )
                                    result.Walllist.SortCriterion.Add(new SortCriterionDescriptior() { Id = Entity.SOSOS, Active = true });
                                if (c.Equals("ScoreX", StringComparison.OrdinalIgnoreCase)
                                    || c.Equals("Очки₂", StringComparison.OrdinalIgnoreCase)
                                    || c.Equals("Очки?", StringComparison.OrdinalIgnoreCase)
                                    || c.Equals("Очки2", StringComparison.OrdinalIgnoreCase)
                                    || c.Equals("Очки+", StringComparison.OrdinalIgnoreCase))
                                    result.Walllist.SortCriterion.Add(new SortCriterionDescriptior() { Id = Entity.ScoreX, Active = true });
                                if (c.Equals("SORP", StringComparison.OrdinalIgnoreCase)
                                    || c.Equals("Доп.", StringComparison.OrdinalIgnoreCase)
                                    || c.Equals("Дод.", StringComparison.OrdinalIgnoreCase)
                                    )
                                    result.Walllist.SortCriterion.Add(new SortCriterionDescriptior() { Id = Entity.SORP, Active = true });
                                if (c.Equals("SOUD", StringComparison.OrdinalIgnoreCase)
                                    || c.Equals("Подъём", StringComparison.OrdinalIgnoreCase)
                                    || c.Equals("Підйом", StringComparison.OrdinalIgnoreCase)
                                    )
                                    result.Walllist.SortCriterion.Add(new SortCriterionDescriptior() { Id = Entity.SORP, Active = true });

                                if (c.Equals("MMS", StringComparison.OrdinalIgnoreCase)
                                    || c.Equals("Group", StringComparison.OrdinalIgnoreCase)
                                    || c.Equals("Группа", StringComparison.OrdinalIgnoreCase)
                                    || c.Equals("Група", StringComparison.OrdinalIgnoreCase))
                                {
                                    result.TournamentSystemMcMahon = true;
                                    result.UseMacMahonStartScoresManually = true;
                                }

                            }

                            if (result.TournamentSystemMcMahon && result.Walllist.SortCriterion.Find(item => item.Id == Entity.Score) == null)
                                result.Walllist.SortCriterion.Insert(0, new SortCriterionDescriptior() { Id = Entity.Score, Active = true });

                            hdrLineDone = true;
                        }
                    }

                    if (!header)
                    {
                        if (arr.Length == 0)
                            continue;
                        if (!IsInt(arr[0]))
                            continue;

                        int start = 1;
                        int shift = 0;
                        if (arr.Length > 1 && IsInt(arr[1]))
                        {
                            start++;
                            shift++;
                        }

                        var player = new Player(result) { Id = ++id };

                        string str;

                        foreach (var column in columns.Keys)
                        {
                            switch (column)
                            {
                                case Entity.Name:
                                    {
                                        bool hasComa = false;

                                        for (int i = start; i < arr.Length; i++)
                                        {
                                            if (i == start) //Surname
                                            {
                                                hasComa = arr[i].EndsWith(",");
                                                player.Surname = hasComa ? arr[i].Substring(0, arr[i].Length - 1) : arr[i];

                                                if (unicode)
                                                {
                                                    if (IsWrongEncoding(player.Surname))
                                                    {
                                                        unicodeError = true;
                                                        return null;
                                                    }
                                                }

                                            }
                                            if (i == start + 1) //FirstName
                                            {
                                                if (hasComa || !IsInt(arr[i]))
                                                {
                                                    player.FirstName = arr[i];
                                                    shift++;
                                                }
                                                break;
                                            }
                                        }
                                        break;
                                    }

                                case Entity.Rank:
                                    {
                                        str = GetStr(columns, arr, Entity.Rank, shift);
                                        if (!string.IsNullOrEmpty(str))
                                        {
                                            int rating = player.Rating;
                                            player.Rank = str;
                                            int z = columns[Entity.Rank] + shift + 1;
                                            if (z < arr.Length)
                                            {
                                                if (arr[z].Equals("d", StringComparison.OrdinalIgnoreCase) ||
                                                    arr[z].Equals("k", StringComparison.OrdinalIgnoreCase) ||
                                                    arr[z].Equals("dan", StringComparison.OrdinalIgnoreCase) ||
                                                    arr[z].Equals("kyu", StringComparison.OrdinalIgnoreCase) ||
                                                    arr[z].Equals("дан", StringComparison.OrdinalIgnoreCase) ||
                                                    arr[z].Equals("кю", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    player.Rating = rating; //avoid rating miscalculating
                                                    player.Rank = str + arr[z];
                                                    shift++;
                                                }
                                            }
                                        }
                                        break;
                                    }
                                case Entity.Rating:
                                    {
                                        int? r = GetInt(GetStr(columns, arr, Entity.Rating, shift));
                                        if (r.HasValue)
                                            player.Rating = r.Value;

                                        break;
                                    }
                                case Entity.Country:
                                    {
                                        player.Country = GetStr(columns, arr, Entity.Country, shift);
                                        break;
                                    }
                                case Entity.Club:
                                    {
                                        str = GetStr(columns, arr, Entity.Club, shift);
                                        if (!string.IsNullOrEmpty(str))
                                        {
                                            if (!IsInt(str, true) && !IsTourRecord(str))
                                            {
                                                StringBuilder sb = new StringBuilder();

                                                //check if country is here
                                                if (str.IndexOf('-') == 2)
                                                {
                                                    player.Country = str.Substring(0, 2);
                                                    str = str.Substring(3);
                                                }

                                                sb.Append(str);
                                                int z = columns[Entity.Club];
                                                while (
                                                    z + shift + 1 < arr.Length &&
                                                    (columns.ContainsKey(Entity.Country) && z + shift + 1 != columns[Entity.Country] + shift) &&
                                                    !IsInt(arr[z + shift + 1]))
                                                {
                                                    sb.Append(' ').Append(arr[columns[Entity.Club] + shift + 1]);
                                                    shift++;
                                                }
                                                player.Club = sb.ToString();
                                            }
                                            else
                                            {
                                                shift--;
                                            }
                                        }
                                        break;
                                    }
                                case Entity.Score:
                                    {
                                        if (!columns.ContainsKey(Entity.Group))
                                        {
                                            str = GetStr(columns, arr, Entity.Score, shift);
                                            if (!string.IsNullOrEmpty(str))
                                            {
                                                int? mms = GetInt(str);
                                                if (mms.HasValue)
                                                {
                                                    player.StartScores = mms.Value;
                                                    int z = columns[Entity.Score] + shift;
                                                    string s = arr[z];
                                                    if (s.EndsWith("Ѕ") //UTF character
                                                        || s.EndsWith("S")
                                                        || s.EndsWith("½")
                                                        || s.EndsWith(".5")
                                                        || s.EndsWith(",5")
                                                        )
                                                        player.StartScores += 0.5;
                                                }
                                            }
                                        }
                                        break;
                                    }
                                case Entity.Group:
                                    {
                                        str = GetStr(columns, arr, Entity.Group, shift);
                                        if (!string.IsNullOrEmpty(str))
                                        {
                                            int? group = GetInt(str);
                                            if (group.HasValue)
                                            {
                                                player.StartScores = group.Value;
                                                int z = columns[Entity.Group] + shift;
                                                string s = arr[z];
                                                if (s.EndsWith("Ѕ") //UTF character
                                                    || s.EndsWith("S")
                                                    || s.EndsWith("½")
                                                    || s.EndsWith(".5")
                                                    || s.EndsWith(",5")
                                                    )
                                                    player.StartScores += 0.5;
                                            }
                                        }
                                        break;
                                    }
                            }
                        }

                        result.Players.Add(player);

                    }

                    lineId++;
                }

                foreach (var player in result.Players)
                {
                    if (player.StartScores > result.NumberOfRounds)
                    {
                        result.TournamentSystemMcMahon = true;
                        result.UseMacMahonStartScoresManually = true;
                        break;
                    }
                }

                id = 0;

                //Fill pairs and games result
                foreach (var line in lines)
                {
                    if (header &&
                        (line.StartsWith("1") ||
                         line.StartsWith("01"))
                        )
                    {
                        header = false;
                    }

                    if (!header)
                    {
                        string[] arr = line.Split();
                        if (arr.Length == 0)
                            continue;
                        if (GetInt(arr[0]) == null)
                            continue;

                        id++;

                        int tourNum = 0;

                        for (int i = 1; i < arr.Length; i++)
                        {
                            if (IsTourRecord(arr[i]))
                            {
                                tourNum++;
                                if (result.Tours.Count < tourNum)
                                    result.Tours.Add(new Tour());

                                if (result.Tours[tourNum - 1].Pairs.Find(id) != null)
                                    continue;

                                int? competitorId = GetInt(arr[i]);

                                if (competitorId != null && competitorId != 0)
                                {
                                    var competitor = result.Players.Find(pl => pl.Id == competitorId);
                                    if (competitor != null)
                                    {
                                        Pair pair = new Pair() { FirstPlayerId = id, SecondPlayerId = competitor.Id };
                                        if (arr[i].Contains('+'))
                                            pair.GameResult = 1;
                                        else
                                            if (arr[i].Contains('-'))
                                                pair.GameResult = 2;
                                            else
                                                if (arr[i].Contains('='))
                                                    pair.GameResult = 3;
                                        
                                        result.Tours[tourNum - 1].Pairs.Add(pair);
                                    }
                                }
                                else
                                {
                                    if (arr[i].Contains("--") ||
                                        arr[i].Equals("-") ||
                                        competitorId == 0
                                        && !arr[i].Equals("00+-")
                                        )
                                    {
                                        var player = result.Players.Find(pl => pl.Id == id);
                                        if (player != null)
                                            player.NotPlayingInRound.Add(tourNum);
                                    }
                                    else
                                        if (arr[i].Equals("free", StringComparison.OrdinalIgnoreCase) ||
                                        arr[i].Equals("Bye", StringComparison.OrdinalIgnoreCase) ||
                                        arr[i].Equals("+-", StringComparison.OrdinalIgnoreCase) ||
                                        arr[i].Equals("00+-", StringComparison.OrdinalIgnoreCase))
                                        {
                                            Pair pair = new Pair() { FirstPlayerId = id, SecondPlayerId = -1 };
                                            result.Tours[tourNum - 1].Pairs.Add(pair);
                                        }
                                }

                            }

                        }

                        if (columns.ContainsKey(Entity.Score) && !columns.ContainsKey(Entity.Group))
                        {
                            var player = result.Players.Find(pl => pl.Id == id);
                            if (player != null)
                                player.StartScores -= player.GetPoints(result, result.Tours.Count);
                        }
                    }

                }

                result.CurrentRoundNumber = result.Tours.Count;
                result.TakeCurrentRoundInAccount = true;

                if (columns.ContainsKey(Entity.Rating) && !columns.ContainsKey(Entity.Rank))
                    result.RatingDeterminesRank = true;

            }
            catch (Exception ex)
            {/* ... */
                ex.ToString();
            }

            return result;
        }

        public Tournament ImportFromTxt(string fileName)
        {
            Tournament result = null;
            try
            {

                var text = File.ReadAllLines(fileName);

                bool unicodeError;
                result = ImportFromTextLines(text, out unicodeError);
                
                if (unicodeError)
                {
                    text = File.ReadAllLines(fileName, Encoding.GetEncoding(1251));
                    result = ImportFromTextLines(text, out unicodeError, false);
                }
            }
            catch(Exception)
            {
            }
            return result;
        }

        public Tournament ImportFromExcel(string fileName, bool unicode = true)
        {
            Tournament result = null;

            var table = new DataTable();

            using (SpreadsheetDocument spreadsheet = SpreadsheetDocument.Open(fileName, false))
            {
                WorkbookPart workbook = spreadsheet.WorkbookPart;
                //create a reference to Sheet1
                WorksheetPart worksheet = workbook.WorksheetParts.Last();
                SheetData data = worksheet.Worksheet.GetFirstChild<SheetData>();

                //add column names to the first row
                Row header = data.GetFirstChild<Row>();

                var rowEnum = header.GetEnumerator();
                while (rowEnum.MoveNext())
                {
                    table.Columns.Add(new DataColumn(rowEnum.Current.InnerText));
                }

                Row row = header.NextSibling() as Row;
                do 
                {
                    if (row != null)
                    {
                        var dataRow = table.Rows.Add();

                        rowEnum = row.GetEnumerator();
                        int i = 0;
                        while (rowEnum.MoveNext())
                        {
                            dataRow[i] = rowEnum.Current.InnerText;
                            i++;
                        }
                        row = row.NextSibling() as Row;
                    }
                }
                while (row != null);
            }

            StringBuilder sb = new StringBuilder();

            foreach (DataColumn item in table.Columns)
            {
                if (item != table.Columns[0])
                {
                    sb.Append(' ');
                }
                sb.Append(item.Caption);
            }
            sb.Append("\n");
            foreach (DataRow item in table.Rows)
            {
                for (int i = 0; i < table.Columns.Count; i++ )
                {
                    if (i != 0)
                    {
                        sb.Append(' ');
                    }
                    sb.Append(item.ItemArray[i]);
                }
                sb.Append('\n');
            }

            bool unicodeError;

            result = ImportFromTextLines(sb.ToString().Split('\n'), out unicodeError);
            return result;
        }
    }

}
