using Entity.Data;
using Entity.Models.IdentityModels;
using IDal.IdentityManager;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dal.IdentityManager
{
    public class ApplicationUserClaimManager : BaseRepository<ApplicationUserClaim>, IApplicationUserClaimManager
    {
        public ApplicationUserClaimManager(RoutineDbContext context) : base(context)
        {
        }
    }
}
