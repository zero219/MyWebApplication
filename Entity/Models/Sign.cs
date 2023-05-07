using System;
using System.Collections.Generic;
using System.Text;

namespace Entity.Models
{
    /// <summary>
    /// 签到
    /// </summary>
    public class Sign
    {
        /// <summary>
        /// id
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// 用户id
        /// </summary>
        public long UserId { get; set; }
        /// <summary>
        /// 签到的年
        /// </summary>
        public int Year { get; set; }
        /// <summary>
        /// 签到的月
        /// </summary>
        public int Month { get; set; }
        /// <summary>
        /// 签到的日
        /// </summary>
        public int Date { get; set; }
        /// <summary>
        /// 是否补签
        /// </summary>
        public sbyte IsBackUp { get; set; }
    }
}
