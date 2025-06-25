using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ChannelsPresentation
{
    public class ThreadYieldExample
    {
        private readonly List<int> _sharedList = new();
        private readonly object _lockObject = new();
        private volatile bool _completed = false;

        public async Task Run()
        {
            Console.WriteLine("=== Проблема конкурентного доступа и решение с Thread.Yield ===\n");

            await RunBadExample();
            await RunGoodExample();
            await RunPerformanceComparison();

            // РЕЗУЛЬТАТЫ СРАВНЕНИЯ (5000 элементов):
            //     ┌─────────────────────────┬──────────────┬────────────────┐
            //     │ Метод                   │ Время (ms)   │ Итерации       │
            //     ├─────────────────────────┼──────────────┼────────────────┤
            //     │ Без Thread.Yield        │          57  │       3361358  │
            //     │ С Thread.Yield          │          56  │        232303  │
            //     └─────────────────────────┴──────────────┴────────────────┘

        }

        // ❌ ПЛОХОЙ ПРИМЕР: Активное ожидание без Thread.Yield
        private async Task RunBadExample()
        {
            Console.WriteLine("--- ❌ ПЛОХОЙ ПРИМЕР: Без Thread.Yield ---");
            
            _sharedList.Clear();
            _completed = false;
            
            var stopwatch = Stopwatch.StartNew();
            var iterations = 0;

            // Producer: добавляет элементы в список
            var producer = Task.Run(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    lock (_lockObject)
                    {
                        _sharedList.Add(i);
                    }
                    Thread.Sleep(1); // Имитация работы
                }
                _completed = true;
                Console.WriteLine("📤 Producer завершен");
            });

            // Consumer: читает элементы из списка (ПЛОХО - активное ожидание)
            var consumer = Task.Run(() =>
            {
                var processedCount = 0;
                
                while (!_completed || _sharedList.Count > 0)
                {
                    iterations++;
                    int item = -1;
                    bool hasItem = false;
                    
                    lock (_lockObject)
                    {
                        if (_sharedList.Count > 0)
                        {
                            item = _sharedList[0];
                            _sharedList.RemoveAt(0); // Неэффективная операция O(n)
                            hasItem = true;
                        }
                    }
                    
                    if (hasItem)
                    {
                        processedCount++;
                        // Имитация обработки
                        Thread.Sleep(1);
                    }
                    // ⚠️ ПРОБЛЕМА: Нет Thread.Yield() - процессор тратится впустую!
                }
                
                Console.WriteLine($"📥 Consumer обработал: {processedCount} элементов");
                return processedCount;
            });

            await Task.WhenAll(producer, consumer);
            stopwatch.Stop();
            
            Console.WriteLine($"⏱️ Время выполнения: {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"🔄 Количество итераций цикла: {iterations:N0}");
            Console.WriteLine($"💻 CPU загрузка: ВЫСОКАЯ (активное ожидание)\n");
        }

        // ✅ ХОРОШИЙ ПРИМЕР: С Thread.Yield для снижения нагрузки на CPU
        private async Task RunGoodExample()
        {
            Console.WriteLine("--- ✅ ХОРОШИЙ ПРИМЕР: С Thread.Yield ---");
            
            _sharedList.Clear();
            _completed = false;
            
            var stopwatch = Stopwatch.StartNew();
            var iterations = 0;

            // Producer: тот же самый
            var producer = Task.Run(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    lock (_lockObject)
                    {
                        _sharedList.Add(i);
                    }
                    Thread.Sleep(1);
                }
                _completed = true;
                Console.WriteLine("📤 Producer завершен");
            });

            // Consumer: с Thread.Yield для оптимизации
            var consumer = Task.Run(() =>
            {
                var processedCount = 0;
                
                while (!_completed || _sharedList.Count > 0)
                {
                    iterations++;
                    int item = -1;
                    bool hasItem = false;
                    
                    lock (_lockObject)
                    {
                        if (_sharedList.Count > 0)
                        {
                            item = _sharedList[0];
                            _sharedList.RemoveAt(0);
                            hasItem = true;
                        }
                    }
                    
                    if (hasItem)
                    {
                        processedCount++;
                        Thread.Sleep(1);
                    }
                    else
                    {
                        // ✅ РЕШЕНИЕ: Thread.Yield() передает управление другим потокам
                        Thread.Yield();
                    }
                }
                
                Console.WriteLine($"📥 Consumer обработал: {processedCount} элементов");
                return processedCount;
            });

            await Task.WhenAll(producer, consumer);
            stopwatch.Stop();
            
            Console.WriteLine($"⏱️ Время выполнения: {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"🔄 Количество итераций цикла: {iterations:N0}");
            Console.WriteLine($"💻 CPU загрузка: НИЗКАЯ (кооперативное ожидание)\n");
        }

        // 📊 Сравнение производительности
        private async Task RunPerformanceComparison()
        {
            Console.WriteLine("--- 📊 Сравнение производительности ---");
            
            const int testItems = 5000;
            
            // Тест без Thread.Yield
            var (timeWithout, iterationsWithout) = await MeasurePerformance(testItems, false);
            
            // Тест с Thread.Yield
            var (timeWith, iterationsWith) = await MeasurePerformance(testItems, true);
            
            Console.WriteLine($"\n📈 РЕЗУЛЬТАТЫ СРАВНЕНИЯ ({testItems} элементов):");
            Console.WriteLine($"┌─────────────────────────┬──────────────┬────────────────┐");
            Console.WriteLine($"│ Метод                   │ Время (ms)   │ Итерации       │");
            Console.WriteLine($"├─────────────────────────┼──────────────┼────────────────┤");
            Console.WriteLine($"│ Без Thread.Yield        │ {timeWithout,11}  │ {iterationsWithout,14:N0} │");
            Console.WriteLine($"│ С Thread.Yield          │ {timeWith,11}  │ {iterationsWith,14:N0} │");
            Console.WriteLine($"└─────────────────────────┴──────────────┴────────────────┘");
            
            var efficiencyImprovement = (double)iterationsWithout / iterationsWith;
            Console.WriteLine($"\n🚀 Улучшение эффективности: {efficiencyImprovement:F1}x меньше итераций");
            Console.WriteLine($"💡 Thread.Yield() снижает нагрузку на CPU и улучшает отзывчивость");
        }

        private async Task<(long timeMs, long iterations)> MeasurePerformance(int itemCount, bool useYield)
        {
            _sharedList.Clear();
            _completed = false;
            
            var stopwatch = Stopwatch.StartNew();
            long iterations = 0;

            var producer = Task.Run(() =>
            {
                for (int i = 0; i < itemCount; i++)
                {
                    lock (_lockObject)
                    {
                        _sharedList.Add(i);
                    }
                    if (i % 100 == 0) Thread.Sleep(1); // Периодическая задержка
                }
                _completed = true;
            });

            var consumer = Task.Run(() =>
            {
                var processedCount = 0;
                
                while (!_completed || _sharedList.Count > 0)
                {
                    Interlocked.Increment(ref iterations);
                    int item = -1;
                    bool hasItem = false;
                    
                    lock (_lockObject)
                    {
                        if (_sharedList.Count > 0)
                        {
                            item = _sharedList[0];
                            _sharedList.RemoveAt(0);
                            hasItem = true;
                        }
                    }
                    
                    if (hasItem)
                    {
                        processedCount++;
                    }
                    else if (useYield)
                    {
                        Thread.Yield(); // Используем только если разрешено
                    }
                }
                
                return processedCount;
            });

            await Task.WhenAll(producer, consumer);
            stopwatch.Stop();
            
            return (stopwatch.ElapsedMilliseconds, iterations);
        }
    }
} 