using HyperMapper;
using HyperMapper.Examples.CodeGen.Models;

namespace HyperMapper.Examples.CodeGen.Profiles;

/// <summary>
/// Mapping profile for CodeGen Mode
/// The Source Generator analyzes this Profile at compile-time
/// and generates optimized C# mapping methods
/// </summary>
public class ProductProfile : Profile
{
    public ProductProfile()
    {
        // Simple struct mapping - compiled at build-time
        CreateMap<ProductMetadata, ProductMetadataDto>();

        // Category mapping - simple property mapping
        CreateMap<Category, CategoryDto>();

        // Product mapping with computed properties and PreCondition
        CreateMap<Product, ProductDto>()
            // Stock only mapped if product is active (PreCondition compiled to if-statement)
            .ForMember(d => d.Stock, opt =>
            {
                opt.PreCondition(s => s.IsActive);
                opt.MapFrom(s => s.Stock);
            })
            // Flatten nested property
            .ForMember(d => d.CategoryName, opt =>
                opt.MapFrom(s => s.Category != null ? s.Category.Name : "Uncategorized"))
            // Computed full name
            .ForMember(d => d.FullName, opt =>
                opt.MapFrom(s => $"{s.Name} ({(s.Category != null ? s.Category.Name : "N/A")})"))
            // Computed age in days
            .ForMember(d => d.AgeInDays, opt =>
                opt.MapFrom(s => (DateTime.Now - s.CreatedDate).Days));
    }
}
