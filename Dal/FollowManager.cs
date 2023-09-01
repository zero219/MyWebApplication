using Entity.Data;
using Entity.Models;
using IDal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dal
{
    public class FollowManager : BaseRepository<Follow>, IFollowManager
    {
        public FollowManager(RoutineDbContext context) : base(context)
        {
        }
    }
}
