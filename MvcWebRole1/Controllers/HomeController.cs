using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Queue;


namespace MvcWebRole1.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.DevelopmentStorageAccount;

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            
            CloudQueue queue = queueClient.GetQueueReference("batatinhas");
            queue.CreateIfNotExists();
            queue.AddMessage(new CloudQueueMessage("Hello world!"));
            
            return View();
        }
    }
}
