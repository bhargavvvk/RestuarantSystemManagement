using AutoMapper;
using RestaurantAPI.Models;
using RestaurantAPI.Models.DTOs;

namespace RestaurantAPI.mapper;

public class Mapping : Profile
{
    public Mapping()
    {
        CreateMap<MenuItem, MenuItemResponseDto>()
            .ForMember(
                dest => dest.CategoryName,
                opt => opt.MapFrom(src => src.Category!.Name)
            );
        CreateMap<Bill, BillResponseDto>()
            .ForMember(dest => dest.CgstPercentage,
                opt => opt.MapFrom(src => src.TaxConfiguration != null ? src.TaxConfiguration.CgstPercentage : 0))
            .ForMember(dest => dest.SgstPercentage,
                opt => opt.MapFrom(src => src.TaxConfiguration != null ? src.TaxConfiguration.SgstPercentage : 0))
            .ForMember(dest => dest.ServiceChargePercentage,
                opt => opt.MapFrom(src => src.TaxConfiguration != null ? src.TaxConfiguration.ServiceChargePercentage : 0));
        CreateMap<OrderItem, OrderItemResponseDto>()
                    .ForMember(
                    dest => dest.OrderItemId,
                    opt => opt.MapFrom(src => src.Id)
                );
        CreateMap<Category, CategoryResponseDto>();
        CreateMap<MenuItem, MenuItemResponseDto>();
        CreateMap<TaxConfiguration, TaxConfigurationResponseDto>();
    }
}