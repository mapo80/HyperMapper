using System.Linq.Expressions;
using HyperMapper.Internal;

namespace HyperMapper.Configuration;

/// <summary>
/// v8.0.0: Implementation of ForPath() configuration.
/// </summary>
internal class PathConfigurationExpression<TSource, TDestination, TMember>
    : IPathConfigurationExpression<TSource, TDestination, TMember>
{
    private readonly LambdaExpression _destinationPath;
    private readonly List<string> _pathSegments;
    private LambdaExpression? _sourceExpression;
    private Delegate? _sourceValueResolver;
    private bool _ignored;
    private Func<object, bool>? _condition;

    public PathConfigurationExpression(Expression<Func<TDestination, TMember>> destinationPath)
    {
        _destinationPath = destinationPath;
        _pathSegments = ExtractPathSegments(destinationPath);
    }

    public void MapFrom<TSourceMember>(Expression<Func<TSource, TSourceMember>> sourceMember)
    {
        _sourceExpression = sourceMember;
        _sourceValueResolver = sourceMember.Compile();
    }

    public void Ignore()
    {
        _ignored = true;
    }

    public void Condition(Func<TSource, bool> condition)
    {
        _condition = obj => condition((TSource)obj);
    }

    internal PathMemberMap ToPathMap()
    {
        return new PathMemberMap(_pathSegments)
        {
            SourceExpression = _sourceExpression,
            SourceValueResolver = _sourceValueResolver,
            Ignored = _ignored,
            Condition = _condition
        };
    }

    private static List<string> ExtractPathSegments<T>(Expression<Func<TDestination, T>> expression)
    {
        var segments = new List<string>();
        var current = expression.Body;

        while (current is MemberExpression memberExpr)
        {
            segments.Insert(0, memberExpr.Member.Name);
            current = memberExpr.Expression;
        }

        // Handle unary expressions (for nullable types)
        if (current is UnaryExpression unaryExpr && unaryExpr.Operand is MemberExpression innerMemberExpr)
        {
            segments.Insert(0, innerMemberExpr.Member.Name);
        }

        return segments;
    }
}
