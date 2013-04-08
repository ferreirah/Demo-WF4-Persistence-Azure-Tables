using System;
using Microsoft.WindowsAzure.Storage.Table.DataServices;

namespace MyLib.WF4.DurableInstancing
{
    internal class AssociationRow : TableServiceEntity
    {
        public Guid Key { get; set; }
    }
}
