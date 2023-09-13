public static class Utility
{
    public static bool HasKeywordArguments(this string Argument)
    {
        return Argument.Contains('=');
    }

    public static Dictionary<string, string> GetKeywordArguments(this string[] Arguments)
    {
        Dictionary<string, string> KeywordArguments = new();

        foreach (string KeywordArgument in Arguments)
        {
            if (!KeywordArgument.HasKeywordArguments())
                continue;
            int EqualsIdx = KeywordArgument.IndexOf('=');
            KeywordArguments.Add(KeywordArgument.Substring(0, EqualsIdx).Trim(), KeywordArgument.Substring(EqualsIdx + 1, KeywordArgument.Length - EqualsIdx - 1));
        }
        return KeywordArguments;
    }
}

public class CLIApplication : IEquatable<CLIApplication>
{
    public const string CommandDictionaryKey = "__Command__";
    public TextWriter Out { get; set; } = Console.Out;
    public TextWriter Error { get; set; } = Console.Error;
    public TextReader In { get; set; } = Console.In;
    public string Name { get; set; } = "CLI App";
    public string EntryMarker { get; set; } = "> ";
    public string FlagDelimiter { get; set; } = "--";
    public CommandHandler? AnyHandler = null;
    public bool Stop = false;
    public List<Command> Commands { get; set; } = new List<Command>();
    public Command? Executing = null;

    public CLIApplication(TextWriter? Out = null, TextWriter? Error = null, TextReader? In = null, string Name = "CLI Application", string EntryMarker = "> ", string FlagDelimiter = "--", CommandHandler? AnyHandler = null, List<Command>? Commands = null, bool Stop = false)
    {
        this.Name = Name;
        this.EntryMarker = EntryMarker;
        this.FlagDelimiter = FlagDelimiter;
        this.AnyHandler = AnyHandler;
        this.Stop = Stop;
        if (Out is not null)
            this.Out = Out;
        if (Error is not null)
            this.Error = Error;
        if (In is not null)
            this.In = In;
        if (Commands is not null)
            this.Commands = Commands;
    }

    public void ShowError(string Message) { Error.WriteLine(Message); }

    public void ShowError<T>(T Exception) where T : Exception { Error.WriteLine($"{typeof(T).Name}: {Exception.Message}"); }

    public void ShowHelp(Command Command, string PreLine = "")
    {
        string HelpLine = $"{PreLine}{Command.Name} ";
        if (Command.ArgumentDefinitions is not null)
            foreach (var Definition in Command.ArgumentDefinitions)
                HelpLine += $"<{Definition.Keyword}{(Definition.Required ? "" : "?")}: {Definition.Type.Name}> ";
        HelpLine += Command.Description;
        Console.WriteLine(HelpLine);
    }

    public void ShowHelp(string CommandName)
    {
        foreach (var Command in Commands)
            if (Command.Name == CommandName)
            {
                ShowHelp(Command);
                return;
            }
        throw new NotImplementedException("Command name not found exception not implemented");
    }

    public void ShowHelp()
    {
        Console.WriteLine($"{Name} Help:");
        foreach (var Command in Commands)
            ShowHelp(Command, "    ");
    }

    public static ArgumentDefinition GetArgumentDefinition(ArgumentDefinition[] ArgumentDefinitions, int ArgumentIndex, out int ArgumentPosition, string? Keyword = null)
    {
        if (Keyword is not null)
            for (int i = 0; i < ArgumentDefinitions.Length; i++)
                if (ArgumentDefinitions[i].Keyword is not null)
                    if (ArgumentDefinitions[i].Keyword == Keyword)
                    {
                        ArgumentPosition = i;
                        return ArgumentDefinitions[i];
                    }
        ArgumentPosition = ArgumentIndex;
        return ArgumentDefinitions[ArgumentIndex];
    }

    public static Tuple<Dictionary<object, object?>, string[]?> ParseInput(string Input, string FlagDelimiter, ArgumentDefinition[]? ArgumentDefinitions)
    {
        Dictionary<object, object?> Arguments = new();
        Input = Input.Trim();

        string CurrentArgument = "";
        bool IsFlag = false;
        bool InString = false;

        List<string> ArgumentInputs = new();
        List<string> FlagInputs = new();

        while (true)
        {
            IsFlag = CurrentArgument.StartsWith(FlagDelimiter);

            if (Input[0] == '"')
            {
                InString = !InString;
                if (Input.Length == 1)
                    goto CompletedArgument;
                goto Removal;
            }

            if (!InString && Input[0] == ' ')
                goto CompletedArgument;

            CurrentArgument += Input[0];
            if (Input.Length > 1)
                goto Removal;
        CompletedArgument:
            if (CurrentArgument == "" || CurrentArgument == " ")
                goto Removal;

            if (IsFlag)
                FlagInputs.Add(CurrentArgument);
            else
                ArgumentInputs.Add(CurrentArgument);

            CurrentArgument = "";
            IsFlag = false;
            if (Input.Length > 1)
                InString = false;
        Removal:
            if (Input.Length > 1)
                Input = Input.Remove(0, 1);
            else
                break;
        }

        if (InString)
            throw new NotImplementedException("Unterminated string exception not implemented");

        if (ArgumentDefinitions is null)
            return new Tuple<Dictionary<object, object?>, string[]?>(Arguments, FlagInputs.ToArray());

        List<int> PositionsDefined = new();

        for (int ArgumentPosition = 0; ArgumentPosition < ArgumentInputs.Count; ArgumentPosition++)
        {
            string ArgumentInput = ArgumentInputs[ArgumentPosition];
            int Position;
            ArgumentDefinition Definition = GetArgumentDefinition(ArgumentDefinitions, ArgumentPosition, out Position, ArgumentInput.Contains('=') ? ArgumentInput.Substring(0, ArgumentInput.IndexOf('=')) : null);
        
            if (PositionsDefined.Contains(Position))
                throw new NotImplementedException("Multiply defined argument exception not implemented");
            PositionsDefined.Add(Position);

            if (ArgumentInput.Contains('='))
                ArgumentInput = ArgumentInput.Substring(ArgumentInput.IndexOf('=') + 1);

            if (Definition.Type == typeof(string))
            {
                Arguments[Position] = ArgumentInput;
            }
            else if (Definition.Type == typeof(int))
            {
                int Parsed;
                if (int.TryParse(ArgumentInput, out Parsed))
                    Arguments[Position] = Parsed;
                else
                    Arguments[Position] = null;
            }
            else if (Definition.Type == typeof(uint))
            {
                uint Parsed;
                if (uint.TryParse(ArgumentInput, out Parsed))
                    Arguments[Position] = Parsed;
                else
                    Arguments[Position] = null;
            }
            else if (Definition.Type == typeof(short))
            {
                short Parsed;
                if (short.TryParse(ArgumentInput, out Parsed))
                    Arguments[Position] = Parsed;
                else
                    Arguments[Position] = null;
            }
            else if (Definition.Type == typeof(ushort))
            {
                ushort Parsed;
                if (ushort.TryParse(ArgumentInput, out Parsed))
                    Arguments[Position] = Parsed;
                else
                    Arguments[Position] = null;
            }
            else if (Definition.Type == typeof(byte))
            {
                byte Parsed;
                if (byte.TryParse(ArgumentInput, out Parsed))
                    Arguments[Position] = Parsed;
                else
                    Arguments[Position] = null;
            }
            else if (Definition.Type == typeof(long))
            {
                long Parsed;
                if (long.TryParse(ArgumentInput, out Parsed))
                    Arguments[Position] = Parsed;
                else
                    Arguments[Position] = null;
            }
            else if (Definition.Type == typeof(ulong))
            {
                ulong Parsed;
                if (ulong.TryParse(ArgumentInput, out Parsed))
                    Arguments[Position] = Parsed;
                else
                    Arguments[Position] = null;
            }
            else if (Definition.Type == typeof(float))
            {
                float Parsed;
                if (float.TryParse(ArgumentInput, out Parsed))
                    Arguments[Position] = Parsed;
                else
                    Arguments[Position] = null;
            }
            else if (Definition.Type == typeof(double))
            {
                double Parsed;
                if (double.TryParse(ArgumentInput, out Parsed))
                    Arguments[Position] = Parsed;
                else
                    Arguments[Position] = null;
            }
            else
            {
                throw new NotImplementedException("Unsupported Type Exception Unimplemented");
            }

            if (Definition.Keyword is not null)
                Arguments[Definition.Keyword] = Arguments[Position];
        }

        for (int ArgumentPosition = 0; ArgumentPosition < ArgumentDefinitions.Length; ArgumentPosition++)
            if (ArgumentDefinitions[ArgumentPosition].Required && !PositionsDefined.Contains(ArgumentPosition))
                throw new NotImplementedException("Undefined required argument exception not implemented");

        return new Tuple<Dictionary<object, object?>, string[]?>(Arguments, FlagInputs.ToArray());
    }

    public void RunCommand(Command Command)
    {
        string? Input = In.ReadLine();
        if (string.IsNullOrEmpty(Input))
            throw new NotImplementedException("Invalid input");

        var UserInputs = ParseInput(Input, FlagDelimiter, Command.ArgumentDefinitions);

        Executing = Command;
        if (AnyHandler is not null)
            AnyHandler(UserInputs.Item1, UserInputs.Item2, this);

        Command.Handler(UserInputs.Item1, UserInputs.Item2, this);
        Executing = null;
    }

    public void RunCommand(string CommandName)
    {
        foreach (var Command in Commands)
            if (Command.Name == CommandName)
            {
                RunCommand(Command);
                return;
            }
        throw new NotImplementedException("Command name not found exception not implemented");
    }

    public void Get()
    {
        Out.Write($"{Name}{EntryMarker}");
        string? Input = In.ReadLine();
        if (string.IsNullOrEmpty(Input))
            throw new NotImplementedException("Invalid input");
        Input = Input.Trim();

        if (Input.StartsWith("help"))
        {
            ShowHelp();
            return;
        }

        if (Input.Contains(' '))
        {
            string CommandName = Input.Substring(0, Input.IndexOf(' '));
            Input = Input.Substring(Input.IndexOf(" ") + 1);

            foreach (var Command in Commands)
                if (Command.Name == CommandName)
                {
                    var UserInputs = ParseInput(Input, FlagDelimiter, Command.ArgumentDefinitions);
                    Executing = Command;
                    if (AnyHandler is not null)
                        AnyHandler(UserInputs.Item1, UserInputs.Item2, this);

                    Command.Handler(UserInputs.Item1, UserInputs.Item2, this);
                    Executing = null;
                    return;
                }
            throw new NotImplementedException("Command name not found exception not implemented");
        }
        else
        {
            foreach (var Command in Commands)
                if (Command.Name == Input)
                {
                    Executing = Command;
                    if (AnyHandler is not null)
                        AnyHandler(new Dictionary<object, object?>(), null, this);

                    Command.Handler(new Dictionary<object, object?>(), null, this);
                    Executing = null;
                    return;
                }
            throw new NotImplementedException("Command name not found exception not implemented");
        }
    }

    public void Run()
    {
        while (!Stop)
            Get();
    }

    public bool Equals(CLIApplication? Other)
    {
        if (Other == null)
            return false;
        return Name.Equals(Other.Name) && In.Equals(Other.In) && Out.Equals(Other.In) && Error.Equals(Other.In) && Commands.Equals(Other.Commands);
    }
}