using Entity.Models.Seckill;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Entity.DataEntityType
{
    public class SeckillVoucherEntityType : IEntityTypeConfiguration<SeckillVoucher>
    {
        public void Configure(EntityTypeBuilder<SeckillVoucher> modelBuilder)
        {
            modelBuilder.HasKey(x => x.Id)
                  .HasName("PRIMARY");

            modelBuilder.Property(x => x.VoucherId)
                 .HasColumnType("bigint(20)")
                 .IsRequired()
                 .HasMaxLength(20);

            modelBuilder.Property(x => x.Stock)
                  .HasColumnType("int")
                  .IsRequired()
                  .HasMaxLength(8);

            modelBuilder.Property(x => x.BeginTime)
                  .HasColumnType("datetime")
                  .IsRequired();

            modelBuilder.Property(x => x.EndTime)
                  .HasColumnType("datetime")
                  .IsRequired();

            modelBuilder.Property(x => x.CreateTime)
                  .HasColumnType("datetime")
                  .IsRequired();

            modelBuilder.Property(x => x.UpdateTime)
                  .HasColumnType("datetime")
                  .IsRequired();

            modelBuilder.HasData(new SeckillVoucher
            {
                Id = 1,
                VoucherId = 1,
                Stock = 500,
                BeginTime = DateTime.Parse("2023-01-01"),
                EndTime = DateTime.Parse("2023-12-31"),
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now,
            },
               new SeckillVoucher
               {
                   Id = 2,
                   VoucherId = 2,
                   Stock = 100,
                   BeginTime = DateTime.Parse("2023-01-01"),
                   EndTime = DateTime.Parse("2023-12-31"),
                   CreateTime = DateTime.Now,
                   UpdateTime = DateTime.Now,
               });
        }
    }
}
