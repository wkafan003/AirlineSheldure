using System;
using System.Collections.Generic;
using System.Text;

namespace AirlineSheldure
{
	class DateTimeHelper
	{
		public static DateTime Today
		{
			get { return DateTime.Today; }
		}
		public static DateTime TodayPlusHour
		{
			get { return DateTime.Today.AddHours(1); }
		}
	}
}
