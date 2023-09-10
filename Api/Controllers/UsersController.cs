using Entity.Dtos.ClaimsDto;
using Entity.Dtos.UsersDtos;
using Entity.Models.IdentityModels;
using IBll.IdentityService;
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
        private readonly IApplicationUserClaimService _applicationUserClaimService;
        public UsersController(UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IApplicationUserClaimService applicationUserClaimService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _applicationUserClaimService = applicationUserClaimService;
        }

        /// <summary>
        /// 获取所有用户
        /// </summary>
        /// <returns></returns>
        [HttpGet("users", Name = nameof(GetUsers))]
        public async Task<IActionResult> GetUsers([FromQuery] string userName)
        {
            if (!string.IsNullOrWhiteSpace(userName))
            {
                var user = await _userManager.FindByNameAsync(userName);
                return Ok(user);
            }
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
                var ph = new PasswordHasher<ApplicationUser>();
                var user = new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = userDto.UserName,
                    PhoneNumber = userDto.PhoneNum,
                    Email = userDto.Email
                };
                user.PasswordHash = ph.HashPassword(user, userDto.PassWord);
                // 添加
                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(result);
                }
                return CreatedAtRoute(nameof(GetUser),
                    new { userId = user.Id },
                    new ApplicationUser() { UserName = userDto.UserName, Email = userDto.Email });

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
                if (user == null)
                {
                    return BadRequest();
                }
                user.UserName = userDto.UserName;
                user.Email = userDto.Email;
                user.PhoneNumber = userDto.PhoneNum;
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(result);
                }
                return CreatedAtRoute(nameof(GetUser),
                    new { userId = user.Id },
                    new ApplicationUser() { UserName = userDto.UserName, Email = userDto.Email });
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
                if (user.UserName.ToLower() == "zero219")
                {
                    return NoContent();
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
        /// 权限树
        /// </summary>
        /// <returns></returns>
        [HttpGet("claimsTree", Name = nameof(GetClaimsTree))]
        public async Task<IActionResult> GetClaimsTree()
        {
            //查询当前用户
            var user = await _userManager.FindByIdAsync("ae5d8653-0ce7-4d72-984b-4658dbdac654");
            if (user == null)
            {
                return BadRequest();
            }
            //获取用户的claim
            var claimList = _applicationUserClaimService.LoadEntities(x => x.UserId == user.Id)
                .OrderBy(x => x.ParentClaimId)
                .ToList()
                .GroupBy(x => new { x.ParentClaimId, x.ParentClaim });
            List<ClaimsTreeDto> claimsTreeDtos = new List<ClaimsTreeDto>();
            foreach (var claim in claimList)
            {
                ClaimsTreeDto claimsTreeDto = new ClaimsTreeDto
                {
                    Id = claim.Key.ParentClaimId,
                    Label = claim.Key.ParentClaim,
                    Children = new List<Children>()
                };
                foreach (var item in claim)
                {
                    var children = new Children()
                    {
                        Id = item.Id,
                        Label = item.ClaimValue
                    };
                    claimsTreeDto.Children.Add(children);
                }
                claimsTreeDtos.Add(claimsTreeDto);
            }

            return Ok(claimsTreeDtos);
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
