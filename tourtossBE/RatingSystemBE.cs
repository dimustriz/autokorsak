namespace Tourtoss.BE
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;

    [Serializable]
    public class RatingSystem
    {
        public RatingSystem()
        {
            this.Persons = new List<Person>();
            this.Ratings = new List<RatingRec>();
            this.Clubs = new List<Club>();
        }
        
        [XmlAttribute(AttributeName = "kind")]
        public RtKind Kind { get; set; }
        [XmlAttribute(AttributeName = "date")]
        public string Date { get; set; }

        [XmlElement(ElementName = "c")]
        public List<Club> Clubs { get; set; }
        [XmlElement(ElementName = "p")]
        public List<Person> Persons { get; set; }
        [XmlElement(ElementName = "r")]
        public List<RatingRec> Ratings { get; set; }

        [Serializable]
        public class Club
        {
            [XmlAttribute(AttributeName = "n")]
            public string Name { get; set; }
            [XmlAttribute(AttributeName = "i")]
            public int Id { get; set; }

            [XmlAttribute(AttributeName = "nu")]
            public string NameUa { get; set; }

            [XmlAttribute(AttributeName = "ne")]
            public string NameEn { get; set; }

            [XmlAttribute(AttributeName = "c")]
            public string EGDName { get; set; }
        }

        [Serializable]
        public class Person
        {
            [XmlAttribute(AttributeName = "l")]
            public string LastName { get; set; }
            [XmlAttribute(AttributeName = "f")]
            public string FirstName { get; set; }
            [XmlAttribute(AttributeName = "i")]
            public int Id { get; set; }
            [XmlAttribute(AttributeName = "c")]
            public int ClubId { get; set; }
            [XmlAttribute(AttributeName = "r")]
            public int Rating { get; set; }

            [XmlAttribute(AttributeName = "rk")]
            public string Rank { get; set; }
            [XmlAttribute(AttributeName = "gr")]
            public int Grade { get; set; }
            [XmlAttribute(AttributeName = "cm")]
            public string Comment { get; set; }

            [XmlAttribute(AttributeName = "lu")]
            public string LastNameUa { get; set; }
            [XmlAttribute(AttributeName = "fu")]
            public string FirstNameUa { get; set; }
            [XmlAttribute(AttributeName = "le")]
            public string LastNameEn { get; set; }
            [XmlAttribute(AttributeName = "fe")]
            public string FirstNameEn { get; set; }

            [XmlIgnore]
            public List<Club> ClubsLink { get; set; }

            [XmlIgnore]
            public string Name
            {
                get
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(this.LastName).Append(", ").Append(this.FirstName);

                    return sb.ToString();
                }
            }

            [XmlIgnore]
            public string NameUa
            {
                get
                {
                    if (string.IsNullOrEmpty(this.FirstNameUa) && string.IsNullOrEmpty(this.LastNameUa))
                    {
                        return this.Name;
                    }

                    StringBuilder sb = new StringBuilder();
                    sb.Append(this.LastNameUa).Append(", ").Append(this.FirstNameUa);

                    return sb.ToString();
                }
            }

            [XmlIgnore]
            public string NameEn
            {
                get
                {
                    if (string.IsNullOrEmpty(this.FirstNameEn) && string.IsNullOrEmpty(this.LastNameEn))
                    {
                        return this.Name;
                    }

                    StringBuilder sb = new StringBuilder();
                    sb.Append(this.LastNameEn).Append(", ").Append(this.FirstNameEn);

                    return sb.ToString();
                }
            }

            [XmlIgnore]
            public string DisplayName
            {
                get
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(this.LastName).Append(" ").Append(this.FirstName);

                    Club club = this.GetClub(this.ClubId);
                    
                    if (club != null)
                    {
                        sb.Append(", ").Append(club.Name);
                    }

                    sb.Append(" (").Append(PlayerInfo.GetRankFromRating(this.Rating)).Append(")");

                    return sb.ToString();
                }
            }

            [XmlIgnore]
            public string DisplayNameUa
            {
                get
                {
                    if (string.IsNullOrEmpty(this.FirstNameUa) && string.IsNullOrEmpty(this.LastNameUa))
                    {
                        return this.DisplayName;
                    }

                    StringBuilder sb = new StringBuilder();
                    sb.Append(this.LastNameUa).Append(" ").Append(this.FirstNameUa);

                    Club club = this.GetClub(this.ClubId);
                    
                    if (club != null)
                    {
                        sb.Append(", ");
                        sb.Append(!string.IsNullOrEmpty(club.NameUa) ? club.NameUa : club.Name);
                    }

                    sb.Append(" (").Append(PlayerInfo.GetRankFromRating(this.Rating)).Append(")");

                    return sb.ToString();
                }
            }

            [XmlIgnore]
            public string DisplayNameEn
            {
                get
                {
                    if (string.IsNullOrEmpty(this.FirstNameEn) && string.IsNullOrEmpty(this.LastNameEn))
                    {
                        return this.DisplayName;
                    }

                    StringBuilder sb = new StringBuilder();
                    sb.Append(this.LastNameEn).Append(" ").Append(this.FirstNameEn);

                    Club club = this.GetClub(this.ClubId);
                    
                    if (club != null)
                    {
                        sb.Append(", ");
                        sb.Append(!string.IsNullOrEmpty(club.NameEn) ? club.NameEn : club.Name);
                    }

                    sb.Append(" (").Append(PlayerInfo.GetRankFromRating(this.Rating)).Append(")");

                    return sb.ToString();
                }
            }

            private Club GetClub(int id)
            {
                return this.ClubsLink != null ? this.ClubsLink.Find(p => p.Id == id) : null;
            }
        }

        [Serializable]
        public class RatingRec
        {
            [XmlAttribute(AttributeName = "p")]
            public int PersonId { get; set; }
            [XmlIgnore]
            public string DateStr
            {
                get
                {
                    return
                        this.Date.Year + "-" +
                        (this.Date.Month < 10 ? "0" : string.Empty) + this.Date.Month + "-" +
                        (this.Date.Day < 10 ? "0" : string.Empty) + this.Date.Day;
                }
            }

            [XmlAttribute(AttributeName = "d", DataType = "date")]
            public DateTime Date { get; set; }
            [XmlAttribute(AttributeName = "r")]
            public double Rating { get; set; }
            [XmlIgnore]
            public bool IsAbnormal { get; set; }
        }
    }
}
