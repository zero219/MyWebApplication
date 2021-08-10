using Entity.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;

namespace IBll
{
    public interface IEmployeeService : IBaseService<Employee>
    {
        IQueryable<Employee> QueryEmployees(Expression<Func<Employee, bool>> whereLambda);

       
    }
}
