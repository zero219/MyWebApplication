using Entity.Models;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IBll
{
    public interface ISignService : IBaseService<Sign>
    {
        Task<bool> Sign(long userId);

        Task<long> SignCountAsync(long userId);
    }
}
