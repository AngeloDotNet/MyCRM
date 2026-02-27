using Api.DTOs;
using Api.Entities;
using AutoMapper;

namespace Api.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Contact, ContactDto>().ReverseMap();
        CreateMap<Company, CompanyDto>().ReverseMap();
    }
}