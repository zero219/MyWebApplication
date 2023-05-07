using Entity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Entity.DataEntityType
{
    public class FollowEntityType : IEntityTypeConfiguration<Follow>
    {
        public void Configure(EntityTypeBuilder<Follow> modelBuilder)
        {
            modelBuilder.HasKey(x => x.Id)
                  .HasName("PRIMARY");

            modelBuilder.Property(x => x.UserId)
                 .HasColumnType("bigint(20)")
                 .IsRequired()
                 .HasMaxLength(20);

            modelBuilder.Property(x => x.FollowUserId)
                 .HasColumnType("bigint(20)")
                 .IsRequired()
                 .HasMaxLength(20);

            modelBuilder.Property(x => x.CreateTime)
                   .HasColumnType("datetime")
                   .IsRequired();
        }
    }
}
