using Entity.Models.Seckill;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IBll
{
    public interface ISeckillVoucherService : IBaseService<SeckillVoucher>
    {
        Task TaskWorkAsync(CancellationToken stoppingToken);
        Task<string> SeckillVoucherAsync(long voucherId, long userId);
        Task<string> SeckillVouchersAsync(long voucherId, long userId);
        Task<string> SeckillVoucherStreamAsync(long voucherId, long userId);
    }
}
