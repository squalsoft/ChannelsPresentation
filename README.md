# 🚀 ChannelsPresentation

**Образовательный проект-презентация о System.Threading.Channels в .NET**

Этот проект демонстрирует различные подходы к асинхронной обработке данных в .NET, с фокусом на **Channels** как современном решении для producer-consumer сценариев.

## 📋 Содержание

- [Возможности](#-возможности)
- [Быстрый старт](#-быстрый-старт)
- [Структура проекта](#-структура-проекта)
- [Примеры кода](#-примеры-кода)
- [Бенчмарки](#-бенчмарки)
- [Результаты производительности](#-результаты-производительности)
- [Технологии](#-технологии)
- [Требования](#-требования)

## 🎯 Возможности

### Демонстрация различных подходов:
- ✅ **System.Threading.Channels** - современное решение для асинхронных потоков данных
- ✅ **ConcurrentQueue** - lock-free коллекция для многопоточной работы
- ✅ **BlockingCollection** - классический подход с блокировками
- ✅ **TPL Dataflow** - мощный фреймворк для обработки данных
- ✅ **Reactive Extensions (Rx)** - реактивное программирование
- ✅ **Akka.NET Actors** - модель акторов для распределенных систем
- ✅ **PLINQ** - параллельные LINQ запросы

### Производительность и анализ:
- 📊 **BenchmarkDotNet** для точных измерений производительности
- 🔍 Анализ потребления памяти и аллокаций
- ⚡ Сравнение различных конфигураций Channels
- 📈 Визуализация результатов в HTML/Markdown

## 🚀 Быстрый старт

### Установка и запуск

```bash
# Клонирование репозитория
git clone <your-repo-url>
cd ChannelsPresentation

# Восстановление зависимостей
dotnet restore

# Запуск примеров
dotnet run

# Запуск бенчмарков
dotnet run -c Release -- benchmark dataprocessing
```

### Доступные команды бенчмарков

```bash
# Сравнение подходов к обработке данных
dotnet run -c Release -- benchmark dataprocessing

# Сравнение конфигураций Channels
dotnet run -c Release -- benchmark channels

# Запуск всех бенчмарков
dotnet run -c Release -- benchmark all
```

## 📁 Структура проекта

```
ChannelsPresentation/
├── 📄 Program.cs                    # Точка входа приложения
├── 📁 Benchmarks/
│   └── PerformanceBenchmarks.cs     # BenchmarkDotNet тесты
├── 📄 ChannelsExample.cs            # Основные примеры Channels
├── 📄 ComparisonExample.cs          # Сравнение различных подходов
├── 📄 ConcurrentQueueExample.cs     # Примеры ConcurrentQueue
├── 📄 BlockingCollectionExample.cs  # Примеры BlockingCollection
├── 📄 TplDataflowExample.cs         # Примеры TPL Dataflow
├── 📄 RxExample.cs                  # Примеры Reactive Extensions
├── 📄 ActorsExample.cs              # Примеры Akka.NET
├── 📄 AsyncAwaitExample.cs          # Основы async/await
├── 📄 TaskWhenAllExample.cs         # Работа с Task.WhenAll
└── 📄 SimpleAsyncExample.cs         # Простые асинхронные примеры
```

## 💡 Примеры кода

### Базовый пример Channels

```csharp
// Создание unbounded channel
var channel = Channel.CreateUnbounded<int>();
var writer = channel.Writer;
var reader = channel.Reader;

// Producer
var producer = Task.Run(async () =>
{
    for (int i = 0; i < 100; i++)
    {
        await writer.WriteAsync(i);
    }
    writer.Complete();
});

// Consumer
var consumer = Task.Run(async () =>
{
    await foreach (var item in reader.ReadAllAsync())
    {
        Console.WriteLine($"Processed: {item}");
    }
});

await Task.WhenAll(producer, consumer);
```

### Bounded Channel с настройками

```csharp
var options = new BoundedChannelOptions(capacity: 100)
{
    FullMode = BoundedChannelFullMode.Wait,
    SingleReader = true,
    SingleWriter = false
};

var channel = Channel.CreateBounded<string>(options);
```

## 📊 Бенчмарки

Проект включает два набора бенчмарков:

### 1. DataProcessingBenchmarks
Сравнение различных подходов к обработке 10,000 элементов:

- `ProcessWithChannels` - System.Threading.Channels (базовый)
- `ProcessWithBoundedChannel` - Channels с ограниченной емкостью
- `ProcessWithConcurrentQueue` - ConcurrentQueue
- `ProcessWithBlockingCollection` - BlockingCollection
- `ProcessWithSimpleParallel` - PLINQ
- `ProcessSequentially` - Последовательная обработка
- `ProcessWithLockAndList` - List с блокировками
- `ProcessWithTplDataflow` - TPL Dataflow
- `ProcessWithRx` - Reactive Extensions
- `ProcessWithAkkaActors` - Akka.NET Actors

### 2. ChannelConfigurationBenchmarks
Сравнение различных конфигураций Channels:

- `UnboundedChannel` - Неограниченный канал
- `BoundedChannel` - Ограниченный канал с ожиданием
- `BoundedChannelDropOldest` - Ограниченный канал с удалением старых элементов

## 📈 Результаты производительности

### Сравнение подходов (10,000 элементов)

| Метод                     | Время        | Память      | Производительность |
|---------------------------|--------------|-------------|-------------------|
| `ProcessSequentially`     | **10.39 us** | 39.37 KB    | 🥇 Самый быстрый   |
| `ProcessWithSimpleParallel` | 42.56 us   | 126.13 KB   | 🥈 Отличная        |
| `ProcessWithRx`           | 79.85 us     | 128.98 KB   | 🥉 Хорошая         |
| `ProcessWithConcurrentQueue` | 189.55 us | 148.02 KB   | ✅ Приемлемая      |
| `ProcessWithChannels`     | 479.14 us    | 198.55 KB   | ✅ Приемлемая      |
| `ProcessWithBoundedChannel` | 1,222.34 us | 138.74 KB  | ⚠️ Медленная       |
| `ProcessWithBlockingCollection` | 1,353.60 us | 151.66 KB | ⚠️ Медленная    |
| `ProcessWithLockAndList`  | 1,658.23 us  | 168.53 KB   | 🐌 Очень медленная |
| `ProcessWithTplDataflow`  | 1,793.53 us  | 219.42 KB   | 🐌 Очень медленная |
| `ProcessWithAkkaActors`   | 2,947.41 us  | 2817.46 KB  | 🐌 Самая медленная |

### Ключевые выводы

1. **Для простых вычислений** - используйте `PLINQ` или последовательную обработку
2. **Для асинхронных producer-consumer сценариев** - `Channels` или `ConcurrentQueue`
3. **Избегайте** `lock` с `List` для высоконагруженных сценариев
4. **Акторы** подходят для сложной бизнес-логики, но медленнее для простых операций

## 🛠 Технологии

- **.NET 8.0** - Целевая платформа
- **System.Threading.Channels** - Основная библиотека для демонстрации
- **BenchmarkDotNet** - Профессиональные бенчмарки
- **Akka.NET** - Модель акторов
- **System.Reactive** - Reactive Extensions
- **TPL Dataflow** - Обработка данных

## 📋 Требования

- **.NET 8.0 SDK** или выше
- **Visual Studio 2022** / **Rider** / **VS Code** (опционально)
- **PowerShell** / **Terminal** для запуска команд

## 🎓 Обучающие материалы

### Когда использовать Channels?

✅ **Подходит для:**
- Producer-Consumer сценариев
- Асинхронной обработки потоков данных
- Backpressure management
- Интеграции с async/await

❌ **Не подходит для:**
- Простых вычислений (используйте PLINQ)
- Синхронных алгоритмов
- Случаев, где достаточно ConcurrentQueue

### Конфигурация Channels

```csharp
// Выбор типа канала
var unbounded = Channel.CreateUnbounded<T>();     // Неограниченный
var bounded = Channel.CreateBounded<T>(capacity); // Ограниченный

// Настройка поведения при переполнении
var options = new BoundedChannelOptions(100)
{
    FullMode = BoundedChannelFullMode.Wait,        // Ждать
    FullMode = BoundedChannelFullMode.DropOldest,  // Удалять старые
    FullMode = BoundedChannelFullMode.DropNewest,  // Удалять новые
    FullMode = BoundedChannelFullMode.DropWrite,   // Игнорировать запись
};
```

## 🤝 Вклад в проект

Приветствуются предложения по улучшению:

1. Fork репозитория
2. Создайте feature branch: `git checkout -b feature/amazing-feature`
3. Commit изменения: `git commit -m 'Add amazing feature'`
4. Push в branch: `git push origin feature/amazing-feature`
5. Откройте Pull Request

## 📝 Лицензия

Этот проект создан в образовательных целях. Используйте код свободно для обучения и экспериментов.

---

**💡 Совет:** Запустите бенчмарки на вашей машине - результаты могут отличаться в зависимости от железа!

```bash
dotnet run -c Release -- benchmark dataprocessing
``` 