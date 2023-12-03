using Entity.Data;
using Entity.Dtos.ClaimsDto;
using Entity.Dtos.RolesDtos;
using Entity.Dtos.UsersDtos;
using Entity.Models.IdentityModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace Api.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "管理员", Policy = "角色管理")]
    public class RolesController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly RoutineDbContext _dbContext;

        public RolesController(IConfiguration configuration, RoleManager<ApplicationRole> roleManager, RoutineDbContext dbContext)
        {
            _configuration = configuration;
            _roleManager = roleManager;
            _dbContext = dbContext;
        }
        private List<ClaimsData> GetClaimsData()
        {
            var claimsData = _configuration.GetSection("MenuData").Get<MenuData>()?.ClaimsData;
            return claimsData ?? new List<ClaimsData>();
        }
        /// <summary>
        /// 查询角色
        /// </summary>
        /// <returns></returns>
        [HttpGet("roles", Name = nameof(GetRoles))]
        public async Task<IActionResult> GetRoles([FromQuery] string roleName)
        {
            if (!string.IsNullOrWhiteSpace(roleName))
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                return Ok(role);
            }
            var roles = await _roleManager.Roles.ToListAsync();
            return Ok(roles);
        }

        /// <summary>
        /// 角色树
        /// </summary>
        /// <returns></returns>
        [HttpGet("rolesTree")]
        public async Task<IActionResult> GetRolesTree()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            var rolesTree = roles.Select(x => new RoleTreeDto()
            {
                Id = x.Id,
                Label = x.Name
            }).ToList();
            return Ok(rolesTree);
        }

        /// <summary>
        /// 查询角色权限
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        [HttpGet("roles/{roleId}/claims")]
        public async Task<IActionResult> GetRoleClaims(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                return BadRequest();
            }
            var roleClaims = await _dbContext.RoleClaims.Where(c => c.RoleId == role.Id).ToListAsync();
            var roleClaimsTree = roleClaims.Select(rc => new
            {
                Id = GetClaimsData().Where(x => x.ClaimValue == rc.ClaimValue).FirstOrDefault().Id.Value,
                Label = rc.ClaimValue
            });
            return Ok(roleClaimsTree);
        }

        /// <summary>
        /// 创建角色
        /// </summary>
        /// <param name="roleDto"></param>
        /// <returns></returns>
        [HttpPost("roles", Name = nameof(CreateRole))]
        public async Task<IActionResult> CreateRole([FromBody] RoleAddDto roleDto)
        {
            var role = new ApplicationRole
            {
                Id = Guid.NewGuid().ToString(),
                Name = roleDto.RoleName,
                NormalizedName = roleDto.NormalizedName.ToUpper(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };
            await _dbContext.Roles.AddAsync(role);
            var result = _dbContext.SaveChanges();
            if (result < 0)
            {
                return BadRequest();
            }
            return CreatedAtRoute(nameof(GetRoles), new { roleName = role.Name },
                    new ApplicationRole() { Name = roleDto.RoleName, NormalizedName = roleDto.NormalizedName });
        }

        /// <summary>
        /// 更新角色
        /// </summary>
        /// <param name="roleDto"></param>
        /// <returns></returns>
        [HttpPut("roles", Name = nameof(UpdateRole))]
        public async Task<IActionResult> UpdateRole([FromBody] RoleUpdateDto roleDto)
        {
            var role = await _roleManager.FindByIdAsync(roleDto.RoleId);
            if (role == null)
            {
                return BadRequest();
            }
            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
            {
                return BadRequest();
            }
            return StatusCode(StatusCodes.Status201Created);
        }

        /// <summary>
        /// 删除角色
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        [HttpDelete("roles/{roleId}")]
        public async Task<IActionResult> DeleteRole(string roleId)
        {
            try
            {
                var role = await _roleManager.FindByIdAsync(roleId);
                if (role == null)
                {
                    return NotFound();
                }
                await _roleManager.DeleteAsync(role);
                return NoContent();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }

        /// <summary>
        /// 保存角色权限
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="roleClaims"></param>
        /// <returns></returns>
        [HttpPost("roles/{roleId}/claims", Name = nameof(RolesClaims))]
        public async Task<IActionResult> RolesClaims(string roleId, RoleClaimsDto roleClaims)
        {
            try
            {
                var role = await _roleManager.FindByIdAsync(roleId);
                if (role == null)
                {
                    return BadRequest();
                }

                List<ApplicationRoleClaim> roleClaimsList = new List<ApplicationRoleClaim>();
                foreach (var claim in roleClaims.Claims)
                {
                    var claimsData = GetClaimsData().Where(x => x.ClaimValue == claim.Label).FirstOrDefault();
                    ApplicationRoleClaim application = new ApplicationRoleClaim();
                    application.RoleId = role.Id;
                    application.ClaimType = claimsData?.ClaimType;
                    application.ClaimValue = claimsData?.ClaimValue;
                    roleClaimsList.Add(application);
                }
                // 开始事务
                _dbContext.Database.BeginTransaction();
                var applicationRoleClaims = await _dbContext.RoleClaims.Where(x => x.RoleId == role.Id).ToListAsync();
                // 先删除后添加
                _dbContext.RoleClaims.RemoveRange(applicationRoleClaims);
                _dbContext.RoleClaims.AddRange(roleClaimsList);
                _dbContext.SaveChanges();
                // 提交事务
                _dbContext.Database.CommitTransaction();
                return StatusCode(StatusCodes.Status201Created);
            }
            catch (Exception ex)
            {
                // 回滚事务
                _dbContext.Database.RollbackTransaction();
                throw new Exception(ex.Message);
            }
        }
    }
}
