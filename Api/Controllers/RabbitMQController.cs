using Common.RabbitMQ;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Dynamic;
using Microsoft.Extensions.Logging;
using Common.Models;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;

namespace Api.Controllers
{
    [Route("api/rabbitMQ")]
    [ApiController]
    public class RabbitMQController : ControllerBase
    {
        private readonly RabbitMQHelper _rabbitMqHelper;
        private readonly Dictionary<string, string> _consumerTags = new Dictionary<string, string>();
        private readonly ILogger<RabbitMQController> _logger;
        public RabbitMQController(RabbitMQHelper rabbitMqHelper, ILogger<RabbitMQController> logger)
        {
            _rabbitMqHelper = rabbitMqHelper;
            _logger = logger;
        }

        #region 简单模式

        /// <summary>
        /// 发布消息-Hello Word
        /// </summary>
        /// <returns></returns>
        [HttpPost("publishSimpleMsg")]
        public IActionResult SendSimpleMessage()
        {
            var message = new { UserId = 123, UserName = "John Doe" };
            _rabbitMqHelper.PublishToQueue(message, persistent: true);
            return Ok(new Result<object> { Success = true, Message = "消息发送成功" });
        }

        /// <summary>
        /// 订阅消息-Hello Word
        /// </summary>
        /// <param name="queueName"></param>
        /// <returns></returns>
        [HttpGet("subscribeSimpleMsg")]
        public IActionResult SubscribeSimpleMessage(string queueName = QueueKeys.User_Queue)
        {
            try
            {
                if (_consumerTags.ContainsKey(queueName))
                {
                    return BadRequest($"队列 {queueName} 已经在订阅中");
                }
                string consumerTag = _rabbitMqHelper.SubscribeToQueue<dynamic>(
                        queueName,
                        async msg =>
                        {
                            Console.WriteLine($"收到用户消息: {msg.UserId}:{msg.UserName}");
                            // 这里可以调用具体的处理服务
                            await Task.Delay(100); // 模拟处理
                            return true;
                        },
                        autoAck: false);


                _consumerTags[queueName] = consumerTag;

                return Ok(new Result<string> { Success = true, Message = "消息订阅成功", Data = consumerTag });
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        #endregion

        #region 工作模式
        /// <summary>
        /// 发送消息-工作模式
        /// </summary>
        /// <returns></returns>
        [HttpPost("publishWorkQueueMsg")]
        public IActionResult PublishWorkQueueMessage()
        {
            // 创建批量消息
            var workItems = new List<WorkItem>
            {
                new WorkItem { Id = 1, TaskName = "处理订单", CreatedAt = DateTime.Now },
                new WorkItem { Id = 2, TaskName = "生成报表", CreatedAt = DateTime.Now },
                new WorkItem { Id = 3, TaskName = "发送邮件", CreatedAt = DateTime.Now }
            };

            // 发送消息到工作队列
            try
            {
                _rabbitMqHelper.PublishToWorkQueueBatch(QueueKeys.Work_Queue, workItems);
                Console.WriteLine("消息发送成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"消息发送失败: {ex.Message}");
            }
            return Ok(new Result<object> { Success = true, Message = "消息发送成功" });
        }

        /// <summary>
        /// 订阅消息-工作模式
        /// </summary>
        /// <param name="queueName"></param>
        /// <returns></returns>
        [HttpGet("subscribeWorkQueueMsg")]
        public IActionResult SubscribeWorkQueueMessage(string queueName = QueueKeys.Work_Queue)
        {
            // 创建多个消费者
            var options = new WorkQueueOptions
            {
                QueueName = queueName,
                Durable = true,
                PrefetchCount = 1
            };

            var consumerTags = _rabbitMqHelper.SubscribeToWorkBatch(
                options: options,
                consumerCount: 3,
                onMessageReceived: async (WorkItem item) =>
                {
                    if (item != null)
                    {
                        Console.WriteLine($"[消费者 {Environment.CurrentManagedThreadId}] 处理: {item.TaskName}");
                        await Task.Delay(1000); // 模拟处理时间
                        Console.WriteLine($"[消费者 {Environment.CurrentManagedThreadId}] 完成: {item.TaskName}");
                    }
                    return true;
                });
            return Ok(new Result<List<string>> { Success = true, Message = "消息发送成功", Data = consumerTags });
        }
        #endregion

        #region Publish/Subscribe
        /// <summary>
        /// 发布消息-Publish
        /// </summary>
        /// <returns></returns>
        [HttpPost("publicMsg")]
        public IActionResult PublicMessage()
        {
            // 发布到特定交换机
            var orderMsg = new
            {
                Title = "订单通知",
                Content = "新订单已创建",
                SentTime = DateTime.Now
            };

            _rabbitMqHelper.Publish(
                message: orderMsg,
                exchangeName: ExchangeKeys.Orders_Exchange,
                routingKey: RoutingKeys.Orders_Created
            );

            // 发布消息到默认交换机
            var sysMsg = new
            {
                Title = "系统通知",
                Content = "服务器将于今晚进行维护",
                SentTime = DateTime.Now
            };

            _rabbitMqHelper.Publish(
                message: sysMsg,
                exchangeName: ExchangeKeys.Sys_Exchange,
                routingKey: RoutingKeys.Sys_Created
            );


            return Ok(new Result<object> { Success = true, Message = "消息发送成功" });
        }
        /// <summary>
        /// 订阅消息-Subscribe
        /// </summary>
        /// <returns></returns>
        [HttpGet("subscribeMsg")]
        public IActionResult SubscribeMessage()
        {
            // 创建多个订阅者
            var subOrder = _rabbitMqHelper.Subscribe<dynamic>(async (msg, ea) =>
            {
                Console.WriteLine($"subOrder 收到消息: {JsonConvert.SerializeObject(msg)}");
                // 模拟处理
                await Task.Delay(100);
                return true;
            }, QueueKeys.Order_Queue, ExchangeKeys.Orders_Exchange, RoutingKeys.Orders_Created);

            var subSys = _rabbitMqHelper.Subscribe<dynamic>(async (msg, ea) =>
            {
                Console.WriteLine($"subSys 收到消息: {JsonConvert.SerializeObject(msg)}");
                // 模拟处理
                await Task.Delay(200);
                return true;
            }, QueueKeys.Sys_Queue, ExchangeKeys.Sys_Exchange, RoutingKeys.Orders_Created);

            return Ok(new Result<object>
            {
                Success = true,
                Message = "消息订阅成功",
                Data = new
                {
                    orderTag = subOrder,
                    sysTag = subSys
                }
            });
        }
        #endregion

        #region 路由模式
        /// <summary>
        /// 发布消息-路由模式
        /// </summary>
        /// <returns></returns>
        [HttpPost("publicRoutingMsg")]
        public IActionResult PublicRoutingMessage()
        {
            // 发送订单创建消息
            _rabbitMqHelper.PublishMultipleModes(
                message: new { OrderId = "ORD001", Status = "created" },
                exchangeName: "orders.direct",
                exchangeType: ExchangeType.Direct,
                routingKey: "order.created"
            );

            // 发送订单取消消息
            _rabbitMqHelper.PublishMultipleModes(
                message: new { OrderId = "ORD002", Status = "cancelled" },
                exchangeName: "orders.direct",
                exchangeType: ExchangeType.Direct,
                routingKey: "order.cancelled"
            );

            // 发送支付成功消息
            _rabbitMqHelper.PublishMultipleModes(
                message: new { PaymentId = "PAY001", Status = "success" },
                exchangeName: "payments.direct",
                exchangeType: ExchangeType.Direct,
                routingKey: "payment.success"
            );

            return Ok(new Result<object> { Success = true, Message = "消息发送成功" });
        }
        /// <summary>
        /// 订阅消息-路由模式
        /// </summary>
        /// <returns></returns>
        [HttpGet("subscribeRoutingMsg")]
        public IActionResult SubscribeRoutingMessage()
        {
            // 订单创建处理器 - 只处理 order.created 路由键
            var orderCreated = _rabbitMqHelper.SubscribeMultipleModes<dynamic>(
                  handler: async (msg, ea) =>
                  {
                      Console.WriteLine($"orderCreated 收到消息: {JsonConvert.SerializeObject(msg)}");
                      // 模拟处理
                      await Task.Delay(200);
                      return true;
                  },
                  queueName: "order.created.queue",
                  exchangeName: "orders.direct",
                  exchangeType: ExchangeType.Direct,
                  routingKey: "order.created"
              );

            // 订单取消处理器 - 只处理 order.cancelled 路由键
            var orderCancelled = _rabbitMqHelper.SubscribeMultipleModes<dynamic>(
                handler: async (msg, ea) =>
                {
                    Console.WriteLine($"orderCancelled 收到消息: {JsonConvert.SerializeObject(msg)}");
                    // 模拟处理
                    await Task.Delay(200);
                    return true;
                },
                queueName: "order_cancelled_queue",
                exchangeName: "orders.direct",
                exchangeType: ExchangeType.Direct,
                routingKey: "order.cancelled"
            );

            // 支付成功处理器
            var paymentSuccess = _rabbitMqHelper.SubscribeMultipleModes<dynamic>(
                handler: async (msg, ea) =>
                {
                    Console.WriteLine($"paymentSuccess 收到消息: {JsonConvert.SerializeObject(msg)}");
                    // 模拟处理
                    await Task.Delay(200);
                    return true;
                },
                queueName: "payment.success.queue",
                exchangeName: "payments.direct",
                exchangeType: ExchangeType.Direct,
                routingKey: "payment.success"
            );

            // 支付失败处理器
            var paymentFailed = _rabbitMqHelper.SubscribeMultipleModes<dynamic>(
                handler: async (msg, ea) =>
                {
                    Console.WriteLine($"paymentFailed 收到消息: {JsonConvert.SerializeObject(msg)}");
                    // 模拟处理
                    await Task.Delay(200);
                    return true;
                },
                queueName: "payment.failed.queue",
                exchangeName: "payments.direct",
                exchangeType: ExchangeType.Direct,
                routingKey: "payment.failed"
            );

            return Ok(new Result<object>
            {
                Success = true,
                Message = "消息订阅成功",
                Data = new
                {
                    orderCreatedTag = orderCreated,
                    orderCancelledTag = orderCancelled,
                    paymentSuccessTag = paymentSuccess,
                    paymentFailedTag = paymentFailed,
                }
            });
        }
        #endregion

        #region 通配符模式
        /// <summary>
        /// 发布消息-通配符模式
        /// </summary>
        /// <returns></returns>
        [HttpPost("publicTopicMsg")]
        public IActionResult PublicTopicMessage()
        {
            // 发送各种路由键的消息
            var messages = new[]
            {
            new { RoutingKey = "order.created.electronics", Message = new  { OrderId = "ORD-ELECT-001" } },
            new { RoutingKey = "order.created.us", Message = new  { OrderId = "ORD-US-001" } },
            new { RoutingKey = "order.updated.eu", Message = new  { OrderId = "ORD-EU-001" } },
            new { RoutingKey = "order.cancelled", Message = new  { OrderId = "ORD-CANCEL-001" } },
            new { RoutingKey = "order.urgent", Message = new  { OrderId = "ORD-URGENT-001" } },
            new { RoutingKey = "order.created.clothing", Message = new  { OrderId = "ORD-CLOTH-001" } }
        };

            foreach (var msg in messages)
            {
                _rabbitMqHelper.PublishMultipleModes(
                    message: msg.Message,
                    exchangeName: "orders.topic",
                    exchangeType: ExchangeType.Topic,
                    routingKey: msg.RoutingKey
                );
            }

            return Ok(new Result<object> { Success = true, Message = "消息发送成功" });
        }
        /// <summary>
        /// 订阅消息-通配符模式
        /// </summary>
        /// <returns></returns>
        [HttpGet("subscribeTopicMsg")]
        public IActionResult SubscribeTopicMessage()
        {
            // 所有订单消息处理器 - 使用通配符 #
            var allOrders = _rabbitMqHelper.SubscribeMultipleModes<dynamic>(
                handler: async (msg, ea) =>
                {
                    Console.WriteLine($"allOrders 收到消息: {JsonConvert.SerializeObject(msg)}");
                    // 模拟处理
                    await Task.Delay(200);
                    return true;
                },
                queueName: "all_orders_queue",
                exchangeName: "orders.topic",
                exchangeType: ExchangeType.Topic,
                routingKey: "order.#" // 匹配所有 order. 开头的路由键
            );

            // 仅处理创建相关的订单消息
            var orderCreationEvents = _rabbitMqHelper.SubscribeMultipleModes<dynamic>(
                handler: async (msg, ea) =>
                {
                    Console.WriteLine($"orderCreationEvents 收到消息: {JsonConvert.SerializeObject(msg)}");
                    // 模拟处理
                    await Task.Delay(200);
                    return true;
                },
                queueName: "order.create.queue",
                exchangeName: "orders.topic",
                exchangeType: ExchangeType.Topic,
                routingKey: "order.created.*" // 匹配 order.created. 开头的路由键
            );

            // 处理紧急订单
            var urgentOrders = _rabbitMqHelper.SubscribeMultipleModes<dynamic>(
                handler: async (msg, ea) =>
                {
                    Console.WriteLine($"urgentOrders 收到消息: {JsonConvert.SerializeObject(msg)}");
                    // 模拟处理
                    await Task.Delay(200);
                    return true;
                },
                queueName: "order.urgent.queue",
                exchangeName: "orders.topic",
                exchangeType: ExchangeType.Topic,
                routingKey: "order.urgent" // 精确匹配
            );

            return Ok(new Result<object>
            {
                Success = true,
                Message = "消息订阅成功",
                Data = new
                {
                    allOrdersTag = allOrders,
                    orderCreationEventsTag = orderCreationEvents,
                    urgentOrdersTag = urgentOrders,
                }
            });
        }
        #endregion

        #region 死信队列
        /// <summary>
        /// 发布消息-死信队列
        /// </summary>
        /// <returns></returns>
        [HttpPost("publicDlxMsg")]
        public IActionResult PublicDlxMessage()
        {
            for (int i = 1; i <= 10; i++)
            {
                var order = new
                {
                    OrderId = $"ORD{DateTime.Now:yyyyMMddHHmmss}{i}",
                    Amount = 100 + i * 10,
                    Status = "new",
                    RetryCount = 0
                };

                _rabbitMqHelper.PublishWithDLX(
                    message: order,
                    exchangeName: "dlx.exchange",
                    routingKey: "dlx.process"
                );

                Console.WriteLine($"已发送订单: {order.OrderId}");
            }
            return Ok(new Result<object> { Success = true, Message = "消息发送成功" });
        }
        /// <summary>
        /// 订阅消息-死信队列
        /// </summary>
        /// <returns></returns>
        [HttpGet("subscribeDlxMsg")]
        public IActionResult SubscribeDlxMessage()
        {
            var deadLetterConfig = new DeadLetterConfig
            {
                DeadLetterExchange = "dead.letter.exchange",
                DeadLetterQueue = "dead_letter_queue",
                DeadLetterRoutingKey = "dead.letter",
                DeadLetterExchangeType = ExchangeType.Direct,
                MessageTTL = 60000, // 1分钟过期
                MaxLength = 1000     // 队列最多1000条消息
            };

            string orderConsumer = _rabbitMqHelper.SubscribeWithDLX<dynamic>(
                 handler: async (msg, ea) =>
                 {
                     Console.WriteLine($"orderConsumer 收到消息: {JsonConvert.SerializeObject(msg)}");
                     // 模拟处理
                     await Task.Delay(200);
                     return true;
                 },
                 exchangeName: "dlx.exchange",
                 exchangeType: ExchangeType.Direct,
                 queueName: "dlx_queue",
                 routingKey: "dlx.process",
                 deadLetterConfig: deadLetterConfig
             );

            Console.WriteLine("订单处理器已启动（带死信队列配置）");

            return Ok(new Result<object>
            {
                Success = true,
                Message = "消息订阅成功",
                Data = new
                {
                    consumerTag = orderConsumer
                }
            });
        }
        #endregion

        #region 延迟队列,插件版
        /// <summary>
        /// 发布消息-延迟队列
        /// </summary>
        /// <returns></returns>
        [HttpPost("publicDelayedMsg")]
        public IActionResult PublicDelayedMessage()
        {
            // 1) 声明延迟交换机 + 队列并绑定
            _rabbitMqHelper.DeclareDelayedTopology(
                delayedExchangeName: "delayed-exchange-demo",
                exchangeInnerType: "direct",    // 或 "topic"
                queueName: "delayed-queue-demo",
                routingKey: "task.delay"
            );

            // 2) 生产者发布延迟消息（延迟 10 秒）
            _rabbitMqHelper.PublishDelayed("delayed-exchange-demo", "task.delay", "hello delayed world", delayMilliseconds: 10000);

            return Ok(new Result<object> { Success = true, Message = "消息发送成功" });
        }
        /// <summary>
        /// 订阅消息-延迟队列
        /// </summary>
        /// <returns></returns>
        [HttpGet("subscribeDelayedMsg")]
        public IActionResult SubscribeDelayedMessage()
        {
            // 3) 消费者启动（手动确认）
            var consumerTag = _rabbitMqHelper.SubscribeDelayed<dynamic>("delayed-queue-demo",
                handler: async (msg, ea) =>
                {
                    Console.WriteLine($"Delaye 收到消息: {JsonConvert.SerializeObject(msg)}");
                    // 模拟处理
                    await Task.Delay(200);
                    return true;
                }, prefetch: 1);

            return Ok(new Result<object>
            {
                Success = true,
                Message = "消息订阅成功",
                Data = new
                {
                    consumerTag = consumerTag
                }
            });
        }
        #endregion
    }

}
