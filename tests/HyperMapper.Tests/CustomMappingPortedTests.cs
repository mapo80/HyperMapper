using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests ported from AutoMapper v14.0.0 CustomMapping.cs
/// Repository: https://github.com/LuckyPennySoftware/AutoMapper/tree/v14.0.0/src/UnitTests
/// License: MIT
/// </summary>
public class CustomMappingPortedTests
{
    #region ConvertUsing Lambda Tests

    [Fact]
    public void ConvertUsing_Lambda_Should_work()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<LambdaConvertProfile>());
        var mapper = config.CreateMapper();

        var source = new ConvertSource { Value = 42 };
        var dest = mapper.Map<ConvertDest>(source);

        Assert.Equal(42 * 2, dest.DoubledValue);
    }

    [Fact]
    public void ConvertUsing_Lambda_With_Complex_Logic_Should_work()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ComplexLambdaConvertProfile>());
        var mapper = config.CreateMapper();

        var source = new PersonSource { FirstName = "John", LastName = "Doe", Age = 30 };
        var dest = mapper.Map<PersonSummary>(source);

        Assert.Equal("John Doe", dest.FullName);
        Assert.True(dest.IsAdult);
    }

    #endregion

    #region ConvertUsing Instance Tests

    [Fact]
    public void ConvertUsing_Instance_Should_work()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<InstanceConvertProfile>());
        var mapper = config.CreateMapper();

        var source = new MoneySource { Amount = 100.50m, Currency = "USD" };
        var dest = mapper.Map<MoneyDisplay>(source);

        Assert.Equal("$100.50", dest.FormattedAmount);
    }

    #endregion

    #region ConvertUsing Type Tests

    [Fact]
    public void ConvertUsing_Type_Should_work()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<TypeConvertProfile>());
        var mapper = config.CreateMapper();

        var source = new CustomDateTimeSource { Date = new DateTime(2024, 6, 15) };
        var dest = mapper.Map<CustomDateStringDest>(source);

        Assert.Equal("2024-06-15", dest.DateString);
    }

    #endregion

    #region Custom MapFrom Expression Tests

    [Fact]
    public void MapFrom_Expression_Should_work()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<MapFromExpressionProfile>());
        var mapper = config.CreateMapper();

        var source = new OrderSource
        {
            Items = new List<OrderItem>
            {
                new() { Price = 10, Quantity = 2 },
                new() { Price = 5, Quantity = 3 }
            }
        };

        var dest = mapper.Map<OrderSummary>(source);

        Assert.Equal(35, dest.TotalPrice); // (10*2) + (5*3)
        Assert.Equal(5, dest.TotalQuantity);
    }

    [Fact]
    public void MapFrom_With_Nested_Property_Should_work()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<NestedMapFromProfile>());
        var mapper = config.CreateMapper();

        var source = new EmployeeSource
        {
            Name = "John",
            Department = new DepartmentSource { Name = "Engineering" }
        };

        var dest = mapper.Map<EmployeeFlat>(source);

        Assert.Equal("John", dest.Name);
        Assert.Equal("Engineering", dest.DepartmentName);
    }

    #endregion

    #region MapFrom Function Tests

    [Fact]
    public void MapFrom_Function_TwoParam_Should_work()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<FunctionMapFromProfile>());
        var mapper = config.CreateMapper();

        var source = new CoordinateSource { X = 3, Y = 4 };
        var dest = mapper.Map<DistanceResult>(source);

        Assert.Equal(5, dest.Distance); // sqrt(3^2 + 4^2)
    }

    #endregion

    #region Mapping From Constant Values Tests

    [Fact]
    public void MapFrom_Constant_Should_work()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ConstantMapFromProfile>());
        var mapper = config.CreateMapper();

        var source = new SimpleSource { Id = 1 };
        var dest = mapper.Map<DestWithDefault>(source);

        Assert.Equal(1, dest.Id);
        Assert.Equal("Default Value", dest.DefaultField);
        Assert.Equal(100, dest.DefaultNumber);
    }

    #endregion

    #region Type Conversion Tests

    [Fact]
    public void When_types_mismatch_Should_use_custom_converter()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<TypeMismatchProfile>());
        var mapper = config.CreateMapper();

        var source = new StringIdSource { Id = "42" };
        var dest = mapper.Map<IntIdDest>(source);

        Assert.Equal(42, dest.Id);
    }

    [Fact]
    public void When_mapping_enum_to_int_Should_convert()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<EnumConversionProfile>());
        var mapper = config.CreateMapper();

        var source = new EnumSourceCustom { Status = CustomStatus.Active };
        var dest = mapper.Map<IntStatusDest>(source);

        Assert.Equal(1, dest.Status);
    }

    [Fact]
    public void When_mapping_int_to_enum_Should_convert()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<EnumConversionProfile>());
        var mapper = config.CreateMapper();

        var source = new IntStatusSource { Status = 2 };
        var dest = mapper.Map<EnumDestCustom>(source);

        Assert.Equal(CustomStatus.Inactive, dest.Status);
    }

    #endregion

    #region Chained Mapping Tests

    [Fact]
    public void Chained_Mappings_Should_work()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ChainedMappingProfile>());
        var mapper = config.CreateMapper();

        var source = new CustomLevel1Source { Value = "Original" };

        var level2 = mapper.Map<CustomLevel2Intermediate>(source);
        Assert.Equal("Original", level2.Value);

        var level3 = mapper.Map<CustomLevel3Final>(level2);
        Assert.Equal("Original", level3.Value);
    }

    #endregion

    #region Multiple Converters Tests

    [Fact]
    public void Multiple_Converters_Same_Type_Should_work()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<MultiConverterProfile>());
        var mapper = config.CreateMapper();

        var source1 = new TypeASource { Code = "A123" };
        var dest1 = mapper.Map<CommonDest>(source1);
        Assert.Equal("TypeA: A123", dest1.Description);

        var source2 = new TypeBSource { Code = "B456" };
        var dest2 = mapper.Map<CommonDest>(source2);
        Assert.Equal("TypeB: B456", dest2.Description);
    }

    #endregion
}

#region Test Classes and Profiles

// Lambda Convert
public class ConvertSource { public int Value { get; set; } }
public class ConvertDest { public int DoubledValue { get; set; } }

public class LambdaConvertProfile : Profile
{
    public LambdaConvertProfile()
    {
        CreateMap<ConvertSource, ConvertDest>()
            .ForMember(d => d.DoubledValue, opt => opt.MapFrom(s => s.Value * 2));
    }
}

// Complex Lambda
public class PersonSource
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int Age { get; set; }
}

public class PersonSummary
{
    public string FullName { get; set; } = string.Empty;
    public bool IsAdult { get; set; }
}

public class ComplexLambdaConvertProfile : Profile
{
    public ComplexLambdaConvertProfile()
    {
        CreateMap<PersonSource, PersonSummary>()
            .ForMember(d => d.FullName, opt => opt.MapFrom(s => $"{s.FirstName} {s.LastName}"))
            .ForMember(d => d.IsAdult, opt => opt.MapFrom(s => s.Age >= 18));
    }
}

// Instance Convert
public class MoneySource
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
}

public class MoneyDisplay
{
    public string FormattedAmount { get; set; } = string.Empty;
}

public class MoneyConverter : ITypeConverter<MoneySource, MoneyDisplay>
{
    public MoneyDisplay Convert(MoneySource source, MoneyDisplay destination, ResolutionContext context)
    {
        var symbol = source.Currency == "USD" ? "$" : source.Currency;
        return new MoneyDisplay { FormattedAmount = $"{symbol}{source.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}" };
    }
}

public class InstanceConvertProfile : Profile
{
    public InstanceConvertProfile()
    {
        CreateMap<MoneySource, MoneyDisplay>()
            .ConvertUsing(new MoneyConverter());
    }
}

// Type Convert
public class CustomDateTimeSource { public DateTime Date { get; set; } }
public class CustomDateStringDest { public string DateString { get; set; } = string.Empty; }

public class CustomDateToStringConverter : ITypeConverter<CustomDateTimeSource, CustomDateStringDest>
{
    public CustomDateStringDest Convert(CustomDateTimeSource source, CustomDateStringDest destination, ResolutionContext context)
    {
        return new CustomDateStringDest { DateString = source.Date.ToString("yyyy-MM-dd") };
    }
}

public class TypeConvertProfile : Profile
{
    public TypeConvertProfile()
    {
        CreateMap<CustomDateTimeSource, CustomDateStringDest>()
            .ConvertUsing<CustomDateToStringConverter>();
    }
}

// MapFrom Expression
public class OrderItem
{
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public class OrderSource
{
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderSummary
{
    public decimal TotalPrice { get; set; }
    public int TotalQuantity { get; set; }
}

public class MapFromExpressionProfile : Profile
{
    public MapFromExpressionProfile()
    {
        CreateMap<OrderSource, OrderSummary>()
            .ForMember(d => d.TotalPrice, opt => opt.MapFrom(s => s.Items.Sum(i => i.Price * i.Quantity)))
            .ForMember(d => d.TotalQuantity, opt => opt.MapFrom(s => s.Items.Sum(i => i.Quantity)));
    }
}

// Nested MapFrom
public class EmployeeSource
{
    public string Name { get; set; } = string.Empty;
    public DepartmentSource? Department { get; set; }
}

public class DepartmentSource
{
    public string Name { get; set; } = string.Empty;
}

public class EmployeeFlat
{
    public string Name { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
}

public class NestedMapFromProfile : Profile
{
    public NestedMapFromProfile()
    {
        CreateMap<EmployeeSource, EmployeeFlat>()
            .ForMember(d => d.DepartmentName, opt => opt.MapFrom(s => s.Department != null ? s.Department.Name : string.Empty));
    }
}

// Function MapFrom
public class CoordinateSource
{
    public double X { get; set; }
    public double Y { get; set; }
}

public class DistanceResult
{
    public double Distance { get; set; }
}

public class FunctionMapFromProfile : Profile
{
    public FunctionMapFromProfile()
    {
        CreateMap<CoordinateSource, DistanceResult>()
            .ForMember(d => d.Distance, opt => opt.MapFrom((src, dest) => Math.Sqrt(src.X * src.X + src.Y * src.Y)));
    }
}

// Constant MapFrom
public class SimpleSourceCustom { public int Id { get; set; } }

public class DestWithDefault
{
    public int Id { get; set; }
    public string DefaultField { get; set; } = string.Empty;
    public int DefaultNumber { get; set; }
}

public class ConstantMapFromProfile : Profile
{
    public ConstantMapFromProfile()
    {
        CreateMap<SimpleSource, DestWithDefault>()
            .ForMember(d => d.DefaultField, opt => opt.MapFrom(s => "Default Value"))
            .ForMember(d => d.DefaultNumber, opt => opt.MapFrom(s => 100));
    }
}

public class CustomSimpleSource { public int Id { get; set; } }

// Type Mismatch
public class StringIdSource { public string Id { get; set; } = string.Empty; }
public class IntIdDest { public int Id { get; set; } }

public class TypeMismatchProfile : Profile
{
    public TypeMismatchProfile()
    {
        CreateMap<StringIdSource, IntIdDest>()
            .ForMember(d => d.Id, opt => opt.MapFrom(s => int.Parse(s.Id)));
    }
}

// Enum Conversion
public enum CustomStatus { Unknown = 0, Active = 1, Inactive = 2 }

public class EnumSourceCustom { public CustomStatus Status { get; set; } }
public class IntStatusDest { public int Status { get; set; } }
public class IntStatusSource { public int Status { get; set; } }
public class EnumDestCustom { public CustomStatus Status { get; set; } }

public class EnumConversionProfile : Profile
{
    public EnumConversionProfile()
    {
        CreateMap<EnumSourceCustom, IntStatusDest>();
        CreateMap<IntStatusSource, EnumDestCustom>();
    }
}

// Chained Mapping
public class CustomLevel1Source { public string Value { get; set; } = string.Empty; }
public class CustomLevel2Intermediate { public string Value { get; set; } = string.Empty; }
public class CustomLevel3Final { public string Value { get; set; } = string.Empty; }

public class ChainedMappingProfile : Profile
{
    public ChainedMappingProfile()
    {
        CreateMap<CustomLevel1Source, CustomLevel2Intermediate>();
        CreateMap<CustomLevel2Intermediate, CustomLevel3Final>();
    }
}

// Multi Converter
public class TypeASource { public string Code { get; set; } = string.Empty; }
public class TypeBSource { public string Code { get; set; } = string.Empty; }
public class CommonDest { public string Description { get; set; } = string.Empty; }

public class MultiConverterProfile : Profile
{
    public MultiConverterProfile()
    {
        CreateMap<TypeASource, CommonDest>()
            .ForMember(d => d.Description, opt => opt.MapFrom(s => $"TypeA: {s.Code}"));
        CreateMap<TypeBSource, CommonDest>()
            .ForMember(d => d.Description, opt => opt.MapFrom(s => $"TypeB: {s.Code}"));
    }
}

#endregion
