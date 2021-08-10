using AutoMapper;
using Entity.Dtos;
using Entity.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Profiles
{
    public class CompanyProfile : Profile
    {
        public CompanyProfile()
        {
            //Company映射到CompanyDto
            CreateMap<Company, CompanyDto>()
                .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.Name));//名称不一致时映射

            CreateMap<CompanyAddDto, Company>().ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.CompanyName));

            CreateMap<Company, CompanyFullDto>().ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.Name));
        }
    }
}
