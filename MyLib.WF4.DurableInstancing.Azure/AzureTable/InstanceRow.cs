using Microsoft.WindowsAzure.Storage.Table.DataServices;
using System.Xml.Linq;

namespace MyLib.WF4.DurableInstancing
{
    internal class InstanceRow : TableServiceEntity
    {
        public XDocument Data { get; set; }
        public XDocument MetaData { get; set; }
    }
}
