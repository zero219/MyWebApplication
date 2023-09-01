using System;
using System.Collections.Generic;
using System.Text;

namespace Entity.Dtos
{
    public class MenuDataListDto
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 子节点
        /// </summary>
        public List<Children> Children { get; set; }
    }
    public class Children
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 路径
        /// </summary>
        public string Path { get; set; }
    }
}
