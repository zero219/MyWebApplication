using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;

namespace Common.RabbitMQ
{
    public partial class RabbitMQHelper : IDisposable
    {
        /// <summary>
        /// 日志记录器
        /// </summary>
        private readonly ILogger<RabbitMQHelper> _logger;

        /// <summary>
        /// 连接工厂
        /// </summary>
        private readonly ConnectionFactory _factory;

        /// <summary>
        /// 连接对象
        /// </summary>
        private IConnection _connection;

        /// <summary>
        /// 通道对象
        /// </summary>
        private IModel _channel;

        /// <summary>
        /// 释放状态标志
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// 连接锁对象
        /// </summary>
        private readonly object _connectionLock = new();

        /// <summary>
        /// 通道锁对象
        /// </summary>
        private readonly object _channelLock = new();

        /// <summary>
        /// 是否已连接
        /// </summary>
        public bool IsConnected => _connection?.IsOpen == true;

        /// <summary>
        /// 通道是否打开
        /// </summary>
        public bool IsChannelOpen => _channel?.IsOpen == true;

        /// <summary>
        /// 客户端提供的名称
        /// </summary>
        public string ClientProvidedName => _connection?.ClientProvidedName;

        /// <summary>
        /// 通道编号
        /// </summary>
        public int? ChannelNumber => _channel?.ChannelNumber;

        /// <summary>
        /// 初始化 RabbitMQHelper
        /// </summary>
        public RabbitMQHelper(RabbitMQOptions options, ILogger<RabbitMQHelper> logger = null)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            _logger = logger;

            // 创建连接工厂（推荐只初始化一次，避免重复 new）
            _factory = new ConnectionFactory
            {
                HostName = options.HostName ?? throw new ArgumentNullException(nameof(options.HostName)),
                Port = options.Port > 0 ? options.Port : 5672,
                VirtualHost = string.IsNullOrEmpty(options.VirtualHost) ? "/" : options.VirtualHost,
                UserName = options.UserName ?? throw new ArgumentNullException(nameof(options.UserName)),
                Password = options.Password ?? throw new ArgumentNullException(nameof(options.Password)),

                // 自动恢复配置
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = options.NetworkRecoveryInterval ?? TimeSpan.FromSeconds(10),
                RequestedHeartbeat = options.RequestedHeartbeat ?? TimeSpan.FromSeconds(30),

                DispatchConsumersAsync = true // 支持异步消费者
            };

            _logger?.LogInformation("RabbitMQHelper 初始化完成，目标服务器: {Host}:{Port}", _factory.HostName, _factory.Port);

            // 如果配置了立即初始化，则立刻建立连接和通道
            if (options.InitializeImmediately)
            {
                Initialize();
            }
        }

        #region 初始化连接/通道
        /// <summary>
        /// 初始化连接和通道
        /// </summary>
        private void Initialize()
        {
            lock (_connectionLock)
            {
                if (IsConnected) return;

                try
                {
                    // 建立连接
                    _connection = _factory.CreateConnection();
                    BindConnectionEvents(_connection);

                    // 创建通道
                    _channel = _connection.CreateModel();
                    BindChannelEvents(_channel);

                    // 设置 QoS，确保公平分发
                    _channel.BasicQos(0, 1, false);

                    _logger?.LogInformation("RabbitMQ 连接成功. Client: {Client}, Channel: #{Channel}",
                        _connection.ClientProvidedName, _channel.ChannelNumber);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "RabbitMQ 初始化失败");
                    throw;
                }
            }
        }

        /// <summary>
        /// 获取一个可用的通道（如果断开则会自动重建）
        /// </summary>
        public IModel GetChannel()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RabbitMQHelper));

            if (IsChannelOpen) return _channel;

            lock (_channelLock)
            {
                if (IsChannelOpen) return _channel;

                if (!IsConnected) Initialize();

                _channel = _connection.CreateModel();
                BindChannelEvents(_channel);
                // prefetchSize: 单条消息大小限制，0表示不限制
                // prefetchCount: 每次预取消息数量，1表示每次只处理1条
                // global: 作用范围，false表示针对单个消费者
                _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                _logger?.LogInformation("RabbitMQ 通道重建成功: Channel#{Channel}", _channel.ChannelNumber);
                return _channel;
            }
        }
        #endregion

        #region Exchange/Queue

        /// <summary>
        /// 声明交换机
        /// </summary>
        /// <param name="channel">RabbitMQ 通道</param>
        /// <param name="exchangeName">交换机名称</param>
        /// <param name="type">交换机类型（direct、fanout、topic、headers 等）</param>
        /// <param name="durable">是否持久化（true 表示 RabbitMQ 重启后交换机依然存在）</param>
        /// <param name="autoDelete">是否自动删除（当没有队列绑定时自动删除交换机）</param>
        /// <param name="arguments">额外参数（如延迟队列需要传入 {"x-delayed-type", "direct"}）</param>
        public void ExchangeDeclare(IModel channel, string exchangeName, string type, bool durable = true, bool autoDelete = false,
            IDictionary<string, object> arguments = null)
        {
            try
            {
                // 声明交换机
                channel.ExchangeDeclare(exchange: exchangeName, type: type, durable: durable, autoDelete: autoDelete, arguments: arguments);

                _logger?.LogDebug("交换机声明成功: {ExchangeName} ({Type})", exchangeName, type);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "交换机声明失败: {ExchangeName}", exchangeName);
                throw;
            }
        }

        /// <summary>
        /// 声明队列
        /// </summary>
        /// <param name="channel">RabbitMQ 通道</param>
        /// <param name="queueName">队列名称</param>
        /// <param name="durable">是否持久化（true 表示 RabbitMQ 重启后队列依然存在）</param>
        /// <param name="exclusive">是否排他（true 表示只有当前连接能使用，连接断开后队列会被删除）</param>
        /// <param name="autoDelete">是否自动删除（true 表示当没有消费者时自动删除队列）</param>
        /// <param name="arguments">额外参数（如死信队列：{"x-dead-letter-exchange", "dlx.exchange"}）</param>
        /// <returns>队列声明结果（包含队列名、消息数、消费者数）</returns>
        public QueueDeclareOk QueueDeclare(IModel channel, string queueName, bool durable = true, bool exclusive = false, bool autoDelete = false,
            IDictionary<string, object> arguments = null)
        {
            try
            {
                var ok = channel.QueueDeclare(queue: queueName, durable: durable, exclusive: exclusive, autoDelete: autoDelete, arguments: arguments);

                _logger?.LogDebug("队列声明成功: {QueueName}", queueName);

                return ok;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "队列声明失败: {QueueName}", queueName);
                throw;
            }
        }

        /// <summary>
        /// 将队列绑定到交换机
        /// </summary>
        /// <param name="channel">RabbitMQ 通道</param>
        /// <param name="queueName">队列名称</param>
        /// <param name="exchange">交换机名称</param>
        /// <param name="routingKey">
        /// 路由键：
        /// - direct 类型交换机：必须完全匹配路由键
        /// - topic 类型交换机：支持通配符（* 表示一个单词，# 表示多个单词）
        /// - fanout 类型交换机：忽略此参数（routingKey 传空字符串）
        /// </param>
        /// <param name="args">额外参数（一般很少用，可为 null）</param>
        public void QueueBind(IModel channel, string queueName, string exchange, string routingKey, IDictionary<string, object> args = null)
        {
            try
            {
                channel.QueueBind(queue: queueName, exchange: exchange, routingKey: routingKey, arguments: args);

                _logger?.LogDebug("队列绑定成功: {Queue} -> {Exchange} [{Key}]", queueName, exchange, routingKey);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "队列绑定失败: {Queue} -> {Exchange} [{Key}]", queueName, exchange, routingKey);
                throw;
            }
        }
        #endregion

        #region 发布确认
        /// <summary>
        /// 启用发布确认（Publisher Confirms）
        /// </summary>
        /// <param name="channel"></param>
        public void EnablePublisherConfirms(IModel channel)
        {
            // 打开确认模式
            channel.ConfirmSelect();

            // 绑定 ACK/NACK 事件
            channel.BasicAcks += (s, ea) =>
                _logger?.LogInformation("[ACK] DeliveryTag={Tag}, multiple={Multiple}", ea.DeliveryTag, ea.Multiple);

            channel.BasicNacks += (s, ea) =>
                _logger?.LogWarning("[NACK] DeliveryTag={Tag}, multiple={Multiple}", ea.DeliveryTag, ea.Multiple);
        }
        #endregion

        #region 设置通道的消息预取
        /// <summary>
        /// 设置通道的消息预取（QoS）
        /// </summary>
        /// <param name="channel">RabbitMQ 通道</param>
        /// <param name="prefetchCount">每个消费者一次最多接收的未确认消息数量</param>
        /// <param name="prefetchSize">消息大小限制（字节），一般设为 0 表示不限</param>
        /// <param name="global">是否对整个通道生效，false：针对当前消费者，true：全局</param>
        public void SetChannelQos(IModel channel, ushort prefetchCount = 1, uint prefetchSize = 0, bool global = false)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));

            channel.BasicQos(prefetchSize: prefetchSize, prefetchCount: prefetchCount, global: global);

            _logger?.LogDebug("通道 QoS 设置成功: PrefetchCount={PrefetchCount}, PrefetchSize={PrefetchSize}, Global={Global}",
                prefetchCount, prefetchSize, global);
        }
        #endregion

        #region 消费端消息确认
        /// <summary>
        /// 消息处理成功，手动确认 ACK
        /// </summary>
        /// <param name="channel">RabbitMQ 通道</param>
        /// <param name="deliveryTag">消息的 DeliveryTag</param>
        /// <param name="multiple">
        /// 是否批量确认：
        ///— true：确认 deliveryTag 及之前所有未确认的消息  
        ///— false：仅确认当前消息
        /// </param>
        public void AckMessage(IModel channel, ulong deliveryTag, bool multiple = false)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));

            channel.BasicAck(deliveryTag: deliveryTag, multiple: multiple);
            _logger?.LogDebug("消息已确认 ACK, DeliveryTag={DeliveryTag}, Multiple={Multiple}", deliveryTag, multiple);
        }

        /// <summary>
        /// 消息处理失败，手动拒绝并选择是否重新入队 NACK
        /// </summary>
        /// <param name="channel">RabbitMQ 通道</param>
        /// <param name="deliveryTag">消息的 DeliveryTag</param>
        /// <param name="multiple">
        /// 是否批量拒绝：
        ///— true：拒绝 deliveryTag 及之前所有未确认的消息  
        ///— false：仅拒绝当前消息
        /// </param>
        /// <param name="requeue">是否重新入队，true 表示重新入队，false 表示丢弃</param>
        public void NackMessage(IModel channel, ulong deliveryTag, bool multiple = false, bool requeue = true)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));

            channel.BasicNack(deliveryTag: deliveryTag, multiple: multiple, requeue: requeue);
            _logger?.LogWarning("消息处理失败 NACK, DeliveryTag={DeliveryTag}, Multiple={Multiple}, Requeue={Requeue}",
                deliveryTag, multiple, requeue);
        }
        #endregion

        #region 事件绑定
        private void BindConnectionEvents(IConnection conn)
        {
            conn.ConnectionShutdown += (s, ea) =>
                _logger?.LogWarning("连接关闭: {ReplyCode} - {ReplyText}", ea.ReplyCode, ea.ReplyText);

            conn.CallbackException += (s, ea) =>
                _logger?.LogError(ea.Exception, "连接回调异常");
        }

        private void BindChannelEvents(IModel channel)
        {
            channel.ModelShutdown += (s, ea) =>
                _logger?.LogWarning("通道关闭: {ReplyCode} - {ReplyText}", ea.ReplyCode, ea.ReplyText);

            channel.CallbackException += (s, ea) =>
                _logger?.LogError(ea.Exception, "通道回调异常");
        }
        #endregion

        #region Dispose
        /// <summary>
        /// 释放连接和通道
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                try
                {
                    _channel?.Close();
                    _channel?.Dispose();
                    _connection?.Close();
                    _connection?.Dispose();
                    _logger?.LogInformation("RabbitMQ 资源已释放");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "释放 RabbitMQ 资源失败");
                }
            }

            _channel = null;
            _connection = null;
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

    }

}

