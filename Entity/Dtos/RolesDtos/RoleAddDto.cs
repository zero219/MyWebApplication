using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Entity.Dtos.RolesDtos
{
    public class RoleAddDto
    {
        [Display(Name ="角色名称")]
        [Required]
        public string RoleName { get; set; }
    }
}
