using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace Common.Helpers
{
    /// <summary>
    /// 多个key,自定义模型绑定
    /// </summary>
    public class ArrayModelBinder : IModelBinder
    {
       

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            //判断类型是否为Enumerable
            if (!bindingContext.ModelMetadata.IsEnumerableType)
            {
                //返回失败
                bindingContext.Result = ModelBindingResult.Failed();
                return Task.CompletedTask;
            }

            //获取值
            var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).ToString();
            if (string.IsNullOrWhiteSpace(value))
            {
                //返回空
                bindingContext.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }
            //获取Guid
            var elementType = bindingContext.ModelType.GetTypeInfo().GenericTypeArguments[0];
            //转换器，把字符串转换成Guid类型
            var converter = TypeDescriptor.GetConverter(elementType);
            //分割逗号
            var values = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => converter.ConvertFromString(x.Trim()))
                .ToArray();
            //设定数组类型
            var typedValues = Array.CreateInstance(elementType, values.Length);
            values.CopyTo(typedValues, 0);
            bindingContext.Model = typedValues;
            //返回成功
            bindingContext.Result = ModelBindingResult.Success(bindingContext.Model);
            return Task.CompletedTask;
        }
    }
}
