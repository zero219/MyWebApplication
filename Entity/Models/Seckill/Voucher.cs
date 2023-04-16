using System;
using System.Collections.Generic;
using System.Text;

namespace Entity.Models.Seckill
{
    /// <summary>
    /// 优惠券表
    /// </summary>
    public class Voucher
    {
        /// <summary>
        /// 主键id
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// 店铺id
        /// </summary>
        public long ShopId { get; set; }
        /// <summary>
        /// 代金券标题
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        ///  副标题
        /// </summary>
        public string SubTitle { get; set; }
        /// <summary>
        /// 使用规则
        /// </summary>
        public string Rules { get; set; }
        /// <summary>
        /// 支付金额，单位分，200代表2元
        /// </summary>
        public long PayValue { get; set; }
        /// <summary>
        /// 抵扣金额，单位分
        /// </summary>
        public long ActualValue { get; set; }
        /// <summary>
        /// 0普通券；1秒杀卷
        /// </summary>
        public sbyte Type { get; set; }
        /// <summary>
        /// 1上架；2下架；3过期
        /// </summary>
        public sbyte Status { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// 更新时间
        /// </summary>s
        public DateTime UpdateTime { get; set; }

    }
}
