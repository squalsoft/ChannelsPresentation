using Akka.Actor;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Collections.Concurrent;
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
    public async Task ProcessWithSimpleParallel()
    {
        await Task.Run(() =>
        {
            var results = _data.AsParallel().Select(x => x * 2).ToArray();
            return results;
        });
    }

    [Benchmark]
    public async Task ProcessSequentially()
    {
        await Task.Run(() =>
        {
            var results = _data.Select(x => x * 2).ToArray();
            return results;
        });
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
                result => results.Add(result), // onNext
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
        
        var results = new List<int>();
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
            Receive<ProcessItem>(item =>
            {
                // Трансформация данных
                var result = item.Value * 2;
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

// | Method                        | Mean        | Error     | StdDev     | Median      | Ratio | RatioSD | Gen0     | Gen1     | Gen2    | Allocated  | Alloc Ratio |
// |------------------------------ |------------:|----------:|-----------:|------------:|------:|--------:|---------:|---------:|--------:|-----------:|------------:|
// | ProcessWithChannels           |   479.14 us | 12.108 us |  35.320 us |   466.31 us |  1.00 |    0.00 |  34.1797 |   8.7891 |       - |  198.55 KB |        1.00 |
// | ProcessWithBoundedChannel     | 1,222.34 us | 23.737 us |  36.249 us | 1,214.01 us |  2.63 |    0.20 |  21.4844 |   3.9063 |       - |  138.74 KB |        0.70 |
// | ProcessWithConcurrentQueue    |   189.55 us |  3.733 us |   5.811 us |   190.16 us |  0.41 |    0.03 |  24.9023 |   6.1035 |       - |  148.02 KB |        0.75 |
// | ProcessWithBlockingCollection | 1,353.60 us | 35.222 us | 103.299 us | 1,324.93 us |  2.84 |    0.28 |  25.3906 |   5.8594 |       - |  151.66 KB |        0.76 |
// | ProcessWithSimpleParallel     |    42.56 us |  0.828 us |   1.868 us |    42.06 us |  0.09 |    0.01 |  23.4985 |   4.5776 |       - |  126.13 KB |        0.64 |
// | ProcessSequentially           |    10.39 us |  0.197 us |   0.453 us |    10.28 us |  0.02 |    0.00 |   6.9122 |   0.3815 |  0.0153 |   39.37 KB |        0.20 |
// | ProcessWithLockAndList        | 1,658.23 us | 32.710 us |  32.125 us | 1,648.84 us |  3.54 |    0.28 |  29.2969 |   5.8594 |       - |  168.53 KB |        0.85 |
// | ProcessWithTplDataflow        | 1,793.53 us | 35.024 us |  45.541 us | 1,794.56 us |  3.83 |    0.30 |  39.0625 |  11.7188 |       - |  219.42 KB |        1.11 |
// | ProcessWithRx                 |    79.85 us |  1.521 us |   1.690 us |    79.51 us |  0.17 |    0.01 |  20.9961 |   4.2725 |       - |  128.98 KB |        0.65 |
// | ProcessWithAkkaActors         | 2,947.41 us | 57.352 us |  80.400 us | 2,930.72 us |  6.35 |    0.49 | 460.9375 | 125.0000 | 85.9375 | 2817.46 KB |       14.19 |
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

    public static void RunAllBenchmarks()
    {
        Console.WriteLine("🚀 Запуск всех бенчмарков...");
        BenchmarkDotNet.Running.BenchmarkRunner.Run(typeof(BenchmarkRunner).Assembly);
    }
} 