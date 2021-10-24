using Common.Test;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using Xunit;
using xUnitTest.CostlyTests;
using xUnitTest.DataDrivenTests;

namespace xUnitTest
{
    //通过TaskCollection类中CollectionDefinition属性中字符串一致,拿到共享的LongTimeTask,去掉则报错
    [Collection("Long Time Task Collection")]
    [Trait("CalculatorTest", "New")]//分组
    public class CalculatorTest
    {
        private readonly LongTimeTask _longTimeTask;
        public CalculatorTest(LongTimeTaskFixtrue fixtrue)
        {
            _longTimeTask = fixtrue._longTimeTask;
        }
        [Fact]
        public void AddTest()
        {
            //1.Arrange
            var sut = new Calculator();//sut system under test
            //2.Act
            var result = sut.Add(1, 2);
            //3.Assert
            Assert.Equal(3, result);
        }

        [Theory]
        [InlineData(1, 2, 3)]//传参执行SimplifyTests方法
        [InlineData(2, 2, 4)]//传参执行SimplifyTests方法
        [InlineData(3, 3, 6)]//传参执行SimplifyTests方法
        public void SimplifyTests(int x, int y, int res)
        {

            var sut = new Calculator();
            var result = sut.Add(x, y);
            Assert.Equal(res, result);
        }

        [Theory]
        [MemberData(nameof(CalculatorTestData.TestData), MemberType = typeof(CalculatorTestData))]//通过数组进行多组执行
        public void DataTests(int x, int y, int res)
        {

            var sut = new Calculator();
            var result = sut.Add(x, y);
            Assert.Equal(res, result);
        }

        [Theory]
        [CalculatorData]//自定义属性多组执行
        public void CustomDataTests(int x, int y, int res)
        {
            var sut = new Calculator();
            var result = sut.Add(x, y);
            Assert.Equal(res, result);
        }
    }
}
