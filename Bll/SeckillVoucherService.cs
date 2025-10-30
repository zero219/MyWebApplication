using Common.Redis;
using Entity.Models.Seckill;
using IBll;
using IDal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Entity.Dtos.SeckillVoucherDto;

namespace Bll
{
    public class SeckillVoucherService : BaseService<SeckillVoucher>, ISeckillVoucherService
    {

        private readonly ISeckillVoucherManager _seckillVoucherManager;

        private readonly IVoucherOrderManager _voucherOrderManager;

        private readonly IRedisCacheManager _redisCacheManager;

        public SeckillVoucherService(ISeckillVoucherManager seckillVoucherManager,
            IVoucherOrderManager voucherOrderManager,
            IRedisCacheManager redisCacheManager) : base(seckillVoucherManager)
        {
            _seckillVoucherManager = seckillVoucherManager;
            _voucherOrderManager = voucherOrderManager;
            _redisCacheManager = redisCacheManager;
        }

        #region MyRegion

        public async Task TaskWorkAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine($"{DateTime.Now}");
            var stream = "stream_orders";
            var streamConsumerGroup = "OrdersGroup";
            var streamConsumerName = "OrderConsumer";
            var lastSeenMessageId = 1;

            var exists = _redisCacheManager.Exist(stream);
            if (!exists)
            {
                // 组不存在创建
                await _redisCacheManager.ExecuteAsync("XGROUP", new object[] { "CREATE", "MyRedis:stream_orders", streamConsumerGroup, "0-0", "MKSTREAM" });
            }
            // redis队列异步创建订单
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var streamEntries = _redisCacheManager.StreamReadGroup(stream, streamConsumerGroup, streamConsumerName, ">", lastSeenMessageId);
                    if (streamEntries?.Length == 0)
                    {
                        await Task.Delay(500);
                        // 如果为null，说明没有消息，继续下一次循环
                        continue;
                    }
                    foreach (var streamEntry in streamEntries)
                    {
                        var voucherOrderStr = "{" + string.Join(",", streamEntry.Values.ToList()) + "}";
                        var voucherOrders = JsonConvert.DeserializeObject<VocherOrderInfoDto>(voucherOrderStr);
                        var count = _voucherOrderManager.GetByWhere(x => x.UserId == voucherOrders.UserId && x.VoucherId == voucherOrders.VoucherId).Count();
                        if (count > 0)
                        {
                            continue;
                        }
                        var seckillVoucher = await _seckillVoucherManager.GetByWhereFirstOrDefaultAsync(x => x.VoucherId == voucherOrders.VoucherId);
                        // 创建事务
                        await _seckillVoucherManager.TransactionDoAsync(seckillVoucher, async (seckillVoucher) =>
                        {
                            // 减库存
                            seckillVoucher.Stock = seckillVoucher.Stock - 1;
                            await _seckillVoucherManager.UpdateAsync(seckillVoucher);

                            var voucherOrder = new VoucherOrder();
                            voucherOrder.Id = voucherOrders.Id;
                            voucherOrder.UserId = voucherOrders.UserId;
                            voucherOrder.VoucherId = voucherOrders.VoucherId;
                            voucherOrder.PayType = 3;
                            voucherOrder.Status = 2;
                            voucherOrder.CreateTime = DateTime.Now;
                            voucherOrder.PayTime = DateTime.Now;
                            voucherOrder.UseTime = DateTime.Now;
                            voucherOrder.RefundTime = DateTime.Now;
                            voucherOrder.UpdateTime = DateTime.Now;
                            // 创建订单
                            await _voucherOrderManager.CreateAsync(voucherOrder);

                            return seckillVoucher;
                        });
                        // 确认消息
                        _redisCacheManager.StreamAcknowledge(stream, streamConsumerName, streamEntry.Id.ToString());
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
                finally
                {

                }
            }
        }
        #endregion

        /// <summary>
        /// 秒杀抢优惠券
        /// </summary>
        /// <param name="voucherId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<string> SeckillVoucherAsync(long voucherId, long userId)
        {
            var seckillVoucher = await _seckillVoucherManager.GetByWhereFirstOrDefaultAsync(x => x.VoucherId == voucherId);
            if (seckillVoucher == null)
            {
                return "没有优惠券";
            }
            var nowTime = DateTime.Now;
            if (seckillVoucher.BeginTime > nowTime)
            {
                return "秒杀没有开始";
            }
            if (seckillVoucher.EndTime < nowTime)
            {
                return "秒杀结束";
            }
            if (seckillVoucher.Stock < 1)
            {
                return "库存不足";
            }
            var guid = Guid.NewGuid().ToString();
            var key = string.Format("{0}:{1}:{2}", CacheKeys.LOCK_KEY, "order", userId);
            // 获取线程id
            var threadId = string.Format("{0}-{1}", guid, Thread.CurrentThread.ManagedThreadId.ToString());
            // 加锁
            var lockStr = _redisCacheManager.StrSetNx(key, threadId, new TimeSpan(0, 1, 0, 0));
            // 加锁失败
            if (!lockStr)
            {
                return "不允许重复下单";
            }
            try
            {
                var voucherOrder = new VoucherOrder();
                // 创建事务
                _seckillVoucherManager.TransactionDo(seckillVoucher, (seckillVoucher) =>
                {
                    // 减库存
                    seckillVoucher.Stock = seckillVoucher.Stock - 1;
                    _seckillVoucherManager.Update(seckillVoucher);
                    _seckillVoucherManager.Save();

                    voucherOrder.Id = _redisCacheManager.NextId("order");
                    voucherOrder.UserId = userId;
                    voucherOrder.VoucherId = voucherId;
                    voucherOrder.PayType = 3;
                    voucherOrder.Status = 2;
                    voucherOrder.CreateTime = DateTime.Now;
                    voucherOrder.PayTime = DateTime.Now;
                    voucherOrder.UseTime = DateTime.Now;
                    voucherOrder.RefundTime = DateTime.Now;
                    voucherOrder.UpdateTime = DateTime.Now;
                    // 创建订单
                    _voucherOrderManager.Create(voucherOrder);
                    _voucherOrderManager.Save();

                    return seckillVoucher;
                });
                return "保存成功";
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                // 解决Redis分布式锁误删问题
                var threadIdValue = _redisCacheManager.StrGet(key);
                string nowThreadId = string.Format("{0}-{1}", guid, Thread.CurrentThread.ManagedThreadId.ToString());
                // 判断标示是否一致
                if (nowThreadId.Equals(threadIdValue))
                {
                    // 释放锁
                    await _redisCacheManager.DeleteAsync(key);
                }
            }
        }

        /// <summary>
        /// 优化后
        /// </summary>
        /// <param name="voucherId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<string> SeckillVouchersAsync(long voucherId, long userId)
        {
            var seckillVoucher = await _seckillVoucherManager.GetByWhereFirstOrDefaultAsync(x => x.VoucherId == voucherId);
            if (seckillVoucher == null)
            {
                return "没有优惠券";
            }
            var nowTime = DateTime.Now;
            if (seckillVoucher.BeginTime > nowTime)
            {
                return "秒杀没有开始";
            }
            if (seckillVoucher.EndTime < nowTime)
            {
                return "秒杀结束";
            }
            if (seckillVoucher.Stock < 1)
            {
                return "库存不足";
            }
            var guid = Guid.NewGuid().ToString();
            var key = string.Format("{0}:{1}:{2}", CacheKeys.LOCK_KEY, "order", userId);
            // 获取线程id
            var threadId = string.Format("{0}-{1}", guid, Thread.CurrentThread.ManagedThreadId.ToString());
            // 加锁
            var lockStr = _redisCacheManager.StrSetNx(key, threadId, new TimeSpan(0, 1, 0, 0));
            // 加锁失败
            if (!lockStr)
            {
                return "不允许重复下单";
            }
            try
            {
                var voucherOrder = new VoucherOrder();
                // 创建事务
                _seckillVoucherManager.TransactionDo(seckillVoucher, (seckillVoucher) =>
                {
                    // 减库存
                    seckillVoucher.Stock = seckillVoucher.Stock - 1;
                    _seckillVoucherManager.Update(seckillVoucher);
                    _seckillVoucherManager.Save();

                    voucherOrder.Id = _redisCacheManager.NextId("order");
                    voucherOrder.UserId = userId;
                    voucherOrder.VoucherId = voucherId;
                    voucherOrder.PayType = 3;
                    voucherOrder.Status = 2;
                    voucherOrder.CreateTime = DateTime.Now;
                    voucherOrder.PayTime = DateTime.Now;
                    voucherOrder.UseTime = DateTime.Now;
                    voucherOrder.RefundTime = DateTime.Now;
                    voucherOrder.UpdateTime = DateTime.Now;
                    // 创建订单
                    _voucherOrderManager.Create(voucherOrder);
                    _voucherOrderManager.Save();

                    return seckillVoucher;
                });
                return "保存成功";
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                // 解决Redis分布式锁误删问题,lua脚本保证原子性
                string nowThreadId = string.Format("{0}-{1}", guid, Thread.CurrentThread.ManagedThreadId.ToString());
                string strLua = @"if (redis.call('GET', @key) == @argv) then 
                                    return redis.call('DEL', @key)
                                 end
                                 return 0";
                _redisCacheManager.LuaScripts(strLua, new { key = string.Format("{0}:{1}", CacheKeys.REDIS_DATA_KEY, key), argv = nowThreadId });
            }
        }

        /// <summary>
        /// 用redis队列优化后
        /// </summary>
        /// <param name="voucherId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<string> SeckillVoucherStreamAsync(long voucherId, long userId)
        {
            var seckillVoucher = await _seckillVoucherManager.GetByWhereFirstOrDefaultAsync(x => x.VoucherId == voucherId);
            if (seckillVoucher == null)
            {
                return "没有优惠券";
            }
            var nowTime = DateTime.Now;
            if (seckillVoucher.BeginTime > nowTime)
            {
                return "秒杀没有开始";
            }
            if (seckillVoucher.EndTime < nowTime)
            {
                return "秒杀结束";
            }
            var orderId = _redisCacheManager.NextId("order");
            // 秒杀优惠券，将库存存到redis中秒杀
            var strLua = @"
               local voucherId = @voucherId
               local userId = @userId
               local orderId = @orderId
               local stockKey = 'MyRedis:seckill:stock:'..voucherId
               local orderKey = 'MyRedis:seckill:order:'..voucherId
               if(tonumber(redis.call('get', stockKey)) <= 0) then
                 return 1
               end
               if(redis.call('sismember', orderKey, userId) == 1) then
                 return 2
               end
               redis.call('incrby', stockKey, -1)
               redis.call('sadd', orderKey, userId)
               redis.call('xadd', 'MyRedis:stream_orders', '*', 'userId', userId, 'voucherId', voucherId, 'id', orderId)
               return 0";
            // 执行lua脚本
            var luaResult = _redisCacheManager.LuaScripts(strLua, new { voucherId = voucherId, userId = userId, orderId = orderId });
            if (long.Parse(luaResult.ToString()) != 0)
            {
                return "库存不足或者重复购买了";
            }
            return orderId.ToString();
        }

    }
}
