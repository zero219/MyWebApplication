using Entity.Models.Seckill;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Entity.DataEntityType
{
    public class VoucherOrderEntityType : IEntityTypeConfiguration<VoucherOrder>
    {
        public void Configure(EntityTypeBuilder<VoucherOrder> modelBuilder)
        {
            modelBuilder.HasKey(x => x.Id)
             .HasName("PRIMARY");

            modelBuilder.Property(x => x.UserId)
             .HasColumnType("bigint(20)")
             .IsRequired()
             .HasMaxLength(20);

            modelBuilder.Property(x => x.VoucherId)
             .HasColumnType("bigint(20)")
             .IsRequired()
             .HasMaxLength(20);

            modelBuilder.Property(x => x.PayType)
             .HasColumnType("tinyint(1)")
             .IsRequired()
             .HasMaxLength(1);

            modelBuilder.Property(x => x.Status)
             .HasColumnType("tinyint(1)")
             .IsRequired()
             .HasMaxLength(1);

            modelBuilder.Property(x => x.CreateTime)
             .HasColumnType("datetime")
             .IsRequired();

            modelBuilder.Property(x => x.PayTime)
             .HasColumnType("datetime")
             .IsRequired();

            modelBuilder.Property(x => x.UseTime)
             .HasColumnType("datetime")
             .IsRequired();

            modelBuilder.Property(x => x.RefundTime)
             .HasColumnType("datetime")
             .IsRequired();

            modelBuilder.Property(x => x.UpdateTime)
             .HasColumnType("datetime")
             .IsRequired();
        }
    }
}
