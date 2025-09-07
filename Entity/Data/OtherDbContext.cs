using Entity.DataEntityType;
using Entity.Models;
using Entity.Models.Seckill;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Entity.Data
{
    public class OtherDbContext : DbContext
    {
        public OtherDbContext(DbContextOptions<OtherDbContext> options) : base(options)
        {

        }
        public DbSet<SeckillVoucher> SeckillVouchers { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            new SeckillVoucherEntityType().Configure(modelBuilder.Entity<SeckillVoucher>());
        }
    }
}
