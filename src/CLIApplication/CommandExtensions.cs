﻿using System.ComponentModel;
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

        public static string GetCommandName(this Command command)
        {
            if (command.Info.GetCustomAttribute<DisplayNameAttribute>() is DisplayNameAttribute displayName)
                return displayName.DisplayName;
            return command.Info.GetDeclaredName();
        }


        public static string GetCommandDescription(this Command command)
        {
            if (command.Info.GetCustomAttribute<DescriptionAttribute>() is DescriptionAttribute attribute)
                return attribute.Description;
            return $"{command.GetCommandName()}: unfinished!";
        }

        public static int RequiredCommandArguments(this Command command)
        {
            int count = 0;
            foreach (ParameterInfo parameter in command.Info.GetParameters())
            {
                if (parameter.HasDefaultValue ||
                    parameter.ParameterType == typeof(string[]) ||
                    parameter.ParameterType == typeof(CLIInterpreter)) //what about `CLIInterpreter?`?
                    continue;
                count++;
            }
            return count;
        }

        public static Command? FindCommand(this Command[] commands, string commandName, bool ignoreCase = false)
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