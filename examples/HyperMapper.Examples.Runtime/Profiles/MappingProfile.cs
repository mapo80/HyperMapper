using HyperMapper;
using HyperMapper.Examples.Runtime.Models;

namespace HyperMapper.Examples.Runtime.Profiles;

/// <summary>
/// Mapping profile demonstrating Runtime Mode configuration
/// This profile uses AutoMapper-compatible API at runtime
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Simple mapping with bidirectional support
        CreateMap<Address, AddressDto>()
            .ForMember(d => d.FullAddress, opt => opt.MapFrom(s =>
                $"{s.Street}, {s.City}, {s.State} {s.ZipCode}"))
            .ReverseMap();

        // OrderItem with computed property
        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(d => d.LineTotal, opt => opt.MapFrom(s =>
                s.Quantity * s.UnitPrice));

        // Order with enum to string conversion and computed properties
        CreateMap<Order, OrderDto>()
            .ForMember(d => d.Status, opt => opt.MapFrom(s =>
                s.Status.ToString()))
            .ForMember(d => d.ItemCount, opt => opt.MapFrom(s =>
                s.Items.Count));

        // Customer with nested objects and computed properties
        CreateMap<Customer, CustomerDto>()
            // Combine first and last name
            .ForMember(d => d.FullName, opt => opt.MapFrom(s =>
                $"{s.FirstName} {s.LastName}"))
            // Calculate age from birth date
            .ForMember(d => d.Age, opt => opt.MapFrom(s =>
                CalculateAge(s.BirthDate)))
            // Computed order count
            .ForMember(d => d.OrderCount, opt => opt.MapFrom(s =>
                s.Orders.Count));
    }

    /// <summary>
    /// Helper method to calculate age from birth date
    /// </summary>
    private static int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;

        // Adjust if birthday hasn't occurred this year
        if (birthDate.Date > today.AddYears(-age))
        {
            age--;
        }

        return age;
    }
}
