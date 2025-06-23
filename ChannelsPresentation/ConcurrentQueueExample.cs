using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ChannelsPresentation
{
    public class ConcurrentQueueExample
    {
        private readonly ConcurrentQueue<WorkItem> _queue = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public async Task Run()
        {
            Console.WriteLine("=== ConcurrentQueue Producer/Consumer Example ===\n");

            // Запускаем несколько производителей (producers)
            var producers = new[]
            {
                Task.Run(() => ProducerAsync("Producer-1", 1000)),
                Task.Run(() => ProducerAsync("Producer-2", 1500)),
                Task.Run(() => ProducerAsync("Producer-3", 800))
            };

            // Запускаем несколько потребителей (consumers)
            var consumers = new[]
            {
                Task.Run(() => ConsumerAsync("Consumer-A")),
                Task.Run(() => ConsumerAsync("Consumer-B")),
                Task.Run(() => ConsumerAsync("Consumer-C"))
            };

            // Работаем 10 секунд
            await Task.Delay(10000);
            
            // Останавливаем производство
            _cancellationTokenSource.Cancel();
            
            // Ждем завершения всех задач
            await Task.WhenAll(producers);
            await Task.WhenAll(consumers);

            Console.WriteLine($"\nОстаток в очереди: {_queue.Count} элементов");
        }

        private async Task ProducerAsync(string producerName, int intervalMs)
        {
            int itemId = 1;
            
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                var workItem = new WorkItem($"{producerName}-Item-{itemId++}", DateTime.Now);
                
                // Добавляем элемент в очередь (thread-safe операция)
                _queue.Enqueue(workItem);
                
                Console.WriteLine($"📤 {producerName} произвел: {workItem.Name}");
                
                try
                {
                    await Task.Delay(intervalMs, _cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
            
            Console.WriteLine($"✅ {producerName} завершил работу");
        }

        private async Task ConsumerAsync(string consumerName)
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested || !_queue.IsEmpty)
            {
                // Пытаемся извлечь элемент из очереди (thread-safe операция)
                if (_queue.TryDequeue(out var workItem))
                {
                    Console.WriteLine($"📥 {consumerName} обрабатывает: {workItem.Name}");
                    
                    // Симулируем обработку
                    await Task.Delay(500);
                    
                    Console.WriteLine($"✅ {consumerName} завершил обработку: {workItem.Name}");
                }
                else
                {
                    // Если очередь пуста, ждем немного
                    await Task.Delay(100);
                }
            }
            
            Console.WriteLine($"🏁 {consumerName} завершил работу");
        }
    }

    public record WorkItem(string Name, DateTime CreatedAt);

    // Более сложный пример с ограничением размера очереди
    public class BoundedConcurrentQueueExample
    {
        private readonly ConcurrentQueue<WorkItem> _queue = new();
        private readonly SemaphoreSlim _semaphore;
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public BoundedConcurrentQueueExample(int maxQueueSize)
        {
            _semaphore = new SemaphoreSlim(maxQueueSize, maxQueueSize);
        }

        public async Task Run()
        {
            Console.WriteLine("\n=== Bounded ConcurrentQueue Example ===\n");

            var producer = Task.Run(() => BoundedProducerAsync("BoundedProducer"));
            var consumer = Task.Run(() => BoundedConsumerAsync("BoundedConsumer"));

            await Task.Delay(5000);
            _cancellationTokenSource.Cancel();

            await Task.WhenAll(producer, consumer);
        }

        private async Task BoundedProducerAsync(string producerName)
        {
            int itemId = 1;

            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    // Ждем, пока в очереди освободится место
                    await _semaphore.WaitAsync(_cancellationTokenSource.Token);
                    
                    var workItem = new WorkItem($"{producerName}-Item-{itemId++}", DateTime.Now);
                    _queue.Enqueue(workItem);
                    
                    Console.WriteLine($"📤 {producerName} произвел: {workItem.Name} (очередь: {_queue.Count})");
                    
                    await Task.Delay(200, _cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task BoundedConsumerAsync(string consumerName)
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested || !_queue.IsEmpty)
            {
                if (_queue.TryDequeue(out var workItem))
                {
                    // Освобождаем место в семафоре
                    _semaphore.Release();
                    
                    Console.WriteLine($"📥 {consumerName} обрабатывает: {workItem.Name}");
                    await Task.Delay(800); // Медленная обработка
                    Console.WriteLine($"✅ {consumerName} завершил: {workItem.Name}");
                }
                else
                {
                    await Task.Delay(100);
                }
            }
        }
    }
} 