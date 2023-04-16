using Entity.Data;
using Entity.Models.Seckill;
using IDal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dal
{
    public class VoucherOrderManager : BaseRepository<VoucherOrder>, IVoucherOrderManager
    {
        public VoucherOrderManager(RoutineDbContext context) : base(context)
        {
        }
    }
}
