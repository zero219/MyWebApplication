using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.Helpers
{
    /// <summary>
    /// 分页数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PageList<T> : List<T>
    {
        /// <summary>
        /// 当前页数
        /// </summary>
        public int CurrentPage { get; private set; }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages { get; private set; }

        /// <summary>
        /// 每页行数
        /// </summary>
        public int PageSize { get; private set; }

        /// <summary>
        /// 总数
        /// </summary>
        public int TotalCount { get; private set; }

        /// <summary>
        /// 上一页
        /// </summary>
        public bool HasPrevious { get => CurrentPage > 1; }

        /// <summary>
        /// 下一页
        /// </summary>
        public bool HasNext { get => CurrentPage < TotalPages; }

        public PageList(List<T> list, int count, int pageNumber, int pageSize)
        {
            TotalCount = count;
            CurrentPage = pageNumber;
            PageSize = pageSize;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            //将泛型集合的所有元素到指定泛型集合末尾
            AddRange(list);
        }
    }
}
