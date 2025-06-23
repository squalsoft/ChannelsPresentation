using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ChannelsPresentation
{
    public class ComparisonExample
    {
        private readonly Random _random = new();

        public async Task Run()
        {
            Console.WriteLine("=== Сравнение: Channels vs async/await vs Task.WhenAll ===\n");

            const int itemsCount = 100;
            const int producersCount = 3;
            const int consumersCount = 2;

            Console.WriteLine($"Задача: Обработать {itemsCount} элементов с {producersCount} производителями и {consumersCount} потребителями\n");

            // 1. Channels подход
            await RunChannelsExample(itemsCount, producersCount, consumersCount);

            // 2. async/await с ConcurrentQueue
            await RunAsyncAwaitExample(itemsCount, producersCount, consumersCount);

            // 3. Task.WhenAll подход
            await RunTaskWhenAllExample(itemsCount, producersCount, consumersCount);

            // 4. Сравнение производительности
            await PerformanceComparison(itemsCount);
        }

        // 1. CHANNELS - современный producer/consumer
        private async Task RunChannelsExample(int itemsCount, int producersCount, int consumersCount)
        {
            Console.WriteLine("--- 1. CHANNELS подход ---");
            var stopwatch = Stopwatch.StartNew();

            // Создаем канал с ограниченной емкостью
            var channel = Channel.CreateBounded<DataItem>(capacity: 10);
            var writer = channel.Writer;
            var reader = channel.Reader;

            var processedCount = 0;

            // Производители
            var producers = new Task[producersCount];
            for (int i = 0; i < producersCount; i++)
            {
                int producerId = i;
                producers[i] = Task.Run(async () =>
                {
                    for (int j = 0; j < itemsCount / producersCount; j++)
                    {
                        var item = new DataItem($"P{producerId}-Item{j}", _random.Next(100, 500));
                        await writer.WriteAsync(item);
                        Console.WriteLine($"📤 [Channels] Producer {producerId}: {item.Name}");
                        await Task.Delay(50);
                    }
                });
            }

            // Потребители
            var consumers = new Task[consumersCount];
            for (int i = 0; i < consumersCount; i++)
            {
                int consumerId = i;
                consumers[i] = Task.Run(async () =>
                {
                    await foreach (var item in reader.ReadAllAsync())
                    {
                        await ProcessDataItem(item, $"Consumer{consumerId}");
                        Interlocked.Increment(ref processedCount);
                    }
                });
            }

            // Ждем завершения производителей и закрываем канал
            await Task.WhenAll(producers);
            writer.Complete();

            // Ждем завершения потребителей
            await Task.WhenAll(consumers);

            stopwatch.Stop();
            Console.WriteLine($"✅ [Channels] Обработано: {processedCount} элементов за {stopwatch.ElapsedMilliseconds}ms\n");
        }

        // 2. ASYNC/AWAIT с ConcurrentQueue
        private async Task RunAsyncAwaitExample(int itemsCount, int producersCount, int consumersCount)
        {
            Console.WriteLine("--- 2. ASYNC/AWAIT + ConcurrentQueue подход ---");
            var stopwatch = Stopwatch.StartNew();

            var queue = new ConcurrentQueue<DataItem>();
            var completed = false;
            var processedCount = 0;

            // Производители
            var producers = new Task[producersCount];
            for (int i = 0; i < producersCount; i++)
            {
                int producerId = i;
                producers[i] = Task.Run(async () =>
                {
                    for (int j = 0; j < itemsCount / producersCount; j++)
                    {
                        var item = new DataItem($"P{producerId}-Item{j}", _random.Next(100, 500));
                        queue.Enqueue(item);
                        Console.WriteLine($"📤 [Async/Await] Producer {producerId}: {item.Name}");
                        await Task.Delay(50);
                    }
                });
            }

            // Потребители
            var consumers = new Task[consumersCount];
            for (int i = 0; i < consumersCount; i++)
            {
                int consumerId = i;
                consumers[i] = Task.Run(async () =>
                {
                    while (!completed || !queue.IsEmpty)
                    {
                        if (queue.TryDequeue(out var item))
                        {
                            await ProcessDataItem(item, $"Consumer{consumerId}");
                            Interlocked.Increment(ref processedCount);
                        }
                        else
                        {
                            await Task.Delay(10); // Короткая пауза если очередь пуста
                        }
                    }
                });
            }

            // Ждем производителей
            await Task.WhenAll(producers);
            completed = true;

            // Ждем потребителей
            await Task.WhenAll(consumers);

            stopwatch.Stop();
            Console.WriteLine($"✅ [Async/Await] Обработано: {processedCount} элементов за {stopwatch.ElapsedMilliseconds}ms\n");
        }

        // 3. TASK.WHENALL - параллельная обработка
        private async Task RunTaskWhenAllExample(int itemsCount, int producersCount, int consumersCount)
        {
            Console.WriteLine("--- 3. TASK.WHENALL подход ---");
            var stopwatch = Stopwatch.StartNew();

            // Генерируем все данные заранее
            var allItems = new List<DataItem>();
            for (int i = 0; i < itemsCount; i++)
            {
                allItems.Add(new DataItem($"Item{i}", _random.Next(100, 500)));
            }

            Console.WriteLine($"📦 [Task.WhenAll] Сгенерировано {allItems.Count} элементов");

            // Разделяем работу между потребителями
            var chunkSize = itemsCount / consumersCount;
            var tasks = new List<Task>();

            for (int i = 0; i < consumersCount; i++)
            {
                int consumerId = i;
                int startIndex = i * chunkSize;
                int endIndex = (i == consumersCount - 1) ? allItems.Count : startIndex + chunkSize;

                var chunk = allItems.GetRange(startIndex, endIndex - startIndex);
                
                tasks.Add(Task.Run(async () =>
                {
                    foreach (var item in chunk)
                    {
                        await ProcessDataItem(item, $"Worker{consumerId}");
                    }
                    Console.WriteLine($"✅ [Task.WhenAll] Worker{consumerId} обработал {chunk.Count} элементов");
                }));
            }

            // Ждем завершения всех задач параллельно
            await Task.WhenAll(tasks);

            stopwatch.Stop();
            Console.WriteLine($"✅ [Task.WhenAll] Обработано: {allItems.Count} элементов за {stopwatch.ElapsedMilliseconds}ms\n");
        }

        // 4. Сравнение производительности
        private async Task PerformanceComparison(int itemsCount)
        {
            Console.WriteLine("--- 4. СРАВНЕНИЕ ПРОИЗВОДИТЕЛЬНОСТИ ---");

            const int iterations = 3;
            var channelsTime = new List<long>();
            var asyncAwaitTime = new List<long>();
            var taskWhenAllTime = new List<long>();

            for (int i = 0; i < iterations; i++)
            {
                Console.WriteLine($"Итерация {i + 1}/{iterations}:");

                // Channels
                var sw = Stopwatch.StartNew();
                await QuickChannelsTest(itemsCount);
                sw.Stop();
                channelsTime.Add(sw.ElapsedMilliseconds);
                Console.WriteLine($"  Channels: {sw.ElapsedMilliseconds}ms");

                // Async/Await
                sw.Restart();
                await QuickAsyncAwaitTest(itemsCount);
                sw.Stop();
                asyncAwaitTime.Add(sw.ElapsedMilliseconds);
                Console.WriteLine($"  Async/Await: {sw.ElapsedMilliseconds}ms");

                // Task.WhenAll
                sw.Restart();
                await QuickTaskWhenAllTest(itemsCount);
                sw.Stop();
                taskWhenAllTime.Add(sw.ElapsedMilliseconds);
                Console.WriteLine($"  Task.WhenAll: {sw.ElapsedMilliseconds}ms");
                
                Console.WriteLine();
            }

            // Средние значения
            Console.WriteLine("📊 СРЕДНИЕ РЕЗУЛЬТАТЫ:");
            Console.WriteLine($"  Channels: {channelsTime.Average():F1}ms");
            Console.WriteLine($"  Async/Await: {asyncAwaitTime.Average():F1}ms");
            Console.WriteLine($"  Task.WhenAll: {taskWhenAllTime.Average():F1}ms");

            Console.WriteLine("\n🏆 ВЫВОДЫ:");
            Console.WriteLine("  • Channels: 🎯 Лучший для producer/consumer, управление потоком");
            Console.WriteLine("  • Async/Await: ⚖️ Универсальный, но требует ручного управления");
            Console.WriteLine("  • Task.WhenAll: 🚀 Самый быстрый для известного объема данных");
        }

        // Быстрые тесты для сравнения
        private async Task QuickChannelsTest(int count)
        {
            var channel = Channel.CreateUnbounded<int>();
            var writer = channel.Writer;
            var reader = channel.Reader;

            var producer = Task.Run(async () =>
            {
                for (int i = 0; i < count; i++)
                {
                    await writer.WriteAsync(i);
                }
                writer.Complete();
            });

            var consumer = Task.Run(async () =>
            {
                await foreach (var item in reader.ReadAllAsync())
                {
                    await Task.Delay(1); // Минимальная "обработка"
                }
            });

            await Task.WhenAll(producer, consumer);
        }

        private async Task QuickAsyncAwaitTest(int count)
        {
            var queue = new ConcurrentQueue<int>();
            var completed = false;

            var producer = Task.Run(async () =>
            {
                for (int i = 0; i < count; i++)
                {
                    queue.Enqueue(i);
                    await Task.Yield();
                }
                completed = true;
            });

            var consumer = Task.Run(async () =>
            {
                while (!completed || !queue.IsEmpty)
                {
                    if (queue.TryDequeue(out var item))
                    {
                        await Task.Delay(1);
                    }
                    await Task.Yield();
                }
            });

            await Task.WhenAll(producer, consumer);
        }

        private async Task QuickTaskWhenAllTest(int count)
        {
            var tasks = new List<Task>();
            for (int i = 0; i < count; i++)
            {
                tasks.Add(Task.Delay(1));
            }
            await Task.WhenAll(tasks);
        }

        // Имитация обработки данных
        private async Task ProcessDataItem(DataItem item, string workerName)
        {
            Console.WriteLine($"📥 {workerName} обрабатывает: {item.Name}");
            await Task.Delay(item.ProcessingTimeMs);
            // Console.WriteLine($"✅ {workerName} завершил: {item.Name}");
        }

        public record DataItem(string Name, int ProcessingTimeMs);
    }
} 