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

        private List<ClaimsData> claimsList = new List<ClaimsData>() {
          new ClaimsData  { Id=1, ParentClaimId=1, ParentClaim="用户管理", ClaimType ="Users", ClaimValue="用户列表" },
          new ClaimsData  { Id=2, ParentClaimId=2, ParentClaim="角色管理", ClaimType ="Roles", ClaimValue="角色列表" },
          new ClaimsData  { Id=3, ParentClaimId=3, ParentClaim="员工管理", ClaimType ="Companies", ClaimValue="公司列表" },
          new ClaimsData  { Id=4, ParentClaimId=3, ParentClaim="员工管理", ClaimType ="Employees", ClaimValue="员工列表" },
        };

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

            #region header
            var sig = SecurityAlgorithms.HmacSha256;
            #endregion

            #region payload
            //创建一个身份认证
            var claimsList = new List<Claim>
            {
                //sub (subject)：主题
                new Claim(JwtRegisteredClaimNames.Sub,"Jwt验证"),
                //jti (JWT ID)：编号,jwt的唯一身份标识，主要用来作为一次性token,从而回避重放攻击
                new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
                //iat (Issued At)：jwt的签发时间
                new Claim(JwtRegisteredClaimNames.Iat,$"{new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds()}"),
                //nbf (Not Before)：生效时间
                new Claim(JwtRegisteredClaimNames.Nbf,$"{new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds()}") ,
                //exp (expiration time)：过期时间，这个过期时间必须要大于签发时间,过期时间1000秒
                new Claim (JwtRegisteredClaimNames.Exp,$"{new DateTimeOffset(DateTime.Now.AddSeconds(1800)).ToUnixTimeSeconds()}"),
                //iss (issuer)：签发人
                new Claim(JwtRegisteredClaimNames.Iss,issuer),
                //aud (audience)：受众,接收jwt的一方
                new Claim(JwtRegisteredClaimNames.Aud,audience),
            };
            //获取用户
            var user = await _userManager.FindByNameAsync(loginDto.UserName);
            var userName = new Claim(ClaimTypes.Name, user.UserName);
            claimsList.Add(userName);
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
            #endregion

            #region signiture
            //秘钥
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            //身份验证
            var creds = new SigningCredentials(key, sig);
            //创建jwtToken
            var jwtToken = new JwtSecurityToken(
                claims: claimsList,
                signingCredentials: creds);
            //token转token字符串
            var tokenStr = new JwtSecurityTokenHandler().WriteToken(jwtToken);
            #endregion

            return Ok("Bearer " + tokenStr);
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
                var jwtHandler = new JwtSecurityTokenHandler();
                JwtSecurityToken jwtToken = jwtHandler.ReadJwtToken(token);
                // token校验
                if (!jwtHandler.CanReadToken(token))
                {
                    return BadRequest("令牌不正确");
                }
                //jwt签名
                string secret = _configuration["JwtTokenManagement:Secret"];
                //颁发者
                string issuer = _configuration["JwtTokenManagement:Issuer"];
                //接收者
                string audience = _configuration["JwtTokenManagement:Audience"];
                // 验证令牌
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer, // 替换为令牌中使用的发行者
                    ValidAudience = audience, // 替换为令牌中使用的受众
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
                };
                var principal = jwtHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                if (!principal.Identity.IsAuthenticated)
                {
                    return BadRequest("验证失败");
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

                var roleClaimList = claimsList.Where(x => roleClaimTypes.Contains(x.ClaimType)).ToList();
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
    }
}
