using Entity.Dtos.RolesDtos;
using Entity.Dtos.UsersDtos;
using Entity.Models.IdentityModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
namespace Api.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "管理员", Policy = "角色管理")]
    public class RolesController : ControllerBase
    {
        private readonly RoleManager<ApplicationRole> _roleManager;

        public RolesController(RoleManager<ApplicationRole> roleManager)
        {
            _roleManager = roleManager;
        }

        /// <summary>
        /// 查询角色
        /// </summary>
        /// <returns></returns>
        [HttpGet("roles", Name = nameof(GetRoles))]
        public async Task<IActionResult> GetRoles()
        {
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
        /// 查询单个角色
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        [HttpGet("roles/{roleId}", Name = nameof(GetRole))]
        public async Task<IActionResult> GetRole(string roleId)
        {
            var roles = await _roleManager.FindByIdAsync(roleId);
            return Ok(roles);
        }

        /// <summary>
        /// 创建角色
        /// </summary>
        /// <param name="roleDto"></param>
        /// <returns></returns>
        [HttpPost("role", Name = nameof(CreateRole))]
        public async Task<IActionResult> CreateRole([FromBody] RoleAddDto roleDto)
        {
            var role = new ApplicationRole
            {
                Id = Guid.NewGuid().ToString(),
                Name = roleDto.RoleName
            };
            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                return BadRequest();
            }
            return CreatedAtRoute(nameof(GetRole), role.Id);
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="roleDto"></param>
        /// <returns></returns>
        [HttpPut("role", Name = nameof(UpdateRole))]
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
            return CreatedAtRoute(nameof(GetRole), role.Id);
        }

        /// <summary>
        /// 删除角色
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        [HttpDelete("roles/{roleId}")]
        public async Task<IActionResult> DeleteRole(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null)
            {
                return NotFound();
            }
            await _roleManager.DeleteAsync(role);
            return NoContent();
        }

    }
}
