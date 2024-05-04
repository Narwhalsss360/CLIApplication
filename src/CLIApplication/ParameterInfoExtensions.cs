using System.Reflection;

namespace CLIApplication
{
    public static class ParameterInfoExtensions
    {
        public static readonly Type[] SupportedTypes = new Type[]
        {
            typeof(string),
            typeof(byte),
            typeof(bool),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(Enum)
        };

        public static bool IsSupported(Type type)
        {
            type = type.NullableValueType();
            if (SupportedTypes.Contains(type))
                return true;
            return type.IsEnum;
        }

        public static Type NullableValueType(this Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                return type.GetGenericArguments()[0];
            return type;
        }

        public static object? ParseAs(this string entry, Type type)
        {
            type = type.NullableValueType();

            if (type.IsEnum)
            {
                if (Enum.TryParse(type, entry, out object? result))
                    return result;
                return null;
            }

            if (!SupportedTypes.Contains(type))
                throw new InvalidProgramException("Unsupported type");

            if (type == typeof(string))
                return entry;

            if (type == typeof(byte))
            {
                if (byte.TryParse(entry, out byte value))
                    return value;
                return null;
            }

            if (type == typeof(bool))
            {
                if (bool.TryParse(entry, out bool value))
                    return value;
                return null;
            }

            if (type == typeof(short))
            {
                if (short.TryParse(entry, out short value))
                    return value;
                return null;
            }

            if (type == typeof(ushort))
            {
                if (ushort.TryParse(entry, out ushort value))
                    return value;
                return null;
            }

            if (type == typeof(int))
            {
                if (int.TryParse(entry, out int value))
                    return value;
                return null;
            }

            if (type == typeof(uint))
            {
                if (uint.TryParse(entry, out uint value))
                    return value;
                return null;
            }

            if (type == typeof(long))
            {
                if (long.TryParse(entry, out long value))
                    return value;
                return null;
            }

            if (type == typeof(ulong))
            {
                if (ulong.TryParse(entry, out ulong value))
                    return value;
                return null;
            }

            if (type == typeof(float))
            {
                if (float.TryParse(entry, out float value))
                    return value;
                return null;
            }

            if (type == typeof(double))
            {
                if (double.TryParse(entry, out double value))
                    return value;
                return null;
            }

            if (type == typeof(decimal))
            {
                if (decimal.TryParse(entry, out decimal value))
                    return value;
                return null;
            }

            return null;
        }

        public static object?[] ParseArguments(this Type[] types, string[] entries, Dictionary<int, object?>? defaultValues = null)
        {
            object?[] arguments = new object?[types.Length];

            for (int pos = 0; pos < types.Length; pos++)
            {
                string evaluating;
                Type type = types[pos];
                if (defaultValues is not null)
                {

                    if (Array.Find(entries, entry => entry.StartsWith($"{type.Name}=", StringComparison.InvariantCulture)) is string namedEntry)
                    {
                        if (type.Name is null)
                            continue;
                        evaluating = namedEntry[(type.Name.Length + 1)..];
                    }
                    else if (pos < entries.Length)
                        evaluating = entries[pos];
                    else if (defaultValues.ContainsKey(pos))
                    {
                        arguments[pos] = defaultValues[pos];
                        continue;
                    }
                    else
                        continue;
                }
                else
                    evaluating = entries[pos];

                arguments[pos] = evaluating.ParseAs(type);
            }
            return arguments;
        }

        public static object?[] ParseArguments(this ParameterInfo[] parameters, string[] entries, Dictionary<int, object?>? preset = null)
        {
            object?[] arguments = new object?[parameters.Length];

            foreach (var parameter in parameters)
            {
                if (preset is not null)
                {
                    if (preset.ContainsKey(parameter.Position))
                    {
                        arguments[parameter.Position] = preset[parameter.Position];
                        continue;
                    }
                }

                string evaluating;

                if (parameter.HasDefaultValue)
                {
                    if (Array.Find(entries, entry => entry.StartsWith($"{parameter.Name}=", StringComparison.InvariantCulture)) is string namedEntry)
                    {
                        if (parameter.Name is null)
                            throw new InvalidProgramException("Parameter must be named.");
                        evaluating = namedEntry[(parameter.Name.Length + 1)..];
                    }
                    else if (parameter.Position < entries.Length)
                    {
                        evaluating = entries[parameter.Position];
                    }
                    else
                    {
                        arguments[parameter.Position] = parameter.DefaultValue;
                        continue;
                    }
                }
                else if (parameter.Position < entries.Length)
                    evaluating = entries[parameter.Position];
                else
                    throw new ArgumentException($"Not enough arguments, missing at position {parameter.Position}");

                arguments[parameter.Position] = evaluating.ParseAs(parameter.ParameterType);
            }

            return arguments;
        }
    }
}
