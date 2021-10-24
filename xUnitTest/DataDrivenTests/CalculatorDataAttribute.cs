using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit.Sdk;

namespace xUnitTest.DataDrivenTests
{
    public class CalculatorDataAttribute : DataAttribute
    {

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            yield return new object[] { 1, 2, 3 };
            yield return new object[] { 3, 2, 5 };
            yield return new object[] { 2, 2, 4 };
        }
    }
}
