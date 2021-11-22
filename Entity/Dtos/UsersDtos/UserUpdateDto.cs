﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Entity.Dtos.UsersDtos
{
    public class UserUpdateDto
    {
        [Display(Name = "Id")]
        [Required]
        public string UserId { get; set; }
        [Display(Name = "用户名")]
        [Required]
        public string UserName { get; set; }
        [Display(Name = "邮箱")]
        [Required]
        public string Email { get; set; }

    }
}
