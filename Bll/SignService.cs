using Common.Redis;
using Entity.Models;
using IBll;
using IDal;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Bll
{
    public class SignService : BaseService<Sign>, ISignService
    {
        private readonly ISignManager _signManager;
        private readonly IRedisCacheManager _redisCacheManager;
        public SignService(ISignManager signManager, IRedisCacheManager redisCacheManager) : base(signManager)
        {
            _signManager = signManager;
            _redisCacheManager = redisCacheManager;
        }

        /// <summary>
        /// 签到
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<bool> Sign(long userId)
        {
            DateTime dateTime = DateTime.Now;
            var day = DateTime.Now.Day;
            string key = string.Format("{0}:{1}:{2}", CacheKeys.SIGN_KEY, userId, dateTime.ToString("yyyyMM"));
            var result = await _redisCacheManager.StringSetBitAsync(key, 3, true);
            return result;
        }

        public async Task<long> SignCountAsync(long userId)
        {
            DateTime dateTime = DateTime.Now;
            var day = DateTime.Now.Day;
            string key = string.Format("{0}:{1}:{2}:{3}", CacheKeys.REDIS_DATA_KEY, CacheKeys.SIGN_KEY, userId, dateTime.ToString("yyyyMM"));
            var result = await _redisCacheManager.ExecuteAsync("BITFIELD", key, "GET", $"u{day}", "#0");

            int continuousDays = CountContinuousSignInDays(long.Parse(((RedisValue[])result)[0].ToString()), 6);
            return continuousDays;
        }
        private int CountContinuousSignInDays(long signInBitmap, int days)
        {
            // 将签到情况转换为二进制字符串
            string binaryStr = Convert.ToString(signInBitmap, 2).PadLeft(days, '0');

            // 统计连续签到天数
            int maxContinuousDays = 0;
            int currentContinuousDays = 0;
            for (int i = binaryStr.Length - 1; i >= 0; i--)
            {
                if (binaryStr[i] == '1')
                {
                    currentContinuousDays++;
                    if (currentContinuousDays > maxContinuousDays)
                    {
                        maxContinuousDays = currentContinuousDays;
                    }
                }
                else
                {
                    currentContinuousDays = 0;
                }
            }

            return maxContinuousDays;
        }


    }
}
