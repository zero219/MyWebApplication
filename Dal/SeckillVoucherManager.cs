using Entity.Data;
using Entity.Models.Seckill;
using IDal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dal
{
    public class SeckillVoucherManager : BaseRepository<SeckillVoucher>, ISeckillVoucherManager
    {
        public SeckillVoucherManager(RoutineDbContext context) : base(context)
        {

        }
    }
}
