using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Common.RabbitMQ
{
    public class RabbitMQHelper
    {
        /// <summary>
        /// ip
        /// </summary>
        private readonly string _hostName;
        /// <summary>
        /// 端口
        /// </summary>
        private readonly int _port;
        /// <summary>
        /// 虚拟机
        /// </summary>
        private readonly string _virtualHost;
        /// <summary>
        /// 用户名
        /// </summary>
        private readonly string _userName;
        /// <summary>
        /// 密码
        /// </summary>
        private readonly string _passWord;

        public RabbitMQHelper(string hostName, int port, string virtualHost, string userName, string password)
        {
            _hostName = hostName;
            _port = port;
            _virtualHost = virtualHost;
            _userName = userName;
            _passWord = password;
        }

        /// <summary>
        /// 创建连接
        /// </summary>
        /// <returns></returns>
        private IConnection GetConnection()
        {
            /*
             * 1.创建连接工厂
             * 2.设置参数
             *    HostName:ip
             *    Port:端口
             *    VirtualHost:虚拟机 
             *    UserName:用户名
             *    Password:密码
             */
            var connectionFactory = new ConnectionFactory
            {
                HostName = _hostName,
                Port = _port,
                VirtualHost = _virtualHost,
                UserName = _userName,
                Password = _passWord
            };
            //3.创建连接
            return connectionFactory.CreateConnection();
        }

        /// <summary>
        /// 创建channel
        /// </summary>
        /// <returns></returns>
        private IModel GetCreateModel()
        {
            //4.创建channel(channel是轻量级连接，主要作用：极大减少操作系统建立TCP连接开销)
            return GetConnection().CreateModel();
        }

        /// <summary>
        /// 创建交换机
        /// </summary>
        private void CreateExchangeDeclare(ExchangeDeclareData exchangeDeclare)
        {
            /* 4.1.创建交换机
             *  exchange：交换机名称, 
             *  type：交换机类型（Direct：定向、Fanout：扇形、Headers：参数匹配、Topic：通配符） 
             *  durable：是否持久化  
             *  autoDelete：自动删除
             *  arguments：参数
             */
            GetCreateModel().ExchangeDeclare(exchange: exchangeDeclare.Exchange,
                type: exchangeDeclare.Type,
                durable: exchangeDeclare.Durable,
                autoDelete: exchangeDeclare.AutoDelete,
                arguments: exchangeDeclare.Arguments);
        }
        /// <summary>
        /// 创建队列
        /// </summary>
        /// <param name="queueDeclare"></param>
        /// <returns></returns>
        private QueueDeclareOk CreateQueueDeclare(QueueDeclareData queueDeclare)
        {
            /* 5.创建队列(Queue)
             *   queue：队列名称
             *   durable：是否持久化
             *   exclusive：是否独占
             *   autoDelete：是否自动删除
             *   arguments：参数
             */
            return GetCreateModel().QueueDeclare(queue: queueDeclare.Queue,
                                            durable: queueDeclare.Durable,
                                            exclusive: queueDeclare.Exclusive,
                                            autoDelete: queueDeclare.AutoDelete,
                                            arguments: queueDeclare.Arguments);
        }

        /// <summary>
        /// 绑定队列和交换机
        /// </summary>
        private void GetQueueBind(QueueBindData queueBind)
        {
            /* 5.1.绑定队列和交换机
             * queue：队列名称
             * exchange：交换机名称
             * routingKey：路由键，绑定规则；如果交换机类型为fanout,routingKey设置""
             */
            GetCreateModel().QueueBind(queue: queueBind.Queue, exchange: queueBind.Exchange, routingKey: queueBind.RoutingKey);
        }
        /// <summary>
        /// 发送消息
        /// </summary>
        private void BasicPublishMsg(BasicPublishData basicPublish)
        {
            /* 6.发送消息
             *   exchange：交换机名称,简单模式下使用空字符串""
             *   routingKey：路由名称
             *   mandatory：
             *   basicProperties：配置信息
             *   body： 发送的消息
             */
            GetCreateModel().BasicPublish(exchange: basicPublish.Exchange,
                                          routingKey: basicPublish.RoutingKey,
                                          basicProperties: basicPublish.BasicProperties,
                                          body: basicPublish.Body);
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        private void Close()
        {
            // 7.关闭连接
            GetCreateModel().Close();
            GetConnection().Close();
        }

        /// <summary>
        /// 接收消息
        /// </summary>
        /// <param name="basicConsume"></param>
        private void GetBasicConsume(BasicConsumeData basicConsume)
        {
            /* 6.接收消息
             *   queue：队列名称
             *   autoAck：是否自动确认
             *   consumer：回调对象
             */
            GetCreateModel().BasicConsume(queue: basicConsume.Queue, autoAck: basicConsume.AutoAck, consumer: basicConsume.Consumer);
        }


        #region Simple一对一模式
        /// <summary>
        /// 生产者,一对一模式
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="queueDeclare"></param>
        /// <param name="basicPublish"></param>
        public void SimpleSendMsg(string msg, QueueDeclareData queueDeclare, BasicPublishData basicPublish)
        {
            //启用生产者确认消息
            GetCreateModel().ConfirmSelect();
            // 5.创建队列(Queue)
            CreateQueueDeclare(queueDeclare);

            /* 线程安全有序的集合，适用于高并发
             * 作用：将序号和消息关联，轻松删除多条消息，只要给到序号
             */
            var outstandingConfirms = new ConcurrentDictionary<ulong, string>();

            //消息确认成功回调函数
            GetCreateModel().BasicAcks += (sender, ea) =>
            {
                //是否批量
                if (ea.Multiple)
                {
                    //删除确认的消息
                    var confirmed = outstandingConfirms.Where(k => k.Key <= ea.DeliveryTag);
                    foreach (var entry in confirmed)
                    {
                        outstandingConfirms.TryRemove(entry.Key, out _);
                        Console.WriteLine("发送消息成功！消息标识是：{0}", entry);
                    }
                }
                else
                {
                    outstandingConfirms.TryRemove(ea.DeliveryTag, out _);
                    Console.WriteLine("发送消息成功！消息标识是：{0}", ea.DeliveryTag);
                }

            };

            //消息确认失败回调函数
            GetCreateModel().BasicNacks += (sender, ea) =>
            {
                outstandingConfirms.TryGetValue(ea.DeliveryTag, out string body);
                Console.WriteLine("发送消息失败！消息标识是：{0}", ea.DeliveryTag);
            };

            for (int i = 0; i < 10; i++)
            {
                string message = string.Format("{0}{1}!", msg, (i + 1));
                var body = Encoding.UTF8.GetBytes(msg);

                /* 记录要发送的消息，消息的总和
                 * NextPublishSeqNo：获取序列号
                 * message：消息
                 */
                outstandingConfirms.TryAdd(GetCreateModel().NextPublishSeqNo, message);

                basicPublish.Body = body;
                // 6.发送消息
                BasicPublishMsg(basicPublish);
            }

            //7.关闭连接
            Close();
        }

        /// <summary>
        ///  消费者,一对一模式
        /// </summary>
        /// <param name="queueDeclare"></param>
        public void SimpleReceiveMsg(QueueDeclareData queueDeclare, BasicConsumeData basicConsume)
        {
            /* 5.创建队列(Queue)
             * 注：如果发送消息时已经创建队列，消费者可以省略创建队列
             */
            CreateQueueDeclare(queueDeclare);

            //回调方法，收到消息后自动执行该方法
            var consumer = new EventingBasicConsumer(GetCreateModel());
            consumer.Received += (model, ea) =>
            {

                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine("标识：{0},交换机：{1},路由key：{2},配置信息：{3},数据：{4}",
                                  ea.ConsumerTag,
                                  ea.Exchange,
                                  ea.RoutingKey,
                                  ea.BasicProperties,
                                  message);

                #region 消费者消息可靠性
                try
                {
                    Thread.Sleep(1000);
                    /* 手动签收
                     * deliveryTag：标识
                     * multiple：允许签收多条
                     */
                    GetCreateModel().BasicAck(deliveryTag: ea.DeliveryTag, multiple: true);

                }
                catch (Exception ex)
                {
                    /* 拒绝签收
                     * deliveryTag：标识
                     * multiple：允许签收多条
                     * requeue：true重回队列，会重新发送该消息给消费端
                     */
                    GetCreateModel().BasicNack(deliveryTag: ea.DeliveryTag, multiple: true, requeue: true);
                    //拒绝一次
                    //channel.BasicReject(deliveryTag: ea.DeliveryTag, true);
                }
                #endregion
            };

            #region 消息限流

            /* prefetSize：0
             * prefetCount：这个值一般在设置为非自动ack的情况下生效，每次消费多少条，一般大小为1,。
             * global： true是channel级别， false是消费者级别。
             * 注意：我们要使用非自动ack。
             */
            GetCreateModel().BasicQos(0, 1, false);
            #endregion

            // 6.接收消息
            basicConsume.Consumer = consumer;
            GetBasicConsume(basicConsume);

            //7.消费者是个监听者，不需要关闭资源
        }

        #endregion

        #region WorkQueues一对多模式
        /// <summary>
        /// 生产者,一对多模式
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="queueDeclare"></param>
        /// <param name="basicPublish"></param>
        public void WorkSendMsg(string msg, QueueDeclareData queueDeclare, BasicPublishData basicPublish)
        {
            //创建channel
            GetCreateModel();
            //创建队列
            CreateQueueDeclare(queueDeclare);

            for (int i = 0; i < 10; i++)
            {
                string message = string.Format("{0}{1}!", msg, (i + 1));
                var body = Encoding.UTF8.GetBytes(message);
                basicPublish.Body = body;
                //发送消息
                BasicPublishMsg(basicPublish);
            }
            //关闭连接
            Close();
        }

        /// <summary>
        /// 消费者1,一对多模式
        /// </summary>
        /// <param name="basicConsume"></param>
        public void WorkReceiveMsgOne(BasicConsumeData basicConsume)
        {
            //回调方法，收到消息后自动执行该方法
            var consumer = new EventingBasicConsumer(GetCreateModel());
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine("数据：{0}", message);
            };
            //接收消息
            basicConsume.AutoAck = true;
            basicConsume.Consumer = consumer;
            GetBasicConsume(basicConsume);

            //消费者是个监听者，不需要关闭资源
        }

        /// <summary>
        /// 消费者2,一对多模式
        /// </summary>
        /// <param name="basicConsume"></param>
        public void WorkReceiveMsgTwo(BasicConsumeData basicConsume)
        {
            //回调方法，收到消息后自动执行该方法
            var consumer = new EventingBasicConsumer(GetCreateModel());
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine("数据：{0}", message);
            };
            //接收消息
            basicConsume.AutoAck = true;
            basicConsume.Consumer = consumer;
            GetBasicConsume(basicConsume);

            //消费者是个监听者，不需要关闭资源
        }
        #endregion

        #region 交换机模式
        /// <summary>
        /// 交换机模式
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="exchangeDeclare"></param>
        /// <param name="queueDeclare1"></param>
        /// <param name="queueDeclare2"></param>
        /// <param name="queueBind"></param>
        /// <param name="basicPublish"></param>
        public void PublishSubscribeSendMsg(string msg, ExchangeDeclareData exchangeDeclare, QueueDeclareData queueDeclare1,
            QueueDeclareData queueDeclare2, QueueBindData queueBind, BasicPublishData basicPublish)
        {
            // 4.创建channel
            // 5.创建交换机
            CreateExchangeDeclare(exchangeDeclare);

            // 6.创建队列(Queue)
            var queueName1 = CreateQueueDeclare(queueDeclare1);
            // 7.绑定队列和交换机
            queueBind.Queue = queueName1;
            GetQueueBind(queueBind);

            var queueName2 = CreateQueueDeclare(queueDeclare2);
            queueBind.Queue = queueName2;
            GetQueueBind(queueBind);

            var body = Encoding.UTF8.GetBytes(msg);

            // 8.发送消息
            basicPublish.Body = body;
            BasicPublishMsg(basicPublish);
            //9.关闭连接
            Close();
        }

        public void PublishSubscribeReceiveMsgOne(BasicConsumeData basicConsume)
        {
            //创建channel
            //回调方法，收到消息后自动执行该方法
            var consumer = new EventingBasicConsumer(GetCreateModel());
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine("{0}消息保存到日志。。。", message);
            };
            //接收消息
            GetBasicConsume(basicConsume);
            //消费者是个监听者，不需要关闭资源
        }

        public void PublishSubscribeReceiveMsgTwo(BasicConsumeData basicConsume)
        {

            //创建channel
            //回调方法，收到消息后自动执行该方法
            var consumer = new EventingBasicConsumer(GetCreateModel());
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine("{0}消息保存到数据库。。。", message);
            };
            //接收消息
            GetBasicConsume(basicConsume);

            //消费者是个监听者，不需要关闭资源
        }
        #endregion

        #region 路由模式
        public void RoutingSendMsg()
        {
            //4.创建channel
            var channel = GetConnection().CreateModel();
            /* 5.创建交换机
             *  exchange：交换机名称, 
             *  type：交换机类型（Direct：定向、Fanout：扇形、Headers：参数匹配、Topic：通配符） 
             *  durable：是否持久化  
             *  autoDelete：自动删除
             *  arguments：参数
             */
            var exchangeName = "test_direct";
            channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Direct, durable: true, autoDelete: false, arguments: null);

            /* 6.创建队列(Queue)
              * queue：队列名称
              * durable：是否持久化
              * exclusive：是否独占
              * autoDelete：是否自动删除
              * arguments：参数
              */
            var queue1 = channel.QueueDeclare(queue: "Hello_Routing_Queue1",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

            var queue2 = channel.QueueDeclare(queue: "Hello_Routing_Queue2",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

            /* 7.绑定队列和交换机
             * queue：队列名称
             * exchange：交换机名称
             * routingKey：路由键，绑定规则；如果交换机类型为fanout,routingKey设置""
             */
            channel.QueueBind(queue: queue1,
                              exchange: exchangeName,
                              routingKey: "error");

            channel.QueueBind(queue: queue2,
                              exchange: exchangeName,
                              routingKey: "info");
            channel.QueueBind(queue: queue2,
                              exchange: exchangeName,
                              routingKey: "warning");
            channel.QueueBind(queue: queue2,
                              exchange: exchangeName,
                              routingKey: "error");


            string message = "Hello Routing!，日志级别：error！";
            var body = Encoding.UTF8.GetBytes(message);

            /* 8.发送消息
             * exchange：交换机名称
             * routingKey：路由名称
             * basicProperties：配置信息
             * body： 发送的消息
             */
            channel.BasicPublish(exchange: exchangeName,
                                 routingKey: "error",
                                 basicProperties: null,
                                 body: body);
            //8.关闭连接
            Close();
        }

        public void RoutingReceiveMsgOne()
        {
            //创建channel
            var channel = GetConnection().CreateModel();
            //回调方法，收到消息后自动执行该方法
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine("{0}消息保存到日志。。。", message);
            };
            //接收消息
            channel.BasicConsume(queue: "Hello_Routing_Queue1",
                                  autoAck: true,
                                  consumer: consumer);
            //消费者是个监听者，不需要关闭资源
        }

        public void RoutingReceiveMsgTwo()
        {
            //创建channel
            var channel = GetConnection().CreateModel();
            //回调方法，收到消息后自动执行该方法
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine("{0}消息保存到数据库。。。", message);
            };
            //接收消息
            channel.BasicConsume(queue: "Hello_Routing_Queue2",
                                  autoAck: true,
                                  consumer: consumer);
            //消费者是个监听者，不需要关闭资源
        }
        #endregion

        #region 通配符
        public void TopicsSendMsg()
        {
            //4.创建channel
            var channel = GetConnection().CreateModel();
            /* 5.创建交换机
             *  exchange：交换机名称, 
             *  type：交换机类型（Direct：定向、Fanout：扇形、Headers：参数匹配、Topic：通配符） 
             *  durable：是否持久化  
             *  autoDelete：自动删除
             *  arguments：参数
             */
            var exchangeName = "test_topic";
            channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Topic, durable: true, autoDelete: false, arguments: null);

            /* 6.创建队列(Queue)
              * queue：队列名称
              * durable：是否持久化
              * exclusive：是否独占
              * autoDelete：是否自动删除
              * arguments：参数
              */
            #region ttl
            var args = new Dictionary<string, object>();
            //队列过期时间
            args.Add("x-message-ttl", 10000);

            //消息过期时间
            IBasicProperties props = channel.CreateBasicProperties();
            props.ContentType = "text/plain";
            props.DeliveryMode = 2;
            props.Expiration = "50000";
            /* 注：两个都设置按时间短过期
             * 队列过期将会移除所有消息
             * 只有在消息顶端，才会判断其是否过期（移除）
             */
            #endregion

            var queue1 = channel.QueueDeclare(queue: "Hello_Topics_Queue1",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: args);

            var queue2 = channel.QueueDeclare(queue: "Hello_Topics_Queue2",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: args);

            /* 7.绑定队列和交换机
             * queue：队列名称
             * exchange：交换机名称
             * routingKey：路由键，绑定规则；“*”表示一个占位符，“#”表示一个或者多个
             */
            channel.QueueBind(queue: queue1,
                              exchange: exchangeName,
                              routingKey: "#");

            channel.QueueBind(queue: queue2,
                              exchange: exchangeName,
                              routingKey: "order.*");
            channel.QueueBind(queue: queue2,
                              exchange: exchangeName,
                              routingKey: "order.#");
            channel.QueueBind(queue: queue2,
                              exchange: exchangeName,
                              routingKey: "#.error");



            string message = "Hello Topics!，日志级别：info！";
            var body = Encoding.UTF8.GetBytes(message);

            /* 8.发送消息
             * exchange：交换机名称
             * routingKey：路由名称
             * basicProperties：配置信息
             * body： 发送的消息
             */
            channel.BasicPublish(exchange: exchangeName,
                                 routingKey: "order.info",
                                 basicProperties: props,
                                 body: body);
            //8.关闭连接
            Close();
        }

        public void TopicsReceiveMsgOne()
        {
            //4.创建channel
            var channel = GetConnection().CreateModel();
            //回调方法，收到消息后自动执行该方法
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine("{0}消息保存到日志。。。", message);
            };
            //接收消息
            channel.BasicConsume(queue: "Hello_Topics_Queue1",
                                  autoAck: true,
                                  consumer: consumer);
            //消费者是个监听者，不需要关闭资源
        }
        public void TopicsReceiveMsgTwo()
        {
            //创建channel
            var channel = GetConnection().CreateModel();
            //回调方法，收到消息后自动执行该方法
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine("{0}消息保存到数据库。。。", message);
            };
            //接收消息
            channel.BasicConsume(queue: "Hello_Topics_Queue2",
                                  autoAck: true,
                                  consumer: consumer);
            //消费者是个监听者，不需要关闭资源
        }
        #endregion

        #region 死信队列
        public void DlxSendMsg()
        {
            //创建channel
            var channel = GetConnection().CreateModel();
            //创建交换机
            channel.ExchangeDeclare(exchange: "test_echange", type: ExchangeType.Topic, durable: true, autoDelete: false, arguments: null);

            var args = new Dictionary<string, object>();
            //死信交换机
            args.Add("x-dead-letter-exchange", "test_echange_dlx");
            //死信路由key
            args.Add("x-dead-letter-routing-key", "test_dlx.routing_key");
            //队列过期时间
            args.Add("x-message-ttl", 10000);
            //队列长度
            args.Add("x-max-length", 10);
            var queue = channel.QueueDeclare(queue: "test_queue",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: args);
            //绑定队列和交换机
            channel.QueueBind(queue: queue,
                              exchange: "test_echange",
                              routingKey: "test.#");


            #region 死信队列
            /* 成为死信的队列条件：
             * 1.过期时间
             * 2.队列长度限制
             * 3.消息被拒收
             * 注：RabbitMQ中没有延迟队列，但是队列过期时间+死信交换机=延迟队列（ttl+dlx）
             */

            //声明死信交换机
            channel.ExchangeDeclare(exchange: "test_echange_dlx", type: ExchangeType.Topic, durable: true, autoDelete: false, arguments: null);

            //声明死信队列
            var queueDlx = channel.QueueDeclare(queue: "test_queue_dlx",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);
            //绑定死信队列和死信交换机
            channel.QueueBind(queue: queueDlx,
                              exchange: "test_echange_dlx",
                              routingKey: "test_dlx.#");
            #endregion

            //发送消息
            for (int i = 0; i < 20; i++)
            {
                string message = "Hello DLX" + (i + 1) + "!";
                var body = Encoding.UTF8.GetBytes(message);
                channel.BasicPublish(exchange: "test_echange",
                                 routingKey: "test.dlx",
                                 basicProperties: null,
                                 body: body);
            }

            //关闭连接
            Close();
        }

        public void DlxReceiveMsg()
        {
            //创建channel
            var channel = GetConnection().CreateModel();
            //回调方法，收到消息后自动执行该方法
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                try
                {

                    Thread.Sleep(1000);
                    //throw new Exception();
                    //手动签收
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: true);

                }
                catch (Exception ex)
                {
                    Console.WriteLine("消息异常，拒绝签收消息", message);
                    // 拒绝签收,消息会进入死信队列
                    channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: true, requeue: false);
                }

            };
            //接收消息
            channel.BasicConsume(queue: "test_queue",
                                  autoAck: false,
                                  consumer: consumer);
        }
        #endregion
    }
}
