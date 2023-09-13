public class CommandHandlerArgs : IEquatable<CommandHandlerArgs>
{
    public CLIApplication? Application { get; init; } = null;
    public string[]? Arguments { get; init; } = null;
    public string[]? Flags { get; init; } = null;
    public CommandHandler? Executing { get; init; } = null;
    public string? Input { get; init; } = null;

    public bool Equals(CommandHandlerArgs? Other)
    {
        if (Other == null)
            return false;
        return Application == Other.Application && Arguments == Other.Arguments && Flags == Other.Flags && Input == Other.Input;
    }
}