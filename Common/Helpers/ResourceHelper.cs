using Common.ResourcesParameters;
using Entity.Dtos;
using Entity.EnumModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Common.Helpers
{
    public class ResourceHelper<TParameters> where TParameters : BaseParameters
    {
        private readonly IUrlHelper _urlHelper;

        public ResourceHelper(IUrlHelper urlHelper)
        {
            _urlHelper = urlHelper;
        }

        /// <summary>
        /// 翻页
        /// </summary>
        /// <param name="parameters">查询条件</param>
        /// <param name="resource">翻页</param>
        /// <param name="routeName">路由名称</param>
        /// <returns></returns>
        public string ResourceUri(TParameters parameters, ResourceUriType resource, string routeName = "")
        {
            var queryParams = new Dictionary<string, object>
            {
                { "fields", parameters.fields },
                { "orderBy", parameters.orderBy },
                { "pageNumber", resource == ResourceUriType.PreviousPage ? parameters.pageNumber - 1 : resource == ResourceUriType.NextPage ? parameters.pageNumber + 1 : parameters.pageNumber },
                { "pageSize", parameters.pageSize }
            };

            // 动态获取 TParameters 类型的额外属性
            foreach (var property in typeof(TParameters).GetProperties())
            {
                if (!queryParams.ContainsKey(property.Name) && property.GetValue(parameters) != null)
                {
                    queryParams[property.Name] = property.GetValue(parameters);
                }
            }

            return _urlHelper.Link(routeName, queryParams);
        }

        /// <summary>
        /// 超媒体链接加入分页
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="hasPrevious"></param>
        /// <param name="hasNext"></param>
        /// <returns></returns>
        public IEnumerable<LinkDto> CreateLinksForRouteNamePaging(TParameters parameters, bool hasPrevious, bool hasNext)
        {
            var links = new List<LinkDto>();
            //当前页
            links.Add(new LinkDto(ResourceUri(parameters, ResourceUriType.CurrentPage), "self", "GET"));
            //上一页
            if (hasPrevious)
            {
                links.Add(new LinkDto(ResourceUri(parameters, ResourceUriType.PreviousPage), "previous_page", "GET"));
            }
            //下一页
            if (hasNext)
            {
                links.Add(new LinkDto(ResourceUri(parameters, ResourceUriType.NextPage), "next_page", "GET"));
            }
            return links;
        }
    }
}
