using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;

namespace ChannelsPresentation.Benchmarks
{
    public class AsyncAwaitOnWebServer
    {
        public static void Run()
        {
            var app = WebApplication.CreateBuilder().Build();
            var delay = 2000;
            int requestsCount = 0;

            app.MapGet("/sync", () => Thread.Sleep(delay));

            app.MapGet("/async", async () => await Task.Delay(delay));

            app.Use(async (context, next) =>
                        {
                            Interlocked.Increment(ref requestsCount);
                            await next();
                        });

            new Thread(_ =>
            {
                while (true)
                {
                    ThreadPool.GetAvailableThreads(out var available, out var _);
                    ThreadPool.GetMaxThreads(out var maxThreads, out var _);
                    ThreadPool.GetMinThreads(out var minThreads, out var _);
                    Console.WriteLine($"Requests: {Volatile.Read(ref requestsCount)} - Active: {maxThreads - available}, Available: {available}, Min: {minThreads}, Max: {maxThreads}");
                    Thread.Sleep(1000);
                }
            })
            {
                IsBackground = true,
            }.Start();

            app.Run("http://localhost:5000");   
        }
    }
}