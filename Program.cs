using System.Threading;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LogIPAndLimitConnections
{
    public class Program
    {
        private const int MaxConnections = 5;

        private static int _connectionCount;

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.ConfigureKestrel(kestrelOptions =>
                    {
                        kestrelOptions.ListenAnyIP(5000, listenOptions =>
                        {
                            var logger = listenOptions.ApplicationServices.GetRequiredService<ILogger<Program>>();

                            listenOptions.Use(next =>
                            {
                                return async (connectionContext) =>
                                {
                                    var currentConnectionCount = Interlocked.Increment(ref _connectionCount);

                                    try
                                    {
                                        if (currentConnectionCount > MaxConnections)
                                        {
                                            logger.LogWarning("{ConnectionId} rejected because the concurrent limit exceeded. ({RemoteIP}, {LocalIP})",
                                                connectionContext.ConnectionId,
                                                connectionContext.RemoteEndPoint,
                                                connectionContext.LocalEndPoint);
                                            return;
                                        }

                                        logger.LogInformation("{ConnectionId} accepted.",
                                            connectionContext.ConnectionId,
                                            connectionContext.RemoteEndPoint,
                                            connectionContext.LocalEndPoint);

                                        await next(connectionContext);
                                    }
                                    finally
                                    {
                                        Interlocked.Decrement(ref _connectionCount);
                                    }
                                };
                            });
                        });
                    });
                });
    }
}
