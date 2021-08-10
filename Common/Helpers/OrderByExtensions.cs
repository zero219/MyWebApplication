using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Dynamic.Core;

namespace Common.Helpers
{
    /// <summary>
    /// 排序拓展
    /// </summary>
    public static class OrderByExtensions
    {
        /// <summary>
        /// 多字段排序
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="orderBy"></param>
        /// <param name="mappingDictionary"></param>
        /// <returns></returns>
        public static IQueryable<T> ApplySort<T>(this IQueryable<T> source, string orderBy, Dictionary<string, PropertyMappingValue> mappingDictionary)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (mappingDictionary == null)
            {
                throw new ArgumentNullException("mappingDictionary");
            }

            if (string.IsNullOrWhiteSpace(orderBy))
            {
                return source;
            }

            var orderByString = string.Empty;

            var orderByAfterSplit = orderBy.Split(',');

            foreach (var order in orderByAfterSplit.Reverse())
            {
                var trimmedOrder = order.Trim();

                // 通过字符串“ desc”来判断升序还是降序
                var orderDescending = trimmedOrder.EndsWith(" desc");

                // 判断是否有空格
                var indexOfFirstSpace = trimmedOrder.IndexOf(" ");
                //-1表示不存在,存在则删除
                var propertyName = indexOfFirstSpace == -1 ? trimmedOrder : trimmedOrder.Remove(indexOfFirstSpace);
                //检测字典中是否有propertyName映射
                if (!mappingDictionary.ContainsKey(propertyName))
                {
                    throw new ArgumentException($"没有找到Key为{propertyName}的映射");
                }
                //通过key取值
                var propertyMappingValue = mappingDictionary[propertyName];
                if (propertyMappingValue == null)
                {
                    throw new ArgumentNullException(nameof(propertyMappingValue));
                }

                foreach (var destinationProperty in propertyMappingValue.DestinationProperties.Reverse())
                {
                    // 给IQueryable 添加排序字符串
                    source = source.OrderBy(destinationProperty + (orderDescending ? " descending" : " ascending"));
                }
            }
            return source;
        }


    }
    /// <summary>
    /// 映射model属性
    /// </summary>
    public class PropertyMappingValue
    {
        public IEnumerable<string> DestinationProperties { get; private set; }
        public PropertyMappingValue(IEnumerable<string> destinationProperties)
        {
            DestinationProperties = destinationProperties;
        }
    }
}
