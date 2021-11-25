using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Entity.Models.IdentityModels
{
    /// <summary>
    /// 继承IdentityUser
    /// </summary>
    public class ApplicationUser : IdentityUser<string>
    {
        public ApplicationUser()
        {
            //去掉virtual关键字，实例化防止添加claims时报错
            UserRoles = new List<ApplicationUserRole>();
            Logins = new List<ApplicationUserLogin>();
            Claims = new List<ApplicationUserClaim>();
            Tokens = new List<ApplicationUserToken>();
        }
        public ICollection<ApplicationUserRole> UserRoles { get; set; }
        public ICollection<ApplicationUserLogin> Logins { get; set; }
        public ICollection<ApplicationUserClaim> Claims { get; set; }
        public ICollection<ApplicationUserToken> Tokens { get; set; }
    }
}
