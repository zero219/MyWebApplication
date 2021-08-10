using AutoMapper;
using Entity.Dtos;
using Entity.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Profiles
{
    public class EmployeeProfile : Profile
    {
        public EmployeeProfile()
        {
            //Employee映射到EmployeeDto
            CreateMap<Employee, EmployeeDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => string.Format("{0},{1}", src.FirstName, src.LastName)))
                .ForMember(dest => dest.GenderDisplay, opt => opt.MapFrom(src => src.Gender))
                .ForMember(dest => dest.Age, opt => opt.MapFrom(src => DateTime.Now.Year - src.DateOfBirth.Year));

            CreateMap<EmployeeAddDto, Employee>();
            CreateMap<EmployeeUpdateDto, Employee>();
            CreateMap<Employee, EmployeeUpdateDto>();
        }
    }
}
