using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;

namespace Model
{
	class FlightValidator:AbstractValidator<Flight>
	{
		public FlightValidator()
		{
			
			RuleFor(flight => flight.FromId).NotNull().WithMessage("Укажите аэропорт отправления");
			RuleFor(flight => flight.FromId).NotEqual(flight => flight.ToId).WithMessage("Аэропорты отправления и прибытия должны различаться!");

			RuleFor(flight => flight.ToId).NotNull().WithMessage("Укажите аэропорт прибытия");
			RuleFor(flight => flight.ToId).NotEqual(flight => flight.FromId).WithMessage("Аэропорты отправления и прибытия должны различаться!");

			RuleFor(flight => flight.StartTime).LessThan(flight => flight.EndTime).WithMessage("Дата отправления должна быть меньше даты прибытия!");
			RuleFor(flight => flight.EndTime).GreaterThan(flight => flight.StartTime).WithMessage("Дата прибытия должна быть больше даты отправления!");	
		}
	}
}
