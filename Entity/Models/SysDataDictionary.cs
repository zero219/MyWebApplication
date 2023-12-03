using System;
using System.Collections.Generic;
using System.Text;

namespace Entity.Models
{
    public class SysDataDictionary
    {
        /// <summary>
        /// id
        /// </summary>
        public int DictionaryId { get; set; }
        /// <summary>
        /// 类别key,Golbal
        /// </summary>
        public int CategoryKey { get; set; }
        /// <summary>
        /// 类别，全局配置
        /// </summary>
        public int Category { get; set; }
        /// <summary>
        /// 字典名称
        /// </summary>
        public int Name { get; set; }
        /// <summary>
        /// 键
        /// </summary>
        public int Key { get; set; }
        /// <summary>
        /// 键值
        /// </summary>
        public int Value { get; set; }
        /// <summary>
        /// 排序
        /// </summary>
        public int SortNumber { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int ParentId { get; set; }
        /// <summary>
        /// 是否系统定义；0-用户定义，1系统定义
        /// </summary>
        public int SysDefined { get; set; }
        /// <summary>
        /// Guid
        /// </summary>
        public int RowGuid { get; set; }
        /// <summary>
        /// 创建人
        /// </summary>
        public int Creator { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public int CreatorTime { get; set; }
        /// <summary>
        /// 修改人
        /// </summary>
        public int Modifier { get; set; }
        /// <summary>
        /// 修改时间
        /// </summary>
        public int ModifierTime { get; set; }
        /// <summary>
        /// 是否启动
        /// </summary>
        public int IsEnable { get; set; }

    }
}
