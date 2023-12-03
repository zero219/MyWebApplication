using Entity.Dtos.ClaimsDto;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Entity.Dtos.UsersDtos
{
    public class UserAddToClaimDto
    {
        public ICollection<ClaimsChildren> Claims { get; set; }
    }
    
    public class ClaimsChildren
    {
        /// <summary>
        /// Id
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Label { get; set; }
    }

    public class MenuData
    {
        public List<ClaimsData> ClaimsData { get; set; }
    }

    public class ClaimsData
    {
        public int? Id { get; set; }
        public int? ParentClaimId { get; set; }
        public string ParentClaim { get; set; }
        public string ClaimType { get; set; }
        public string ClaimValue { get; set; }
       
    }
}
