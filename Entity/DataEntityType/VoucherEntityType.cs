using Entity.Models.Seckill;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Entity.DataEntityType
{
    internal class VoucherEntityType : IEntityTypeConfiguration<Voucher>
    {
        public void Configure(EntityTypeBuilder<Voucher> modelBuilder)
        {
            modelBuilder.HasKey(b => b.Id).HasName("PRIMARY");

            modelBuilder.Property(x => x.ShopId)
             .HasColumnType("bigint(20)")
             .IsRequired()
             .HasMaxLength(20);

            modelBuilder.Property(x => x.Title)
             .HasColumnType("varchar(255)")
             .IsRequired()
             .HasMaxLength(255);

            modelBuilder.Property(x => x.SubTitle)
             .HasColumnType("varchar(255)")
             .IsRequired()
             .HasMaxLength(255);

            modelBuilder.Property(x => x.Rules)
             .HasColumnType("varchar(1024)")
             .IsRequired()
             .HasMaxLength(1024);

            modelBuilder.Property(x => x.PayValue)
             .HasColumnType("bigint(10)")
             .IsRequired()
             .HasMaxLength(10);

            modelBuilder.Property(x => x.ActualValue)
             .HasColumnType("bigint(10)")
             .IsRequired()
             .HasMaxLength(20);

            modelBuilder
             .Property(x => x.Type)
             .HasColumnType("tinyint(1)")
             .IsRequired()
             .HasMaxLength(1);

            modelBuilder.Property(x => x.Status)
             .HasColumnType("tinyint(1)")
             .IsRequired()
             .HasMaxLength(1);

            modelBuilder
             .Property(x => x.CreateTime)
             .HasColumnType("datetime")
             .IsRequired();

            modelBuilder.Property(x => x.UpdateTime)
             .HasColumnType("datetime")
             .IsRequired();

            modelBuilder.HasData(new Voucher
            {
                Id = 1,
                ShopId = 1,
                Title = "50元代金券",
                SubTitle = "周一至周日均可使用",
                Rules = "全场通用\\n无需预约\\n可无限叠加\\不兑现、不找零\\n仅限堂食",
                PayValue = 5000,
                ActualValue = 4750,
                Type = 0,
                Status = 1,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now,
            },
            new Voucher
            {
                Id = 2,
                ShopId = 1,
                Title = "100元代金券",
                SubTitle = "周一至周日均可使用",
                Rules = "全场通用\\n无需预约\\n\\不兑现、不找零\\n仅限堂食",
                PayValue = 10000,
                ActualValue = 8000,
                Type = 1,
                Status = 1,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now,
            });
        }
    }
}
