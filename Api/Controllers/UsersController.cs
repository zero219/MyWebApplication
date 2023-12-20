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
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "管理员", Policy = "用户管理")]
    [Authorize(Policy = "自定义用户管理")]
    [Route("api")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly RoutineDbContext _dbContext;

        public UsersController(
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            RoutineDbContext dbContext)
        {
            _configuration = configuration;
            _userManager = userManager;
            _roleManager = roleManager;
            _dbContext = dbContext;
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
            var roleDetails = roles.Select(roleName => new
            {
                Id = _roleManager.Roles.Single(r => r.Name == roleName).Id,
                Label = roleName
            }).ToList();
            return Ok(roleDetails);
        }

        /// <summary>
        /// 用户添加角色
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="userRoles"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpPost("users/{userId}/roles", Name = nameof(UserAddToRoles))]
        public async Task<IActionResult> UserAddToRoles(string userId, UserRolesDto userRoles)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return BadRequest();
                }
                List<ApplicationUserRole> applications = new List<ApplicationUserRole>();
                foreach (var role in userRoles.Roles)
                {
                    ApplicationUserRole applicationUserRole = new ApplicationUserRole();
                    applicationUserRole.UserId = user.Id;
                    applicationUserRole.RoleId = role.Id;
                    applications.Add(applicationUserRole);
                }
                // 开始事务
                _dbContext.Database.BeginTransaction();
                var userRolesList = await _dbContext.UserRoles.Where(x => x.UserId == user.Id).ToListAsync();
                _dbContext.UserRoles.RemoveRange(userRolesList);
                _dbContext.UserRoles.AddRange(applications);
                _dbContext.SaveChanges();
                // 提交事务
                _dbContext.Database.CommitTransaction();
                return StatusCode(StatusCodes.Status201Created);
            }
            catch (Exception e)
            {
                // 回滚事务
                _dbContext.Database.RollbackTransaction();
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
            try
            {
                //查询当前用户
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return BadRequest();
                }
                var userClaims = _dbContext.UserClaims
                    .Where(uc => uc.UserId == userId)
                    .ToList()
                    .Select(uc => new
                    {
                        Id = GetClaimsData().Where(x => x.ClaimValue == uc.ClaimValue).FirstOrDefault().Id.Value,
                        Label = uc.ClaimValue
                    });
                return Ok(userClaims);
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// 权限树
        /// </summary>
        /// <returns></returns>
        [HttpGet("claimsTree", Name = nameof(GetClaimsTree))]
        public IActionResult GetClaimsTree()
        {
            var claimList = GetClaimsData().OrderBy(x => x.ParentClaimId).ToList()
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
        /// <param name="userId"></param>
        /// <param name="userAddToClaimDto"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpPost("users/{userId}/claims", Name = nameof(UserAddToClaims))]
        public async Task<IActionResult> UserAddToClaims(string userId, [FromBody] UserAddToClaimDto userAddToClaimDto)
        {
            try
            {
                //查询当前用户
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return BadRequest();
                }
                List<ApplicationUserClaim> userClaimsList = new List<ApplicationUserClaim>();
                foreach (var claim in userAddToClaimDto.Claims)
                {
                    var claimsData = GetClaimsData().Where(x => x.ClaimValue == claim.Label).FirstOrDefault();
                    var applicationUserClaim = new ApplicationUserClaim();
                    applicationUserClaim.ParentClaimId = claimsData?.ParentClaimId;
                    applicationUserClaim.ParentClaim = claimsData?.ParentClaim;
                    applicationUserClaim.UserId = user.Id;
                    applicationUserClaim.ClaimType = claimsData?.ClaimType.ToString();
                    applicationUserClaim.ClaimValue = claimsData?.ClaimValue.ToString();
                    userClaimsList.Add(applicationUserClaim);
                }
                // 开始事务
                _dbContext.Database.BeginTransaction();
                var userClaims = await _dbContext.UserClaims.Where(uc => uc.UserId == user.Id).ToListAsync();
                // 先删除后添加
                _dbContext.UserClaims.RemoveRange(userClaims);
                _dbContext.UserClaims.AddRange(userClaimsList);
                _dbContext.SaveChanges();
                // 提交事务
                _dbContext.Database.CommitTransaction();

                return StatusCode(StatusCodes.Status201Created);
            }
            catch (Exception e)
            {
                // 回滚事务
                _dbContext.Database.RollbackTransaction();
                throw new Exception(e.Message);
            }
        }

        /// <summary>
        /// 获取配置文件数据
        /// </summary>
        /// <returns></returns>
        private List<ClaimsData> GetClaimsData()
        {
            var claimsData = _configuration.GetSection("MenuData").Get<MenuData>()?.ClaimsData;
            return claimsData ?? new List<ClaimsData>();
        }
    }
}
