using FluentValidation;

namespace MinimalnieAPI;

// Validator dla konkretnego modelu
public class ToDoValidator : AbstractValidator<ToDo> // pochodzi z paczki FluentValidation.DependencyInjectionExtensions
{
    public ToDoValidator()
    {
        RuleFor(t => t.Value)
            .NotEmpty()
            .MinimumLength(5)
            .WithMessage("Wartość musi mieć najmniej 5 znaków!");
    }
}
