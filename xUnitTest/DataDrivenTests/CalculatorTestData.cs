using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xUnitTest.DataDrivenTests
{
    public class CalculatorTestData
    {
        private static readonly List<object[]> data = new List<object[]>()
        {
            new object[]{ 1, 2, 3 },
            new object[]{ 2, 3, 5 },
            new object[]{ 3, 3, 6 },
        };
        public static IEnumerable<object[]> TestData => data;
    }
}
