using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Entity.Dtos.SeckillVoucherDto
{
    public class SeckillVoucherDto
    {
        [Display(Name = "券ID")]
        [Required(ErrorMessage = "{0}不能为空")]
        public long VoucherId { get; set; }

        [Display(Name = "员工号")]
        [Required(ErrorMessage = "{0}不能为空")]
        public long UserId { get; set; }
    }
}
