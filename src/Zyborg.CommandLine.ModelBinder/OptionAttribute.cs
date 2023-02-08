using System.Reflection;
using Humanizer;

namespace System.CommandLine.ModelBinder;

[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
public sealed class OptionAttribute : Attribute
{
    public OptionAttribute(string? name = null, string? description = null)
    {
        Name = name;
        Description = description;
    }

    public bool Global { get; set; }

    public string? Name { get; set; }

    public string[] Aliases { get; set; } = Array.Empty<string>();

    public string? Description { get; set; }

    public string? ArgumentHelpName { get; set; }

    public bool AllowMultipleArguemtnsPerToken { get; set; }

    public int MinValues { get; set; }
    public int MaxValues { get; set; }
    public Arity MinMaxValues { get; set; }

    public bool IsHidden { get; set; }
    public bool IsRequired { get; set; }
    public bool LegalFileNamesOnly { get; set; }
    public bool LegalFIlePathsOnly { get; set; }
    public bool ExistingOnly { get; set; }

    public Option ToOption(Type propType, string propName,
        MethodInfo? parserMeth,
        MethodInfo? defaultMeth)
    {
        var name = Name;
        if (name == null)
        {
            name = propName;
            if (!AllowMultipleArguemtnsPerToken)
                name = name.Singularize();
            name = $"--{name.Kebaberize()}";
        }

        var optType = typeof(Option<>).MakeGenericType(propType);
        object?[] optConsParams;

        if (parserMeth != null && parserMeth.ReturnType == propType)
        {
            var parserType = typeof(Parsing.ParseArgument<>).MakeGenericType(propType);
            var parser = Delegate.CreateDelegate(parserType, parserMeth);
            optConsParams = new object?[]
            {
                name,
                parser,
                false,  // isDefault, TODO: do we need a way to set this?
                null,   // we'll set description below
            };
        }
        else
        {
            optConsParams = new object?[]
            {
                name,
                null,   // we'll set description below
            };
        }

        var option = (Option)Activator.CreateInstance(optType, optConsParams)!;

        foreach (var alias in Aliases)
        {
            option.AddAlias(alias);
        }

        option.Description = Description;
        option.ArgumentHelpName = ArgumentHelpName;
        option.AllowMultipleArgumentsPerToken = AllowMultipleArguemtnsPerToken;
        option.IsHidden = IsHidden;
        option.IsRequired = IsRequired;

        if (LegalFileNamesOnly)
        {
            option.LegalFileNamesOnly();
        }
        else if (LegalFIlePathsOnly)
        {
            option.LegalFilePathsOnly();
        }
        else if (ExistingOnly)
        {
            if (option is Option<FileInfo> ofi) ofi.ExistingOnly();
            else if (option is Option<DirectoryInfo> odi) odi.ExistingOnly();
            else if (option is Option<FileSystemInfo> ofsi) ofsi.ExistingOnly();
            else
                throw new NotImplementedException("TODO: need to implement generic ext method call");
        }

        if (MinValues > -1 || MaxValues > -1)
        {
            MinValues = MinValues > -1 ? MinValues : 0;
            MaxValues = MaxValues > -1 ? MaxValues : int.MaxValue;

            option.Arity = new ArgumentArity(MinValues, MaxValues);
        }
        else
        {
            option.Arity = MinMaxValues switch
            {
                Arity.Zero => ArgumentArity.Zero,
                Arity.ZeroOrOne => ArgumentArity.ZeroOrOne,
                Arity.ZeroOrMore => ArgumentArity.ZeroOrMore,
                Arity.ExactlyOne => ArgumentArity.ExactlyOne,
                Arity.OneOrMore => ArgumentArity.OneOrMore,
                _ => ArgumentArity.ExactlyOne,
            };
        }

        if (defaultMeth != null)
        {
            option.SetDefaultValueFactory(() => defaultMeth.Invoke(null, null));
        }

        return option;
    }
}
