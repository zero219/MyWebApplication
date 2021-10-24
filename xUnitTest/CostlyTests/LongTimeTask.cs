using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace xUnitTest.CostlyTests
{
    public class LongTimeTask
    {
        public LongTimeTask()
        {
            Thread.Sleep(3000);
        }
    }
}
