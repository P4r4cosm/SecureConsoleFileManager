using System.Reflection;
using FluentValidation;
using MediatR;
using SecureConsoleFileManager.Models;

namespace SecureConsoleFileManager.Common;


public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : MbResult
{
    // Мы получаем все валидаторы, которые есть в проекте, через DI
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Если для данной команды (TRequest) нет валидаторов, просто идем дальше
        if (!_validators.Any())
        {
            return await next();
        }

        // Создаем контекст валидации
        var context = new ValidationContext<TRequest>(request);

        // Запускаем ВСЕ валидаторы, которые подходят для этой команды
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken))
        );

        // Собираем все ошибки из всех валидаторов
        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();
        // Если нашлись ошибки...
        if (failures.Any())
        {
            var errorMessage = string.Join("\n", failures.Select(f => f.ErrorMessage));
            // Собираем все сообщения об ошибках в одну строку
            if (!typeof(TResponse).IsGenericType || typeof(TResponse).GetGenericTypeDefinition() != typeof(MbResult<>))
            {
                // Если TResponse это не-генерик MbResult, используем простой Failure
                return (TResponse)MbResult.Failure(errorMessage);
            }
            
            var resultType = typeof(TResponse).GetGenericArguments()[0];

            // 2. Ищем ВСЕ методы с именем "Failure"
            var failureMethodInfo = typeof(MbResult)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                // 3. Отбираем тот, который является универсальным определением (Generic Method Definition)
                .FirstOrDefault(m => m.Name == nameof(MbResult.Failure) && m.IsGenericMethodDefinition);

            if (failureMethodInfo == null)
            {
                // На всякий случай, если метод будет переименован
                throw new InvalidOperationException("Generic method 'Failure<T>' not found on MbResult.");
            }

            // 4. Создаем конкретный метод Failure<T>
            var genericFailureMethod = failureMethodInfo.MakeGenericMethod(resultType);

            // 5. Вызываем его
            return (TResponse)genericFailureMethod.Invoke(null, new object[] { errorMessage });
        }

        // Если ошибок нет, передаем управление следующему элементу в конвейере (или самому обработчику)
        return await next();
    }
}