using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace ChannelsPresentation;

public class RxExample
{
    public async Task RunSubject()
    {
        var subject = new Subject<string>();

        // Подписываемся на получение данных
        subject.Subscribe(x => Console.WriteLine($"Получено: {x}"));

        // Отправляем данные
        subject.OnNext("Привет");    // Выведет: "Получено: Привет"
        subject.OnNext("Alfabank");       // Выведет: "Получено: Мир"
        subject.OnCompleted();       // Завершаем поток данных
    }

    public async Task RunStockMonitor()
    {
        var tcs = new TaskCompletionSource();

        new StockMonitor().MonitorStocks()
            .TakeUntil(Observable.Timer(TimeSpan.FromSeconds(5)))
            .Subscribe(
                r => Console.WriteLine($"Средняя цена: {r}"),
                error => tcs.SetException(error),
                () => tcs.SetResult());

        await tcs.Task;
        //await Task.Delay(10000);
    }

    public class StockMonitor
    {
        public IObservable<decimal> MonitorStocks()
        {
            var stockA = GetStockPrice("AAPL");
            var stockB = GetStockPrice("GOOGL");
            var stockC = GetStockPrice("MSFT");

            return Observable
                .Merge(stockA, stockB, stockC)  // объединяем потоки
                .Buffer(10)                     // группируем по 10 значений
                .Select(prices => prices.Average())  // считаем среднее
                .Where(avg => avg > 100);           // фильтруем
        }

        // Источник данных
        private IObservable<decimal> GetStockPrice(string symbol)
        {
            return Observable.Interval(TimeSpan.FromSeconds(1))
                .SelectMany(_ => GetPriceAsync(symbol));
        }

        // Симуляция получения внешних данных
        private async Task<decimal> GetPriceAsync(string symbol)
        {
            //await Task.Delay(100);
            return (decimal)(new Random().Next(50, 150) + new Random().NextDouble());
        }
    }
}