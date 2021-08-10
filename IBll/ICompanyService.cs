using Common.Helpers;
using Common.ResourcesParameters;
using Entity.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace IBll
{
    public interface ICompanyService : IBaseService<Company>
    {
        IQueryable<Company> QueryWhere(Expression<Func<Company, bool>> whereLambda);

        IQueryable<Company> QueryAll();
        Task<PageList<Company>> QueryPage(CompanyParameters parameters);

    }
}
