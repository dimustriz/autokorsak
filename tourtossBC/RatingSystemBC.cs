using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Globalization;
using System.Threading;

using Tourtoss.BE;
using Tourtoss.DL;

namespace Tourtoss.BC
{
    public class RatingSystemBC: BaseBC<RatingSystemDL>
    {
        public RatingSystem Load(RtKind kind)
        {
            return GetDL().Load(kind);
        }

        public bool Save(RatingSystem rs)
        {
            return GetDL().Save(rs);
        }

        public RatingSystem ImportFromRatingLists(RtKind kind)
        {
            RatingSystem result = new RatingSystem();
            result.Kind = kind;

            DateTime maxDate = DateTime.MinValue;

            var rlBc = new RatingListBC();

            foreach (string dir in System.IO.Directory.EnumerateDirectories(RatingListDL.LocalDataFolder))
            {
                string fileName = dir + "\\" + System.IO.Path.GetFileName(rlBc.GetFileName(kind));
                var lst = rlBc.LoadRatingList(kind, fileName);
                if (lst != null)
                {
                    foreach (var item in lst.Items)
                    {
                        string fn = item.FirstName == null ? string.Empty : item.FirstName.Trim();
                        string ln = item.LastName == null ? string.Empty : item.LastName.Trim();
                        var person = result.Persons.Find(fnd => fnd != null && item != null &&
                            string.Compare(fnd.LastName, ln, true) == 0 && string.Compare(fnd.FirstName, fn, true) == 0);
                        if (person == null)
                        {
                            person = new RatingSystem.Person();
                            person.Id = result.Persons.Count + 1;
                            person.FirstName = fn;
                            person.LastName = ln;
                            result.Persons.Add(person);
                        }

                        person.Rating = item.Rating;
                        person.Rank = item.Rank;
                        person.Grade = Grade.Parse(item.Grade);
                        person.Comment = item.Comment;

                        if (!string.IsNullOrEmpty(item.City))
                        {
                            string nm = item.City == null ? string.Empty : item.City.Trim();
                            if (!string.IsNullOrEmpty(nm))
                            {
                                var club = result.Clubs.Find(fnd => string.Compare(fnd.Name, nm, true) == 0);
                                if (club == null)
                                {
                                    club = new RatingSystem.Club();
                                    club.Id = result.Clubs.Count + 1;
                                    club.Name = nm;
                                    result.Clubs.Add(club);
                                }
                                person.ClubId = club.Id;
                            }
                        }

                        string date = System.IO.Path.GetFileName(dir);
                        int y = 0;
                        int m = 0;
                        int d = 0;
                        if (int.TryParse(date.Substring(0, 4), out y) &&
                            int.TryParse(date.Substring(5, 2), out m) &&
                            int.TryParse(date.Substring(8, 2), out d))
                        {
                            DateTime dt = new DateTime(y, m, d);
                            var prev = result.Ratings.Find(fnd => fnd.PersonId == person.Id);
                            if (!(prev != null && prev.Rating == item.Rating))
                                result.Ratings.Insert(0, new RatingSystem.RatingRec() { Date = dt, Rating = item.Rating, PersonId = person.Id });

                            if (maxDate.CompareTo(dt) < 0)
                                maxDate = dt;
                        }
                    }
                }
            }

            result.Clubs.Sort(delegate(RatingSystem.Club item1, RatingSystem.Club item2) { return string.Compare(item1.Name, item2.Name); });
            result.Persons.Sort(delegate(RatingSystem.Person item1, RatingSystem.Person item2) 
                {
                    int r = string.Compare(item1.LastName, item2.LastName);
                    if (r == 0)
                        r = string.Compare(item1.FirstName, item2.FirstName);
                    return r;
                });
            result.Ratings.Sort(delegate(RatingSystem.RatingRec item1, RatingSystem.RatingRec item2) 
                { 
                    int r = item1.PersonId - item2.PersonId; 
                    if (r == 0)
                        r = string.Compare(item2.DateStr, item1.DateStr, true);
                    return r;
                });

            result.Date = maxDate.Day + "." + (maxDate.Month < 10 ? "0" : string.Empty) + maxDate.Month + "." + maxDate.Year;

            Save(result);
            return result;
        }

        public RatingSystem MergeWithRatingList(RtKind kind)
        {
            RatingSystem result = Load(kind);
            
            var rlBc = new RatingListBC();

            string fileName = rlBc.GetFileName(kind);
            var lst = rlBc.LoadRatingList(kind, fileName);
            if (lst != null)
            {
                foreach (var item in lst.Items)
                {
                    string fn = item.FirstName == null ? string.Empty : item.FirstName.Trim();
                    string ln = item.LastName == null ? string.Empty : item.LastName.Trim();
                    
                    //Find in default and Ua langs
                    var person = result.Persons.Find(fnd => fnd != null && item != null &&
                        (
                            (string.Compare(fnd.LastName, ln, true) == 0 && string.Compare(fnd.FirstName, fn, true) == 0) ||
                            (string.Compare(fnd.LastNameUa, ln, true) == 0 && string.Compare(fnd.FirstNameUa, fn, true) == 0)   
                        ));

                    if (person == null)
                    {
                        person = new RatingSystem.Person();
                        person.Id = result.Persons.Count + 1;
                        
                        person.FirstName = fn;
                        person.LastName = ln;
                        person.FirstNameUa = item.FirstNameUa;
                        person.LastNameUa = item.LastNameUa;

                        result.Persons.Add(person);
                    }
                    else
                    { 
                        //Update Names
                        if (string.Compare(person.LastName, ln, true) == 0 && string.Compare(person.FirstName, fn, true) == 0)
                        {
                            if (string.IsNullOrEmpty(person.FirstNameUa) && !string.IsNullOrEmpty(item.FirstNameUa))
                                person.FirstNameUa = item.FirstNameUa;
                            if (string.IsNullOrEmpty(person.LastNameUa) && !string.IsNullOrEmpty(item.LastNameUa))
                                person.LastNameUa = item.LastNameUa;
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(person.FirstName) && !string.IsNullOrEmpty(item.FirstName))
                                person.FirstName = item.FirstName;
                            if (string.IsNullOrEmpty(person.FirstName) && !string.IsNullOrEmpty(item.LastName))
                                person.LastName = item.LastName;
                        }
                    }

                    person.Rating = item.Rating;
                    person.Rank = item.Rank;
                    person.Grade = Grade.Parse(item.Grade);
                    person.Comment = item.Comment;

                    if (!string.IsNullOrEmpty(item.City))
                    {
                        string nm = item.City == null ? string.Empty : item.City.Trim();
                        string nmUa = item.CityUa == null ? string.Empty : item.CityUa.Trim();
                        if (!string.IsNullOrEmpty(nm))
                        {
                            var club = result.Clubs.Find(fnd =>
                                string.Compare(fnd.Name, nm, true) == 0 || 
                                string.Compare(fnd.NameUa, nm, true) == 0);

                            if (club == null)
                            {
                                club = new RatingSystem.Club();
                                club.Id = result.Clubs.Count + 1;

                                club.Name = nm;
                                club.NameUa = nmUa;

                                result.Clubs.Add(club);
                            }
                            else
                            {
                                //Update Names
                                if (string.Compare(club.Name, nm, true) == 0)
                                {
                                    if (string.IsNullOrEmpty(club.NameUa) && !string.IsNullOrEmpty(nmUa))
                                        club.NameUa = nmUa;
                                }
                                else
                                {
                                    if (string.IsNullOrEmpty(club.Name) && !string.IsNullOrEmpty(nm))
                                        club.Name = nm;
                                }
                            }

                            person.ClubId = club.Id;
                        }
                    }

                    string date = !string.IsNullOrEmpty(item.Date) ? item.Date : lst.Date;
                    if (!string.IsNullOrEmpty(date))
                    {
                        string[] arr = date.Split('.');
                        if (arr.Length > 2)
                        {
                            int y = 0;
                            int m = 0;
                            int d = 0;
                            if (int.TryParse(arr[2], out y) &&
                                int.TryParse(arr[1], out m) &&
                                int.TryParse(arr[0], out d))
                            {
                                var prev = result.Ratings.Find(fnd => fnd.PersonId == person.Id);
                                DateTime dt = new DateTime(y, m, d);
                                if (!(prev != null && prev.Rating == item.Rating))
                                    if (result.Ratings.Find(fnd => fnd.PersonId == person.Id && fnd.Date == dt) == null)
                                    {
                                        result.Ratings.Add(new RatingSystem.RatingRec() { Date = dt, Rating = item.Rating, PersonId = person.Id });
                                        result.Date = lst.Date;
                                    }
                            }
                        }
                    }
                }
            }

            result.Clubs.Sort(delegate(RatingSystem.Club item1, RatingSystem.Club item2) { return string.Compare(item1.Name, item2.Name); });
            result.Persons.Sort(delegate(RatingSystem.Person item1, RatingSystem.Person item2)
            {
                int r = string.Compare(item1.LastName, item2.LastName);
                if (r == 0)
                    r = string.Compare(item1.FirstName, item2.FirstName);
                return r;
            });
            result.Ratings.Sort(delegate(RatingSystem.RatingRec item1, RatingSystem.RatingRec item2)
            {
                int r = item1.PersonId - item2.PersonId;
                if (r == 0)
                    r = string.Compare(item2.DateStr, item1.DateStr, true);
                return r;
            });

            Save(result);
            return result;
        }

        public RatingSystem ImportRatingSystem(RtKind kind)
        {
            return GetDL().ImportRatingSystem(kind);
        }
    }

}
