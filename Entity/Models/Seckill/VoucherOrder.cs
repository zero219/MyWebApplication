using System;
using System.Collections.Generic;
using System.Text;

namespace Entity.Models.Seckill
{
    public class VoucherOrder
    {
        /// <summary>
        /// 主键
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// 下单的用户id
        /// </summary>
        public long UserId { get; set; }
        /// <summary>
        /// 购买的代金券id
        /// </summary>
        public long VoucherId { get; set; }
        /// <summary>
        /// 支付方式 1：余额支付；2：支付宝；3：微信
        /// </summary>
        public sbyte PayType { get; set; }
        /// <summary>
        /// 订单状态，1：未支付；2：已支付；3：已核销；4：已取消；5：退款中；6：已退款
        /// </summary>
        public sbyte Status { get; set; }
        /// <summary>
        /// 下单时间
        /// </summary>
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// 支付时间
        /// </summary>
        public DateTime PayTime { get; set; }
        /// <summary>
        /// 核销时间
        /// </summary>
        public DateTime UseTime { get; set; }
        /// <summary>
        /// 退款时间
        /// </summary>
        public DateTime RefundTime { get; set; }
        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; }
       
    }
}
