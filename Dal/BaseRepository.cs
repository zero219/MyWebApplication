
using Common.Helpers;
using Entity.Data;
using IDal;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Dal
{
    public class BaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : class, new()
    {
        private readonly RoutineDbContext _context;

        public BaseRepository(RoutineDbContext context)
        {
            //如果 ?? 运算符的左操作数非空，该运算符将返回左操作数，否则返回右操作数。
            _context = context ?? throw new ArgumentException(nameof(context));
        }

        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public TEntity Create(TEntity entity)
        {
            _context.Set<TEntity>().Add(entity);
            return entity;
        }

        /// <summary>
        /// 添加(异步)
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task<TEntity> CreateAsync(TEntity entity)
        {
            await _context.Set<TEntity>().AddAsync(entity);
            return entity;
        }

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="entity"></param>
        public void Update(TEntity entity)
        {
            //修改全部属性
            _context.Entry(entity).State = EntityState.Modified;
        }

        /// <summary>
        /// 修改(异步)
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Task UpdateAsync(TEntity entity)
        {
            //修改全部属性
            return Task.Run(() =>
            {
                _context.Entry(entity).State = EntityState.Modified;
            });
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="entity"></param>
        public void Delete(TEntity entity)
        {
            _context.Entry<TEntity>(entity).State = EntityState.Deleted;
        }

        /// <summary>
        /// 删除(异步)
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Task DeleteAsync(TEntity entity)
        {
            return Task.Run(() =>
            {
                _context.Entry<TEntity>(entity).State = EntityState.Deleted;
            });
        }

        /// <summary>
        /// 获取全部数据
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public IQueryable<TEntity> GetAll(string str)
        {
            return string.IsNullOrEmpty(str) ? _context.Set<TEntity>().AsQueryable() : _context.Set<TEntity>().Include(str).AsQueryable();
        }

        /// <summary>
        /// 获取全部数据(异步)
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public Task<IQueryable<TEntity>> GetAllAsync(string str)
        {
            return string.IsNullOrEmpty(str) ? (Task<IQueryable<TEntity>>)Task.Run(() =>
            {
                _context.Set<TEntity>().AsQueryable();
            }) : (Task<IQueryable<TEntity>>)Task.Run(() =>
            {
                _context.Set<TEntity>().Include(str).AsQueryable();

            });
        }

        /// <summary>
        /// 条件查询
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public IQueryable<TEntity> GetByWhere(Expression<Func<TEntity, bool>> where)
        {
            return _context.Set<TEntity>().Where<TEntity>(where).AsQueryable();
        }

        /// <summary>
        /// 条件查询(异步)
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public Task<IQueryable<TEntity>> GetByWhereAsync(Expression<Func<TEntity, bool>> where)
        {
            return (Task<IQueryable<TEntity>>)Task.Run(() =>
            {
                _context.Set<TEntity>().Where<TEntity>(where).AsQueryable();
            });
        }

        /// <summary>
        /// 条件查询(异步),转对象
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public async Task<TEntity> GetByWhereFirstOrDefaultAsync(Expression<Func<TEntity, bool>> where)
        {
            return await _context.Set<TEntity>().Where(where).FirstOrDefaultAsync();
        }

        /// <summary>
        /// 分页
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <param name="whereLambda"></param>
        /// <param name="orderBy"></param>
        /// <param name="keyValuePairs"></param>
        /// <returns></returns>
        public PageList<TEntity> GetPageOrderByQuery(int pageNumber, int pageSize,
            Expression<Func<TEntity, bool>> whereLambda, string orderBy,
            Dictionary<string, PropertyMappingValue> keyValuePairs)
        {
            IQueryable<TEntity> queryable;

            if (whereLambda != null)
            {
                queryable = _context.Set<TEntity>().Where<TEntity>(whereLambda.Compile()).AsQueryable();
            }
            else
            {
                queryable = _context.Set<TEntity>().AsQueryable();
            }
            //Compile()将表达式树描述的 lambda 表达式编译为可执行代码，并生成表示 lambda 表达式的委托
            //var queryable = _context.Set<TEntity>().Where<TEntity>(whereLambda.Compile()).AsQueryable();
            var count = queryable.Count();

            //排序
            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                queryable = queryable.ApplySort(orderBy, keyValuePairs);
            }
            List<TEntity> rows;
            rows = queryable.Skip<TEntity>((pageNumber - 1) * pageSize).Take<TEntity>(pageSize).ToList();

            return new PageList<TEntity>(rows, count, pageNumber, pageSize);
        }

        /// <summary>
        /// 事务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public T TransactionDo<T>(T t, Func<T, T> func)
        {
            T result = default(T);

            try
            {
                _context.Database.BeginTransaction();
                result = func(t);
                _context.SaveChanges();
                _context.Database.CommitTransaction();
            }
            catch (Exception ex)
            {
                _context.Database.RollbackTransaction();
                throw ex;
            }
            finally
            {
              
            }
            return result;
        }

        /// <summary>
        /// 异步事务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public async Task<T> TransactionDoAsync<T>(T t, Func<T, Task<T>> func)
        {
            T result = default(T);

            try
            {
                await _context.Database.BeginTransactionAsync();
                result = await func(t);
                await _context.SaveChangesAsync();
                await _context.Database.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                await _context.Database.RollbackTransactionAsync();
                throw ex;
            }
            finally
            {
               
            }
            return result;
        }
        /// <summary>
        /// 保存
        /// </summary>
        /// <returns></returns>
        public bool Save()
        {
            return _context.SaveChanges() >= 0;
        }

        /// <summary>
        /// 保存(异步)
        /// </summary>
        /// <returns></returns>
        public async Task<bool> SaveAsync()
        {
            return await _context.SaveChangesAsync() >= 0;
        }

        /// <summary>
        /// 释放
        /// </summary>
        public void Dispose()
        {
            _context.Dispose();
        }

        /// <summary>
        /// 释放资源(异步)
        /// </summary>
        /// <returns></returns>
        public Task DisposeAsync()
        {
            return Task.Run(() =>
            {
                _context.Dispose();
            });
        }
    }
}
