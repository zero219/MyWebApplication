using System;
using System.Collections.Generic;

namespace Timer.Models
{
    public class JobConfig
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 类型
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 触发器
        /// </summary>
        public TriggerConfig Trigger { get; set; }
    }
    public class TriggerConfig
    {
        /// <summary>
        /// 触发器类型
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 触发器属性
        /// </summary>
        public Dictionary<string, string> Properties { get; set; }

    }
}
