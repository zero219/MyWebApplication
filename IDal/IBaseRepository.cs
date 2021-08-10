using Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace IDal
{
    public interface IBaseRepository<TEntity> where TEntity : class, new()
    {
        Task<TEntity> CreateAsync(TEntity entity);
        TEntity Create(TEntity entity);

        void Update(TEntity entity);

        void Delete(TEntity entity);

        IQueryable<TEntity> GetByWhere(Expression<Func<TEntity, bool>> where);
        IQueryable<TEntity> GetAll(string str);
        public Task<PageList<TEntity>> GetPageOrderByQuery(int pageNumber, int pageSize, Expression<Func<TEntity, bool>> whereLambda, string orderBy, Dictionary<string, PropertyMappingValue> keyValuePairs);
        Task<bool> SaveAsync();

    }
}
