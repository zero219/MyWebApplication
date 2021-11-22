using Entity.Attributes;
using Entity.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Entity.Dtos
{
    public abstract class EmployeeShareDto : IValidatableObject
    {
        [Display(Name = "员工号")]
        [Required(ErrorMessage = "{0}不能为空")]
        [StringLength(10, MinimumLength = 1, ErrorMessage = "{0}长度是{1}")]
        public string EmployeeNo { get; set; }

        [Display(Name = "名")]
        [Required(ErrorMessage = "{0}不能为空")]
        [MaxLength(50, ErrorMessage = "{0}长度不能超过{1}")]
        public string FirstName { get; set; }

        [Display(Name = "姓")]
        [Required(ErrorMessage = "{0}不能为空")]
        [MaxLength(50, ErrorMessage = "{0}长度不能超过{1}")]
        public string LastName { get; set; }

        [Display(Name = "性别")]
        public Gender Gender { get; set; }

        [Display(Name = "出生日期")]
        public DateTime DateOfBirth { get; set; }

        /// <summary>
        /// 属性级别的自定义验证
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (FirstName == LastName)
            {
                yield return new ValidationResult("姓和名称不能一样", new[] { nameof(FirstName), nameof(LastName) });
            }
        }
    }
}
