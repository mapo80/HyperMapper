using System.Linq.Expressions;

namespace HyperMapper.Internal;

/// <summary>
/// ExpressionVisitor that replaces a ParameterExpression with another Expression.
/// Used to rewrite MapFrom lambdas to use the local parameter from the execution plan.
///
/// Example:
///   Original: s => s.Sub.ProperName  (where s is the lambda parameter)
///   Rewritten: typedSource.Sub.ProperName  (where typedSource is the local variable)
/// </summary>
internal class ParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _oldParam;
    private readonly Expression _newExpr;

    public ParameterReplacer(ParameterExpression oldParam, Expression newExpr)
    {
        _oldParam = oldParam ?? throw new ArgumentNullException(nameof(oldParam));
        _newExpr = newExpr ?? throw new ArgumentNullException(nameof(newExpr));
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        // If this is the parameter we want to replace, return the new expression
        if (node == _oldParam)
        {
            return _newExpr;
        }
        return base.VisitParameter(node);
    }
}
