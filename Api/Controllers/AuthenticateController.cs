using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Entity.Dtos;
using Microsoft.AspNetCore.Identity;
using Entity.Models.IdentityModels;
using Common.Log4net;
using Newtonsoft.Json;
using log4net.Core;
using Microsoft.Extensions.Logging;
using Common.Redis;
using Microsoft.EntityFrameworkCore;
using Entity.Data;
using Entity.Dtos.UsersDtos;
using Microsoft.Extensions.Caching.Memory;
using Common.Jwt;
using System.Linq.Dynamic.Core.Tokenizer;

namespace Api.Controllers
{
    [Route("api/auth")]
    [ApiController]

    public class AuthenticateController : ControllerBase
    {
        private readonly IRedisCacheManager _redisCacheManager;

        private readonly ILogger<AuthenticateController> _logger;

        private readonly IConfiguration _configuration;
        /// <summary>
        /// 用户储存帮助类
        /// </summary>
        private readonly UserManager<ApplicationUser> _userManager;
        /// <summary>
        /// 用户登录帮助类
        /// </summary>
        private readonly SignInManager<ApplicationUser> _signInManager;

        private readonly RoleManager<ApplicationRole> _roleManager;

        private readonly RoutineDbContext _dbContext;

        public AuthenticateController(IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            RoutineDbContext dbContext,
            ILogger<AuthenticateController> logger,
            IRedisCacheManager redisCacheManager)
        {
            _logger = logger;
            _redisCacheManager = redisCacheManager;
            _configuration = configuration;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _dbContext = dbContext;

        }

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="registerDto"></param>
        /// <returns></returns>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = registerDto.UserName,
                Email = registerDto.Email
            };
            var result = await _userManager.CreateAsync(user, registerDto.PassWord);
            if (!result.Succeeded)
            {
                return BadRequest();
            }
            return Ok();
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="loginDto"></param>
        /// <returns></returns>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var loginResult = await _signInManager.PasswordSignInAsync(
                    loginDto.UserName,
                    loginDto.PassWord,
                    false,//是否保存cookie
                    false//3次是否锁定
            );
            //判断登录
            if (!loginResult.Succeeded)
            {
                return BadRequest();
            }
            //jwt签名
            string secret = _configuration["JwtTokenManagement:Secret"];
            //颁发者
            string issuer = _configuration["JwtTokenManagement:Issuer"];
            //接收者
            string audience = _configuration["JwtTokenManagement:Audience"];

            var user = await _userManager.FindByNameAsync(loginDto.UserName);
            if (user == null)
            {
                return BadRequest("查询不到用户");
            }

            var claim = GetUserInfo(user.Id).Result;

            var newAccessToken = JwtHelper.GenerateAccessToken(issuer, audience, secret, claim);

            var newRefreshToken = JwtHelper.GenerateRefreshToken();

            var applicationUserToken = await _dbContext.UserTokens.Where(x => x.UserId == user.Id).FirstOrDefaultAsync();
            // 过期时间戳
            var expires = DateTimeOffset.UtcNow.AddSeconds(1800).ToUnixTimeMilliseconds();
            if (applicationUserToken == null)
            {
                ApplicationUserToken userToken = new ApplicationUserToken()
                {
                    UserId = user.Id,
                    LoginProvider = "localhost",
                    Name = "RefreshToken",
                    Value = newRefreshToken,
                    Expires = expires,
                };
                await _dbContext.UserTokens.AddAsync(userToken);
            }
            else
            {
                applicationUserToken.Value = newRefreshToken;
                applicationUserToken.Expires = expires;
                _dbContext.UserTokens.Update(applicationUserToken);
            }
            await _dbContext.SaveChangesAsync();
            return Ok(new { accessToken = newAccessToken, refreshToken = newRefreshToken });
        }

        /// <summary>
        /// 刷新token
        /// </summary>
        /// <returns></returns>
        [HttpGet("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var accessToken = HttpContext.Request.Headers.Authorization.ToString();
            var refreshToken = HttpContext.Request.Headers["RefreshToken"].ToString();
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return Unauthorized(new { success = false, msg = "无法获取到AccessToken" });
            }
            // 去掉Bearer
            accessToken = accessToken.Replace("Bearer ", null);
            //jwt签名
            string secret = _configuration["JwtTokenManagement:Secret"];
            //颁发者
            string issuer = _configuration["JwtTokenManagement:Issuer"];
            //接收者
            string audience = _configuration["JwtTokenManagement:Audience"];

            // 验证过期的accessToken
            var principal = JwtHelper.GetPrincipalFromExpiredToken(accessToken, issuer, audience, secret);
            // 如果 Refresh Token 无效，返回错误响应
            if (principal == null)
            {
                return Unauthorized(new { success = false, msg = "无效的刷新令牌" });
            }
            // 查找用户标识声明
            var userIdClaim = principal.FindFirst(ClaimTypes.Name)?.Value;

            var user = await _userManager.FindByNameAsync(userIdClaim);

            if (user == null)
            {
                return Unauthorized(new { success = false, msg = "查询不到用户" });
            }
            // 查询token
            var applicationUserToken = await _dbContext.UserTokens.Where(x => x.UserId == user.Id && x.Value == refreshToken).FirstOrDefaultAsync();

            if (applicationUserToken == null)
            {
                return Unauthorized(new { success = false, msg = "查询不到令牌" });
            }

            // 验证 Refresh Token 的有效期
            if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > applicationUserToken.Expires)
            {
                return Unauthorized(new { success = false, msg = "刷新令牌已过期" });
            }

            // 如果 Refresh Token 有效，生成新的 Access Token 和 Refresh Token
            var newAccessToken = JwtHelper.GenerateAccessToken(issuer, audience, secret, GetUserInfo(user.Id).Result);

            var newRefreshToken = JwtHelper.GenerateRefreshToken();

            // 更新存储
            applicationUserToken.Value = newRefreshToken;
            _dbContext.UserTokens.Update(applicationUserToken);
            await _dbContext.SaveChangesAsync();

            return Ok(new { accessToken = newAccessToken, refreshToken = newRefreshToken });
        }

        /// <summary>
        /// 登出
        /// </summary>
        /// <returns></returns>
        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok();
        }

        /// <summary>
        /// 菜单列表
        /// </summary>
        /// <returns></returns>
        [HttpGet("menuList")]
        public async Task<IActionResult> MenuList()
        {
            try
            {
                string token = Request.Headers["Authorization"];
                if (string.IsNullOrEmpty(token))
                {
                    return NotFound("token为空");
                }
                // 去掉Bearer
                token = token.Replace("Bearer ", null);
                //jwt签名
                string secret = _configuration["JwtTokenManagement:Secret"];
                //颁发者
                string issuer = _configuration["JwtTokenManagement:Issuer"];
                //接收者
                string audience = _configuration["JwtTokenManagement:Audience"];

                var principal = JwtHelper.VerifyToken(token, issuer, audience, secret);
                if (principal == null)
                {
                    return Unauthorized("Token验证不通过");
                }
                // 获取用户名（ClaimTypes.Name）
                var userName = principal.FindFirst(ClaimTypes.Name);
                //获取用户
                var user = await _userManager.FindByNameAsync(userName?.Value);
                if (user == null)
                {
                    return NotFound("查询用户失败");
                }
                List<MenuDataListDto> menuDataListDtos = new List<MenuDataListDto>();
                //获取用户的claim
                var userClaimList = await _dbContext.UserClaims
                    .Where(x => x.UserId == user.Id)
                    .Select(x => new ClaimsData()
                    {
                        Id = x.Id,
                        ParentClaimId = x.ParentClaimId,
                        ParentClaim = x.ParentClaim,
                        ClaimType = x.ClaimType,
                        ClaimValue = x.ClaimValue,
                    })
                    .ToListAsync();

                //获取多个角色
                var rolesList = await _dbContext.UserRoles
                    .Where(x => x.UserId == user.Id)
                    .Select(x => x.RoleId).ToListAsync();

                // 根据角色获取权限
                var roleClaimTypes = await _dbContext.RoleClaims.
                    Where(x => rolesList.Contains(x.RoleId))
                    .Select(x => x.ClaimType).ToListAsync();

                var roleClaimList = GetClaimsData().Where(x => roleClaimTypes.Contains(x.ClaimType)).ToList();
                //合并权限
                userClaimList.AddRange(roleClaimList);
                // 先分组去重，在分组加载
                var claims = userClaimList.GroupBy(x => new
                {
                    x.ParentClaimId,
                    x.ParentClaim,
                    x.ClaimType,
                    x.ClaimValue
                }).Select(x => x.First())
                  .OrderBy(x => x.ParentClaimId)
                  .ToList()
                  .GroupBy(x => new { x.ParentClaimId, x.ParentClaim });

                foreach (var claim in claims)
                {
                    MenuDataListDto menuDataListDto = new MenuDataListDto
                    {
                        Id = claim.Key.ParentClaimId.Value,
                        Name = claim.Key.ParentClaim,
                        Children = new List<Children>()
                    };
                    foreach (var item in claim)
                    {
                        var children = new Children()
                        {
                            Id = item.Id.Value,
                            Name = item.ClaimValue,
                            Path = "/" + item.ClaimType
                        };
                        menuDataListDto.Children.Add(children);
                    }
                    menuDataListDtos.Add(menuDataListDto);
                }
                return Ok(menuDataListDtos);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
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

        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        private async Task<List<Claim>> GetUserInfo(string userId)
        {
            var claimsList = new List<Claim>();
            //获取用户
            var user = await _userManager.FindByIdAsync(userId);
            var userNameInfo = new Claim(ClaimTypes.Name, user.UserName);
            claimsList.Add(userNameInfo);
            //获取角色
            var roles = await _userManager.GetRolesAsync(user);
            //多个角色
            claimsList.AddRange(roles.Select(s => new Claim(ClaimTypes.Role, s)));
            List<Claim> claims = new List<Claim>();
            //获取用户的claims
            var userClaims = await _userManager.GetClaimsAsync(user);
            //获取角色的claims
            claims.AddRange(userClaims);
            foreach (var role in roles)
            {
                var applicationRole = _roleManager.Roles.Where(x => x.Name == role).FirstOrDefault();
                var roleClaims = await _roleManager.GetClaimsAsync(applicationRole);
                claims.AddRange(roleClaims);
            }
            var distinctClaims = claims.Where((x, i) => claims.FindIndex(f => f.Type == x.Type && f.Value == x.Value) == i).ToList();
            claimsList.AddRange(distinctClaims);
            return claimsList;
        }
    }
}
