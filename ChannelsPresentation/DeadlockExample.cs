using System.Diagnostics;

namespace ChannelsPresentation;

/// <summary>
/// Deadlock и её решения
/// </summary>
public class DeadlockExample
{
    private readonly object _lock1 = new();
    private readonly object _lock2 = new();

    public async Task Run()
    {
        Console.WriteLine("=== Deadlock: Проблема и решение ===\n");
        
        await DemonstrateDeadlock();
        Console.WriteLine();
        await DemonstrateSolution();
        Console.WriteLine();
        ShowDeadlockPatterns();
    }

    // ПРОБЛЕМА: Deadlock (взаимная блокировка)
    private async Task DemonstrateDeadlock()
    {
        Console.WriteLine("❌ ПРОБЛЕМА: Deadlock");
        Console.WriteLine("Два потока захватывают блокировки в разном порядке...\n");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        
        try
        {
            var task1 = Task.Run(() =>
            {
                lock (_lock1)
                {
                    Console.WriteLine("Поток 1: Захватил lock1");
                    Thread.Sleep(100); // Имитация работы
                    
                    Console.WriteLine("Поток 1: Пытается захватить lock2...");
                    lock (_lock2) // ⚠️ DEADLOCK! Поток 2 уже держит lock2
                    {
                        Console.WriteLine("Поток 1: Захватил lock2 (никогда не выполнится)");
                    }
                }
            });

            var task2 = Task.Run(() =>
            {
                lock (_lock2)
                {
                    Console.WriteLine("Поток 2: Захватил lock2");
                    Thread.Sleep(100); // Имитация работы
                    
                    Console.WriteLine("Поток 2: Пытается захватить lock1...");
                    lock (_lock1) // ⚠️ DEADLOCK! Поток 1 уже держит lock1
                    {
                        Console.WriteLine("Поток 2: Захватил lock1 (никогда не выполнится)");
                    }
                }
            });

            await Task.WhenAll(task1, task2).WaitAsync(cts.Token);
            Console.WriteLine("✅ Задачи завершились (не должно произойти при deadlock)");
        }
        catch (TimeoutException)
        {
            Console.WriteLine("⏰ TIMEOUT: Deadlock обнаружен!");
            Console.WriteLine("💀 Потоки заблокированы навсегда - классический deadlock\n");
        }
    }

    // ✅ РЕШЕНИЕ: Упорядоченный захват блокировок
    private async Task DemonstrateSolution()
    {
        Console.WriteLine("✅ РЕШЕНИЕ: Упорядоченный захват блокировок");
        Console.WriteLine("Оба потока захватывают блокировки в одинаковом порядке\n");

        var stopwatch = Stopwatch.StartNew();

        var task1 = Task.Run(() =>
        {
            // ✅ Всегда сначала lock1, потом lock2
            lock (_lock1)
            {
                Console.WriteLine("Поток 1: Захватил lock1");
                Thread.Sleep(50);
                
                lock (_lock2)
                {
                    Console.WriteLine("Поток 1: Захватил lock2");
                    Thread.Sleep(50);
                    Console.WriteLine("Поток 1: Работа завершена");
                }
            }
        });

        var task2 = Task.Run(() =>
        {
            // ✅ Тот же порядок: сначала lock1, потом lock2
            lock (_lock1)
            {
                Console.WriteLine("Поток 2: Захватил lock1");
                Thread.Sleep(50);
                
                lock (_lock2)
                {
                    Console.WriteLine("Поток 2: Захватил lock2");
                    Thread.Sleep(50);
                    Console.WriteLine("Поток 2: Работа завершена");
                }
            }
        });

        await Task.WhenAll(task1, task2);
        stopwatch.Stop();
        
        Console.WriteLine($"✅ Все задачи завершились за {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine("🎯 Deadlock предотвращен!\n");
    }

    private void ShowDeadlockPatterns()
    {
        Console.WriteLine("🔧 СПОСОБЫ ПРЕДОТВРАЩЕНИЯ DEADLOCK:\n");

        Console.WriteLine("1️⃣ УПОРЯДОЧЕННЫЙ ЗАХВАТ:");
        Console.WriteLine(@"
// ❌ Неправильно - разный порядок:
Task 1: lock(A) -> lock(B)
Task 2: lock(B) -> lock(A)

// ✅ Правильно - одинаковый порядок:
Task 1: lock(A) -> lock(B)
Task 2: lock(A) -> lock(B)");

        Console.WriteLine("\n2️⃣ TIMEOUT ПРИ ЗАХВАТЕ:");
        Console.WriteLine(@"
if (Monitor.TryEnter(lock1, TimeSpan.FromSeconds(1)))
{
    try
    {
        if (Monitor.TryEnter(lock2, TimeSpan.FromSeconds(1)))
        {
            try { /* работа */ }
            finally { Monitor.Exit(lock2); }
        }
    }
    finally { Monitor.Exit(lock1); }
}");

        Console.WriteLine("\n3️⃣ ИСПОЛЬЗОВАНИЕ SEMAPHORESLIM:");
        Console.WriteLine(@"
var semaphore = new SemaphoreSlim(1, 1);
await semaphore.WaitAsync(timeout);
try { /* работа */ }
finally { semaphore.Release(); }");

        Console.WriteLine("\n💡 ЗОЛОТЫЕ ПРАВИЛА:");
        Console.WriteLine("✅ Всегда захватывайте блокировки в одном порядке");
        Console.WriteLine("✅ Минимизируйте время удержания блокировок");
        Console.WriteLine("✅ Избегайте вложенных блокировок");
        Console.WriteLine("✅ Используйте timeout при захвате");
        Console.WriteLine("✅ Рассмотрите lock-free алгоритмы");
    }
}