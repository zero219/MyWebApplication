using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Redis
{
    public class RedisData
    {
        public object obj { get; set; }

        /// <summary>
        /// 到期时间
        /// </summary>
        public long? expireTime { get; set; }
    }
}
