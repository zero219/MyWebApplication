using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Entity.Models.IdentityModels
{
    public class ApplicationUserToken : IdentityUserToken<string>
    {
        public virtual ApplicationUser User { get; set; }

        /// <summary>
        /// 过期时间
        /// </summary>
        public long? Expires { get; set; }
    }
}
