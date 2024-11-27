using Common.Helpers;
using Common.ResourcesParameters;
using Entity.Dtos;
using Entity.Models;
using IBll;
using IDal;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Bll
{
    public class CompanyService : BaseService<Company>, ICompanyService
    {
        private Dictionary<string, PropertyMappingValue> _companyPropertyMapping =
            new Dictionary<string, PropertyMappingValue>(StringComparer.OrdinalIgnoreCase)
          {
               { "Id", new PropertyMappingValue(new List<string>(){ "Id" }) },
               { "CompanyName", new PropertyMappingValue(new List<string>(){ "Name" })},
               { "Country", new PropertyMappingValue(new List<string>(){ "Country" })},
               { "Industry", new PropertyMappingValue(new List<string>(){ "Industry" })},
               { "Product", new PropertyMappingValue(new List<string>(){ "Product" })},
               { "Introduction", new PropertyMappingValue(new List<string>(){ "Introduction" })},
          };

        private readonly ICompanyManager _companyManager;
        public CompanyService(ICompanyManager companyManager) : base(companyManager)
        {
            _companyManager = companyManager;
            //保存
            _propertyMappings.Add(new PropertyMapping<CompanyDto, Company>(_companyPropertyMapping));
        }

        /// <summary>
        /// 条件查询
        /// </summary>
        /// <param name="whereLambda"></param>
        /// <returns></returns>
        public IQueryable<Company> QueryWhere(Expression<Func<Company, bool>> whereLambda)
        {
            return LoadEntities(whereLambda).AsQueryable();
        }

        /// <summary>
        /// 查询全部
        /// </summary>
        /// <returns></returns>
        public IQueryable<Company> QueryAll()
        {
            return LoadEntitiesAll("").AsQueryable();
        }

        /// <summary>
        /// 分页排序
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public PageList<Company> QueryPage(CompanyParameters parameters)
        {
            Dictionary<string, PropertyMappingValue> propertyMapping = GetPropertyMapping<CompanyDto, Company>();

            if (string.IsNullOrWhiteSpace(parameters.CompanyName))
            {
                return LoadPage(parameters.pageNumber, parameters.pageSize, null, parameters.orderBy, propertyMapping);
            }
            return LoadPage(parameters.pageNumber, parameters.pageSize, x => x.Name == parameters.CompanyName, parameters.orderBy, propertyMapping);

        }

        /// <summary>
        /// 集合资源超媒体链接
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        public IEnumerable<LinkDto> CreateLinksForCompanies(string fields, IUrlHelper urlHelper, string routeName)
        {
            List<LinkDto> linkList = new List<LinkDto>();
            if (string.IsNullOrWhiteSpace(fields))
            {
                linkList.Add(new LinkDto(urlHelper.Link(routeName, new { }), "self", "GET"));
            }
            else
            {
                linkList.Add(new LinkDto(urlHelper.Link(routeName, new { fields }), "self", "GET"));
            }
            return linkList;
        }

        /// <summary>
        /// 单个资源超媒体链接
        /// </summary>
        /// <param name="companyId"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public IEnumerable<LinkDto> CreateLinksForCompany(Guid? companyId, string fields, IUrlHelper urlHelper, string routeName)
        {
            List<LinkDto> linkList = new List<LinkDto>();
            if (string.IsNullOrWhiteSpace(fields))
            {
                linkList.Add(new LinkDto(urlHelper.Link(routeName, new { companyId }), "self", "GET"));
            }
            else
            {
                linkList.Add(new LinkDto(urlHelper.Link(routeName, new { fields }), "self", "GET"));
            }
            linkList.Add(new LinkDto(urlHelper.Link(routeName, new { companyId }), "delete_company", "DELETE"));
            linkList.Add(new LinkDto(urlHelper.Link(routeName, new { companyId }), "company_for_self_employees", "GET"));
            linkList.Add(new LinkDto(urlHelper.Link(routeName, new { companyId }), "company_for_create_employee", "POST"));
            return linkList;
        }
    }
}
