using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using doc_stack_app_api.Store;
using System;
using System.Net.Http;
using System.Globalization;
using System.Text.RegularExpressions;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace docstackapp.Controllers
{
    [Route("api/[controller]")]
    public class UploadController : Controller
    {
        private readonly IConfiguration config;
        private readonly IHostingEnvironment environment;
        private readonly ILogger<UploadController> logger;
        private readonly IQueueService queue;

        public UploadController(IHostingEnvironment environment, ILogger<UploadController> logger, IConfiguration config, IQueueService queue)
        {
            this.config = config;
            this.environment = environment;
            this.logger = logger;
            this.queue = queue;
        }

        [HttpGet]
        public async Task<string> Get(ICollection<IFormFile> file)
        {
            return "upload a file to this endpoint!";
        }

        [HttpPost]
        public async Task<bool> Index(ICollection<IFormFile> file)
        {
            this.logger.LogInformation("Document uploaded");
            var allowedContentTypes = new List<string>() { "image/png", "image/jpg", "image/jpeg", "image/gif", "application/pdf" };
            var result = false;
            var uploads = Path.Combine(environment.WebRootPath, "uploads");
            foreach (var f in file)
            {
                if (!allowedContentTypes.Contains(f.ContentType))
                {   
                    return false;
                }

                if (f.Length > 0)
                {
                    var documentId = System.Guid.NewGuid();
                    var user = "dummyUser";
                    var client = "dummyClient";
                    var bytes = ConvertToBytes(f);
                    var stringRepresentationOfFile = System.Convert.ToBase64String(bytes);

                    this.logger.LogInformation("Adding document to store...");

                    //adding doc to store
                    await AddToStore(user, client, documentId, f, this.config["StoreHostName"]);

                    //adding doc to queue for further processing
                    this.logger.LogInformation("Adding document to queue...");
                    var payload = $"{{\"id\":\"{documentId}\",\"name\":\"{f.FileName}\", \"size\":{f.Length}, \"user\":\"{user}\", \"client\":\"{client}\", \"content\":\"{stringRepresentationOfFile}\"}}";
                    AddToQueue("documents:process:0", payload);
                    result = true;
                }
            }
            return result;
        }

        public async Task<bool> AddToStore(string user, string clientName, Guid documentId, IFormFile f, string uploadHost)
        {
            byte[] document = ConvertToBytes(f);
            using (var client = new HttpClient())
            {
                using (var content = new MultipartFormDataContent("Upload----" + DateTime.Now.ToString(CultureInfo.InvariantCulture)))
                {
                    var values = new[]
                    {
                        new KeyValuePair<string, string>("user", user),
                        new KeyValuePair<string, string>("client", clientName),
                        new KeyValuePair<string, string>("id", documentId.ToString())
                    };

                    foreach (var keyValuePair in values)
                    {
                        content.Add(new StringContent(keyValuePair.Value), keyValuePair.Key);
                    }

                    content.Add(new StreamContent(new MemoryStream(document)), "file", f.FileName);
                    using (var message = await client.PostAsync($"http://{uploadHost}/api/Document", content))
                    {
                        var input = await message.Content.ReadAsStringAsync();
                        this.logger.LogInformation($"Document uploades -> {input}");
                        return true;
                    }
                }
            }
        }

        internal static byte[] ConvertToBytes(IFormFile image)
        {
            byte[] bytes = null;
            BinaryReader reader = new BinaryReader(image.OpenReadStream());
            bytes = reader.ReadBytes((int)image.Length);
            return bytes;
        }


        internal void AddToQueue(string key, string value)
        {
            queue.AddItem(key, value);
        }



    }
}
