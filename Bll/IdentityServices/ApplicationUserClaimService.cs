using Entity.Models.IdentityModels;
using IBll.IdentityService;
using IDal;
using IDal.IdentityManager;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bll.IdentityServices
{
    public class ApplicationUserClaimService : BaseService<ApplicationUserClaim>, IApplicationUserClaimService
    {
        private readonly IApplicationUserClaimManager _applicationUserClaimManager;
        public ApplicationUserClaimService(IApplicationUserClaimManager applicationUserClaimManager) : base(applicationUserClaimManager)
        {
            _applicationUserClaimManager = applicationUserClaimManager;
        }
    }
}
