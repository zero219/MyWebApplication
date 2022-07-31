using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Common.RabbitMQ
{
    /// <summary>
    /// 创建队列实体
    /// </summary>
    public class QueueDeclareData
    {
        /// <summary>
        /// 队列名称
        /// </summary>
        public string Queue { get; set; }
        /// <summary>
        /// 是否持久化
        /// </summary>
        public bool Durable { get; set; } = false;
        /// <summary>
        /// 是否独占
        /// </summary>
        public bool Exclusive { get; set; } = false;
        /// <summary>
        /// 是否自动删除
        /// </summary>
        public bool AutoDelete { get; set; } = false;
        /// <summary>
        /// 参数
        /// </summary>
        public Dictionary<string, object> Arguments { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 发送消息实体
    /// </summary>
    public class BasicPublishData
    {
        /// <summary>
        /// 交换机
        /// </summary>
        public string Exchange { get; set; } = string.Empty;
        /// <summary>
        /// 路由名称
        /// </summary>
        public string RoutingKey { get; set; }
        /// <summary>
        /// 配置信息
        /// </summary>
        public IBasicProperties BasicProperties { get; set; } = null;
        /// <summary>
        /// 发送的消息
        /// </summary>
        public ReadOnlyMemory<byte> Body { get; set; }
    }

    /// <summary>
    /// 接收消息实体
    /// </summary>
    public class BasicConsumeData
    {
        /// <summary>
        /// 队列名称
        /// </summary>
        public string Queue { get; set; }

        /// <summary>
        /// 是否自动确认
        /// </summary>
        public bool AutoAck { get; set; } = false;

        /// <summary>
        /// 回调对象
        /// </summary>
        public IBasicConsumer Consumer { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ExchangeDeclareData
    {
        /// <summary>
        /// 交换机
        /// </summary>
        public string Exchange { get; set; }
        /// <summary>
        /// 交换机类型（Direct：定向、Fanout：扇形、Headers：参数匹配、Topic：通配符） 
        /// </summary>
        public string Type { get; set;}

        /// <summary>
        /// 是否持久化
        /// </summary>
        public bool Durable { get; set; } = true;
        /// <summary>
        /// 是否自动删除
        /// </summary>
        public bool AutoDelete { get; set; } = false;
        /// <summary>
        /// 参数
        /// </summary>
        public Dictionary<string, object> Arguments { get; set; } = new Dictionary<string, object>();
    }
    /// <summary>
    /// 绑定队列和交换机实体
    /// </summary>
    public class QueueBindData
    {
        /// <summary>
        /// 队列名称
        /// </summary>
        public string Queue { get; set; }
        /// <summary>
        /// 交换机
        /// </summary>
        public string Exchange { get; set; }

        /// <summary>
        /// 路由名称
        /// </summary>
        public string RoutingKey { get; set; } = string.Empty;
    }
}
