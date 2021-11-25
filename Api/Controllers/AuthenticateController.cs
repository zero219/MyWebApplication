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

namespace Api.Controllers
{
    [Route("api/auth")]
    [ApiController]

    public class AuthenticateController : ControllerBase
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
        public AuthenticateController(IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _configuration = configuration;
            _userManager = userManager;
            _signInManager = signInManager;
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
            //获取用户
            var user = await _userManager.FindByNameAsync(loginDto.UserName);
            //获取角色
            var roles = await _userManager.GetRolesAsync(user);
            //获取claims
            var claims = await _userManager.GetClaimsAsync(user);
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
            //多个角色
            claimsList.AddRange(roles.Select(s => new Claim(ClaimTypes.Role, s)));
            //claims.AddRange("Admin,System".Split(',').Select(x => new Claim(ClaimTypes.Role, x)));
            claimsList.AddRange(claims.Select(s => new Claim(s.Type, s.Value)));
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

    }
}
