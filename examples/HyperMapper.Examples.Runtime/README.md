# HyperMapper - Runtime Mode Example

This example demonstrates **Runtime Mode** - the AutoMapper-compatible configuration approach.

## Overview

Runtime Mode uses the familiar AutoMapper API:
- `MapperConfiguration` for setup
- `Profile` classes with `CreateMap<>()`
- `ForMember()` for custom mappings
- Dynamic runtime configuration

## What This Example Demonstrates

1. **Basic Configuration**: Setting up `MapperConfiguration` and `Profile`
2. **Computed Properties**: `FullName` from `FirstName + LastName`, `Age` from `BirthDate`
3. **Nested Objects**: Mapping `Customer.Address` to `CustomerDto.Address`
4. **Collections**: Mapping `List<Order>` to `List<OrderDto>`
5. **Enum Conversion**: `OrderStatus` enum to string
6. **ReverseMap**: Bidirectional mapping between `Address` and `AddressDto`
7. **Map to Existing**: Updating existing DTO instances
8. **Performance**: Typical ~100-200ns per mapping

## Running the Example

```bash
cd examples/HyperMapper.Examples.Runtime
dotnet run
```

## Project Structure

```
HyperMapper.Examples.Runtime/
├── Models/
│   ├── Address.cs          - Simple entity with computed FullAddress in DTO
│   ├── Customer.cs         - Main entity with nested objects
│   ├── CustomerDto.cs      - DTO with computed properties
│   ├── Order.cs            - Order entity with enum status
│   └── OrderDto.cs         - Order DTO with enum→string conversion
├── Profiles/
│   └── MappingProfile.cs   - Centralized mapping configuration
└── Program.cs              - 8 demonstration examples
```

## Key Code Snippets

### Configuration (Program.cs)

```csharp
var config = new MapperConfiguration(cfg =>
{
    cfg.AddProfile<MappingProfile>();
});

config.AssertConfigurationIsValid();
var mapper = config.CreateMapper();
```

### Profile with Computed Properties (MappingProfile.cs)

```csharp
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Customer, CustomerDto>()
            .ForMember(d => d.FullName, opt => opt.MapFrom(s =>
                $"{s.FirstName} {s.LastName}"))
            .ForMember(d => d.Age, opt => opt.MapFrom(s =>
                CalculateAge(s.BirthDate)))
            .ForMember(d => d.OrderCount, opt => opt.MapFrom(s =>
                s.Orders.Count));
    }

    private static int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }
}
```

### Usage

```csharp
// Simple mapping
var customerDto = mapper.Map<CustomerDto>(customer);

// Map to existing (update)
mapper.Map(customer, existingDto);

// Collection mapping
var dtos = mapper.Map<List<OrderDto>>(orders);
```

## Performance

Runtime Mode provides excellent performance:
- **Simple mapping**: ~100-200ns
- **Complex nested**: ~500-1000ns
- **Collections (1000 items)**: ~30ms

For even better performance, see the [CodeGen example](../HyperMapper.Examples.CodeGen/).

## When to Use Runtime Mode

✅ **Good for:**
- Rapid prototyping
- Dynamic type resolution
- Complex custom converters
- When you need `BeforeMap`/`AfterMap` hooks

❌ **Consider CodeGen Mode for:**
- Production applications (2-3x faster)
- AOT/Native compilation
- Maximum performance requirements
- Compile-time error detection

## Learn More

- [Main README](../../README.md) - Complete documentation
- [CodeGen Example](../HyperMapper.Examples.CodeGen/) - Source Generator mode
- [HyperMapper GitHub](https://github.com/your-org/HyperMapper) - Full project

## 100% AutoMapper Compatible

This example uses **only AutoMapper-compatible APIs**. To migrate from AutoMapper:

1. Change `using AutoMapper;` to `using HyperMapper;`
2. Update project references
3. Done! No code changes required.

```bash
# Find and replace in all files
find . -name "*.cs" -exec sed -i '' 's/using AutoMapper;/using HyperMapper;/g' {} \;
```
