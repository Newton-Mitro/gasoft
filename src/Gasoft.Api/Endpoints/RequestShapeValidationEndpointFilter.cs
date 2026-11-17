using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;

namespace Gasoft.Api.Endpoints;

internal static class RequestShapeValidationEndpointFilter
{
    public static EndpointFilterDelegate Create(
        EndpointFilterFactoryContext factoryContext,
        EndpointFilterDelegate next)
    {
        var requestParameters = factoryContext.MethodInfo
            .GetParameters()
            .Select((parameter, index) => (parameter, index))
            .Where(item => IsRequestType(item.parameter.ParameterType))
            .ToArray();

        return async invocationContext =>
        {
            var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var nullability = new NullabilityInfoContext();

            foreach (var (parameter, index) in requestParameters)
            {
                var argument = invocationContext.Arguments[index];
                if (argument is null)
                {
                    AddError(
                        errors,
                        JsonNamingPolicy.CamelCase.ConvertName(parameter.Name ?? "request"),
                        "The request body is required.");
                    continue;
                }

                ValidateObject(
                    argument,
                    string.Empty,
                    errors,
                    new HashSet<object>(ReferenceEqualityComparer.Instance),
                    nullability);
            }

            if (errors.Count != 0)
            {
                return Results.ValidationProblem(
                    errors.ToDictionary(error => error.Key, error => error.Value.ToArray(), StringComparer.Ordinal));
            }

            return await next(invocationContext);
        };
    }

    private static void ValidateObject(
        object instance,
        string prefix,
        Dictionary<string, List<string>> errors,
        HashSet<object> visited,
        NullabilityInfoContext nullabilityContext)
    {
        if (!visited.Add(instance))
        {
            return;
        }

        foreach (var property in instance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetIndexParameters().Length != 0 || property.GetMethod is null)
            {
                continue;
            }

            var value = property.GetValue(instance);
            var key = Join(prefix, JsonNamingPolicy.CamelCase.ConvertName(property.Name));

            var validationResults = new List<ValidationResult>();
            Validator.TryValidateProperty(
                value,
                new ValidationContext(instance) { MemberName = property.Name },
                validationResults);

            foreach (var result in validationResults)
            {
                AddError(errors, key, result.ErrorMessage ?? "The value is invalid.");
            }

            var nullability = nullabilityContext.Create(property);
            if (value is null)
            {
                if (!property.PropertyType.IsValueType && nullability.ReadState == NullabilityState.NotNull)
                {
                    AddError(errors, key, "The field is required.");
                }

                continue;
            }

            if (value is string text)
            {
                if (nullability.ReadState == NullabilityState.NotNull && string.IsNullOrWhiteSpace(text))
                {
                    AddError(errors, key, "The field is required.");
                }

                if (property.Name.EndsWith("Id", StringComparison.Ordinal) &&
                    long.TryParse(text, out var stringId) &&
                    stringId <= 0)
                {
                    AddError(errors, key, "The identifier must be greater than zero.");
                }

                continue;
            }

            if (property.Name.EndsWith("Id", StringComparison.Ordinal) && IsIntegral(value.GetType()) &&
                Convert.ToInt64(value) <= 0)
            {
                AddError(errors, key, "The identifier must be greater than zero.");
            }

            if (value is DateOnly date && date == DateOnly.MinValue)
            {
                AddError(errors, key, "The date is required.");
            }

            if (value is IEnumerable collection)
            {
                var index = 0;
                foreach (var item in collection)
                {
                    if (item is not null && IsRequestObjectType(item.GetType()))
                    {
                        ValidateObject(item, $"{key}[{index}]", errors, visited, nullabilityContext);
                    }
                    else if (item is not null &&
                             property.Name.EndsWith("Ids", StringComparison.Ordinal) &&
                             IsIntegral(item.GetType()) &&
                             Convert.ToInt64(item) <= 0)
                    {
                        AddError(errors, $"{key}[{index}]", "The identifier must be greater than zero.");
                    }
                    else if (item is null && GetElementNullability(property, nullabilityContext) == NullabilityState.NotNull)
                    {
                        AddError(errors, $"{key}[{index}]", "The item is required.");
                    }

                    index++;
                }
            }
            else if (IsRequestObjectType(value.GetType()))
            {
                ValidateObject(value, key, errors, visited, nullabilityContext);
            }
        }
    }

    private static NullabilityState GetElementNullability(
        PropertyInfo property,
        NullabilityInfoContext nullabilityContext)
    {
        var nullability = nullabilityContext.Create(property);
        var elementState = nullability.ElementType?.ReadState ??
                           nullability.GenericTypeArguments.FirstOrDefault()?.ReadState;
        if (elementState.HasValue)
        {
            return elementState.Value;
        }

        return NullabilityState.Unknown;
    }

    private static bool IsRequestType(Type type) =>
        type.IsClass &&
        type.Name.EndsWith("Request", StringComparison.Ordinal);

    private static bool IsRequestObjectType(Type type) =>
        IsRequestType(type) ||
        (type.IsClass &&
         type.Namespace?.StartsWith("Gasoft.Application.", StringComparison.Ordinal) == true);

    private static bool IsIntegral(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type == typeof(byte) || type == typeof(sbyte) ||
               type == typeof(short) || type == typeof(ushort) ||
               type == typeof(int) || type == typeof(uint) ||
               type == typeof(long) || type == typeof(ulong);
    }

    private static string Join(string prefix, string propertyName) =>
        prefix.Length == 0 ? propertyName : $"{prefix}.{propertyName}";

    private static void AddError(Dictionary<string, List<string>> errors, string key, string message)
    {
        if (!errors.TryGetValue(key, out var messages))
        {
            messages = [];
            errors.Add(key, messages);
        }

        if (!messages.Contains(message, StringComparer.Ordinal))
        {
            messages.Add(message);
        }
    }
}
