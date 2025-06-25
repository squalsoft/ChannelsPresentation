using Akka.Actor;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Channels;
using System.Threading.Tasks.Dataflow;

namespace ChannelsPresentation.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class DataProcessingBenchmarks
{
    private const int ItemCount = 10000;
    private readonly int[] _data;

    public DataProcessingBenchmarks()
    {
        _data = Enumerable.Range(1, ItemCount).ToArray();
    }

    // Базовый бенчмарк - Channels
    [Benchmark(Baseline = true)]
    public async Task ProcessWithChannels()
    {
        var channel = Channel.CreateUnbounded<int>();
        var writer = channel.Writer;
        var reader = channel.Reader;

        // Producer task
        var producer = Task.Run(async () =>
        {
            foreach (var item in _data)
            {
                await writer.WriteAsync(item);
            }
            writer.Complete();
        });

        // Consumer task
        var consumer = Task.Run(async () =>
        {
            var results = new List<int>();
            await foreach (var item in reader.ReadAllAsync())
            {
                results.Add(item * 2); // Простая обработка
            }
            return results;
        });

        await Task.WhenAll(producer, consumer);
    }

    [Benchmark]
    public async Task ProcessWithConcurrentQueue()
    {
        var queue = new ConcurrentQueue<int>();
        var completed = false;

        // Producer task
        var producer = Task.Run(() =>
        {
            foreach (var item in _data)
            {
                queue.Enqueue(item);
            }
            completed = true;
        });

        // Consumer task
        var consumer = Task.Run(() =>
        {
            var results = new List<int>();
            while (!completed || !queue.IsEmpty)
            {
                if (queue.TryDequeue(out var item))
                {
                    results.Add(item * 2); // Простая обработка
                }
                else
                {
                    Thread.Yield();
                }
            }
            return results;
        });

        await Task.WhenAll(producer, consumer);
    }

    [Benchmark]
    public async Task ProcessWithBoundedChannel()
    {
        var options = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };
        var channel = Channel.CreateBounded<int>(options);
        var writer = channel.Writer;
        var reader = channel.Reader;

        // Producer task
        var producer = Task.Run(async () =>
        {
            foreach (var item in _data)
            {
                await writer.WriteAsync(item);
            }
            writer.Complete();
        });

        // Consumer task
        var consumer = Task.Run(async () =>
        {
            var results = new List<int>();
            await foreach (var item in reader.ReadAllAsync())
            {
                results.Add(item * 2); // Простая обработка
            }
            return results;
        });

        await Task.WhenAll(producer, consumer);
    }

    [Benchmark]
    public async Task ProcessWithBlockingCollection()
    {
        using var collection = new BlockingCollection<int>();

        // Producer task
        var producer = Task.Run(() =>
        {
            foreach (var item in _data)
            {
                collection.Add(item);
            }
            collection.CompleteAdding();
        });

        // Consumer task
        var consumer = Task.Run(() =>
        {
            var results = new List<int>();
            foreach (var item in collection.GetConsumingEnumerable())
            {
                results.Add(item * 2); // Простая обработка
            }
            return results;
        });

        await Task.WhenAll(producer, consumer);
    }    

    [Benchmark]
    public async Task ProcessWithLockAndList()
    {
        var list = new List<int>();
        var lockObject = new object();

        // Producer task
        var producer = Task.Run(() =>
        {
            foreach (var item in _data)
            {
                lock (lockObject)
                {
                    list.Add(item);
                }
            }
        });

        // Consumer task
        var consumer = Task.Run(() =>
        {
            var results = new List<int>();
            while (true)
            {
                int item;
                lock (lockObject)
                {
                    if (list.Count == 0)
                    {
                        if (producer.IsCompleted)
                            break;
                        continue;
                    }
                    item = list[0];
                    list.RemoveAt(0);
                }
                results.Add(item * 2); // Простая обработка
            }
            return results;
        });

        await Task.WhenAll(producer, consumer);
    }

    [Benchmark]
    public async Task ProcessWithTplDataflow()
    {
        var bufferBlock = new BufferBlock<int>();
        var transformBlock = new TransformBlock<int, int>(x => x * 2);
        var actionBlock = new ActionBlock<int>(_ => { /* Обработка завершена */ });

        // Соединяем блоки
        bufferBlock.LinkTo(transformBlock, new DataflowLinkOptions { PropagateCompletion = true });
        transformBlock.LinkTo(actionBlock, new DataflowLinkOptions { PropagateCompletion = true });

        // Producer task
        var producer = Task.Run(async () =>
        {
            foreach (var item in _data)
            {
                await bufferBlock.SendAsync(item);
            }
            bufferBlock.Complete();
        });

        // Ждем завершения всех блоков
        await Task.WhenAll(producer, actionBlock.Completion);
    }

    [Benchmark]
    public async Task ProcessWithRx()
    {
        var subject = new Subject<int>();
        var results = new List<int>();
        
        // Подписываемся на поток данных
        var subscription = subject
            .Select(x => x * 2) // Трансформация
            .Subscribe(
                result => 
                {
                    lock (results)
                    {
                        results.Add(result);
                    }
                }, // onNext
                ex => { }, // onError
                () => { }  // onCompleted
            );

        // Producer task
        var producer = Task.Run(() =>
        {
            foreach (var item in _data)
            {
                subject.OnNext(item);
            }
            subject.OnCompleted();
        });

        // Ждем завершения producer
        await producer;
        
        // Отписываемся
        subscription.Dispose();
    }

    [Benchmark]
    public async Task ProcessWithAkkaActors()
    {
        var system = ActorSystem.Create("BenchmarkSystem");
        var actorRef = system.ActorOf<ProcessingActor>("processor");
        
        var completionSource = new TaskCompletionSource<bool>();
        
        // Отправляем данные актору
        foreach (var item in _data)
        {
            actorRef.Tell(new ProcessItem(item));
        }
        
        // Отправляем сигнал завершения
        actorRef.Tell(new CompleteProcessing(completionSource));
        
        // Ждем завершения обработки
        await completionSource.Task;
        
        // Останавливаем систему акторов
        await system.Terminate();
    }

    // Актор для обработки данных
    public class ProcessingActor : ReceiveActor
    {
        public ProcessingActor()
        {
            var results = new List<int>();
            Receive<ProcessItem>(item =>
            {
                // Трансформация данных
                var result = item.Value * 2;
                results.Add(result);
                // В реальном приложении здесь была бы дополнительная логика
            });
            
            Receive<CompleteProcessing>(complete =>
            {
                complete.CompletionSource.SetResult(true);
            });
        }
    }

    // Сообщения для актора
    public class ProcessItem
    {
        public int Value { get; }
        public ProcessItem(int value) => Value = value;
    }

    public class CompleteProcessing
    {
        public TaskCompletionSource<bool> CompletionSource { get; }
        public CompleteProcessing(TaskCompletionSource<bool> completionSource) 
            => CompletionSource = completionSource;
    }

// | Method                        | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0     | Gen1     | Gen2    | Allocated  | Alloc Ratio |
// |------------------------------ |-----------:|----------:|----------:|------:|--------:|---------:|---------:|--------:|-----------:|------------:|
// | ProcessWithChannels           |   490.4 us |   9.54 us |  26.28 us |  1.00 |    0.00 |  34.1797 |   8.7891 |  0.9766 |  199.69 KB |        1.00 |
// | ProcessWithConcurrentQueue    |   188.0 us |   3.29 us |   4.92 us |  0.39 |    0.03 |  24.9023 |   6.1035 |       - |  146.69 KB |        0.73 |
// | ProcessWithBoundedChannel     |   925.2 us |  17.33 us |  18.54 us |  1.91 |    0.13 |  22.4609 |   3.9063 |       - |  138.75 KB |        0.69 |
// | ProcessWithBlockingCollection | 1,365.0 us |  28.54 us |  83.69 us |  2.79 |    0.23 |  25.3906 |   5.8594 |       - |  153.97 KB |        0.77 |
// | ProcessWithLockAndList        | 1,320.7 us |  18.60 us |  15.53 us |  2.70 |    0.22 |  27.3438 |   5.8594 |       - |  161.33 KB |        0.81 |
// | ProcessWithTplDataflow        | 1,814.7 us |  35.72 us |  73.78 us |  3.75 |    0.25 |  39.0625 |  11.7188 |       - |  227.88 KB |        1.14 |
// | ProcessWithRx                 |   158.6 us |   3.02 us |   3.36 us |  0.33 |    0.02 |  20.9961 |   4.3945 |       - |  128.98 KB |        0.65 |
// | ProcessWithAkkaActors         | 3,090.8 us | 118.17 us | 344.70 us |  6.27 |    0.75 | 476.5625 | 109.3750 | 93.7500 | 2991.99 KB |       14.98 |
}

[MemoryDiagnoser]
[SimpleJob]
public class ChannelConfigurationBenchmarks
{
    private const int ItemCount = 5000;
    private readonly int[] _data;

    public ChannelConfigurationBenchmarks()
    {
        _data = Enumerable.Range(1, ItemCount).ToArray();
    }

    [Benchmark(Baseline = true)]
    public async Task UnboundedChannel()
    {
        var channel = Channel.CreateUnbounded<int>(); // Под капотом ConcurrentQueue
        await ProcessChannel(channel);
    }

    [Benchmark]
    [Arguments(100)]
    [Arguments(500)]
    [Arguments(1000)]
    public async Task BoundedChannel(int capacity)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };
        var channel = Channel.CreateBounded<int>(options);
        await ProcessChannel(channel);
    }

    [Benchmark]
    [Arguments(100)]
    [Arguments(500)]
    [Arguments(1000)]
    public async Task BoundedChannelDropOldest(int capacity)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = false,
            SingleWriter = false
        };
        var channel = Channel.CreateBounded<int>(options);
        await ProcessChannel(channel);
    }

    private async Task ProcessChannel(Channel<int> channel)
    {
        var writer = channel.Writer;
        var reader = channel.Reader;

        var producer = Task.Run(async () =>
        {
            try
            {
                foreach (var item in _data)
                {
                    await writer.WriteAsync(item);
                }
            }
            finally
            {
                writer.Complete();
            }
        });

        var consumer = Task.Run(async () =>
        {
            var results = new List<int>();
            await foreach (var item in reader.ReadAllAsync())
            {
                results.Add(item * item); // Обработка
                await Task.Delay(1); // Имитация работы
            }
            return results;
        });

        await Task.WhenAll(producer, consumer);
    }


// | Method                   | capacity | Mean       | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
// |------------------------- |--------- |-----------:|----------:|----------:|------:|--------:|----------:|------------:|
// | UnboundedChannel         | ?        | 6,635.9 ms | 131.35 ms | 188.38 ms |  1.00 |    0.00 | 954.64 KB |        1.00 |
// |                          |          |            |           |           |       |         |           |             |
// | BoundedChannel           | 100      | 6,693.2 ms | 129.94 ms | 344.59 ms |     ? |       ? | 890.05 KB |           ? |
// | BoundedChannelDropOldest | 100      |   142.4 ms |   4.53 ms |  13.28 ms |     ? |       ? |  21.23 KB |           ? |
// |                          |          |            |           |           |       |         |           |             |
// | BoundedChannel           | 500      | 6,188.6 ms | 123.56 ms | 173.22 ms |     ? |       ? | 892.16 KB |           ? |
// | BoundedChannelDropOldest | 500      |   618.1 ms |  12.13 ms |  16.61 ms |     ? |       ? |  93.78 KB |           ? |
// |                          |          |            |           |           |       |         |           |             |
// | BoundedChannel           | 1000     | 6,424.3 ms | 127.39 ms | 337.81 ms |     ? |       ? | 896.47 KB |           ? |
// | BoundedChannelDropOldest | 1000     | 1,306.8 ms |  27.43 ms |  80.01 ms |     ? |       ? | 183.86 KB |           ? |
}

[MemoryDiagnoser]
[SimpleJob]
public class AsyncVsSyncBenchmarks
{
    private const int ItemCount = 10000;
    private readonly int[] _data;
    private static readonly ConcurrentBag<int> _usedThreadIds = new();

    public AsyncVsSyncBenchmarks()
    {
        _data = Enumerable.Range(1, ItemCount).ToArray();
    }

    private static void TrackCurrentThread()
    {
        _usedThreadIds.Add(Thread.CurrentThread.ManagedThreadId);
    }

    private static void ResetThreadTracking()
    {
        _usedThreadIds.Clear();
    }

    private static ThreadMetrics GetThreadMetrics()
    {
        var uniqueThreads = _usedThreadIds.Distinct().Count();
        var currentThreadPoolWorkers = 0;
        var currentThreadPoolIO = 0;
        ThreadPool.GetAvailableThreads(out var availableWorkers, out var availableIO);
        ThreadPool.GetMaxThreads(out var maxWorkers, out var maxIO);
        
        currentThreadPoolWorkers = maxWorkers - availableWorkers;
        currentThreadPoolIO = maxIO - availableIO;
        
        return new ThreadMetrics
        {
            UniqueThreadsUsed = uniqueThreads,
            ThreadPoolWorkers = currentThreadPoolWorkers,
            ThreadPoolIO = currentThreadPoolIO,
            TotalProcessThreads = Process.GetCurrentProcess().Threads.Count
        };
    }

    public class ThreadMetrics
    {
        public int UniqueThreadsUsed { get; set; }
        public int ThreadPoolWorkers { get; set; }
        public int ThreadPoolIO { get; set; }
        public int TotalProcessThreads { get; set; }

        public override string ToString()
        {
            return $"Unique: {UniqueThreadsUsed}, Pool Workers: {ThreadPoolWorkers}, Pool IO: {ThreadPoolIO}, Total: {TotalProcessThreads}";
        }
    }

    [Benchmark(Baseline = true)]
    public async Task ProcessDataAsyncWithDelay()
    {
        ResetThreadTracking();
        var beforeMetrics = GetThreadMetrics();
        
        var results = new List<int>();
        
        // Асинхронная обработка с небольшими задержками
        for (int i = 0; i < Math.Min(_data.Length, 100); i++) // Уменьшил для демонстрации
        {
            TrackCurrentThread();
            // Имитация асинхронной IO операции
            await Task.Delay(1);
            TrackCurrentThread();
            
            var processed = ProcessItem(_data[i]);
            results.Add(processed);
        }
        
        var afterMetrics = GetThreadMetrics();
        Console.WriteLine($"AsyncWithDelay - Before: {beforeMetrics}, After: {afterMetrics}");
    }

    [Benchmark]
    public Task ProcessDataSyncWithThreadSleep()
    {
        ResetThreadTracking();
        var beforeMetrics = GetThreadMetrics();
        
        var results = new List<int>();
        
        // Синхронная обработка с блокирующими задержками
        return Task.Run(() =>
        {
            for (int i = 0; i < Math.Min(_data.Length, 100); i++) // Уменьшил для демонстрации
            {
                TrackCurrentThread();
                // Имитация блокирующей IO операции
                Thread.Sleep(1);
                
                var processed = ProcessItem(_data[i]);
                results.Add(processed);
            }
            
            var afterMetrics = GetThreadMetrics();
            Console.WriteLine($"SyncWithThreadSleep - Before: {beforeMetrics}, After: {afterMetrics}");
        });
    }

    [Benchmark]
    public async Task ProcessDataAsyncParallelWithTracking()
    {
        ResetThreadTracking();
        var beforeMetrics = GetThreadMetrics();
        
        // Параллельная асинхронная обработка
        var tasks = _data.Take(100).Select(async item => // Берем только 100 элементов для демонстрации
        {
            TrackCurrentThread();
            await Task.Delay(1); // Имитация I/O
            TrackCurrentThread();
            return ProcessItem(item);
        });
        
        var results = await Task.WhenAll(tasks);
        
        var afterMetrics = GetThreadMetrics();
        Console.WriteLine($"AsyncParallel - Before: {beforeMetrics}, After: {afterMetrics}");
    }

    [Benchmark]
    public async Task ProcessDataSyncParallelWithTracking()
    {
        ResetThreadTracking();
        var beforeMetrics = GetThreadMetrics();
        
        // Параллельная синхронная обработка
        await Task.Run(() =>
        {
            var results = _data.Take(100).AsParallel().Select(item =>
            {
                TrackCurrentThread();
                Thread.Sleep(1); // Имитация блокирующей I/O
                return ProcessItem(item);
            }).ToArray();
        });
        
        var afterMetrics = GetThreadMetrics();
        Console.WriteLine($"SyncParallel - Before: {beforeMetrics}, After: {afterMetrics}");
    }    

    private int ProcessItem(int item)
    {
        // Простая CPU-intensive операция
        return item * item + item / 2;
    }

// | Method                         | Mean            | Error         | StdDev        | Median          | Ratio   | RatioSD | Gen0     | Gen1     | Gen2    | Allocated  | Alloc Ratio |
// |------------------------------- |----------------:|--------------:|--------------:|----------------:|--------:|--------:|---------:|---------:|--------:|-----------:|------------:|
// | ProcessDataAsync               |     5,138.08 us |    140.221 us |    413.444 us |     5,298.41 us |   1.000 |    0.00 |  19.5313 |        - |       - |  128.82 KB |        1.00 |
// | ProcessDataSync                |        35.22 us |      0.697 us |      1.085 us |        35.01 us |   0.007 |    0.00 |  20.9351 |   4.3945 |       - |  128.52 KB |        1.00 |
// | ProcessDataAsyncWithDelay      | 1,189,510.10 us | 23,409.631 us | 47,819.672 us | 1,168,134.88 us | 234.426 |   25.72 |        - |        - |       - |  173.46 KB |        1.35 |
// | ProcessDataSyncWithThreadSleep | 1,197,157.72 us | 23,672.277 us | 45,038.967 us | 1,198,662.00 us | 238.198 |   24.73 |        - |        - |       - |   10.38 KB |        0.08 |
// | ProcessDataAsyncParallel       |     4,387.64 us |     87.589 us |    235.303 us |     4,419.70 us |   0.859 |    0.10 | 238.2813 | 117.1875 | 66.4063 | 1389.64 KB |       10.79 |
// | ProcessDataSyncParallel        |        44.77 us |      0.869 us |      1.001 us |        44.94 us |   0.009 |    0.00 |  23.4985 |   4.5776 |       - |   126.1 KB |        0.98 |
}

public static class BenchmarkRunner
{
    public static void RunDataProcessingBenchmarks()
    {
        Console.WriteLine("🚀 Запуск Data Processing бенчмарков...");
        BenchmarkDotNet.Running.BenchmarkRunner.Run<DataProcessingBenchmarks>();
    }

    public static void RunChannelConfigurationBenchmarks()
    {
        Console.WriteLine("🚀 Запуск Channel Configuration бенчмарков...");
        BenchmarkDotNet.Running.BenchmarkRunner.Run<ChannelConfigurationBenchmarks>();
    }

    public static void RunAsyncVsSyncBenchmarks()
    {
        Console.WriteLine("🚀 Запуск Async vs Sync бенчмарков...");
        BenchmarkDotNet.Running.BenchmarkRunner.Run<AsyncVsSyncBenchmarks>();
    }

    public static void RunAllBenchmarks()
    {
        Console.WriteLine("🚀 Запуск всех бенчмарков...");
        BenchmarkDotNet.Running.BenchmarkRunner.Run(typeof(BenchmarkRunner).Assembly);
    }
} 