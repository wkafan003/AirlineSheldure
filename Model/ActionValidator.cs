using FluentValidation;

namespace Model
{
    public class ActionValidator:AbstractValidator<Action>
    {
        public ActionValidator()
        {
            RuleFor(action => action.StartTime).LessThan(action => action.EndTime).WithMessage("Дата начала должна быть меньше даты окончания!");
            RuleFor(action => action.EndTime).GreaterThan(action => action.StartTime).WithMessage("Дата окончания должна быть больше даты начала!");	
        }
    }
}