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

namespace Api.Controllers
{
    [Route("api/auth")]
    [ApiController]
    
    public class AuthenticateController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public AuthenticateController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        /// <summary>
        /// jwtToken
        /// </summary>
        /// <returns></returns>
        [HttpGet("token")]
        [AllowAnonymous]
        public IActionResult Token()
        {
            //用户
            var tokenModel = new
            {
                //用户id
                Uid = 1,
                //角色名
                Role = "Admin,System",
                //职能
                Duty = "All"
            };
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
            var claims = new List<Claim>
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
            claims.AddRange(tokenModel.Role.Split(',').Select(x => new Claim(ClaimTypes.Role, x)));
            #endregion

            #region signiture
            //秘钥
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            //身份验证
            var creds = new SigningCredentials(key, sig);
            //创建jwtToken
            var jwtToken = new JwtSecurityToken(
                claims: claims,
                signingCredentials: creds);
            //token转token字符串
            var tokenStr = new JwtSecurityTokenHandler().WriteToken(jwtToken);
            #endregion

            return Ok("Bearer " + tokenStr);
        }
    }
}
