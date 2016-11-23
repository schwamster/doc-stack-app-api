using System;
using doc_stack_app_api.Store;
using Xunit;

namespace Tests
{
    public class RedisQueueServiceTest
    {
         private readonly RedisQueueService _redisService;
        //  public RedisQueueServiceTest()
        //  {
        //      _redisService = new RedisQueueService(null, null);
        //  }

        [Fact]
        public void RedisQueueService_GivenLocalhost_ReturnsExpectedIp()
        {
            var result = RedisQueueService.GetIp("localhost");
            Assert.Equal("127.0.0.1", result);
        }
    }
}
