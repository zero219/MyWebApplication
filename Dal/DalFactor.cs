using Entity.Data;
using Entity.Models;
using IDal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dal
{
    public class CompanyManager : BaseRepository<Company>, ICompanyManager
    {

        //base(context)就是调用父类的带有context参数的构造函数。
        public CompanyManager(RoutineDbContext context) : base(context)
        {

        }
    }

    public class EmployeeManager : BaseRepository<Employee>, IEmployeeManager
    {

        //base(context)就是调用父类的带有context参数的构造函数。
        public EmployeeManager(RoutineDbContext context) : base(context)
        {

        }
    }
}
