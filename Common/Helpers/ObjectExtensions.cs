using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using System.Text;

namespace Common.Helpers
{
    /// <summary>
    /// 数据塑性拓展方法
    /// </summary>
    public static class DataPlasticityExtensions
    {
        /// <summary>
        /// 多个资源塑性
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source"></param>
        /// <param name="fields"></param>
        /// <returns>ExpandoObject动态类型对象,动态添加或删除一个类成员对象</returns>
        public static IEnumerable<ExpandoObject> ShapeData<TSource>(this IEnumerable<TSource> source, string fields)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var expandoObjList = new List<ExpandoObject>();
            //创建属性列表
            var propertyInfoList = new List<PropertyInfo>();
            if (string.IsNullOrWhiteSpace(fields))
            {
                //返回类型动态对象ExpandoObject的所有属性
                var propertyInfos = typeof(TSource).GetProperties(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                propertyInfoList.AddRange(propertyInfos);
            }
            else
            {
                //分割逗号
                var fieldsSplit = fields.Split(',');
                foreach (var filed in fieldsSplit)
                {
                    //去空格
                    var propertyName = filed.Trim();
                    //获取类成员属性
                    var propertyInfo = typeof(TSource).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    if (propertyInfo == null)
                    {
                        throw new Exception($"属性:{propertyName}没有找到");
                    }
                    propertyInfoList.Add(propertyInfo);
                }

            }
            foreach (TSource soureObject in source)
            {
                //创建动态类型对象,创建数据塑性对象
                var dataShapedObject = new ExpandoObject();
                //循环属性
                foreach (var propertyInfo in propertyInfoList)
                {
                    //获得属性真实数据
                    var propertyValue = propertyInfo.GetValue(soureObject);
                    //保存属性名,属性值
                    ((IDictionary<string, object>)dataShapedObject).Add(propertyInfo.Name, propertyValue);
                }
                expandoObjList.Add(dataShapedObject);
            }
            return expandoObjList;
        }
        /// <summary>
        /// 单个资源塑性
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public static ExpandoObject SingleShapeData<TSource>(this TSource source, string fields)
        {

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var expandoObject = new ExpandoObject();
            if (string.IsNullOrWhiteSpace(fields))
            {
                //返回类型动态对象ExpandoObject的所有属性
                var propertyInfos = typeof(TSource).GetProperties(BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                foreach (var propertyInfo in propertyInfos)
                {
                    var propertyValue = propertyInfo.GetValue(source);
                    ((IDictionary<string, object>)expandoObject).Add(propertyInfo.Name, propertyValue);
                }
                return expandoObject;
            }

            //分割逗号
            var fieldsSplit = fields.Split(',');
            foreach (var filed in fieldsSplit)
            {
                //去空格
                var propertyName = filed.Trim();
                //获取类成员属性
                var propertyInfo = typeof(TSource).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo == null)
                {
                    throw new Exception($"属性:{propertyName}没有找到");
                }
                var propertyValue = propertyInfo.GetValue(source);
                ((IDictionary<string, object>)expandoObject).Add(propertyInfo.Name, propertyValue);
            }

            return expandoObject;
        }
    }
}
