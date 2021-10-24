using Common.Test;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using Xunit;
using xUnitTest.CostlyTests;
using xUnitTest.DataDrivenTests;

namespace xUnitTest
{
    //ͨ��TaskCollection����CollectionDefinition�������ַ���һ��,�õ������LongTimeTask,ȥ���򱨴�
    [Collection("Long Time Task Collection")]
    [Trait("CalculatorTest", "New")]//����
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
        [InlineData(1, 2, 3)]//����ִ��SimplifyTests����
        [InlineData(2, 2, 4)]//����ִ��SimplifyTests����
        [InlineData(3, 3, 6)]//����ִ��SimplifyTests����
        public void SimplifyTests(int x, int y, int res)
        {

            var sut = new Calculator();
            var result = sut.Add(x, y);
            Assert.Equal(res, result);
        }

        [Theory]
        [MemberData(nameof(CalculatorTestData.TestData), MemberType = typeof(CalculatorTestData))]//ͨ��������ж���ִ��
        public void DataTests(int x, int y, int res)
        {

            var sut = new Calculator();
            var result = sut.Add(x, y);
            Assert.Equal(res, result);
        }

        [Theory]
        [CalculatorData]//�Զ������Զ���ִ��
        public void CustomDataTests(int x, int y, int res)
        {
            var sut = new Calculator();
            var result = sut.Add(x, y);
            Assert.Equal(res, result);
        }
    }
}
