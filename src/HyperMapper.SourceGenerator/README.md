# HyperMapper.CodeGen - Source Generator

**Compile-time code generation for HyperMapper** - Zero reflection, maximum performance.

## Overview

HyperMapper.CodeGen is a Roslyn Source Generator that generates optimized mapping code at compile time. It analyzes your mapping profiles and produces static methods that are **up to 5.4x faster** than AutoMapper and **4.6x faster** than HyperMapper's runtime mode.

## Installation

### 1. Install the core library

```bash
dotnet add package HyperMapper.Core
```

### 2. Install the Source Generator

```bash
dotnet add package HyperMapper.CodeGen
```

Or add to your `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="HyperMapper.Core" Version="12.1.2" />
  <PackageReference Include="HyperMapper.CodeGen" Version="12.1.2" />
</ItemGroup>
```

## Quick Start

### 1. Define your models and profile

```csharp
using HyperMapper;

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; }
}

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(d => d.FullName, opt => opt.MapFrom(s => $"{s.FirstName} {s.LastName}"));
    }
}
```

### 2. Configure HyperMapper

```csharp
var config = new MapperConfiguration(cfg =>
{
    cfg.AddProfile<UserProfile>();
});

var mapper = config.CreateMapper();
```

### 3. Build your project

The Source Generator will automatically create a `Mapper.g.cs` file with optimized mapping methods.

### 4. Use the generated mapper

```csharp
// Runtime mode (slower, more flexible)
var dto1 = mapper.Map<UserDto>(user);

// CodeGen mode (faster, recommended for production)
var dto2 = HyperMapper.Generated.Mapper.Map<User, UserDto>(user);
```

## Features

### ✅ Supported Features (CodeGen Mode)

All standard mapping features work in CodeGen mode:

- **Property mapping** - Direct property-to-property
- **Custom expressions** - `MapFrom(s => s.Property)`
- **Nested objects** - Automatic recursive mapping
- **Collections** - List, Array, Dictionary, HashSet, etc.
- **Flattening** - `source.Address.City` → `dest.City`
- **Type conversions** - String ↔ Enum, numeric conversions
- **Null handling** - Automatic null checks
- **ReverseMap** - Bidirectional mappings
- **Value Resolvers** - `MapFrom<TValueResolver>()`
- **Type Converters** - Class-based converters
- **Conditions** - `Condition()` and `PreCondition()`
- **Transformations** - `AddTransform<T>()`
- **External types** - Types from NuGet packages (v12.1.x)
- **Ambiguous classes** - Auto-qualified `Path`, `File`, `Math` (v12.1.x)
- **Base class properties** - Inherited property resolution (v12.1.x)
- **Lambda parameters** - Ternary, null coalescing operators (v12.1.x)

### ⚠️ Runtime-Only Features

These features fall back to Runtime mode:

- **BeforeMap/AfterMap** - Requires runtime hooks
- **ITypeConverter instances** - Needs pre-instantiated converters
- **ConstructUsing instances** - Needs factory instances

## Performance Comparison

**Environment**: macOS, Apple M2 Pro, .NET 8.0

| Scenario | Manual | HyperMapper CodeGen | HyperMapper Runtime | AutoMapper |
|----------|-------:|--------------------:|--------------------:|-----------:|
| Simple Mapping | 22 ns | **32 ns** ✅ | 147 ns | 170 ns |
| Complex Object | 161 ns | **149 ns** ✅ | 295 ns | 370 ns |
| Collection (1000) | 18,914 ns | **29,935 ns** ✅ | 34,548 ns | 37,402 ns |
| Flattening | 51 ns | **75 ns** ✅ | 161 ns | 202 ns |

**CodeGen is 1.4x to 5.4x faster across all scenarios!**

## Advanced Configuration

### Enable generated file inspection (debugging)

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)\Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

Generated files will be written to `obj/Generated/HyperMapper.SourceGenerator/`.

### Using external types (v12.1.x)

Types from external assemblies are fully supported:

```csharp
using Microsoft.EntityFrameworkCore;

CreateMap<EntitySource, EntityDto>()
    .ForMember(d => d.State, opt => opt.MapFrom(s => s.EntityState))
    .ForMember(d => d.Extension, opt => opt.MapFrom(s => Path.GetExtension(s.FilePath)));
```

### Ambiguous static classes (v12.1.x)

Common static classes are automatically qualified to prevent conflicts:

```csharp
CreateMap<FileSource, FileDto>()
    .ForMember(d => d.Extension, opt => opt.MapFrom(s => Path.GetExtension(s.Path)))
    .ForMember(d => d.Rounded, opt => opt.MapFrom(s => Math.Round(s.Value, 2)))
    .ForMember(d => d.IntValue, opt => opt.MapFrom(s => Convert.ToInt32(s.StringValue)));
```

Auto-qualified: `Path`, `File`, `Directory`, `Math`, `Convert`, `Encoding`, `Environment`

### Base class properties (v12.1.x)

Properties from base classes are automatically resolved:

```csharp
public class BaseEntity
{
    public int Id { get; set; }
}

public class DerivedEntity : BaseEntity
{
    public string Name { get; set; }
}

CreateMap<DerivedEntity, EntityDto>()
    .ForMember(d => d.EntityId, opt => opt.MapFrom(s => s.Id)); // ✅ Id from base class
```

## How It Works

1. **Compile-time analysis** - The Source Generator analyzes your `Profile` classes during compilation
2. **Code generation** - Generates static mapping methods in `Mapper.g.cs`
3. **Zero reflection** - Direct property assignments, no runtime type inspection
4. **AOT-ready** - Works with Native AOT compilation

## Requirements

- **.NET 8.0+** (or .NET Standard 2.0 for the generator itself)
- **C# 11+** (for latest features)
- **HyperMapper.Core 12.1.2+**

## Troubleshooting

### "No generated code found"

Make sure you have both packages installed and your project has been built at least once.

### "Type not found in generated code"

Check that your mapping profile is properly configured with `CreateMap<TSource, TDest>()`.

### "Feature not working in CodeGen"

Some features like `BeforeMap`/`AfterMap` require Runtime mode. Use the `IMapper` API for these cases.

## Documentation

Full documentation: https://github.com/mapo80/HyperMapper

## License

MIT License - see [LICENSE](https://github.com/mapo80/HyperMapper/blob/main/LICENSE)

## Changelog

### v12.1.2
- External assembly type resolution
- Automatic qualification of ambiguous static classes
- Base class property resolution
- Standalone lambda parameter support

### v12.0.1
- IValueResolver support with full AutoMapper compatibility
- Class-based ITypeConverter
- Complex LINQ expression support

### v11.0.1
- Initial Source Generator release
- Basic mapping code generation
- Collection and nested object support
