using Entity.Attributes;
using Entity.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Entity.Dtos.EmployeesDtos
{
    [Custom(ErrorMessage = "员工编号不能和名一样")]
    public class EmployeeAddDto : EmployeeShareDto
    {
       
    }
}
