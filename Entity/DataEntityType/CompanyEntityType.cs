using Entity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Entity.DataEntityType
{
    public class CompanyEntityType
    {
        public void Configure(EntityTypeBuilder<Company> modelBuilder)
        {

            modelBuilder.HasKey(x => x.Id).HasName("PRIMARY");

            modelBuilder
              .Property(x => x.Name)
              .IsRequired()
              .HasColumnType("varchar(100)")
              .HasMaxLength(100);

            modelBuilder
                .Property(x => x.Introduction)
                .HasColumnType("varchar(500)")
                .HasMaxLength(500);

            modelBuilder
                .Property(x => x.Country)
                .HasColumnType("varchar(50)")
                .HasMaxLength(50);

            modelBuilder
                .Property(x => x.Industry)
                .HasColumnType("varchar(50)")
                .HasMaxLength(50);

            modelBuilder
                .Property(x => x.Product)
                .HasColumnType("varchar(100)")
                .HasMaxLength(100);
            //种子数据
            modelBuilder.HasData(
                new Company
                {
                    Id = Guid.Parse("c0ba00d5-198b-49a3-90a0-3dcc764c57c9"),
                    Name = "Microsoft",
                    Introduction = "Great Company",
                    Country = "USA",
                    Industry = "Software",
                    Product = "Software"
                },
               new Company
               {
                   Id = Guid.Parse("750b9941-fb6d-4a83-9ee5-20a5ebda0d8a"),
                   Name = "Goole",
                   Introduction = "Don't be evil",
                   Country = "USA",
                   Industry = "Internet",
                   Product = "Software"
               },
               new Company
               {
                   Id = Guid.Parse("bd65e1ce-2c82-497f-ad51-25332adbe0c2"),
                   Name = "Alibaba",
                   Introduction = "Fubao Company",
                   Country = "China",
                   Industry = "Internet",
                   Product = "Software"
               }, new Company
               {
                   Id = Guid.Parse("bbdee09c-089b-4d30-bece-44df59237100"),
                   Name = "Tencent",
                   Introduction = "From Shenzhen",
                   Country = "China",
                   Industry = "ECommerce",
                   Product = "Software"
               },
               new Company
               {
                   Id = Guid.Parse("6fb600c1-9011-4fd7-9234-881379716400"),
                   Name = "Baidu",
                   Introduction = "From Beijing",
                   Country = "China",
                   Industry = "Internet",
                   Product = "Software"
               },
               new Company
               {
                   Id = Guid.Parse("5efc910b-2f45-43df-afae-620d40542800"),
                   Name = "Adobe",
                   Introduction = "Photoshop?",
                   Country = "USA",
                   Industry = "Software",
                   Product = "Software"
               },
               new Company
               {
                   Id = Guid.Parse("bbdee09c-089b-4d30-bece-44df59237111"),
                   Name = "SpaceX",
                   Introduction = "Wow",
                   Country = "USA",
                   Industry = "Technology",
                   Product = "Rocket"
               },
               new Company
               {
                   Id = Guid.Parse("6fb600c1-9011-4fd7-9234-881379716411"),
                   Name = "AC Milan",
                   Introduction = "Football Club",
                   Country = "Italy",
                   Industry = "Football",
                   Product = "Football Match"
               },
               new Company
               {
                   Id = Guid.Parse("5efc910b-2f45-43df-afae-620d40542811"),
                   Name = "Suning",
                   Introduction = "From Jiangsu",
                   Country = "China",
                   Industry = "ECommerce",
                   Product = "Goods"
               },
               new Company
               {
                   Id = Guid.Parse("bbdee09c-089b-4d30-bece-44df59237122"),
                   Name = "Twitter",
                   Introduction = "Blocked",
                   Country = "USA",
                   Industry = "Internet",
                   Product = "Tweets"
               },
               new Company
               {
                   Id = Guid.Parse("6fb600c1-9011-4fd7-9234-881379716422"),
                   Name = "Youtube",
                   Introduction = "Blocked",
                   Country = "USA",
                   Industry = "Internet",
                   Product = "Videos"
               },
               new Company
               {
                   Id = Guid.Parse("5efc910b-2f45-43df-afae-620d40542822"),
                   Name = "360",
                   Introduction = "- -",
                   Country = "China",
                   Industry = "Security",
                   Product = "Security Product"
               },
               new Company
               {
                   Id = Guid.Parse("bbdee09c-089b-4d30-bece-44df59237133"),
                   Name = "Jingdong",
                   Introduction = "Brothers",
                   Country = "China",
                   Industry = "ECommerce",
                   Product = "Goods"
               },
               new Company
               {
                   Id = Guid.Parse("6fb600c1-9011-4fd7-9234-881379716433"),
                   Name = "NetEase",
                   Introduction = "Music?",
                   Country = "China",
                   Industry = "Internet",
                   Product = "Songs"
               },
               new Company
               {
                   Id = Guid.Parse("5efc910b-2f45-43df-afae-620d40542833"),
                   Name = "Amazon",
                   Introduction = "Store",
                   Country = "USA",
                   Industry = "ECommerce",
                   Product = "Books"
               },
               new Company
               {
                   Id = Guid.Parse("bbdee09c-089b-4d30-bece-44df59237144"),
                   Name = "AOL",
                   Introduction = "Not Exists?",
                   Country = "USA",
                   Industry = "Internet",
                   Product = "Website"
               },
               new Company
               {
                   Id = Guid.Parse("6fb600c1-9011-4fd7-9234-881379716444"),
                   Name = "Yahoo",
                   Introduction = "Who?",
                   Country = "USA",
                   Industry = "Internet",
                   Product = "Mail"
               },
               new Company
               {
                   Id = Guid.Parse("5efc910b-2f45-43df-afae-620d40542844"),
                   Name = "Firefox",
                   Introduction = "Is it a company?",
                   Country = "USA",
                   Industry = "Internet",
                   Product = "Browser"
               });
        }
    }
}
