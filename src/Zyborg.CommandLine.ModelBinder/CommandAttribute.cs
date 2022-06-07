using Humanizer;

namespace System.CommandLine.ModelBinder;

[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
public sealed class CommandAttribute : Attribute
{
    public CommandAttribute(string? name = null, string? description = null)
    {
        Name = name;
        Description = description;
    }

    public string? Name { get; set; }

    public string[] Aliases { get; set; } = Array.Empty<string>();

    public string? Description { get; set; }

    public bool IsHidden { get; private set; }
    public bool TreatUnmatchedTokensAsErrors { get; private set; }

    public Command ToCommand(string propName) => ConfigureCommand(
        new Command(Name ?? propName.Kebaberize()));

    public Command ConfigureCommand(Command command)
    {
        foreach (var alias in Aliases)
        {
            command.AddAlias(alias);
        }

        command.Description = Description;
        command.Handler = null;
        command.IsHidden = IsHidden;
        command.TreatUnmatchedTokensAsErrors = TreatUnmatchedTokensAsErrors;

        //command.SetHandler(null);

        return command;
    }
}
