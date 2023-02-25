using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace Entity.Models.IdentityModels
{
    public class ApplicationUserClaim : IdentityUserClaim<string>
    {
        public virtual ApplicationUser User { get; set; }

        /// <summary>
        /// 父节点ID
        /// </summary>
        public int ParentClaimId { get; set; }
        /// <summary>
        /// 父节点
        /// </summary>
        public string ParentClaim { get; set; }
    }
}
