using Entity.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Entity.Models.IdentityModels;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Entity.Models.Seckill;
using Entity.DataEntityType;

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
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<SeckillVoucher> SeckillVouchers { get; set; }
        public DbSet<VoucherOrder> VoucherOrders { get; set; }
        public DbSet<Follow> Follows { get; set; }
        public DbSet<Sign> Signs { get; set; }

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

            modelBuilder.Entity<ApplicationUser>()
             .Property(x => x.Id)
             .IsRequired()
             .HasColumnType("varchar(50)")
             .HasMaxLength(50);

            modelBuilder.Entity<ApplicationUser>()
             .Property(x => x.UserName)
             .IsRequired()
             .HasColumnType("varchar(50)")
             .HasMaxLength(50);

            modelBuilder.Entity<ApplicationUser>()
             .Property(x => x.NormalizedUserName)
             .IsRequired()
             .HasColumnType("varchar(50)")
             .HasMaxLength(50);

            modelBuilder.Entity<ApplicationUser>()
             .Property(x => x.Email)
             .IsRequired()
             .HasColumnType("varchar(50)")
             .HasMaxLength(50);

            modelBuilder.Entity<ApplicationUser>()
             .Property(x => x.NormalizedEmail)
             .IsRequired()
             .HasColumnType("varchar(50)")
             .HasMaxLength(50);

            modelBuilder.Entity<ApplicationUser>()
             .Property(x => x.EmailConfirmed)
             .IsRequired()
             .HasColumnType("tinyint(1)")
             .HasMaxLength(1);

            modelBuilder.Entity<ApplicationUser>()
             .Property(x => x.PasswordHash)
             .IsRequired()
             .HasColumnType("varchar(50)")
             .HasMaxLength(50);

            modelBuilder.Entity<ApplicationUser>()
             .Property(x => x.SecurityStamp)
             .IsRequired()
             .HasColumnType("varchar(50)")
             .HasMaxLength(50);

            modelBuilder.Entity<ApplicationUser>()
             .Property(x => x.ConcurrencyStamp)
             .IsRequired()
             .HasColumnType("varchar(50)")
             .HasMaxLength(50);

            modelBuilder.Entity<ApplicationUser>()
             .Property(x => x.PhoneNumber)
             .IsRequired()
             .HasColumnType("varchar(50)")
             .HasMaxLength(50);

            modelBuilder.Entity<ApplicationUser>()
            .Property(x => x.PhoneNumberConfirmed)
            .IsRequired()
            .HasColumnType("tinyint(1)")
            .HasMaxLength(1);

            modelBuilder.Entity<ApplicationUser>()
            .Property(x => x.TwoFactorEnabled)
            .IsRequired()
            .HasColumnType("tinyint(1)")
            .HasMaxLength(1);

            modelBuilder.Entity<ApplicationUser>()
             .Property(x => x.LockoutEnabled)
             .IsRequired()
             .HasColumnType("tinyint(1)")
             .HasMaxLength(1);

            modelBuilder.Entity<ApplicationUser>()
            .Property(x => x.LockoutEnd)
            .HasColumnType("datetime");

            modelBuilder.Entity<ApplicationUser>()
            .Property(x => x.AccessFailedCount)
            .IsRequired()
            .HasColumnType("integer");

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

            modelBuilder.Entity<ApplicationRole>()
             .Property(x => x.Id)
             .IsRequired()
             .HasColumnType("varchar(50)")
             .HasMaxLength(50);

            modelBuilder.Entity<ApplicationRole>()
             .Property(x => x.Name)
             .IsRequired()
             .HasColumnType("varchar(50)")
             .HasMaxLength(50);

            modelBuilder.Entity<ApplicationRole>()
             .Property(x => x.NormalizedName)
             .IsRequired()
             .HasColumnType("varchar(50)")
             .HasMaxLength(50);

            modelBuilder.Entity<ApplicationRole>()
             .Property(x => x.ConcurrencyStamp)
             .IsRequired()
             .HasColumnType("varchar(50)")
             .HasMaxLength(50);

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

            modelBuilder.Entity<ApplicationUserClaim>()
              .Property(x => x.Id)
              .IsRequired()
              .HasColumnType("integer");

            modelBuilder.Entity<ApplicationUserClaim>()
              .Property(x => x.ParentClaimId)
              .HasColumnType("integer");

            modelBuilder.Entity<ApplicationUserClaim>()
              .Property(x => x.ParentClaim)
              .IsRequired()
              .HasColumnType("varchar(50)")
              .HasMaxLength(50);

            modelBuilder.Entity<ApplicationUserClaim>()
              .Property(x => x.ClaimType)
              .IsRequired()
              .HasColumnType("varchar(50)")
              .HasMaxLength(50);

            modelBuilder.Entity<ApplicationUserClaim>()
              .Property(x => x.ClaimValue)
              .IsRequired()
              .HasColumnType("varchar(50)")
              .HasMaxLength(50);

            modelBuilder.Entity<ApplicationUserClaim>()
             .Property(x => x.UserId)
             .IsRequired()
             .HasColumnType("varchar(50)")
             .HasMaxLength(50);

            modelBuilder.Entity<ApplicationUserClaim>().HasData(new ApplicationUserClaim
            {
                Id = 1,
                ParentClaimId = 1,
                ParentClaim = "用户管理",
                ClaimType = "Users",
                ClaimValue = "用户列表",
                UserId = "ae5d8653-0ce7-4d72-984b-4658dbdac654",
            },
            new ApplicationUserClaim
            {
                Id = 2,
                ParentClaimId = 2,
                ParentClaim = "角色管理",
                ClaimType = "Roles",
                ClaimValue = "角色列表",
                UserId = "ae5d8653-0ce7-4d72-984b-4658dbdac654",
            },
            new ApplicationUserClaim
            {
                Id = 3,
                ParentClaimId = 3,
                ParentClaim = "员工管理",
                ClaimType = "Companies",
                ClaimValue = "公司列表",
                UserId = "ae5d8653-0ce7-4d72-984b-4658dbdac654",
            },
            new ApplicationUserClaim
            {
                Id = 4,
                ParentClaimId = 3,
                ParentClaim = "员工管理",
                ClaimType = "Employees",
                ClaimValue = "员工列表",
                UserId = "ae5d8653-0ce7-4d72-984b-4658dbdac654",
            });
            #endregion

            #region ApplicationUserRole
            modelBuilder.Entity<ApplicationUserRole>()
            .Property(x => x.RoleId)
            .IsRequired()
            .HasColumnType("varchar(50)")
            .HasMaxLength(50);

            modelBuilder.Entity<ApplicationUserRole>()
             .Property(x => x.UserId)
             .IsRequired()
             .HasColumnType("varchar(50)")
             .HasMaxLength(50);
            //用户绑定角色
            modelBuilder.Entity<ApplicationUserRole>().HasData(new ApplicationUserRole
            {
                RoleId = "cd2fd2f8-c589-49c4-9159-bc470ae66c8d",
                UserId = "ae5d8653-0ce7-4d72-984b-4658dbdac654"
            });
            #endregion

            #region ApplicationUserRole

            modelBuilder.Entity<ApplicationRoleClaim>()
                .Property(x => x.Id)
                .IsRequired()
                .HasColumnType("integer");

            modelBuilder.Entity<ApplicationRoleClaim>()
              .Property(x => x.RoleId)
              .IsRequired()
              .HasColumnType("varchar(50)")
              .HasMaxLength(50);

            modelBuilder.Entity<ApplicationRoleClaim>()
              .Property(x => x.ClaimType)
              .IsRequired()
              .HasColumnType("varchar(50)")
              .HasMaxLength(50);

            modelBuilder.Entity<ApplicationRoleClaim>()
              .Property(x => x.ClaimValue)
              .IsRequired()
              .HasColumnType("varchar(50)")
              .HasMaxLength(50);

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

            #region ApplicationUserToken
            modelBuilder.Entity<ApplicationUserToken>()
                .Property(x => x.UserId)
                .IsRequired()
                .HasColumnType("varchar(50)")
                .HasMaxLength(50);

            modelBuilder.Entity<ApplicationUserToken>()
                .Property(x => x.LoginProvider)
                .IsRequired()
                .HasColumnType("varchar(20)")
                .HasMaxLength(20);

            modelBuilder.Entity<ApplicationUserToken>()
                .Property(x => x.Name)
                .IsRequired()
                .HasColumnType("varchar(50)")
                .HasMaxLength(50);

            modelBuilder.Entity<ApplicationUserToken>()
                .Property(x => x.Value)
                .IsRequired()
                .HasColumnType("varchar(50)")
                .HasMaxLength(500);

            modelBuilder.Entity<ApplicationUserToken>()
                .Property(x => x.Expires)
                .IsRequired()
                .HasColumnType("bigint(20)")
                .HasMaxLength(20);

            modelBuilder.Entity<ApplicationUserToken>().HasData(new ApplicationUserToken
            {
                UserId = "ae5d8653-0ce7-4d72-984b-4658dbdac654",
                LoginProvider = "localhost",
                Name = "RefreshToken",
                Value = "A8Bhd0QaJSY45V6CAAEo31/JO5/oC9atCXAHBJdKgjQ=",
                Expires = 0L
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

            #region 优惠券秒杀

            new VoucherEntityType().Configure(modelBuilder.Entity<Voucher>());
            new SeckillVoucherEntityType().Configure(modelBuilder.Entity<SeckillVoucher>());
            new VoucherOrderEntityType().Configure(modelBuilder.Entity<VoucherOrder>());

            #endregion
            //关注
            new FollowEntityType().Configure(modelBuilder.Entity<Follow>());
            // 签到
            new SignEntityType().Configure(modelBuilder.Entity<Sign>());
            // 
            new CompanyEntityType().Configure(modelBuilder.Entity<Company>());

            new EmployeeEntityType().Configure(modelBuilder.Entity<Employee>());
        }
    }
}
