using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ChannelsPresentation
{
    public class TaskWhenAllExample
    {
        public async Task Run()
        {
            Console.WriteLine("=== Task.WhenAll - Параллельная магия ===\n");

            await DemoBasicWhenAll();
            await DemoSequentialVsParallel();
            await DemoErrorHandling();
            await DemoRealWorldScenarios();
            ShowImportance();
        }

        // 1. Базовый пример Task.WhenAll
        private async Task DemoBasicWhenAll()
        {
            Console.WriteLine("--- 1. Базовый пример ---");

            Console.WriteLine("🚀 Запускаем 3 задачи параллельно...");


            await DownloadDataAsync("Сервер-A", 1000); // 1 сек
            await DownloadDataAsync("Сервер-B", 1500); // 1.5 сек
            await DownloadDataAsync("Сервер-C", 800); // 0.8 сек
            // Суммарное время выполнения: 3.3 сек
            
            var task1 = DownloadDataAsync("Сервер-A", 1000); // 1 сек
            var task2 = DownloadDataAsync("Сервер-B", 1500); // 1.5 сек
            var task3 = DownloadDataAsync("Сервер-C", 800); // 0.8 сек

            // Ждем завершения ВСЕХ задач
            var results = await Task.WhenAll(task1, task2, task3); 
            // Суммарное время выполнения: 1.5 сек

            Console.WriteLine("✅ Все задачи завершены!");
            foreach (var result in results)
            {
                Console.WriteLine($"   📦 {result}");
            }
            Console.WriteLine();
        }

        // 2. Последовательное vs Параллельное выполнение
        private async Task DemoSequentialVsParallel()
        {
            Console.WriteLine("--- 2. Последовательное vs Параллельное ---");

            // ❌ МЕДЛЕННО: Последовательное выполнение
            Console.WriteLine("❌ Последовательно:");
            var sw = Stopwatch.StartNew();
            
            await ProcessOrderAsync("Заказ-1");
            await ProcessOrderAsync("Заказ-2");
            await ProcessOrderAsync("Заказ-3");
            
            sw.Stop();
            Console.WriteLine($"   ⏱️ Время: {sw.ElapsedMilliseconds}ms");

            // ✅ БЫСТРО: Параллельное выполнение
            Console.WriteLine("✅ Параллельно (Task.WhenAll):");
            sw.Restart();
            
            await Task.WhenAll(
                ProcessOrderAsync("Заказ-1"),
                ProcessOrderAsync("Заказ-2"),
                ProcessOrderAsync("Заказ-3")
            );
            
            sw.Stop();
            Console.WriteLine($"   ⚡ Время: {sw.ElapsedMilliseconds}ms");
            Console.WriteLine("   🎯 Ускорение в ~3 раза!");
            Console.WriteLine();
        }

        // 3. Обработка ошибок
        private async Task DemoErrorHandling()
        {
            Console.WriteLine("--- 3. Обработка ошибок ---");

            try
            {
                // Одна задача упадет, но остальные продолжат работу
                await Task.WhenAll(
                    ReliableTaskAsync("Задача-1"),
                    FailingTaskAsync("Задача-2"), // Эта упадет
                    ReliableTaskAsync("Задача-3")
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Одна из задач упала: {ex.Message}");
                Console.WriteLine("💡 Но остальные задачи завершились успешно!");
            }
            Console.WriteLine();
        }

        // 4. Реальные сценарии
        private async Task DemoRealWorldScenarios()
        {
            Console.WriteLine("--- 4. Реальные сценарии ---");

            // Сценарий 1: Загрузка данных из нескольких API
            await LoadDashboardDataAsync();

            // Сценарий 2: Обработка файлов
            await ProcessMultipleFilesAsync();

            // Сценарий 3: Уведомления пользователей
            await SendNotificationsAsync();
        }

        // Реальный сценарий: Загрузка данных для дашборда
        private async Task LoadDashboardDataAsync()
        {
            Console.WriteLine("📊 Загружаем дашборд...");
            var sw = Stopwatch.StartNew();

            // Запускаем все запросы параллельно
            var tasks = new[]
            {
                LoadUserProfileAsync(),
                LoadSalesDataAsync(),
                LoadAnalyticsAsync(),
                LoadNotificationsAsync(),
                LoadRecentOrdersAsync()
            };

            var results = await Task.WhenAll(tasks);
            sw.Stop();

            Console.WriteLine($"✅ Дашборд загружен за {sw.ElapsedMilliseconds}ms:");
            foreach (var result in results)
            {
                Console.WriteLine($"   📊 {result}");
            }
            Console.WriteLine();
        }

        // Реальный сценарий: Обработка нескольких файлов
        private async Task ProcessMultipleFilesAsync()
        {
            Console.WriteLine("📁 Обрабатываем файлы...");
            
            var files = new[] { "report.pdf", "data.xlsx", "backup.zip", "logs.txt" };
            
            var tasks = files.Select(file => ProcessFileAsync(file));
            await Task.WhenAll(tasks);
            
            Console.WriteLine("✅ Все файлы обработаны!");
            Console.WriteLine();
        }

        // Реальный сценарий: Массовые уведомления
        private async Task SendNotificationsAsync()
        {
            Console.WriteLine("📧 Отправляем уведомления...");
            
            var users = new[] { "user1@mail.ru", "user2@mail.ru", "user3@mail.ru", "user4@mail.ru" };
            
            var tasks = users.Select(user => SendEmailAsync(user));
            await Task.WhenAll(tasks);
            
            Console.WriteLine("✅ Все уведомления отправлены!");
            Console.WriteLine();
        }

        // Вспомогательные методы
        private async Task<string> DownloadDataAsync(string server, int delayMs)
        {
            Console.WriteLine($"   🔄 Скачиваем с {server}...");
            await Task.Delay(delayMs);
            Console.WriteLine($"   ✅ {server} завершен");
            return $"Данные с {server}";
        }

        private async Task ProcessOrderAsync(string orderId)
        {
            Console.WriteLine($"   📦 Обрабатываем {orderId}...");
            await Task.Delay(500);
            Console.WriteLine($"   ✅ {orderId} готов");
        }

        private async Task<string> ReliableTaskAsync(string name)
        {
            Console.WriteLine($"   🔄 Выполняем {name}...");
            await Task.Delay(300);
            Console.WriteLine($"   ✅ {name} завершена");
            return $"Результат {name}";
        }

        private async Task<string> FailingTaskAsync(string name)
        {
            Console.WriteLine($"   💥 {name} сейчас упадет...");
            await Task.Delay(200);
            throw new InvalidOperationException($"{name} упала с ошибкой!");
        }

        // Методы для дашборда
        private async Task<string> LoadUserProfileAsync()
        {
            await Task.Delay(300);
            return "Профиль пользователя";
        }

        private async Task<string> LoadSalesDataAsync()
        {
            await Task.Delay(500);
            return "Данные продаж";
        }

        private async Task<string> LoadAnalyticsAsync()
        {
            await Task.Delay(400);
            return "Аналитика";
        }

        private async Task<string> LoadNotificationsAsync()
        {
            await Task.Delay(200);
            return "Уведомления";
        }

        private async Task<string> LoadRecentOrdersAsync()
        {
            await Task.Delay(600);
            return "Последние заказы";
        }

        private async Task ProcessFileAsync(string fileName)
        {
            Console.WriteLine($"   📄 Обрабатываем {fileName}...");
            await Task.Delay(300);
            Console.WriteLine($"   ✅ {fileName} готов");
        }

        private async Task SendEmailAsync(string email)
        {
            Console.WriteLine($"   📧 Отправляем письмо {email}...");
            await Task.Delay(250);
            Console.WriteLine($"   ✅ Письмо {email} отправлено");
        }

        // Объяснение важности Task.WhenAll
        private void ShowImportance()
        {
            Console.WriteLine("--- 🎯 ВАЖНОСТЬ TASK.WHENALL ---");
            Console.WriteLine();
            
            Console.WriteLine("❌ БЕЗ Task.WhenAll:");
            Console.WriteLine("   • Задачи выполняются последовательно");
            Console.WriteLine("   • Время = сумма всех задач");
            Console.WriteLine("   • Неэффективное использование ресурсов");
            Console.WriteLine("   • Медленная работа приложения");
            Console.WriteLine();
            
            Console.WriteLine("✨ С Task.WhenAll:");
            Console.WriteLine("   • 🚀 Параллельное выполнение - все задачи сразу");
            Console.WriteLine("   • ⚡ Время = время самой долгой задачи");
            Console.WriteLine("   • 🎯 Максимальное использование ресурсов");
            Console.WriteLine("   • 📈 Ускорение в N раз (где N - количество задач)");
            Console.WriteLine("   • 🛡️ Ждет завершения ВСЕХ задач");
            Console.WriteLine();
            
            Console.WriteLine("📊 МАТЕМАТИКА:");
            Console.WriteLine("   3 задачи по 1 секунде каждая:");
            Console.WriteLine("   • Последовательно: 1 + 1 + 1 = 3 сек");
            Console.WriteLine("   • Task.WhenAll: max(1, 1, 1) = 1 сек");
            Console.WriteLine("   • Ускорение: 300%! 🚀");
            Console.WriteLine();
            
            Console.WriteLine("🏆 ИДЕАЛЬНО ДЛЯ:");
            Console.WriteLine("   ✅ Загрузка данных из нескольких API");
            Console.WriteLine("   ✅ Обработка множества файлов");
            Console.WriteLine("   ✅ Массовые операции (email, уведомления)");
            Console.WriteLine("   ✅ Параллельные вычисления");
            Console.WriteLine("   ✅ Инициализация нескольких сервисов");
            Console.WriteLine();
            
            Console.WriteLine("⚠️ ВАЖНО:");
            Console.WriteLine("   • Если одна задача упала - Task.WhenAll тоже упадет");
            Console.WriteLine("   • Но остальные задачи продолжат работу в фоне");
            Console.WriteLine("   • Используйте try-catch для обработки ошибок");
        }
    }

    // Расширенный пример с обработкой результатов
    public class AdvancedTaskWhenAllExample
    {
        public async Task DemoAdvancedPatterns()
        {
            Console.WriteLine("\n--- 🔥 ПРОДВИНУТЫЕ ПАТТЕРНЫ ---");

            // Паттерн 1: Обработка результатов с индексами
            await ProcessWithIndexes();

            // Паттерн 2: Ограничение количества параллельных задач
            await ProcessWithThrottling();
        }

        private async Task ProcessWithIndexes()
        {
            Console.WriteLine("📋 Обработка с индексами:");
            
            var urls = new[] { "api1.com", "api2.com", "api3.com" };
            var tasks = urls.Select((url, index) => 
                FetchDataWithIndexAsync(index, url));
            
            var results = await Task.WhenAll(tasks);
            
            foreach (var result in results)
            {
                Console.WriteLine($"   {result}");
            }
            Console.WriteLine();
        }

        private async Task ProcessWithThrottling()
        {
            Console.WriteLine("🎚️ Ограничение параллелизма (по 2 задачи):");
            
            var items = Enumerable.Range(1, 8).ToList();
            const int batchSize = 2;
            
            for (int i = 0; i < items.Count; i += batchSize)
            {
                var batch = items.Skip(i).Take(batchSize);
                var tasks = batch.Select(item => ProcessItemAsync(item));
                
                await Task.WhenAll(tasks);
                Console.WriteLine($"   Batch {i/batchSize + 1} завершен");
            }
        }

        private async Task<string> FetchDataWithIndexAsync(int index, string url)
        {
            await Task.Delay(300);
            return $"№{index}: Данные с {url}";
        }

        private async Task ProcessItemAsync(int item)
        {
            Console.WriteLine($"     🔄 Обрабатываем элемент {item}");
            await Task.Delay(500);
            Console.WriteLine($"     ✅ Элемент {item} готов");
        }
    }
} 