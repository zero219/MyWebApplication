using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.IdentityAuth
{
    public class ClaimsRequirement : IAuthorizationRequirement
    {
        public string Claim { get; set; }

        public ClaimsRequirement(string claim)
        {
            Claim = claim;
        }
    }

    public class ClaimsHandler : AuthorizationHandler<ClaimsRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ClaimsRequirement requirement)
        {
            var claim = context.User.Claims.Where(x => x.Type.Contains(requirement.Claim)).FirstOrDefault();

            if (claim != null)
            {
                if (claim.Type.EndsWith(requirement.Claim))
                {
                    context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }
}
