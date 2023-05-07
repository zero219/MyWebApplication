using Entity.Data;
using Entity.Models;
using IDal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dal
{
    internal class SignManager : BaseRepository<Sign>, ISignManager
    {
        public SignManager(RoutineDbContext context) : base(context)
        {
        }
    }
}
