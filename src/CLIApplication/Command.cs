using System.Reflection;

public delegate void CommandHandler(Dictionary<object, object?> Arguments, string[]? Flags, object? Caller);

public class ArgumentDefinition
{
    public static readonly char[] InvalidNameChars = new char[] { ' ', '/', '\\', '(', ')' };
    public readonly Type Type;
    public readonly bool Required;
    public readonly string Keyword;

    public ArgumentDefinition(Type Type, bool Required, string Keyword)
    {
        this.Type = Type;
        this.Required = Required;
        this.Keyword = Keyword;
    }
}

public static class ArgumentDefinitionExtensions
{
    public static int RequiredArguments(this ArgumentDefinition[] ArgumentDefinitions)
    {
        int Count = 0;
        foreach (var Definition in ArgumentDefinitions)
            if (Definition.Required)
                Count++;
        return Count;
    }
}

public class Command
{
    public readonly string Name;
    public readonly CommandHandler Handler;
    public readonly string Description;
    public readonly ArgumentDefinition[]? ArgumentDefinitions;

    public Command(string Name, CommandHandler Handler, string Description = "", ArgumentDefinition[]? ArgumentDefinitions = null)
    {
        this.Name = Name;
        this.Handler = Handler;
        this.Description = Description;
        this.ArgumentDefinitions = ArgumentDefinitions;
    }
}