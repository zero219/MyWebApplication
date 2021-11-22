using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Entity.Dtos
{
    /// <summary>
    /// 登录
    /// </summary>
    public class LoginDto
    {
        [Display(Name = "邮箱")]
        [Required]
        public string UserName { get; set; }
        [Display(Name = "密码")]
        [Required]
        public string PassWord { get; set; }
    }
}
