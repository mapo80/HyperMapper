using HyperMapper.IntegrationTests.Dtos;
using HyperMapper.IntegrationTests.Entities;

namespace HyperMapper.IntegrationTests.Profiles;

public class TestMappingProfile : Profile
{
    public TestMappingProfile()
    {
        // Address -> AddressDto
        CreateMap<Address, AddressDto>();

        // Customer -> CustomerDto
        CreateMap<Customer, CustomerDto>()
            .ForMember(d => d.CustomerType, opt => opt.MapFrom(s => s.Type.ToString()))
            .ForMember(d => d.OrderCount, opt => opt.MapFrom(s => s.Orders.Count));

        // Customer -> CustomerSummaryDto
        CreateMap<Customer, CustomerSummaryDto>()
            .ForMember(d => d.FullAddress, opt => opt.MapFrom(s =>
                s.Address != null
                    ? $"{s.Address.Street}, {s.Address.City}, {s.Address.PostalCode}, {s.Address.Country}"
                    : null))
            .ForMember(d => d.OrderCount, opt => opt.MapFrom(s => s.Orders.Count));

        // OrderItem -> OrderItemDto
        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(d => d.ProductName, opt => opt.MapFrom(s => s.Product.Name))
            .ForMember(d => d.ProductSku, opt => opt.MapFrom(s => s.Product.Sku))
            .ForMember(d => d.LineTotal, opt => opt.MapFrom(s => s.Quantity * s.UnitPrice));

        // Order -> OrderDto
        CreateMap<Order, OrderDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.CustomerName, opt => opt.MapFrom(s => s.Customer.Name));

        // Order -> OrderSummaryDto
        CreateMap<Order, OrderSummaryDto>()
            .ForMember(d => d.ItemCount, opt => opt.MapFrom(s => s.Items.Count));

        // Product -> ProductDto
        CreateMap<Product, ProductDto>()
            .ForMember(d => d.Category, opt => opt.MapFrom(s => s.Category.ToString()));
    }
}
