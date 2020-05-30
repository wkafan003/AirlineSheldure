using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

            AirlineRostering();
            _db.SaveChanges();
            CrewRostering();
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

        public static void AirlineRostering()
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
                MakePairsAirline(fSort, pairsAirline, new List<int>(), minTurnTime);
                MakePiAndCAndTetaAirline(airports, airplanes, turnTimes, fSort, pairsAirline, out PiAirline,
                    out cAirline,
                    out teta);


                pairs_flag = MipSolverAirline(PiAirline, cAirline, teta, airplanes.Select(a => a.Count).ToArray());
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
                                fSort[num].AirplaneId = airplanes[j].Id;
                            }

                            p.Airplane = airplanes[j];
                            _db.AirplanePairs.Add(p);
                            break;
                        }
                    }
                }
            }

            int[] crewCounts = new[] {15, 24, 18, 1};
            crewCounts = crewCounts.Select(c => c * 5).ToArray();
            for (int j = 0; j < airports.Length; j++)
            {
                for (int i = 0; i < crewCounts[j]; i++)
                {
                    Crewmember pilot = new Crewmember()
                        {BaseId = airports[j].Id, FirstName = $"Иван {i}_{j}", LastName = "Иванов"};
                    Roster r = new Roster();
                    pilot.Roster = r;
                    for (int k = 0; k < airplanes.Length; k++)
                    {
                        Permission p = new Permission()
                            {AirplaneId = airplanes[k].Id, FirstPilot = true, SecondPilot = true};
                        pilot.Permissions.Add(p);
                        _db.Permissions.Add(p);
                    }

                    _db.Rosters.Add(r);
                    _db.Crewmembers.Add(pilot);
                }
            }

            //db.SaveChanges();
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
                        ActionTypeId = isFirstPilot ? (int) ActionEnum.FlyFirst : (int) ActionEnum.FliSecond,
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
                            ActionTypeId = (int) ActionEnum.WaitingNewFly,
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
                        ActionTypeId = (int) ActionEnum.Hotel,
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
            ComboBox combo = (ComboBox) sender;
            CollectionViewSource airoportViewSource =
                ((CollectionViewSource) (this.FindResource("AiroportViewSource")));
            // Binding b = new Binding();
            // b.Source = _db.Airplanes.Local.ToObservableCollection();
            // b.Mode = BindingMode.TwoWay;
            // b.Path = new PropertyPath("this");
            // combo.SetBinding(ItemsControl.ItemsSourceProperty, b);
            combo.ItemsSource = (IEnumerable) airoportViewSource.Source;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(" ");
        }

        private void Datagrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            CollectionViewSource flightViewSource =
                ((CollectionViewSource) (this.FindResource("FlightViewSource")));
            CollectionViewSource airplaneViewSource =
                ((CollectionViewSource) (this.FindResource("AirplaneViewSource")));
            CollectionViewSource airoportViewSource =
                ((CollectionViewSource) (this.FindResource("AiroportViewSource")));
            Flight add = (Flight) FindResource("FlightAdd");
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
            _db.Crewmembers.Include(i => i.Permissions).Include(c => c.Roster).ThenInclude(r => r.Actions).Load();
            _db.AirplanePairs.Load();

            // After the data is loaded call the DbSet<T>.Local property
            // to use the DbSet<T> as a binding source.

            flightViewSource.Source = _db.Flights.Local.ToObservableCollection();
            airplaneViewSource.Source = _db.Airplanes.Local.ToObservableCollection();
            airoportViewSource.Source = _db.Airports.Local.ToObservableCollection();
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
                add = (Flight) FindResource("FlightAdd");

                newFlight = new Flight()
                {
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
                MessageBox.Show("Ошибка записи в базу данных! ","Ошибка");
            }
            catch (Exception ee)
            {
                MessageBox.Show("Ошибка чтения файла! " + ee.Message,"Ошибка");
            }
        }

		private void ButtonAiroportDelete_Click(object sender, RoutedEventArgs e)
		{
            int count = DatagridAiroport.SelectedItems.Count;
            if (MessageBox.Show(this, $"Удалить {count} аэропортов. Вы уверены?", "Удаление аэропортов",
                MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                try
                {
                    var airports = DatagridAiroport.SelectedItems.Cast<Airport>().ToArray();
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

		private void ButtonAiroportLoad_Click(object sender, RoutedEventArgs e)
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
                    string[][] source = File.ReadAllLines(op.FileName).Where(s => !s.StartsWith("#"))
                        .Select(s => s.Split()).ToArray();
                    foreach (var s in source)
                    {
                        string name = s[0];
                        if(name.Length != 3 | name.Any(c => !char.IsLetter(c))){
                            MessageBox.Show("Неверный код аэропорта ", "Ошибка");
                        }
                        string fullname = s[1].Trim('"');
                        airports.Add(new Airport()
                        {
                            Name=name,
                            Fullname=fullname,
                        });
                    }

                    _db.Airports.RemoveRange(_db.Airports);
                    _db.Airports.AddRange(airports.OrderBy(i=>i.Name));
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
            try
            {
                add = (Airport)FindResource("AirportAdd");

                newAirport = new Airport()
                {
                    Name=add.Name,
                    Fullname=add.Fullname
                };

                _db.Airports.Add(newAirport);
                _db.SaveChanges();
            }

            catch (DbUpdateException ee)
            {
                MessageBox.Show("Ошибка добавления аэропорта! Проверьте, является ли значение кода ИАТА уникальным, а его длина составляет 3 символа.",
                    "Ошибка добавления аэропрта");
                if (newAirport != null)
                    _db.Airports.Remove(newAirport);
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }

            Console.WriteLine("");
        }
	}
}