using AutoMapper;
using Entity.Models;
using IBll;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Entity.Dtos;
using Common.ResourcesParameters;
using Common.Helpers;
using Entity.EnumModels;
using System.Text.Json;
using System.Text.Encodings.Web;
using Microsoft.Net.Http.Headers;
using Common.ActionAttributes;
using Marvin.Cache.Headers;
using Microsoft.AspNetCore.Authorization;
using Entity.Dtos.CompaniesDtos;

namespace Api.Controllers
{
    /**
     * ApiController是应用于Controller，他并不是强制，回启动以下行为：
     * 要求使用属性路由
     * 自动HTTP 400响应
     * 推断参数的绑定源
     *  Multipart/form-data请求推断
     *  错误状态代码的问题和详细信息 
     **/
    [ApiController]
    [Route("api")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Authorize(Policy = "SystemAndAdmin")]
    //隐藏某个webapi
    //[ApiExplorerSettings(IgnoreApi = true)]
    public class CompaniesController : ControllerBase
    {
        private readonly ICompanyService _companyService;
        private readonly IMapper _mapper;
        private readonly IPropertyCheckerService _propertyCheckerService;
        public CompaniesController(ICompanyService companyService, IMapper mapper, IPropertyCheckerService propertyCheckerService)
        {
            _companyService = companyService ?? throw new ArgumentNullException(nameof(companyService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _propertyCheckerService = propertyCheckerService;
        }

        /// <summary>
        /// 获取所有公司
        /// </summary>
        /// <param name="mediaType">输出类型</param>
        /// <param name="parameters">参数</param>
        /// <returns></returns>
        [HttpGet("companies", Name = nameof(GetCompanies))]
        [HttpHead("companies")]
        //局部注册Accept的MediaType
        [Produces("application/json",
            "application/vnd.company.hateoas+json",
            "application/vnd.company.company.full+json",
            "application/vnd.company.company.full.hateoas+json")]
        //ETAG缓存过期模型
        [HttpCacheExpiration(CacheLocation = CacheLocation.Public, MaxAge = 60)]
        //ETAG缓存验证模型
        [HttpCacheValidation(MustRevalidate = true)]
        public async Task<ActionResult> GetCompanies([FromHeader(Name = "Accept")] string mediaType, [FromQuery] CompanyParameters parameters)
        {

            //判断字段是否为空
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }
            //判断排序字段是否正确
            if (!_companyService.IsMappingExists<CompanyDto, Company>(parameters.orderBy))
            {
                return BadRequest("请输入正确的排序参数");
            }
            //判断塑性字段是否存在
            if (!_propertyCheckerService.TypeHasProperties<CompanyDto>(parameters.fields))
            {
                return BadRequest();
            }
            //判断数据
            if (!await _companyService.QueryAll().AnyAsync())
            {
                return NotFound();
            }

            var company = await _companyService.QueryPage(parameters);

            #region 分页
            //上一页
            var previousPageLink = company.HasPrevious ? ResoureUri(parameters, ResourceUriType.PreviousPage) : null;
            //下一页
            var nextPageLink = company.HasNext ? ResoureUri(parameters, ResourceUriType.NextPage) : null;

            var paginationMetadata = new
            {
                totalCount = company.TotalCount,
                pageSize = company.PageSize,
                currentPage = company.CurrentPage,
                totalPages = company.TotalPages,
                previousPage = previousPageLink,
                nextPage = nextPageLink,
            };
            //自定义翻页Header
            Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(paginationMetadata, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            }));
            #endregion

            #region Media Type
            //解析MediaType,并赋值parsedMediaType
            if (MediaTypeHeaderValue.TryParse(mediaType, out MediaTypeHeaderValue parsedMediaType))
            {
                //判断请求头的尾部是否包含hateoas
                var includeLinks = parsedMediaType.SubTypeWithoutSuffix.EndsWith("hateoas", StringComparison.InvariantCultureIgnoreCase);

                var primaryMediaType = includeLinks
                   ? parsedMediaType.SubTypeWithoutSuffix.Substring(0, parsedMediaType.SubTypeWithoutSuffix.Length - 8)
                   : parsedMediaType.SubTypeWithoutSuffix;

                //判断请求头是否为vnd.company.company.full
                if (primaryMediaType.Value == "vnd.company.company.full")
                {
                    var companyFullDto = _mapper.Map<IEnumerable<CompanyFullDto>>(company);
                    //ShapeData数据塑形
                    var shapeFullData = companyFullDto.ShapeData(parameters.fields);
                    if (includeLinks)
                    {
                        //数据中加入超媒体链接
                        var shapeCompaniesWithLinks = shapeFullData.Select(c =>
                        {
                            var companyDict = c as IDictionary<string, object>;
                            var companyLinks = CreateLinksForCompanies(parameters.fields);
                            companyDict.Add("links", companyLinks);
                            return companyDict;
                        });
                        //value:数据，links:分页超媒体链接
                        var fullLinkedCollectionResource = new
                        {
                            value = shapeCompaniesWithLinks,
                            links = CreateLinksForCompany(parameters, company.HasPrevious, company.HasNext)
                        };
                        return Ok(fullLinkedCollectionResource);
                    }
                    return Ok(shapeFullData);
                }

                if (includeLinks)
                {
                    var companyHateoasDto = _mapper.Map<IEnumerable<CompanyDto>>(company);
                    //ShapeData数据塑形
                    var shapeHateoasData = companyHateoasDto.ShapeData(parameters.fields);
                    //数据中加入超媒体链接
                    var shapeCompaniesWithLinks = shapeHateoasData.Select(c =>
                    {
                        var companyDict = c as IDictionary<string, object>;
                        var companyLinks = CreateLinksForCompany((Guid)companyDict["Id"], null);
                        companyDict.Add("links", companyLinks);
                        return companyDict;
                    });
                    //value:数据，links:分页超媒体链接
                    var linkedCollectionResource = new
                    {
                        value = shapeCompaniesWithLinks,
                        links = CreateLinksForCompany(parameters, company.HasPrevious, company.HasNext)
                    };
                    return Ok(linkedCollectionResource);
                }
            }
            #endregion

            //转换dto
            var companyDto = _mapper.Map<IEnumerable<CompanyDto>>(company);
            //ShapeData数据塑形
            var shapeData = companyDto.ShapeData(parameters.fields);
            return Ok(shapeData);

        }

        /// <summary>
        /// 获取公司
        /// </summary>
        /// <param name="companyId"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        [HttpGet("companies/{companyId}", Name = nameof(GetCompany))]//Name:路由名称
        public async Task<ActionResult> GetCompany(Guid companyId, string fields)
        {
            if (!await _companyService.QueryWhere(p => p.Id == companyId).AnyAsync())
            {
                return NotFound();
            }
            //判断塑性字段是否存在
            if (!_propertyCheckerService.TypeHasProperties<CompanyDto>(fields))
            {
                return BadRequest();
            }
            var company = await _companyService.QueryWhere(p => p.Id == companyId).FirstOrDefaultAsync();
            var companyDtos = _mapper.Map<CompanyDto>(company);
            //SingleShapeData单个资源数据塑性
            var shapeData = companyDtos.SingleShapeData(fields) as IDictionary<string, object>;
            //添加超媒体链接
            shapeData.Add("links", CreateLinksForCompany(companyId, fields));
            return Ok(shapeData);
        }

        /// <summary>
        /// 添加公司资源
        /// </summary>
        /// <returns></returns>
        [HttpPost("company", Name = nameof(CreateCompany))]
        //自定义Content-Type的Media Type,这个有bug,未研究...
        [RequestHeaderMatchesMediaType("Content-Type", "application/json",
            "application/vnd.company.companyforcreation+json")]
        //消耗Media Type
        [Consumes("application/json", "application/vnd.company.companyforcreation+json")]
        public async Task<ActionResult<CompanyDto>> CreateCompany(CompanyAddDto companyAddDto)
        {
            //转换
            var company = _mapper.Map<Company>(companyAddDto);
            company.Id = Guid.NewGuid();
            //添加
            await _companyService.AddEntity(company);
            //转换
            var companyDto = _mapper.Map<CompanyDto>(company);
            //返回状态码201,跳转路由名称为GetCompany
            return CreatedAtRoute(nameof(GetCompany), new
            {
                companyId = company.Id,
                fields = string.Empty
            }, companyDto); ;
        }

        /// <summary>
        /// 批量添加公司资源
        /// </summary>
        /// <param name="companyAddDto">company集合</param>
        /// <returns></returns>
        [HttpPost("companies", Name = nameof(CreateCompanies))]
        public async Task<ActionResult<CompanyDto>> CreateCompanies(IEnumerable<CompanyAddDto> companyAddDto)
        {
            //转换
            var companies = _mapper.Map<IEnumerable<Company>>(companyAddDto);
            //批量添加
            foreach (var item in companies)
            {
                item.Id = Guid.NewGuid();
                //添加
                await _companyService.AddEntity(item);
            }
            //转换
            var companiesDto = _mapper.Map<IEnumerable<CompanyDto>>(companies);
            //获取添加公司id
            var idsString = string.Join(',', companiesDto.Select(x => x.Id));

            return CreatedAtRoute(nameof(GetCompanyIds), new
            {
                ids = idsString
            }, companiesDto);
        }

        /// <summary>
        /// 批量查询公司
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        [HttpGet("companies/({ids})", Name = nameof(GetCompanyIds))]
        public async Task<IActionResult> GetCompanyIds([FromRoute][ModelBinder(BinderType = typeof(ArrayModelBinder))] IEnumerable<Guid> ids)
        {
            if (ids == null)
            {
                return BadRequest();
            }
            var companies = await _companyService.QueryWhere(p => ids.Contains(p.Id)).ToListAsync();
            if (ids.Count() != companies.Count())
            {
                return NotFound();
            }
            var companiesDto = _mapper.Map<IEnumerable<CompanyDto>>(companies);
            return Ok(companiesDto);
        }

        /// <summary>
        /// 父子资源删除,种子数据中有主外键关联
        /// </summary>
        /// <param name="companyId"></param>
        /// <returns></returns>
        [HttpDelete("companies/{companyId}", Name = nameof(DeleteCompany))]
        public async Task<IActionResult> DeleteCompany(Guid companyId)
        {

            if (!await _companyService.QueryWhere(x => x.Id == companyId).AnyAsync())
            {
                return NotFound();
            }
            var companyEntity = await _companyService.QueryWhere(x => x.Id == companyId).FirstOrDefaultAsync();
            await _companyService.DeleteEntityAsync(companyEntity);
            return NoContent();
        }

        /// <summary>
        /// OPTIONS获取webapi的通信选项信息
        /// </summary>
        /// <returns></returns>
        [HttpOptions("companies")]
        public IActionResult GetCompaniesOptions()
        {
            Response.Headers.Add("Allow", "GET,POST,HEAD,OPTIONS");
            return Ok();
        }


        /// <summary>
        /// 翻页
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="resource"></param>
        /// <returns></returns>
        private string ResoureUri(CompanyParameters parameters, ResourceUriType resource)
        {
            switch (resource)
            {
                //上一页
                case ResourceUriType.PreviousPage:
                    return Url.Link(nameof(GetCompanies), new
                    {
                        fields = parameters.fields,
                        orderBy = parameters.orderBy,
                        pageNumber = parameters.pageNumber - 1,
                        pageSize = parameters.pageSize,
                        companyName = parameters.CompanyName
                    });
                //下一页
                case ResourceUriType.NextPage:
                    return Url.Link(nameof(GetCompanies), new
                    {
                        fields = parameters.fields,
                        orderBy = parameters.orderBy,
                        pageNumber = parameters.pageNumber + 1,
                        pageSize = parameters.pageSize,
                        companyName = parameters.CompanyName
                    });
                //当前页
                default:
                    return Url.Link(nameof(GetCompanies), new
                    {
                        fields = parameters.fields,
                        orderBy = parameters.orderBy,
                        pageNumber = parameters.pageNumber,
                        pageSize = parameters.pageSize,
                        companyName = parameters.CompanyName
                    });
            }
        }

        /// <summary>
        /// 集合资源超媒体链接
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        private IEnumerable<LinkDto> CreateLinksForCompanies(string fields)
        {
            List<LinkDto> linkList = new List<LinkDto>();
            if (string.IsNullOrWhiteSpace(fields))
            {
                linkList.Add(new LinkDto(Url.Link(nameof(GetCompanies), new { }), "self", "GET"));
            }
            else
            {
                linkList.Add(new LinkDto(Url.Link(nameof(GetCompanies), new { fields }), "self", "GET"));
            }
            return linkList;
        }

        /// <summary>
        /// 单个资源超媒体链接
        /// </summary>
        /// <param name="companyId"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        private IEnumerable<LinkDto> CreateLinksForCompany(Guid? companyId, string fields)
        {
            List<LinkDto> linkList = new List<LinkDto>();
            if (string.IsNullOrWhiteSpace(fields))
            {
                linkList.Add(new LinkDto(Url.Link(nameof(GetCompany), new { companyId }), "self", "GET"));
            }
            else
            {
                linkList.Add(new LinkDto(Url.Link(nameof(GetCompany), new { fields }), "self", "GET"));
            }
            linkList.Add(new LinkDto(Url.Link(nameof(DeleteCompany), new { companyId }), "delete_company", "DELETE"));
            linkList.Add(new LinkDto(Url.Link(nameof(EmployeesController.GetEmployees), new { companyId }), "company_for_self_employees", "GET"));
            linkList.Add(new LinkDto(Url.Link(nameof(EmployeesController.CreateEmployee), new { companyId }), "company_for_create_employee", "POST"));
            return linkList;
        }
        /// <summary>
        /// 超媒体链接加入分页
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="hasPrevious"></param>
        /// <param name="hasNext"></param>
        /// <returns></returns>
        private IEnumerable<LinkDto> CreateLinksForCompany(CompanyParameters parameters, bool hasPrevious, bool hasNext)
        {
            var links = new List<LinkDto>();
            //当前页
            links.Add(new LinkDto(ResoureUri(parameters, ResourceUriType.CurrentPage), "self", "GET"));
            //上一页
            if (hasPrevious)
            {
                links.Add(new LinkDto(ResoureUri(parameters, ResourceUriType.PreviousPage), "previous_page", "GET"));
            }
            //下一页
            if (hasNext)
            {
                links.Add(new LinkDto(ResoureUri(parameters, ResourceUriType.NextPage), "next_page", "GET"));
            }
            return links;
        }

    }
}
