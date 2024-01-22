using Microsoft.VisualBasic;
using System.ComponentModel;
using System.Globalization;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;

namespace CLIApplication
{
    public class CLIInterpreter
    {
        public readonly struct Command : IFormattable
        {
            public static readonly Type[] SupportedTypes =
            {
                typeof(string),
                typeof(bool),
                typeof(decimal),
            };

            public Delegate Delegate { get; init; }

            public MethodInfo Info { get; init; }

            public Command(Delegate @delegate, MethodInfo info)
            {
                bool hasFlags = false;
                bool hasCaller = false;
                foreach (var parameter in info.GetParameters())
                {
                    if (parameter.Name is null)
                        throw new InvalidProgramException($"Parameter name must not be null. {info}");
                    if (SupportedTypes.Contains(parameter.ParameterType))
                        continue;
                    if (parameter.ParameterType.IsEnum)
                        continue;

                    if (parameter.ParameterType == typeof(string[]))
                    {
                        if (hasFlags)
                            throw new InvalidProgramException($"Cannot have multiple flags (string[]?) in parameters. {info}");
                        if (!parameter.HasDefaultValue)
                            throw new InvalidProgramException($"Flags (string[]) must have defualt value. {info}");
                        hasFlags = true;
                        continue;
                    }

                    if (parameter.ParameterType == typeof(CLIInterpreter))
                    {
                        if (hasCaller)
                            throw new InvalidProgramException($"Cannot have multiple callers (CLIApplication?) in parameters. {info}");
                        if (!parameter.HasDefaultValue)
                            throw new InvalidProgramException($"Caller (CLIApplication) must have defualt value. {info}");
                        hasCaller = true;
                        continue;
                    }
                }

                Delegate = @delegate;
                Info = info;
            }

            public readonly object? Invoke(string[] entries, string[] flags, CLIInterpreter? caller = null)
            {
                ParameterInfo[] parameters = Info.GetParameters();
                object?[] arguments = new object?[parameters.Length];
                foreach (ParameterInfo parameter in parameters)
                {
                    if (parameter.ParameterType == typeof(string[]))
                    {
                        arguments[parameter.Position] = flags;
                        continue;
                    }
                    else if (parameter.ParameterType == typeof(CLIInterpreter))
                    {
                        arguments[parameter.Position] = caller;
                        continue;
                    }

                    string evaluating;

                    if (parameter.HasDefaultValue)
                    {
                        // Specify StringComparison for correctness. parameter.Name is checked non-null in constructor.
                        // Dereference of a possibly null reference. parameter.Name is checked non-null in constructor.
#pragma warning disable CA1310
                        if (Array.Find(entries, entry => entry.StartsWith($"{parameter.Name}=")) is string namedEntry)
#pragma warning restore CA1310
#pragma warning disable CS8602
                            evaluating = namedEntry[(parameter.Name.Length + 1)..];
#pragma warning restore CS8602 
                        else if (parameter.Position < entries.Length)
                            evaluating = entries[parameter.Position];
                        else
                            continue;
                    }
                    else
                    {
                        evaluating = entries[parameter.Position];
                    }

                    if (parameter.ParameterType == typeof(string))
                    {
                        arguments[parameter.Position] = evaluating;
                    }
                    else if (parameter.ParameterType == typeof(bool))
                    {
                        if (bool.TryParse(evaluating, out bool boolArgument))
                            arguments[parameter.Position] = boolArgument;
                        else
                            throw new ArgumentException($"Could not parse {evaluating} as {parameter.ParameterType}");
                    }
                    else if (parameter.ParameterType == typeof(decimal))
                    {
                        if (decimal.TryParse(evaluating, out decimal decimalArgument))
                            arguments[parameter.Position] = decimalArgument;
                        else
                            throw new ArgumentException($"Could not parse {evaluating} as {parameter.ParameterType}");
                    }
                    else
                    {
                        if (!Enum.TryParse(parameter.ParameterType, evaluating, out arguments[parameter.Position]))
                            throw new ArgumentException($"Could not parse {evaluating} as {parameter.ParameterType}");
                    }
                }

                return Delegate.DynamicInvoke(arguments);
            }

            public string ToString(string? format = null, IFormatProvider? formatProvider = null) => this.GetCommandName();
        }

        public string InterfaceName { get; set; } = "CLIApp";

        public string EntryMarker { get; set; } = ">";

        public string FlagMarker { get; set; } = "--";

        public TextWriter Out { get; set; } = Console.Out;

        public TextReader In { get; set; } = Console.In;

        public TextWriter Error { get; set; } = Console.Error;

        public bool IgnoreCase { get; set; }

        private readonly Command[] _commands;

        public event EventHandler<Command>? InvokingCommand;

        public event EventHandler<Command>? CommandInvoked;

        private Command? _executing;

        public Command? Executing { get => _executing; }

        private bool _stopExecution = false;

        public bool StopRunExecution { get => _stopExecution; set => _stopExecution = value; }

        public CLIInterpreter(params Delegate[] delegates)
        {
            _commands = new Command[delegates.Length];
            for (int i = 0; i < delegates.Length; i++)
                _commands[i] = new Command { Delegate = delegates[i], Info = delegates[i].GetMethodInfo() };
        }

        public void Execute(string? lineInput)
        {
            if (string.IsNullOrEmpty(lineInput))
                return;

            List<string> entries = lineInput.DeliminateOutside();

            if (_commands.FindCommand(entries[0], IgnoreCase) is not Command cmd)
            {
                if (entries[0].ToLower(CultureInfo.InvariantCulture) == "help")
                    foreach (var command in _commands)
                        Console.WriteLine(command.GetCommandDescription());
                else
                    throw new InvalidOperationException($"Command not found: {entries[0]}");
                return;
            }

            entries.RemoveAt(0);
            List<string> flags = new();

            for (int i = 0;i < entries.Count; i++)
            {
                if (entries[i].StartsWith(FlagMarker, StringComparison.InvariantCulture))
                {
                    flags.Add(entries[i]);
                    entries.RemoveAt(i);
                }
            }

            int required = cmd.RequiredCommandArguments();
            if (entries.Count < required)
                throw new InvalidOperationException($"Not enough positional arguments, required {required}, given {entries.Count}.");

            _executing = cmd;
            InvokingCommand?.Invoke(this, cmd);
            try
            {
                if (cmd.Invoke(entries.ToArray(), flags.ToArray(), this) is object obj)
                    Error.WriteLine($"On execution of {cmd}: {obj}");
            }
            catch (ArgumentException)
            {
                throw;
            }
            finally
            {
                _executing = null;
            }
            CommandInvoked?.Invoke(this, cmd);
        }

        public void Run(CancellationToken? token = null)
        {
            while (!StopRunExecution)
            {
                if (token.HasValue)
                    if (token.Value.IsCancellationRequested)
                        break;

                Out.Write($"{InterfaceName}{EntryMarker}");
                try
                {
                    Execute(Console.ReadLine());
                }
                catch (InvalidOperationException e)
                {
                    if (!e.WasThrownBy(Execute))
                        throw;
                    Error.WriteLine(e.Message);
                }
                catch (ArgumentException e)
                {
                    if (!e.WasThrownBy(Execute))
                        throw;
                    Error.WriteLine(e.Message);
                }
            }
            StopRunExecution = false;
        }
    }
}