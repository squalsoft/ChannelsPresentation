using System.Threading.Tasks;

namespace ChannelsPresentation
{
    public class SimpleAsyncExample
    {
        public void Run()
        {
            Console.WriteLine("=== Короткий пример: Синхронный vs Асинхронный ===\n");
            
            Console.WriteLine("❌ СИНХРОННЫЙ (блокирует поток):");
            Console.WriteLine(@"
public void SaveUser(User user)
{
    _context.Users.Add(user);
    _context.SaveChanges();        // 🔥 БЛОКИРУЕТ поток!
    Console.WriteLine(""Saved"");   // Выполнится только после БД
}");

            Console.WriteLine("\n✅ АСИНХРОННЫЙ (не блокирует поток):");
            Console.WriteLine(@"
public async Task SaveUserAsync(User user)
{
    _context.Users.Add(user);
    await _context.SaveChangesAsync();  // 🚀 НЕ блокирует!
    Console.WriteLine(""Saved"");        // Поток свободен для других задач
}");

            Console.WriteLine("\n🎯 РАЗНИЦА:");
            Console.WriteLine("• Синхронный: поток ждет БД → зависание UI/сервера");
            Console.WriteLine("• Асинхронный: поток свободен → отзывчивость приложения");
            Console.WriteLine("• Результат: async = в 10-100 раз больше пользователей!");
        }
    }
} 