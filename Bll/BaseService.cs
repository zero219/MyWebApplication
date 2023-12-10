using Common.Helpers;
using IBll;
using IDal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Bll
{
    public class BaseService<T> : IBaseService<T> where T : class, new()
    {
        public IList<IPropertyMapping> _propertyMappings = new List<IPropertyMapping>();

        private readonly IBaseRepository<T> _repository;

        public BaseService(IBaseRepository<T> repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// 添加(异步)
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public async Task<bool> AddEntity(T entity)
        {
            await _repository.CreateAsync(entity);
            return await _repository.SaveAsync();
        }

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool EditEntity(T entity)
        {
            _repository.Update(entity);
            return _repository.Save();
        }

        /// <summary>
        /// 修改(异步)
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Task<bool> EditEntityAsync(T entity)
        {
            _repository.UpdateAsync(entity);
            return _repository.SaveAsync();
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool DeleteEntity(T entity)
        {
            _repository.Delete(entity);
            return _repository.Save();
        }

        /// <summary>
        /// 删除(异步)
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Task<bool> DeleteEntityAsync(T entity)
        {
            _repository.DeleteAsync(entity);
            return _repository.SaveAsync();
        }

        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="whereLambda"></param>
        /// <returns></returns>
        public IQueryable<T> LoadEntities(Expression<Func<T, bool>> whereLambda)
        {
            return _repository.GetByWhere(whereLambda);
        }

        /// <summary>
        /// 查询(异步)
        /// </summary>
        /// <param name="whereLambda"></param>
        /// <returns></returns>
        public Task<IQueryable<T>> LoadEntitiesAsync(Expression<Func<T, bool>> whereLambda)
        {
            return _repository.GetByWhereAsync(whereLambda);
        }

        /// <summary>
        /// 查询不带条件
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public IQueryable<T> LoadEntitiesAll(string entity = "")
        {
            return _repository.GetAll(entity);
        }

        /// <summary>
        /// 查询不带条件(异步)
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Task<IQueryable<T>> LoadEntitiesAllAsync(string entity)
        {
            return _repository.GetAllAsync(entity);
        }

        /// <summary>
        /// 分页
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="whereLambda"></param>
        /// <param name="orderby"></param>
        /// <param name="keyValuePairs"></param>
        /// <returns></returns>
        public PageList<T> LoadPage(int pageIndex, int pageSize, Expression<Func<T, bool>> whereLambda, string orderBy, Dictionary<string, PropertyMappingValue> keyValuePairs)
        {
            return _repository.GetPageOrderByQuery(pageIndex, pageSize, whereLambda, orderBy, keyValuePairs);
        }

        #region 动态排序
        /// <summary>
        /// 获得匹配的映射对象
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TDestination"></typeparam>
        /// <returns></returns>
        public Dictionary<string, PropertyMappingValue> GetPropertyMapping<TSource, TDestination>()
        {
            // 获得匹配的映射对象
            var matchingMapping = _propertyMappings.OfType<PropertyMapping<TSource, TDestination>>();
            if (matchingMapping.Count() == 1)
            {
                return matchingMapping.First()._mappingDictionary;
            }

            throw new Exception(
                $"无法找到唯一的映射关系:{typeof(TSource)},{typeof(TDestination)}");

        }
        /// <summary>
        /// 检测排序字段
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TDestination"></typeparam>
        /// <param name="fields"></param>
        /// <returns></returns>
        public bool IsMappingExists<TSource, TDestination>(string fields)
        {
            var propertyMapping = GetPropertyMapping<TSource, TDestination>();

            if (string.IsNullOrWhiteSpace(fields))
            {
                return true;
            }

            //逗号来分隔字段字符串
            var fieldsAfterSplit = fields.Split(",");

            foreach (var field in fieldsAfterSplit)
            {
                // 去掉空格
                var trimmedField = field.Trim();
                // 获得属性名称字符串
                var indexOfFirstSpace = trimmedField.IndexOf(" ", StringComparison.Ordinal);
                //-1表示空格不存在,存在则删除
                var propertyName = indexOfFirstSpace == -1 ? trimmedField : trimmedField.Remove(indexOfFirstSpace);
                //有一个不存在返回false
                if (!propertyMapping.ContainsKey(propertyName))
                {
                    return false;
                }
            }
            return true;
        }
        #endregion

    }
}
