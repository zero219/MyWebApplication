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

        Task<bool> AddEntity(T entity);

        bool EditEntity(T entity);

        Task<bool> EditEntityAsync(T entity);

        bool DeleteEntity(T entity);

        Task<bool> DeleteEntityAsync(T entity);

        IQueryable<T> LoadEntities(Expression<Func<T, bool>> whereLambda);

        Task<IQueryable<T>> LoadEntitiesAsync(Expression<Func<T, bool>> whereLambda);

        IQueryable<T> LoadEntitiesAll(string entity);

        Task<IQueryable<T>> LoadEntitiesAllAsync(string entity);

        PageList<T> LoadPage(int pageIndex, int pageSize, Expression<Func<T, bool>> whereLambda, string orderby, Dictionary<string, PropertyMappingValue> keyValuePairs);

        Dictionary<string, PropertyMappingValue> GetPropertyMapping<TSource, TDestination>();

        bool IsMappingExists<TSource, TDestination>(string fields);
    }
}
