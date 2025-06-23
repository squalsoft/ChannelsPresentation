using System.Threading.Tasks.Dataflow;

namespace ChannelsPresentation;

public class TplDataflowExample
{
    // Простой пример
    public async Task RunFirstBlocks()
    {
        var transformBlock = new TransformBlock<int, string>(
            num => num.ToString());

        var actionBlock = new ActionBlock<string>(
            str => Console.WriteLine(str));

        // Связываем блоки
        transformBlock.LinkTo(actionBlock);

        // Отправляем данные
        for (int i = 0; i < 10; i++)
        {
            transformBlock.Post(i);
        }

        // Завершаем отправку данных
        transformBlock.Complete();

        // Ожидаем завершения всех блоков
        await transformBlock.Completion;
    }

    // Определяем модели данных
    public class Order
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; }
        public List<string> Items { get; set; }
    }

    public class ProcessedOrder : Order
    {
        public decimal TotalPrice { get; set; }
        public decimal Tax { get; set; }
    }

    public async Task RunShopBlocks()
    {
        // Создаем блоки
        var bufferBlock = new BufferBlock<Order>(
            new DataflowBlockOptions
            {
                BoundedCapacity = 5
            });

        // Order -> ProcessedOrder
        var transformBlock = new TransformBlock<Order, ProcessedOrder>(
            order => new ProcessedOrder
            {
                OrderId = order.OrderId,
                CustomerName = order.CustomerName,
                Items = order.Items,
                TotalPrice = CalculatePrice(order),
                Tax = CalculateTax(order)
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 2
            });

        var actionBlock = new ActionBlock<ProcessedOrder>(
            async processedOrder =>
            {
                // Сохранение в базу данных
                await SaveToDatabase(processedOrder);
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 2
            });

        // Связываем блоки с прокидыванием сигнала завершения
        bufferBlock.LinkTo(transformBlock,
            new DataflowLinkOptions { PropagateCompletion = true });
        transformBlock.LinkTo(actionBlock,
            new DataflowLinkOptions { PropagateCompletion = true });

        // Использование
        for (int i = 0; i < 10; i++)
        {
            await bufferBlock.SendAsync(new Order
            {
                OrderId = i,
                CustomerName = "John Doe " + i,
                Items = ["Item1", "Item2", "Item" + i]
            });
        }
        // Завершаем отправку данных
        bufferBlock.Complete();

        await actionBlock.Completion;
    }

    // ================
    private decimal CalculateTax(Order order)
    {
        return order.Items.Count * 6;
    }

    private decimal CalculatePrice(Order order)
    {
        return order.Items.Count * 100;
    }

    private async Task SaveToDatabase(ProcessedOrder processedOrder)
    {
        Console.WriteLine($"Заказ сохранен {processedOrder.OrderId}");
    }


    public class CpuBlockingExample
    {
        // ХОРОШО: ограничиваем количество CPU-задач
        private readonly SemaphoreSlim _cpuSemaphore = 
            new(Math.Max(1, Environment.ProcessorCount - 2));
        public async Task ProcessDataAsync()
        {
            await _cpuSemaphore.WaitAsync();
            try
            {
                await Task.Run(() => {
                    for (long i = 0; i < 10_000_000_000; i++)
                    {
                        Math.Sqrt(i); // Тяжелые вычисления
                    }
                });
            }
            finally
            {
                _cpuSemaphore.Release();
            }
        }
        
        public async Task HandleRequestsAsync()
        {
            var tasks = new List<Task>();
            
            // Запускаем 50 тяжелых задач
            for (int i = 0; i < 50; i++)
            {
                tasks.Add(ProcessDataAsync());
            }
            
            await Task.WhenAll(tasks); // Все ядра процессора загружены на 100%
        }
    }
}
