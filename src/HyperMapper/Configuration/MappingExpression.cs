using System.Linq.Expressions;
using HyperMapper.Internal;

namespace HyperMapper.Configuration;

internal class MappingExpressionBase : IMappingExpressionBase
{
    private readonly TypeMap _typeMap;

    internal MappingExpressionBase(TypeMap typeMap)
    {
        _typeMap = typeMap;
    }

    public IMappingExpressionBase ConvertUsing(Type converterType)
    {
        _typeMap.SetConverterType(converterType);
        return this;
    }
}

internal class MappingExpression<TSource, TDestination> : IMappingExpression<TSource, TDestination>
{
    private readonly TypeMap _typeMap;
    private readonly Profile _profile;

    internal MappingExpression(TypeMap typeMap, Profile profile)
    {
        _typeMap = typeMap;
        _profile = profile;
    }

    public IMappingExpression<TSource, TDestination> ForMember<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember,
        Action<IMemberConfigurationExpression<TSource, TDestination, TMember>> memberOptions)
    {
        var memberName = GetMemberName(destinationMember);
        var memberConfig = new MemberConfigurationExpression<TSource, TDestination, TMember>(memberName);
        memberOptions(memberConfig);
        _typeMap.AddMemberMap(memberConfig.ToMemberMap());
        return this;
    }

    public IMappingExpression<TDestination, TSource> ReverseMap()
    {
        var reverseTypeMap = new TypeMap(typeof(TDestination), typeof(TSource));
        _profile.AddTypeMap(reverseTypeMap);
        return new MappingExpression<TDestination, TSource>(reverseTypeMap, _profile);
    }

    public IMappingExpression<TSource, TDestination> ConvertUsing(ITypeConverter<TSource, TDestination> converter)
    {
        _typeMap.SetConverter(converter);
        return this;
    }

    public IMappingExpression<TSource, TDestination> ConvertUsing<TConverter>()
        where TConverter : ITypeConverter<TSource, TDestination>, new()
    {
        _typeMap.SetConverterType(typeof(TConverter));
        return this;
    }

    public IMappingExpression<TSource, TDestination> ConvertUsing(Func<TSource, TDestination> converter)
    {
        _typeMap.SetLambdaConverter(converter);
        return this;
    }

    IMappingExpressionBase IMappingExpressionBase.ConvertUsing(Type converterType)
    {
        _typeMap.SetConverterType(converterType);
        return this;
    }

    IMappingExpression<TSource, TDestination> IMappingExpression<TSource, TDestination>.ConvertUsing(Type converterType)
    {
        _typeMap.SetConverterType(converterType);
        return this;
    }

    // ===== v8.0.0: New AutoMapper-compatible features =====

    public IMappingExpression<TSource, TDestination> AddTransform<TValue>(Expression<Func<TValue, TValue>> transformer)
    {
        _typeMap.AddTransform(typeof(TValue), transformer);
        return this;
    }

    public IMappingExpression<TSource, TDestination> BeforeMap(Action<TSource, TDestination> beforeFunction)
    {
        _typeMap.SetBeforeMap(new Action<object, object>((src, dest) => beforeFunction((TSource)src, (TDestination)dest)));
        return this;
    }

    public IMappingExpression<TSource, TDestination> BeforeMap(Action<TSource, TDestination, ResolutionContext> beforeFunction)
    {
        _typeMap.SetBeforeMapWithContext(new Action<object, object, ResolutionContext>((src, dest, ctx) => beforeFunction((TSource)src, (TDestination)dest, ctx)));
        return this;
    }

    public IMappingExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination> afterFunction)
    {
        _typeMap.SetAfterMap(new Action<object, object>((src, dest) => afterFunction((TSource)src, (TDestination)dest)));
        return this;
    }

    public IMappingExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination, ResolutionContext> afterFunction)
    {
        _typeMap.SetAfterMapWithContext(new Action<object, object, ResolutionContext>((src, dest, ctx) => afterFunction((TSource)src, (TDestination)dest, ctx)));
        return this;
    }

    public IMappingExpression<TSource, TDestination> ForPath<TMember>(
        Expression<Func<TDestination, TMember>> destinationPath,
        Action<IPathConfigurationExpression<TSource, TDestination, TMember>> pathOptions)
    {
        var pathConfig = new PathConfigurationExpression<TSource, TDestination, TMember>(destinationPath);
        pathOptions(pathConfig);
        _typeMap.AddPathMap(pathConfig.ToPathMap());
        return this;
    }

    public IMappingExpression<TSource, TDestination> MaxDepth(int depth)
    {
        _typeMap.SetMaxDepth(depth);
        return this;
    }

    public IMappingExpression<TSource, TDestination> PreserveReferences()
    {
        _typeMap.SetPreserveReferences();
        return this;
    }

    public IMappingExpression<TSource, TDestination> IncludeMembers(params Expression<Func<TSource, object?>>[] memberExpressions)
    {
        foreach (var expr in memberExpressions)
        {
            _typeMap.AddIncludedMember(expr);
        }
        return this;
    }

    public IMappingExpression<TSource, TDestination> Include<TDerivedSource, TDerivedDestination>()
        where TDerivedSource : TSource
        where TDerivedDestination : TDestination
    {
        _typeMap.AddIncludedDerivedType(typeof(TDerivedSource), typeof(TDerivedDestination));
        return this;
    }

    public IMappingExpression<TSource, TDestination> IncludeBase<TBaseSource, TBaseDestination>()
    {
        // Runtime validation: TSource must be assignable to TBaseSource
        if (!typeof(TBaseSource).IsAssignableFrom(typeof(TSource)))
            throw new InvalidOperationException($"{typeof(TSource).Name} must derive from {typeof(TBaseSource).Name}");
        if (!typeof(TBaseDestination).IsAssignableFrom(typeof(TDestination)))
            throw new InvalidOperationException($"{typeof(TDestination).Name} must derive from {typeof(TBaseDestination).Name}");

        _typeMap.SetIncludedBaseType(typeof(TBaseSource), typeof(TBaseDestination));
        return this;
    }

    public IMappingExpression<TSource, TDestination> ValidateMemberList(MemberList memberList)
    {
        _typeMap.SetValidateMemberList(memberList);
        return this;
    }

    public IMappingExpression<TSource, TDestination> ConstructUsing(Func<TSource, TDestination> constructor)
    {
        _typeMap.SetConstructUsing(new Func<object, object>((src) => constructor((TSource)src)!));
        return this;
    }

    public IMappingExpression<TSource, TDestination> ConstructUsing(Func<TSource, ResolutionContext, TDestination> constructor)
    {
        _typeMap.SetConstructUsingWithContext(new Func<object, ResolutionContext, object>((src, ctx) => constructor((TSource)src, ctx)!));
        return this;
    }

    public IMappingExpression<TSource, TDestination> ForCtorParam(
        string ctorParamName,
        Action<ICtorParamConfigurationExpression<TSource>> config)
    {
        var ctorConfig = new CtorParamConfigurationExpression<TSource>(ctorParamName);
        config(ctorConfig);
        _typeMap.AddCtorParamMap(ctorConfig.ToCtorParamMap());
        return this;
    }

    public IMappingExpression<TSource, TDestination> ForAllMembers(
        Action<IMemberConfigurationExpression<TSource, TDestination, object>> memberOptions)
    {
        _typeMap.SetForAllMembers((string memberName) =>
        {
            var memberConfig = new MemberConfigurationExpression<TSource, TDestination, object>(memberName);
            memberOptions(memberConfig);
            return memberConfig.ToMemberMap();
        });
        return this;
    }

    public IMappingExpression<TSource, TDestination> ForAllOtherMembers(
        Action<IMemberConfigurationExpression<TSource, TDestination, object>> memberOptions)
    {
        _typeMap.SetForAllOtherMembers((string memberName) =>
        {
            var memberConfig = new MemberConfigurationExpression<TSource, TDestination, object>(memberName);
            memberOptions(memberConfig);
            return memberConfig.ToMemberMap();
        });
        return this;
    }

    private static string GetMemberName<TMember>(Expression<Func<TDestination, TMember>> expression)
    {
        if (expression.Body is MemberExpression memberExpr)
            return memberExpr.Member.Name;

        // Handle unary expressions (for nullable types)
        if (expression.Body is UnaryExpression unaryExpr && unaryExpr.Operand is MemberExpression innerMemberExpr)
            return innerMemberExpr.Member.Name;

        throw new ArgumentException("Expression must be a member access expression");
    }
}
