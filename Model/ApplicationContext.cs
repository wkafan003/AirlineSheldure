using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Model
{
    public class ApplicationContext : DbContext
    {
        public DbSet<Airport> Airports { get; set; }
        public DbSet<Flight> Flights { get; set; }
        public DbSet<Airplane> Airplanes { get; set; }
        public DbSet<AirplanePair> AirplanePairs { get; set; }
        public DbSet<TurnTime> TurnTimes { get; set; }
        public DbSet<Crewmember> Crewmembers { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<CrewmemberDuty> CrewmemberDuties { get; set; }
        public DbSet<CrewmemberPair> CrewmemberPairs { get; set; }

        public DbSet<Roster> Rosters { get; set; }
        public DbSet<Action> Actions { get; set; }
        public DbSet<ActionType> ActionTypes { get; set; }


        public ApplicationContext()
        {
            Database.EnsureDeleted();
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=diplom;Username=postgres;Password=123");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Flight[] flights;
            List<Flight> allFlights = new List<Flight>();
            for (int day = 0; day < 1; day++)
            {
                flights = File.ReadAllLines("расписание.txt").Select(s => s.Split())
                    .Select(s => new Flight(0, int.Parse(s[0]), int.Parse(s[1]), int.Parse(s[2]),
                        DateTime.Parse(s[3]), DateTime.Parse(s[4]), double.Parse(s[5]), double.Parse(s[6]))).ToArray();
                for (int i = 0; i < flights.Length; i++)
                {
                    flights[i].StartTime += TimeSpan.FromDays(day);
                    flights[i].EndTime += TimeSpan.FromDays(day);
                    flights[i].Num += day * 100;
                    flights[i].Id = -flights.Length * (day + 1) + i;
                }

                allFlights.AddRange(flights);
            }


            modelBuilder.Entity<Flight>().HasData(allFlights);
            modelBuilder.Entity<Airplane>().HasData(new[]
            {
                new Airplane() {Id = -3, Name = "A320", Count = 4, Cost = 2700, Capacity = 164},
                new Airplane() {Id = -2, Name = "B735", Count = 2, Cost = 2400, Capacity = 138},
                new Airplane() {Id = -1, Name = "B772", Count = 3, Cost = 6100, Capacity = 305}
            });
            modelBuilder.Entity<Airport>().HasData(new Airport[]
            {
                new Airport() {Id = -4, Name = "CDG", Fullname = "Charles De Gaulle, Париж, Франция"},
                new Airport() {Id = -3, Name = "LED", Fullname = "Пулково, С.Петербург, Россия"},
                new Airport() {Id = -2, Name = "OVB", Fullname = "Толмачево, Новосибирск, Россия"},
                new Airport() {Id = -1, Name = "SVO", Fullname = "Шереметьево, Москва, Россия"},
            });
            modelBuilder.Entity<TurnTime>().HasData(new TurnTime[]
            {
                new TurnTime() {Id = -12, AirportId = -4, AirplaneId = -3, Time = new TimeSpan(0, 35, 0)},
                new TurnTime() {Id = -11, AirportId = -4, AirplaneId = -2, Time = new TimeSpan(0, 40, 0)},
                new TurnTime() {Id = -10, AirportId = -4, AirplaneId = -1, Time = new TimeSpan(0, 60, 0)},
                new TurnTime() {Id = -9, AirportId = -3, AirplaneId = -3, Time = new TimeSpan(0, 40, 0)},
                new TurnTime() {Id = -8, AirportId = -3, AirplaneId = -2, Time = new TimeSpan(0, 45, 0)},
                new TurnTime() {Id = -7, AirportId = -3, AirplaneId = -1, Time = new TimeSpan(0, 55, 0)},
                new TurnTime() {Id = -6, AirportId = -2, AirplaneId = -3, Time = new TimeSpan(0, 45, 0)},
                new TurnTime() {Id = -5, AirportId = -2, AirplaneId = -2, Time = new TimeSpan(0, 50, 0)},
                new TurnTime() {Id = -4, AirportId = -2, AirplaneId = -1, Time = new TimeSpan(0, 70, 0)},
                new TurnTime() {Id = -3, AirportId = -1, AirplaneId = -3, Time = new TimeSpan(0, 40, 0)},
                new TurnTime() {Id = -2, AirportId = -1, AirplaneId = -2, Time = new TimeSpan(0, 45, 0)},
                new TurnTime() {Id = -1, AirportId = -1, AirplaneId = -1, Time = new TimeSpan(0, 60, 0)},
            });
            Crewmember[] crewmembers = new Crewmember[]
            {
                new Crewmember() {Id = -8, Fullname = "Иван Петров", BaseId = -4},
                new Crewmember() {Id = -7, Fullname = "Антон Краснов", BaseId = -4},
                new Crewmember() {Id = -6, Fullname = "Артем Соловьев", BaseId = -3},
                new Crewmember() {Id = -5, Fullname = "Максим Сидоров", BaseId = -3},
                new Crewmember() {Id = -4, Fullname = "Илья Козырев", BaseId = -2},
                new Crewmember() {Id = -3, Fullname = "Андрей Морозов", BaseId = -2},
                new Crewmember() {Id = -2, Fullname = "Владимир Иванов", BaseId = -1},
                new Crewmember() {Id = -1, Fullname = "Никита Горелов", BaseId = -1},
            };
            modelBuilder.Entity<Crewmember>().HasData(crewmembers);
            Roster[] rosters = new Roster[crewmembers.Length];
            for (int i = 0; i < rosters.Length; i++)
            {
                rosters[i] = new Roster() { Id = crewmembers[i].Id };
                crewmembers[i].RosterId = rosters[i].Id;
            }

            modelBuilder.Entity<Roster>().HasData(rosters);
            modelBuilder.Entity<Permission>().HasData(new Permission[]
            {
                new Permission() {Id = -24, CrewmemberId = -8, AirplaneId = -3, FirstPilot = true, SecondPilot = true},
                new Permission() {Id = -23, CrewmemberId = -8, AirplaneId = -2, FirstPilot = true, SecondPilot = true},
                new Permission() {Id = -22, CrewmemberId = -8, AirplaneId = -1, FirstPilot = false, SecondPilot = true},
                new Permission() {Id = -21, CrewmemberId = -7, AirplaneId = -3, FirstPilot = false, SecondPilot = true},
                new Permission() {Id = -20, CrewmemberId = -7, AirplaneId = -2, FirstPilot = false, SecondPilot = true},
                new Permission() {Id = -19, CrewmemberId = -7, AirplaneId = -1, FirstPilot = false, SecondPilot = true},

                new Permission(){Id=-18,CrewmemberId = -6,AirplaneId = -3,FirstPilot = true,SecondPilot = true},
                new Permission(){Id=-17,CrewmemberId = -6,AirplaneId = -2,FirstPilot = true,SecondPilot = true},
                new Permission(){Id=-16,CrewmemberId = -6,AirplaneId = -1,FirstPilot = false,SecondPilot = true},
                new Permission(){Id=-15,CrewmemberId = -5,AirplaneId = -3,FirstPilot = false,SecondPilot = true},
                new Permission(){Id=-14,CrewmemberId = -5,AirplaneId = -2,FirstPilot = false,SecondPilot = true},
                new Permission(){Id=-13,CrewmemberId = -5,AirplaneId = -1,FirstPilot = false,SecondPilot = true},

                new Permission(){Id=-12,CrewmemberId = -4,AirplaneId = -3,FirstPilot = true,SecondPilot = true},
                new Permission(){Id=-11,CrewmemberId = -4,AirplaneId = -2,FirstPilot = true,SecondPilot = true},
                new Permission(){Id=-10,CrewmemberId = -4,AirplaneId = -1,FirstPilot = false,SecondPilot = true},
                new Permission(){Id=-9,CrewmemberId = -3,AirplaneId = -3,FirstPilot = false,SecondPilot = true},
                new Permission(){Id=-8,CrewmemberId = -3,AirplaneId = -2,FirstPilot = false,SecondPilot = true},
                new Permission(){Id=-7,CrewmemberId = -3,AirplaneId = -1,FirstPilot = false,SecondPilot = true},

                new Permission(){Id=-6,CrewmemberId = -2,AirplaneId = -3,FirstPilot = true,SecondPilot = true},
                new Permission(){Id=-5,CrewmemberId = -2,AirplaneId = -2,FirstPilot = true,SecondPilot = true},
                new Permission(){Id=-4,CrewmemberId = -2,AirplaneId = -1,FirstPilot = false,SecondPilot = true},
                new Permission(){Id=-3,CrewmemberId = -1,AirplaneId = -3,FirstPilot = false,SecondPilot = true},
                new Permission(){Id=-2,CrewmemberId = -1,AirplaneId = -2,FirstPilot = false,SecondPilot = true},
                new Permission(){Id=-1,CrewmemberId = -1,AirplaneId = -1,FirstPilot = false,SecondPilot = true},
            });
            modelBuilder.Entity<ActionType>().HasData(new ActionType[]
            {
                new ActionType(){Id =-7,Type = "Другая причина отсутствия"},
                new ActionType(){Id =-6,Type = "Отпуск"},
                new ActionType(){Id =-5,Type = "Ожидание в гостинице"},
                new ActionType(){Id =-4,Type = "Ожидание нового рейса"},
                new ActionType(){Id =-3,Type = "Занятие на тренажере"},
                new ActionType(){Id =-2,Type = "Полет первым пилотом"},
                new ActionType(){Id =-1,Type = "Полет вторым пилотом"},

            });
        }
    }

    /// <summary>
    /// Класс аэропорта
    /// </summary>
    public class Airport
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Fullname { get; set; }

        public Airport()
        {
        }

        public Airport(int id, string name, string fullname)
        {
            Id = id;
            Name = name;
            Fullname = fullname;
        }
    }

    /// <summary>
    /// Класс полета
    /// </summary>
    public class Flight
    {
        public int Id { get; set; }
        public int Num { get; set; }

        public int FromId { get; set; }
        public Airport From { get; set; }

        public int ToId { get; set; }
        public Airport To { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double Demand { get; set; }
        public double Price { get; set; }
        public int? AirplaneId { get; set; }
        public Airplane Airplane { get; set; }
        public Flight()
        {
        }

        public Flight(int id, int num, int fromId, int toId, DateTime startTime, DateTime endTime, double demand,
            double price)
        {
            this.Id = id;
            this.Num = num;
            this.FromId = fromId;
            this.ToId = toId;
            this.StartTime = startTime;
            this.EndTime = endTime;
            this.Demand = demand;
            this.Price = price;
        }
    }

    /// <summary>
    /// Класс цепочки полетов (начинается и заканчивается в одном аэропорте)
    /// </summary>
    public class AirplanePair
    {
        public int Id { get; set; }
        public List<Flight> Flights { get; }
        public int AirplaneId { get; set; }
        public Airplane Airplane { get; set; }

        public AirplanePair()
        {
            Flights = new List<Flight>();
        }

        public string ToStringPair()
        {
            string buf = "";
            for (int i = 0; i < this.Flights.Count; i++)
            {
                if (i > 0)
                {
                    buf += '-';
                }

                buf += Flights[i].Num.ToString();
            }

            return buf;
        }
    }

    /// <summary>
    /// Класс воздушного судна
    /// </summary>
    public class Airplane
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Count { get; set; }
        public double Cost { get; set; }
        public int Capacity { get; set; }
    }


    /// <summary>
    /// Оборотное время (время обсуживания)
    /// </summary>
    public class TurnTime
    {
        public int Id { get; set; }
        public int AirplaneId { get; set; }
        public virtual Airplane Airplane { get; set; }
        public int AirportId { get; set; }
        public virtual Airport Airport { get; set; }
        public TimeSpan Time { get; set; }
    }

    /// <summary>
    /// Член экипажа
    /// </summary>
    public class Crewmember
    {
        public int Id { get; set; }

        public int BaseId { get; set; }
        public Airport Base { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        [NotMapped]
        public string Fullname
        {
            get { return $"{FirstName} {LastName}"; }
            set
            {
                string[] args = value.Split();
                if (args.Length == 2)
                {
                    FirstName = args[0];
                    LastName = args[1];
                }
            }
        }
        public int? RosterId { get; set; }
        public Roster Roster { get; set; }
        public List<Permission> Permissions { get; set; }
        public Crewmember()
        {
            Permissions = new List<Permission>();
        }
    }

    /// <summary>
    /// Класс связки для членов экипажа. Содержит набор дежурств
    /// </summary>
    public class CrewmemberPair
    {
        public int Id { get; set; }
        public List<CrewmemberDuty> CrewmemberDuties { get; }
        public int? CrewmemberFirstId { get; set; }
        public Crewmember CrewmemberFirst { get; set; }
        public int? CrewmemberSecondId { get; set; }
        public Crewmember CrewmemberSecond { get; set; }
        public int? AirplaneId { get; set; }
        public Airplane Airplane { get; set; }
        public TimeSpan FlyTime { get; set; }
        public TimeSpan ElapseTime { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public CrewmemberPair()
        {
            CrewmemberDuties = new List<CrewmemberDuty>();
        }

        public string ToStringPair()
        {
            string buf = "";
            for (int i = 0; i < this.CrewmemberDuties.Count; i++)
            {
                if (i > 0)
                {
                    buf += ' ';
                }

                buf += CrewmemberDuties[i].ToStringDuty();
            }

            return buf;
        }
    }
    public class CrewmemberDuty
    {
        public int Id { get; set; }
        public List<Flight> Flights { get; set; }
        public int? AirplaneId { get; set; }
        public Airplane Airplane { get; set; }
        public TimeSpan FlyTime { get; set; }
        public TimeSpan ElapseTime { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public CrewmemberDuty()
        {
            Flights = new List<Flight>();
        }

        public string ToStringDuty()
        {
            string buf = "";
            for (int i = 0; i < this.Flights.Count; i++)
            {
                if (i > 0)
                {
                    buf += '-';
                }

                buf += Flights[i].Num.ToString();
            }

            return buf;
        }
    }
    /// <summary>
    /// Разрешение на полет
    /// </summary>
    public class Permission
    {
        public int Id { get; set; }
        public int AirplaneId { get; set; }
        public Airplane Airplane { get; set; }
        public bool FirstPilot { get; set; }
        public bool SecondPilot { get; set; }
        public int CrewmemberId { get; set; }
        public Crewmember Crewmember { get; set; }
    }

    /// <summary>
    /// Расписание каждого члена экипажа
    /// </summary>
    public class Roster
    {
        public int Id { get; set; }

        public List<Action> Actions { get; set; }
        public TimeSpan FlyTime { get; set; }
        public TimeSpan ElapseTime { get; set; }
        public Roster()
        {
            Actions = new List<Action>();
        }
    }
    /// <summary>
    /// Конкретное действие члена экипажа, с указанием типа, даты начала и даты окончания
    /// </summary>
    public class Action
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public int ActionTypeId { get; set; }
        public ActionType ActionType { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

    }

    /// <summary>
    /// Тип действия (отпуск, полет, гостиница и т.д.)
    /// </summary>
    public class ActionType
    {
        public int Id { get; set; }
        public string Type { get; set; }
    }
    public enum ActionEnum
    {
        Other = -7,
        Holiday,
        Hotel,
        WaitingNewFly,
        Training,
        FlyFirst,
        FliSecond
    }
}