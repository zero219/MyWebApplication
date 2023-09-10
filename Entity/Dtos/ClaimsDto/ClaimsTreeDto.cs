using System;
using System.Collections.Generic;
using System.Text;

namespace Entity.Dtos.ClaimsDto
{
    /// <summary>
    /// 权限
    /// </summary>
    public class ClaimsTreeDto
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Label { get; set; }
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
        public string Label { get; set; }
    }
}
