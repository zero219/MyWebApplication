using Entity.Dtos.UsersDtos;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Entity.Dtos.RolesDtos
{
    public class RoleAddDto
    {
        [Display(Name = "角色名称")]
        [Required]
        public string RoleName { get; set; }
        [Display(Name = "正式名称")]
        [Required]
        public string NormalizedName { get; set; }
    }

    public class UserRolesDto
    {
        public ICollection<RoleTreeDto> Roles { get; set; }
    }

    public class RoleClaimsDto
    {
        public ICollection<ClaimsChildren> Claims { get; set; }
    }
}
