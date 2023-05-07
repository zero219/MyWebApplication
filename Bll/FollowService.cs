using Common.Redis;
using Entity.Models;
using IBll;
using IDal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Bll
{
    public class FollowService : BaseService<Follow>, IFollowService
    {
        private static IFollowManager _followManager;

        private readonly IRedisCacheManager _redisCacheManager;
        public FollowService(IFollowManager followManager,
            IRedisCacheManager redisCacheManager) : base(followManager)
        {
            _followManager = followManager;
            _redisCacheManager = redisCacheManager;
        }

        public async Task<string> IsFollowAsync(long followUserId, bool isFollow)
        {
            var result = string.Empty;
            var key = string.Format("{0}:{1}", CacheKeys.FOLLOWS_KEY, "jerry");
            if (isFollow)
            {
                result = await _redisCacheManager.SetAddAsync(key, followUserId.ToString()) ? "关注成功" : "您已成功关注,不需要再重复关注了";
            }
            else
            {
                result = await _redisCacheManager.SetRemoveAsync(key, followUserId.ToString()) ? "取消关注成功" : "您已取消了关注";
            }
            return result;
        }
    }
}
