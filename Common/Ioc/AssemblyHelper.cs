using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Common.Ioc
{
    public static class AssemblyHelper
    {
        /// <summary>
        /// 程序集依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <param name="assemblyName"></param>
        /// <param name="serviceLifetime"></param>
        public static void AddAssembly(this IServiceCollection services, string assemblyName, ServiceLifetime serviceLifetime)
        {
            try
            {
                if (services == null)
                {
                    throw new ArgumentNullException((nameof(services) + "为空"));
                }
                if (string.IsNullOrEmpty(assemblyName))
                {
                    throw new ArgumentNullException((nameof(assemblyName) + "为空"));
                }
                //根据 AssemblyName 解析并加载程序集。
                //var assemblyByName = AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(assemblyName));
                var assemblyByName = Assembly.LoadFrom(assemblyName);
                if (assemblyByName == null)
                {
                    throw new DllNotFoundException(nameof(assemblyByName) + ".dll不存在");
                }
                //获取当前实例的 Type
                var types = assemblyByName.GetTypes();
                //获取不是值类型或接口类型、不为抽象类、泛型类型的程序集
                var list = types.Where(x => x.IsClass && !x.IsAbstract && !x.IsGenericType).ToList();
                foreach (var type in list)
                {
                    //获取由当前 Type 实现或继承的所有接口。
                    var iList = type.GetInterfaces();
                    //将所有接口注入。
                    foreach (var item in iList)
                    {
                        //选择依赖注入类型
                        switch (serviceLifetime)
                        {
                            case ServiceLifetime.Singleton:
                                services.AddSingleton(item, type);
                                break;
                            case ServiceLifetime.Scoped:
                                services.AddScoped(item, type);
                                break;
                            case ServiceLifetime.Transient:
                                services.AddTransient(item, type);
                                break;
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

        }
    }
}
