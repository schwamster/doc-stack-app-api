using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using doc_stack_app_api.Store;

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
                    
                    this.logger.LogInformation("Adding document to queue");
                    
                    //adding doc to store
                    AddToStore(user, client, documentId, f);

                    //adding doc to queue for further processing
                    var payload = $"{{\"id\":\"{documentId}\",\"name\":\"{f.FileName}\", \"size\":{f.Length}, \"user\":\"{user}\", \"client\":\"{client}\", \"content\":\"{stringRepresentationOfFile}\"}}";
                    AddToQueue("documents:process:0", payload);
                    result = true;
                }
            }
            return result;
        }

        internal byte[] ConvertToBytes(IFormFile image)
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
