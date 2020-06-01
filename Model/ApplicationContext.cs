using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
            modelBuilder
                .Entity<Flight>()
                .HasIndex(f => f.Num)
                .IsUnique();
            modelBuilder
                .Entity<Airport>()
                .HasIndex(a=>a.Name)
                .IsUnique();
            modelBuilder
                .Entity<Airplane>()
                .HasIndex(a=>a.Name)
                .IsUnique();
            modelBuilder.Entity<Airport>().Property(a => a.Name).HasMaxLength(3);
            Flight[] flights;
            List<Flight> allFlights = new List<Flight>();
            for (int day = 0; day < 10; day++)
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
                new Crewmember() {Id = -8, Fullname = "Иван Петров Григорьевич", BaseId = -4},
                new Crewmember() {Id = -7, Fullname = "Антон Краснов Григорьевич", BaseId = -4},
                new Crewmember() {Id = -6, Fullname = "Артем Соловьев Григорьевич", BaseId = -3},
                new Crewmember() {Id = -5, Fullname = "Максим Сидоров Григорьевич", BaseId = -3},
                new Crewmember() {Id = -4, Fullname = "Илья Козырев Григорьевич", BaseId = -2},
                new Crewmember() {Id = -3, Fullname = "Андрей Морозов Григорьевич", BaseId = -2},
                new Crewmember() {Id = -2, Fullname = "Владимир Иванов Григорьевич", BaseId = -1},
                new Crewmember() {Id = -1, Fullname = "Никита Горелов Григорьевич", BaseId = -1},
            };
            modelBuilder.Entity<Crewmember>().HasData(crewmembers);
            Roster[] rosters = new Roster[crewmembers.Length];
            for (int i = 0; i < rosters.Length; i++)
            {
                rosters[i] = new Roster() {Id = crewmembers[i].Id};
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

                new Permission() {Id = -18, CrewmemberId = -6, AirplaneId = -3, FirstPilot = true, SecondPilot = true},
                new Permission() {Id = -17, CrewmemberId = -6, AirplaneId = -2, FirstPilot = true, SecondPilot = true},
                new Permission() {Id = -16, CrewmemberId = -6, AirplaneId = -1, FirstPilot = false, SecondPilot = true},
                new Permission() {Id = -15, CrewmemberId = -5, AirplaneId = -3, FirstPilot = false, SecondPilot = true},
                new Permission() {Id = -14, CrewmemberId = -5, AirplaneId = -2, FirstPilot = false, SecondPilot = true},
                new Permission() {Id = -13, CrewmemberId = -5, AirplaneId = -1, FirstPilot = false, SecondPilot = true},

                new Permission() {Id = -12, CrewmemberId = -4, AirplaneId = -3, FirstPilot = true, SecondPilot = true},
                new Permission() {Id = -11, CrewmemberId = -4, AirplaneId = -2, FirstPilot = true, SecondPilot = true},
                new Permission() {Id = -10, CrewmemberId = -4, AirplaneId = -1, FirstPilot = false, SecondPilot = true},
                new Permission() {Id = -9, CrewmemberId = -3, AirplaneId = -3, FirstPilot = false, SecondPilot = true},
                new Permission() {Id = -8, CrewmemberId = -3, AirplaneId = -2, FirstPilot = false, SecondPilot = true},
                new Permission() {Id = -7, CrewmemberId = -3, AirplaneId = -1, FirstPilot = false, SecondPilot = true},

                new Permission() {Id = -6, CrewmemberId = -2, AirplaneId = -3, FirstPilot = true, SecondPilot = true},
                new Permission() {Id = -5, CrewmemberId = -2, AirplaneId = -2, FirstPilot = true, SecondPilot = true},
                new Permission() {Id = -4, CrewmemberId = -2, AirplaneId = -1, FirstPilot = false, SecondPilot = true},
                new Permission() {Id = -3, CrewmemberId = -1, AirplaneId = -3, FirstPilot = false, SecondPilot = true},
                new Permission() {Id = -2, CrewmemberId = -1, AirplaneId = -2, FirstPilot = false, SecondPilot = true},
                new Permission() {Id = -1, CrewmemberId = -1, AirplaneId = -1, FirstPilot = false, SecondPilot = true},
            });
            modelBuilder.Entity<ActionType>().HasData(new ActionType[]
            {
                new ActionType() {Id = -7, Type = "Другая причина отсутствия"},
                new ActionType() {Id = -6, Type = "Отпуск"},
                new ActionType() {Id = -5, Type = "Ожидание в гостинице"},
                new ActionType() {Id = -4, Type = "Ожидание нового рейса"},
                new ActionType() {Id = -3, Type = "Занятие на тренажере"},
                new ActionType() {Id = -2, Type = "Полет первым пилотом"},
                new ActionType() {Id = -1, Type = "Полет вторым пилотом"},
            });
        }
    }

    /// <summary>
    /// Класс аэропорта
    /// </summary>
    public class Airport : INotifyPropertyChanged
    {
        private int _id;
        private string _name;
        private string _fullname;

        public int Id
        {
            get => _id;
            set
            {
                if (_id == value) return;
                _id = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                if (_name == value) return;
                _name = value;
                OnPropertyChanged();
            }
        }

        public string Fullname
        {
            get => _fullname;
            set
            {
                if (_fullname == value) return;
                _fullname = value;
                OnPropertyChanged();
            }
        }
        public virtual IList<TurnTime> TurnTimes { get; }

        public Airport()
        {
            TurnTimes = new ObservableCollection<TurnTime>();
        }

        public Airport(int id, string name, string fullname)
        {
            Id = id;
            Name = name;
            Fullname = fullname;
            TurnTimes = new ObservableCollection<TurnTime>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }

    /// <summary>
    /// Класс полета
    /// </summary>
    public class Flight : INotifyPropertyChanged, IDataErrorInfo
    {
        private readonly FlightValidator _validator;
        private int _id;
        private int _num;
        private int? _fromId;
        private Airport _from;
        private int? _toId;
        private Airport _to;
        private DateTime _startTime;
        private DateTime _endTime;
        private double _demand;
        private double _price;
        private int? _airplaneId;
        private Airplane _airplane;

        public int Id
        {
            get => _id;
            set
            {
                if (_id == value) return;
                _id = value;
                OnPropertyChanged();
            }
        }

        public int Num
        {
            get => _num;
            set
            {
                if (_num == value) return;
                _num = value;
                OnPropertyChanged();
            }
        }

        public int? FromId
        {
            get => _fromId;
            set
            {
                if (_fromId == value) return;
                _fromId = value;
                OnPropertyChanged();
            }
        }

        public virtual Airport From
        {
            get => _from;
            set
            {
                if (_from == value) return;
                _from = value;
                OnPropertyChanged();
            }
        }

        public int? ToId
        {
            get => _toId;
            set
            {
                if (_toId == value) return;
                _toId = value;
                OnPropertyChanged();
            }
        }

        public virtual Airport To
        {
            get => _to;
            set
            {
                if (_to == value) return;
                _to = value;
                OnPropertyChanged();
            }
        }

        public DateTime StartTime
        {
            get => _startTime;
            set
            {
                if (_startTime == value) return;
                _startTime = value;
                OnPropertyChanged();
            }
        }

        public DateTime EndTime
        {
            get => _endTime;
            set
            {
                if (_endTime == value) return;
                _endTime = value;
                OnPropertyChanged();
            }
        }

        public double Demand
        {
            get => _demand;
            set
            {
                if (_demand == value) return;
                _demand = value;
                OnPropertyChanged();
            }
        }

        public double Price
        {
            get => _price;
            set
            {
                if (_price == value) return;
                _price = value;
                OnPropertyChanged();
            }
        }

        public int? AirplaneId
        {
            get => _airplaneId;
            set
            {
                if (_airplaneId == value) return;
                _airplaneId = value;
                OnPropertyChanged();
            }
        }

        public virtual Airplane Airplane
        {
            get => _airplane;
            set
            {
                if (_airplane == value) return;
                _airplane = value;
                OnPropertyChanged();
            }
        }

        public Flight()
        {
            _validator = new FlightValidator();
        }

        public Flight(int id, int num, int fromId, int toId, DateTime startTime, DateTime endTime, double demand,
            double price)
        {
            _validator = new FlightValidator();
            this.Id = id;
            this.Num = num;
            this.FromId = fromId;
            this.ToId = toId;
            this.StartTime = startTime;
            this.EndTime = endTime;
            this.Demand = demand;
            this.Price = price;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        public string this[string columnName]
        {
            get
            {
                var firstOrDefault = _validator.Validate(this).Errors.FirstOrDefault(lol => lol.PropertyName == columnName);
                OnPropertyChanged("IsValid");
                if (firstOrDefault != null)
                    return _validator != null ? firstOrDefault.ErrorMessage : "";
                return "";
            }
        }
        [NotMapped]
        public string Error
        {
            get
            {
                if (_validator != null)
                {
                    var results = _validator.Validate(this);
                    if (results != null && results.Errors.Any())
                    {
                        var errors = string.Join(Environment.NewLine, results.Errors.Select(x => x.ErrorMessage).ToArray());
                        return errors;
                    }
                }
                return string.Empty;
            }
        }

        [NotMapped]
        public bool IsValid => _validator.Validate(this).Errors.Count == 0;
    }

    /// <summary>
    /// Класс цепочки полетов (начинается и заканчивается в одном аэропорте)
    /// </summary>
    public class AirplanePair : INotifyPropertyChanged
    {
        private int _id;
        private int _airplaneId;
        private Airplane _airplane;

        public int Id
        {
            get => _id;
            set
            {
                if (_id == value) return;
                _id = value;
                OnPropertyChanged();
            }
        }

        public virtual IList<Flight> Flights { get; }

        public int AirplaneId
        {
            get => _airplaneId;
            set
            {
                if (_airplaneId == value) return;
                _airplaneId = value;
                OnPropertyChanged();
            }
        }

        public virtual Airplane Airplane
        {
            get => _airplane;
            set
            {
                if (_airplane == value) return;
                _airplane = value;
                OnPropertyChanged();
            }
        }

        public AirplanePair()
        {
            Flights = new ObservableCollection<Flight>();
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

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }

    /// <summary>
    /// Класс воздушного судна
    /// </summary>
    public class Airplane : INotifyPropertyChanged
    {
        private int _id;
        private string _name;
        private int _count;
        private double _cost;
        private int _capacity;

        public Airplane()
		{
            TurnTimes = new ObservableCollection<TurnTime>();
		}
        public int Id
        {
            get => _id;
            set
            {
                if (_id == value) return;
                _id = value;
                OnPropertyChanged();
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                if (_name == value) return;
                _name = value;
                OnPropertyChanged();
            }
        }

        public int Count
        {
            get => _count;
            set
            {
                if (_count == value) return;
                _count = value;
                OnPropertyChanged();
            }
        }

        public double Cost
        {
            get => _cost;
            set
            {
                if (_cost == value) return;
                _cost = value;
                OnPropertyChanged();
            }
        }

        public int Capacity
        {
            get => _capacity;
            set
            {
                if (_capacity == value) return;
                _capacity = value;
                OnPropertyChanged();
            }
        }
        public virtual IList<TurnTime> TurnTimes { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }


    /// <summary>
    /// Оборотное время (время обсуживания)
    /// </summary>
    public class TurnTime : INotifyPropertyChanged
    {
        private int _id;
        private int _airplaneId;
        private Airplane _airplane;
        private int _airportId;
        private Airport _airport;
        private TimeSpan _time;

        public int Id
        {
            get => _id;
            set
            {
                if (_id == value) return;
                _id = value;
                OnPropertyChanged();
            }
        }

        public int AirplaneId
        {
            get => _airplaneId;
            set
            {
                if (_airplaneId == value) return;
                _airplaneId = value;
                OnPropertyChanged();
            }
        }

        public virtual Airplane Airplane
        {
            get => _airplane;
            set
            {
                if (_airplane == value) return;
                _airplane = value;
                OnPropertyChanged();
            }
        }

        public int AirportId
        {
            get => _airportId;
            set
            {
                if (_airportId == value) return;
                _airportId = value;
                OnPropertyChanged();
            }
        }

        public virtual Airport Airport
        {
            get => _airport;
            set
            {
                if (_airport == value) return;
                _airport = value;
                OnPropertyChanged();
            }
        }

        public TimeSpan Time
        {
            get => _time;
            set
            {
                if (_time == value) return;
                _time = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }

	/// <summary>
	/// Член экипажа
	/// </summary>
	public class Crewmember : INotifyPropertyChanged
	{
		private int _id;
		private int _baseId;
		private Airport _base;
		private string _firstName;
        private string _secondName;
        private string _lastName;
		private int? _rosterId;
		private Roster _roster;
		
		public int Id
		{
			get => _id;
			set
			{
				if (_id == value) return;
				_id = value;
				OnPropertyChanged();
			}
		}

		public int BaseId
		{
			get => _baseId;
			set
			{
				if (_baseId == value) return;
				_baseId = value;
				OnPropertyChanged();
			}
		}

		public virtual Airport Base
		{
			get => _base;
			set
			{
				if (_base == value) return;
				_base = value;
				OnPropertyChanged();
			}
		}

		public string FirstName
		{
			get => _firstName;
			set
			{
				if (_firstName == value) return;
				_firstName = value;
				OnPropertyChanged();
			}
		}
		public string SecondName
        {
            get => _secondName; set
            {
                if (_secondName == value) return;
                _secondName = value;
                OnPropertyChanged();
            }
        }
		public string LastName
		{
			get => _lastName;
			set
			{
				if (_lastName == value) return;
				_lastName = value;
				OnPropertyChanged();
			}
		}

		[NotMapped]
		public string Fullname
		{
			get { return $"{FirstName} {SecondName} {LastName}"; }
			set
			{
				string[] args = value.Split();
				if (args.Length == 3)
				{
					FirstName = args[0];
					SecondName = args[1];
					LastName = args[2];
				}

				OnPropertyChanged();
			}
		}

		public int? RosterId
		{
			get => _rosterId;
			set
			{
				if (_rosterId == value) return;
				_rosterId = value;
				OnPropertyChanged();
			}
		}

		public virtual Roster Roster
		{
			get => _roster;
			set
			{
				if (_roster == value) return;
				_roster = value;
				OnPropertyChanged();
			}
		}

		public virtual IList<Permission> Permissions { get; set; }

		public Crewmember()
		{
			Permissions = new ObservableCollection<Permission>();
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public void OnPropertyChanged([CallerMemberName] string prop = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
		}
	}

	/// <summary>
	/// Класс связки для членов экипажа. Содержит набор дежурств
	/// </summary>
	public class CrewmemberPair : INotifyPropertyChanged
    {
        private int _id;
        private int? _crewmemberFirstId;
        private Crewmember _crewmemberFirst;
        private int? _crewmemberSecondId;
        private Crewmember _crewmemberSecond;
        private int? _airplaneId;
        private Airplane _airplane;
        private TimeSpan _flyTime;
        private TimeSpan _elapseTime;
        private DateTime _startTime;
        private DateTime _endTime;

        public int Id
        {
            get => _id;
            set
            {
                if (_id == value) return;
                _id = value;
                OnPropertyChanged();
            }
        }

        public virtual IList<CrewmemberDuty> CrewmemberDuties { get; }

        public int? CrewmemberFirstId
        {
            get => _crewmemberFirstId;
            set
            {
                if (_crewmemberFirstId == value) return;
                _crewmemberFirstId = value;
                OnPropertyChanged();
            }
        }

        public virtual Crewmember CrewmemberFirst
        {
            get => _crewmemberFirst;
            set
            {
                if (_crewmemberFirst == value) return;
                _crewmemberFirst = value;
                OnPropertyChanged();
            }
        }

        public int? CrewmemberSecondId
        {
            get => _crewmemberSecondId;
            set
            {
                if (_crewmemberSecondId == value) return;
                _crewmemberSecondId = value;
                OnPropertyChanged();
            }
        }

        public Crewmember CrewmemberSecond
        {
            get => _crewmemberSecond;
            set
            {
                if (_crewmemberSecond == value) return;
                _crewmemberSecond = value;
                OnPropertyChanged();
            }
        }

        public int? AirplaneId
        {
            get => _airplaneId;
            set
            {
                if (_airplaneId == value) return;
                _airplaneId = value;
                OnPropertyChanged();
            }
        }

        public virtual Airplane Airplane
        {
            get => _airplane;
            set
            {
                if (_airplane == value) return;
                _airplane = value;
                OnPropertyChanged();
            }
        }

        public TimeSpan FlyTime
        {
            get => _flyTime;
            set
            {
                if (_flyTime == value) return;
                _flyTime = value;
                OnPropertyChanged();
            }
        }

        public TimeSpan ElapseTime
        {
            get => _elapseTime;
            set
            {
                if (_elapseTime == value) return;
                _elapseTime = value;
                OnPropertyChanged();
            }
        }

        public DateTime StartTime
        {
            get => _startTime;
            set
            {
                if (_startTime == value) return;
                _startTime = value;
                OnPropertyChanged();
            }
        }

        public DateTime EndTime
        {
            get => _endTime;
            set
            {
                if (_endTime == value) return;
                _endTime = value;
                OnPropertyChanged();
            }
        }

        public CrewmemberPair()
        {
            CrewmemberDuties = new ObservableCollection<CrewmemberDuty>();
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

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }

    public class CrewmemberDuty : INotifyPropertyChanged
    {
        private int _id;
        private int? _airplaneId;
        private Airplane _airplane;
        private TimeSpan _flyTime;
        private TimeSpan _elapseTime;
        private DateTime _startTime;
        private DateTime _endTime;

        public int Id
        {
            get => _id;
            set
            {
                if (_id == value) return;
                _id = value;
                OnPropertyChanged();
            }
        }

        public IList<Flight> Flights { get; set; }

        public int? AirplaneId
        {
            get => _airplaneId;
            set
            {
                if (_airplaneId == value) return;
                _airplaneId = value;
                OnPropertyChanged();
            }
        }

        public virtual Airplane Airplane
        {
            get => _airplane;
            set
            {
                if (_airplane == value) return;
                _airplane = value;
                OnPropertyChanged();
            }
        }

        public TimeSpan FlyTime
        {
            get => _flyTime;
            set
            {
                if (_flyTime == value) return;
                _flyTime = value;
                OnPropertyChanged();
            }
        }

        public TimeSpan ElapseTime
        {
            get => _elapseTime;
            set
            {
                if (_elapseTime == value) return;
                _elapseTime = value;
                OnPropertyChanged();
            }
        }

        public DateTime StartTime
        {
            get => _startTime;
            set
            {
                if (_startTime == value) return;
                _startTime = value;
                OnPropertyChanged();
            }
        }

        public DateTime EndTime
        {
            get => _endTime;
            set
            {
                if (_endTime == value) return;
                _endTime = value;
                OnPropertyChanged();
            }
        }

        public CrewmemberDuty()
        {
            Flights = new ObservableCollection<Flight>();
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

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }

    /// <summary>
    /// Разрешение на полет
    /// </summary>
    public class Permission : INotifyPropertyChanged
    {
        private int _id;
        private int _airplaneId;
        private Airplane _airplane;
        private bool _firstPilot;
        private bool _secondPilot;
        private int _crewmemberId;

        public int Id
        {
            get => _id;
            set
            {
                if (_id == value) return;
                _id = value;
                OnPropertyChanged();
            }
        }

        public int AirplaneId
        {
            get => _airplaneId;
            set
            {
                if (_airplaneId == value) return;
                _airplaneId = value;
                OnPropertyChanged();
            }
        }

        public virtual Airplane Airplane
        {
            get => _airplane;
            set
            {
                if (_airplane == value) return;
                _airplane = value;
                OnPropertyChanged();
            }
        }

        public bool FirstPilot
        {
            get => _firstPilot;
            set
            {
                if (_firstPilot == value) return;
                _firstPilot = value;
                OnPropertyChanged();
            }
        }

        public bool SecondPilot
        {
            get => _secondPilot;
            set
            {
                if (_secondPilot == value) return;
                _secondPilot = value;
                OnPropertyChanged();
            }
        }

        public int CrewmemberId
        {
            get => _crewmemberId;
            set
            {
                if (_crewmemberId == value) return;
                _crewmemberId = value;
                OnPropertyChanged();
            }
        }

        public virtual Crewmember Crewmember { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }

    /// <summary>
    /// Расписание каждого члена экипажа
    /// </summary>
    public class Roster : INotifyPropertyChanged
    {
        private int _id;
        private TimeSpan _flyTime;
        private TimeSpan _elapseTime;

        public int Id
        {
            get => _id;
            set
            {
                if (_id == value) return;
                _id = value;
                OnPropertyChanged();
            }
        }

        public IList<Action> Actions { get; set; }

        public TimeSpan FlyTime
        {
            get => _flyTime;
            set
            {
                if (_flyTime == value) return;
                _flyTime = value;
                OnPropertyChanged();
            }
        }

        public TimeSpan ElapseTime
        {
            get => _elapseTime;
            set
            {
                if (_elapseTime == value) return;
                _elapseTime = value;
                OnPropertyChanged();
            }
        }

        public Roster()
        {
            Actions = new ObservableCollection<Action>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }

    /// <summary>
    /// Конкретное действие члена экипажа, с указанием типа, даты начала и даты окончания
    /// </summary>
    public class Action : INotifyPropertyChanged
    {
        private int _id;
        private string _description;
        private int _actionTypeId;
        private ActionType _actionType;
        private DateTime _startTime;
        private DateTime _endTime;

        public int Id
        {
            get => _id;
            set
            {
                if (_id == value) return;
                _id = value;
                OnPropertyChanged();
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (_description == value) return;
                _description = value;
                OnPropertyChanged();
            }
        }

        public int ActionTypeId
        {
            get => _actionTypeId;
            set
            {
                if (_actionTypeId == value) return;
                _actionTypeId = value;
                OnPropertyChanged();
            }
        }

        public virtual ActionType ActionType
        {
            get => _actionType;
            set
            {
                if (_actionType == value) return;
                _actionType = value;
                OnPropertyChanged();
            }
        }

        public DateTime StartTime
        {
            get => _startTime;
            set
            {
                if (_startTime == value) return;
                _startTime = value;
                OnPropertyChanged();
            }
        }

        public DateTime EndTime
        {
            get => _endTime;
            set
            {
                if (_endTime == value) return;
                _endTime = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }

    /// <summary>
    /// Тип действия (отпуск, полет, гостиница и т.д.)
    /// </summary>
    public class ActionType : INotifyPropertyChanged
    {
        private int _id;
        private string _type;

        public int Id
        {
            get => _id;
            set
            {
                if (_id == value) return;
                _id = value;
                OnPropertyChanged();
            }
        }

        public string Type
        {
            get => _type;
            set
            {
                if (_type == value) return;
                _type = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }

    /// <summary>
    /// Перечисление всех возможных событий расписания.
    /// </summary>
    public enum ActionEnum
    {
        Other = -7,
        Holiday,
        Hotel,
        WaitingNewFly,
        Training,
        FlyFirst,
        FlySecond
    }
}