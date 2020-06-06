using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Google.OrTools.LinearSolver;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Model;
using Xceed;

namespace AirlineSheldure
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private static ApplicationContext _db;
		private static Thread _backgroundWorker;
		public MainWindow()
		{
			InitializeComponent();
			for (int i = 0; i < 15; i++)
			{
				var b = MakeBorder($"{i}", 35, 40, 100 * i, 100, Color.FromArgb(10 * 255 / 100, 197, 39, 39));
				MainCanvas.Children.Add(b);
			}

			MainCanvas.Width = 14 * 100 + 35;


			_db = new ApplicationContext();
			foreach (var flight in _db.Flights.AsEnumerable().Where(f => f.EndTime.TimeOfDay == TimeSpan.Zero))
			{
				flight.EndTime += TimeSpan.FromDays(1);
			}

			//Datagrid1.DataContext = db.Flights.Local.ToObservableCollection();
			//Datagrid1.ItemsSource = _db.Flights.Local.ToObservableCollection();
			//_db.Airplanes.Add(new Airplane() {Capacity = 200, Cost = 2600, Count = 4, Name = "Кекс летающий"});
			//_db.Airplanes.RemoveRange(_db.Airplanes);
			//_db.Flights.RemoveRange(_db.Flights);
			//_db.SaveChanges();

			//AirlineRostering();
			//_db.SaveChanges();
			//CrewRostering();
			_db.SaveChanges();

			Console.WriteLine();
		}

		public static Border MakeBorder(string text, int width, int height, double x, double y, Color background)
		{
			Border b = new Border()
			{
				Width = width,
				Height = height,
				Background = new SolidColorBrush(background),
				BorderThickness = new Thickness(1),
				BorderBrush = new SolidColorBrush(Colors.Black),
			};
			TextBlock tb = new TextBlock()
			{
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Text = text,
			};
			b.ToolTip = "Ожидание";
			b.Child = tb;
			b.SetValue(Canvas.LeftProperty, x);
			b.SetValue(Canvas.TopProperty, y);
			return b;
		}

		public static int AirlineRostering()
		{
			TimeSpan minTurnTime = _db.TurnTimes.Min(i => i.Time);

			bool[,] PiAirline;
			double[,] cAirline;
			bool[,] teta;

			Flight[] fSort;

			List<int[]> pairsAirline;
			bool[][] pairs_flag;
			//MakeTurnTimes
			TimeSpan[,] turnTimes = new TimeSpan[_db.Airports.Count(), _db.Airplanes.Count()];
			Airport[] airports = _db.Airports.OrderBy(i => i.Id).ToArray();
			Airplane[] airplanes = _db.Airplanes.OrderBy(i => i.Id).ToArray();
			for (var i = 0; i < airports.Length; i++)
			{
				var buf = _db.TurnTimes.Where(t => t.AirportId == airports[i].Id).OrderBy(t => t.AirplaneId).ToArray();
				for (int j = 0; j < buf.Length; j++)
				{
					turnTimes[i, j] = buf[j].Time;
				}
			}

			int dayBufFlight = 1;
			var dailyFlights = (_db.Flights.AsEnumerable() ?? throw new Exception("Список полетов пуст!."))
				.GroupBy(i => i.StartTime.DayOfYear).ToArray();

			for (int day = 0; day < dailyFlights.Length; day += dayBufFlight)
			{
				//f_sort = db.Flights.Where(f=>f.StartTime ==DateTime.Now).OrderBy(f => f.StartTime).ToArray();
				IEnumerable<Flight> bufEnumerable = dailyFlights[day];
				for (int i = 1; (i < dayBufFlight) & (day + i < dailyFlights.Length); i++)
				{
					bufEnumerable = bufEnumerable.Concat(dailyFlights[day + i]);
				}

				//fSort = dailyFlights[day].OrderBy(f => f.StartTime).ToArray();
				fSort = bufEnumerable.OrderBy(f => f.StartTime).ToArray();
				pairsAirline = new List<int[]>();
				try
				{
					MakePairsAirline(fSort, pairsAirline, new List<int>(), minTurnTime);
				}
				catch
				{
					return 1;
				}
				MakePiAndCAndTetaAirline(airports, airplanes, turnTimes, fSort, pairsAirline, out PiAirline,
					out cAirline,
					out teta);

				
				pairs_flag = MipSolverAirline(PiAirline, cAirline, teta, airplanes.Select(a => a.Count).ToArray());
				if (pairs_flag == null)
				{
					return 2;
				}
				List<int> coversFlight = new List<int>();
				for (int i = 0; i < pairs_flag[0].Length; i++)
				{
					for (int j = 0; j < airplanes.Length; j++)
					{
						if (pairs_flag[j][i])
						{
							coversFlight.AddRange(pairsAirline[i]);
							AirplanePair p = new AirplanePair();
							foreach (var num in pairsAirline[i])
							{
								p.Flights.Add(fSort[num]);
								fSort[num].Airplane = airplanes[j];
							}

							p.Airplane = airplanes[j];
							_db.AirplanePairs.Add(p);
							break;
						}
					}
				}
			}

			//int[] crewCounts = new[] { 15, 24, 18, 1 };
			//crewCounts = crewCounts.Select(c => c * 5).ToArray();
			//for (int j = 0; j < airports.Length; j++)
			//{
			//	for (int i = 0; i < crewCounts[j]; i++)
			//	{
			//		Crewmember pilot = new Crewmember()
			//		{
			//			BaseId = airports[j].Id,
			//			FirstName = $"Иван {i}_{j}",
			//			SecondName = "Иванов",
			//			LastName = "Иванович"
			//		};
			//		Roster r = new Roster();
			//		//r.Actions.Add(new Model.Action() { ActionTypeId = (int)ActionEnum.Holiday, StartTime = DateTime.Now, EndTime = DateTime.Now.AddDays(1)});
			//		pilot.Roster = r;
			//		for (int k = 0; k < airplanes.Length; k++)
			//		{
			//			Permission p = new Permission()
			//			{ AirplaneId = airplanes[k].Id, FirstPilot = true, SecondPilot = true };
			//			pilot.Permissions.Add(p);
			//			_db.Permissions.Add(p);
			//		}

			//		_db.Rosters.Add(r);
			//		_db.Crewmembers.Add(pilot);
			//	}
			//}

			//db.SaveChanges();
			return 0;
		}

		public static void CrewRostering()
		{
			Airport[] airports = _db.Airports.OrderBy(i => i.Id).ToArray();
			Airplane[] airplanes = _db.Airplanes.OrderBy(i => i.Id).ToArray();
			//fSort = db.Flights.OrderBy(f => f.StartTime).ToArray();
			int dayBufCrew = 10;
			var dailyCrew = (_db.Flights.AsEnumerable() ?? throw new Exception("Список полетов пуст!."))
				.GroupBy(i => i.StartTime.DayOfYear).ToArray();
			Flight[] fSort;
			for (int day = 0; day < dailyCrew.Length; day += dayBufCrew)
			{
				//f_sort = db.Flights.Where(f=>f.StartTime ==DateTime.Now).OrderBy(f => f.StartTime).ToArray();
				IEnumerable<Flight> bufEnumerable = dailyCrew[day];
				for (int i = 1; (i < dayBufCrew) & (day + i < dailyCrew.Length); i++)
				{
					bufEnumerable = bufEnumerable.Concat(dailyCrew[day + i]);
				}

				//fSort = dailyFlights[day].OrderBy(f => f.StartTime).ToArray();
				fSort = bufEnumerable.OrderBy(f => f.StartTime).ToArray();
				List<int[]> duties = new List<int[]>();
				double minSit = 0.5;
				double maxSit = 4;
				double maxElapse = 12;
				double maxFly = 8;
				MakeDutiesCrew(minSit, maxSit, maxElapse, maxFly, fSort, duties, new List<int>(fSort.Length));
				duties = duties.OrderBy(i => fSort[i.First()].StartTime).ToList();

				List<int[][]> pairsCrew = new List<int[][]>();
				int maxDuties = 3;
				double minRest = 12;
				double maxTAFB = 24 * 4;
				MakePairsCrew(maxDuties, minRest, maxTAFB, fSort, duties, pairsCrew, new List<int[]>(maxDuties));

				bool[,] Pi;
				double[] c;
				double mg1 = 3;
				double f1 = 4 / 7;
				double mg2 = 4.75;
				double f2 = 2 / 7;
				MakePiAndCCrew(fSort, pairsCrew, out Pi, out c, mg1, f1, mg2, f2);

				bool[] pairCrewFlags = MipSolverCrew(Pi, c);
				for (int i = 0; i < pairsCrew.Count; i++)
				{
					if (pairCrewFlags[i])
					{
						CrewmemberPair pairCrew = new CrewmemberPair();
						for (int j = 0; j < pairsCrew[i].Length; j++)
						{
							CrewmemberDuty duty = new CrewmemberDuty();
							foreach (Flight flight in pairsCrew[i][j].Select(i => fSort[i]))
							{
								duty.Flights.Add(flight);
							}

							//duty.Flights.AddRange(pairsCrew[i][j].Select(i => fSort[i]));
							duty.AirplaneId = fSort[pairsCrew[i][j].First()].AirplaneId;
							duty.StartTime = duty.Flights.Min(f => f.StartTime);
							duty.EndTime = duty.Flights.Max(f => f.EndTime);
							duty.ElapseTime = duty.Flights.Last().EndTime - duty.Flights.First().StartTime;
							duty.FlyTime = TimeSpan.Zero;

							foreach (var flight in duty.Flights)
							{
								duty.FlyTime += flight.EndTime - flight.StartTime;
							}

							pairCrew.CrewmemberDuties.Add(duty);
							_db.CrewmemberDuties.Add(duty);
						}

						pairCrew.ElapseTime = pairCrew.CrewmemberDuties.Last().Flights.Last().EndTime -
											  pairCrew.CrewmemberDuties.First().Flights.First().StartTime;
						pairCrew.FlyTime = TimeSpan.Zero;
						foreach (var fly in pairCrew.CrewmemberDuties.Select(d => d.FlyTime))
						{
							pairCrew.FlyTime += fly;
						}

						pairCrew.StartTime = pairCrew.CrewmemberDuties.Min(f => f.StartTime);
						pairCrew.EndTime = pairCrew.CrewmemberDuties.Max(f => f.EndTime);
						pairCrew.AirplaneId = pairCrew.CrewmemberDuties.First().AirplaneId;
						_db.CrewmemberPairs.Add(pairCrew);
					}
				}

				_db.SaveChanges();
				// List<int> hashSet = new List<int>();
				// for (int i = 0; i < pairsCrew.Count; i++)
				// {
				//     if (pairCrewFlags[i])
				//     {
				//         for (int j = 0; j < pairsCrew[i].Length; j++)
				//         {
				//             hashSet.AddRange(pairsCrew[i][j]);
				//         }
				//     }
				// }

				//db.SaveChanges();
				foreach (Airplane airplane in airplanes)
				{
					var keks = _db.CrewmemberPairs.Include(p => p.CrewmemberDuties)
						.Where(p => p.AirplaneId == airplane.Id);
					Console.WriteLine(airplane.Name);
					Console.WriteLine(string.Join('\n', keks.Select(k => k.ToStringPair())));
				}


				TimeSpan maxFlyWeek = TimeSpan.FromHours(80);
				for (int i = 0; i < airports.Length; i++)
				{
					var pairsCrewBase = _db.CrewmemberPairs
						.Where(p => p.CrewmemberDuties.First().Flights.First().FromId == airports[i].Id)
						.OrderBy(p => p.CrewmemberDuties.First().Flights.First().StartTime).ToList();
					var crews = _db.Crewmembers.Include(c => c.Permissions).Include(c => c.Roster)
						.Where(c => c.BaseId == airports[i].Id).OrderBy(c => c.Id).ToArray();

					for (int j = 0; j < crews.Length; j++)
					{
						for (int k = 0; k < pairsCrewBase.Count; k++)
						{
							var permission = crews[j].Permissions
								.FirstOrDefault(p => p.Airplane.Id == pairsCrewBase[k].AirplaneId);
							var pairBase = pairsCrewBase[k];
							if (permission != null & permission.FirstPilot &
								(crews[j].Roster.FlyTime + pairBase.FlyTime) < maxFlyWeek)
							{
								bool valid = IsRosterValid(crews[j], pairBase.StartTime, pairBase.EndTime);

								if (valid)
								{
									CrewmemberAddPairToRoster(_db, crews[j], pairBase, true);
									pairBase.CrewmemberFirstId = crews[j].Id;
									pairsCrewBase.Remove(pairBase);
									k--;
								}
							}
						}
					}

					if (pairsCrewBase.Count > 0)
					{
						throw new Exception("Невозможно покрыть связку!");
					}
				}

				for (int i = 0; i < airports.Length; i++)
				{
					var pairsCrewBase = _db.CrewmemberPairs
						.Where(p => p.CrewmemberDuties.First().Flights.First().FromId == airports[i].Id)
						.OrderBy(p => p.CrewmemberDuties.First().Flights.First().StartTime).ToList();
					var crews = _db.Crewmembers.Where(c => c.BaseId == airports[i].Id).OrderBy(c => c.Id).ToArray();

					for (int j = 0; j < crews.Length; j++)
					{
						for (int k = 0; k < pairsCrewBase.Count; k++)
						{
							var permission = crews[j].Permissions
								.FirstOrDefault(p => p.Airplane.Id == pairsCrewBase[k].AirplaneId);
							var pairBase = pairsCrewBase[k];
							if (permission != null & permission.SecondPilot &
								(crews[j].Roster.FlyTime + pairBase.FlyTime) < maxFlyWeek)
							{
								bool valid = IsRosterValid(crews[j], pairBase.StartTime, pairBase.EndTime);

								if (valid)
								{
									CrewmemberAddPairToRoster(_db, crews[j], pairBase, false);
									pairBase.CrewmemberSecondId = crews[j].Id;
									pairsCrewBase.Remove(pairBase);
									k--;
								}
							}
						}
					}

					if (pairsCrewBase.Count > 0)
					{
						throw new Exception("Невозможно покрыть связку!");
					}
				}
			}


			//db.SaveChanges();
		}

		public static bool IsRosterValid(Crewmember crewmember, DateTime startTime, DateTime EndTime)
		{
			var actions = crewmember.Roster.Actions.OrderBy(a => a.StartTime).ToList();
			if (actions.Count == 0)
				return true;
			if ((actions.First().StartTime - EndTime).TotalHours > 12)
				return true;

			for (int i = 0; i < actions.Count - 1; i++)
			{
				if ((startTime - actions[i].EndTime).TotalHours > 12 &
					(actions[i + 1].StartTime - EndTime).TotalHours > 12)
				{
					return true;
				}
			}

			if ((startTime - actions.Last().EndTime).TotalHours > 12)
				return true;
			return false;
		}

		public static void CrewmemberAddPairToRoster(ApplicationContext db, Crewmember crewmember, CrewmemberPair pair,
			bool isFirstPilot)
		{
			if (isFirstPilot)
			{
				pair.CrewmemberFirstId = crewmember.Id;
			}
			else
			{
				pair.CrewmemberSecondId = crewmember.Id;
			}

			Model.Action a;
			crewmember.Roster.FlyTime += pair.FlyTime;
			crewmember.Roster.ElapseTime += pair.ElapseTime;
			for (int i = 0; i < pair.CrewmemberDuties.Count; i++)
			{
				for (int j = 0; j < pair.CrewmemberDuties[i].Flights.Count; j++)
				{
					a = new Model.Action
					{
						StartTime = pair.CrewmemberDuties[i].Flights[j].StartTime,
						EndTime = pair.CrewmemberDuties[i].Flights[j].EndTime,
						ActionTypeId = isFirstPilot ? (int)ActionEnum.FlyFirst : (int)ActionEnum.FlySecond,
						Description =
							$"{pair.CrewmemberDuties[i].Flights[j].From.Name}-{pair.CrewmemberDuties[i].Flights[j].To.Name} "
					};
					a.Description += isFirstPilot ? "Первый пилот." : "Второй пилот";

					crewmember.Roster.Actions.Add(a);
					db.Actions.Add(a);
					if (j < pair.CrewmemberDuties[i].Flights.Count - 1)
					{
						a = new Model.Action
						{
							StartTime = pair.CrewmemberDuties[i].Flights[j].EndTime,
							EndTime = pair.CrewmemberDuties[i].Flights[j + 1].StartTime,
							ActionTypeId = (int)ActionEnum.WaitingNewFly,
							Description = "Ожидание нового полета"
						};

						crewmember.Roster.Actions.Add(a);
						db.Actions.Add(a);
					}
				}

				if (i < pair.CrewmemberDuties.Count - 1)
				{
					a = new Model.Action
					{
						StartTime = pair.CrewmemberDuties[i].EndTime,
						EndTime = pair.CrewmemberDuties[i + 1].StartTime,
						ActionTypeId = (int)ActionEnum.Hotel,
						Description = "Отдых в гостинице."
					};

					crewmember.Roster.Actions.Add(a);
					db.Actions.Add(a);
				}
			}
		}

		public static void MakePairsAirline(Flight[] fSort, List<int[]> pairs, List<int> pair,
			TimeSpan? minTurnTime = null,
			int pos = 0)
		{
			if (pairs.Count > 300000)
			{
				throw new Exception("Слишком много пар!");
			}

			minTurnTime ??= TimeSpan.FromMinutes(15);


			if (pos == fSort.Length)
				return;
			bool added = false;
			if (pair.Count == 0)
			{
				pair.Add(pos);
				added = true;
			}
			else if (fSort[pair.Last()].ToId == fSort[pos].FromId &
					 (fSort[pos].StartTime - fSort[pair.Last()].EndTime) >= minTurnTime)
			// else if (fSort[pair.Last()].To == fSort[pos].From )
			{
				pair.Add(pos);
				added = true;
				if (fSort[pair.First()].FromId == fSort[pair.Last()].ToId)
				{
					int[] goodPair = new int[pair.Count];
					pair.CopyTo(goodPair);
					pairs.Add(goodPair);
				}
			}

			MakePairsAirline(fSort, pairs, pair, minTurnTime, pos + 1);
			if (added)
			{
				pair.RemoveAt(pair.Count - 1);
				MakePairsAirline(fSort, pairs, pair, minTurnTime, pos + 1);
			}
		}

		public static void MakeDutiesCrew(double minSit, double maxSit, double maxElapse, double maxFly, Flight[] fSort,
			List<int[]> duties, List<int> duty,
			int pos = 0)
		{
			if (duties.Count > 500000)
			{
				throw new Exception("Слишком много пар!");
			}

			if (pos == fSort.Length)
				return;
			bool added = false;
			if (duty.Count == 0)
			{
				duty.Add(pos);
				added = true;
				int[] goodDuty = new int[duty.Count];
				duty.CopyTo(goodDuty);
				duties.Add(goodDuty);
			}
			else if (fSort[duty.Last()].ToId == fSort[pos].FromId)
			{
				TimeSpan sit = fSort[pos].StartTime - fSort[duty.Last()].EndTime;
				if (sit.TotalHours >= minSit & sit.TotalHours <= maxSit)
				{
					TimeSpan elapse = fSort[duty.Last()].EndTime - fSort[duty.First()].StartTime;
					if (elapse.TotalHours <= maxElapse)
					{
						TimeSpan fly = TimeSpan.Zero;
						foreach (var leg in duty)
						{
							fly += fSort[leg].EndTime - fSort[leg].StartTime;
						}

						//Добавка
						fly += fSort[pos].EndTime - fSort[pos].StartTime;
						if (fly.TotalHours <= maxFly & fSort[pos].AirplaneId == fSort[duty.First()].AirplaneId)
						{
							duty.Add(pos);
							added = true;
							int[] goodDuty = new int[duty.Count];
							duty.CopyTo(goodDuty);
							duties.Add(goodDuty);
						}
					}
				}
			}

			MakeDutiesCrew(minSit, maxSit, maxElapse, maxFly, fSort, duties, duty, pos + 1);
			if (added)
			{
				duty.RemoveAt(duty.Count - 1);
				MakeDutiesCrew(minSit, maxSit, maxElapse, maxFly, fSort, duties, duty, pos + 1);
			}
		}

		public static void MakePairsCrew(int maxDuties, double minRest, double maxTAFB, Flight[] fSort,
			List<int[]> duties, List<int[][]> pairs, List<int[]> pair,
			int pos = 0)
		{
			if (pairs.Count > 300000)
			{
				throw new Exception("Слишком много пар!");
			}

			if (pos == duties.Count)
				return;
			bool added = false;
			if (pair.Count == 0)
			{
				pair.Add(duties[pos]);
				added = true;
				if (fSort[pair.First().First()].FromId == fSort[pair.Last().Last()].ToId)
				{
					int[][] goodPair = new int[pair.Count][];
					pair.CopyTo(goodPair);
					pairs.Add(goodPair);
				}
			}
			else if (pair.Count < maxDuties & fSort[pair.Last().Last()].EndTime < fSort[duties[pos].First()].StartTime &
					 fSort[pair.Last().Last()].ToId == fSort[duties[pos].First()].FromId &
					 fSort[pair.First().First()].AirplaneId == fSort[duties[pos].First()].AirplaneId)
			{
				TimeSpan rest = fSort[duties[pos].First()].StartTime - fSort[pair.Last().Last()].EndTime;
				if (rest.TotalHours >= minRest)
				{
					TimeSpan TAFB = fSort[pair.Last().Last()].EndTime - fSort[pair.First().First()].StartTime;
					if (TAFB.TotalHours <= maxTAFB)
					{
						pair.Add(duties[pos]);
						added = true;
						if (fSort[pair.First().First()].FromId == fSort[pair.Last().Last()].ToId)
						{
							int[][] goodPair = new int[pair.Count][];
							pair.CopyTo(goodPair);
							pairs.Add(goodPair);
						}
					}
				}
			}

			MakePairsCrew(maxDuties, minRest, maxTAFB, fSort, duties, pairs, pair, pos + 1);
			if (added)
			{
				pair.RemoveAt(pair.Count - 1);
				MakePairsCrew(maxDuties, minRest, maxTAFB, fSort, duties, pairs, pair, pos + 1);
			}
		}

		public static void MakePiAndCCrew(Flight[] fSort, List<int[][]> pairs, out bool[,] Pi, out double[] c,
			double mg1, double f1, double mg2, double f2)
		{
			Pi = new bool[fSort.Length, pairs.Count];
			c = new double[pairs.Count];
			double[] bd;
			double fly;
			double elapse;
			double TABS;
			for (int i = 0; i < pairs.Count; i++)
			{
				bd = new double[pairs[i].Length];

				for (int j = 0; j < pairs[i].Length; j++)
				{
					fly = 0;
					elapse = (fSort[pairs[i][j].Last()].EndTime - fSort[pairs[i][j].First()].StartTime).TotalHours;
					for (int f = 0; f < pairs[i][j].Length; f++)
					{
						Pi[pairs[i][j][f], i] = true;
						fly += (fSort[pairs[i][j][f]].EndTime - fSort[pairs[i][j][f]].StartTime).TotalHours;
					}

					bd[j] = Math.Max(mg1, Math.Max(f1 * elapse, fly));
				}

				TABS = (fSort[pairs[i].Last().Last()].EndTime - fSort[pairs[i].First().First()].StartTime).TotalHours;
				c[i] = Math.Max(mg2 * pairs[i].Length, Math.Max(f2 * TABS, bd.Sum()));
			}
		}

		public static void MakePiAndCAndTetaAirline(Airport[] airports, Airplane[] airplanes, TimeSpan[,] turnTimes,
			Flight[] fSort,
			List<int[]> pairs, out bool[,] Pi, out double[,] c, out bool[,] teta)
		{
			Pi = new bool[fSort.Length, pairs.Count];
			c = new double[pairs.Count, turnTimes.GetLength(1)];
			teta = new bool[pairs.Count, turnTimes.GetLength(1)];
			//Pi кол-во-полетов x количество цепочек, показывает что  i полет покрывает j цепочка
			//c - стоимость. размер кол-во цепочек x кол-во самолетов
			//teta размер как с, показывает, что самолет можно назначит на цепочку, т.к. есть минимальное оборотное время
			//turnTimes время оборота авиапорты х самолеты

			//Порядковый номер аэропорта для проверки на возможность присвоения цепочки
			int buf = 0;
			for (int i = 0; i < pairs.Count; i++)
			{
				for (int j = 0; j < c.GetLength(1); j++)
				{
					c[i, j] = 0;
					for (int k = 0; k < pairs[i].Length; k++)
					{
						double passangers = Math.Min(airplanes[j].Capacity, fSort[pairs[i][k]].Demand);
						//Сбор за влет+ посадку + сбор за авиационную безопасность + за багаж + уборка
						c[i, j] -= 21000 + 16500 + 180 * passangers + (2500 + 1220 + 2500 + 1500);
						c[i, j] += passangers *
								   fSort[pairs[i][k]].Price;
						c[i, j] -= airplanes[j].Cost * 32.90 *
								   (fSort[pairs[i][k]].EndTime - fSort[pairs[i][k]].StartTime).TotalHours;
					}

					teta[i, j] = true;

					for (int k = 1; k < pairs[i].Length; k++)
					{
						for (int l = 0; l < airports.Length; l++)
						{
							if (fSort[pairs[i][k]].From.Id == airports[l].Id)
							{
								buf = l;
								//break;
							}
						}

						if (fSort[pairs[i][k]].StartTime - fSort[pairs[i][k - 1]].EndTime < turnTimes[buf, j])
						{
							teta[i, j] = false;
							break;
						}
					}
				}

				for (int j = 0; j < pairs[i].Length; j++)
				{
					Pi[pairs[i][j], i] = true;
				}
			}
		}

		public static bool[][] MipSolverAirline(bool[,] Pi, double[,] c, bool[,] teta, int[] airplaneCount)
		{
			// Create the linear solver with the CBC backend.
			//solver = Solver.CreateSolver("SimpleMipProgram", "CBC_MIXED_INTEGER_PROGRAMMING");
			Solver solver = Solver.CreateSolver("SimpleMipProgram", "CBC_MIXED_INTEGER_PROGRAMMING");
			//CpSolver solver = new CpSolver();

			int flight = Pi.GetLength(0);
			int pair = c.GetLength(0);
			int air_num = c.GetLength(1);
			Variable[][] x = new Variable[air_num][];
			for (int i = 0; i < x.Length; i++)
			{
				x[i] = solver.MakeBoolVarArray(pair, $"x_{i}");
			}

			//Условие разбиения
			for (int i = 0; i < flight; i++)
			{
				var con = solver.MakeConstraint(1, 1);
				for (int j = 0; j < pair; j++)
				{
					for (int k = 0; k < air_num; k++)
					{
						//if (Pi[i, j] & teta[j, k])
						//    coef = 1;
						//con.SetCoefficient(x[k][j], Pi[i, j]& teta[j,k]?1:0);
						if (Pi[i, j] & teta[j, k])
						{
							con.SetCoefficient(x[k][j], Pi[i, j] & teta[j, k] ? 1 : 0);
						}
					}
				}
			}

			//Условие покрытия
			// for (int i = 0; i < pair; i++)
			// {
			//     var con = solver.MakeConstraint(0, 1);
			//     for (int j = 0; j < air_num; j++)
			//     {
			//         con.SetCoefficient(x[j][i], 1);
			//     }
			// }

			//Условия мощности парка 
			for (int i = 0; i < air_num; i++)
			{
				var con = solver.MakeConstraint(0, airplaneCount[i]);
				for (int j = 0; j < pair; j++)
				{
					con.SetCoefficient(x[i][j], 1);
				}
			}

			//Проверка возожности назначения рейса на цепочку
			// for (int i = 0; i < pair; i++)
			// {
			//     for (int j = 0; j < air_num; j++)
			//     {
			//         if (teta[i, j] == false)
			//         {
			//             var con = solver.MakeConstraint(0, 0);
			//             con.SetCoefficient(x[j][i], 1);
			//         }
			//     }
			// }

			//Целевая функция дохода
			var obj = solver.Objective();
			for (int i = 0; i < pair; i++)
			{
				for (int j = 0; j < air_num; j++)
				{
					obj.SetCoefficient(x[j][i], c[i, j]);
				}
			}


			obj.SetMaximization();
			Console.WriteLine("Number of variables = " + solver.NumVariables());

			Console.WriteLine("Number of constraints = " + solver.NumConstraints());

			Solver.ResultStatus resultStatus = solver.Solve();
			
			// Check that the problem has an optimal solution.
			if (resultStatus != Solver.ResultStatus.OPTIMAL)
			{
				Console.WriteLine("The problem does not have an optimal solution!");
				return null;
			}

			Console.WriteLine("Solution:");
			Console.WriteLine("Objective value = " + solver.Objective().Value());
			solver.EnableOutput();
			//for (int i = 0; i < air_num; i++)
			// {
			//     for (int j = 0; j < pair; j++)
			//     {
			//         Console.Write("{0} ", x[i][j].SolutionValue());
			//     }
			//
			//     Console.WriteLine();
			// }

			Console.WriteLine("\nAdvanced usage:");
			Console.WriteLine("Problem solved in " + solver.WallTime() + " milliseconds");
			Console.WriteLine("Problem solved in " + solver.Iterations() + " iterations");
			Console.WriteLine("Problem solved in " + solver.Nodes() + " branch-and-bound nodes");
			//return x.Select(i => i.SolutionValue() == 1 ? true : false).ToArray();
			return x.Select(i => i.Select(j => j.SolutionValue() == 1 ? true : false).ToArray()).ToArray();
		}

		public static double MipSolverCrew2(bool[,] Pi, double[] c, bool[] x, int pos = 0)
		{
			int n;
			if (pos == c.Length)
			{
				for (int i = 0; i < Pi.GetLength(0); i++)
				{
					n = 0;
					for (int j = 0; j < Pi.GetLength(1); j++)
					{
						if (x[j] & Pi[i, j])
							n++;
						if (n != 1)
							return Double.MaxValue;
					}
				}

				double res = 0;
				for (int i = 0; i < c.Length; i++)
				{
					res += x[i] ? c[i] : 0;
				}

				return res;
			}

			for (int i = 0; i < Pi.GetLength(0); i++)
			{
				n = 0;
				for (int j = 0; j < Pi.GetLength(1); j++)
				{
					if (x[j] & Pi[i, j])
						++n;
					if (n > 1)
						return Double.MaxValue;
				}
			}


			x[pos] = true;
			double on = MipSolverCrew2(Pi, c, x, pos + 1);
			x[pos] = false;
			double off = MipSolverCrew2(Pi, c, x, pos + 1);
			return Math.Min(on, off);
		}

		public static bool[] MipSolverCrew(bool[,] Pi, double[] c)
		{
			// Create the linear solver with the CBC backend.
			Solver solver = Solver.CreateSolver("SimpleMipProgram", "CBC_MIXED_INTEGER_PROGRAMMING");

			int flight = Pi.GetLength(0);
			int pair = c.Length;

			Variable[] x = solver.MakeBoolVarArray(pair, "x");
			for (int i = 0; i < flight; i++)
			{
				var con = solver.MakeConstraint(1, 1);

				for (int j = 0; j < pair; j++)
				{
					if (Pi[i, j])
					{
						con.SetCoefficient(x[j], 1);
					}

					//con.SetCoefficient(x[j], Pi[i, j] ? 1 : 0);
				}
			}

			var obj = solver.Objective();
			for (int i = 0; i < pair; i++)
			{
				obj.SetCoefficient(x[i], c[i]);
			}

			obj.SetMinimization();

			Console.WriteLine("Number of variables = " + solver.NumVariables());

			Console.WriteLine("Number of constraints = " + solver.NumConstraints());

			Solver.ResultStatus resultStatus = solver.Solve();

			// Check that the problem has an optimal solution.
			if (resultStatus != Solver.ResultStatus.OPTIMAL)
			{
				Console.WriteLine("The problem does not have an optimal solution!");
				return null;
			}

			Console.WriteLine("Solution:");
			Console.WriteLine("Objective value = " + solver.Objective().Value());
			solver.EnableOutput();
			//for (int i = 0; i < x.Length; i++)
			//{
			//    Console.Write("{0} ", x[i].SolutionValue());
			//}

			Console.WriteLine("\nAdvanced usage:");
			Console.WriteLine("Problem solved in " + solver.WallTime() + " milliseconds");
			Console.WriteLine("Problem solved in " + solver.Iterations() + " iterations");
			Console.WriteLine("Problem solved in " + solver.Nodes() + " branch-and-bound nodes");
			return x.Select(i => i.SolutionValue() == 1 ? true : false).ToArray();
		}

		private void ComboBoxFlight_Loaded(object sender, RoutedEventArgs e)
		{
			ComboBox combo = (ComboBox)sender;
			CollectionViewSource airoportViewSource =
				((CollectionViewSource)(this.FindResource("AiroportViewSource")));
			// Binding b = new Binding();
			// b.Source = _db.Airplanes.Local.ToObservableCollection();
			// b.Mode = BindingMode.TwoWay;
			// b.Path = new PropertyPath("this");
			// combo.SetBinding(ItemsControl.ItemsSourceProperty, b);
			combo.ItemsSource = (IEnumerable)airoportViewSource.Source;
		}
		private void ComboBoxAction_Loaded(object sender, RoutedEventArgs e)
		{
			ComboBox combo = (ComboBox)sender;
			CollectionViewSource actionTypesViewSource =
				((CollectionViewSource)(this.FindResource("ActionTypesViewSource")));
			// Binding b = new Binding();
			// b.Source = _db.Airplanes.Local.ToObservableCollection();
			// b.Mode = BindingMode.TwoWay;
			// b.Path = new PropertyPath("this");
			// combo.SetBinding(ItemsControl.ItemsSourceProperty, b);
			combo.ItemsSource = (IEnumerable)actionTypesViewSource.Source;
		}

		private void Datagrid_LoadingRow(object sender, DataGridRowEventArgs e)
		{
			e.Row.Header = (e.Row.GetIndex() + 1).ToString();
		}

		private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
		{
			CollectionViewSource flightViewSource =
				((CollectionViewSource)(this.FindResource("FlightViewSource")));
			CollectionViewSource airplaneViewSource =
				((CollectionViewSource)(this.FindResource("AirplaneViewSource")));
			CollectionViewSource airoportViewSource =
				((CollectionViewSource)(this.FindResource("AiroportViewSource")));
			CollectionViewSource crewmemberViewSource =
				((CollectionViewSource)(this.FindResource("CrewmemberViewSource")));
			CollectionViewSource actionTypesViewSource =
				((CollectionViewSource)(this.FindResource("ActionTypesViewSource")));

			Flight add = (Flight)FindResource("FlightAdd");
			// Load is an extension method on IQueryable,
			// defined in the System.Data.Entity namespace.
			// This method enumerates the results of the query,
			// similar to ToList but without creating a list.
			// When used with Linq to Entities this method
			// creates entity objects and adds them to the context.
			//add.EndTime = DateTime.Now.Date + TimeSpan.FromHours(1);
			//add.StartTime = DateTime.Now.Date;


			_db.Airplanes.Load();
			_db.Flights.Load();
			_db.Airports.Load();
			_db.TurnTimes.Load();
			_db.Crewmembers.Include(i => i.Permissions).Include(c => c.Roster).ThenInclude(r => r.Actions).Load();
			//_db.Rosters.Load();
			//_db.Actions.Load();
			_db.ActionTypes.Load();
			_db.AirplanePairs.Load();
			foreach (var crewmember in _db.Crewmembers)
			{
				// crewmember.Roster.Actions.Add(new Model.Action()
				// {
				//     ActionTypeId = (int) ActionEnum.Holiday, StartTime = DateTime.Now, EndTime = DateTime.Now.AddDays(1),
				//     Description = "лулзич"
				// });
			}
			// After the data is loaded call the DbSet<T>.Local property
			// to use the DbSet<T> as a binding source.

			flightViewSource.Source = _db.Flights.Local.ToObservableCollection();
			airplaneViewSource.Source = _db.Airplanes.Local.ToObservableCollection();
			airoportViewSource.Source = _db.Airports.Local.ToObservableCollection();
			crewmemberViewSource.Source = _db.Crewmembers.Local.ToObservableCollection();
			actionTypesViewSource.Source = _db.ActionTypes.Local.ToObservableCollection();
		}

		private void ButtonFlightDelete_Click(object sender, RoutedEventArgs e)
		{
			int count = DatagridFlight.SelectedItems.Count;
			if (MessageBox.Show(this, $"Удалить {count} рейсов. Вы уверены?", "Удаление рейсов",
				MessageBoxButton.OKCancel) == MessageBoxResult.OK)
			{
				try
				{
					_db.Flights.RemoveRange(DatagridFlight.SelectedItems.Cast<Flight>().ToArray());
					_db.SaveChanges();
				}
				catch (DbUpdateException ee)
				{
					MessageBox.Show("Ошибка удаления рейсов!", "Непредвиденная ошибка");
				}
				catch (Exception ee)
				{
					MessageBox.Show(ee.Message);
				}
			}
		}

		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			BackgroundWorker worker = new BackgroundWorker();
			worker.WorkerSupportsCancellation = true;
			worker.DoWork += (o, ea) =>
			{
				//List<String> listOfString = new List<string>();
				//for (int i = 0; i < 10000000; i++)
				//{
				//    listOfString.Add(String.Format("Item: {0}", i));
				//}
				////use the Dispatcher to delegate the listOfStrings collection back to the UI
				//Dispatcher.Invoke((System.Action)(() => _listBox.ItemsSource = listOfString));
				Thread.Sleep(1000 * 1);
				worker.CancelAsync();
				Thread.Sleep(1000 * 5);
			};
			worker.RunWorkerCompleted += (o, ea) => { MainBusy.IsBusy = false; };
			MainBusy.IsBusy = true;

			worker.RunWorkerAsync();
		}

		private void ButtonFlightAdd_Click(object sender, RoutedEventArgs e)
		{
			Flight add, newFlight = null;
			try
			{
				add = (Flight)FindResource("FlightAdd");

				newFlight = new Flight()
				{
					Num=add.Num,
					FromId = add.FromId,
					StartTime = add.StartTime,
					ToId = add.ToId,
					EndTime = add.EndTime,
					Demand = add.Demand,
					Price = add.Price
				};

				_db.Flights.Add(newFlight);
				_db.SaveChanges();
			}

			catch (DbUpdateException ee)
			{
				MessageBox.Show("Ошибка добавления рейса! Проверьте, является ли значение номера рейса уникальным.",
					"Ошибка добавления рейса");
				if (newFlight != null)
					_db.Flights.Remove(newFlight);
			}
			catch (Exception ee)
			{
				MessageBox.Show(ee.Message);
			}

			Console.WriteLine("");
		}

		private void ButtonFlightLoad_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog op = new OpenFileDialog()
			{
				Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
			};
			try
			{
				if (op.ShowDialog() == true)
				{
					List<Flight> flights = new List<Flight>();

					string[][] source = File.ReadAllLines(op.FileName).Where(s => !s.StartsWith("#"))
						.Select(s => s.Split()).ToArray();
					foreach (var s in source)
					{
						int num = int.Parse(s[0]);
						var from = _db.Airports.FirstOrDefault(a =>
							a.Name == s[1]);
						if (from == null)
							throw new ArgumentException($"Аэропорта {s[1]} не существует!");
						int fromId = from.Id;

						DateTime startTime = DateTime.Parse(s[2] + " " + s[3]);

						var to = _db.Airports.FirstOrDefault(a =>
							a.Name == s[4]);
						if (to == null)
							throw new ArgumentException($"Аэропорта {s[4]} не существует!");
						int toId = to.Id;

						DateTime endTime = DateTime.Parse(s[5] + " " + s[6]);

						double demand = Double.Parse(s[7]);
						double price = Double.Parse(s[8]);
						flights.Add(new Flight()
						{
							Num = num,
							FromId = fromId,
							StartTime = startTime,
							ToId = toId,
							EndTime = endTime,
							Demand = demand,
							Price = price
						});
					}

					_db.Flights.RemoveRange(_db.Flights);
					_db.Flights.AddRange(flights);
					_db.SaveChanges();
				}
			}
			catch (DbUpdateException ee)
			{
				MessageBox.Show("Ошибка записи в базу данных! ", "Ошибка");
			}
			catch (Exception ee)
			{
				MessageBox.Show("Ошибка чтения файла! " + ee.Message, "Ошибка");
			}
		}

		private void ButtonAirportDelete_Click(object sender, RoutedEventArgs e)
		{
			int count = DatagridAirport.SelectedItems.Count;
			if (MessageBox.Show(this, $"Удалить {count} аэропортов. Вы уверены?", "Удаление аэропортов",
				MessageBoxButton.OKCancel) == MessageBoxResult.OK)
			{
				try
				{
					var airports = DatagridAirport.SelectedItems.Cast<Airport>().ToArray();
					foreach (var airport in airports)
					{
						_db.TurnTimes.RemoveRange(_db.TurnTimes.Where(t => t.AirportId == airport.Id));
					}

					_db.Airports.RemoveRange(airports);
					_db.SaveChanges();
				}
				catch (DbUpdateException ee)
				{
					MessageBox.Show("Ошибка удаления аэропортов!", "Непредвиденная ошибка");
				}
				catch (Exception ee)
				{
					MessageBox.Show(ee.Message);
				}
			}
		}

		private void ButtonAirportLoad_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog op = new OpenFileDialog()
			{
				Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
			};
			try
			{
				if (op.ShowDialog() == true)
				{
					List<Airport> airports = new List<Airport>();
					List<TurnTime> times = new List<TurnTime>();
					var airplanes = _db.Airplanes;
					Airport newAirport;
					string[][] source = File.ReadAllLines(op.FileName).Where(s => !s.StartsWith("#"))
						.Select(s => s.Split('\"').Select(a=>a.Trim()).ToArray()).ToArray();
					foreach (var s in source)
					{
						string name = s[0];
						if (name.Length != 3 | name.Any(c => !char.IsLetter(c)))
						{
							MessageBox.Show("Неверный код аэропорта ", "Ошибка");
							return;
						}

						string fullname = s[1].Trim('"');
						newAirport = new Airport()
						{
							Name = name,
							Fullname = fullname,
						};
						airports.Add(newAirport);
						foreach (var airplane in airplanes)
						{
							times.Add(new TurnTime() { Airport = newAirport, Airplane = airplane, Time = TimeSpan.Zero });
						}
					}

					_db.Airports.RemoveRange(_db.Airports);
					_db.Airports.AddRange(airports.OrderBy(i => i.Name));
					_db.TurnTimes.RemoveRange(_db.TurnTimes);
					_db.TurnTimes.AddRange(times);
					_db.SaveChanges();
				}
			}
			catch (DbUpdateException ee)
			{
				MessageBox.Show("Ошибка записи в базу данных! ", "Ошибка");
			}
			catch (Exception ee)
			{
				MessageBox.Show("Ошибка чтения файла! " + ee.Message, "Ошибка");
			}
		}

		private void ButtonAirportAdd_Click(object sender, RoutedEventArgs e)
		{
			Airport add, newAirport = null;
			List<TurnTime> times = new List<TurnTime>();
			try
			{
				add = (Airport)FindResource("AirportAdd");
				var airplanes = _db.Airplanes;

				newAirport = new Airport()
				{
					Name = add.Name,
					Fullname = add.Fullname
				};

				_db.Airports.Add(newAirport);

				foreach (var airplane in airplanes)
				{
					times.Add(new TurnTime() { AirplaneId = airplane.Id, Airport = newAirport, Time = TimeSpan.Zero });
				}

				_db.TurnTimes.AddRange(times);
				_db.SaveChanges();
			}
			catch (DbUpdateException ee)
			{
				MessageBox.Show(
					"Ошибка добавления аэропорта! Проверьте, является ли значение кода ИАТА уникальным, а его длина составляет 3 символа.",
					"Ошибка добавления аэропрта");
				if (newAirport != null)
				{
					_db.TurnTimes.RemoveRange(times);
					_db.Airports.Remove(newAirport);
				}
			}
			catch (Exception ee)
			{
				MessageBox.Show(ee.Message);
			}


			Console.WriteLine("");
		}


		private void ButtonAirplaneLoad_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog op = new OpenFileDialog()
			{
				Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
			};
			try
			{
				if (op.ShowDialog() == true)
				{
					List<TurnTime> times = new List<TurnTime>();
					List<Permission> permissions = new List<Permission>();
					var crews = _db.Crewmembers;
					var airports = _db.Airports;
					List<Airplane> airplanes = new List<Airplane>();
					string[][] source = File.ReadAllLines(op.FileName).Where(s => !s.StartsWith("#"))
						.Select(s => s.Split()).ToArray();
					Airplane newAirplane;
					foreach (var s in source)
					{
						string name = s[0];
						int count = int.Parse(s[1]);
						double cost = int.Parse(s[2]);
						int capacity = int.Parse(s[3]);
						newAirplane = new Airplane()
						{
							Name = name,
							Count = count,
							Cost = cost,
							Capacity = capacity,
						};
						airplanes.Add(newAirplane);
						for(int i = 4; i < s.Count(); i+=2)
						{
							Airport a = _db.Airports.FirstOrDefault(a => a.Name == s[i]);
							times.Add(new TurnTime() { Airplane = newAirplane, Airport = a, Time = TimeSpan.Parse(s[i + 1]) });
						}
						if (times.Count!=_db.Airports.Count()*airplanes.Count)
						{
							throw new InvalidDataException("Не хватает данных !");
						}
						//foreach (var airport in airports)
						//{
						//	times.Add(new TurnTime() { Airport = airport, Airplane = newAirplane, Time = TimeSpan.Zero });
						//}

						foreach (var crewmember in crews)
						{
							permissions.Add(new Permission()
							{
								Airplane = newAirplane,
								Crewmember = crewmember,
								FirstPilot = false,
								SecondPilot = false
							});
						}
					}

					_db.AirplanePairs.RemoveRange(_db.AirplanePairs);
					_db.Airplanes.RemoveRange(_db.Airplanes);
					_db.Airplanes.AddRange(airplanes);
					_db.TurnTimes.RemoveRange(_db.TurnTimes);
					_db.TurnTimes.AddRange(times);
					_db.Permissions.RemoveRange(_db.Permissions);
					_db.Permissions.AddRange(permissions);
					
					_db.SaveChanges();
				}
			}
			catch (DbUpdateException ee)
			{
				MessageBox.Show("Ошибка записи в базу данных! ", "Ошибка");
			}
			catch (Exception ee)
			{
				MessageBox.Show("Ошибка чтения файла! " + ee.Message, "Ошибка");
			}
		}

		private void ButtonAirplaneDelete_Click(object sender, RoutedEventArgs e)
		{

			if (TabControlAirplane.SelectedItem != null & MessageBox.Show(this, $"Удалить запись об ВС. Вы уверены?", "Удаление типов ВС",
				MessageBoxButton.OKCancel) == MessageBoxResult.OK)
			{
				try
				{
					var airplanes = new List<Airplane>() { (Airplane)TabControlAirplane.SelectedItem };
					foreach (var airplane in airplanes)
					{
						_db.TurnTimes.RemoveRange(_db.TurnTimes.Where(t => t.AirplaneId == airplane.Id));
						_db.Permissions.RemoveRange(_db.Permissions.Where(p => p.AirplaneId == airplane.Id));
					}

					_db.Airplanes.RemoveRange(airplanes);
					_db.SaveChanges();
				}
				catch (DbUpdateException ee)
				{
					MessageBox.Show("Ошибка удаления аэропортов!", "Непредвиденная ошибка");
				}
				catch (Exception ee)
				{
					MessageBox.Show(ee.Message);
				}
			}
		}

		private void ButtonAirplaneAdd_Click(object sender, RoutedEventArgs e)
		{
			Airplane add, newAirplane = null;
			List<TurnTime> times = new List<TurnTime>();
			List<Permission> permissions = new List<Permission>();
			try
			{
				add = (Airplane)FindResource("AirplaneAdd");

				var airports = _db.Airports;
				var crews = _db.Crewmembers;

				newAirplane = new Airplane()
				{
					Name = add.Name,
					Count = add.Count,
					Cost = add.Cost,
					Capacity = add.Capacity
				};

				_db.Airplanes.Add(newAirplane);

				foreach (var airport in airports)
				{
					times.Add(new TurnTime() { Airport = airport, Airplane = newAirplane, Time = TimeSpan.Zero });
				}

				foreach (var crewmember in crews)
				{
					permissions.Add(new Permission()
					{ Airplane = newAirplane, Crewmember = crewmember, FirstPilot = false, SecondPilot = false });
				}

				_db.Permissions.AddRange(permissions);
				_db.TurnTimes.AddRange(times);
				_db.SaveChanges();
			}
			catch (DbUpdateException ee)
			{
				MessageBox.Show("Ошибка добавления типа ВС! Проверьте, не повторяется ли значение названия ВС.",
					"Ошибка добавления типа ВС");
				if (newAirplane != null)
				{
					_db.TurnTimes.RemoveRange(times);
					foreach (var permission in permissions)
					{
						permission.Crewmember.Permissions.Remove(permission);
					}

					_db.Permissions.RemoveRange(permissions);
					_db.Airplanes.Remove(newAirplane);
				}
			}
			catch (Exception ee)
			{
				MessageBox.Show(ee.Message);
			}


			Console.WriteLine("");
		}
		private void ButtonCrewMemberLoad_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog op = new OpenFileDialog()
			{
				Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
			};
			try
			{
				if (op.ShowDialog() == true)
				{
					List<Crewmember> crewmembers = new List<Crewmember>();
					List<Permission> permissions = new List<Permission>();
					List<Roster> rosters = new List<Roster>();
					List<Model.Action> actions = new List<Model.Action>();

					string[][] source = File.ReadAllLines(op.FileName).Where(s => !s.StartsWith("#"))
						.Select(s => s.Split(' ',StringSplitOptions.RemoveEmptyEntries)).ToArray();
					Crewmember crew;
					for (int i = 0; i < source.Length; i+=3)
					{
						crew = new Crewmember()
						{
							FirstName = source[i][0],
							SecondName = source[i][1],
							LastName = source[i][2],
							Base = _db.Airports.FirstOrDefault(a => a.Name ==source[i][3]) ?? throw new Exception("Ошибка во входных данных"),
						};
						for (int j = 0; j < source[i+1].Length; j+=3)
						{
							permissions.Add(new Permission()
							{
								Crewmember=crew,
								Airplane = _db.Airplanes.FirstOrDefault(a => a.Name == source[i+1][j]) ?? throw new Exception("Ошибка во входных данных"),
								FirstPilot = source[i+1][j+1]=="Да",
								SecondPilot = source[i+1][j+2]=="Да"
							});
						}
						foreach (var airplane in _db.Airplanes.AsEnumerable().Except(permissions.Where(p=>p.Crewmember==crew).Select(p=>p.Airplane)))
						{
							permissions.Add(new Permission()
							{
								Crewmember = crew,
								Airplane = airplane,
								FirstPilot = false,
								SecondPilot = false,
							});
						}
						Roster r = new Roster();
						crew.Roster = r;
						string[] s = string.Join(" ", source[i + 2]).Split('\"').Select(a => a.Trim()).Where(a => a.Length > 0).ToArray();
						for (int j = 0; j < s.Length; j+=4)
						{
							
							actions.Add(new Model.Action
							{
								ActionTypeId = int.Parse(s[j*4+0]),
								Description = s[j * 4 + 1],
								StartTime = DateTime.Parse(s[j * 4 + 2]),
								EndTime = DateTime.Parse(s[j * 4 + 3]),
								Roster=r
							});
						}
						rosters.Add(r);
					}
					


					_db.Permissions.RemoveRange(_db.Permissions);
					_db.Permissions.AddRange(permissions);
					_db.Actions.RemoveRange(_db.Actions);
					_db.Actions.AddRange(actions);
					_db.Rosters.RemoveRange(_db.Rosters);
					_db.Rosters.AddRange(rosters);
					_db.Crewmembers.RemoveRange(_db.Crewmembers);
					_db.Crewmembers.AddRange(crewmembers);
					_db.CrewmemberDuties.RemoveRange(_db.CrewmemberDuties);
					_db.CrewmemberPairs.RemoveRange(_db.CrewmemberPairs);
					_db.SaveChanges();
				}
			}
			catch (DbUpdateException ee)
			{
				MessageBox.Show("Ошибка записи в базу данных! ", "Ошибка");
			}
			catch (Exception ee)
			{
				MessageBox.Show("Ошибка чтения файла! " + ee.Message, "Ошибка");
			}
		}

		private void ButtonCrewmemberAdd_Click(object sender, RoutedEventArgs e)
		{
			Crewmember add, newCrewmember = null;
			List<Permission> permissions = new List<Permission>();
			Roster r = new Roster();
			try
			{
				add = (Crewmember)FindResource("CrewmemberAdd");

				var airplanes = _db.Airplanes;
				var crews = _db.Crewmembers;

				newCrewmember = new Crewmember()
				{
					FirstName = add.FirstName,
					SecondName = add.SecondName,
					LastName = add.LastName,
					BaseId = add.BaseId
				};

				_db.Crewmembers.Add(newCrewmember);
				newCrewmember.Roster = r;

				foreach (var airplane in airplanes)
				{
					permissions.Add(new Permission() { Airplane = airplane, Crewmember = newCrewmember, FirstPilot = false, SecondPilot = false });
				}

				_db.Permissions.AddRange(permissions);
				_db.SaveChanges();
			}
			catch (DbUpdateException ee)
			{
				MessageBox.Show("Ошибка добавления члена экипажа!",
					"Ошибка добавления типа ВС");
				if (newCrewmember != null)
				{
					foreach (var permission in permissions)
					{
						permission.Crewmember.Permissions.Remove(permission);
					}

					_db.Permissions.RemoveRange(permissions);
					_db.Rosters.Remove(r);
					_db.Crewmembers.Remove(newCrewmember);
				}
			}
			catch (Exception ee)
			{
				MessageBox.Show(ee.Message);
			}


			Console.WriteLine("");
		}

		private void ButtonCrewMemberDelete_Click(object sender, RoutedEventArgs e)
		{
			if (TabControlCrewmember.SelectedItem != null & MessageBox.Show(this, $"Удалить запись о члене экипажа - {((Crewmember)TabControlCrewmember.SelectedItem).Fullname}. Вы уверены?", "Удаление члена экипажа",
			   MessageBoxButton.OKCancel) == MessageBoxResult.OK)
			{
				try
				{
					Crewmember crew = (Crewmember)TabControlCrewmember.SelectedItem;
					_db.Permissions.RemoveRange(crew.Permissions);
					_db.Actions.RemoveRange(crew.Roster.Actions);
					_db.Rosters.Remove(crew.Roster);
					_db.Crewmembers.Remove(crew);
					_db.SaveChanges();
				}
				catch (DbUpdateException ee)
				{
					MessageBox.Show("Ошибка удаления записи о члене экипажа!", "Непредвиденная ошибка");
				}
				catch (Exception ee)
				{
					MessageBox.Show(ee.Message);
				}
			}
		}
		private void ButtonActionAdd_Click(object sender, RoutedEventArgs e)
		{
			Crewmember c = ((Button)sender).DataContext as Crewmember;
			Model.Action a = new Model.Action() 
			{
				ActionTypeId = (int)ActionEnum.Other, 
				StartTime = DateTime.Now, 
				EndTime = DateTime.Now.AddHours(1), 
				Description = "Не указано" 
			};
			c.Roster.Actions.Add(a);
			_db.Actions.Add(a);
			try
			{
				_db.SaveChanges();

			}
			catch (DbUpdateException ee)
			{
				MessageBox.Show("Ошибка добавления события расписания в базу данных, проверьтре правильность вводе других данных!", "Непредвиденная ошибка");
			}
			catch (Exception ee)
			{
				MessageBox.Show(ee.Message);
			}

		}

		private void ButtonAirplaneSheldure_Click(object sender, RoutedEventArgs e)
		{

			_backgroundWorker = new Thread(() =>
			{
				Dispatcher.BeginInvoke((System.Action)(() => MainBusy.IsBusy = true));
				
				_db.AirplanePairs.RemoveRange(_db.AirplanePairs);
				foreach (var flight in _db.Flights)
				{
					flight.Airplane = null;
				}
				_db.SaveChanges();
				int res;
				try
				{
					res = AirlineRostering();
				}
				catch(Exception ee)
				{
					MessageBox.Show("Непредвиденная ошибка.", "Ошибка");
					return;
				}
				if (res == 1)
				{
					MessageBox.Show("Ошибка. Слишком большое количество цепочек рейсов. Попробуйте разбить список рейсов на меньшие списки.","Ошибка");
					return;
				}
				if (res == 2)
				{
					MessageBox.Show("Ошибка. Невозможно найти действительное решение задачи расстановки парка ВС по рейсам. Попробуйте изменить список рейсов и/или данные о типах ВС.", "Ошибка");
					return;
				}
				_db.SaveChanges();
				Dispatcher.BeginInvoke((System.Action)(() => MainBusy.IsBusy = false));
			});
			_backgroundWorker.Start();
			//t.Join();
			//MainBusy.IsBusy = false;
		}
		private void ButtonBusy_Click(object sender, RoutedEventArgs e)
		{
			if(_backgroundWorker!=null && _backgroundWorker.IsAlive)
			{
				TerminateThread(_backgroundWorker.ManagedThreadId);
				MainBusy.IsBusy = false;
			}
		}
		[DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
		public static extern int TerminateThread(int hThread);

		
	}
}