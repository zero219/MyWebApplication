using System;
using System.Collections.Generic;
using System.Text;

namespace Entity.Models
{
    /// <summary>
    /// 关注
    /// </summary>
    public class Follow
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
        /// 关注用户id
        /// </summary>
        public long FollowUserId { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
    }
}
