using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests ported from AutoMapper v14.0.0 OpenGenerics.cs
/// Repository: https://github.com/LuckyPennySoftware/AutoMapper/tree/v14.0.0/src/UnitTests
/// License: MIT
/// Note: Tests requiring string-based ForMember for open generics are skipped as HyperMapper
/// doesn't support string-based member configuration on IMappingExpressionBase.
/// </summary>
public class OpenGenericsPortedTests
{
    #region Simple Generic Type Mapping Tests

    [Fact]
    public void Can_map_simple_generic_types()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<SimpleGenericProfile>());
        var mapper = config.CreateMapper();

        var source = new PortedGenericSource<int> { Value = 5 };
        var dest = mapper.Map<PortedGenericDest<int>>(source);

        Assert.Equal(5, dest.Value);
    }

    [Fact]
    public void Can_map_non_generic_members()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<GenericWithNonGenericProfile>());
        var mapper = config.CreateMapper();

        var source = new GenericSourceWithNonGeneric<int> { A = 5, Value = 10 };
        var dest = mapper.Map<GenericDestWithNonGeneric<int>>(source);

        Assert.Equal(5, dest.A);
        Assert.Equal(10, dest.Value);
    }

    #endregion

    #region Recursive Generic Types Tests

    [Fact]
    public void Can_map_recursive_generic_types()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<SimpleGenericProfile>());
        var mapper = config.CreateMapper();

        var source = new PortedGenericSource<PortedGenericSource<int>>
        {
            Value = new PortedGenericSource<int> { Value = 5 }
        };

        var dest = mapper.Map<PortedGenericDest<PortedGenericDest<int>>>(source);

        Assert.Equal(5, dest.Value.Value);
    }

    #endregion

    #region Open Generics With ConvertUsing Type Tests

    [Fact]
    public void OpenGenerics_With_ConvertUsing_Should_work()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<OpenGenericConverterProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new GenericBox<string> { Item = "Test" };
        var dest = mapper.Map<GenericBoxDto<string>>(source);

        Assert.Equal("Test", dest.Item);
    }

    #endregion

    #region Recursive Open Generics Tests

    [Fact]
    public void RecursiveOpenGenerics_Should_work()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<RecursiveTreeProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceTree<string>("value", Array.Empty<SourceTree<string>>());
        var dest = mapper.Map<DestinationTree<string>>(source);

        Assert.Equal("value", dest.Value);
    }

    #endregion

    #region Two Generic Parameters Tests

    [Fact]
    public void Can_map_open_generic_with_two_type_parameters()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<KeyValueProfile>());
        var mapper = config.CreateMapper();

        var source = new KeyValueSource<string, int> { Key = "test", Value = 42 };
        var dest = mapper.Map<KeyValueDest<string, int>>(source);

        Assert.Equal("test", dest.Key);
        Assert.Equal(42, dest.Value);
    }

    #endregion
}

#region Test Classes and Profiles

// Simple Generic
public class PortedGenericSource<T>
{
    public T Value { get; set; } = default!;
}

public class PortedGenericDest<T>
{
    public T Value { get; set; } = default!;
}

public class SimpleGenericProfile : Profile
{
    public SimpleGenericProfile()
    {
        CreateMap(typeof(PortedGenericSource<>), typeof(PortedGenericDest<>));
    }
}

// Generic with Non-Generic Members
public class GenericSourceWithNonGeneric<T>
{
    public int A { get; set; }
    public T Value { get; set; } = default!;
}

public class GenericDestWithNonGeneric<T>
{
    public int A { get; set; }
    public T Value { get; set; } = default!;
}

public class GenericWithNonGenericProfile : Profile
{
    public GenericWithNonGenericProfile()
    {
        CreateMap(typeof(GenericSourceWithNonGeneric<>), typeof(GenericDestWithNonGeneric<>));
    }
}

// Generic Box (with ConvertUsing)
public class GenericBox<T>
{
    public T? Item { get; set; }
}

public class GenericBoxDto<T>
{
    public T? Item { get; set; }
}

public class OpenGenericConverterProfile : Profile
{
    public OpenGenericConverterProfile()
    {
        CreateMap(typeof(GenericBox<>), typeof(GenericBoxDto<>))
            .ConvertUsing(typeof(GenericBoxConverter<>));
    }
}

public class GenericBoxConverter<T> : ITypeConverter<GenericBox<T>, GenericBoxDto<T>>
{
    public GenericBoxDto<T> Convert(GenericBox<T> source, GenericBoxDto<T> destination, ResolutionContext context)
    {
        return new GenericBoxDto<T> { Item = source.Item };
    }
}

// Recursive Tree
public class SourceTree<T>
{
    public SourceTree(T value, SourceTree<T>[] children)
    {
        Value = value;
        Children = children;
    }

    public T Value { get; }
    public SourceTree<T>[] Children { get; }
}

public class DestinationTree<T>
{
    public DestinationTree() { }

    public DestinationTree(T value, DestinationTree<T>[] children)
    {
        Value = value;
        Children = children;
    }

    public T Value { get; set; } = default!;
    public DestinationTree<T>[]? Children { get; set; }
}

public class RecursiveTreeProfile : Profile
{
    public RecursiveTreeProfile()
    {
        CreateMap(typeof(SourceTree<>), typeof(DestinationTree<>));
    }
}

// Two Generic Parameters
public class KeyValueSource<TKey, TValue>
{
    public TKey Key { get; set; } = default!;
    public TValue Value { get; set; } = default!;
}

public class KeyValueDest<TKey, TValue>
{
    public TKey Key { get; set; } = default!;
    public TValue Value { get; set; } = default!;
}

public class KeyValueProfile : Profile
{
    public KeyValueProfile()
    {
        CreateMap(typeof(KeyValueSource<,>), typeof(KeyValueDest<,>));
    }
}

#endregion
