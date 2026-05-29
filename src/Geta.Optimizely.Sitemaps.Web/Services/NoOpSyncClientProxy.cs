using System.Reflection;

namespace Geta.Optimizely.Sitemaps.Web.Services;

internal class NoOpSyncClientProxy : DispatchProxy
{
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        var returnType = targetMethod!.ReturnType;

        if (returnType == typeof(Task))
            return Task.CompletedTask;

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = returnType.GetGenericArguments()[0];
            var defaultValue = resultType.IsValueType ? Activator.CreateInstance(resultType) : null;
            return typeof(Task)
                .GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(resultType)
                .Invoke(null, [defaultValue]);
        }

        return null;
    }
}
