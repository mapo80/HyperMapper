using Xunit;

namespace HyperMapper.Tests;

/// <summary>
/// Extended tests for Mapper functionality to achieve 90%+ coverage.
/// </summary>
public class MapperExtendedTests
{
    #region Array Mapping Tests

    [Fact]
    public void Map_Array_MapsToArray()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ExtendedTestProfile>());
        var mapper = config.CreateMapper();

        var sources = new SimpleSource[]
        {
            new() { Id = 1, Value = "A" },
            new() { Id = 2, Value = "B" }
        };

        var result = mapper.Map<SimpleDest[]>(sources);

        Assert.Equal(2, result.Length);
        Assert.Equal("A", result[0].Value);
        Assert.Equal("B", result[1].Value);
    }

    [Fact]
    public void Map_CollectionWithNullItems_HandlesNulls()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ExtendedTestProfile>());
        var mapper = config.CreateMapper();

        var sources = new List<SimpleSource?> { new() { Id = 1, Value = "A" }, null, new() { Id = 2, Value = "B" } };

        var result = mapper.Map<List<SimpleDest>>(sources);

        Assert.Equal(3, result.Count);
        Assert.NotNull(result[0]);
        Assert.Null(result[1]);
        Assert.NotNull(result[2]);
    }

    #endregion

    #region Type Conversion Tests

    [Fact]
    public void Map_EnumToInt_ConvertsCorrectly()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ExtendedTestProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithEnum { Status = TestStatus.Active };
        var dest = mapper.Map<DestWithInt>(source);

        Assert.Equal(1, dest.Status);
    }

    [Fact]
    public void Map_IntToEnum_ConvertsCorrectly()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ExtendedTestProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithInt { Status = 2 };
        var dest = mapper.Map<DestWithEnum>(source);

        Assert.Equal(TestStatus.Inactive, dest.Status);
    }

    [Fact]
    public void Map_DateTimeToDateOnly_ConvertsCorrectly()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ExtendedTestProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithDateTime { Date = new DateTime(2024, 6, 15, 10, 30, 0) };
        var dest = mapper.Map<DestWithDateOnly>(source);

        Assert.Equal(new DateOnly(2024, 6, 15), dest.Date);
    }

    [Fact]
    public void Map_DateOnlyToDateTime_ConvertsCorrectly()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ExtendedTestProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithDateOnly { Date = new DateOnly(2024, 6, 15) };
        var dest = mapper.Map<DestWithDateTime>(source);

        Assert.Equal(new DateTime(2024, 6, 15, 0, 0, 0), dest.Date);
    }

    [Fact]
    public void Map_ToNullableType_HandlesConversion()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ExtendedTestProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithValue { Value = 42 };
        var dest = mapper.Map<DestWithNullable>(source);

        Assert.Equal(42, dest.Value);
    }

    [Fact]
    public void Map_ToStringType_ConvertsCorrectly()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ExtendedTestProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithInt { Status = 123 };
        var dest = mapper.Map<ExtDestWithString>(source);

        Assert.Equal("123", dest.Status);
    }

    #endregion

    #region Map Overload Tests

    [Fact]
    public void Map_ObjectOverload_MapsCorrectly()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ExtendedTestProfile>());
        var mapper = config.CreateMapper();

        var source = new SimpleSource { Id = 1, Value = "Test" };
        var result = mapper.Map(source, typeof(SimpleSource), typeof(SimpleDest));

        Assert.IsType<SimpleDest>(result);
        Assert.Equal("Test", ((SimpleDest)result).Value);
    }

    [Fact]
    public void Map_ObjectToExistingOverload_UpdatesDestination()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ExtendedTestProfile>());
        var mapper = config.CreateMapper();

        var source = new SimpleSource { Id = 5, Value = "Updated" };
        var dest = new SimpleDest { Id = 1, Value = "Original" };

        var result = mapper.Map(source, dest, typeof(SimpleSource), typeof(SimpleDest));

        Assert.Same(dest, result);
        Assert.Equal(5, dest.Id);
        Assert.Equal("Updated", dest.Value);
    }

    [Fact]
    public void Map_GenericToExisting_NullSource_ReturnsDestination()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ExtendedTestProfile>());
        var mapper = config.CreateMapper();

        SimpleSource? source = null;
        var dest = new SimpleDest { Id = 1, Value = "Original" };

        var result = mapper.Map(source, dest);

        Assert.Same(dest, result);
        Assert.Equal("Original", dest.Value);
    }

    #endregion

    #region Convention Mapping Tests

    [Fact]
    public void Map_WithoutProfile_UsesConventionMapping()
    {
        var config = new MapperConfiguration(cfg => { });
        var mapper = config.CreateMapper();

        var source = new SimpleSource { Id = 1, Value = "Convention" };
        var dest = mapper.Map<SimpleDest>(source);

        Assert.Equal(1, dest.Id);
        Assert.Equal("Convention", dest.Value);
    }

    [Fact]
    public void Map_CaseInsensitivePropertyMatching_Works()
    {
        var config = new MapperConfiguration(cfg => { });
        var mapper = config.CreateMapper();

        var source = new LowerCaseSource { id = 1, value = "Test" };
        var dest = mapper.Map<UpperCaseDest>(source);

        Assert.Equal(1, dest.ID);
        Assert.Equal("Test", dest.VALUE);
    }

    [Fact]
    public void Map_NullPropertyValue_SetsNullOnNullableDestination()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ExtendedTestProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithNullable { Value = null };
        var dest = mapper.Map<DestWithNullable>(source);

        Assert.Null(dest.Value);
    }

    #endregion

    #region IReadOnlyList and IReadOnlyCollection Tests

    [Fact]
    public void Map_IReadOnlyList_MapsCorrectly()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ExtendedTestProfile>());
        var mapper = config.CreateMapper();

        IReadOnlyList<SimpleSource> sources = new List<SimpleSource>
        {
            new() { Id = 1, Value = "A" },
            new() { Id = 2, Value = "B" }
        };

        var result = mapper.Map<IReadOnlyList<SimpleDest>>(sources);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Map_IReadOnlyCollection_MapsCorrectly()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ExtendedTestProfile>());
        var mapper = config.CreateMapper();

        IReadOnlyCollection<SimpleSource> sources = new List<SimpleSource>
        {
            new() { Id = 1, Value = "A" }
        };

        var result = mapper.Map<IReadOnlyCollection<SimpleDest>>(sources);

        Assert.Single(result);
    }

    [Fact]
    public void Map_ICollection_MapsCorrectly()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ExtendedTestProfile>());
        var mapper = config.CreateMapper();

        ICollection<SimpleSource> sources = new List<SimpleSource>
        {
            new() { Id = 1, Value = "A" }
        };

        var result = mapper.Map<ICollection<SimpleDest>>(sources);

        Assert.Single(result);
    }

    [Fact]
    public void Map_IList_MapsCorrectly()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ExtendedTestProfile>());
        var mapper = config.CreateMapper();

        IList<SimpleSource> sources = new List<SimpleSource>
        {
            new() { Id = 1, Value = "A" }
        };

        var result = mapper.Map<IList<SimpleDest>>(sources);

        Assert.Single(result);
    }

    #endregion

    #region MapFrom with Two Parameters Tests

    [Fact]
    public void Map_MapFromWithTwoParams_Works()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<TwoParamMapFromProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new SimpleSource { Id = 1, Value = "Test" };
        var dest = mapper.Map<DestWithComputed>(source);

        Assert.Equal("Test-1", dest.Computed);
    }

    #endregion

    #region Simple Type Detection Tests

    [Fact]
    public void Map_GuidProperty_MapsCorrectly()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ExtendedTestProfile>());
        var mapper = config.CreateMapper();

        var guid = Guid.NewGuid();
        var source = new SourceWithGuid { Id = guid };
        var dest = mapper.Map<DestWithGuid>(source);

        Assert.Equal(guid, dest.Id);
    }

    [Fact]
    public void Map_TimeSpanProperty_MapsCorrectly()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ExtendedTestProfile>());
        var mapper = config.CreateMapper();

        var span = TimeSpan.FromHours(2);
        var source = new SourceWithTimeSpan { Duration = span };
        var dest = mapper.Map<DestWithTimeSpan>(source);

        Assert.Equal(span, dest.Duration);
    }

    [Fact]
    public void Map_DecimalProperty_MapsCorrectly()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ExtendedTestProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithDecimal { Amount = 123.45m };
        var dest = mapper.Map<DestWithDecimal>(source);

        Assert.Equal(123.45m, dest.Amount);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Map_ReadOnlyDestinationProperty_IsSkipped()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ExtendedTestProfile>());
        var mapper = config.CreateMapper();

        var source = new SimpleSource { Id = 1, Value = "Test" };
        var dest = mapper.Map<DestWithReadOnly>(source);

        Assert.Equal(1, dest.Id);
        Assert.Equal("ReadOnly", dest.ReadOnlyValue); // Default value, not mapped
    }

    [Fact]
    public void Map_ChangeType_Fallback_Works()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ExtendedTestProfile>());
        var mapper = config.CreateMapper();

        var source = new SourceWithLong { Value = 42L };
        var dest = mapper.Map<DestWithIntFromLong>(source);

        Assert.Equal(42, dest.Value);
    }

    #endregion

    #region Primitive Collection Edge Cases Tests

    [Fact]
    public void Map_LongArrayToIntArray_ConvertsCorrectly()
    {
        var config = new MapperConfiguration(cfg => { });
        var mapper = config.CreateMapper();

        var sources = new long[] { 1L, 2L, 3L };
        var dests = mapper.Map<int[]>(sources);

        Assert.Equal(3, dests.Length);
        Assert.Equal(new[] { 1, 2, 3 }, dests);
    }

    [Fact]
    public void Map_ShortArrayToLongArray_ConvertsCorrectly()
    {
        var config = new MapperConfiguration(cfg => { });
        var mapper = config.CreateMapper();

        short[] sources = new short[] { 1, 2, 3 };
        var dests = mapper.Map<long[]>(sources);

        Assert.Equal(3, dests.Length);
        Assert.Equal(new long[] { 1L, 2L, 3L }, dests);
    }

    [Fact]
    public void Map_ByteArrayToIntArray_ConvertsCorrectly()
    {
        var config = new MapperConfiguration(cfg => { });
        var mapper = config.CreateMapper();

        byte[] sources = new byte[] { 1, 2, 3 };
        var dests = mapper.Map<int[]>(sources);

        Assert.Equal(3, dests.Length);
        Assert.Equal(new[] { 1, 2, 3 }, dests);
    }

    [Fact]
    public void Map_EnumArrayToIntArray_ConvertsCorrectly()
    {
        var config = new MapperConfiguration(cfg => { });
        var mapper = config.CreateMapper();

        var sources = new TestStatus[] { TestStatus.Active, TestStatus.Inactive };
        var dests = mapper.Map<int[]>(sources);

        Assert.Equal(2, dests.Length);
        Assert.Equal(new[] { 1, 2 }, dests);
    }

    [Fact]
    public void Map_IntArrayToEnumArray_ConvertsCorrectly()
    {
        var config = new MapperConfiguration(cfg => { });
        var mapper = config.CreateMapper();

        var sources = new int[] { 1, 2 };
        var dests = mapper.Map<TestStatus[]>(sources);

        Assert.Equal(2, dests.Length);
        Assert.Equal(new[] { TestStatus.Active, TestStatus.Inactive }, dests);
    }

    [Fact]
    public void Map_StringArrayToEnumArray_ConvertsCorrectly()
    {
        var config = new MapperConfiguration(cfg => { });
        var mapper = config.CreateMapper();

        var sources = new string[] { "Active", "Inactive" };
        var dests = mapper.Map<TestStatus[]>(sources);

        Assert.Equal(2, dests.Length);
        Assert.Equal(new[] { TestStatus.Active, TestStatus.Inactive }, dests);
    }

    [Fact]
    public void Map_EnumArrayToStringArray_ConvertsCorrectly()
    {
        var config = new MapperConfiguration(cfg => { });
        var mapper = config.CreateMapper();

        var sources = new TestStatus[] { TestStatus.Active, TestStatus.Unknown };
        var dests = mapper.Map<string[]>(sources);

        Assert.Equal(2, dests.Length);
        Assert.Equal(new[] { "Active", "Unknown" }, dests);
    }

    [Fact]
    public void Map_IntArrayToStringArray_ConvertsCorrectly()
    {
        var config = new MapperConfiguration(cfg => { });
        var mapper = config.CreateMapper();

        var sources = new int[] { 1, 2, 3 };
        var dests = mapper.Map<string[]>(sources);

        Assert.Equal(3, dests.Length);
        Assert.Equal(new[] { "1", "2", "3" }, dests);
    }

    [Fact]
    public void Map_NullableIntArrayToIntArray_ConvertsCorrectly()
    {
        var config = new MapperConfiguration(cfg => { });
        var mapper = config.CreateMapper();

        var sources = new int?[] { 1, 2, null, 3 };
        var dests = mapper.Map<int?[]>(sources);

        Assert.Equal(4, dests.Length);
        Assert.Equal(1, dests[0]);
        Assert.Equal(2, dests[1]);
        Assert.Null(dests[2]);
        Assert.Equal(3, dests[3]);
    }

    [Fact]
    public void Map_DecimalArray_ConvertsCorrectly()
    {
        var config = new MapperConfiguration(cfg => { });
        var mapper = config.CreateMapper();

        var sources = new decimal[] { 1.1m, 2.2m, 3.3m };
        var dests = mapper.Map<decimal[]>(sources);

        Assert.Equal(3, dests.Length);
        Assert.Equal(sources, dests);
    }

    [Fact]
    public void Map_GuidList_ConvertsCorrectly()
    {
        var config = new MapperConfiguration(cfg => { });
        var mapper = config.CreateMapper();

        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var sources = new List<Guid> { guid1, guid2 };
        var dests = mapper.Map<List<Guid>>(sources);

        Assert.Equal(2, dests.Count);
        Assert.Equal(sources, dests);
    }

    [Fact]
    public void Map_DateTimeList_ConvertsCorrectly()
    {
        var config = new MapperConfiguration(cfg => { });
        var mapper = config.CreateMapper();

        var date1 = DateTime.Now;
        var date2 = DateTime.Now.AddDays(1);
        var sources = new List<DateTime> { date1, date2 };
        var dests = mapper.Map<List<DateTime>>(sources);

        Assert.Equal(2, dests.Count);
        Assert.Equal(sources, dests);
    }

    [Fact]
    public void Map_StringToEnum_InvalidValue_ReturnsDefault()
    {
        var config = new MapperConfiguration(cfg => { });
        var mapper = config.CreateMapper();

        var sources = new string[] { "InvalidValue" };
        var dests = mapper.Map<TestStatus[]>(sources);

        Assert.Single(dests);
        Assert.Equal(TestStatus.Unknown, dests[0]); // Default (0) value
    }

    [Fact]
    public void Map_UIntToLong_ConvertsCorrectly()
    {
        var config = new MapperConfiguration(cfg => { });
        var mapper = config.CreateMapper();

        var sources = new uint[] { 1, 2, 3 };
        var dests = mapper.Map<long[]>(sources);

        Assert.Equal(3, dests.Length);
        Assert.Equal(new long[] { 1, 2, 3 }, dests);
    }

    [Fact]
    public void Map_SByteToInt_ConvertsCorrectly()
    {
        var config = new MapperConfiguration(cfg => { });
        var mapper = config.CreateMapper();

        sbyte[] sources = new sbyte[] { 1, -2, 3 };
        var dests = mapper.Map<int[]>(sources);

        Assert.Equal(3, dests.Length);
        Assert.Equal(new[] { 1, -2, 3 }, dests);
    }

    [Fact]
    public void Map_UShortToInt_ConvertsCorrectly()
    {
        var config = new MapperConfiguration(cfg => { });
        var mapper = config.CreateMapper();

        ushort[] sources = new ushort[] { 1, 2, 3 };
        var dests = mapper.Map<int[]>(sources);

        Assert.Equal(3, dests.Length);
        Assert.Equal(new[] { 1, 2, 3 }, dests);
    }

    [Fact]
    public void Map_ULongToLong_ConvertsCorrectly()
    {
        var config = new MapperConfiguration(cfg => { });
        var mapper = config.CreateMapper();

        ulong[] sources = new ulong[] { 1, 2, 3 };
        var dests = mapper.Map<long[]>(sources);

        Assert.Equal(3, dests.Length);
        Assert.Equal(new long[] { 1, 2, 3 }, dests);
    }

    #endregion

    #region Null Source Handling Tests

    [Fact]
    public void Map_NullSource_WithExplicitTypes_ReturnsDefault()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ExtendedTestProfile>());
        var mapper = config.CreateMapper();

        SimpleSource? source = null;
        var result = mapper.Map<SimpleSource, SimpleDest>(source!);

        Assert.Null(result);
    }

    #endregion
}

#region Test Classes

public class SimpleSource
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
}

public class SimpleDest
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
}

public enum TestStatus
{
    Unknown = 0,
    Active = 1,
    Inactive = 2
}

public class SourceWithEnum
{
    public TestStatus Status { get; set; }
}

public class DestWithInt
{
    public int Status { get; set; }
}

public class SourceWithInt
{
    public int Status { get; set; }
}

public class DestWithEnum
{
    public TestStatus Status { get; set; }
}

public class SourceWithDateTime
{
    public DateTime Date { get; set; }
}

public class DestWithDateOnly
{
    public DateOnly Date { get; set; }
}

public class SourceWithDateOnly
{
    public DateOnly Date { get; set; }
}

public class DestWithDateTime
{
    public DateTime Date { get; set; }
}

public class SourceWithValue
{
    public int Value { get; set; }
}

public class SourceWithNullable
{
    public int? Value { get; set; }
}

public class DestWithNullable
{
    public int? Value { get; set; }
}

public class ExtDestWithString
{
    public string Status { get; set; } = string.Empty;
}

public class LowerCaseSource
{
    public int id { get; set; }
    public string value { get; set; } = string.Empty;
}

public class UpperCaseDest
{
    public int ID { get; set; }
    public string VALUE { get; set; } = string.Empty;
}

public class SourceWithGuid
{
    public Guid Id { get; set; }
}

public class DestWithGuid
{
    public Guid Id { get; set; }
}

public class SourceWithTimeSpan
{
    public TimeSpan Duration { get; set; }
}

public class DestWithTimeSpan
{
    public TimeSpan Duration { get; set; }
}

public class SourceWithDecimal
{
    public decimal Amount { get; set; }
}

public class DestWithDecimal
{
    public decimal Amount { get; set; }
}

public class DestWithComputed
{
    public int Id { get; set; }
    public string Computed { get; set; } = string.Empty;
}

public class DestWithReadOnly
{
    public int Id { get; set; }
    public string ReadOnlyValue { get; } = "ReadOnly";
}

public class SourceWithLong
{
    public long Value { get; set; }
}

public class DestWithIntFromLong
{
    public int Value { get; set; }
}

#endregion

#region Test Profiles

public class ExtendedTestProfile : Profile
{
    public ExtendedTestProfile()
    {
        CreateMap<SimpleSource, SimpleDest>();
        CreateMap<SourceWithEnum, DestWithInt>();
        CreateMap<SourceWithInt, DestWithEnum>();
        CreateMap<SourceWithInt, ExtDestWithString>();
        CreateMap<SourceWithDateTime, DestWithDateOnly>();
        CreateMap<SourceWithDateOnly, DestWithDateTime>();
        CreateMap<SourceWithValue, DestWithNullable>();
        CreateMap<SourceWithNullable, DestWithNullable>();
        CreateMap<SourceWithGuid, DestWithGuid>();
        CreateMap<SourceWithTimeSpan, DestWithTimeSpan>();
        CreateMap<SourceWithDecimal, DestWithDecimal>();
        CreateMap<SimpleSource, DestWithReadOnly>();
        CreateMap<SourceWithLong, DestWithIntFromLong>();
    }
}

public class TwoParamMapFromProfile : Profile
{
    public TwoParamMapFromProfile()
    {
        CreateMap<SimpleSource, DestWithComputed>()
            .ForMember(d => d.Computed, opt => opt.MapFrom((src, dest) => $"{src.Value}-{src.Id}"));
    }
}

#endregion
