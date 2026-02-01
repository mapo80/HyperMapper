using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Tests ported from AutoMapper v14.0.0 Profiles.cs
/// Repository: https://github.com/LuckyPennySoftware/AutoMapper/tree/v14.0.0/src/UnitTests
/// License: MIT
/// </summary>
public class ProfilesPortedTests
{
    #region Basic Profile Tests

    [Fact]
    public void Should_use_single_profile()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<SingleProfile>());
        var mapper = config.CreateMapper();

        var source = new ProfileSource { Name = "Test", Value = 42 };
        var dest = mapper.Map<ProfileDest>(source);

        Assert.Equal("Test", dest.Name);
        Assert.Equal(42, dest.Value);
    }

    [Fact]
    public void Should_use_multiple_profiles()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ProfileA>();
            cfg.AddProfile<ProfileB>();
        });
        var mapper = config.CreateMapper();

        var sourceA = new SourceA { PropA = "A" };
        var sourceB = new SourceB { PropB = "B" };

        var destA = mapper.Map<DestA>(sourceA);
        var destB = mapper.Map<DestB>(sourceB);

        Assert.Equal("A", destA.PropA);
        Assert.Equal("B", destB.PropB);
    }

    [Fact]
    public void Should_use_profile_instance()
    {
        var profile = new SingleProfile();
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile(profile));
        var mapper = config.CreateMapper();

        var source = new ProfileSource { Name = "Instance", Value = 100 };
        var dest = mapper.Map<ProfileDest>(source);

        Assert.Equal("Instance", dest.Name);
    }

    #endregion

    #region Profile with ForMember Tests

    [Fact]
    public void Profile_Should_support_ForMember()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ForMemberProfile>());
        var mapper = config.CreateMapper();

        var source = new ForMemberSource { First = "John", Last = "Doe" };
        var dest = mapper.Map<ForMemberDest>(source);

        Assert.Equal("John Doe", dest.FullName);
    }

    [Fact]
    public void Profile_Should_support_multiple_ForMember()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<MultiForMemberProfile>());
        var mapper = config.CreateMapper();

        var source = new ProfileMultiSource { A = 1, B = 2, C = 3 };
        var dest = mapper.Map<ProfileMultiDest>(source);

        Assert.Equal(2, dest.X); // A + 1
        Assert.Equal(4, dest.Y); // B * 2
        Assert.Equal("3", dest.Z); // C.ToString()
    }

    #endregion

    #region Profile with ReverseMap Tests

    [Fact]
    public void Profile_Should_support_ReverseMap()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ProfileReverseMapProfile>());
        var mapper = config.CreateMapper();

        var source = new ProfileReverseSource { Id = 1, Name = "Test" };
        var dest = mapper.Map<ProfileReverseDest>(source);
        var back = mapper.Map<ProfileReverseSource>(dest);

        Assert.Equal(source.Id, back.Id);
        Assert.Equal(source.Name, back.Name);
    }

    #endregion

    #region Profile with ConvertUsing Tests

    [Fact]
    public void Profile_Should_support_ConvertUsing_with_instance()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ProfileConverterProfile>());
        var mapper = config.CreateMapper();

        var source = new ProfileConverterSource { Amount = 100 };
        var dest = mapper.Map<ProfileConverterDest>(source);

        Assert.Equal("$100.00", dest.FormattedAmount);
    }

    [Fact]
    public void Profile_Should_support_ConvertUsing_with_type()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<TypeConverterProfile>());
        var mapper = config.CreateMapper();

        var source = new TypeConverterSource { Value = 42 };
        var dest = mapper.Map<TypeConverterDest>(source);

        Assert.Equal("Converted: 42", dest.Result);
    }

    #endregion

    #region Profile with Ignore Tests

    [Fact]
    public void Profile_Should_support_Ignore()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<IgnoreProfile>());
        var mapper = config.CreateMapper();

        var source = new IgnoreSource { Keep = "Keep", Skip = "Skip" };
        var dest = mapper.Map<IgnoreDest>(source);

        Assert.Equal("Keep", dest.Keep);
        Assert.Null(dest.Skip); // Ignored
    }

    #endregion

    #region Profile with Nested Mapping Tests

    [Fact]
    public void Profile_Should_map_nested_types()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<ProfileNestedProfile>());
        var mapper = config.CreateMapper();

        var source = new NestedProfileTestSource
        {
            Name = "Parent",
            Child = new ChildProfileTestSource { ChildName = "Child" }
        };

        var dest = mapper.Map<NestedProfileTestDest>(source);

        Assert.Equal("Parent", dest.Name);
        Assert.NotNull(dest.Child);
        Assert.Equal("Child", dest.Child.ChildName);
    }

    #endregion

    #region Multiple CreateMap in Single Profile Tests

    [Fact]
    public void Profile_Should_support_multiple_CreateMap()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<MultiMapProfile>());
        var mapper = config.CreateMapper();

        var cat = new Cat { Name = "Whiskers", Meows = true };
        var dog = new Dog { Name = "Rex", Barks = true };

        var catDto = mapper.Map<CatDto>(cat);
        var dogDto = mapper.Map<DogDto>(dog);

        Assert.Equal("Whiskers", catDto.Name);
        Assert.True(catDto.Meows);
        Assert.Equal("Rex", dogDto.Name);
        Assert.True(dogDto.Barks);
    }

    #endregion
}

#region Test Classes and Profiles

// Basic Profile
public class ProfileSource
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class ProfileDest
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class SingleProfile : Profile
{
    public SingleProfile()
    {
        CreateMap<ProfileSource, ProfileDest>();
    }
}

// Multiple Profiles
public class SourceA { public string PropA { get; set; } = string.Empty; }
public class DestA { public string PropA { get; set; } = string.Empty; }
public class SourceB { public string PropB { get; set; } = string.Empty; }
public class DestB { public string PropB { get; set; } = string.Empty; }

public class ProfileA : Profile
{
    public ProfileA()
    {
        CreateMap<SourceA, DestA>();
    }
}

public class ProfileB : Profile
{
    public ProfileB()
    {
        CreateMap<SourceB, DestB>();
    }
}

// ForMember Profile
public class ForMemberSource
{
    public string First { get; set; } = string.Empty;
    public string Last { get; set; } = string.Empty;
}

public class ForMemberDest
{
    public string FullName { get; set; } = string.Empty;
}

public class ForMemberProfile : Profile
{
    public ForMemberProfile()
    {
        CreateMap<ForMemberSource, ForMemberDest>()
            .ForMember(d => d.FullName, opt => opt.MapFrom(s => s.First + " " + s.Last));
    }
}

// Multi ForMember Profile
public class ProfileMultiSource
{
    public int A { get; set; }
    public int B { get; set; }
    public int C { get; set; }
}

public class ProfileMultiDest
{
    public int X { get; set; }
    public int Y { get; set; }
    public string Z { get; set; } = string.Empty;
}

public class MultiForMemberProfile : Profile
{
    public MultiForMemberProfile()
    {
        CreateMap<ProfileMultiSource, ProfileMultiDest>()
            .ForMember(d => d.X, opt => opt.MapFrom(s => s.A + 1))
            .ForMember(d => d.Y, opt => opt.MapFrom(s => s.B * 2))
            .ForMember(d => d.Z, opt => opt.MapFrom(s => s.C.ToString()));
    }
}

// ReverseMap Profile
public class ProfileReverseSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ProfileReverseDest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ProfileReverseMapProfile : Profile
{
    public ProfileReverseMapProfile()
    {
        CreateMap<ProfileReverseSource, ProfileReverseDest>()
            .ReverseMap();
    }
}

// ConvertUsing Profile
public class ProfileConverterSource
{
    public decimal Amount { get; set; }
}

public class ProfileConverterDest
{
    public string FormattedAmount { get; set; } = string.Empty;
}

public class ProfileAmountConverter : ITypeConverter<ProfileConverterSource, ProfileConverterDest>
{
    public ProfileConverterDest Convert(ProfileConverterSource source, ProfileConverterDest destination, ResolutionContext context)
    {
        return new ProfileConverterDest { FormattedAmount = $"${source.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}" };
    }
}

public class ProfileConverterProfile : Profile
{
    public ProfileConverterProfile()
    {
        CreateMap<ProfileConverterSource, ProfileConverterDest>()
            .ConvertUsing(new ProfileAmountConverter());
    }
}

// Type Converter Profile
public class TypeConverterSource
{
    public int Value { get; set; }
}

public class TypeConverterDest
{
    public string Result { get; set; } = string.Empty;
}

public class ValueToResultConverter : ITypeConverter<TypeConverterSource, TypeConverterDest>
{
    public TypeConverterDest Convert(TypeConverterSource source, TypeConverterDest destination, ResolutionContext context)
    {
        return new TypeConverterDest { Result = $"Converted: {source.Value}" };
    }
}

public class TypeConverterProfile : Profile
{
    public TypeConverterProfile()
    {
        CreateMap<TypeConverterSource, TypeConverterDest>()
            .ConvertUsing<ValueToResultConverter>();
    }
}

// Ignore Profile
public class IgnoreSource
{
    public string Keep { get; set; } = string.Empty;
    public string Skip { get; set; } = string.Empty;
}

public class IgnoreDest
{
    public string Keep { get; set; } = string.Empty;
    public string? Skip { get; set; }
}

public class IgnoreProfile : Profile
{
    public IgnoreProfile()
    {
        CreateMap<IgnoreSource, IgnoreDest>()
            .ForMember(d => d.Skip, opt => opt.Ignore());
    }
}

// Nested Profile
public class ChildProfileTestSource
{
    public string ChildName { get; set; } = string.Empty;
}

public class ChildProfileTestDest
{
    public string ChildName { get; set; } = string.Empty;
}

public class NestedProfileTestSource
{
    public string Name { get; set; } = string.Empty;
    public ChildProfileTestSource? Child { get; set; }
}

public class NestedProfileTestDest
{
    public string Name { get; set; } = string.Empty;
    public ChildProfileTestDest? Child { get; set; }
}

public class ProfileNestedProfile : Profile
{
    public ProfileNestedProfile()
    {
        CreateMap<ChildProfileTestSource, ChildProfileTestDest>();
        CreateMap<NestedProfileTestSource, NestedProfileTestDest>();
    }
}

// Multi Map Profile
public class Cat
{
    public string Name { get; set; } = string.Empty;
    public bool Meows { get; set; }
}

public class CatDto
{
    public string Name { get; set; } = string.Empty;
    public bool Meows { get; set; }
}

public class Dog
{
    public string Name { get; set; } = string.Empty;
    public bool Barks { get; set; }
}

public class DogDto
{
    public string Name { get; set; } = string.Empty;
    public bool Barks { get; set; }
}

public class MultiMapProfile : Profile
{
    public MultiMapProfile()
    {
        CreateMap<Cat, CatDto>();
        CreateMap<Dog, DogDto>();
    }
}

#endregion
