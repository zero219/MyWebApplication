using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Entity.Dtos
{
    /// <summary>
    /// 注册
    /// </summary>
    public class RegisterDto
    {
        [Display(Name = "用户名")]
        [Required]
        public string UserName { get; set; }
        [Display(Name = "邮箱")]
        [Required]
        public string Email { get; set; }
        [Display(Name = "密码")]
        [Required]
        public string PassWord { get; set; }
        [Display(Name = "确认密码")]
        [Compare(nameof(PassWord), ErrorMessage = "密码不一致")]
        [Required]
        public string ConfirmPassWord { get; set; }
    }
}
