using Entity.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace IDal
{
    public interface ICompanyManager : IBaseRepository<Company>
    {

    }

    public interface IEmployeeManager : IBaseRepository<Employee>
    {

    }
}
