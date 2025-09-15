using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.RabbitMQ
{
    public class WorkItem
    {
        public int Id { get; set; }
        public string TaskName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
