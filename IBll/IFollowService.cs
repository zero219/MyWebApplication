using Entity.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IBll
{
    public interface IFollowService : IBaseService<Follow>
    {
        Task<string> IsFollowAsync(long followUserId, bool isFollow);
    }
}
