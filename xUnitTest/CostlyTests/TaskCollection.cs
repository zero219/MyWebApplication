using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace xUnitTest.CostlyTests
{
    [CollectionDefinition("Long Time Task Collection")]
    public class TaskCollection:ICollectionFixture<LongTimeTaskFixtrue>
    {
    }
}
