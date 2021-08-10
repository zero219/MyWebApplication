using Entity.Models;
using IBll;
using IDal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Bll
{
    public class EmployeeService : BaseService<Employee>, IEmployeeService
    {
        private readonly IEmployeeManager _employeeManager;
        public EmployeeService(IEmployeeManager employeeManager) : base(employeeManager)
        {
            _employeeManager = employeeManager;
        }

        public IQueryable<Employee> QueryEmployees(Expression<Func<Employee, bool>> whereLambda)
        {
            return LoadEntities(whereLambda).AsQueryable();
        }

    }
}
