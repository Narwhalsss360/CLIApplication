using System.Globalization;
using System.Reflection;

namespace CLIApplication
{
    public class CLIInterpreter
    {
        public readonly struct Command : IFormattable
        {
            public Delegate Delegate { get; init; }

            public MethodInfo Info { get; init; }

            public Command(Delegate @delegate, MethodInfo info)
            {
                bool hasFlags = false;
                bool hasCaller = false;
                foreach (var parameter in info.GetParameters())
                {
                    if (ParameterInfoExtensions.IsSupported(parameter.ParameterType))
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

            public Command(Delegate @delegate)
                : this(@delegate, @delegate.GetMethodInfo())
            {
            }

            public readonly object? Invoke(string[] entries, string[] flags, CLIInterpreter? caller = null)
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
                _commands[i] = new Command(delegates[i]);
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

        public void Stop() => StopRunExecution = false;
    }
}