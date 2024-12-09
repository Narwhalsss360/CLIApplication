using System.Linq.Expressions;
using System.Reflection;

namespace CLIApplication
{
    public static class MethodInfoExtensions
    {
        public static Delegate CreateDelegate(this MethodInfo methodInfo, object? target = null)
        {
            var paramTypes = methodInfo.GetParameters().Select(parm => parm.ParameterType);
            var paramTypesAndReturnType = paramTypes.Append(methodInfo.ReturnType).ToArray();
            var delegateType = Expression.GetDelegateType(paramTypesAndReturnType);

            if (methodInfo.IsStatic)
                return methodInfo.CreateDelegate(delegateType);
            return methodInfo.CreateDelegate(delegateType, target);
        }
    }
}
