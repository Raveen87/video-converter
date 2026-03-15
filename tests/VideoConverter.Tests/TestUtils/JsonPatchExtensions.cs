using System.Linq.Expressions;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Operations;

namespace VideoConverter.Tests.TestUtils;

public static class JsonPatchExtensions
{
    public static void Replace<T, TProp>(this JsonPatchDocument<T> doc, Expression<Func<T, TProp>> path, TProp value)
        where T : class
    {
        var propertyName = GetPropertyName(path);
        doc.Operations.Add(new Operation<T>
        {
            op = "replace",
            path = "/" + propertyName,
            value = value,
        });
    }

    private static string GetPropertyName<T, TProp>(Expression<Func<T, TProp>> expression)
    {
        if (expression.Body is MemberExpression member)
        {
            return member.Member.Name;
        }

        if (expression.Body is UnaryExpression unary && unary.Operand is MemberExpression unaryMember)
        {
            return unaryMember.Member.Name;
        }

        throw new ArgumentException("Expression must be a member access expression (e.g., x => x.PropertyName).", nameof(expression));
    }
}
