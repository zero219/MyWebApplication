using Entity.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Entity.Models.IdentityModels;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Entity.Data
{
    public class RoutineDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string,
        ApplicationUserClaim, ApplicationUserRole, ApplicationUserLogin,
        ApplicationRoleClaim, ApplicationUserToken>
    {

        public bool UseIntProperty { get; set; }

        public RoutineDbContext(DbContextOptions<RoutineDbContext> options) : base(options)
        {

        }


        public DbSet<Company> Companies { get; set; }
        public DbSet<Employee> Employees { get; set; }

        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) 
        //    => optionsBuilder
        //    .UseInMemoryDatabase("RoutineDbContext")
        //    .ReplaceService<IModelCacheKeyFactory, RoutineModelCacheKeyFactory>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region OnModelCreating
            /*
             * 使用Idetity时,更新IdentityUser
             * 与IdentityUserLogin、IdentityUserRole、IdentityUserClaim、IdentityUserToken表主键时，
             * 必须调用此方法
             */
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema("ApplicationDB");
            #endregion

            #region Company
            modelBuilder.Entity<Company>()
              .Property(x => x.Name)
              .IsRequired()
              .HasMaxLength(100);

            modelBuilder.Entity<Company>()
                .Property(x => x.Introduction)
                .HasMaxLength(500);

            modelBuilder.Entity<Company>()
                .Property(x => x.Country).HasMaxLength(50);

            modelBuilder.Entity<Company>()
                .Property(x => x.Industry).HasMaxLength(50);

            modelBuilder.Entity<Company>()
                .Property(x => x.Product).HasMaxLength(100);
            //种子数据
            modelBuilder.Entity<Company>().HasData(new Company
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
            #endregion

            #region Employee
            modelBuilder.Entity<Employee>()
              .Property(x => x.EmployeeNo)
              .IsRequired().HasMaxLength(10);

            modelBuilder.Entity<Employee>()
                .Property(x => x.FirstName)
                .IsRequired()
                .HasMaxLength(50);

            modelBuilder.Entity<Employee>()
                .Property(x => x.LastName)
                .IsRequired().HasMaxLength(50);

            modelBuilder
                .Entity<Employee>()
                .HasOne(x => x.Company)
                .WithMany(x => x.Employees)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Cascade); //Cascade级联删除,Restrict不允许级联删除

            modelBuilder.Entity<Employee>().HasData(
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
            #endregion

            #region ApplicationUser
            //主外键关联
            modelBuilder.Entity<ApplicationUser>(b =>
            {
                //更新用户和登录表外键
                b.HasMany(u => u.Logins).WithOne(e => e.User).HasForeignKey(ul => ul.UserId).IsRequired();

                //更新用户和角色外键
                b.HasMany(u => u.UserRoles).WithOne(e => e.User).HasForeignKey(ur => ur.UserId).IsRequired();

                //更新用户和权限外键
                b.HasMany(u => u.Claims).WithOne(e => e.User).HasForeignKey(uc => uc.UserId).IsRequired();

                //更新用户和Token表外键
                b.HasMany(u => u.Tokens).WithOne(e => e.User).HasForeignKey(ut => ut.UserId).IsRequired();
            });

            //添加用户数据
            ApplicationUser applicationUser = new ApplicationUser()
            {
                Id = "ae5d8653-0ce7-4d72-984b-4658dbdac654",
                UserName = "zero219",
                NormalizedUserName = "zero219".ToUpper(),
                Email = "123@qq.com",
                NormalizedEmail = "123@qq.com".ToLower(),
                EmailConfirmed = true,
                TwoFactorEnabled = false,
                PhoneNumber = "001",
                PhoneNumberConfirmed = false,
                SecurityStamp = "SFLV3VE5FQGPDFVMYEUIZSP6GNHWQBQ6"
            };
            var ph = new PasswordHasher<ApplicationUser>();
            applicationUser.PasswordHash = ph.HashPassword(applicationUser, "zzc123");
            modelBuilder.Entity<ApplicationUser>().HasData(applicationUser);

            #endregion

            #region ApplicationRole
            //添加角色数据
            modelBuilder.Entity<ApplicationRole>().HasData(new ApplicationRole
            {
                Id = "cd2fd2f8-c589-49c4-9159-bc470ae66c8d",
                Name = "管理员",
                NormalizedName = "Admin".ToUpper(),//规范名称
            });
            //主外键关联
            modelBuilder.Entity<ApplicationRole>(b =>
            {
                b.HasMany(e => e.UserRoles).WithOne(e => e.Role).HasForeignKey(ur => ur.RoleId).IsRequired();
                b.HasMany(e => e.RoleClaims).WithOne(e => e.Role).HasForeignKey(rc => rc.RoleId).IsRequired();
            });
            #endregion

            #region ApplicationClaims
            modelBuilder.Entity<ApplicationUserClaim>().HasData(new ApplicationUserClaim
            {
                Id = 1,
                ParentClaimId = 3,
                ParentClaim = "员工管理",
                ClaimType = "Companies",
                ClaimValue = "公司列表",
                UserId = "ae5d8653-0ce7-4d72-984b-4658dbdac654",
            },
            new ApplicationUserClaim
            {
                Id = 2,
                ParentClaimId = 3,
                ParentClaim = "员工管理",
                ClaimType = "Employees",
                ClaimValue = "员工列表",
                UserId = "ae5d8653-0ce7-4d72-984b-4658dbdac654",
            },
            new ApplicationUserClaim
            {
                Id = 3,
                ParentClaimId = 1,
                ParentClaim = "用户管理",
                ClaimType = "Users",
                ClaimValue = "用户列表",
                UserId = "ae5d8653-0ce7-4d72-984b-4658dbdac654",
            },
            new ApplicationUserClaim
            {
                Id = 4,
                ParentClaimId = 2,
                ParentClaim = "角色管理",
                ClaimType = "Roles",
                ClaimValue = "角色列表",
                UserId = "ae5d8653-0ce7-4d72-984b-4658dbdac654",
            });
            #endregion

            #region ApplicationUserRole
            //用户绑定角色
            modelBuilder.Entity<ApplicationUserRole>().HasData(new ApplicationUserRole
            {
                RoleId = "cd2fd2f8-c589-49c4-9159-bc470ae66c8d",
                UserId = "ae5d8653-0ce7-4d72-984b-4658dbdac654"
            });
            #endregion

            #region ApplicationUserRole
            modelBuilder.Entity<ApplicationRoleClaim>().HasData(new ApplicationRoleClaim
            {
                Id = 1,
                RoleId = "cd2fd2f8-c589-49c4-9159-bc470ae66c8d",
                ClaimType = "Companies",
                ClaimValue = "公司列表"
            },
            new ApplicationRoleClaim
            {
                Id = 2,
                RoleId = "cd2fd2f8-c589-49c4-9159-bc470ae66c8d",
                ClaimType = "Employees",
                ClaimValue = "员工列表"
            },
            new ApplicationRoleClaim
            {
                Id = 3,
                RoleId = "cd2fd2f8-c589-49c4-9159-bc470ae66c8d",
                ClaimType = "Users",
                ClaimValue = "用户列表"
            },
            new ApplicationRoleClaim
            {
                Id = 4,
                RoleId = "cd2fd2f8-c589-49c4-9159-bc470ae66c8d",
                ClaimType = "Roles",
                ClaimValue = "角色列表"
            });
            #endregion

            #region 修改表名
            //修改表名
            modelBuilder.Entity<ApplicationUser>().ToTable("ApplicationUsers");
            modelBuilder.Entity<ApplicationUserClaim>().ToTable("ApplicationUserClaims");
            modelBuilder.Entity<ApplicationUserToken>().ToTable("ApplicationUserTokens");
            modelBuilder.Entity<ApplicationUserLogin>().ToTable("ApplicationUserLogins");
            modelBuilder.Entity<ApplicationUserRole>().ToTable("ApplicationUserRoles");
            modelBuilder.Entity<ApplicationRole>().ToTable("ApplicationRoles");
            modelBuilder.Entity<ApplicationRoleClaim>().ToTable("ApplicationRoleClaims");
            #endregion
        }
    }
}
