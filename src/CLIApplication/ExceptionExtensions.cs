using System.Reflection;

namespace CLIApplication
{
    public static class ExceptionExtensions
    {
        public static bool WasThrownBy(this Exception e, Delegate function)
        {
            if (e.TargetSite is null)
                return false;
            return e.TargetSite == function.GetMethodInfo();
        }
    }
}
