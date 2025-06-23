using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;

namespace ChannelsPresentation
{
    public class ActorsExample
    {
        public async Task Run()
        {
            var system = ActorSystem.Create("MySystem");
            var dbActor = system.ActorOf<DatabaseActor>("database");

            var res = await dbActor.Ask<DataSavedMessage>(new SaveDataMessage("555", "Hello, Alfabank!"));
            Console.WriteLine($"Data saved: {res.Id}");
        }
    }

    public class DatabaseActor : ReceiveActor
    {
        public DatabaseActor()
        {
            ReceiveAsync<SaveDataMessage>(async message => {
                // Асинхронная операция с базой данных
                await SaveToDatabase(message.Data);
                
                // Отправляем результат обратно
                Sender.Tell(new DataSavedMessage(message.Id));
            });
            
            ReceiveAsync<GetDataMessage>(async message => {
                var data = await LoadFromDatabase(message.Id);
                Sender.Tell(new DataResponse(data));
            });
        }

        // Placeholder methods for database operations
        private async Task SaveToDatabase(string data)
        {
            // Simulate async database save operation
            await Task.Delay(100);
            Console.WriteLine($"Data saved: {data}");
        }

        private async Task<string> LoadFromDatabase(string id)
        {
            // Simulate async database load operation
            await Task.Delay(50);
            return $"Data for ID: {id}";
        }
    }

    // Message classes
    public record SaveDataMessage(string Id, string Data);
    public record GetDataMessage(string Id);
    public record DataSavedMessage(string Id);
    public record DataResponse(string Data);

    // Использование
    // public class ApiController : ControllerBase
    // {
    //     private readonly IActorRef _dbActor;

    //     [HttpPost]
    //     public async Task<IActionResult> SaveData(string data)
    //     {
    //         // Асинхронная отправка сообщения с ожиданием ответа
    //         var result = await _dbActor.Ask<DataSavedMessage>(
    //             new SaveDataMessage(data), 
    //             TimeSpan.FromSeconds(30)
    //         );
        
    //         return Ok($"Saved with ID: {result.Id}");
    //     }
    // }
}