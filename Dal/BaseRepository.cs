﻿
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
        /// 修改
        /// </summary>
        /// <param name="entity"></param>
        public void Update(TEntity entity)
        {
            //修改全部属性
            _context.Entry(entity).State = EntityState.Modified;
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
        /// 获取全部数据
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public IQueryable<TEntity> GetAll(string str)
        {
            return string.IsNullOrEmpty(str) ? _context.Set<TEntity>().AsQueryable() : _context.Set<TEntity>().Include(str).AsQueryable();
        }

        /// <summary>
        /// 条件查询
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public IQueryable<TEntity> GetByWhere(Expression<Func<TEntity, bool>> where)
        {
            return _context.Set<TEntity>().Where<TEntity>(where);
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
        public async Task<PageList<TEntity>> GetPageOrderByQuery(int pageNumber, int pageSize, Expression<Func<TEntity, bool>> whereLambda, string orderBy, Dictionary<string, PropertyMappingValue> keyValuePairs)
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
            var count = await queryable.CountAsync();

            //排序
            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                queryable= queryable.ApplySort(orderBy, keyValuePairs);
            }
            List<TEntity> rows;
            rows = await queryable.Skip<TEntity>((pageNumber - 1) * pageSize).Take<TEntity>(pageSize).ToListAsync();


            return new PageList<TEntity>(rows, count, pageNumber, pageSize);
        }



        /// <summary>
        /// 保存
        /// </summary>
        /// <returns></returns>
        public async Task<bool> SaveAsync()
        {
            return await _context.SaveChangesAsync() >= 0;
        }

        public void Dispose()
        {
            _context.Dispose();
        }

    }
}
