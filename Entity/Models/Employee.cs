﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Entity.Models
{
    public class Employee
    {
        public Guid Id { get; set; }

        public Guid CompanyId { get; set; }

        public string EmployeeNo { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Gender Gender { get; set; }
        public DateTime DateOfBirth { get; set; }
        public Company Company { get; set; }
    }

    public enum Gender
    {
        女 = 0,
        男 = 1
    }
}
