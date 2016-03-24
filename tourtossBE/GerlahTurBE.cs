namespace Tourtoss.BE
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public enum TurSections
    {
        Start,
        Signature,
        Header,
        Player,
        Finish
    }

    public struct TurPlayerRoundInfo
    {
        public short P1_CompetitorId; // 254 - not set, 255 - free
        public byte P3_GameResult; // 129 - ?-?, 130 - 1-0, 132 - 0-1, 136 - jigo, 192 - 1-0! (free), 160 - 0-1!, 144 - 0-0!, 128 - 1-1!, 0 - no result
        public byte P4_Color; // >= 32 - white, otherwise black
        public byte P5_Handicap;
        public byte P6;
        public short P7_Board;
    }

    public struct TurPlayerFooter
    {
        public byte P01;
        public byte P02;
        public byte P03_RegisterKind; // 32 - prelominary, 64 - finally
        public byte P04;
        public short P05_Rank; // >0 - Kyu, <=0 - Dan
        public byte P07;
        public byte P08;

        public byte P09_Bracket; // "]"
        public short P10_Rating;
        public short P12_Num; // registered number
        public byte P14;
        public byte P15;

    }

    public class TurPlayer
    {
        public string FirstName;
        public string Surname;
        public string Comment;
        public int CountryId;
        public int ClubId;
        public TurPlayerRoundInfo[] RoundInfo = new TurPlayerRoundInfo[18];
        public TurPlayerFooter Footer = new TurPlayerFooter();
        
        public int Id 
        { 
            get { return this.Footer.P12_Num; } 
        }

        public bool IsPreliminary 
        { 
            get { return this.Footer.P03_RegisterKind == 32; } 
        }

        public bool IsDan 
        { 
            get { return this.Footer.P05_Rank <= 0; } 
        }

        public string Rank 
        { 
            get { return IntToRank(this.Footer.P05_Rank); } 
        }

        public int Rating 
        { 
            get { return this.Footer.P10_Rating; } 
        }

        public static string IntToRank(int value)
        {
            return (value <= 0 ? 1 - 1 * value : value).ToString() + (value <= 0 ? "d" : "k");
        }
    }

    public class TurFile
    {
        public string Name;
        public string Descr;
        public string CreatedBy;
        public string ModifiedBy;
        public int NumberofRounds;
        public int CurrentRound;
        public byte TournamentType; // 0 - MacMahon, 1 - Swiss
        public int NumberOfPlayers;
        public int HandicapType; // 131 - only even game, 138 - use handicap below... + MMS difference, 140 - use handicap below... + strangth difference, 146 - use different strategic, 

        public List<TurPlayer> Players = new List<TurPlayer>();
        public List<string> Countries = new List<string>();
        public List<string> Clubs = new List<string>();
        public List<int> Criteria = new List<int>();

        private int handicapBelow;
        private int topMcMahonBarInt;
        private int lowMcMahonBarInt;

        public string HandicapBelowLevel 
        { 
            get { return TurPlayer.IntToRank(this.handicapBelow); } 
        }
        
        public bool HandicapBelow 
        { 
            get { return this.HandicapType != 131; } 
        }

        public string TopMcMahonBarString 
        { 
            get { return TurPlayer.IntToRank(this.topMcMahonBarInt); } 
        }
        
        public string LowMcMahonBarString 
        { 
            get { return TurPlayer.IntToRank(this.lowMcMahonBarInt); } 
        }
        
        public bool UseMacMahon 
        { 
            get { return this.TournamentType == 0; } 
        }

        public bool Load(string fileName)
        {
            bool result = false;
            if (File.Exists(fileName))
            {

                try
                {
                    TurSections currSection = TurSections.Start;
                    using (BinaryReader reader = new BinaryReader(File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Encoding.ASCII))
                    {
                        while (reader.BaseStream.Position < reader.BaseStream.Length)
                        {
                            char ch = reader.ReadChar();

                            if ((byte)ch == 2)
                            {
                                switch (currSection)
                                {
                                    case TurSections.Start:
                                        {
                                            currSection = TurSections.Header;

                                            byte b;
                                            reader.BaseStream.Position += 15;

                                            this.Name = reader.ReadString();
                                            this.Descr = reader.ReadString();
                                            this.CreatedBy = reader.ReadString();
                                            this.ModifiedBy = reader.ReadString();

                                            b = reader.ReadByte();
                                            b = reader.ReadByte();
                                            b = reader.ReadByte();
                                            b = reader.ReadByte();
                                            b = reader.ReadByte();

                                            this.TournamentType = reader.ReadByte(); // 1

                                            b = reader.ReadByte();

                                            this.NumberofRounds = reader.ReadInt16();
                                            this.CurrentRound = reader.ReadInt16();
                                            int p3 = reader.ReadInt16();
                                            this.NumberOfPlayers = reader.ReadInt16();

                                            for (int i = 0; i < this.NumberOfPlayers; i++)
                                            {
                                                reader.BaseStream.Position += 10;

                                                TurPlayer player = new TurPlayer();

                                                player.Surname = this.ReadAsciiString(reader);
                                                player.FirstName = this.ReadAsciiString(reader);

                                                player.CountryId = reader.ReadInt16();
                                                player.ClubId = reader.ReadInt16();

                                                // reading pairing
                                                for (int j = 0; j < 18; j++)
                                                {
                                                    player.RoundInfo[j] = new TurPlayerRoundInfo();

                                                    player.RoundInfo[j].P1_CompetitorId = reader.ReadInt16();
                                                    player.RoundInfo[j].P3_GameResult = reader.ReadByte();
                                                    player.RoundInfo[j].P4_Color = reader.ReadByte();
                                                    player.RoundInfo[j].P5_Handicap = reader.ReadByte();
                                                    player.RoundInfo[j].P6 = reader.ReadByte();
                                                    player.RoundInfo[j].P7_Board = reader.ReadInt16();
                                                }

                                                player.Footer.P01 = reader.ReadByte();
                                                player.Footer.P02 = reader.ReadByte();
                                                player.Footer.P03_RegisterKind = reader.ReadByte();
                                                player.Footer.P04 = reader.ReadByte();
                                                player.Footer.P05_Rank = reader.ReadInt16();
                                                player.Footer.P07 = reader.ReadByte();
                                                player.Footer.P08 = reader.ReadByte();

                                                player.Footer.P09_Bracket = reader.ReadByte(); // ']'

                                                player.Footer.P10_Rating = reader.ReadInt16();
                                                player.Footer.P12_Num = reader.ReadInt16();
                                                player.Footer.P14 = reader.ReadByte();
                                                player.Footer.P15 = reader.ReadByte();

                                                player.Comment = this.ReadAsciiString(reader);

                                                this.Players.Add(player);
                                            }

                                            b = reader.ReadByte();
                                            b = reader.ReadByte();
                                            b = reader.ReadByte();
                                            b = reader.ReadByte();
                                            b = reader.ReadByte();
                                            b = reader.ReadByte();
                                            b = reader.ReadByte();
                                            b = reader.ReadByte();
                                            b = reader.ReadByte();
                                            b = reader.ReadByte();
                                            b = reader.ReadByte();
                                            b = reader.ReadByte();
                                            b = reader.ReadByte();
                                            b = reader.ReadByte();
                                            b = reader.ReadByte();
                                            b = reader.ReadByte();

                                            for (int i = 0; i < 8; i++)
                                            {
                                                int criteria = reader.ReadInt16();
                                                if (criteria > 0)
                                                {
                                                    this.Criteria.Add(criteria);
                                                }
                                            }
                                            b = reader.ReadByte();
                                            b = reader.ReadByte();
                                            b = reader.ReadByte();
                                            b = reader.ReadByte();
                                            b = reader.ReadByte();
                                            b = reader.ReadByte();

                                            this.lowMcMahonBarInt = reader.ReadInt16();
                                            this.topMcMahonBarInt = reader.ReadInt16();

                                            b = reader.ReadByte();
                                            b = reader.ReadByte();
                                            b = reader.ReadByte();
                                            b = reader.ReadByte();
                                            b = reader.ReadByte();
                                            b = reader.ReadByte();
                                            b = reader.ReadByte();
                                            b = reader.ReadByte();
                                            b = reader.ReadByte();
                                            b = reader.ReadByte();
                                            b = reader.ReadByte();
                                            b = reader.ReadByte();

                                            this.HandicapType = reader.ReadInt16();
                                            reader.ReadInt16();
                                            this.handicapBelow = reader.ReadInt16();

                                            long pos = reader.BaseStream.Position;

                                            reader.BaseStream.Position = reader.BaseStream.Length - 4;
                                            int counter = 0;
                                            do
                                            {
                                                char[] chars = reader.ReadChars(4);
                                                if (chars.Length == 4 && chars[0] == '_' && chars[1] == 'e' && chars[2] == 'n' && chars[3] == 'd')
                                                    counter++;
                                                if (counter == 2)
                                                {
                                                    reader.BaseStream.Position -= 5;
                                                    do
                                                    {
                                                        byte p = reader.ReadByte();
                                                        if (p == 0)
                                                            break;

                                                        reader.BaseStream.Position -= 2;
                                                    } 
                                                    while (reader.BaseStream.Position >= 0);
                                                    break;
                                                }
                                                reader.BaseStream.Position -= 5;
                                            } 
                                            while (reader.BaseStream.Position >= 0);

                                            string s = string.Empty;
                                            do
                                            {
                                                s = this.ReadAsciiString(reader);
                                                if (s != "_end")
                                                {
                                                    this.Countries.Add(s);
                                                }
                                                else
                                                {
                                                    break;
                                                }
                                            }
                                            while (reader.BaseStream.Position < reader.BaseStream.Length);

                                            do
                                            {
                                                s = this.ReadAsciiString(reader);
                                                if (s != "_end")
                                                {
                                                    this.Clubs.Add(s);
                                                }
                                                else
                                                {
                                                    break;
                                                }
                                            }
                                            while (reader.BaseStream.Position < reader.BaseStream.Length);

                                            break;
                                        }
                                    case TurSections.Header:
                                        currSection = TurSections.Player;
                                        break;
                                }
                            }
                        }
                    }

                    result = true;
                }
                catch 
                { 
                }

            }
            return result;
        }

        private string ReadAsciiString(BinaryReader reader)
        {
            byte b = reader.ReadByte();
            byte[] bt = new byte[b];
            for (byte z = 0; z < b; z++)
            {
                byte c = reader.ReadByte();
                bt[z] = c;
            }
            var encoding = Encoding.GetEncoding(1251);
            var coding = new System.Text.UTF8Encoding(false);

            var utf = Encoding.Convert(encoding, coding, bt);

            // Convert the new byte[] into a char[] and then into a string.
            char[] utfChars = new char[coding.GetCharCount(utf, 0, utf.Length)];
            coding.GetChars(utf, 0, utf.Length, utfChars, 0);
            return new string(utfChars);
        }
    }
}
