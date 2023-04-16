using Entity.Models.Seckill;
using IBll;
using IDal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bll
{
    public class VoucherService : BaseService<Voucher>, IVoucherService
    {
        private readonly IVoucherManager _voucherManager;
        public VoucherService(IVoucherManager voucherManager) : base(voucherManager)
        {
            _voucherManager = voucherManager;
        }
    }
}
