using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading;
using System.Collections;
using System.Threading.Channels;

namespace Common.RabbitMQ
{
    public partial class RabbitMQHelper
    {
        #region 简单模式,一对一模式(Hello World)

        /// <summary>
        /// 发送消息到指定队列（一对一模式）
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="message">消息内容</param>
        /// <param name="queueName">队列名称</param>
        /// <param name="persistent">是否持久化</param>
        /// <param name="headers">附加头信息</param>
        public void PublishToQueue<T>(T message, bool persistent = true, IDictionary<string, object> headers = null)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var channel = GetChannel();

            try
            {
                // 设置消息属性
                var properties = channel.CreateBasicProperties();
                properties.Persistent = persistent; // 消息持久化
                properties.Headers = headers; // 自定义头信息
                properties.MessageId = Guid.NewGuid().ToString();
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                // 序列化消息
                var json = JsonConvert.SerializeObject(message);
                var body = Encoding.UTF8.GetBytes(json);
                // 发布消息
                channel.BasicPublish(
                    exchange: "", // 一对一模式使用默认交换机
                    routingKey: "",
                    mandatory: true,
                    basicProperties: properties,
                    body: body);

                _logger?.LogDebug("消息发送成功, 消息ID: {MessageId}", properties.MessageId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "消息发送失败!");
                throw;
            }
        }

        /// <summary>
        /// 订阅指定队列的消息（一对一模式）
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="queueName">队列名称</param>
        /// <param name="onMessageReceived">消息处理委托</param>
        /// <param name="autoAck">是否自动确认</param>
        /// <returns>消费者标签</returns>
        public string SubscribeToQueue<T>(string queueName, Func<T, Task<bool>> onMessageReceived, bool autoAck = false)
        {
            if (string.IsNullOrEmpty(queueName))
                throw new ArgumentNullException(nameof(queueName));

            if (onMessageReceived == null)
                throw new ArgumentNullException(nameof(onMessageReceived));

            var channel = GetChannel();

            try
            {
                // 声明队列（确保队列存在）
                QueueDeclare(channel, queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

                // 创建消费者
                var consumer = new AsyncEventingBasicConsumer(channel);

                consumer.Received += async (model, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var json = Encoding.UTF8.GetString(body);
                        var message = JsonConvert.DeserializeObject<T>(json);

                        _logger?.LogDebug("收到来自队列 {QueueName} 的消息: {MessageId}", queueName, ea.BasicProperties.MessageId);

                        // 处理消息
                        bool success = await onMessageReceived(message);

                        if (!autoAck)
                        {
                            if (success)
                            {
                                channel.BasicAck(ea.DeliveryTag, false);
                                _logger?.LogDebug("消息确认成功: {MessageId}", ea.BasicProperties.MessageId);
                            }
                            else
                            {
                                channel.BasicNack(ea.DeliveryTag, false, true); // 重新入队
                                _logger?.LogWarning("消息处理失败，已重新入队: {MessageId}", ea.BasicProperties.MessageId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "处理消息时发生异常");

                        if (!autoAck)
                        {
                            try
                            {
                                channel.BasicNack(ea.DeliveryTag, false, false); // 拒绝消息，不重新入队
                            }
                            catch (Exception ackEx)
                            {
                                _logger?.LogError(ackEx, "消息确认操作失败");
                            }
                        }
                    }
                };

                // 开始消费
                string consumerTag = channel.BasicConsume(queue: queueName, autoAck: autoAck, consumer: consumer);

                _logger?.LogInformation("开始监听队列: {QueueName}, 消费者标签: {ConsumerTag}", queueName, consumerTag);

                return consumerTag;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "订阅队列失败: {QueueName}", queueName);
                throw;
            }
        }
        #endregion

        #region 工作模式,一对多(Work Queues)
        /// <summary>
        /// 发送消息到工作队列
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queueName"></param>
        /// <param name="message"></param>
        /// <param name="persistent"></param>
        /// <param name="headers"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void PublishToWorkQueue<T>(T message, string queueName, bool persistent = true, IDictionary<string, object> headers = null)
        {
            if (string.IsNullOrEmpty(queueName))
                throw new ArgumentNullException(nameof(queueName));

            var channel = GetChannel();

            try
            {
                // 序列化消息
                var json = JsonConvert.SerializeObject(message);
                var body = Encoding.UTF8.GetBytes(json);

                // 设置消息属性
                var properties = channel.CreateBasicProperties();
                properties.Persistent = persistent;
                properties.Headers = headers;
                properties.MessageId = Guid.NewGuid().ToString();
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                // 发布到默认交换机，使用队列名作为路由键
                channel.BasicPublish(
                    exchange: "",
                    routingKey: "",
                    mandatory: true,
                    basicProperties: properties,
                    body: body);

                _logger?.LogDebug("工作队列消息发送成功: {QueueName}, 消息ID: {MessageId}", queueName, properties.MessageId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "工作队列消息发送失败: {QueueName}", queueName);
                throw;
            }
        }


        /// <summary>
        /// 批量发送消息到工作队列
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queueName"></param>
        /// <param name="messages"></param>
        /// <param name="persistent"></param>
        public void PublishToWorkQueueBatch<T>(string queueName, IEnumerable<T> messages, bool persistent = true)
        {
            var channel = GetChannel();

            try
            {
                var messageList = messages.ToList();
                foreach (var message in messageList)
                {
                    var json = JsonConvert.SerializeObject(message);
                    var body = Encoding.UTF8.GetBytes(json);

                    var properties = channel.CreateBasicProperties();
                    properties.Persistent = persistent;
                    properties.MessageId = Guid.NewGuid().ToString();
                    properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                    channel.BasicPublish(
                        exchange: "",
                        routingKey: queueName,
                        mandatory: true,
                        basicProperties: properties,
                        body: body);
                }

                _logger?.LogDebug("批量发送工作队列消息成功: {QueueName}, 消息数量: {Count}", queueName, messages.Count());
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "批量发送工作队列消息失败: {QueueName}", queueName);
                throw;
            }
        }

        /// <summary>
        /// 订阅工作队列（多个消费者竞争消费）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="options"></param>
        /// <param name="onMessageReceived"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public string SubscribeToWorkQueue<T>(WorkQueueOptions options,
            Func<T, Task<bool>> onMessageReceived, CancellationToken cancellationToken = default)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (onMessageReceived == null)
            {
                throw new ArgumentNullException(nameof(onMessageReceived));
            }

            var channel = GetChannel();

            try
            {
                // 声明工作队列
                QueueDeclare(channel, options.QueueName, options.Durable, options.Exclusive, options.AutoDelete, options.Arguments);

                // 设置QoS，控制每个消费者的预取数量
                channel.BasicQos(prefetchSize: 0, prefetchCount: options.PrefetchCount, global: false);

                // 创建消费者
                var consumer = new AsyncEventingBasicConsumer(channel);
                // 回调方法，收到消息后自动执行该方法
                consumer.Received += async (model, ea) =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        channel.BasicNack(ea.DeliveryTag, false, true); // 重新入队
                        return;
                    }

                    try
                    {
                        var body = ea.Body.ToArray();
                        var json = Encoding.UTF8.GetString(body);
                        var message = JsonConvert.DeserializeObject<T>(json);

                        _logger?.LogDebug("工作队列收到消息: {QueueName}, 消息ID: {MessageId}",
                            options.QueueName, ea.BasicProperties.MessageId);

                        // 处理消息
                        bool success = await onMessageReceived(message);

                        if (success)
                        {
                            channel.BasicAck(ea.DeliveryTag, false);
                            _logger?.LogDebug("工作队列消息处理成功: {MessageId}",
                                ea.BasicProperties.MessageId);
                        }
                        else
                        {
                            channel.BasicNack(ea.DeliveryTag, false, false); // 不重新入队
                            _logger?.LogWarning("工作队列消息处理失败: {MessageId}",
                                ea.BasicProperties.MessageId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "工作队列消息处理异常");
                        channel.BasicNack(ea.DeliveryTag, false, true); // 重新入队重试
                    }
                };

                // 开始消费
                string consumerTag = channel.BasicConsume(
                    queue: options.QueueName,
                    autoAck: false, // 手动确认
                    consumer: consumer);

                _logger?.LogInformation("工作队列消费者启动: {QueueName}, 消费者标签: {ConsumerTag}, Prefetch: {PrefetchCount}",
                    options.QueueName, consumerTag, options.PrefetchCount);

                return consumerTag;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "工作队列订阅失败: {QueueName}", options.QueueName);
                throw;
            }
        }

        /// <summary>
        /// 创建多个工作消费者（一对多模式）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="options"></param>
        /// <param name="consumerCount"></param>
        /// <param name="onMessageReceived"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public List<string> SubscribeToWorkBatch<T>(WorkQueueOptions options,
            int consumerCount, Func<T, Task<bool>> onMessageReceived,
            CancellationToken cancellationToken = default)
        {
            if (consumerCount <= 0)
                throw new ArgumentException("消费者数量必须大于0", nameof(consumerCount));

            var consumerTags = new List<string>();

            for (int i = 0; i < consumerCount; i++)
            {
                var consumerTag = SubscribeToWorkQueue(options, onMessageReceived, cancellationToken);
                consumerTags.Add(consumerTag);

                _logger?.LogInformation("创建工作消费者 {Index}/{Total}: {ConsumerTag}", i + 1, consumerCount, consumerTag);
            }

            return consumerTags;
        }


        #endregion

        #region 发布订阅模式(Publish/Subscribe)
        /// <summary>
        /// 发布消息到交换机（发布订阅模式）
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="message">消息内容</param
        /// <param name="queueName">队列名称（如果为空则创建匿名队列）</param>
        /// <param name="exchangeName">交换机名称</param>
        /// <param name="routingKey">路由键（在fanout模式下通常为空）</param>
        /// <param name="confirmSelect">是否启用生产者端消息确认机制</param>
        /// <param name="persistent">是否持久化</param>
        public void Publish<T>(T message, string exchangeName = "pubsub.exchange", string routingKey = "pubsub.Routing",
            bool confirmSelect = false, bool persistent = true)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var channel = GetChannel();

            try
            {
                // 声明交换机
                ExchangeDeclare(channel, exchangeName: exchangeName, type: ExchangeType.Fanout, durable: true, autoDelete: false, arguments: null);

                // 是否启用生产者端消息确认机制
                if (confirmSelect)
                {
                    EnablePublisherConfirms(channel);
                }

                var properties = channel.CreateBasicProperties();
                properties.Persistent = persistent;
                properties.MessageId = Guid.NewGuid().ToString();
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                var messageBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));

                channel.BasicPublish(
                    exchange: exchangeName,
                    routingKey: routingKey,
                    basicProperties: properties,
                    body: messageBytes);

                _logger?.LogInformation("消息发布成功: Exchange={Exchange}, RoutingKey={RoutingKey}, MessageId={MessageId}",
                    exchangeName, routingKey, properties.MessageId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "消息发布失败: Exchange={Exchange}", exchangeName);
                throw;
            }
        }

        /// <summary>
        /// 订阅消息（创建匿名队列并绑定到交换机）
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="handler">消息处理委托</param
        /// <param name="queueName">队列名称（如果为空则创建匿名队列）</param>
        /// <param name="exchangeName">交换机名称</param>
        /// <returns>订阅标识符，可用于取消订阅</returns>
        public string Subscribe<T>(
            Func<T, BasicDeliverEventArgs, Task<bool>> handler,
            string queueName = "pubsub_queue",
            string exchangeName = "pubsub.exchange",
            string routingKey = "pubsub.Routing")
        {
            var channel = GetChannel();

            try
            {
                // 声明交换机
                ExchangeDeclare(channel, exchangeName: exchangeName, type: ExchangeType.Fanout, durable: true, autoDelete: false, arguments: null);

                // 创建队列
                QueueDeclare(channel, queueName: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

                // 绑定队列到交换机
                QueueBind(channel, queueName, exchangeName, routingKey);

                // 创建消费者
                var consumer = new AsyncEventingBasicConsumer(channel);

                consumer.Received += async (model, ea) =>
                {
                    try
                    {
                        var messageString = Encoding.UTF8.GetString(ea.Body.ToArray());
                        var message = JsonConvert.DeserializeObject<T>(messageString);
                        if (message != null)
                        {
                            var success = await handler(message, ea);
                            if (success)
                            {
                                channel.BasicAck(ea.DeliveryTag, false);
                                _logger?.LogDebug("消息处理成功: DeliveryTag={DeliveryTag}", ea.DeliveryTag);
                            }
                            else
                            {
                                channel.BasicNack(ea.DeliveryTag, false, true);
                                _logger?.LogWarning("消息处理失败，已重新入队: DeliveryTag={DeliveryTag}", ea.DeliveryTag);
                            }
                        }
                        else
                        {
                            channel.BasicNack(ea.DeliveryTag, false, false);
                            _logger?.LogError("消息反序列化失败: DeliveryTag={DeliveryTag}", ea.DeliveryTag);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "消息处理异常: DeliveryTag={DeliveryTag}", ea.DeliveryTag);
                        channel.BasicNack(ea.DeliveryTag, false, true);
                    }
                };

                // 开始消费
                string consumerTag = channel.BasicConsume(
                    queue: queueName,
                    autoAck: false,
                    consumer: consumer);

                _logger?.LogInformation("订阅创建成功: Exchange={Exchange}, Queue={Queue}, ConsumerTag={ConsumerTag}",
                    exchangeName, queueName, consumerTag);

                return consumerTag;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "订阅创建失败: Exchange={Exchange}", exchangeName);
                throw;
            }
        }

        #endregion

        #region 发布订阅模式|路由模式|通配符模式
        /// <summary>
        /// 发布消息（支持多路由模式）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <param name="exchangeName"></param>
        /// <param name="exchangeType"></param>
        /// <param name="routingKey"></param>
        /// <param name="confirmSelect">是否启用生产者端消息确认机制</param>
        /// <param name="persistent"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void PublishMultipleModes<T>(T message,
            string exchangeName = "my.exchange",
            string exchangeType = ExchangeType.Direct, // 新增参数
            string routingKey = "",
            bool confirmSelect = false,
            bool persistent = true,
            IDictionary<string, object> headers = null)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (string.IsNullOrWhiteSpace(routingKey))
            {
                throw new ArgumentNullException(nameof(routingKey));
            }

            var channel = GetChannel();

            try
            {
                // 声明交换机
                ExchangeDeclare(channel, exchangeName: exchangeName, type: exchangeType, durable: true, autoDelete: false, arguments: null);

                // 是否启用生产者端消息确认机制
                if (confirmSelect)
                {
                    EnablePublisherConfirms(channel);
                }

                var properties = channel.CreateBasicProperties();
                properties.Persistent = persistent;
                properties.MessageId = Guid.NewGuid().ToString();
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                // 设置消息头
                if (headers != null)
                {
                    properties.Headers = headers;
                }
                var messageBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));

                // 发布消息
                channel.BasicPublish(exchange: exchangeName, routingKey: routingKey, basicProperties: properties, body: messageBytes);

                _logger?.LogInformation("消息发布成功: Exchange={Exchange}, RoutingKey={RoutingKey}, Type={Type}",
                    exchangeName, routingKey, exchangeType);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "消息发布失败");
                throw;
            }
        }

        /// <summary>
        /// 订阅消息（支持多路由模式）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        /// <param name="queueName"></param>
        /// <param name="exchangeName"></param>
        /// <param name="exchangeType"></param>
        /// <param name="routingKey"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public string SubscribeMultipleModes<T>(
            Func<T, BasicDeliverEventArgs, Task<bool>> handler,
            string queueName = "my_queue",
            string exchangeName = "my.exchange",
            string exchangeType = ExchangeType.Direct, // 新增参数
            string routingKey = "",
            Dictionary<string, object> arguments = null)
        {
            var channel = GetChannel();

            try
            {
                // 声明交换机
                ExchangeDeclare(channel, exchangeName: exchangeName, type: exchangeType, durable: true, autoDelete: false, arguments: null);

                // 创建队列
                QueueDeclare(channel, queueName: queueName, durable: true, exclusive: false, autoDelete: false, arguments: arguments);

                // 绑定队列到交换机
                QueueBind(channel, queueName, exchangeName, routingKey);

                // 创建消费者（代码同上）
                var consumer = new AsyncEventingBasicConsumer(channel);

                consumer.Received += async (model, ea) =>
                {
                    try
                    {
                        var messageString = Encoding.UTF8.GetString(ea.Body.ToArray());
                        var message = JsonConvert.DeserializeObject<T>(messageString);
                        if (message != null)
                        {
                            var success = await handler(message, ea);
                            if (success)
                            {
                                // 手动确认
                                channel.BasicAck(ea.DeliveryTag, false);
                                _logger?.LogDebug("消息处理成功: DeliveryTag={DeliveryTag}", ea.DeliveryTag);
                            }
                            else
                            {
                                // 消费失败，消息重新入队
                                channel.BasicNack(ea.DeliveryTag, false, true);
                                _logger?.LogWarning("消息处理失败，已重新入队: DeliveryTag={DeliveryTag}", ea.DeliveryTag);
                            }
                        }
                        else
                        {
                            channel.BasicNack(ea.DeliveryTag, false, false);
                            _logger?.LogError("消息反序列化失败: DeliveryTag={DeliveryTag}", ea.DeliveryTag);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "消息处理异常: DeliveryTag={DeliveryTag}", ea.DeliveryTag);
                        channel.BasicNack(ea.DeliveryTag, false, true);
                    }
                };

                string consumerTag = channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

                return consumerTag;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "订阅创建失败");
                throw;
            }
        }
        #endregion

        #region 死信队列

        /// <summary>
        /// 发布消息（支持死信队列配置）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <param name="exchangeName"></param>
        /// <param name="routingKey"></param>
        /// <param name="persistent"></param>
        /// <param name="headers"></param>
        public void PublishWithDLX<T>(T message, string exchangeName = "dlx.exchange", string routingKey = "", bool persistent = true,
            IDictionary<string, object> headers = null)
        {
            var channel = GetChannel();

            try
            {
                var properties = channel.CreateBasicProperties();
                properties.Persistent = persistent;
                properties.MessageId = Guid.NewGuid().ToString();
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                // 设置消息头
                if (headers != null)
                {
                    properties.Headers = headers;
                }

                var messageBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));

                channel.BasicPublish(
                    exchange: exchangeName,
                    routingKey: routingKey,
                    basicProperties: properties,
                    body: messageBytes);

                _logger?.LogInformation("消息发布成功: {Exchange}, {RoutingKey}", exchangeName, routingKey);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "消息发布失败");
                throw;
            }
        }

        /// <summary>
        /// 订阅消息（支持死信队列配置）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        /// <param name="exchangeName"></param>
        /// <param name="exchangeType"></param>
        /// <param name="queueName"></param>
        /// <param name="routingKey"></param>
        /// <param name="deadLetterConfig"></param>
        /// <returns></returns>
        public string SubscribeWithDLX<T>(Func<T, BasicDeliverEventArgs, Task<bool>> handler,
            string exchangeName = "main.exchange", string exchangeType = ExchangeType.Direct, string queueName = "main.queue",
            string routingKey = "", DeadLetterConfig deadLetterConfig = null) // 新增死信配置参数
        {
            var channel = GetChannel();

            try
            {
                // 声明主交换机
                ExchangeDeclare(channel, exchangeName: exchangeName, type: exchangeType, durable: true, autoDelete: false);

                // 构建队列参数
                var queueArguments = BuildQueueArguments(deadLetterConfig);

                // 声明主队列
                QueueDeclare(channel, queueName: queueName, durable: true, exclusive: false, autoDelete: false, arguments: queueArguments);

                // 绑定主队列到主交换机
                QueueBind(channel, queueName, exchangeName, routingKey);

                // 如果配置了死信队列，声明死信交换机和队列
                if (deadLetterConfig != null && !string.IsNullOrEmpty(deadLetterConfig.DeadLetterExchange))
                {
                    // 声明死信交换机
                    ExchangeDeclare(channel, exchangeName: deadLetterConfig.DeadLetterExchange, type: deadLetterConfig.DeadLetterExchangeType,
                        durable: true, autoDelete: false);

                    // 声明死信队列
                    QueueDeclare(channel, queueName: deadLetterConfig.DeadLetterQueue, durable: true, exclusive: false, autoDelete: false,
                        arguments: null);

                    // 绑定死信队列到死信交换机
                    QueueBind(channel, deadLetterConfig.DeadLetterQueue, deadLetterConfig.DeadLetterExchange,
                        deadLetterConfig.DeadLetterRoutingKey);
                }

                // 创建消费者（处理逻辑保持不变）
                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.Received += async (model, ea) =>
                {
                    try
                    {
                        var messageString = Encoding.UTF8.GetString(ea.Body.ToArray());
                        var message = JsonConvert.DeserializeObject<T>(messageString);
                        if (message != null)
                        {
                            var success = await handler(message, ea);
                            if (success)
                            {
                                channel.BasicAck(ea.DeliveryTag, false);
                            }
                            else
                            {
                                // 处理失败，拒绝消息（会进入死信队列）
                                channel.BasicNack(ea.DeliveryTag, false, false);
                            }
                        }
                        else
                        {
                            channel.BasicNack(ea.DeliveryTag, false, false);
                        }
                    }
                    catch (Exception ex)
                    {
                        channel.BasicNack(ea.DeliveryTag, false, false);
                    }
                };

                return channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "订阅创建失败");
                throw;
            }
        }

        /// <summary> 
        /// 构建队列参数（包含死信队列配置） 
        /// </summary> /// <param name="deadLetterConfig">死信队列配置</param> 
        /// <param name="additionalArguments">额外参数</param> 
        /// <returns>队列参数字典</returns> 
        public Dictionary<string, object> BuildQueueArguments(DeadLetterConfig deadLetterConfig = null, 
            Dictionary<string, object> additionalArguments = null)
        {
            var queueArguments = new Dictionary<string, object>();
            // 添加额外参数（如果有）
            if (additionalArguments != null)
            {
                foreach (var arg in additionalArguments)
                {
                    queueArguments[arg.Key] = arg.Value;
                }
            }
            // 配置死信队列参数
            if (deadLetterConfig != null)
            {
                // 设置死信交换机
                if (!string.IsNullOrEmpty(deadLetterConfig.DeadLetterExchange))
                { queueArguments["x-dead-letter-exchange"] = deadLetterConfig.DeadLetterExchange; }
                // 设置死信路由键
                if (!string.IsNullOrEmpty(deadLetterConfig.DeadLetterRoutingKey))
                {
                    queueArguments["x-dead-letter-routing-key"] = deadLetterConfig.DeadLetterRoutingKey;
                }
                // 设置消息TTL（毫秒）
                if (deadLetterConfig.MessageTTL.HasValue)
                {
                    queueArguments["x-message-ttl"] = deadLetterConfig.MessageTTL.Value;
                }
                // 设置队列最大长度
                if (deadLetterConfig.MaxLength.HasValue)
                {
                    queueArguments["x-max-length"] = deadLetterConfig.MaxLength.Value;
                }
                // 设置最大优先级
                if (deadLetterConfig.MaxPriority.HasValue)
                {
                    queueArguments["x-max-priority"] = deadLetterConfig.MaxPriority.Value;
                }
                // 设置溢出行为
                if (!string.IsNullOrEmpty(deadLetterConfig.OverflowBehavior))
                {
                    queueArguments["x-overflow"] = deadLetterConfig.OverflowBehavior;
                }
            }
            return queueArguments;
        }

        #endregion

        #region 延迟队列
        /// <summary>
        /// 声明一个基于插件的延迟交换机（x-delayed-message）和一个队列并绑定
        /// </summary>
        /// <param name="delayedExchangeName">延迟交换机名</param>
        /// <param name="exchangeInnerType">内部路由类型：direct/topic/fanout</param>
        /// <param name="queueName">队列名</param>
        /// <param name="routingKey">路由键</param>
        public void DeclareDelayedTopology(string delayedExchangeName, string exchangeInnerType, string queueName, string routingKey)
        {
            var channel = GetChannel();

            // 声明延迟交换机（需要 rabbitmq_delayed_message_exchange 插件）
            var args = new Dictionary<string, object>
            {
                { "x-delayed-type", exchangeInnerType }
            };

            // exchange type 必须写 "x-delayed-message"
            channel.ExchangeDeclare(exchange: delayedExchangeName, type: "x-delayed-message", durable: true, autoDelete: false, arguments: args);
            _logger?.LogInformation("Declared delayed exchange: {0} (innerType={1})", delayedExchangeName, exchangeInnerType);

            // 声明队列（持久化）
            channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            _logger?.LogInformation("Declared queue: {0}", queueName);

            // 绑定队列到延迟交换机（routingKey 根据 innerType 决定）
            channel.QueueBind(queue: queueName, exchange: delayedExchangeName, routingKey: routingKey);
            _logger?.LogInformation("Queue {0} bound to {1} with key {2}", queueName, delayedExchangeName, routingKey);
        }

        /// <summary>
        /// 生产者：发布延迟消息（单位：毫秒）
        /// </summary>
        /// <param name="exchange">延迟交换机名（x-delayed-message）</param>
        /// <param name="routingKey">路由键</param>
        /// <param name="body">消息文本</param>
        /// <param name="delayMilliseconds">延迟毫秒</param>
        /// <param name="persistent">是否持久化消息</param>
        public void PublishDelayed<T>(string exchange, string routingKey, T message, int delayMilliseconds, bool persistent = true)
        {
            var channel = GetChannel();

            var properties = channel.CreateBasicProperties();
            properties.Persistent = persistent;

            // x-delay 必须放在 headers 中，值为毫秒（整数）
            properties.Headers = properties.Headers ?? new Dictionary<string, object>();
            properties.Headers["x-delay"] = delayMilliseconds;

            var messageBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));

            channel.BasicPublish(exchange: exchange, routingKey: routingKey, basicProperties: properties, body: messageBytes);

            _logger?.LogInformation("Published delayed message to {0} with delay {1}ms: {2}", exchange, delayMilliseconds, message);
        }

        /// <summary>
        /// 启动消费者（手动确认），自动处理 BasicAck / BasicNack
        /// </summary>
        /// <param name="queueName">队列名</param>
        /// <param name="onMessage">成功处理后的回调 (string msg)</param>
        /// <param name="prefetch">预取数量（推荐 1）</param>
        public string SubscribeDelayed<T>(string queueName, Func<T, BasicDeliverEventArgs, Task<bool>> handler, ushort prefetch = 1)
        {
            var channel = GetChannel();

            // 设置 prefetch，保证公平分发
            channel.BasicQos(0, prefetch, false);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var msg = Encoding.UTF8.GetString(body);

                try
                {
                    _logger?.LogInformation("Received message: {0}", msg);

                    var messageString = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var message = JsonConvert.DeserializeObject<T>(messageString);
                    if (message != null)
                    {
                        var success = await handler(message, ea);
                        if (success)
                        {
                            // 处理成功，手动确认
                            channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        }
                        else
                        {
                            // 消费失败，消息重新入队
                            channel.BasicNack(ea.DeliveryTag, false, true);
                            _logger?.LogWarning("消息处理失败，已重新入队: DeliveryTag={DeliveryTag}", ea.DeliveryTag);
                        }
                    }
                    else
                    {
                        channel.BasicNack(ea.DeliveryTag, false, false);
                        _logger?.LogError("消息反序列化失败: DeliveryTag={DeliveryTag}", ea.DeliveryTag);
                    }

                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Message processing failed, sending Nack and requeue");

                    // 处理失败：这里选择重新入队（requeue: true）
                    channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            string consumerTag = channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
            _logger?.LogInformation("Consumer started on queue: {0}", queueName);
            return consumerTag;
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 取消订阅
        /// </summary>
        /// <param name="consumerTag">消费者标签</param>
        public void Unsubscribe(string consumerTag)
        {
            if (string.IsNullOrEmpty(consumerTag))
                throw new ArgumentNullException(nameof(consumerTag));

            var channel = GetChannel();

            try
            {
                channel.BasicCancel(consumerTag);
                _logger?.LogInformation("取消订阅成功: {ConsumerTag}", consumerTag);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "取消订阅失败: {ConsumerTag}", consumerTag);
                throw;
            }
        }


        /// <summary>
        /// 获取消息的重试次数
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        private int GetRetryCount(IBasicProperties properties)
        {
            if (properties.Headers != null && properties.Headers.ContainsKey("x-retry-count"))
            {
                if (properties.Headers["x-retry-count"] is int retryCount)
                {
                    return retryCount;
                }
            }
            return 0;
        }

        /// <summary>
        /// 增加消息的重试次数
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        private IBasicProperties IncrementRetryCount(IBasicProperties properties, IModel channel)
        {
            var newProperties = channel.CreateBasicProperties();
            properties.Headers = properties.Headers ?? new Dictionary<string, object>();

            var retryCount = GetRetryCount(properties);
            properties.Headers["x-retry-count"] = retryCount + 1;

            // 复制其他属性
            newProperties.Persistent = properties.Persistent;
            newProperties.MessageId = properties.MessageId;
            newProperties.Timestamp = properties.Timestamp;
            newProperties.Headers = properties.Headers;

            return newProperties;
        }

        /// <summary>
        /// 获取队列消息数量
        /// </summary>
        /// <param name="queueName"></param>
        /// <returns></returns>
        public uint GetQueueMessageCount(string queueName)
        {
            var channel = GetChannel();

            try
            {
                var declareResult = channel.QueueDeclarePassive(queueName);
                return declareResult.MessageCount;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取队列消息数量失败: {QueueName}", queueName);
                throw;
            }
        }


        /// <summary> 
        /// 删除队列 
        /// </summary> 
        /// <param name="queueName"></param> 
        /// <param name="ifUnused">如果队列没有被使用（没有消费者），才允许删除</param> 
        /// <param name="ifEmpty">如果队列是空的（没有消息），才允许删除</param> 
        public void QueueDelete(IModel channel, string queueName, bool ifUnused = false, bool ifEmpty = false)
        {
            try
            {
                channel.QueueDelete(queueName, ifUnused, ifEmpty);
                _logger?.LogInformation("队列删除成功: {QueueName}", queueName);
            }
            catch (Exception ex) { _logger?.LogError(ex, "队列删除失败: {QueueName}", queueName); throw; }
        }

        /// <summary>
        /// 清空队列中的消息
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="queueName"></param>
        /// <returns></returns>
        public uint QueuePurge(IModel channel, string queueName)
        {
            try
            {
                var result = channel.QueuePurge(queueName);
                _logger?.LogInformation("队列清空成功: {QueueName}, 删除消息数: {Count}", queueName, result);
                return result;
            }
            catch (Exception ex) { _logger?.LogError(ex, "队列清空失败: {QueueName}", queueName); throw; }
        }
        #endregion
    }
}
