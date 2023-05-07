using Entity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Entity.DataEntityType
{
    public class SignEntityType : IEntityTypeConfiguration<Sign>
    {
        
        public void Configure(EntityTypeBuilder<Sign> modelBuilder)
        {
            modelBuilder.HasKey(x => x.Id)
                  .HasName("PRIMARY");

            modelBuilder.Property(x => x.UserId)
                 .HasColumnType("bigint(20)")
                 .IsRequired()
                 .HasMaxLength(20);

            modelBuilder.Property(x => x.Year)
                 .HasColumnType("int(4)")
                 .IsRequired()
                 .HasMaxLength(4);

            modelBuilder.Property(x => x.Year)
                 .HasColumnType("int(2)")
                 .IsRequired()
                 .HasMaxLength(2);

            modelBuilder.Property(x => x.Date)
                 .HasColumnType("int(2)")
                 .IsRequired()
                 .HasMaxLength(2);

            modelBuilder.Property(x => x.Date)
                 .HasColumnType("tinyint(1)")
                 .IsRequired()
                 .HasMaxLength(1);
        }
    }
}
