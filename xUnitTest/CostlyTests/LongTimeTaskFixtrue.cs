using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xUnitTest.CostlyTests
{
    public class LongTimeTaskFixtrue:IDisposable
    {
        public LongTimeTask _longTimeTask { get; }
        public LongTimeTaskFixtrue()
        {
            _longTimeTask = new LongTimeTask();
        }

        public void Dispose()
        {
            
        }
    }
}
