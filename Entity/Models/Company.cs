using System;
using System.Collections.Generic;
using System.Text;

namespace Entity.Models
{
    public class Company
    {
        /// <summary>
        /// 编号
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 国家
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// 行业性质
        /// </summary>
        public string Industry { get; set; }

        /// <summary>
        /// 产品
        /// </summary>
        public string Product { get; set; }

        /// <summary>
        /// 介绍
        /// </summary>
        public string Introduction { get; set; }

        /// <summary>
        /// 时间
        /// </summary>
        public DateTime? BankruptTime { get; set; }

        public ICollection<Employee> Employees { get; set; }
    }
}
