using Microsoft.Extensions.Configuration;
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
        private readonly ILogger logger;
        private readonly IConfiguration configuration;

        public RedisQueueService(ILogger<RedisQueueService> logger, IConfiguration config)
        {
            this.logger = logger;
            this.configuration = config;
            this.Initialize();
        }

        public void Initialize()
        {

            this.connection = CreateConnectionMultiplexer(configuration["RedisHostName"]);
        }

        internal ConnectionMultiplexer CreateConnectionMultiplexer(string hostName)
        {
            // Use IP address to workaround https://github.com/StackExchange/StackExchange.Redis/issues/410
            this.logger.LogInformation("Trying to find redis...");
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
            try
            {
                var db = this.connection.GetDatabase();
                db.ListLeftPush(key, value);
                this.logger.LogInformation("Item added to queue");
            }
            catch (Exception ex)
            {
                this.logger.LogInformation("Item could not be added to queue => {0}", ex);
            }

        }
    }
}
