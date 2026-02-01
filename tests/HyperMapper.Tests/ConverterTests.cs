using Xunit;

namespace HyperMapper.Tests;

public class ConverterTests
{
    [Fact]
    public void ConvertUsing_WithInstanceConverter_Works()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ConverterProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithDate { Date = new DateTime(2024, 1, 15) };
        var dest = mapper.Map<DestWithString>(source);

        Assert.Equal("2024-01-15", dest.DateString);
    }

    [Fact]
    public void ConvertUsing_ContextMapperAccess_Works()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ConverterProfile>());
        var mapper = config.CreateMapper();

        var source = new Wrapper<Inner> { Item = new Inner { Value = 42 } };
        var dest = mapper.Map<WrapperDto<InnerDto>>(source);

        Assert.NotNull(dest.Item);
        Assert.Equal(42, dest.Item.Value);
    }

    [Fact]
    public void ConvertUsing_OpenGeneric_Works()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ConverterProfile>());
        var mapper = config.CreateMapper();

        var source = new Container<string> { Data = "test data" };
        var dest = mapper.Map<ContainerDto<string>>(source);

        Assert.Equal("test data", dest.Data);
    }
}

public class ConverterProfile : Profile
{
    public ConverterProfile()
    {
        CreateMap<SourceWithDate, DestWithString>()
            .ConvertUsing(new DateToStringConverter());

        CreateMap<Inner, InnerDto>();

        CreateMap(typeof(Wrapper<>), typeof(WrapperDto<>))
            .ConvertUsing(typeof(WrapperConverter<,>));

        CreateMap(typeof(Container<>), typeof(ContainerDto<>))
            .ConvertUsing(typeof(ContainerConverter<>));
    }
}

public class DateToStringConverter : ITypeConverter<SourceWithDate, DestWithString>
{
    public DestWithString Convert(SourceWithDate source, DestWithString destination, ResolutionContext ctx)
    {
        return new DestWithString { DateString = source.Date.ToString("yyyy-MM-dd") };
    }
}

public class WrapperConverter<T1, T2> : ITypeConverter<Wrapper<T1>, WrapperDto<T2>>
{
    public WrapperDto<T2> Convert(Wrapper<T1> source, WrapperDto<T2> destination, ResolutionContext ctx)
    {
        return new WrapperDto<T2> { Item = ctx.Mapper.Map<T2>(source.Item!) };
    }
}

public class ContainerConverter<T> : ITypeConverter<Container<T>, ContainerDto<T>>
{
    public ContainerDto<T> Convert(Container<T> source, ContainerDto<T> destination, ResolutionContext ctx)
    {
        return new ContainerDto<T> { Data = source.Data };
    }
}

// Test classes
public class SourceWithDate
{
    public DateTime Date { get; set; }
}

public class DestWithString
{
    public string DateString { get; set; } = string.Empty;
}

public class Wrapper<T>
{
    public T? Item { get; set; }
}

public class WrapperDto<T>
{
    public T? Item { get; set; }
}

public class Container<T>
{
    public T? Data { get; set; }
}

public class ContainerDto<T>
{
    public T? Data { get; set; }
}

public class Inner
{
    public int Value { get; set; }
}

public class InnerDto
{
    public int Value { get; set; }
}
