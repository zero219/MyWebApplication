using System;
using System.Collections.Generic;
using System.Text;

namespace Entity.Models.Seckill
{
    public class SeckillVoucher
    {
        public int Id { get; set; }
        /// <summary>
        /// 关联的优惠券Id
        /// </summary>
        public long VoucherId { get; set; }
        /// <summary>
        /// 库存
        /// </summary>
        public int Stock { get; set; }
        /// <summary>
        /// 生效时间
        /// </summary>
        public DateTime BeginTime { get; set; }
        /// <summary>
        /// 失效时间
        /// </summary>
        public DateTime EndTime { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; }
    }
}
