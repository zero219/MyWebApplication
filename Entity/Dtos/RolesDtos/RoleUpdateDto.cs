using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Entity.Dtos.RolesDtos
{
    public class RoleUpdateDto
    {
        [Display(Name = "角色Id")]
        [Required]
        public string RoleId { get; set; }

        [Display(Name = "角色名称")]
        [Required]
        public string RoleName { get; set; }
    }
}
