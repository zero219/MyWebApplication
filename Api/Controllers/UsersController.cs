using Entity.Dtos.UsersDtos;
using Entity.Models.IdentityModels;
using Marvin.Cache.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "管理员", Policy = "用户管理")]
    [Authorize(Policy = "自定义用户管理")]
    [Route("api")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        public UsersController(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        /// <summary>
        /// 获取所有用户
        /// </summary>
        /// <returns></returns>
        [HttpGet("users", Name = nameof(GetUsers))]
        public async Task<IActionResult> GetUsers()
        {
            var usersList = await _userManager.Users.ToListAsync();
            return Ok(usersList);
        }

        /// <summary>
        /// 获取单个用户
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("users/{userId}", Name = nameof(GetUser))]
        public async Task<IActionResult> GetUser(string userId)
        {
            var usersList = await _userManager.FindByIdAsync(userId);
            return Ok(usersList);
        }

        /// <summary>
        /// 添加用户
        /// </summary>
        /// <param name="userDto"></param>
        /// <returns></returns>
        [HttpPost("user", Name = nameof(CreateUser))]
        public async Task<IActionResult> CreateUser([FromBody] UserAddDto userDto)
        {
            try
            {
                var user = new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = userDto.UserName,
                    Email = userDto.Email
                };
                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(result);
                }
                return CreatedAtRoute(nameof(GetUser), user.Id);

            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="userDto"></param>
        /// <returns></returns>
        [HttpPut("user", Name = nameof(UpdateUser))]
        public async Task<IActionResult> UpdateUser([FromBody] UserUpdateDto userDto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userDto.UserId);
                if (string.IsNullOrEmpty(user.Id))
                {
                    return BadRequest();
                }
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(result);
                }
                return CreatedAtRoute(nameof(GetUser), user.Id);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound();
                }
                await _userManager.DeleteAsync(user);
                return NoContent();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        /// <summary>
        /// 查询用户的角色
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("users/{userId}/roles", Name = nameof(UserToRoles))]
        public async Task<IActionResult> UserToRoles(string userId)
        {
            //查询当前用户
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return BadRequest();
            }
            var roles = await _userManager.GetRolesAsync(user);
            return Ok(roles);
        }

        /// <summary>
        /// 用户添加角色
        /// </summary>
        /// <param name="userId">用户Id</param>
        /// <param name="RoleNames">角色名</param>
        /// <returns></returns>
        [HttpPost("users/{userId}/roles", Name = nameof(UserAddToRoles))]
        public async Task<IActionResult> UserAddToRoles(string userId, IEnumerable<string> RoleNames)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return BadRequest();
                }
                //为用户添加没有使用的角色
                var result = await _userManager.AddToRolesAsync(user, RoleNames);
                if (!result.Succeeded)
                {
                    return BadRequest();
                }
                var roles = await _userManager.GetRolesAsync(user);
                return CreatedAtRoute(nameof(UserToRoles), new { user.Id }, roles);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        /// <summary>
        /// 加载用户是否有claims
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("users/{userId}/claims", Name = nameof(UserToClaims))]
        public async Task<IActionResult> UserToClaims([FromRoute] string userId)
        {
            //查询当前用户
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return BadRequest();
            }
            var claims = await _userManager.GetClaimsAsync(user);
            return Ok(claims);
        }

        /// <summary>
        /// 添加Claims
        /// </summary>
        /// <param name="userAddToClaimDto"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpPost("users/claims", Name = nameof(UserAddToClaims))]
        public async Task<IActionResult> UserAddToClaims([FromBody] UserAddToClaimDto userAddToClaimDto)
        {
            try
            {
                //查询当前用户
                var user = await _userManager.FindByIdAsync(userAddToClaimDto.UserId);
                if (user == null)
                {
                    return BadRequest();
                }
                foreach (var claim in userAddToClaimDto.ClaimsList)
                {
                    var applicationClaim = new ApplicationUserClaim()
                    {
                        ClaimType = claim.ClaimType,
                        ClaimValue = claim.ClaimValue,
                    };
                    user.Claims.Add(applicationClaim);
                    await _userManager.UpdateAsync(user);
                }
                var claims = await _userManager.GetClaimsAsync(user);
                return CreatedAtRoute(nameof(UserToClaims), new { userId = user.Id }, claims);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
    }
}
