using Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace IBll
{
    public interface IBaseService<T> where T : class, new()
    {

        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="whereLambda"></param>
        /// <returns></returns>
        IQueryable<T> LoadEntities(Expression<Func<T, bool>> whereLambda);

        /// <summary>
        /// 查询不带条件
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        IQueryable<T> LoadEntitiesAll(string entity);



        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task<bool> DeleteEntity(T entity);

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task<bool> EditEntity(T entity);

        Task<PageList<T>> LoadPage(int pageIndex, int pageSize, Expression<Func<T, bool>> whereLambda, string orderby, Dictionary<string, PropertyMappingValue> keyValuePairs);
        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task<bool> AddEntity(T entity);


        Dictionary<string, PropertyMappingValue> GetPropertyMapping<TSource, TDestination>();
        bool IsMappingExists<TSource, TDestination>(string fields);
    }
}
