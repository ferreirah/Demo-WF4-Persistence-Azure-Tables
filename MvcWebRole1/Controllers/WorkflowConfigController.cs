using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json.Linq;

namespace MvcWebRole1.Controllers
{
    public class WorkflowConfigController : ApiController
    {
        private CloudStorageAccount account;
        private CloudQueueClient queueClient;
        private CloudQueue queue;

        public WorkflowConfigController()
        {
            account = CloudStorageAccount.DevelopmentStorageAccount;
            queueClient = account.CreateCloudQueueClient();
            queue = queueClient.GetQueueReference("batatinhas");
            queue.CreateIfNotExists();
        }

        public HttpResponseMessage Post()
        {
            queue.AddMessage(new CloudQueueMessage("Hello World!"));

            return new HttpResponseMessage(HttpStatusCode.Accepted);
        }

        public HttpResponseMessage Get()
        {
            var messages = queue.PeekMessages(500);
            JObject obj = new JObject();
            JArray array = new JArray();

            obj.Add("messages", array);

            foreach (var _message in messages)
            {
                JObject jsonMessage = new JObject();
                jsonMessage.Add("id", _message.Id);
                jsonMessage.Add("content", _message.AsString);
                jsonMessage.Add("expiration_time", _message.ExpirationTime);
                array.Add(jsonMessage);
            }

            return Request.CreateResponse(HttpStatusCode.OK, obj);
        }
    }
}