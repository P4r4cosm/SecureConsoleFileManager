using FluentValidation;

namespace SecureConsoleFileManager.Feature.Users.LoginUser;

public class LoginUserValidator: AbstractValidator<LoginUserCommand>
{
    public LoginUserValidator()
    {
        // Правила логина
        RuleFor(x => x.Login).NotNull().NotEmpty();
        
        // Правила пароля
        RuleFor(x => x.Password).NotNull().NotEmpty();
    }
}