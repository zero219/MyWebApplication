using System;
using System.Collections.Generic;
using System.Text;

namespace Entity.Dtos.UsersDtos
{
    public class UserAddToClaimDto
    {
        public string UserId { get; set; }
        public ICollection<ClaimsList> ClaimsList { get; set; }
       
    }

    public class ClaimsList
    {
        public string ClaimValue { get; set; }
        public string ClaimType { get; set; }
    }
}
