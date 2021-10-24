using Common.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using xUnitTest.CostlyTests;

namespace xUnitTest
{
    public class PatientTest : IClassFixture<LongTimeTaskFixtrue>, IDisposable
    {
        private readonly ITestOutputHelper _testOutput;
        private readonly LongTimeTask _longTimeTask;
        public PatientTest(ITestOutputHelper testOutput, LongTimeTaskFixtrue fixtrue)
        {
            _testOutput = testOutput;//自定义测试结果

            #region 共享实例
            //_longTimeTask = new LongTimeTask();//该实例耗时较长
            _longTimeTask = fixtrue._longTimeTask;//通过IClassFixture,让LongTimeTask共享，耗时较少
            #endregion

        }

        [Fact]
        [Trait("PatientTest", "Patient")]//分组
        public void TruePatient()
        {
            _testOutput.WriteLine("测试成功了！！！");
            var sut = new Patient();
            var result = sut.IsNew;
            //测试bool类型
            Assert.True(result);
        }

        [Fact(Skip = "忽略StringPatient测试")]
        [Trait("PatientTest", "Patient")]//分组
        public void StringPatient()
        {
            var sut = new Patient()
            {
                FirstName = "Nick",
                LastName = "Zero"
            };
            var result = sut.FullName;
            //测试string类型
            Assert.Equal("Nick Zero", result);
            Assert.NotEqual("Nick", result);
            Assert.StartsWith("Nick", result);
            Assert.EndsWith("Zero", result);
            Assert.Contains("Ni", result);//包含
            Assert.Matches(@"^[A-Z][a-z]*\s[A-Z][a-z]*", result);//正则表达式
        }

        [Fact]
        [Trait("PatientTest", "Patient")]//分组
        public void FloatPatient()
        {
            var sut = new Patient();
            var result = sut.BloodSugar;
            //测试float
            Assert.Equal(4.9f, result);
            Assert.InRange(result, 3.9f, 6.1f);//范围值
        }

        [Fact]
        [Trait("PatientTest", "Patient")]//分组
        public void NullPatient()
        {
            var sut = new Patient();
            var result = sut.FirstName;
            //测试Null
            Assert.Null(result);
            Assert.NotNull(sut);
        }
        [Fact]
        [Trait("PatientTest", "Patient")]//分组
        public void AggregatePatient()
        {
            var diseases = new List<string>()
            {
                "感冒",
                "发烧",
                "水痘",
                "腹泻"
            };
            var sut = new Patient();
            sut.History.Add("感冒");
            sut.History.Add("发烧");
            sut.History.Add("水痘");
            sut.History.Add("腹泻");
            //测试集合
            Assert.Contains("感冒", sut.History);
            Assert.DoesNotContain("心脏病", sut.History);//没有包含的病例
            Assert.Contains(sut.History, x => x.StartsWith("水"));//lambda表达式查询
            Assert.Equal(diseases, sut.History);//是否相等
            Assert.All(sut.History, x => Assert.True(x.Length >= 2));//病例从长度最少等于2
        }
        [Fact]
        [Trait("PatientTest", "New")]//分组
        public void TypePatient()
        {
            var sut = new Patient();
            var p = new Persen();

            Assert.IsType<Patient>(sut);//测试是一个类型
            Assert.IsNotType<Persen>(sut);//测试不是一个类型
            Assert.IsAssignableFrom<Persen>(p);//验证对象是否为给定类型或派生类型。
            Assert.NotSame(sut, p);//不是同一个实例
        }

        [Fact]
        [Trait("PatientTest", "New")]//分组
        public void ThorwPatient()
        {
            var p = new Patient();
            //检测是否一场
            var ex = Assert.Throws<Exception>(() =>
              {
                  p.NotAllowed();
              });
            //检测异常文字是否相等
            Assert.Equal("异常了", ex.Message);
        }
        [Fact]
        [Trait("PatientTest", "New")]//分组
        public void EventHandlerPatient()
        {
            var p = new Patient();
            //事件是否发生
            Assert.Raises<EventArgs>(
                handler => p.PatientSlept += handler,
                handler => p.PatientSlept -= handler,
                () => p.Sleep());
        }

        public void Dispose()
        {
            //每个测试执行后都执行这个方法
            _testOutput.WriteLine("释放资源");
        }
    }
}

