using IBll;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Bll
{
    public class PropertyCheckerService : IPropertyCheckerService
    {
        /// <summary>
        /// 检测塑形字段是否存在
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fields"></param>
        /// <returns></returns>
        public bool TypeHasProperties<T>(string fields)
        {
            if (string.IsNullOrEmpty(fields))
            {
                return true;
            }
            //分割逗号
            var fieldsSplit = fields.Split(',');
            foreach (var filed in fieldsSplit)
            {
                //去空格
                var propertyName = filed.Trim();
                //获取类成员属性
                var propertyInfo = typeof(T).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo == null)
                {
                   return false;
                }
            }
            return true;
        }
    }
}
