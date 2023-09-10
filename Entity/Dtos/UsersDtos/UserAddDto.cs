using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Entity.Dtos.UsersDtos
{
    public class UserAddDto
    {
        [Display(Name = "用户名")]
        [Required]
        public string UserName { get; set; }
        [Display(Name = "邮箱")]
        [Required]
        public string Email { get; set; }
        [Display(Name = "手机")]
        [Required]
        public string PhoneNum { get; set; }
        [Display(Name = "密码")]
        [Required]
        public string PassWord { get; set; }
        
    }
}
