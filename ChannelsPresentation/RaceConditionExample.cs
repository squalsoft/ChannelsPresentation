using System.Diagnostics;

namespace ChannelsPresentation;

/// <summary>
/// Race Condition и её решения
/// </summary>
public class  RaceConditionExample
{
    private int _counter = 0;
    private int _safeCounter = 0;
    private readonly object _lock = new();

    public async Task Run()
    {
        Console.WriteLine("=== Race Condition: Проблема и решение ===\n");
        
        await DemonstrateRaceCondition();
        Console.WriteLine();
        await DemonstrateSafeSolution();
        Console.WriteLine();
    }

    // ❌ ПРОБЛЕМА: Race Condition
    private async Task DemonstrateRaceCondition()
    {
        Console.WriteLine("❌ ПРОБЛЕМА: Race Condition");
        Console.WriteLine("Запускаем 5 потоков, каждый инкрементирует счетчик 10,000 раз");
        Console.WriteLine("Ожидаемый результат: 50,000\n");

        _counter = 0;
        var tasks = new List<Task>();

        var stopwatch = Stopwatch.StartNew();

        // Запускаем 5 потоков одновременно
        for (int i = 0; i < 5; i++)
        {
            int threadId = i + 1;
            tasks.Add(Task.Run(() => UnsafeIncrement(threadId)));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        Console.WriteLine($"Фактический результат: {_counter}");
        Console.WriteLine($"Потеряно: {50000 - _counter} операций");
    }

    private void UnsafeIncrement(int threadId)
    {
        for (int i = 0; i < 10000; i++)
        {
            // RACE CONDITION: Не атомарная операция!
            // 1. Читаем значение _counter
            // 2. Увеличиваем на 1  
            // 3. Записываем обратно
            // Между этими шагами другой поток может изменить _counter!
            _counter++;
        }
        Console.WriteLine($"🧵 Поток {threadId} завершён");
    }

    // РЕШЕНИЕ: Синхронизация с lock
    private async Task DemonstrateSafeSolution()
    {
        Console.WriteLine("✅ РЕШЕНИЕ: Синхронизация с lock");
        Console.WriteLine("Тот же тест, но с правильной синхронизацией\n");

        _safeCounter = 0;
        var tasks = new List<Task>();

        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < 5; i++)
        {
            int threadId = i + 1;
            tasks.Add(Task.Run(() => SafeIncrement(threadId)));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        Console.WriteLine($"Результат: {_safeCounter}");
        Console.WriteLine($"Корректность: {(_safeCounter == 50000 ? "ПРАВИЛЬНО" : "ОШИБКА")}");
    }

    private void SafeIncrement(int threadId)
    {
        for (int i = 0; i < 10000; i++)
        {
            // Атомарная операция с lock
            lock (_lock)
            {
                _safeCounter++;
            }
        }
        Console.WriteLine($"🔒 Безопасный поток {threadId} завершён");
    }
}