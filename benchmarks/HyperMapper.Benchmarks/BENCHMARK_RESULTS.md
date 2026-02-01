# HyperMapper Benchmark Results

**Data**: 2026-02-01
**Ambiente**: macOS 26.2, Apple M2 Pro, .NET 10.0.2
**Tool**: BenchmarkDotNet v0.14.0

---

## v11.0.0 - GeneratedMapper (Massime Performance)

### Due Modalità d'Uso

HyperMapper offre due modalità d'uso con diversi livelli di performance:

| Modalità | Factory | `Map<S,D>()` | Note |
|----------|---------|--------------|------|
| **Standard** | `config.CreateMapper()` | ~42 ns | API 100% compatibile AutoMapper |
| **Performance** | `HyperMapperGeneratedRegistry.CreateMapper(config)` | ~39 ns | Wrapper ottimizzato |
| **Maximum** | `GeneratedMapperDispatch.MapDirect<S,D>()` | ~21 ns | Chiamata diretta (nessun fallback) |
| **Maximum** | `mapper.MapFast<S,D>()` | ~19 ns | Extension method ottimizzato |

### Benchmark v11.0.0 - Simple Mapping

```
| Method                | Mean      | Ratio | Allocated |
|---------------------- |----------:|------:|----------:|
| Manual                |  21.0 ns  |  1.00 |      48 B |
| HyperMapper_MapFast   |  19.5 ns  |  0.93 |      48 B |  ← MAX PERFORMANCE
| HyperMapper_Direct    |  21.1 ns  |  1.00 |      48 B |  ← MAX PERFORMANCE
| HyperMapper_Generated |  39.1 ns  |  1.86 |      48 B |  ← RECOMMENDED
| HyperMapper_CodeGen   |  42.1 ns  |  2.01 |      48 B |
| HyperMapper (Runtime) | 121.2 ns  |  5.78 |      48 B |
| AutoMapper            | 153.7 ns  |  7.32 |      48 B |
```

### Quale modalità scegliere?

**1. Modalità Standard** (42 ns) - Compatibilità 100% AutoMapper
```csharp
var config = new MapperConfiguration(cfg => cfg.AddProfile<MyProfile>());
HyperMapperGeneratedRegistry.Initialize(config);  // Per Source Generator
var mapper = config.CreateMapper();

// Supporta TUTTE le feature: ForMember, MapFrom, trasformazioni, etc.
mapper.Map<PersonDto>(person);
```

**2. Modalità Performance** (39 ns) - Raccomandato per production
```csharp
var config = new MapperConfiguration(cfg => cfg.AddProfile<MyProfile>());
var mapper = HyperMapperGeneratedRegistry.CreateMapper(config);  // ← Unica differenza

// Usa MapDirect internamente per mapping generati
// Fallback automatico a runtime per mapping non generati
mapper.Map<PersonDto>(person);
```

**3. Modalità Maximum** (19-21 ns) - Per hot paths critici
```csharp
// Opzione A: Extension method (con fallback)
mapper.MapFast<Person, PersonDto>(person);

// Opzione B: Chiamata diretta (senza fallback, solo mapping generati)
var result = GeneratedMapperDispatch.MapDirect<Person, PersonDto>(person);
```

### Nota Importante

La **Modalità Performance** (`GeneratedMapper`) usa il codice generato dal Source Generator che **non supporta trasformazioni runtime** come `.MapFrom(s => s.Name.ToUpper())`. Per queste feature, usa la Modalità Standard.

---

## Riepilogo Esecutivo (v6.0.0 - Source Generators)

| Scenario | HyperMapper | AutoMapper | vs AutoMapper | Stato |
|----------|-------------|------------|---------------|-------|
| **Simple Mapping** | 94 ns | 148 ns | **1.57x FASTER** | DONE |
| **Flattening** | 104 ns | 151 ns | **1.45x FASTER** | DONE |
| **Deep Nesting** | 272 ns | 349 ns | **1.28x FASTER** | DONE |
| **Complex (Full)** | 227 ns | 308 ns | **1.36x FASTER** | DONE |
| **Complex (WithNulls)** | 160 ns | 209 ns | **1.31x FASTER** | DONE |
| **Collection (Small)** | 485 ns | 614 ns | **1.27x FASTER** | DONE |
| **Collection (Medium)** | 3,018 ns | 3,817 ns | **1.26x FASTER** | DONE |
| **Collection (Large)** | 26,235 ns | 38,723 ns | **1.48x FASTER** | DONE |

### v6.0.0 Source Generator Benefits

| Aspetto | Prima (v5.0.0) | Dopo (v6.0.0) | Miglioramento |
|---------|----------------|---------------|---------------|
| **Prima chiamata** | ~1-5ms (JIT + build) | ~100ns | **10,000x+ FASTER** |
| **Warm-up** | Necessario | Zero | Eliminato |
| **AOT/Native** | Parziale | Completo | Full support |
| **Errori** | Runtime | Compile-time | Catch early |

### Scenari Ottimizzati (v6.0.0 Source Generators) - ALL DONE

| Scenario | HyperMapper | AutoMapper | Risultato |
|----------|-------------|------------|-----------|
| Simple Mapping | 94 ns | 148 ns | **1.57x FASTER** |
| Flattening | 104 ns | 151 ns | **1.45x FASTER** |
| Deep Nesting | 272 ns | 349 ns | **1.28x FASTER** |
| Complex (Full) | 227 ns | 308 ns | **1.36x FASTER** |
| Complex (WithNulls) | 160 ns | 209 ns | **1.31x FASTER** |

### Collection Mapping - NOW OPTIMIZED (v4.3.0 - v6.0.0)

| Scenario | HyperMapper | AutoMapper | Risultato |
|----------|-------------|------------|-----------|
| Collection (Small, 10) | 485 ns | 614 ns | **1.27x FASTER** |
| Collection (Medium, 100) | 3,018 ns | 3,817 ns | **1.26x FASTER** |
| Collection (Large, 1000) | 26,235 ns | 38,723 ns | **1.48x FASTER** |

### Memory Usage (v5.0.0+)

| Scenario | HyperMapper | AutoMapper | Risparmio |
|----------|-------------|------------|-----------|
| Collection (Small) | 672 B | 808 B | **-17%** |
| Collection (Medium) | 5,712 B | 6,992 B | **-18%** |
| Collection (Large) | 56,112 B | 64,600 B | **-13%** |

---

## Risultati Dettagliati

### 1. Simple Mapping (5 proprietà flat)

```
| Method      | Mean      | Ratio | Allocated | Alloc Ratio |
|------------ |----------:|------:|----------:|------------:|
| Manual      |  21.58 ns |  1.00 |      48 B |        1.00 |
| HyperMapper |  95.47 ns |  4.43 |      48 B |        1.00 |
| AutoMapper  | 154.68 ns |  7.17 |      48 B |        1.00 |
```

**HyperMapper e 1.6x PIU VELOCE di AutoMapper!**
Zero allocazioni extra rispetto al mapping manuale.

### 2. Flattening (nested 3 livelli -> flat)

```
| Method      | Mean       | Ratio | Allocated | Alloc Ratio |
|------------ |-----------:|------:|----------:|------------:|
| Manual      |   29.14 ns |  1.00 |      56 B |        1.00 |
| HyperMapper |  104.98 ns |  3.60 |      56 B |        1.00 |
| AutoMapper  |  167.65 ns |  5.75 |      56 B |        1.00 |
```

**HyperMapper e 1.6x PIU VELOCE di AutoMapper!**
**Miglioramento v3.0.0**: 1,093 ns -> 105 ns (**10.4x faster**)

### 3. Deep Nesting (10 livelli annidati)

```
| Method      | Mean       | Ratio | Allocated | Alloc Ratio |
|------------ |-----------:|------:|----------:|------------:|
| Manual      |    189.7 ns |  1.00 |     320 B |        1.00 |
| HyperMapper |    258.0 ns |  1.36 |     320 B |        1.00 |
| AutoMapper  |    346.5 ns |  1.83 |     320 B |        1.00 |
```

**HyperMapper e 1.34x PIU VELOCE di AutoMapper!**
**Miglioramento v4.0.0**: 6,116 ns -> 258 ns (**23.7x faster**)
Zero allocazioni extra rispetto al mapping manuale.

### 4. Complex Objects

```
| Method                | Mean        | Ratio | Allocated | Alloc Ratio |
|---------------------- |-----------:|------:|----------:|------------:|
| Manual_Full           |   132.52 ns |  1.00 |     264 B |        1.00 |
| HyperMapper_Full      |   213.43 ns |  1.61 |     264 B |        1.00 |
| AutoMapper_Full       |   306.88 ns |  2.32 |     272 B |        1.03 |
| Manual_WithNulls      |    68.95 ns |  0.52 |     168 B |        0.64 |
| HyperMapper_WithNulls |   146.94 ns |  1.11 |     168 B |        0.64 |
| AutoMapper_WithNulls  |   199.21 ns |  1.50 |     168 B |        0.64 |
```

**HyperMapper e 1.44x PIU VELOCE di AutoMapper!**
Zero allocazioni extra rispetto al mapping manuale.

**Miglioramento v4.2.0 (Primitive Collection Inlining):**
- Complex_Full: 567 ns -> 213 ns (**2.7x faster**)
- Complex_WithNulls: 560 ns -> 147 ns (**3.8x faster**)
- Allocazioni identiche al manual (264B Full, 168B WithNulls)

**Nota**: La collection `List<string> Tags` viene ora copiata inline usando `new List<string>(source.Tags)` invece del legacy path.

**Miglioramento v4.1.1 (Hybrid Execution Plans):**
- Complex_Full: 1,815 ns -> 567 ns (**3.2x faster**)
- Complex_WithNulls: 1,626 ns -> 560 ns (**2.9x faster**)
- Allocazioni ridotte: 336B -> 184B (-45%), 240B -> 136B (-43%)

**Nota**: Hybrid execution compila proprietà semplici/nested, usa legacy solo per collections.

### 5. Collections

```
| Method             | Mean         | Ratio  | Allocated | Alloc Ratio |
|------------------- |-------------:|-------:|----------:|------------:|
| Manual_Small       |     313.9 ns |   1.00 |     616 B |        1.00 |
| HyperMapper_Small  |   1,496.3 ns |   4.77 |   1,008 B |        1.64 |
| AutoMapper_Small   |     631.8 ns |   2.01 |     808 B |        1.31 |
| Manual_Medium      |   2,930.7 ns |   9.34 |   5,656 B |        9.18 |
| HyperMapper_Medium |  10,520.4 ns |  33.52 |   7,912 B |       12.84 |
| AutoMapper_Medium  |   3,891.6 ns |  12.40 |   6,992 B |       11.35 |
| Manual_Large       |  29,106.6 ns |  92.73 |  56,056 B |       91.00 |
| HyperMapper_Large  | 106,096.3 ns | 338.03 |  72,720 B |      118.05 |
| AutoMapper_Large   |  35,768.6 ns | 113.96 |  64,600 B |      104.87 |
```

**Nota**: Collections mappano elemento per elemento (legacy path).

---

## Ottimizzazioni Applicate (v4.2.0)

| # | Ottimizzazione | Tecnica | Stato |
|---|----------------|---------|-------|
| 1 | Compiled property accessors | `Expression.Compile()` per getter/setter | v1.1.0 |
| 2 | Typed delegates | Eliminato `DynamicInvoke` con `CompiledResolver` | v1.1.0 |
| 3 | Compiled object factory | `Expression.New()` compilato in `ReflectionCache` | v1.1.0 |
| 4 | Collection factory | Typed builders per evitare `MethodInfo.Invoke` | v1.1.0 |
| 5 | Type analysis cache | `TypeAnalysisResult` per cachare tutti i type checks | v2.0.0 |
| 6 | Pre-computed member sets | HashSet per ConfiguredMembers/IgnoredMembers | v2.0.0 |
| 7 | Case-insensitive dict cache | Elimina O(n) loop per property matching | v2.0.0 |
| 8 | **Execution Plans** | Compila mapping completo a configuration-time | v2.0.0 |
| 9 | **MapFrom Expression Integration** | Integra `ForMember().MapFrom()` nell'execution plan | v3.0.0 |
| 10 | **Inline Nested Object Mapping** | Integra mapping nested nell'execution plan | v4.0.0 |
| 11 | **Hybrid Execution Plans** | Compila props semplici/nested, legacy solo per collections | v4.1.1 |
| 12 | **Primitive Collection Inlining** | `new List<T>(source)` per List<primitive> | v4.2.0 |
| 13 | **Collection Execution Plans** | Loop tipizzati compilati con element mapping inline | v4.3.0 |
| 14 | **ArrayPool Memory Optimization** | Riutilizzo array nel legacy path | v5.0.0 |
| 15 | **Source Generators** | Codice mapping generato a compile-time, zero JIT | v6.0.0 |

### Architettura Execution Plans con MapFrom

```csharp
// A configuration-time, HyperMapper compila anche i MapFrom:
Func<object, object> plan = source => {
    var typedSource = (TSource)source;
    var dest = new TDest();

    // Convention mapping
    dest.BaseDate = typedSource.BaseDate;

    // MapFrom integrati (NON chiamate a funzione separate!)
    dest.SubProperName = typedSource.Sub.ProperName;
    dest.SubSubSubIAmACoolProperty = typedSource.Sub.SubSub != null
        ? typedSource.Sub.SubSub.IAmACoolProperty
        : string.Empty;

    return dest;
};

// A runtime = UNA singola chiamata di funzione
```

### Architettura Inline Nested Object Mapping (v4.0.0)

```csharp
// Per deep nesting (10 livelli), HyperMapper genera TUTTO inline:
Func<object, object> plan = source => {
    var typedSource = (DeepLevel1Source)source;
    var dest = new DeepLevel1Destination();

    dest.Id = typedSource.Id;
    dest.Name = typedSource.Name;

    // Nested object inlined con null check (NO chiamate ricorsive a MapInternal!)
    if (typedSource.Level2 != null) {
        var nested_0 = new DeepLevel2Destination();
        nested_0.Value = typedSource.Level2.Value;

        if (typedSource.Level2.Level3 != null) {
            var nested_1 = new DeepLevel3Destination();
            nested_1.Value = typedSource.Level2.Level3.Value;
            // ... continua inline fino a Level10
            nested_0.Level3 = nested_1;
        }
        dest.Level2 = nested_0;
    }

    return dest;
};

// Risultato: 258ns vs 6,116ns (23.7x faster!)
// Una singola chiamata di funzione invece di 10 chiamate ricorsive
```

---

## Ottimizzazioni v4.1/v4.2 (Build-time)

| # | Ottimizzazione | Stato | Note |
|---|----------------|-------|------|
| 1 | Cache TypeMap lookups | DONE | Riduce allocazioni durante build execution plans |
| 2 | Fix Nullable struct (`T?`) | DONE | Genera `HasValue` check per nullable structs |
| 3 | **Hybrid execution plans** | **DONE v4.1.1** | **3.2x faster** per Complex Objects (567ns vs 1,815ns) |
| 4 | **Primitive Collection Inlining** | **DONE v4.2.0** | **2.7x faster** per Complex Objects (213ns vs 567ns) |

### Architettura Primitive Collection Inlining (v4.2.0)

```csharp
// Complex object con List<string> Tags:
// PRIMA (v4.1.1): Hybrid execution -> 567ns
//   - Execution plan per props semplici/nested
//   - ApplyCollectionMappingOnly per Tags (legacy path lento)

// DOPO (v4.2.0): Full inline -> 213ns
Func<object, object> plan = source => {
    var typedSource = (ComplexSource)source;
    var dest = new ComplexDestination();

    // Props semplici
    dest.Id = typedSource.Id;
    dest.Name = typedSource.Name;
    // ...

    // Props nested (inline)
    if (typedSource.Address != null) {
        var nested_0 = new ComplexAddressDestination();
        nested_0.Street = typedSource.Address.Street;
        nested_0.City = typedSource.Address.City;
        dest.Address = nested_0;
    }

    // Primitive collection INLINE! (nuovo in v4.2.0)
    dest.Tags = typedSource.Tags != null
        ? new List<string>(typedSource.Tags)  // Costruttore efficiente!
        : new List<string>();

    return dest;
};

// Tutto compilato, nessuna chiamata a ApplyCollectionMappingOnly!
// Il costruttore List<T>(IEnumerable<T>) è altamente ottimizzato dalla BCL
```

### Architettura Hybrid Execution Plans (v4.1.1)

```csharp
// Complex object con collection (es. ComplexSource con List<string> Tags):
// PRIMA: Tutto legacy path -> 1,815ns
// DOPO:  Hybrid execution -> 567ns

// ExecutionPlanBuilder genera plan per props semplici + nested:
Func<object, object> plan = source => {
    var typedSource = (ComplexSource)source;
    var dest = new ComplexDestination();

    // Props semplici (compilate nel plan)
    dest.Id = typedSource.Id;
    dest.Name = typedSource.Name;
    dest.Status = typedSource.Status;
    dest.CreatedAt = typedSource.CreatedAt;
    // ...

    // Props nested (compilate inline)
    if (typedSource.Address != null) {
        var nested_0 = new ComplexAddressDestination();
        nested_0.Street = typedSource.Address.Street;
        nested_0.City = typedSource.Address.City;
        dest.Address = nested_0;
    }

    return dest;
};

// TypeMap.CollectionProperties = { "Tags" }
// Mapper applica execution plan, poi mappa Tags via legacy path
```

---

## Backlog Ottimizzazioni - COMPLETATO

| # | Ottimizzazione | Tecnica | Impatto | Stato |
|---|----------------|---------|---------|-------|
| 1 | ~~Nested object execution plans~~ | ~~Integrare mapping nested nell'execution plan~~ | ~~-80% deep nesting~~ | **DONE v4.0.0** |
| 2 | ~~Hybrid execution plans~~ | ~~Combinare fast path (nested) + legacy (collections)~~ | ~~-70% complex objects~~ | **DONE v4.1.1** |
| 3 | ~~Primitive collection inlining~~ | ~~`new List<T>(source)` per primitive~~ | ~~-60% complex objects~~ | **DONE v4.2.0** |
| 4 | ~~Collection Execution Plans~~ | ~~Mappare List<ComplexObject> inline~~ | ~~-50% collections~~ | **DONE v4.3.0** |
| 5 | ~~ArrayPool/Memory Optimization~~ | ~~Ridurre allocazioni per collezioni~~ | ~~-18% memoria~~ | **DONE v5.0.0** |
| 6 | ~~Source Generators~~ | ~~Generare codice mapping a compile-time~~ | ~~Zero warm-up~~ | **DONE v6.0.0** |

### Ottimizzazioni Future (Opzionali)

| # | Ottimizzazione | Tecnica | Impatto Stimato | Effort |
|---|----------------|---------|-----------------|--------|
| 1 | Span/Memory<T> | Zero-copy per collezioni grandi | -20% memoria | Medio |
| 2 | ProjectTo | Generare Expression Trees per EF Core | LINQ support | Alto |

---

## Come Eseguire i Benchmark

```bash
cd api/HyperMapper/benchmarks/HyperMapper.Benchmarks

# Tutti i benchmark (completo, ~16 min)
dotnet run -c Release -- --cli /opt/homebrew/Cellar/dotnet/10.0.102/bin/dotnet --filter "*"

# Singolo benchmark
dotnet run -c Release -- --cli /opt/homebrew/Cellar/dotnet/10.0.102/bin/dotnet --filter "*Simple*"

# Job veloce (per sviluppo)
dotnet run -c Release -- --job short --cli /opt/homebrew/Cellar/dotnet/10.0.102/bin/dotnet --filter "*"
```

---

## Storico Performance

### Simple Mapping

| Data | Versione | Tempo (ns) | vs AutoMapper | Note |
|------|----------|------------|---------------|------|
| 2026-01-30 | v1.0.0 | 1,778 | 10x slower | Baseline - Reflection pura |
| 2026-01-30 | v1.1.0 | 833 | 4.8x slower | Ottimizzazioni Expression Trees |
| 2026-01-30 | v2.0.0 | 88 | **1.8x FASTER** | Execution Plans |
| 2026-01-30 | v3.0.0 | 95 | **1.6x FASTER** | Stabile |

### Flattening

| Data | Versione | Tempo (ns) | vs AutoMapper | Note |
|------|----------|------------|---------------|------|
| 2026-01-30 | v1.0.0 | ~1,700 | 10x slower | Baseline |
| 2026-01-30 | v2.0.0 | 1,093 | 6.6x slower | Legacy path per MapFrom |
| 2026-01-30 | v3.0.0 | 105 | **1.6x FASTER** | MapFrom Expression Integration |

### Deep Nesting

| Data | Versione | Tempo (ns) | vs AutoMapper | Note |
|------|----------|------------|---------------|------|
| 2026-01-30 | v1.0.0 | ~6,000 | 17x slower | Baseline - Reflection ricorsiva |
| 2026-01-30 | v3.0.0 | 6,116 | 17x slower | Legacy path per nested objects |
| 2026-01-30 | v4.0.0 | 258 | **1.34x FASTER** | Inline Nested Object Mapping |

### Complex Objects

| Data | Versione | Tempo (ns) | vs AutoMapper | Note |
|------|----------|------------|---------------|------|
| 2026-01-30 | v4.0.0 | 1,815 | 6x slower | Full legacy path per collections |
| 2026-01-30 | v4.1.1 | 567 | 2x slower | **Hybrid Execution Plans** |
| 2026-01-30 | v4.2.0 | 213 | **1.44x FASTER** | **Primitive Collection Inlining** |
| 2026-01-30 | v4.3.0 | 227 | **1.36x FASTER** | Collection Execution Plans |
| 2026-01-30 | v5.0.0 | 227 | **1.36x FASTER** | ArrayPool Memory Optimization |
| 2026-01-30 | v6.0.0 | 227 | **1.36x FASTER** | **Source Generators** (zero warm-up) |

### Collections

| Data | Versione | Tempo (ns) Small | vs AutoMapper | Note |
|------|----------|------------------|---------------|------|
| 2026-01-30 | v4.2.0 | 1,496 | 2.4x slower | Legacy path per elemento |
| 2026-01-30 | v4.3.0 | 484 | **1.27x FASTER** | Collection Execution Plans |
| 2026-01-30 | v5.0.0 | 484 | **1.27x FASTER** | ArrayPool Memory (-17%) |
| 2026-01-30 | v6.0.0 | 485 | **1.27x FASTER** | Source Generators
