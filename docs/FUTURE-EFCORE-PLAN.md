# HyperMapper.EFCore - Piano Futuro

> **NOTA**: Questo piano è per una implementazione futura. ProjectTo richiede dipendenze EF Core.
> Per evitare dipendenze non necessarie nel pacchetto base, questa funzionalità sarà implementata
> in un **pacchetto separato**: `HyperMapper.EFCore`

---

## Struttura del Nuovo Pacchetto

```
api/HyperMapper/
├── src/
│   ├── HyperMapper/                    # Pacchetto base (senza dipendenze EF Core)
│   └── HyperMapper.EFCore/             # NUOVO - Pacchetto EF Core
│       ├── HyperMapper.EFCore.csproj
│       ├── IConfigurationProvider.cs
│       ├── QueryableExtensions.cs
│       ├── ProjectionBuilder.cs
│       └── ExpressionVisitors/
│           ├── ProjectionExpressionBuilder.cs
│           └── MemberPathVisitor.cs
└── tests/
    └── HyperMapper.EFCore.Tests/       # Test separati per EF Core
```

---

## 1. Creazione Pacchetto HyperMapper.EFCore

**HyperMapper.EFCore.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <PackageId>HyperMapper.EFCore</PackageId>
    <Description>EF Core integration for HyperMapper - ProjectTo support</Description>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\HyperMapper\HyperMapper.csproj" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
  </ItemGroup>
</Project>
```

---

## 2. API IConfigurationProvider (in HyperMapper base)

Per permettere l'integrazione, aggiungiamo l'interfaccia base nel pacchetto principale:

**src/HyperMapper/IConfigurationProvider.cs:**
```csharp
namespace HyperMapper;

/// <summary>
/// Provides configuration for IQueryable projections.
/// Implemented by MapperConfiguration.
/// </summary>
public interface IConfigurationProvider
{
    /// <summary>
    /// Gets projection expression for transforming source to destination.
    /// Used by HyperMapper.EFCore for ProjectTo support.
    /// </summary>
    Expression<Func<TSource, TDestination>>? GetProjectionExpression<TSource, TDestination>();
}
```

**Modifiche a MapperConfiguration.cs:**
```csharp
public class MapperConfiguration : IConfigurationProvider
{
    public Expression<Func<TSource, TDestination>>? GetProjectionExpression<TSource, TDestination>()
    {
        var typeMap = _registry.FindTypeMap(typeof(TSource), typeof(TDestination));
        if (typeMap == null) return null;

        // Build projection expression from TypeMap
        return ProjectionExpressionBuilder.Build<TSource, TDestination>(typeMap);
    }
}
```

---

## 3. ProjectTo Implementation (in HyperMapper.EFCore)

**QueryableExtensions.cs:**
```csharp
namespace HyperMapper.EFCore;

public static class QueryableExtensions
{
    public static IQueryable<TDestination> ProjectTo<TDestination>(
        this IQueryable source,
        IConfigurationProvider configurationProvider,
        object? parameters = null,
        params Expression<Func<TDestination, object>>[] membersToExpand)
    {
        var sourceType = source.ElementType;
        var method = typeof(QueryableExtensions)
            .GetMethod(nameof(ProjectToCore), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(sourceType, typeof(TDestination));

        return (IQueryable<TDestination>)method.Invoke(null,
            new object?[] { source, configurationProvider, parameters, membersToExpand })!;
    }

    private static IQueryable<TDestination> ProjectToCore<TSource, TDestination>(
        IQueryable<TSource> source,
        IConfigurationProvider configurationProvider,
        object? parameters,
        Expression<Func<TDestination, object>>[] membersToExpand)
    {
        var projection = configurationProvider.GetProjectionExpression<TSource, TDestination>();
        if (projection == null)
            throw new InvalidOperationException(
                $"No mapping configured for {typeof(TSource).Name} -> {typeof(TDestination).Name}");

        return source.Select(projection);
    }
}
```

---

## 4. ProjectionExpressionBuilder

**ProjectionExpressionBuilder.cs:**
```csharp
namespace HyperMapper.EFCore.Internal;

internal static class ProjectionExpressionBuilder
{
    public static Expression<Func<TSource, TDestination>> Build<TSource, TDestination>(TypeMap typeMap)
    {
        var sourceParam = Expression.Parameter(typeof(TSource), "src");
        var memberBindings = new List<MemberBinding>();

        // Build member bindings from TypeMap
        foreach (var memberMap in typeMap.MemberMaps)
        {
            if (memberMap.Ignored) continue;

            var destProperty = typeof(TDestination).GetProperty(memberMap.DestinationMemberName);
            if (destProperty == null) continue;

            Expression sourceExpr;
            if (memberMap.SourceExpression != null)
            {
                // Use configured MapFrom expression
                sourceExpr = ReplaceParameter(memberMap.SourceExpression.Body,
                    memberMap.SourceExpression.Parameters[0], sourceParam);
            }
            else
            {
                // Convention: same-name property
                var sourceProperty = typeof(TSource).GetProperty(memberMap.DestinationMemberName);
                if (sourceProperty == null) continue;
                sourceExpr = Expression.Property(sourceParam, sourceProperty);
            }

            memberBindings.Add(Expression.Bind(destProperty, sourceExpr));
        }

        var newExpr = Expression.MemberInit(
            Expression.New(typeof(TDestination)),
            memberBindings);

        return Expression.Lambda<Func<TSource, TDestination>>(newExpr, sourceParam);
    }
}
```

---

## 5. Test (25 test) - in HyperMapper.EFCore.Tests

- ProjectTo_BasicProperties_Projects
- ProjectTo_NestedObject_Projects
- ProjectTo_Collection_Projects
- ProjectTo_WithMapFrom_UsesExpression
- ProjectTo_WithIgnore_ExcludesProperty
- ProjectTo_WithPreCondition_Conditional (limited support)
- ProjectTo_WithNullable_HandlesNull
- ProjectTo_MultiLevel_Works
- ProjectTo_ComplexExpression_Projects
- ProjectTo_EFCore_GeneratesCorrectSQL
- ProjectTo_InMemory_Works
- ProjectTo_CircularReference_Handles
- ProjectTo_Flattening_Works
- ProjectTo_ToList_Materializes
- ProjectTo_FirstOrDefault_Works
- ProjectTo_Count_Works
- ProjectTo_Where_CombinesExpressions
- ProjectTo_OrderBy_Works
- ProjectTo_Paging_Works
- ... (altri test)

---

## Riepilogo File da Creare/Modificare

### HyperMapper (base) - Modifiche

| File | Azione |
|------|--------|
| `IConfigurationProvider.cs` | NUOVO - interfaccia base per proiezioni |
| `MapperConfiguration.cs` | Implementare IConfigurationProvider |

### HyperMapper.EFCore (NUOVO pacchetto)

| File | Descrizione |
|------|-------------|
| `HyperMapper.EFCore.csproj` | Project file con dipendenza EF Core |
| `QueryableExtensions.cs` | Extension method ProjectTo<T> |
| `Internal/ProjectionExpressionBuilder.cs` | Costruisce Expression<Func<S,D>> da TypeMap |
| `Internal/MemberPathVisitor.cs` | Gestisce path complessi nelle espressioni |

### Test

| Progetto | Descrizione |
|----------|-------------|
| `HyperMapper.EFCore.Tests` | NUOVO - Test specifici EF Core con InMemory provider |

---

## Note Architetturali

### Vantaggi della Separazione EFCore

1. **Nessuna dipendenza EF Core nel pacchetto base** - Progetti che non usano EF Core non avranno dipendenze inutili
2. **Versioning indipendente** - HyperMapper.EFCore può seguire le versioni EF Core
3. **Opzionalità** - L'utente installa solo quello che serve
4. **Testing isolato** - I test EF Core richiedono provider specifici

### Uso da parte dell'utente

```csharp
// Solo mapping base
dotnet add package HyperMapper

// Con supporto EF Core
dotnet add package HyperMapper
dotnet add package HyperMapper.EFCore
```

```csharp
using HyperMapper;
using HyperMapper.EFCore; // Solo se serve ProjectTo

// Uso normale
var dto = mapper.Map<UserDto>(user);

// Uso con EF Core (richiede HyperMapper.EFCore)
var dtos = context.Users
    .ProjectTo<UserDto>(mapper.ConfigurationProvider)
    .ToList();
```
