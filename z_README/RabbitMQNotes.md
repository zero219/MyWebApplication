## 安装RabbitMQ

- windows下安装
- Linux下安装

## 几种工作模式

**文档地址 `https://www.rabbitmq.com/tutorials/tutorial-one-dotnet`**

- Hello Word模式
- 工作模式，一对多
- 发布订阅模式
- 路由模式
- 通配符模式
- 死信队列
- 延迟队列，需要安装插件 `rabbitmq-delayed-message-exchange`

## 生产者端消息确认机制

**代码**

```c#
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
```

**使用方式**

```c#
 // 声明交换机
 ExchangeDeclare(channel, exchangeName: exchangeName, type: exchangeType, durable: true, autoDelete: false, arguments: null);

 // 是否启用生产者端消息确认机制
 if (confirmSelect)
 {
     EnablePublisherConfirms(channel);
 }
```



## 持久化

### 1️⃣ 队列持久化（Queue Durable）

#### 定义

- **Durable = true**：队列持久化，RabbitMQ 重启后队列仍存在。
- **Durable = false**：队列非持久化，RabbitMQ 重启后队列消失。

> ✅ 注意：队列持久化 **只保证队列存在**，消息是否持久还需设置消息属性 `Persistent=true`。

------

### 2️⃣ 消息持久化（Message Persistent）

#### 定义

- 在发送消息时，需要设置 `IBasicProperties.Persistent = true`。
- 这保证消息写入磁盘，而不是仅保存在内存中。

```c#
var properties = channel.CreateBasicProperties();
properties.Persistent = true; // 消息持久化
```

> 如果队列持久化，但消息没有设置持久化，重启 RabbitMQ 后消息会丢失。

------

### 3️⃣ 交换机持久化（Exchange Durable）

#### 定义

- **Durable = true**：交换机重启后仍存在。
- **Durable = false**：交换机重启后消失。

> 注意：交换机持久化不影响消息持久化，消息需要单独设置。

## 消费端消息确认

### 自动确认（AutoAck = true）

- 消费者收到消息后，RabbitMQ **立即认为消息已处理完成**。
- 如果消费者处理失败或进程崩溃，消息会丢失。

### 手动确认（AutoAck = false）

- 消费者处理完消息后 **主动发送 ACK**。
- 如果处理失败，可以发送 **NACK** 或不发送 ACK，消息会重新入队。
- 推荐在 **生产环境** 使用。

```c#
consumer.Received += (model, ea) =>
{
    try
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        Console.WriteLine("收到消息: " + message);

        // 处理成功
        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
    }
    catch
    {
        // 处理失败，重新入队
        channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
    }
};


```
### 参数说明

| 参数        | 说明                                                       |
| ----------- | ---------------------------------------------------------- |
| deliveryTag | 消息标识，每条消息唯一                                     |
| multiple    | 是否批量确认（true 会确认小于等于 deliveryTag 的所有消息） |
| requeue     | 是否重新入队（NACK 时）                                    |

## Prefetch

### Prefetch 的概念

- **PrefetchCount**（最常用的参数）表示 **每个消费者一次性最多接收多少条未确认消息**。
- RabbitMQ 会根据 PrefetchCount 限制，避免一次性把大量消息推给消费者，防止消费者处理不过来。
- 和手动确认 (`autoAck = false`) 配合使用效果最好。

如果 `prefetchCount=1`，就是 **一次只发一条消息**，严格限制并发处理。

### 代码示例

```
// 设置 QoS (Quality of Service)
channel.BasicQos(
    prefetchSize: 0,      // 消息大小不限
    prefetchCount: 5,     // 一次最多接收 5 条未确认消息
    global: false         // false：针对当前通道的消费者，true：针对整个通道
);
```

### 参数说明

| 参数          | 作用                                                  |
| ------------- | ----------------------------------------------------- |
| prefetchCount | 每个消费者一次最多接收未确认的消息数量                |
| prefetchSize  | 消息字节大小限制（一般 0，表示不限制）                |
| global        | 是否对整个通道生效（true 全局，false 针对每个消费者） |

## 消息过期

### 队列级别 TTL

如果队列设置了 `x-message-ttl`，队列中每条消息在达到 TTL 后会被 **自动删除**，默认情况下：

- 消息被直接丢弃，不会投递给消费者。
- 如果绑定了 **死信交换机（DLX）**，消息会被转发到死信队列。

```c#
var properties = channel.CreateBasicProperties();
properties.Expiration = "60000"; // 单位毫秒，60秒后过期
channel.BasicPublish( exchange: "", routingKey: queueName, basicProperties: properties, body: body);

```



### 消息级别 TTL

如果单条消息设置了 `Expiration` 属性，效果类似：

- 消息过期后从队列中移除。
- 如果配置了 DLX，则会进入死信队列

```c#
var args = new Dictionary<string, object>
{
    { "x-message-ttl", 60000 } // 60秒
};

channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: args);
```



## 队列优先级

### 1. RabbitMQ 队列优先级的概念

- 默认情况下，RabbitMQ **队列是先进先出 (FIFO)**。
- 但是可以通过 **优先级 (Priority)** 来打破这个规则：
  - 优先级高的消息会比低优先级的消息 **先被消费者消费**。
  - 同一优先级的消息，还是保持 **FIFO 顺序**。

------

### 2. 如何启用队列优先级

要让队列支持优先级，需要在声明队列时设置：

```c#
// C# 示例 (使用 RabbitMQ.Client)
var args = new Dictionary<string, object>
{
    { "x-max-priority", 10 } // 最大优先级，取值 1~255，常用 10
};

channel.QueueDeclare(
    queue: "priority-queue",
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: args);
```

- **x-max-priority**：定义队列支持的最大优先级值（推荐 1~10）。
  - 消息发送时如果优先级大于该值，就会被限制为最大值。
  - 如果不设置这个参数，消息的优先级会被忽略，队列就是普通队列。

------

### 3. 发送消息时指定优先级

发送消息时，通过 **BasicProperties.Priority** 指定消息优先级：

```c#
var properties = channel.CreateBasicProperties();
properties.Persistent = true;      // 持久化消息
properties.Priority = 5;           // 设置优先级 (0~9)

channel.BasicPublish(
    exchange: "",
    routingKey: "priority-queue",
    basicProperties: properties,
    body: Encoding.UTF8.GetBytes("Hello Priority Message"));
```

- **Priority 范围：0~255**
- 一般建议设置 **0~10** 之间，数值越大优先级越高。

------

### 4. 注意事项 ⚠️

1. **性能开销**：启用优先级队列，RabbitMQ 内部需要维护排序，会增加内存和 CPU 消耗。
   - 优先级值越多，性能影响越大。
   - 官方建议最大优先级不要超过 **10**。
2. **并不是严格排序**：RabbitMQ 优先级并不是“全局重新排序”，而是保证在队列中，高优先级消息会尽量优先被消费。
3. **与消费者公平调度**：
   - 如果多个消费者并发消费，还会受到 **prefetch（qos 设置）** 的影响。
   - 比如 prefetch=1，可以保证消费者逐条公平获取高优先级消息。

------

### 5. 使用场景

- **订单系统**：紧急订单（高优先级）优先处理，普通订单稍后处理。
- **消息通知**：支付成功通知（高优先级）先发，营销消息（低优先级）后发。
- **任务调度**：重要任务优先执行，非关键任务延后。

## 惰性队列

### 1. 什么是惰性队列 (Lazy Queue)

普通队列中：

- 消息会尽量保存在 **内存** 里，消费者消费时直接取内存，提高吞吐量。
- 但是如果消息积压太多，内存可能被撑爆，RabbitMQ 会开始把消息刷到磁盘（性能抖动）。

惰性队列（Lazy Queue）：

- 一旦消息进入队列，RabbitMQ **立刻写入磁盘**，而不是先放内存。
- 消费者需要时，再从磁盘中读出来投递。
- 特点是 **更省内存，更适合海量消息积压场景**。

👉 适用场景：

- 大量消息堆积（比如日志收集、延时处理）。
- 消费速度可能落后于生产速度。

------

### 2. 如何声明惰性队列

有两种方式：

#### (1) 队列参数设置

声明队列时加上 `x-queue-mode=lazy`：

```c#
var args = new Dictionary<string, object>
{
    { "x-queue-mode", "lazy" }  // 开启惰性模式
};

channel.QueueDeclare(
    queue: "lazy-queue",
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: args);
```

#### (2) 全局策略设置

通过 RabbitMQ **policy** 设置：

```
rabbitmqctl set_policy LazyQueues "^lazy-.*" \
    '{"queue-mode":"lazy"}' --apply-to queues
```

上面会把所有以 `lazy-` 开头的队列设为惰性队列。

------

### 3. 惰性队列 vs 普通队列

| 特性     | 普通队列 (default)           | 惰性队列 (lazy)          |
| -------- | ---------------------------- | ------------------------ |
| 存储位置 | 优先放内存，内存不足才刷盘   | 直接刷盘                 |
| 内存占用 | 高                           | 低                       |
| 性能     | 内存快，但内存不足时性能抖动 | 磁盘 IO 稳定，但延迟略高 |
| 适用场景 | 高吞吐、低延迟               | 大量消息堆积             |

------

### 4. 注意事项

- 惰性队列适合 **积压型消息**，但如果你追求 **实时性（低延迟）**，普通队列更合适。
- 惰性队列会增加 **磁盘 IO 压力**，所以需要配置好磁盘和持久化策略。
- 可以和 **优先级队列** 结合使用，但要注意性能消耗更大。

------

✅ **总结**

- 惰性队列是 **磁盘优先** 的队列模式。
- 主要解决 **消息堆积导致内存爆掉** 的问题。
- 设置方式：队列参数 `x-queue-mode=lazy` 或 RabbitMQ 策略。

## 仲裁队列

### 什么是仲裁队列 (Quorum Queue)

仲裁队列是 RabbitMQ **高可用队列** 的推荐实现方式，用来替代旧的 **镜像队列 (Mirrored Queue)**。
 它基于 **Raft 共识算法**，保证队列在多节点集群中数据的一致性和高可用性。

------

### 仲裁队列的核心特性

1. **高可用**
   - 消息在多个节点上复制（仲裁节点）。
   - 主节点故障时，可由副本节点选举新的主节点。
2. **一致性**
   - 基于 Raft 算法：消息必须写入多数节点才能确认。
   - 保证数据不丢失，但牺牲部分性能。
3. **持久化**
   - 仲裁队列只能是 **持久化队列**。
   - 所有消息都写入磁盘，重启不会丢失。
4. **替代镜像队列**
   - 镜像队列依赖 Erlang Mnesia，复杂度高，恢复慢。
   - 仲裁队列设计更简单、恢复更快，是 RabbitMQ 官方推荐的高可用方案。

------

### 如何创建仲裁队列

可以通过 **参数声明** 或 **策略 (policy)** 两种方式。

#### 1. 参数声明

在声明队列时加上 `x-queue-type=quorum`：

```
var args = new Dictionary<string, object>
{
    { "x-queue-type", "quorum" }  // 设置为仲裁队列
};

channel.QueueDeclare(
    queue: "quorum-queue",
    durable: true,   // 必须持久化
    exclusive: false,
    autoDelete: false,
    arguments: args);
```

#### 2. 策略 (Policy)

设置匹配的队列自动变为仲裁队列：

```
rabbitmqctl set_policy quorum "quorum-.*" \
  '{"queue-type":"quorum"}' --apply-to queues
```

------

### 仲裁队列 vs 经典队列 (Classic Queue)

| 特性     | 经典队列 (Classic) | 仲裁队列 (Quorum)    |
| -------- | ------------------ | -------------------- |
| 存储     | 单节点             | 多节点复制           |
| 高可用   | 依赖镜像队列       | 内置 Raft 算法       |
| 持久化   | 可选               | 必须持久化           |
| 性能     | 高吞吐，低延迟     | 较低吞吐，延迟略高   |
| 推荐场景 | 临时性或非关键消息 | 关键业务、高可用要求 |

------

### 使用建议

- 如果你需要 **高可用 + 数据一致性** → 选 **仲裁队列**。
- 如果你只追求 **高性能** → 经典队列更合适。
- **延时敏感、积压大量消息** → 可以结合 **惰性队列 + 仲裁队列**。

## 死信队列

### 什么是死信队列 (Dead Letter Queue)

死信队列是 RabbitMQ 提供的一种 **消息处理机制**：
 当某些消息在原队列中无法被正常消费时，这些“失败消息”不会丢失，而是会被投递到一个专门的 **死信交换机 (DLX, Dead Letter Exchange)**，再由 DLX 路由到对应的 **死信队列 (DLQ)**。

这样可以：

- 保证消息不会因为消费失败而丢失。
- 方便后续进行排查、补偿或重新处理。

------

### 哪些情况会进入死信队列

消息可能因为以下原因变成“死信”：

1. **消息被拒绝 (BasicReject / BasicNack)**
    且 `requeue=false`（不重新入队）。
2. **消息过期 (TTL 超时)**
   - 单条消息设置了过期时间 `expiration`。
   - 队列设置了过期时间 `x-message-ttl`。
3. **队列达到最大长度限制**
   - 队列设置了 `x-max-length` 或 `x-max-length-bytes`，超出部分消息会被丢弃到 DLQ。

------

### 如何配置死信队列

#### 1. 声明死信交换机和死信队列

```
// 声明死信交换机
channel.ExchangeDeclare("dlx.exchange", ExchangeType.Direct);

// 声明死信队列
channel.QueueDeclare("dlx.queue", durable: true, exclusive: false, autoDelete: false);
channel.QueueBind("dlx.queue", "dlx.exchange", "dlx.routingkey");
```

#### 2. 在业务队列上绑定死信交换机

```
var args = new Dictionary<string, object>
{
    { "x-dead-letter-exchange", "dlx.exchange" },  // 绑定死信交换机
    { "x-dead-letter-routing-key", "dlx.routingkey" } // 指定路由 key
};

channel.QueueDeclare(
    queue: "business.queue",
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: args);
```

这样：

- 消息在 `business.queue` 中如果被拒绝 / 过期 / 超长，就会被投递到 `dlx.exchange`，再路由到 `dlx.queue`。

------

### 死信队列的应用场景

1. **异常消息处理**
   - 消息格式错误、反序列化失败，可以丢到 DLQ 做人工排查。
2. **延迟队列**
   - 通过设置 TTL + DLX，可以模拟延时消息（消息过期后进入 DLQ，再由消费者处理）。
3. **监控与报警**
   - 监听死信队列，一旦有大量死信出现，说明业务处理异常，需要报警。

------

### 死信队列 vs 普通队列

| 特性         | 普通队列       | 死信队列                                |
| ------------ | -------------- | --------------------------------------- |
| 消息来源     | 生产者直接投递 | 普通队列中的失败消息                    |
| 使用场景     | 正常业务处理   | 异常/过期/丢弃消息处理                  |
| 是否手动声明 | 必须           | 必须                                    |
| 是否直接消费 | 是             | 一般用于监控/补偿，不直接参与业务主流程 |

------

✅ **总结**：

- 死信队列是处理 **无法消费消息** 的兜底机制。
- 配置时需要：**DLX（交换机） + DLQ（队列） + 在业务队列上绑定 DLX**。
- 应用广泛，比如 **异常处理、延时任务、监控报警**。

## 延迟队列

### 什么是延迟队列 (Delayed Queue)

延迟队列是一种特殊的队列，消息在进入队列后不会立刻被消费，而是要等到 **指定的延迟时间** 之后才会投递给消费者。

常见应用场景：

- 订单超时未支付 → 30 分钟后自动取消。
- 用户注册后 → 5 分钟后发送欢迎消息。
- 重试机制 → 任务失败后延迟一段时间再执行。

------

### 实现延迟队列的两种方式

#### 方式一：使用 **TTL + 死信队列 (DLX)**

这是最常见的做法，不需要额外插件。
 原理：

1. 消息进入业务队列 (queueA)，并设置过期时间 (TTL)。
2. 到期未被消费的消息，变成“死信”，进入 **死信交换机 (DLX)**。
3. DLX 把消息路由到 **死信队列 (queueB)**，消费者从 queueB 消费。

**代码示例 (C#)**

```
// 死信交换机 & 队列
channel.ExchangeDeclare("dlx.exchange", ExchangeType.Direct);
channel.QueueDeclare("delay.queue", durable: true, exclusive: false, autoDelete: false);
channel.QueueBind("delay.queue", "dlx.exchange", "delay");

// 原始队列（带 TTL + DLX）
var args = new Dictionary<string, object>
{
    { "x-dead-letter-exchange", "dlx.exchange" },
    { "x-dead-letter-routing-key", "delay" },
    { "x-message-ttl", 60000 } // 60秒延迟
};

channel.QueueDeclare("origin.queue", durable: true, exclusive: false, autoDelete: false, arguments: args);

// 发送消息
var props = channel.CreateBasicProperties();
props.Expiration = "60000"; // 单条消息设置 TTL（毫秒）
channel.BasicPublish("", "origin.queue", props, Encoding.UTF8.GetBytes("订单支付超时消息"));
```

✅ 优点：不需要插件，直接用 RabbitMQ 原生功能。
 ⚠️ 缺点：

- 只能设置固定 TTL，灵活性不足。
- 如果队列很大，消息需要逐个检查 TTL，可能影响性能。

------

#### 方式二：使用 **延迟插件 (rabbitmq_delayed_message_exchange)**

RabbitMQ 官方提供插件：`rabbitmq_delayed_message_exchange`。
 安装后支持 **任意消息级别的延迟投递**。

**使用步骤：**

1. 安装插件：

   ```
   rabbitmq-plugins enable rabbitmq_delayed_message_exchange
   ```

2. 声明交换机（类型为 `x-delayed-message`）：

   ```
   var args = new Dictionary<string, object>
   {
       { "x-delayed-type", "direct" }
   };
   
   channel.ExchangeDeclare("delay.exchange", "x-delayed-message", true, false, args);
   channel.QueueDeclare("delay.queue", true, false, false, null);
   channel.QueueBind("delay.queue", "delay.exchange", "delay");
   ```

3. 发布消息时指定延迟时间：

   ```
   var props = channel.CreateBasicProperties();
   props.Headers = new Dictionary<string, object> { { "x-delay", 30000 } }; // 延迟30秒
   
   channel.BasicPublish("delay.exchange", "delay", props, Encoding.UTF8.GetBytes("30秒延迟任务"));
   ```

✅ 优点：

- 支持 **任意粒度的延迟**，灵活性高。
- 消息到期后直接投递，不需要死信中转。
   ⚠️ 缺点：需要安装插件，集群环境也要保持插件一致。

------

### 延迟队列的应用场景

- **订单超时关闭**（电商）。
- **延迟任务调度**（定时提醒、延迟执行）。
- **消息重试**（失败任务延迟再消费）。
- **限流与削峰**（消息分批延迟投递）。

## 用死信队列实现延迟队列利弊

### 使用死信队列实现延迟队列的优点

1. **无需插件，RabbitMQ 原生支持**
   - 只依赖 `TTL` 和 `DLX` 参数配置即可，兼容性强。
   - 不需要在服务器上额外安装 `rabbitmq_delayed_message_exchange` 插件。
2. **实现简单**
   - 配置一个 **业务队列 (带 TTL + DLX)** 和一个 **死信队列** 就能完成。
   - 逻辑清晰：消息过期 → 转入 DLX → 投递到 DLQ → 消费。
3. **适合批量相同延迟时间**
   - 比如：所有订单都是 **30 分钟超时关闭**，这种统一延迟场景特别适合。
4. **可靠性好**
   - 消息过期后会转入死信队列，不会丢失。
   - 可结合持久化机制保证消息安全。

------

### 使用死信队列实现延迟队列的缺点

1. **延迟粒度有限**

   - 队列级别 TTL：所有消息延迟时间一样。
   - 消息级别 TTL：不同 TTL 的消息在队列里可能 **乱序**，必须等前面的消息过期才能处理后面的消息。

   ⚠️ 举例：

   - 先发了 60 秒的消息，再发了 10 秒的消息。
   - 结果：10 秒消息要等 60 秒消息先过期才能出来，导致延迟不准确。

2. **性能问题（大规模消息积压）**

   - 如果队列里堆积了大量设置 TTL 的消息，RabbitMQ 需要不断检查是否过期，会增加 CPU/内存消耗。
   - 批量过期时，消息集中涌入 DLX，可能造成消费端压力突增。

3. **灵活性差**

   - 想要支持 **多种延迟时间**（如 10s、30s、5min），往往需要创建 **多个 TTL 队列 + 对应 DLQ**。
   - 队列数量会爆炸，运维成本增加。

4. **延迟不精确**

   - TTL + DLX 的延迟机制并不是 **实时触发**，而是等到消息出队检查 TTL。
   - 在极端情况下，可能延迟比设定值更久。

------

### 总结对比

| 方式                                             | 优点                                   | 缺点                             | 适用场景                             |
| ------------------------------------------------ | -------------------------------------- | -------------------------------- | ------------------------------------ |
| **TTL + DLX (死信队列)**                         | 原生支持、简单易用、可靠               | 延迟粒度有限、消息乱序、性能受限 | 固定延迟场景（如订单 30 分钟超时）   |
| **延迟插件 (rabbitmq_delayed_message_exchange)** | 延迟精确、支持消息级别灵活延迟、队列少 | 需要安装插件、集群要统一配置     | 任意延迟场景（如定时任务、灵活重试） |

------

✅ **结论**：

- 如果你只需要 **固定延迟（如订单超时关闭）**，用 **死信队列 + TTL** 足够。
- 如果你需要 **灵活延迟（不同消息不同延迟时间）**，最好用 **延迟插件**。



## Stream Queue

### 什么是 RabbitMQ Stream Queue

RabbitMQ Stream 是 **RabbitMQ 3.9 及以上版本**推出的 **高吞吐量消息队列**，专门针对 **大规模流式数据场景**。

- 它不是传统队列，而是 **日志型持久化队列**，消息追加到磁盘日志中。
- 适合 **海量消息、高并发写入、快速消费**。
- 结合 **流式消费模式 (Streaming Consumers)**，可以实现 **按序回溯消费**。

------

### Stream Queue 的核心特性

1. **高吞吐量**
   - 内部采用 **顺序追加 + 零拷贝** 写磁盘，性能比经典队列高。
2. **持久化 + 消息回溯**
   - 消息持久化到磁盘。
   - 消费者可以指定从某个 offset 开始消费，实现 **历史消息回溯**。
3. **顺序保证**
   - 消息在 Stream 内按写入顺序存储，保证消费顺序。
4. **支持大规模消息积压**
   - 使用 **磁盘存储 + 内存缓存**，不会像普通队列那样因消息堆积耗尽内存。
5. **多消费者模式**
   - **消费组（Consumer Group）**：类似 Kafka 消费者组，一个组内按序消费。
   - **独立消费者**：每个消费者独立读取，不影响其他消费者。

------

### Stream Queue 与普通队列对比

| 特性       | 普通队列       | Stream Queue                   |
| ---------- | -------------- | ------------------------------ |
| 吞吐量     | 中等           | 高                             |
| 消息持久化 | 可选           | 必须（日志追加）               |
| 消费顺序   | 单队列 FIFO    | 顺序保证，可回溯               |
| 消息积压   | 消息堆积占内存 | 消息写磁盘，不占内存           |
| 消费模式   | 单消费者/轮询  | 消费组 / 独立消费              |
| 场景       | 小规模业务消息 | 大规模日志、事件流、物联网数据 |

------

### 如何创建 Stream Queue

1. **声明 Stream 类型队列**

```
var args = new Dictionary<string, object>
{
    { "x-queue-type", "stream" },
    { "x-stream-max-length-bytes", 1000000000 } // 最大大小，可选
};

channel.QueueDeclare("stream.queue", durable: true, exclusive: false, autoDelete: false, arguments: args);
```

1. **发送消息**
    与普通队列类似，消息写入 Stream 后追加到日志。
2. **消费消息**

- 可以从 **最新消息** 开始消费，也可以从 **特定 offset** 回溯历史消息。

------

### Stream Queue 的应用场景

- **日志收集**：海量日志按序写入，支持回溯分析。
- **事件流处理**：IoT 传感器数据、用户行为事件流。
- **消息回溯**：可以实现类似 Kafka 的历史数据消费。
- **高吞吐消息系统**：金融交易、监控告警、指标收集。

------

✅ **总结**

- RabbitMQ Stream Queue 是 **高吞吐量、持久化、顺序保证的流式队列**。
- 与普通队列相比，更适合 **大规模消息流处理、回溯消费和高并发场景**。
- 消费模式灵活，可使用 **消费组或独立消费者**。



## 事务消息之生产者端

### 什么是事务消息

RabbitMQ 的事务消息机制是为了解决：
 **生产者把消息发到 Broker 时，如何确保消息不会丢失？**

在 RabbitMQ 中，生产者端有两种可靠投递方案：

1. **事务模式 (Tx)** → `txSelect / txCommit / txRollback`
2. **Confirm 模式 (Publisher Confirms)** → `confirmSelect / BasicAcks`

------

### 事务模式 (Tx)

事务模式类似于数据库事务，保证消息可靠性：

- **开启事务**：`txSelect`
- **提交事务**：`txCommit`
- **回滚事务**：`txRollback`

**流程：**

1. 生产者开启事务。
2. 发送消息到 RabbitMQ。
3. Broker 确认消息是否成功接收：
   - 成功 → `txCommit` 提交事务。
   - 失败 → `txRollback` 回滚事务，生产者可重试。

**C# 示例：**

```c#
// 开启事务
channel.TxSelect();

try
{
    var message = "事务消息";
    var body = Encoding.UTF8.GetBytes(message);

    channel.BasicPublish(exchange: "",
                         routingKey: "tx.queue",
                         basicProperties: null,
                         body: body);

    // 提交事务
    channel.TxCommit();
    Console.WriteLine("消息已提交");
}
catch (Exception)
{
    // 回滚事务
    channel.TxRollback();
    Console.WriteLine("消息发送失败，已回滚");
}
```

⚠️ **缺点**：

- 每条消息都要等待 RabbitMQ 返回确认，性能非常差。
- 一般生产环境很少使用。

------

### Confirm 模式 (Publisher Confirms)

事务模式的轻量替代方案，更常用。

- 生产者开启 **Confirm 模式**：`channel.ConfirmSelect()`
- 每次发送消息时，Broker 会异步返回 **ACK** 或 **NACK**。
- 可以批量确认，提高性能。

**C# 示例：**

```c#
channel.ConfirmSelect(); // 开启 Confirm 模式

var body = Encoding.UTF8.GetBytes("Confirm 消息");
channel.BasicPublish(exchange: "",
                     routingKey: "confirm.queue",
                     basicProperties: null,
                     body: body);

// 同步等待确认
if (channel.WaitForConfirms())
{
    Console.WriteLine("消息已确认");
}
else
{
    Console.WriteLine("消息投递失败");
}
```

✅ **优点**：

- 比事务模式高效。
- 支持批量确认、异步确认。
- 生产环境推荐使用。

------

### 生产者端事务消息的最佳实践

1. **使用 Confirm 模式** 替代事务模式，提高吞吐量。
2. **结合持久化**
   - 队列声明为 `durable=true`
   - 消息设置 `persistent` 属性，确保 Broker 崩溃后仍能恢复。
3. **异常处理 + 重试机制**
   - 如果 Confirm 模式返回 NACK，需要重试或记录日志。
4. **监控与报警**
   - 监控 Confirm 的失败率，异常时报警。

------

✅ **总结**

- **事务模式 (Tx)**：保证强一致性，但性能差，基本不用。
- **Confirm 模式**：可靠 + 高性能，是 RabbitMQ 生产者端事务消息的最佳实践。

## 集群搭建



## 插件

### Federation插件



### Shovel

