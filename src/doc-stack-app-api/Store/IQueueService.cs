using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace doc_stack_app_api.Store
{
    public interface IQueueService
    {
        void AddItem(string key, string value);
    }

    public class RedisQueueService : IQueueService
    {
        internal ConnectionMultiplexer connection;
        private string hostName;
        private readonly ILogger logger;

        public RedisQueueService(ILoggerFactory loggerFactory, string hostName)
        {
            this.logger = loggerFactory.CreateLogger<RedisQueueService>();
            this.hostName = hostName;
        }

        public void Initialize()
        {
            this.connection = CreateConnectionMultiplexer(this.hostName);
        }

        internal ConnectionMultiplexer CreateConnectionMultiplexer(string hostName)
        {
            // Use IP address to workaround https://github.com/StackExchange/StackExchange.Redis/issues/410
            var ipAddress = GetIp(hostName);
            this.logger.LogInformation($"Found redis at {ipAddress}");

            while (true)
            {
                try
                {
                    logger.LogInformation("Connecting to redis");
                    return ConnectionMultiplexer.Connect(ipAddress);
                }
                catch (RedisConnectionException ex)
                {
                    logger.LogWarning("Waiting for redis => {0}", ex);
                    Thread.Sleep(1000);
                }
            }
        }

        private static string GetIp(string hostname)
            => Dns.GetHostEntryAsync(hostname)
                .Result
                .AddressList
                .First(a => a.AddressFamily == AddressFamily.InterNetwork)
                .ToString();


        public void AddItem(string key, string value)
        {
            var db = this.connection.GetDatabase();
            db.ListLeftPush(key, value);
        }
    }
}
