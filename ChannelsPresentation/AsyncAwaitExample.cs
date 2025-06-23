using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ChannelsPresentation
{
    public class AsyncAwaitExample
    {
        public async Task Run()
        {
            Console.WriteLine("=== Async/Await - Основы и важность ===\n");

            await DemoBasicAsyncAwait();
            await DemoSequentialVsParallel();
            await DemoErrorHandling();
            ShowImportance();
        }

        // 1. Базовый пример async/await
        private async Task DemoBasicAsyncAwait()
        {
            Console.WriteLine("--- 1. Базовый пример ---");

            Console.WriteLine("🔄 Начинаем загрузку данных...");
            
            // Симулируем загрузку данных из разных источников
            var userTask = LoadUserAsync("user123");
            var settingsTask = LoadSettingsAsync("user123");
            
            // Ждем результатов асинхронно
            var user = await userTask;
            var settings = await settingsTask;
            
            Console.WriteLine($"✅ Пользователь: {user}");
            Console.WriteLine($"✅ Настройки: {settings}");
            Console.WriteLine();
        }

        // 2. Последовательное vs Параллельное выполнение
        private async Task DemoSequentialVsParallel()
        {
            Console.WriteLine("--- 2. Последовательное vs Параллельное ---");

            // ❌ ПЛОХО: Последовательное выполнение
            Console.WriteLine("❌ Последовательно (медленно):");
            var start = DateTime.Now;
            
            var data1 = await FetchDataAsync("API-1", 1000);
            var data2 = await FetchDataAsync("API-2", 1500);
            var data3 = await FetchDataAsync("API-3", 800);
            
            Console.WriteLine($"   Время: {(DateTime.Now - start).TotalMilliseconds:F0}ms");

            // ✅ ХОРОШО: Параллельное выполнение
            Console.WriteLine("✅ Параллельно (быстро):");
            start = DateTime.Now;
            
            var task1 = FetchDataAsync("API-1", 1000);
            var task2 = FetchDataAsync("API-2", 1500);
            var task3 = FetchDataAsync("API-3", 800);
            
            await Task.WhenAll(task1, task2, task3);
            
            Console.WriteLine($"   Время: {(DateTime.Now - start).TotalMilliseconds:F0}ms");
            Console.WriteLine();
        }

        // 3. Обработка ошибок
        private async Task DemoErrorHandling()
        {
            Console.WriteLine("--- 3. Обработка ошибок ---");

            try
            {
                // Попытка выполнить операцию, которая может упасть
                await RiskyOperationAsync();
                Console.WriteLine("✅ Операция выполнена успешно");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка: {ex.Message}");
            }
            Console.WriteLine();
        }

        // Вспомогательные методы
        private async Task<string> LoadUserAsync(string userId)
        {
            Console.WriteLine($"📥 Загружаем пользователя {userId}...");
            await Task.Delay(500); // Симуляция сетевого запроса
            return $"User({userId})";
        }

        private async Task<string> LoadSettingsAsync(string userId)
        {
            Console.WriteLine($"📥 Загружаем настройки для {userId}...");
            await Task.Delay(300); // Симуляция запроса к БД
            return $"Settings({userId})";
        }

        private async Task<string> FetchDataAsync(string apiName, int delayMs)
        {
            Console.WriteLine($"   📡 Запрос к {apiName}...");
            await Task.Delay(delayMs);
            Console.WriteLine($"   ✅ {apiName} ответил");
            return $"Data from {apiName}";
        }

        private async Task RiskyOperationAsync()
        {
            Console.WriteLine("🎲 Выполняем рискованную операцию...");
            await Task.Delay(300);
            
            // Случайно падаем
            if (new Random().Next(2) == 0)
                throw new InvalidOperationException("Что-то пошло не так!");
            
            Console.WriteLine("✅ Операция завершена");
        }

        // Объяснение важности async/await
        private void ShowImportance()
        {
            Console.WriteLine("--- 🎯 ВАЖНОСТЬ ASYNC/AWAIT ---");
            Console.WriteLine();
            
            Console.WriteLine("🔥 БЕЗ Async/Await (синхронный код):");
            Console.WriteLine("   • Блокирует поток выполнения");
            Console.WriteLine("   • UI зависает при длительных операциях");
            Console.WriteLine("   • Плохая масштабируемость сервера");
            Console.WriteLine("   • Неэффективное использование ресурсов");
            Console.WriteLine();
            
            Console.WriteLine("✨ С Async/Await:");
            Console.WriteLine("   • 🚀 Неблокирующие операции - поток освобождается");
            Console.WriteLine("   • 💻 Отзывчивый UI - интерфейс не зависает");
            Console.WriteLine("   • 📈 Высокая пропускная способность сервера");
            Console.WriteLine("   • 🎯 Эффективное использование потоков ThreadPool");
            Console.WriteLine("   • 🧩 Простота написания и понимания кода");
            Console.WriteLine();
            
            Console.WriteLine("📊 МАГИЯ:");
            Console.WriteLine("   Один поток может обслуживать ТЫСЯЧИ запросов!");
            Console.WriteLine("   Вместо 1000 потоков -> 1 поток + async/await = 🚀");
            Console.WriteLine();
            
            Console.WriteLine("🏆 КОГДА ИСПОЛЬЗОВАТЬ:");
            Console.WriteLine("   ✅ Сетевые запросы (HTTP, API)");
            Console.WriteLine("   ✅ Операции с базой данных");
            Console.WriteLine("   ✅ Файловые операции");
            Console.WriteLine("   ✅ Любые I/O операции");
            Console.WriteLine("   ❌ CPU-интенсивные вычисления (лучше Task.Run)");
        }
    }

    // Практический пример: Загрузка данных о погоде
    public class WeatherService
    {
        private static readonly HttpClient httpClient = new();

        // Асинхронная загрузка погоды для нескольких городов
        public async Task<Dictionary<string, string>> GetWeatherForCitiesAsync(string[] cities)
        {
            var tasks = cities.Select(city => GetWeatherAsync(city));
            var results = await Task.WhenAll(tasks);
            
            return cities.Zip(results, (city, weather) => new { city, weather })
                        .ToDictionary(x => x.city, x => x.weather);
        }

        private async Task<string> GetWeatherAsync(string city)
        {
            // В реальности здесь был бы HTTP запрос к API погоды
            await Task.Delay(500); // Симуляция сетевого запроса
            
            var temperatures = new[] { "☀️ +25°C", "🌤️ +20°C", "🌧️ +15°C", "❄️ -5°C" };
            return temperatures[city.GetHashCode() % temperatures.Length];
        }
    }
} 