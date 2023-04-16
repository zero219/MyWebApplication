using Entity.Data;
using Entity.Models.Seckill;
using IDal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dal
{
    public class VoucherManager : BaseRepository<Voucher>, IVoucherManager
    {
        public VoucherManager(RoutineDbContext context) : base(context)
        {

        }
    }
}
