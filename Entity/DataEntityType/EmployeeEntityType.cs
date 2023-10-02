using Entity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Entity.DataEntityType
{
    public class EmployeeEntityType
    {
        public void Configure(EntityTypeBuilder<Employee> modelBuilder)
        {

            modelBuilder.HasKey(x => x.Id).HasName("PRIMARY");

            modelBuilder
              .Property(x => x.EmployeeNo)
              .IsRequired()
              .HasColumnType("varchar(10)")
              .HasMaxLength(10);

            modelBuilder
                    .Property(x => x.FirstName)
                    .IsRequired()
                    .HasColumnType("varchar(50)")
                    .HasMaxLength(50);

            modelBuilder
                    .Property(x => x.LastName)
                    .IsRequired()
                    .HasColumnType("varchar(50)")
                    .HasMaxLength(50);

            modelBuilder.HasOne(x => x.Company)
                    .WithMany(x => x.Employees)
                    .HasForeignKey(x => x.CompanyId)
                    .OnDelete(DeleteBehavior.Cascade); //Cascade级联删除,Restrict不允许级联删除

            modelBuilder.HasData(
                new Employee
                {
                    Id = Guid.Parse("4b501cb3-d168-4cc0-b375-48fb33f318a4"),
                    CompanyId = Guid.Parse("c0ba00d5-198b-49a3-90a0-3dcc764c57c9"),
                    DateOfBirth = new DateTime(1976, 1, 2),
                    EmployeeNo = "MSFT231",
                    FirstName = "Nick",
                    LastName = "Carter",
                    Gender = Gender.男
                },
                new Employee
                {
                    Id = Guid.Parse("7eaa532c-1be5-472c-a738-94fd26e5fad6"),
                    CompanyId = Guid.Parse("c0ba00d5-198b-49a3-90a0-3dcc764c57c9"),
                    DateOfBirth = new DateTime(1981, 12, 5),
                    EmployeeNo = "MSFT245",
                    FirstName = "Vince",
                    LastName = "Carter",
                    Gender = Gender.男
                },
                new Employee
                {
                    Id = Guid.Parse("72457e73-ea34-4e02-b575-8d384e82a481"),
                    CompanyId = Guid.Parse("750b9941-fb6d-4a83-9ee5-20a5ebda0d8a"),
                    DateOfBirth = new DateTime(1986, 11, 4),
                    EmployeeNo = "G003",
                    FirstName = "Mary",
                    LastName = "King",
                    Gender = Gender.女
                },
                new Employee
                {
                    Id = Guid.Parse("7644b71d-d74e-43e2-ac32-8cbadd7b1c3a"),
                    CompanyId = Guid.Parse("750b9941-fb6d-4a83-9ee5-20a5ebda0d8a"),
                    DateOfBirth = new DateTime(1977, 4, 6),
                    EmployeeNo = "G097",
                    FirstName = "Kevin",
                    LastName = "Richardson",
                    Gender = Gender.男
                },
                new Employee
                {
                    Id = Guid.Parse("679dfd33-32e4-4393-b061-f7abb8956f53"),
                    CompanyId = Guid.Parse("bd65e1ce-2c82-497f-ad51-25332adbe0c2"),
                    DateOfBirth = new DateTime(1967, 1, 24),
                    EmployeeNo = "A009",
                    FirstName = "卡",
                    LastName = "里",
                    Gender = Gender.女
                },
                new Employee
                {
                    Id = Guid.Parse("1861341e-b42b-410c-ae21-cf11f36fc574"),
                    CompanyId = Guid.Parse("bd65e1ce-2c82-497f-ad51-25332adbe0c2"),
                    DateOfBirth = new DateTime(1957, 3, 8),
                    EmployeeNo = "A404",
                    FirstName = "Not",
                    LastName = "Man",
                    Gender = Gender.男
                }, new Employee
                {
                    Id = Guid.Parse("1861341e-b42b-410c-ae21-cf11f36fc965"),
                    CompanyId = Guid.Parse("bbdee09c-089b-4d30-bece-44df59237100"),
                    DateOfBirth = new DateTime(1957, 3, 8),
                    EmployeeNo = "A403",
                    FirstName = "Not",
                    LastName = "Man",
                    Gender = Gender.男
                });
        }
    }
}
