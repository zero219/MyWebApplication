using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Entity.Dtos
{
    public class CompanyAddDto
    {
        /// <summary>
        /// 公司名称
        /// </summary>
        [Display(Name = "公司名称")]
        [Required(ErrorMessage = "{0}不能为空")]
        [MaxLength(20, ErrorMessage = "{0}的长度不超过{1}")]
        public string CompanyName { get; set; }
        /// <summary>
        /// 公司介绍
        /// </summary>
        [Display(Name = "公司简介")]
        [StringLength(500, MinimumLength = 1, ErrorMessage = "{0}的范围从{2}到{1}")]
        public string Introduction { get; set; }

        /// <summary>
        /// 添加员工,ICollection提供了同步处理、赋值及返回内含元素数目的功能
        /// </summary>
        public ICollection<EmployeeAddDto> Employees { get; set; } = new List<EmployeeAddDto>();
    }
}
