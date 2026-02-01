using BenchmarkDotNet.Attributes;
using HyperMapper.Benchmarks.Models;
using HyperMapper.Benchmarks.Profiles;

namespace HyperMapper.Benchmarks.Benchmarks;

/// <summary>
/// Complex object benchmark - nullable, enum, DateTime, collections, nested objects
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class ComplexObjectBenchmark
{
    private ComplexSource _source = null!;
    private ComplexSource _sourceWithNulls = null!;

    private HyperMapper.IMapper _linksMapper = null!;
    private HyperMapper.IMapper _linksMapperCodeGen = null!;
    private AutoMapper.IMapper _autoMapper = null!;

    [GlobalSetup]
    public void Setup()
    {
        _source = new ComplexSource
        {
            Id = 1,
            Name = "Complex Object",
            Description = "A complex object with all properties set",
            Status = ComplexStatus.Active,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now.AddDays(1),
            Price = 99.99m,
            OptionalQuantity = 10,
            Tags = new List<string> { "tag1", "tag2", "tag3" },
            Address = new ComplexAddressSource
            {
                Street = "123 Main St",
                City = "Test City",
                PostalCode = "12345",
                Country = "Test Country"
            }
        };

        _sourceWithNulls = new ComplexSource
        {
            Id = 2,
            Name = "Sparse Object",
            Description = null,
            Status = ComplexStatus.Draft,
            CreatedAt = DateTime.Now,
            UpdatedAt = null,
            Price = 0m,
            OptionalQuantity = null,
            Tags = new List<string>(),
            Address = null
        };

        // HyperMapper Runtime setup
        var linksConfig = new HyperMapper.MapperConfiguration(cfg =>
        {
            cfg.AddProfile<HyperMapperComplexProfile>();
        });
        _linksMapper = linksConfig.CreateMapper();

        // HyperMapper CodeGen setup
        var linksCodeGenConfig = new HyperMapper.MapperConfiguration(cfg =>
        {
            cfg.AddProfile<HyperMapperComplexProfile>();
        });
        HyperMapper.Generated.HyperMapperGeneratedRegistry.Initialize(linksCodeGenConfig);
        _linksMapperCodeGen = linksCodeGenConfig.CreateMapper();

        // AutoMapper setup
        var autoConfig = new AutoMapper.MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AutoComplexProfile>();
        });
        _autoMapper = autoConfig.CreateMapper();
    }

    // Full object mapping

    [Benchmark(Baseline = true)]
    public ComplexDestination Manual_Full()
    {
        return new ComplexDestination
        {
            Id = _source.Id,
            Name = _source.Name,
            Description = _source.Description,
            Status = _source.Status,
            CreatedAt = _source.CreatedAt,
            UpdatedAt = _source.UpdatedAt,
            Price = _source.Price,
            OptionalQuantity = _source.OptionalQuantity,
            Tags = new List<string>(_source.Tags),
            Address = _source.Address != null ? new ComplexAddressDestination
            {
                Street = _source.Address.Street,
                City = _source.Address.City,
                PostalCode = _source.Address.PostalCode,
                Country = _source.Address.Country
            } : null
        };
    }

    [Benchmark]
    public ComplexDestination HyperMapper_Full()
    {
        return _linksMapper.Map<ComplexSource, ComplexDestination>(_source);
    }

    [Benchmark]
    public ComplexDestination HyperMapper_CodeGen_Full()
    {
        return _linksMapperCodeGen.Map<ComplexSource, ComplexDestination>(_source);
    }

    [Benchmark]
    public ComplexDestination AutoMapper_Full()
    {
        return _autoMapper.Map<ComplexSource, ComplexDestination>(_source);
    }

    // Sparse object with nulls

    [Benchmark]
    public ComplexDestination Manual_WithNulls()
    {
        return new ComplexDestination
        {
            Id = _sourceWithNulls.Id,
            Name = _sourceWithNulls.Name,
            Description = _sourceWithNulls.Description,
            Status = _sourceWithNulls.Status,
            CreatedAt = _sourceWithNulls.CreatedAt,
            UpdatedAt = _sourceWithNulls.UpdatedAt,
            Price = _sourceWithNulls.Price,
            OptionalQuantity = _sourceWithNulls.OptionalQuantity,
            Tags = new List<string>(_sourceWithNulls.Tags),
            Address = _sourceWithNulls.Address != null ? new ComplexAddressDestination
            {
                Street = _sourceWithNulls.Address.Street,
                City = _sourceWithNulls.Address.City,
                PostalCode = _sourceWithNulls.Address.PostalCode,
                Country = _sourceWithNulls.Address.Country
            } : null
        };
    }

    [Benchmark]
    public ComplexDestination HyperMapper_WithNulls()
    {
        return _linksMapper.Map<ComplexSource, ComplexDestination>(_sourceWithNulls);
    }

    [Benchmark]
    public ComplexDestination HyperMapper_CodeGen_WithNulls()
    {
        return _linksMapperCodeGen.Map<ComplexSource, ComplexDestination>(_sourceWithNulls);
    }

    [Benchmark]
    public ComplexDestination AutoMapper_WithNulls()
    {
        return _autoMapper.Map<ComplexSource, ComplexDestination>(_sourceWithNulls);
    }
}
