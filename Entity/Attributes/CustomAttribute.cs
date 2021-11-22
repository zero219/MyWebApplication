using Entity.Dtos;
using Entity.Dtos.EmployeesDtos;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Entity.Attributes
{
    /// <summary>
    /// 自定义model验证,作用于类级别
    /// </summary>
    public class CustomAttribute : ValidationAttribute
    {
        /// <summary>
        /// 验证
        /// </summary>
        /// <param name="value">验证的值</param>
        /// <param name="validationContext">验证的上下文</param>
        /// <returns>验证实例</returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (validationContext.ObjectInstance.GetType() == typeof(EmployeeAddDto))
            {
                //获取验证的对象
                var addDto = (EmployeeAddDto)validationContext.ObjectInstance;

                if (addDto.EmployeeNo == addDto.FirstName)
                {
                    return new ValidationResult(ErrorMessage, new[] { nameof(EmployeeAddDto) });
                }
            }
            else
            {
                //验证对象集合
                var addDto = (ICollection<EmployeeAddDto>)validationContext.ObjectInstance;

                foreach (var item in addDto)
                {
                    if (item.EmployeeNo == item.FirstName)
                    {
                        return new ValidationResult(ErrorMessage, new[] { nameof(EmployeeAddDto) });
                    }
                }

            }
            return ValidationResult.Success;
        }
    }
}
