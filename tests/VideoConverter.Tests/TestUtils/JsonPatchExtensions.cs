using System.Linq.Expressions;
using System.Text.Json;
using VideoConverter.Api.Shared.Infrastructure.Serialization;

namespace VideoConverter.Tests.TestUtils;

public static class JsonPatchExtensions
{
    public static void Replace<T, TProp>(this JsonPatchDocument<T> doc, Expression<Func<T, TProp>> path, TProp value)
        where T : class
    {
        var propertyName = GetPropertyName(path);
        doc.Operations.Add(new JsonPatchOperation
        {
            Op = "replace",
            Path = propertyName,
            Value = JsonSerializer.SerializeToNode(value),
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

        throw new ArgumentException("Invalid expression");
    }
}
