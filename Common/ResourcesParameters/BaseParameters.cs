using System;
using System.Collections.Generic;
using System.Text;

namespace Common.ResourcesParameters
{
    public class BaseParameters
    {
        private int _pageSize = 5;

        /// <summary>
        /// 最大行数,const声明常量和局部变量,使变量不能修改
        /// </summary>
        private const int maxPageSize = 10;
        /// <summary>
        /// 页码
        /// </summary>
        public int pageNumber { get; set; } = 1;

        /// <summary>
        /// 行数
        /// </summary>
        public int pageSize
        {
            get => _pageSize;

            set => _pageSize = value > maxPageSize ? maxPageSize : value;
        }

        /// <summary>
        /// 塑形的字段
        /// </summary>
        public string fields { get; set; }

        /// <summary>
        /// 多个或单个字段排序
        /// </summary>
        public string orderBy { get; set; }
    }
}
