using System.Threading.Channels;

namespace ChannelsPresentation;

public class ChannelsExample
{
    public async Task RunBounded()
    {
        // Создаем канал с максимальным размером буфера 10 элементов
        var channel = Channel.CreateBounded<int>(10); // Channel.CreateUnbounded<int>(); - без ограничения

        // Пример асинхронного производителя
        async Task ProducerAsync()
        {
            for (int i = 0; i < 20; i++)
            {
                await channel.Writer.WriteAsync(i);
                Console.WriteLine($"Произведено: {i}");
                await Task.Delay(100); // Имитация работы
            }

            // Важно: сообщаем, что запись завершена
            channel.Writer.Complete();
        }

        // Пример асинхронного потребителя
        async Task ConsumerAsync()
        {
            await foreach (var item in channel.Reader.ReadAllAsync())
            {
                Console.WriteLine($"Получено: {item}");
                await Task.Delay(300); // Потребитель работает медленнее
            }
        }

        // Запускаем producer и consumer
        await Task.WhenAll(
            ProducerAsync(),
            ConsumerAsync()
        );
    }
}