using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Test
{
    /// <summary>
    /// 病人
    /// </summary>
    public class Patient : Persen
    {
        public Patient()
        {
            IsNew = true;
            BloodSugar = 4.9f;
            History = new List<string>();

        }
        public string FullName => $"{FirstName} {LastName}";
        /// <summary>
        /// 血糖
        /// </summary>
        public float BloodSugar { get; set; }
        /// <summary>
        /// 心率
        /// </summary>
        public int HeartBeatRate { get; set; }
        /// <summary>
        /// 是否是新病人
        /// </summary>
        public bool IsNew { get; set; }
        /// <summary>
        /// 历史病例
        /// </summary>
        public List<string> History { get; set; }
        /// <summary>
        /// 增加心率
        /// </summary>
        public void IncreaseHeartBeatRate()
        {
            HeartBeatRate = CalculateHeartBeatRate() + 2;
        }
        /// <summary>
        /// 计算心率
        /// </summary>
        /// <returns></returns>
        private int CalculateHeartBeatRate()
        {
            Random random = new Random();
            return random.Next(1, 100);
        }
        /// <summary>
        /// 抛异常
        /// </summary>
        public void NotAllowed()
        {
            throw new Exception("异常了");
        }

        public event EventHandler<EventArgs> PatientSlept;
        public void OnPatientSleep()
        {
            PatientSlept?.Invoke(this, EventArgs.Empty);
        }
        public void Sleep()
        {
            OnPatientSleep();
        }
    }

    public class Persen
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
