using FluentValidation;

namespace DispancerPacient.Application.Patients.Commands;

public class CreatePatientValidator : AbstractValidator<CreatePatientCommand>
{
    public CreatePatientValidator()
    {
        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Фамилия обязательна.")
            .MaximumLength(100);

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Имя обязательно.")
            .MaximumLength(100);

        RuleFor(x => x.BirthDate)
            .NotEmpty().WithMessage("Дата рождения обязательна.")
            .LessThan(DateTime.Today).WithMessage("Дата рождения должна быть в прошлом.");

        RuleFor(x => x.IpnCode)
            .Length(10).When(x => !string.IsNullOrEmpty(x.IpnCode))
            .WithMessage("ИНН должен содержать 10 цифр.");

        RuleFor(x => x.DiagnosisCode)
            .Matches(@"^[A-Z]\d{2}(\.\d{1,2})?$")
            .When(x => !string.IsNullOrEmpty(x.DiagnosisCode))
            .WithMessage("Код диагноза должен соответствовать формату МКХ-10 (напр. F20.0).");
    }
}
