using FluentValidation;

namespace SecureConsoleFileManager.Feature.Users.CreateUser;

public class CreateUserValidator: AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        
        // Правила логина
        RuleFor(x => x.Login).NotNull().NotEmpty();
        
        // Правила пароля
        RuleFor(x => x.Password).NotNull().NotEmpty();
    }
}