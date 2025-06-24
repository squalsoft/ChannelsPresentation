
// Проверка на аргументы бенчмарка

using ChannelsPresentation;
using ChannelsPresentation.Benchmarks;

// dotnet build -c Release
// dotnet run -c Release -- benchmark dataprocessing
if (args.Length > 0 && args[0].ToLower() == "benchmark")
{
    if (args.Length > 1)
    {
        switch (args[1].ToLower())
        {
            case "dataprocessing":
                BenchmarkRunner.RunDataProcessingBenchmarks();
                return;
            case "channels":
                BenchmarkRunner.RunChannelConfigurationBenchmarks();
                return;
            case "all":
                BenchmarkRunner.RunAllBenchmarks();
                return;
            default:
                Console.WriteLine("❌ Неизвестный тип бенчмарка. Доступные опции:");
                Console.WriteLine("  - dataprocessing: Сравнение Channels, ConcurrentQueue, BlockingCollection и PLINQ");
                Console.WriteLine("  - channels: Сравнение различных конфигураций Channel");
                Console.WriteLine("  - all: Запустить все бенчмарки");
                return;
        }
    }
    else
    {
        Console.WriteLine("🚀 Доступные бенчмарки:");
        Console.WriteLine("  dotnet run -c Release -- benchmark dataprocessing");
        Console.WriteLine("  dotnet run -c Release -- benchmark channels");
        Console.WriteLine("  dotnet run -c Release -- benchmark all");
        return;
    }
}

Console.WriteLine("🎭 ChannelsPresentation Examples");
Console.WriteLine("================================");
Console.WriteLine("💡 Для запуска бенчмарков, используйте: dotnet run -- benchmark [type]");
Console.WriteLine();

//await new TplDataflowExample().RunFirstBlocks();

//await new TplDataflowExample().RunShopBlocks();

// Демонстрация Race Condition и решения
await new RaceConditionExample().Run();

// Показать типичные паттерны race condition
RaceConditionPatterns.ShowCommonPatterns();

// Решение проблемы блокировки CPU с помощью Thread.Yield
//await new ThreadYieldExample().Run();

// await new RxExample().RunSubject();
//await new RxExample().RunStockMonitor();

// ConcurrentQueue examples
//await new ConcurrentQueueExample().Run();
//await new BoundedConcurrentQueueExample(5).Run();

// BlockingCollection example
//await new BlockingCollectionExample().Run();

// Async/Await fundamentals
//await new AsyncAwaitExample().Run();

// Simple async example
//new SimpleAsyncExample().Run();

// Task.WhenAll fundamentals
//await new TaskWhenAllExample().Run();

// Comparison of different approaches
//await new ComparisonExample().Run();

//await new ActorsExample().Run();

Console.WriteLine("🏁🏁🏁 Finish 🏁🏁🏁");