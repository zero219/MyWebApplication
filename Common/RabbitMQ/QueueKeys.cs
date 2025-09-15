using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.RabbitMQ
{
    public class QueueKeys
    {
        public const string User_Queue = "user_queue";

        public const string Work_Queue = "work_queue";

        public const string Order_Queue = "order_queue";

        public const string Sys_Queue = "sys_queue";
    }

    public class ExchangeKeys
    {
        public const string Orders_Exchange = "orders.exchange";

        public const string Sys_Exchange = "sys.exchange";
    }

    public class RoutingKeys
    {
        public const string Orders_Created = "order.created";

        public const string Sys_Created = "sys.created";
    }
}
