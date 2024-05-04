using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using static CLIApplication.CLIInterpreter;

namespace CLIApplication
{
    public static class CommandExtensions
    {
        public static string GetDeclaredName(this MethodInfo methodInfo)
        {
            string name = methodInfo.Name;
            if (!name.Contains("__"))
                return name;
            //Top-level declaration
            int start = name.LastIndexOf("__", StringComparison.InvariantCulture) + 2;
            int count = name.LastIndexOf("|", StringComparison.InvariantCulture) - start;
            return name.Substring(start, count);
        }

        public static string GetDisplayName(this MethodInfo methodInfo)
        {
            if (methodInfo.GetCustomAttribute<DisplayNameAttribute>() is DisplayNameAttribute displayName)
                return displayName.DisplayName;
            return methodInfo.GetDeclaredName();
        }

        public static string GetCommandName(this Command command) => command.Info.GetDisplayName();

        public static string GetParametersDescription(this ParameterInfo[] parameters)
        {
            string description = "";
            foreach (var parameter in parameters)
            {
                if (parameter.ParameterType == typeof(string[]))
                    continue;

                if (parameter.Position != 0)
                    description += ", ";

                if (parameter.ParameterType == typeof(object[]))
                {
                    description += $"object[Variable] {parameter.Name}";
                    continue;
                }
                else
                {
                    description += $"{parameter.ParameterType.NullableValueType().Name} {parameter.Name}";
                }
            }
            return description;
        }

        public static string GetDescription(this MethodInfo methodInfo)
        {
            string description = $"{methodInfo.GetDisplayName()}(";
            description += methodInfo.GetParameters().GetParametersDescription();
            description += ")";
            if (methodInfo.GetCustomAttribute<DescriptionAttribute>() is DescriptionAttribute attribute)
                description += $":{attribute.Description}";
            return description;
        }

        public static string GetCommandDescription(this Command command) => command.Info.GetDescription();

        public static string GetFullDescription(this Command command)
        {
            string fullDescription = command.GetCommandDescription();
            foreach (ParameterInfo parameterInfo in command.Info.GetParameters())
            {
                if (parameterInfo.ParameterType.IsEnum)
                {
                    fullDescription += $"\n\t{parameterInfo.Name}:";
                    if (parameterInfo.GetCustomAttribute<DescriptionAttribute>() is DescriptionAttribute descriptionAttribute)
                        fullDescription += $"{descriptionAttribute.Description}";
                    foreach (FieldInfo field in parameterInfo.ParameterType.GetFields())
                    {
                        if (field.FieldType != parameterInfo.ParameterType)
                            continue;
                        fullDescription += $"\n\t\t{field.Name}";
                        if (field.GetCustomAttribute<DescriptionAttribute>() is DescriptionAttribute valueDescriptionAttribute)
                            fullDescription += $":{valueDescriptionAttribute.Description}";
                    }    
                }
                else if (parameterInfo.GetCustomAttribute<DescriptionAttribute>() is DescriptionAttribute descriptionAttribute)
                {
                    fullDescription += $"\n\t{parameterInfo.Name}:{descriptionAttribute.Description}";
                }
            }
            return fullDescription;
        }

        public static int RequiredArguments(this ParameterInfo[] parameters)
        {
            int count = 0;
            foreach (ParameterInfo parameter in parameters)
            {
                if (parameter.HasDefaultValue)
                    continue;
                count++;
            }
            return count;
        }

        public static int RequiredCommandArguments(this Command command)
        {
            int count = 0;
            foreach (ParameterInfo parameter in command.Info.GetParameters())
            {
                if (parameter.HasDefaultValue ||
                    parameter.ParameterType == typeof(string[]) ||
                    parameter.ParameterType == typeof(CLIInterpreter) ||
                    parameter.ParameterType == typeof(object[]))
                    continue;
                count++;
            }
            return count;
        }

        public static Command? FindCommand(this IEnumerable<Command> commands, string commandName, bool ignoreCase = false)
        {
            foreach (var command in commands)
            {
                if (ignoreCase)
                {
                    if (command.GetCommandName().ToLower(CultureInfo.InvariantCulture) == commandName.ToLower(CultureInfo.InvariantCulture))
                        return command;
                }
                else
                {
                    if (command.GetCommandName() == commandName)
                        return command;
                }
            }
            return null;
        }
    }
}
