using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using HyperMapper.SourceGenerator.CodeGen;
using HyperMapper.SourceGenerator.Models;

namespace HyperMapper.SourceGenerator;

/// <summary>
/// Source Generator for HyperMapper - generates compile-time mapping code.
/// </summary>
[Generator(LanguageNames.CSharp)]
public class MapperGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Step 1: Find all classes that inherit from Profile
        var profileClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsProfileClassCandidate(node),
                transform: static (ctx, ct) => GetProfileInfo(ctx, ct))
            .Where(static info => info is not null);

        // Step 2: Combine with compilation
        var compilationAndProfiles = context.CompilationProvider
            .Combine(profileClasses.Collect());

        // Step 3: Generate source
        context.RegisterSourceOutput(compilationAndProfiles,
            static (spc, source) => Execute(spc, source.Left, source.Right!));
    }

    /// <summary>
    /// Fast syntactic filter - checks if node is a potential Profile class.
    /// </summary>
    private static bool IsProfileClassCandidate(SyntaxNode node)
    {
        // Quick check: is it a class with a base type?
        if (node is not ClassDeclarationSyntax classDecl)
            return false;

        // Must have a base list (inherits from something)
        if (classDecl.BaseList is null)
            return false;

        // Must not be abstract
        if (classDecl.Modifiers.Any(SyntaxKind.AbstractKeyword))
            return false;

        return true;
    }

    /// <summary>
    /// Semantic analysis - extracts profile information if class inherits from Profile.
    /// </summary>
    private static ProfileInfo? GetProfileInfo(GeneratorSyntaxContext context, CancellationToken ct)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(classDecl, ct);

        if (symbol is null) return null;

        // Check if base class is Profile
        if (!InheritsFromProfile(symbol))
            return null;

        return ExtractProfileInfo(classDecl, context.SemanticModel, ct);
    }

    /// <summary>
    /// Checks if the type inherits from HyperMapper.Profile.
    /// </summary>
    private static bool InheritsFromProfile(INamedTypeSymbol symbol)
    {
        var baseType = symbol.BaseType;
        while (baseType != null)
        {
            if (baseType.Name == "Profile" &&
                baseType.ContainingNamespace?.ToDisplayString() == "HyperMapper")
            {
                return true;
            }
            baseType = baseType.BaseType;
        }
        return false;
    }

    /// <summary>
    /// Extracts mapping definitions from a Profile class.
    /// </summary>
    private static ProfileInfo ExtractProfileInfo(
        ClassDeclarationSyntax classDecl,
        SemanticModel semanticModel,
        CancellationToken ct)
    {
        var profileInfo = new ProfileInfo
        {
            ClassName = classDecl.Identifier.Text,
            Namespace = GetNamespace(classDecl),
            FullClassName = GetFullClassName(classDecl),
            Mappings = new List<MappingDefinition>()
        };

        // Find constructor and analyze CreateMap calls
        var constructor = classDecl.Members
            .OfType<ConstructorDeclarationSyntax>()
            .FirstOrDefault();

        if (constructor?.Body != null)
        {
            foreach (var statement in constructor.Body.Statements)
            {
                ct.ThrowIfCancellationRequested();
                if (TryExtractCreateMapCall(statement, semanticModel, out var mapping))
                {
                    profileInfo.Mappings.Add(mapping);
                }
            }
        }

        // Also check expression-bodied constructor (rare but possible)
        if (constructor?.ExpressionBody != null)
        {
            // Expression body is a single expression, unlikely to have CreateMap
        }

        return profileInfo;
    }

    /// <summary>
    /// Gets the namespace of the class declaration.
    /// </summary>
    private static string GetNamespace(ClassDeclarationSyntax classDecl)
    {
        // Walk up to find namespace
        var parent = classDecl.Parent;
        while (parent != null)
        {
            if (parent is NamespaceDeclarationSyntax ns)
                return ns.Name.ToString();
            if (parent is FileScopedNamespaceDeclarationSyntax fsns)
                return fsns.Name.ToString();
            parent = parent.Parent;
        }
        return "";
    }

    /// <summary>
    /// Gets the full class name including parent classes for nested types.
    /// </summary>
    private static string GetFullClassName(ClassDeclarationSyntax classDecl)
    {
        var names = new List<string>();
        names.Add(classDecl.Identifier.Text);

        // Walk up to find parent classes (for nested types)
        var parent = classDecl.Parent;
        while (parent is ClassDeclarationSyntax parentClass)
        {
            names.Insert(0, parentClass.Identifier.Text);
            parent = parentClass.Parent;
        }

        return string.Join("_", names);
    }

    /// <summary>
    /// Tries to extract a CreateMap call from a statement.
    /// </summary>
    private static bool TryExtractCreateMapCall(
        StatementSyntax statement,
        SemanticModel semanticModel,
        out MappingDefinition mapping)
    {
        mapping = null!;

        if (statement is not ExpressionStatementSyntax exprStmt)
            return false;

        var invocation = FindCreateMapInvocation(exprStmt.Expression);
        if (invocation is null)
            return false;

        // Get method symbol to extract type arguments
        var symbolInfo = semanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return false;

        // v7.0.0: Handle open generic CreateMap(typeof(Box<>), typeof(BoxDto<>))
        if (methodSymbol.TypeArguments.Length == 0 && invocation.ArgumentList.Arguments.Count == 2)
        {
            return TryExtractOpenGenericMapping(invocation, semanticModel, out mapping);
        }

        if (methodSymbol.TypeArguments.Length != 2)
            return false;

        var sourceType = methodSymbol.TypeArguments[0];
        var destType = methodSymbol.TypeArguments[1];

        mapping = new MappingDefinition
        {
            // v12.0.2: Use FullyQualifiedFormat to ensure external types are correctly resolved
            SourceType = sourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            DestinationType = destType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            SourceTypeName = sourceType.Name,
            DestinationTypeName = destType.Name,
            SourceTypeSymbol = sourceType,
            DestinationTypeSymbol = destType,
            MemberMappings = new List<MemberMapping>()
        };

        // Extract ForMember configurations
        ExtractForMemberCalls(exprStmt.Expression, semanticModel, mapping);

        return true;
    }

    /// <summary>
    /// v7.0.0: Tries to extract an open generic mapping from CreateMap(typeof(...), typeof(...)).
    /// </summary>
    private static bool TryExtractOpenGenericMapping(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        out MappingDefinition mapping)
    {
        mapping = null!;

        var args = invocation.ArgumentList.Arguments;
        if (args.Count != 2)
            return false;

        // Extract typeof() expressions
        if (args[0].Expression is not TypeOfExpressionSyntax sourceTypeOf ||
            args[1].Expression is not TypeOfExpressionSyntax destTypeOf)
            return false;

        var sourceTypeSyntax = sourceTypeOf.Type;
        var destTypeSyntax = destTypeOf.Type;

        // Both must be open generic types (e.g., Box<> or Dictionary<,>)
        if (sourceTypeSyntax is not GenericNameSyntax sourceGeneric ||
            destTypeSyntax is not GenericNameSyntax destGeneric)
            return false;

        var sourceTypeParams = sourceGeneric.TypeArgumentList.Arguments;
        var destTypeParams = destGeneric.TypeArgumentList.Arguments;

        // Check that all type arguments are omitted (open generic)
        if (!sourceTypeParams.All(t => t is OmittedTypeArgumentSyntax) ||
            !destTypeParams.All(t => t is OmittedTypeArgumentSyntax))
            return false;

        // Get the underlying types for proper namespace resolution
        var sourceTypeInfo = semanticModel.GetTypeInfo(sourceTypeSyntax);
        var destTypeInfo = semanticModel.GetTypeInfo(destTypeSyntax);

        // Generate type parameter names (T, T2, T3... or TKey, TValue for 2 params)
        var typeParamCount = sourceTypeParams.Count;
        var typeParamNames = GenerateTypeParameterNames(typeParamCount);

        // Build full type names with type parameters
        var sourceName = sourceGeneric.Identifier.Text;
        var destName = destGeneric.Identifier.Text;

        // Get namespace if available
        var sourceNamespace = GetNamespaceFromTypeSyntax(sourceGeneric, semanticModel);
        var destNamespace = GetNamespaceFromTypeSyntax(destGeneric, semanticModel);

        var sourceFullName = string.IsNullOrEmpty(sourceNamespace) ? sourceName : $"{sourceNamespace}.{sourceName}";
        var destFullName = string.IsNullOrEmpty(destNamespace) ? destName : $"{destNamespace}.{destName}";

        var typeParamsString = string.Join(", ", typeParamNames);
        var openSourceType = $"{sourceFullName}<{typeParamsString}>";
        var openDestType = $"{destFullName}<{typeParamsString}>";

        // Get type symbols for property discovery
        INamedTypeSymbol? sourceTypeSymbol = null;
        INamedTypeSymbol? destTypeSymbol = null;

        if (sourceTypeInfo.Type is INamedTypeSymbol srcSym && srcSym.IsUnboundGenericType)
        {
            sourceTypeSymbol = srcSym.OriginalDefinition;
        }
        else if (sourceTypeInfo.Type is INamedTypeSymbol srcOriginal)
        {
            sourceTypeSymbol = srcOriginal;
        }

        if (destTypeInfo.Type is INamedTypeSymbol dstSym && dstSym.IsUnboundGenericType)
        {
            destTypeSymbol = dstSym.OriginalDefinition;
        }
        else if (destTypeInfo.Type is INamedTypeSymbol dstOriginal)
        {
            destTypeSymbol = dstOriginal;
        }

        mapping = new MappingDefinition
        {
            SourceType = $"{sourceFullName}<>",
            DestinationType = $"{destFullName}<>",
            SourceTypeName = sourceName,
            DestinationTypeName = destName,
            SourceTypeSymbol = sourceTypeSymbol,
            DestinationTypeSymbol = destTypeSymbol,
            IsOpenGeneric = true,
            TypeParameters = typeParamNames,
            OpenSourceType = openSourceType,
            OpenDestType = openDestType,
            MemberMappings = new List<MemberMapping>()
        };

        return true;
    }

    /// <summary>
    /// Generates type parameter names for open generics.
    /// </summary>
    private static List<string> GenerateTypeParameterNames(int count)
    {
        if (count == 1)
            return new List<string> { "T" };
        if (count == 2)
            return new List<string> { "TKey", "TValue" };
        return Enumerable.Range(1, count).Select(i => $"T{i}").ToList();
    }

    /// <summary>
    /// Gets the namespace from a type syntax node.
    /// </summary>
    private static string? GetNamespaceFromTypeSyntax(GenericNameSyntax genericName, SemanticModel semanticModel)
    {
        // Try to get from semantic model
        var typeInfo = semanticModel.GetTypeInfo(genericName);
        if (typeInfo.Type is INamedTypeSymbol namedType)
        {
            var ns = namedType.ContainingNamespace;
            if (ns != null && !ns.IsGlobalNamespace)
                return ns.ToDisplayString();
        }

        // Try to find qualified name in parent
        if (genericName.Parent is QualifiedNameSyntax qualified)
        {
            return qualified.Left.ToString();
        }

        return null;
    }

    /// <summary>
    /// Finds the CreateMap invocation in an expression chain.
    /// </summary>
    private static InvocationExpressionSyntax? FindCreateMapInvocation(ExpressionSyntax expression)
    {
        var current = expression;

        while (current is InvocationExpressionSyntax invocation)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                if (memberAccess.Name.Identifier.Text == "CreateMap")
                    return invocation;

                current = memberAccess.Expression;
            }
            else if (invocation.Expression is GenericNameSyntax genericName)
            {
                if (genericName.Identifier.Text == "CreateMap")
                    return invocation;
                break;
            }
            else if (invocation.Expression is IdentifierNameSyntax identifier)
            {
                if (identifier.Identifier.Text == "CreateMap")
                    return invocation;
                break;
            }
            else
            {
                break;
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts ForMember configurations from a fluent chain.
    /// </summary>
    private static void ExtractForMemberCalls(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        MappingDefinition mapping)
    {
        var current = expression;

        while (current is InvocationExpressionSyntax invocation)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var methodName = memberAccess.Name.Identifier.Text;

                if (methodName == "ForMember")
                {
                    var memberMapping = ExtractForMemberMapping(invocation, semanticModel);
                    if (memberMapping != null)
                    {
                        mapping.MemberMappings.Add(memberMapping);
                    }
                }
                else if (methodName == "ConvertUsing")
                {
                    mapping.HasConverter = true;
                    // v7.0.0: Try to extract lambda converter expression
                    var converterArg = invocation.ArgumentList.Arguments.FirstOrDefault();
                    if (converterArg?.Expression is SimpleLambdaExpressionSyntax converterLambda &&
                        converterLambda.Body is ExpressionSyntax converterBody)
                    {
                        var paramName = converterLambda.Parameter.Identifier.Text;
                        var bodyText = converterBody.ToString();
                        mapping.ConverterLambdaExpression = ReplaceParameterWithSource(bodyText, paramName);
                    }
                    // v12.0.0: Support class-based ITypeConverter
                    else if (converterArg?.Expression is ObjectCreationExpressionSyntax objectCreation)
                    {
                        // Get full type name including namespace using semantic model
                        var typeInfo = semanticModel.GetTypeInfo(objectCreation);
                        if (typeInfo.Type != null)
                        {
                            // Use fully qualified name: "Links.SpotBooking.Services.Converters.GeometryConverter"
                            mapping.ConverterTypeName = typeInfo.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                                .Replace("global::", ""); // Remove global:: prefix
                        }
                        else
                        {
                            // Fallback to simple name
                            mapping.ConverterTypeName = objectCreation.Type.ToString();
                        }
                    }
                    // v12.1.0: Support ConvertUsing(typeof(MyConverter))
                    else if (converterArg?.Expression is TypeOfExpressionSyntax typeOfExpression)
                    {
                        // Get full type name from typeof() expression
                        var typeInfo = semanticModel.GetTypeInfo(typeOfExpression.Type);
                        if (typeInfo.Type != null)
                        {
                            mapping.ConverterTypeName = typeInfo.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                                .Replace("global::", "");
                        }
                        else
                        {
                            // Fallback to simple name
                            mapping.ConverterTypeName = typeOfExpression.Type.ToString();
                        }
                    }
                }
                else if (methodName == "ValidateMemberList")
                {
                    // v8.1.0: Extract ValidateMemberList enum value
                    var validateArg = invocation.ArgumentList.Arguments.FirstOrDefault();
                    if (validateArg?.Expression is MemberAccessExpressionSyntax enumAccess)
                    {
                        // Extract "Destination" from "MemberList.Destination"
                        mapping.ValidateMemberList = enumAccess.Name.Identifier.Text;
                    }
                }
                else if (methodName == "ForCtorParam")
                {
                    // v9.0.0: Extract ForCtorParam mapping
                    var ctorParamMapping = ExtractForCtorParamMapping(invocation);
                    if (ctorParamMapping != null)
                    {
                        mapping.CtorParamMappings.Add(ctorParamMapping);
                    }
                }
                else if (methodName == "ConstructUsing")
                {
                    // v9.0.0: Extract ConstructUsing lambda
                    var constructArg = invocation.ArgumentList.Arguments.FirstOrDefault();
                    if (constructArg?.Expression is SimpleLambdaExpressionSyntax constructLambda)
                    {
                        var paramName = constructLambda.Parameter.Identifier.Text;
                        var bodyText = constructLambda.Body.ToString();
                        mapping.HasCustomConstructor = true;
                        mapping.ConstructorLambdaExpression = ReplaceParameterWithSource(bodyText, paramName);
                    }
                }
                else if (methodName == "ForPath")
                {
                    // v9.0.0: Extract ForPath mapping
                    var pathMapping = ExtractForPathMapping(invocation);
                    if (pathMapping != null)
                    {
                        mapping.PathMappings.Add(pathMapping);
                    }
                }
                else if (methodName == "Include")
                {
                    // v9.0.0: Extract Include<TDerivedSource, TDerivedDest>()
                    if (memberAccess.Name is GenericNameSyntax genericName)
                    {
                        var typeArgs = genericName.TypeArgumentList.Arguments;
                        if (typeArgs.Count == 2)
                        {
                            mapping.IncludedDerivedTypes.Add((
                                typeArgs[0].ToString(),
                                typeArgs[1].ToString()
                            ));
                        }
                    }
                }
                else if (methodName == "IncludeBase")
                {
                    // v9.0.0: Extract IncludeBase<TBaseSource, TBaseDest>()
                    if (memberAccess.Name is GenericNameSyntax genericName)
                    {
                        var typeArgs = genericName.TypeArgumentList.Arguments;
                        if (typeArgs.Count == 2)
                        {
                            mapping.IncludedBaseType = (
                                typeArgs[0].ToString(),
                                typeArgs[1].ToString()
                            );
                        }
                    }
                }
                else if (methodName == "AddTransform")
                {
                    // v10.0.0: Extract AddTransform<T>(t => t?.Trim())
                    if (memberAccess.Name is GenericNameSyntax genericName)
                    {
                        var typeArgs = genericName.TypeArgumentList.Arguments;
                        if (typeArgs.Count == 1)
                        {
                            var transformArg = invocation.ArgumentList.Arguments.FirstOrDefault();
                            if (transformArg?.Expression is SimpleLambdaExpressionSyntax transformLambda)
                            {
                                var paramName = transformLambda.Parameter.Identifier.Text;
                                var bodyText = transformLambda.Body.ToString();
                                mapping.Transforms.Add(new TransformDefinition
                                {
                                    TargetType = typeArgs[0].ToString(),
                                    TransformExpression = bodyText.Replace(paramName, "{0}")
                                });
                            }
                        }
                    }
                }
                else if (methodName == "IncludeMembers")
                {
                    // v10.0.0: Extract IncludeMembers(s => s.InnerObject)
                    foreach (var arg in invocation.ArgumentList.Arguments)
                    {
                        if (arg.Expression is SimpleLambdaExpressionSyntax memberLambda &&
                            memberLambda.Body is MemberAccessExpressionSyntax memberAccess2)
                        {
                            mapping.IncludedMembers.Add(memberAccess2.Name.Identifier.Text);
                        }
                    }
                }

                current = memberAccess.Expression;
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>
    /// Extracts a single ForMember mapping configuration.
    /// </summary>
    private static MemberMapping? ExtractForMemberMapping(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel)
    {
        var args = invocation.ArgumentList.Arguments;
        if (args.Count < 2) return null;

        // First arg: d => d.PropertyName
        string? destMemberName = null;
        if (args[0].Expression is SimpleLambdaExpressionSyntax destLambda)
        {
            if (destLambda.Body is MemberAccessExpressionSyntax destMemberAccess)
            {
                destMemberName = destMemberAccess.Name.Identifier.Text;
            }
        }

        if (destMemberName is null) return null;

        var memberMapping = new MemberMapping
        {
            DestinationMember = destMemberName
        };

        // Second arg: opt => opt.MapFrom(...) or opt => opt.Ignore()
        // Can also be chained: opt => opt.PreCondition(s => s.IsActive).MapFrom(s => s.Value)
        if (args[1].Expression is SimpleLambdaExpressionSyntax optionsLambda &&
            optionsLambda.Body is ExpressionSyntax optionsBody)
        {
            // Walk the chain of method calls to extract all options
            ExtractOptionsFromChain(optionsBody, memberMapping, semanticModel);
        }

        return memberMapping;
    }

    /// <summary>
    /// Extracts all options from a chained method call expression.
    /// Handles patterns like: opt.PreCondition(x).MapFrom(y)
    /// </summary>
    private static void ExtractOptionsFromChain(ExpressionSyntax expression, MemberMapping memberMapping, SemanticModel? semanticModel = null, CancellationToken cancellationToken = default)
    {
        var current = expression;

        while (current is InvocationExpressionSyntax optionsInvocation)
        {
            if (optionsInvocation.Expression is MemberAccessExpressionSyntax optionsMemberAccess)
            {
                var optionMethod = optionsMemberAccess.Name.Identifier.Text;

                switch (optionMethod)
                {
                    case "Ignore":
                        memberMapping.IsIgnored = true;
                        break;

                    case "MapFrom":
                        // v12.0.0: Check if it's generic MapFrom<TResolver>() for IValueResolver
                        if (optionsMemberAccess.Name is GenericNameSyntax genericMapFrom &&
                            optionsInvocation.ArgumentList.Arguments.Count == 0)
                        {
                            // MapFrom<TResolver>() - extract the resolver type
                            var typeArgs = genericMapFrom.TypeArgumentList.Arguments;
                            if (typeArgs.Count == 1)
                            {
                                var resolverTypeSyntax = typeArgs[0];
                                // Get the fully qualified type name
                                if (semanticModel != null)
                                {
                                    var typeInfo = semanticModel.GetTypeInfo(resolverTypeSyntax, cancellationToken);
                                    if (typeInfo.Type != null)
                                    {
                                        // Check if it implements IValueResolver<,,>
                                        var isResolver = typeInfo.Type.AllInterfaces
                                            .Any(i => i.Name == "IValueResolver" && i.TypeArguments.Length == 3);

                                        if (isResolver)
                                        {
                                            memberMapping.ResolverTypeName = typeInfo.Type
                                                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                                                .Replace("global::", "");
                                            break;
                                        }
                                    }
                                }
                                // Fallback: use the type name as-is
                                memberMapping.ResolverTypeName = resolverTypeSyntax.ToString();
                            }
                        }
                        else
                        {
                            // Standard MapFrom with lambda expression
                            var mapFromArg = optionsInvocation.ArgumentList.Arguments.FirstOrDefault();
                            if (mapFromArg?.Expression is SimpleLambdaExpressionSyntax mapFromLambda)
                            {
                                var paramName = mapFromLambda.Parameter.Identifier.Text;
                                var bodyText = mapFromLambda.Body.ToString();
                                memberMapping.SourceExpression = ReplaceParameterWithSource(bodyText, paramName);
                            }
                            else if (mapFromArg?.Expression is ParenthesizedLambdaExpressionSyntax mapFromMulti)
                            {
                                // v10.0.0: MapFrom((src, dest) => ...) - two parameters
                                var parameters = mapFromMulti.ParameterList.Parameters;
                                if (parameters.Count >= 2)
                                {
                                    var srcParam = parameters[0].Identifier.Text;
                                    var destParam = parameters[1].Identifier.Text;
                                    var bodyText = mapFromMulti.Body.ToString();
                                    // Replace source parameter with "source"
                                    bodyText = ReplaceParameterWithSource(bodyText, srcParam);
                                    // Replace destination parameter with "result"
                                    bodyText = bodyText.Replace($"{destParam}.", "result.");
                                    memberMapping.SourceExpression = bodyText;
                                    memberMapping.HasDestinationParameter = true;
                                }
                            }
                        }
                        break;

                    case "PreCondition":
                        memberMapping.HasPreCondition = true;
                        // v7.0.0: Extract the PreCondition expression
                        var preCondArg = optionsInvocation.ArgumentList.Arguments.FirstOrDefault();
                        if (preCondArg?.Expression is SimpleLambdaExpressionSyntax preCondLambda)
                        {
                            var paramName = preCondLambda.Parameter.Identifier.Text;
                            var bodyText = preCondLambda.Body.ToString();
                            memberMapping.PreConditionExpression = ReplaceParameterWithSource(bodyText, paramName);
                        }
                        break;

                    case "NullSubstitute":
                        // v8.1.0: Extract NullSubstitute value
                        var nullSubstArg = optionsInvocation.ArgumentList.Arguments.FirstOrDefault();
                        if (nullSubstArg?.Expression != null)
                        {
                            memberMapping.NullSubstituteExpression = nullSubstArg.Expression.ToString();
                            memberMapping.HasNullSubstitute = true;
                        }
                        break;

                    case "Condition":
                        // v8.1.0: Extract Condition expression
                        // Condition has multiple overloads:
                        // - Condition(Func<TSource, TDestination, TMember, bool>)
                        // - Condition(Func<TSource, TDestination, TMember, ResolutionContext, bool>)
                        var condArg = optionsInvocation.ArgumentList.Arguments.FirstOrDefault();
                        if (condArg?.Expression is SimpleLambdaExpressionSyntax condLambdaSimple)
                        {
                            // Single parameter: (srcMember) => srcMember > 0
                            memberMapping.HasCondition = true;
                            var paramName = condLambdaSimple.Parameter.Identifier.Text;
                            var bodyText = condLambdaSimple.Body.ToString();
                            // Replace parameter with "_value" which we'll use in generation
                            memberMapping.ConditionExpression = bodyText.Replace(paramName, "_value");
                        }
                        else if (condArg?.Expression is ParenthesizedLambdaExpressionSyntax condLambdaMulti)
                        {
                            // Multiple parameters: (src, dest, srcMember) => srcMember > 0
                            memberMapping.HasCondition = true;
                            var parameters = condLambdaMulti.ParameterList.Parameters;
                            var bodyText = condLambdaMulti.Body.ToString();

                            // Replace parameters:
                            // - First param (src) -> "source"
                            // - Second param (dest) -> "result"
                            // - Third param (srcMember) -> "_value"
                            if (parameters.Count >= 3)
                            {
                                bodyText = bodyText.Replace(parameters[0].Identifier.Text + ".", "source.");
                                bodyText = bodyText.Replace(parameters[1].Identifier.Text + ".", "result.");
                                bodyText = bodyText.Replace(parameters[2].Identifier.Text, "_value");
                            }
                            else if (parameters.Count == 1)
                            {
                                bodyText = bodyText.Replace(parameters[0].Identifier.Text, "_value");
                            }

                            memberMapping.ConditionExpression = bodyText;
                        }
                        break;
                }

                // Move to the inner expression (the receiver of the method call)
                current = optionsMemberAccess.Expression;
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>
    /// v9.0.0: Extracts a ForPath mapping.
    /// Pattern: ForPath(d => d.Address.Street, opt => opt.MapFrom(s => s.StreetName))
    /// </summary>
    private static PathMapping? ExtractForPathMapping(InvocationExpressionSyntax invocation)
    {
        var args = invocation.ArgumentList.Arguments;
        if (args.Count < 2) return null;

        // First arg: d => d.Address.Street (path expression)
        var pathSegments = new List<string>();
        if (args[0].Expression is SimpleLambdaExpressionSyntax destLambda)
        {
            // Walk the member access chain to extract segments
            var current = destLambda.Body;
            while (current is MemberAccessExpressionSyntax memberAccess)
            {
                pathSegments.Insert(0, memberAccess.Name.Identifier.Text);
                current = memberAccess.Expression;
            }
        }

        if (pathSegments.Count == 0) return null;

        var pathMapping = new PathMapping
        {
            PathSegments = pathSegments
        };

        // Second arg: opt => opt.MapFrom(s => s.Property) or opt => opt.Ignore()
        if (args[1].Expression is SimpleLambdaExpressionSyntax optionsLambda &&
            optionsLambda.Body is InvocationExpressionSyntax optionsInvocation)
        {
            if (optionsInvocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var methodName = memberAccess.Name.Identifier.Text;

                if (methodName == "MapFrom")
                {
                    var mapFromArg = optionsInvocation.ArgumentList.Arguments.FirstOrDefault();
                    if (mapFromArg?.Expression is SimpleLambdaExpressionSyntax mapFromLambda)
                    {
                        var sourceParamName = mapFromLambda.Parameter.Identifier.Text;
                        var bodyText = mapFromLambda.Body.ToString();
                        pathMapping.SourceExpression = ReplaceParameterWithSource(bodyText, sourceParamName);
                    }
                }
                else if (methodName == "Ignore")
                {
                    pathMapping.IsIgnored = true;
                }
            }
        }

        return pathMapping;
    }

    /// <summary>
    /// v9.0.0: Extracts a ForCtorParam mapping.
    /// Pattern: ForCtorParam("paramName", opt => opt.MapFrom(s => s.Property))
    /// </summary>
    private static CtorParamMapping? ExtractForCtorParamMapping(InvocationExpressionSyntax invocation)
    {
        var args = invocation.ArgumentList.Arguments;
        if (args.Count < 2) return null;

        // First arg: parameter name as string literal
        string? paramName = null;
        if (args[0].Expression is LiteralExpressionSyntax literal &&
            literal.Kind() == SyntaxKind.StringLiteralExpression)
        {
            paramName = literal.Token.ValueText;
        }

        if (paramName is null) return null;

        var ctorMapping = new CtorParamMapping
        {
            ParameterName = paramName
        };

        // Second arg: opt => opt.MapFrom(s => s.Property)
        if (args[1].Expression is SimpleLambdaExpressionSyntax optionsLambda &&
            optionsLambda.Body is InvocationExpressionSyntax optionsInvocation)
        {
            if (optionsInvocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name.Identifier.Text == "MapFrom")
            {
                var mapFromArg = optionsInvocation.ArgumentList.Arguments.FirstOrDefault();
                if (mapFromArg?.Expression is SimpleLambdaExpressionSyntax mapFromLambda)
                {
                    var sourceParamName = mapFromLambda.Parameter.Identifier.Text;
                    var bodyText = mapFromLambda.Body.ToString();
                    ctorMapping.SourceExpression = ReplaceParameterWithSource(bodyText, sourceParamName);
                }
            }
        }

        return ctorMapping;
    }

    /// <summary>
    /// Replaces lambda parameter with "source" for generated code.
    /// v12.1.0: Also handles standalone parameter (e.g., "src" -> "source").
    /// </summary>
    private static string ReplaceParameterWithSource(string expression, string paramName)
    {
        // Replace all occurrences of the parameter followed by . or in expressions
        // Handle patterns like: s.Prop, s?.Prop, $"{s.Prop}", s.Prop + s.Other, etc.
        // v12.1.0: Also handle standalone parameter: "src" -> "source"

        var result = new System.Text.StringBuilder();
        int i = 0;
        int paramLen = paramName.Length;

        while (i < expression.Length)
        {
            // Check if we're at the start of the parameter name
            if (i + paramLen <= expression.Length &&
                expression.Substring(i, paramLen) == paramName)
            {
                // Check if it's a full identifier (not part of another word)
                bool isStartOk = i == 0 || !char.IsLetterOrDigit(expression[i - 1]);

                // Check what comes after the parameter name
                int afterParam = i + paramLen;
                bool isEndOk = afterParam >= expression.Length || !char.IsLetterOrDigit(expression[afterParam]);

                if (isStartOk && isEndOk)
                {
                    // v12.1.0: Replace if:
                    // 1. Followed by . or ?. or [ (member/index access)
                    // 2. End of expression (standalone parameter)
                    // 3. Followed by whitespace, comma, parenthesis, etc.
                    if (afterParam >= expression.Length)
                    {
                        // Standalone parameter at end of expression
                        result.Append("source");
                        i += paramLen;
                        continue;
                    }

                    char nextChar = expression[afterParam];
                    if (nextChar == '.' || nextChar == '?' || nextChar == '[' ||
                        nextChar == ')' || nextChar == ',' || nextChar == ' ' ||
                        nextChar == '+' || nextChar == '-' || nextChar == '*' ||
                        nextChar == '/' || nextChar == '!' || nextChar == '=' ||
                        nextChar == '<' || nextChar == '>' || nextChar == '&' ||
                        nextChar == '|' || nextChar == ':' || nextChar == ';')
                    {
                        result.Append("source");
                        i += paramLen;
                        continue;
                    }
                }
            }
            result.Append(expression[i]);
            i++;
        }

        return result.ToString();
    }

    /// <summary>
    /// Main execution - generates source files for all profiles.
    /// </summary>
    private static void Execute(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<ProfileInfo?> profiles)
    {
        if (profiles.IsDefaultOrEmpty) return;

        var validProfiles = profiles.Where(p => p is not null && p.Mappings.Count > 0).ToList();
        if (validProfiles.Count == 0) return;

        var codeBuilder = new MappingCodeBuilder();

        foreach (var profile in validProfiles)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var generatedCode = codeBuilder.GenerateMapperClass(profile!, compilation, context);

            // Use namespace + full class name (including parent classes for nested types) to ensure unique file names
            var fullName = string.IsNullOrEmpty(profile!.FullClassName)
                ? profile.ClassName
                : profile.FullClassName;
            var fileNamePrefix = string.IsNullOrEmpty(profile.Namespace)
                ? fullName
                : profile.Namespace.Replace(".", "_") + "_" + fullName;
            var fileName = $"{fileNamePrefix}GeneratedMappers.g.cs";
            context.AddSource(fileName, generatedCode);
        }

        // Generate registry initialization
        var registryCode = codeBuilder.GenerateRegistryInitializer(validProfiles!);
        context.AddSource("HyperMapperGeneratedRegistry.g.cs", registryCode);

        // v8.0.0: Generate direct dispatch for zero-overhead type switching
        var dispatchCode = codeBuilder.GenerateDirectDispatch(validProfiles!);
        context.AddSource("GeneratedMapperDispatch.g.cs", dispatchCode);

        // v11.0.0: Generate static Mapper for max performance (~21ns)
        var staticMapperCode = codeBuilder.GenerateStaticMapper();
        context.AddSource("Mapper.g.cs", staticMapperCode);
    }
}
