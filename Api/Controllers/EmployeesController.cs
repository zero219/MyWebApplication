using Entity.Dtos;
using IBll;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Entity.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Authorization;
using Entity.Dtos.EmployeesDtos;

namespace Api.Controllers
{

    [ApiController]
    [Route("api")]
    //使用Identity框架的多角色验证是，中间件用的并不是jwt验证，这里必须使用jwt的Bearer验证
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Authorize(Policy = "SystemAndAdmin")]
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;
        private readonly ICompanyService _companyService;
        private readonly IMapper _mapper;
        public EmployeesController(IEmployeeService employeeService, ICompanyService companyService, IMapper mapper)
        {
            _employeeService = employeeService;
            _companyService = companyService;
            _mapper = mapper;
        }
        /// <summary>
        /// 获取公司员工
        /// </summary>
        /// <param name="companyId">公司ID</param>
        /// <returns></returns>
        [HttpGet("companies/{companyId}/employees", Name = nameof(GetEmployees))]
        //框架自带的响应缓存,但会被ETAG覆盖,然并卵.
        [ResponseCache(Duration = 60)]
        public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetEmployees(Guid companyId)
        {
            if (!await _employeeService.QueryEmployees(p => p.CompanyId == companyId).AnyAsync())
            {
                return NotFound("查询无此数据");
            }
            var employees = await _employeeService.QueryEmployees(p => p.CompanyId == companyId).ToListAsync();
            var employeesDto = _mapper.Map<IEnumerable<EmployeeDto>>(employees);
            return Ok(employeesDto);
        }

        /// <summary>
        /// 获取公司某个员工
        /// </summary>
        /// <param name="companyId">公司ID</param>
        /// <param name="employeeId">员工ID</param>
        /// <returns></returns>
        [HttpGet("companies/{companyId}/employees/{employeeId}", Name = nameof(GetEmployee))]
        //框架自带的响应缓存,但会被ETAG覆盖,然并卵.
        [ResponseCache(CacheProfileName = "CacheProfileKey")]
        public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetEmployee(Guid companyId, Guid employeeId)
        {
            if (!await _employeeService.QueryEmployees(p => p.CompanyId == companyId && p.Id == employeeId).AnyAsync())
            {
                return NotFound("查询无此数据");
            }
            var employee = await _employeeService.QueryEmployees(p => p.CompanyId == companyId && p.Id == employeeId).ToListAsync();
            var employeeDto = _mapper.Map<IEnumerable<EmployeeDto>>(employee);
            return Ok(employeeDto);

        }
        /// <summary>
        /// 创建员工资源
        /// </summary>
        /// <param name="companyId">公司id</param>
        /// <param name="employeeAddDto">添加的数据</param>
        /// <returns></returns>
        [HttpPost("companies/{companyId}/employees", Name = nameof(CreateEmployee))]
        public async Task<ActionResult<Employee>> CreateEmployee(Guid companyId, EmployeeAddDto employeeAddDto)
        {
            //判断该公司是否存在
            if (!await _employeeService.QueryEmployees(p => p.CompanyId == companyId).AnyAsync())
            {
                return NotFound("查询无此数据");
            }
            var employee = _mapper.Map<Employee>(employeeAddDto);
            employee.Id = Guid.NewGuid();
            employee.CompanyId = companyId;
            await _employeeService.AddEntity(employee);
            var employeeDto = _mapper.Map<EmployeeDto>(employee);
            return CreatedAtRoute(nameof(GetEmployee), new
            {
                companyId = employee.CompanyId,
                employeeId = employeeDto.Id
            }, employeeDto);
        }

        /// <summary>
        /// 更新员工的全部字段
        /// </summary>
        /// <param name="companyId"></param>
        /// <param name="employeeId"></param>
        /// <param name="employeeUpdateDto"></param>
        /// <returns></returns>
        [HttpPut("companies/{companyId}/employees/{employeeId}")]
        public async Task<ActionResult> UpdateEmployee(Guid companyId, Guid employeeId, EmployeeUpdateDto employeeUpdateDto)
        {

            if (!await _employeeService.QueryEmployees(p => p.CompanyId == companyId && p.Id == employeeId).AnyAsync())
            {
                return NotFound("查询无此数据");
            }
            var employeeEntity = await _employeeService.QueryEmployees(p => p.CompanyId == companyId && p.Id == employeeId).FirstOrDefaultAsync();
            //1.employeeEntity映射employeeUpdateDto
            //2.传进来的参数更新到employeeUpdateDto,
            //3.employeeUpdateDto映射回employeeEntity
            _mapper.Map(employeeUpdateDto, employeeEntity);

            await _employeeService.EditEntityAsync(employeeEntity);

            return NoContent();
        }

        /// <summary>
        /// 部分更新
        /// </summary>
        /// <param name="companyId"></param>
        /// <param name="employeeId"></param>
        /// <param name="jsonPatchDocument"></param>
        /// <returns></returns>
        [HttpPatch("companies/{companyId}/employees/{employeeId}")]
        public async Task<ActionResult> PartialUpdateEmployee(Guid companyId, Guid employeeId, [FromBody] JsonPatchDocument<EmployeeUpdateDto> jsonPatchDocument)
        {
            if (!await _employeeService.QueryEmployees(p => p.CompanyId == companyId && p.Id == employeeId).AnyAsync())
            {
                return NotFound("查询无此数据");
            }
            var employee = await _employeeService.QueryEmployees(x => x.Id == employeeId).FirstOrDefaultAsync();
            var pathEmployee = _mapper.Map<EmployeeUpdateDto>(employee);
            //添加json补丁
            jsonPatchDocument.ApplyTo(pathEmployee, ModelState);
            //验证model
            if (!TryValidateModel(ModelState))
            {
                return new UnprocessableEntityObjectResult(ModelState);
            }
            _mapper.Map(pathEmployee, employee);
            await _employeeService.EditEntityAsync(employee);
            return NoContent();
        }

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <param name="companyId"></param>
        /// <param name="employeeId"></param>
        /// <returns></returns>
        [HttpDelete("companies/{companyId}/employees/{employeeId}")]
        public async Task<ActionResult> DeleteEmployee(Guid companyId, Guid employeeId)
        {
            if (!await _employeeService.QueryEmployees(p => p.CompanyId == companyId && p.Id == employeeId).AnyAsync())
            {
                return NotFound("查询无此数据");
            }
            var employee = await _employeeService.QueryEmployees(x => x.Id == employeeId).FirstOrDefaultAsync();
            await _employeeService.DeleteEntityAsync(employee);
            return NoContent();
        }
    }
}

