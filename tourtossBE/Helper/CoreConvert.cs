// -----------------------------------------------------------------------
// <copyright file="Class1.cs" company="Infopulse">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Tourtoss.BE.Helper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
	public class CoreConvert
	{
		private static string _emptyString = string.Empty;
		private static DateTime _emptyDate = DateTime.MinValue;
        private static int _currentCentury = 21;

		#region empty values

		public static string EmptyString
		{
			get
			{
				return _emptyString;
			}
			set
			{
				_emptyString = value;
			}
		}

        public static DateTime EmptyDate
		{
			get
			{
				return _emptyDate;
			}
			set
			{
				_emptyDate = value;
			}
		}
		
        #endregion

		public static bool IsEmpty(object value1)
		{
			if (value1 == null)
			{
				return true;
			}

			if (value1.GetType().Equals(typeof(string)))
			{
				return EmptyString.Equals((string)value1);
			}

			if (value1.GetType().Equals(typeof(DateTime)))
			{
				return EmptyDate.Equals((DateTime)value1);
			}

            if (value1.GetType().Equals(typeof(byte[])))
            {
                return ((byte[])value1).Length == 0;
            }

			return false;
		}

        public static DateTime ToDateTime(string dateStr)
		{
			DateTime date = CoreConvert.EmptyDate;
            if (IsEmpty(dateStr))
			{
				return date;
			}
			//
            bool isPM = dateStr.ToUpper().IndexOf("PM") > -1;
            dateStr = dateStr.Replace("AM", "").Replace("PM", "").Trim();
			//
			int hour = -1;
			int minute = -1;
            string[] arr = dateStr.Split(new Char[] { '/', '-', '.', ' ', ':' });
			if (arr.Length >= 5)
			{
				hour   = Convert.ToInt32(arr[3]);
				minute = Convert.ToInt32(arr[4]);
			}
			else if (arr.Length == 4)
			{
				hour   = Convert.ToInt32(arr[3]);
				minute = 0;
			}
			if (isPM)
			{
				hour += 12;
			}
			if (hour > 0)
			{
				try
				{
                    int _year = Convert.ToInt32(arr[2]);
                    if (_year < 100) _year += _currentCentury * 100;
                    date = new DateTime(_year, Convert.ToInt32(arr[1]), Convert.ToInt32(arr[0]), hour, minute, 0);
				}
				catch
				{
				}
			}
			else
			{
				date = ToDate(dateStr);
			}
			//
			return date;
		}

		public static DateTime ToTime(string dateStr)
		{
			DateTime date = CoreConvert.EmptyDate;
            if (IsEmpty(dateStr))
			{
				return date;
			}
			//
            bool isPM = dateStr.ToUpper().IndexOf("PM") > -1;
            dateStr = dateStr.Replace("AM", "").Replace("PM", "").Trim();
			//
			int hour = -1;
			int minute = -1;
            string[] arr = dateStr.Split(new Char[] { '-', '.', ' ', ':' });
			if (arr.Length >= 2)
			{
				hour   = Convert.ToInt32(arr[0]);
				minute = Convert.ToInt32(arr[1]);
			}
			else if (arr.Length == 1)
			{
				hour   = Convert.ToInt32(arr[0]);
				minute = 0;
			}
			if (isPM)
			{
				hour += 12;
			}
			if (hour > 0)
			{
				try
				{
					date = new DateTime(CoreConvert.EmptyDate.Year, CoreConvert.EmptyDate.Month, CoreConvert.EmptyDate.Day, hour, minute, 0);
				}
				catch
				{
				}
			}
			else
			{
				date = CoreConvert.EmptyDate;
			}
			//
			return date;
		}

		public static DateTime ToDate(string dateStr)
		{
			DateTime date = CoreConvert.EmptyDate;
            if (IsEmpty(dateStr))
			{
				return date;
			}
			//
            dateStr = dateStr.Trim();
			//
            string[] arr = dateStr.Split(new Char[] { '/', '-', '.', ' ' });
			if (arr.Length >= 3)
			{
				try
				{
                    int _year = Convert.ToInt32(arr[2]);
                    if (_year < 100) _year += _currentCentury * 100;
                    date = new DateTime(_year, Convert.ToInt32(arr[1]), Convert.ToInt32(arr[0]));
				}
				catch
				{
				}
			}
			//
			return date;
		}

        public static string ToDateString(DateTime date)
        {
            return !IsEmpty(date) ? date.Date.ToString("dd.MM.yyyy") : string.Empty;
        }
    }
}
