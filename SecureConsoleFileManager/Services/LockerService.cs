using Microsoft.Extensions.Logging;
using SecureConsoleFileManager.Models;
using SecureConsoleFileManager.Services.Interfaces;

namespace SecureConsoleFileManager.Services;

public class LockerService(ILogger<LockerService> logger) : ILockerService
{
    private const string MutexPrefix = "Global\\FM_";
    private string GetMutexName(string fullPath) => MutexPrefix + fullPath.Replace('\\', '_').Replace('/', '_');

    public MbResult ExecuteLocked(string fullPath, Func<MbResult> operation)
    {
        string mutexName = GetMutexName(fullPath);
        using (var mutex = new Mutex(false, mutexName))
        {
            bool hasHandle = false;
            try
            {
                hasHandle = mutex.WaitOne(TimeSpan.FromSeconds(5));
                if (!hasHandle)
                {
                    return MbResult.Failure(
                        $"Ресурс  '{fullPath}' в данный момент заблокирован другой операцией.");
                }

                return operation();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ошибка при выполнении операции над ресурсом: {path}", fullPath);
                return MbResult.Failure($"Внутренняя ошибка: {e.Message}");
            }
            finally
            {
                if (hasHandle) mutex.ReleaseMutex();
            }
        }
    }

    public async Task<MbResult> ExecuteLockedAsync(string fullPath, Func<Task<MbResult>> asyncOperation)
    {
        var mutexName = GetMutexName(fullPath);
        using (var mutex = new Mutex(false, mutexName))
        {
            bool hasHandle = false;
            try
            {
                // Используем асинхронное ожидание на WaitHandle
                hasHandle = await Task.Run(() => mutex.WaitOne(TimeSpan.FromSeconds(5)));
                if (!hasHandle)
                {
                    return MbResult.Failure($"Ресурс '{fullPath}' заблокирован другой операцией.");
                }

                return await asyncOperation();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ошибка при выполнении асинхронной операции над ресурсом: {path}", fullPath);
                return MbResult.Failure($"Внутренняя ошибка: {e.Message}");
            }
            finally
            {
                if (hasHandle) mutex.ReleaseMutex();
            }
        }
    }

    public MbResult ExecuteLocked(string fullPath1, string fullPath2, Func<MbResult> operation)
    {
        var mutexName1 = GetMutexName(fullPath1);
        var mutexName2 = GetMutexName(fullPath2);

        // Гарантируем порядок захвата для предотвращения дедлоков
        if (string.Compare(mutexName1, mutexName2, StringComparison.Ordinal) > 0)
        {
            (mutexName1, mutexName2) = (mutexName2, mutexName1);
        }

        using (var mutex1 = new Mutex(false, mutexName1))
        using (var mutex2 = new Mutex(false, mutexName2))
        {
            bool hasHandle1 = false;
            bool hasHandle2 = false;
            try
            {
                hasHandle1 = mutex1.WaitOne(TimeSpan.FromSeconds(5));
                if (!hasHandle1) return MbResult.Failure("Не удалось заблокировать ресурс для операции перемещения.");

                hasHandle2 = mutex2.WaitOne(TimeSpan.FromSeconds(5));
                if (!hasHandle2)
                    return MbResult.Failure("Не удалось заблокировать целевой ресурс для операции перемещения.");

                return operation();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ошибка при выполнении операции над двумя ресурсами: {path1}, {path2}", fullPath1,
                    fullPath2);
                return MbResult.Failure($"Внутренняя ошибка: {e.Message}");
            }
            finally
            {
                if (hasHandle2) mutex2.ReleaseMutex();
                if (hasHandle1) mutex1.ReleaseMutex();
            }
        }
    }
}