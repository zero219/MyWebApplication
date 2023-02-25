using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Entity.Data
{
    public class RoutineModelCacheKeyFactory : IModelCacheKeyFactory
    {
        public object Create(DbContext context, bool designTime)
        => context is RoutineDbContext routineDbContext
            ? (context.GetType(), routineDbContext.UseIntProperty, designTime)
            : (object)context.GetType();

        public object Create(DbContext context)
           => Create(context, false);
    }
}
