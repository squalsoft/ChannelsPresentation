using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ChannelsPresentation
{
    public class BlockingCollectionExample
    {
        public async Task Run()
        {
            Console.WriteLine("=== BlockingCollection Examples ===\n");

            await BasicExample();
            await BoundedCapacityExample();
            await CompletionSignalingExample();
            await MultipleProducersConsumersExample();
            await DifferentCollectionTypesExample();
        }

        // 1. Базовый пример
        private async Task BasicExample()
        {
            Console.WriteLine("--- 1. Базовый пример ---");
            
            var collection = new BlockingCollection<string>();

            // Producer задача
            var producer = Task.Run(() =>
            {
                for (int i = 1; i <= 5; i++)
                {
                    var item = $"Item-{i}";
                    collection.Add(item);
                    Console.WriteLine($"📤 Произведен: {item}");
                    Thread.Sleep(500);
                }
                collection.CompleteAdding(); // Сигнализируем завершение
                Console.WriteLine("✅ Producer завершен");
            });

            // Consumer задача
            var consumer = Task.Run(() =>
            {
                // GetConsumingEnumerable автоматически завершается при CompleteAdding
                foreach (var item in collection.GetConsumingEnumerable())
                {
                    Console.WriteLine($"📥 Обрабатывается: {item}");
                    Thread.Sleep(800); // Медленная обработка
                    Console.WriteLine($"✅ Обработан: {item}");
                }
                Console.WriteLine("🏁 Consumer завершен");
            });

            await Task.WhenAll(producer, consumer);
            Console.WriteLine();
        }

        // 2. Пример с ограничением емкости
        private async Task BoundedCapacityExample()
        {
            Console.WriteLine("--- 2. Ограниченная емкость (3 элемента) ---");
            
            var collection = new BlockingCollection<int>(boundedCapacity: 3);

            var producer = Task.Run(() =>
            {
                for (int i = 1; i <= 8; i++)
                {
                    Console.WriteLine($"🔄 Producer пытается добавить: {i}");
                    collection.Add(i); // Блокируется при заполнении
                    Console
                        .WriteLine($"📤 Добавлен: {i} (в очереди: {collection.Count})");
                    Thread.Sleep(200);
                }
                collection.CompleteAdding();
            });

            var consumer = Task.Run(() =>
            {
                Thread.Sleep(1000); // Начинаем с задержкой
                
                while (!collection.IsCompleted)
                {
                    if (collection.TryTake(out int item, 100))
                    {
                        Console.WriteLine(
                            $"📥 Взят: {item} (в очереди: {collection.Count})");
                        Thread.Sleep(600); // Медленная обработка
                    }
                }
            });

            await Task.WhenAll(producer, consumer);
            Console.WriteLine();
        }

        // 3. Пример с сигнализацией завершения
        private async Task CompletionSignalingExample()
        {
            Console.WriteLine("--- 3. Сигнализация завершения ---");
            
            var collection = new BlockingCollection<WorkItem>();

            var producer = Task.Run(() =>
            {
                for (int i = 1; i <= 3; i++)
                {
                    var workItem = new WorkItem($"Task-{i}", DateTime.Now);
                    collection.Add(workItem);
                    Console.WriteLine($"📤 Добавлена задача: {workItem.Name}");
                    Thread.Sleep(300);
                }
                
                Console.WriteLine("🚫 Завершаем добавление новых элементов");
                collection.CompleteAdding();
            });

            var consumer = Task.Run(() =>
            {
                try
                {
                    while (true)
                    {
                        // Take() выбросит InvalidOperationException когда коллекция завершена и пуста
                        var item = collection.Take();
                        Console.WriteLine($"📥 Обрабатываем: {item.Name}");
                        Thread.Sleep(500);
                        Console.WriteLine($"✅ Завершили: {item.Name}");
                    }
                }
                catch (InvalidOperationException)
                {
                    Console.WriteLine("🏁 Коллекция завершена, consumer останавливается");
                }
            });

            await Task.WhenAll(producer, consumer);
            Console.WriteLine();
        }

        // 4. Несколько производителей и потребителей
        private async Task MultipleProducersConsumersExample()
        {
            Console.WriteLine("--- 4. Несколько producers/consumers ---");
            
            var collection = new BlockingCollection<string>(10);

            // Запускаем 2 производителя
            var producers = new[]
            {
                Task.Run(() => ProducerWorker("Producer-A", collection, 200)),
                Task.Run(() => ProducerWorker("Producer-B", collection, 300))
            };

            // Запускаем 2 потребителя
            var consumers = new[]
            {
                Task.Run(() => ConsumerWorker("Consumer-1", collection)),
                Task.Run(() => ConsumerWorker("Consumer-2", collection))
            };

            // Работаем 2 секунды
            await Task.Delay(2000);
            
            // Завершаем производство
            collection.CompleteAdding();
            
            await Task.WhenAll(producers.Concat(consumers).ToArray());
            Console.WriteLine();
        }

        private void ProducerWorker(string name, BlockingCollection<string> collection, int delay)
        {
            int counter = 1;
            try
            {
                while (!collection.IsAddingCompleted)
                {
                    var item = $"{name}-Item-{counter++}";
                    if (collection.TryAdd(item, 50)) // Timeout 50ms
                    {
                        Console.WriteLine($"📤 {name}: {item}");
                    }
                    Thread.Sleep(delay);
                }
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine($"✅ {name} завершен");
            }
        }

        private void ConsumerWorker(string name, BlockingCollection<string> collection)
        {
            foreach (var item in collection.GetConsumingEnumerable())
            {
                Console.WriteLine($"📥 {name} обрабатывает: {item}");
                Thread.Sleep(400);
            }
            Console.WriteLine($"🏁 {name} завершен");
        }

        // 5. Разные типы базовых коллекций
        private async Task DifferentCollectionTypesExample()
        {
            Console.WriteLine("--- 5. Разные типы коллекций ---");

            // FIFO (по умолчанию - ConcurrentQueue)
            Console.WriteLine("FIFO (Queue):");
            var fifoCollection = new BlockingCollection<int>();
            await DemonstrateCollectionOrder(fifoCollection, "FIFO");

            // LIFO (Stack)
            Console.WriteLine("LIFO (Stack):");
            var lifoCollection = new BlockingCollection<int>(new ConcurrentStack<int>());
            await DemonstrateCollectionOrder(lifoCollection, "LIFO");

            // Unordered (Bag)
            Console.WriteLine("Unordered (Bag):");
            var bagCollection = new BlockingCollection<int>(new ConcurrentBag<int>());
            await DemonstrateCollectionOrder(bagCollection, "Bag");
        }

        private async Task DemonstrateCollectionOrder(BlockingCollection<int> collection, string type)
        {
            // Добавляем элементы
            for (int i = 1; i <= 5; i++)
            {
                collection.Add(i);
            }
            collection.CompleteAdding();

            // Извлекаем и показываем порядок
            var result = new List<int>();
            foreach (var item in collection.GetConsumingEnumerable())
            {
                result.Add(item);
            }
            
            Console.WriteLine($"  Добавили: [1, 2, 3, 4, 5]");
            Console.WriteLine($"  Получили ({type}): [{string.Join(", ", result)}]");
            Console.WriteLine();
        }
    }
} 