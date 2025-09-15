using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Common.RabbitMQ
{

    /// <summary>
    /// MQ配置项
    /// </summary>
    public class RabbitMQOptions
    {
        /// <summary>
        /// RabbitMQ服务器主机名或IP地址
        /// </summary>
        public string HostName { get; set; }
        /// <summary>
        /// RabbitMQ服务器端口号
        /// </summary>
        public int Port { get; set; }
        /// <summary>
        /// 虚拟主机路径
        /// </summary>
        public string VirtualHost { get; set; }
        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        /// 是否立即初始化连接
        /// </summary>
        public bool InitializeImmediately { get; set; }
        /// <summary>
        /// 网络恢复间隔时间
        /// </summary>
        public TimeSpan? NetworkRecoveryInterval { get; set; }
        /// <summary>
        /// 请求的心跳间隔时间
        /// </summary>
        public TimeSpan? RequestedHeartbeat { get; set; }
    }

    /// <summary>
    /// 工作队列配置
    /// </summary>
    public class WorkQueueOptions
    {
        /// <summary>
        /// 队列名称
        /// </summary>
        public string QueueName { get; set; }
        /// <summary>
        /// 是否持久化队列（服务器重启后队列仍然存在）
        /// 默认值：true
        /// </summary>
        public bool Durable { get; set; } = true;
        /// <summary>
        /// 是否为独占队列（仅限当前连接使用）
        /// 默认值：false
        /// </summary>
        public bool Exclusive { get; set; } = false;
        /// <summary>
        /// 是否自动删除队列（当所有消费者断开连接后自动删除）
        /// 默认值：false
        /// </summary>
        public bool AutoDelete { get; set; } = false;
        /// <summary>
        /// 预取数量（每个消费者最多同时处理的消息数量）
        /// 默认值：1（公平分发模式）
        /// </summary>
        public ushort PrefetchCount { get; set; } = 1;
        /// <summary>
        /// 队列附加参数（用于设置特殊功能如：消息TTL、队列长度限制等）
        /// </summary>
        public Dictionary<string, object> Arguments { get; set; }
    }

    /// <summary>
    /// 死信队列配置
    /// </summary>
    public class DeadLetterConfig
    {
        /// <summary>
        /// 死信交换机名称
        /// </summary>
        public string DeadLetterExchange { get; set; } = "dead.letter.exchange";

        /// <summary>
        /// 死信队列名称
        /// </summary>
        public string DeadLetterQueue { get; set; } = "dead_letter_queue";

        /// <summary>
        /// 死信路由键
        /// </summary>
        public string DeadLetterRoutingKey { get; set; } = "dead.letter";

        /// <summary>
        /// 死信交换机类型
        /// </summary>
        public string DeadLetterExchangeType { get; set; } = ExchangeType.Direct;

        /// <summary>
        /// 消息存活时间（毫秒），超过时间未消费则进入死信队列
        /// </summary>
        public int? MessageTTL { get; set; }

        /// <summary>
        /// 队列最大长度，超过长度则最早的消息进入死信队列
        /// </summary>
        public int? MaxLength { get; set; }

        /// <summary>
        /// 最大优先级
        /// </summary>
        public int? MaxPriority { get; set; }

        /// <summary>
        /// 队列溢出行为, reject-publish, drop-head
        /// </summary>
        public string OverflowBehavior { get; set; }
    }


}
