using Entity.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Linq.Dynamic.Core.Tokenizer;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Common.Jwt
{
    public static class JwtHelper
    {
        /// <summary>
        /// 生成accessToken
        /// </summary>
        /// <param name="issuer"></param>
        /// <param name="audience"></param>
        /// <param name="secret"></param>
        /// <param name="claimList"></param>
        /// <returns></returns>
        public static string GenerateAccessToken(string issuer, string audience, string secret, List<Claim> claimList)
        {
            if (string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience) || string.IsNullOrEmpty(secret))
            {
                return "请填写所需的参数";
            }
            var time = DateTime.Now;
            var nowTime = new DateTimeOffset(time).ToUnixTimeSeconds();
            var expiraTime = new DateTimeOffset(time.AddDays(7)).ToUnixTimeSeconds();
            var newclaimList = new List<Claim>
            {
                //sub (subject)：主题
                new Claim(JwtRegisteredClaimNames.Sub,"Jwt验证"),
                //jti (JWT ID)：编号,jwt的唯一身份标识，主要用来作为一次性token,从而回避重放攻击
                new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
                //iat (Issued At)：jwt的签发时间
                new Claim(JwtRegisteredClaimNames.Iat,$"{nowTime}"),
                //nbf (Not Before)：生效时间
                new Claim(JwtRegisteredClaimNames.Nbf,$"{nowTime}") ,
                //exp (expiration time)：过期时间，这个过期时间必须要大于签发时间,过期时间1000秒
                new Claim (JwtRegisteredClaimNames.Exp,$"{expiraTime}"),
                //iss (issuer)：签发人
                new Claim(JwtRegisteredClaimNames.Iss,issuer),
                //aud (audience)：受众,接收jwt的一方
                new Claim(JwtRegisteredClaimNames.Aud,audience),
            };
            // 不为空就添加
            if (claimList != null && claimList.Count > 0)
            {
                newclaimList.AddRange(claimList);
            }
            // 加密算法
            var sig = SecurityAlgorithms.HmacSha256;
            //秘钥
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            //身份验证
            var creds = new SigningCredentials(key, sig);
            //创建jwtToken
            var jwtToken = new JwtSecurityToken(claims: newclaimList, signingCredentials: creds);
            //token转token字符串
            var tokenStr = new JwtSecurityTokenHandler().WriteToken(jwtToken);

            return tokenStr;
        }

        /// <summary>
        ///  生成refreshToken
        /// </summary>
        /// <returns></returns>
        public static string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        /// <summary>
        /// 验证accessToken
        /// </summary>
        /// <param name="token">accessToken</param>
        /// <returns></returns>
        public static ClaimsPrincipal GetPrincipalFromExpiredToken(string token, string issuer, string audience, string secret)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience) || string.IsNullOrEmpty(secret))
            {
                return null;
            }
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret)),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = issuer, // 替换为实际的发行者
                    ValidAudience = audience, // 替换为实际的受众
                    ValidateLifetime = false // 允许过期的 Token
                }, out var securityToken);

                if (!(securityToken is JwtSecurityToken jwtSecurityToken) ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }
                return principal;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// 校验accessToken是否过期
        /// </summary>
        /// <param name="token"></param>
        /// <param name="issuer"></param>
        /// <param name="audience"></param>
        /// <param name="secret"></param>
        /// <returns></returns>
        public static ClaimsPrincipal VerifyToken(string token, string issuer, string audience, string secret)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience) || string.IsNullOrEmpty(secret))
            {
                return null;
            }
            try
            {
                //创建 JwtSecurityTokenHandler 实例, 该实例用于读取和验证 JWT
                var jwtHandler = new JwtSecurityTokenHandler();
                // 读取 JWT
                JwtSecurityToken jwtToken = jwtHandler.ReadJwtToken(token);
                // 检查是否可以读取 Token
                if (!jwtHandler.CanReadToken(token))
                {
                    return null;
                }
                // 创建一个 TokenValidationParameters 对象，配置了 JWT 的验证参数，包括发行者验证、受众验证、生存期验证、签名密钥验证等。
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
                // 验证令牌
                var principal = jwtHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                // 检查用户是否已认证
                if (!principal.Identity.IsAuthenticated)
                {
                    return null;
                }
                return principal;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
