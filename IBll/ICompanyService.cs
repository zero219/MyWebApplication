using Common.Helpers;
using Common.ResourcesParameters;
using Entity.Dtos;
using Entity.Models;
using Microsoft.AspNetCore.Mvc;
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
        
        PageList<Company> QueryPage(CompanyParameters parameters);
       
        IEnumerable<LinkDto> CreateLinksForCompanies(string fields, IUrlHelper urlHelper, string routeName);
        
        IEnumerable<LinkDto> CreateLinksForCompany(Guid? companyId, string fields, IUrlHelper urlHelper, string routeName);
    }
}
