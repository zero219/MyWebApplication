using Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace IDal
{
    public interface IBaseRepository<TEntity>  where TEntity : class, new()
    {
        TEntity Create(TEntity entity);

        Task<TEntity> CreateAsync(TEntity entity);

        void Update(TEntity entity);

        Task UpdateAsync(TEntity entity);

        void Delete(TEntity entity);

        Task DeleteAsync(TEntity entity);

        IQueryable<TEntity> GetAll(string str);

        Task<IQueryable<TEntity>> GetAllAsync(string str);

        IQueryable<TEntity> GetByWhere(Expression<Func<TEntity, bool>> where);

        Task<IQueryable<TEntity>> GetByWhereAsync(Expression<Func<TEntity, bool>> where);

        public Task<PageList<TEntity>> GetPageOrderByQuery(int pageNumber, int pageSize, Expression<Func<TEntity, bool>> whereLambda, string orderBy, Dictionary<string, PropertyMappingValue> keyValuePairs);

        bool Save();

        Task<bool> SaveAsync();

        void Dispose();

        Task DisposeAsync();
    }
}
