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
using IBll.IdentityService;

namespace Api.Controllers
{
    [Route("api/auth")]
    [ApiController]

    public class AuthenticateController : CustomBase<AuthenticateController>
    {
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

        private readonly IApplicationUserClaimService _applicationUserClaimService;
        public AuthenticateController(IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<ApplicationRole> roleManager,
            IApplicationUserClaimService applicationUserClaimService,
            ILogger<AuthenticateController> logger,
            IRedisCacheManager redisCacheManager) : base(logger, redisCacheManager)
        {
            _configuration = configuration;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _applicationUserClaimService = applicationUserClaimService;

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
            loginDto.UserName = "zero219",
            loginDto.PassWord = "zzc123",
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
            //claims.AddRange("Admin,System".Split(',').Select(x => new Claim(ClaimTypes.Role, x)));

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
        [Authorize(Roles = "Admin")]
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
        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpGet("menuList")]
        public async Task<IActionResult> MenuList()
        {
            string token = Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(token))
            {
                return NotFound("token为空");
            }
            token = token.Replace("Bearer ", null);
            var jwtHandler = new JwtSecurityTokenHandler();
            // token校验
            if (!jwtHandler.CanReadToken(token))
            {
                return BadRequest();
            }
            JwtSecurityToken jwtToken = jwtHandler.ReadJwtToken(token);
            var claims = jwtToken.Claims.ToDictionary(x => x.Type);
            var issuer = claims.Where(x => x.Key == "iss").FirstOrDefault().Value.Issuer;
            //获取用户
            var user = await _userManager.FindByNameAsync(issuer);
            if (user == null)
            {
                return NotFound("查询用户失败");
            }
            List<MenuDataListDto> menuDataListDtos = new List<MenuDataListDto>();
            //获取用户的claim
            var claimList = _applicationUserClaimService.LoadEntities(x => x.UserId == user.Id)
                .OrderBy(x => x.ParentClaimId)
                .ToList()
                .GroupBy(x => new { x.ParentClaimId, x.ParentClaim });

            foreach (var claim in claimList)
            {
                MenuDataListDto menuDataListDto = new MenuDataListDto
                {
                    Id = claim.Key.ParentClaimId,
                    Name = claim.Key.ParentClaim,
                    Children = new List<Children>()
                };
                foreach (var item in claim)
                {
                    var children = new Children()
                    {
                        Id = item.Id,
                        Name = item.ClaimValue,
                        Path = "/" + item.ClaimType
                    };
                    menuDataListDto.Children.Add(children);
                }
                menuDataListDtos.Add(menuDataListDto);
            }
            return Ok(menuDataListDtos);
        }
    }
}
