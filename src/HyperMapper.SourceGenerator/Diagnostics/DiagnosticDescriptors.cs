using Microsoft.CodeAnalysis;

namespace HyperMapper.SourceGenerator.Diagnostics;

/// <summary>
/// Compile-time diagnostic descriptors for HyperMapper Source Generator.
/// </summary>
internal static class DiagnosticDescriptors
{
    private const string Category = "HyperMapper";

    public static readonly DiagnosticDescriptor NoParameterlessConstructor = new(
        id: "LMAP001",
        title: "Destination type lacks parameterless constructor",
        messageFormat: "Type '{0}' must have a parameterless constructor for mapping",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The destination type must have a public parameterless constructor to be instantiated by the mapper.");

    public static readonly DiagnosticDescriptor UnmappedProperty = new(
        id: "LMAP002",
        title: "Unmapped destination property",
        messageFormat: "Property '{0}' on '{1}' has no corresponding source property and is not ignored",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Consider using ForMember().Ignore() or ensuring a matching source property exists.");

    public static readonly DiagnosticDescriptor IncompatibleTypes = new(
        id: "LMAP003",
        title: "Incompatible property types",
        messageFormat: "Cannot map '{0}' ({1}) to '{2}' ({3}) - types are incompatible",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The source and destination property types are not compatible for automatic mapping.");

    public static readonly DiagnosticDescriptor ConverterRequiresRuntime = new(
        id: "LMAP004",
        title: "Converter requires runtime execution",
        messageFormat: "Mapping '{0}' -> '{1}' uses a converter and will fall back to runtime path",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "ITypeConverter instances cannot be inlined at compile-time. The mapping will use the runtime execution path.");

    public static readonly DiagnosticDescriptor CircularReference = new(
        id: "LMAP005",
        title: "Circular reference detected",
        messageFormat: "Circular reference detected in mapping chain: {0}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The mapping chain contains a circular reference which may cause infinite recursion.");

    public static readonly DiagnosticDescriptor PreConditionRequiresRuntime = new(
        id: "LMAP006",
        title: "PreCondition requires runtime execution",
        messageFormat: "Property '{0}' has a PreCondition and will fall back to runtime path",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "PreCondition delegates cannot be evaluated at compile-time. The mapping will use the runtime execution path.");
}
