using FluentValidation;

namespace SecureConsoleFileManager.Feature.Users.CreateUser;

public class CreateUserValidator: AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        
        // Правила логина
        RuleFor(x => x.Login).NotNull().NotEmpty();
        
        // Правила пароля
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обязателен")
            .MinimumLength(8).WithMessage("Минимальная длина пароля — 8 символов")
            .Matches("[A-Z]").WithMessage("Пароль должен содержать хотя бы одну заглавную букву")
            .Matches("[a-z]").WithMessage("Пароль должен содержать хотя бы одну строчную букву")
            .Matches("[0-9]").WithMessage("Пароль должен содержать хотя бы одну цифру")
            // [^a-zA-Z0-9] означает "любой символ, кроме латинских букв и цифр"
            .Matches("[^a-zA-Z0-9]").WithMessage("Пароль должен содержать спецсимвол (!? *.)");
    }
}