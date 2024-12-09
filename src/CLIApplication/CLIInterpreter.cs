using System.Globalization;
using System.Reflection;

namespace CLIApplication
{
    public class CLIInterpreter
    {
        public class Command : IFormattable
        {
            public Delegate Delegate { get; init; }

            public MethodInfo Info { get; init; }

            public Command(Delegate @delegate, MethodInfo info)
            {
                bool hasFlags = false;
                bool hasCaller = false;
                bool hasParamsModifier = false;
                foreach (var parameter in info.GetParameters())
                {
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

                    if (parameter.ParameterType == typeof(object[]))
                    {
                        if (hasParamsModifier)
                            throw new InvalidProgramException($"Cannoy have multiple params (params object[]) in paramter. {info}");
                        hasParamsModifier = true;
                        continue;
                    }

                    if (ParameterInfoExtensions.IsSupported(parameter.ParameterType))
                        continue;
                }

                Delegate = @delegate;
                Info = info;
            }

            public Command(Delegate @delegate)
                : this(@delegate, @delegate.GetMethodInfo())
            {
            }

            public Command(MethodInfo methodInfo)
                : this(methodInfo.CreateDelegate(), methodInfo)
            {
            }

            public object? Invoke(string[] entries, string[] flags, CLIInterpreter? caller = null)
            {
                ParameterInfo[] parameters = Info.GetParameters();
                Dictionary<int, object?> flagsAndArguments = new();
                foreach (var parameter in parameters)
                {
                    if (parameter.ParameterType == typeof(string[]))
                        flagsAndArguments.Add(parameter.Position, flags);
                    else if (parameter.ParameterType == typeof(CLIInterpreter))
                        flagsAndArguments.Add(parameter.Position, caller);
                }
                return Delegate.DynamicInvoke(parameters.ParseArguments(entries, flagsAndArguments));
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

#pragma warning disable CA1051 // Do not declare visible instance fields
        public List<Command> Commands = new();
#pragma warning restore CA1051 // Do not declare visible instance fields

        public event EventHandler<Command>? InvokingCommand;

        public event EventHandler<Command>? CommandInvoked;

        private Command? _executing;

        public Command? Executing { get => _executing; }

        private bool _stopExecution;

        public bool StopRunExecution { get => _stopExecution; set => _stopExecution = value; }

        public CLIInterpreter(params Delegate[] delegates)
        {
            foreach (Delegate @delegate in delegates)
                Commands.Add(new(@delegate));
        }

        public void Execute(string? lineInput)
        {
            if (string.IsNullOrEmpty(lineInput))
                return;

            List<string> entries = lineInput.DeliminateOutside();

            if (Commands.FindCommand(entries[0], IgnoreCase) is not Command cmd)
            {
                if (entries[0].ToLower(CultureInfo.InvariantCulture) == "help")
                    ShowHelp(entries.Count > 1 ? entries[1] : null);
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
                    i--;
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
            finally
            {
                _executing = null;
            }
            CommandInvoked?.Invoke(this, cmd);
        }

        public void Run(CancellationToken? token = null)
        {
            if (token.HasValue)
                token.Value.ThrowIfCancellationRequested();

            while (!StopRunExecution)
            {
                Out.Write($"{InterfaceName}{EntryMarker}");

                Task<string?> task = In.ForceReadLineAsync(token ?? default);
                while (!task.IsCompleted)
                {
                    if (task.IsCanceled ||
                        task.IsFaulted ||
                        StopRunExecution)
                        goto cancelled;
                }

                try
                {
                    Execute(task.Result);
                    if (token.HasValue)
                        if (token.Value.IsCancellationRequested)
                            goto cancelled;
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

                continue;
            cancelled:
                break;
            }
            StopRunExecution = false;
        }

        public void ShowHelp(string? commandName = null)
        {
            if (commandName is null)
            {
                foreach (var command in Commands)
                    Out.WriteLine(command.GetCommandDescription());
                return;
            }

            if (Commands.FindCommand(commandName, IgnoreCase) is not Command cmd)
            {
                Error.WriteLine($"Command {commandName} not found! Showing help.");
                ShowHelp();
                return;
            }

            Out.WriteLine(cmd.GetFullDescription());
        }

        public void Stop() => StopRunExecution = true;
    }
}