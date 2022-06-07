using System.Reflection;
using Humanizer;

namespace System.CommandLine.ModelBinder;

[AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
public sealed class ArgumentAttribute : Attribute
{
    public ArgumentAttribute()
    {
    }

    public bool NoName { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? HelpName { get; set; }

    public int? MinValues { get; set; }
    public int? MaxValues { get; set; }
    public Arity? MinMaxValues { get; set; }

    public bool IsHidden { get; private set; }
    public bool IsRequired { get; private set; }
    public bool LegalFileNamesOnly { get; private set; }
    public bool LegalFIlePathsOnly { get; private set; }
    public bool ExistingOnly { get; private set; }

    public Argument ToArgument(Type propType, string propName,
        MethodInfo? parserMeth,
        MethodInfo? defaultMeth)
    {
        var argType = typeof(Argument<>).MakeGenericType(propType);
        var argConsParams = Array.Empty<object>();

        if (parserMeth != null && parserMeth.ReturnType == propType)
        {
            var parserType = typeof(Parsing.ParseArgument<>).MakeGenericType(propType);
            var parser = Delegate.CreateDelegate(parserType, parserMeth);
            argConsParams = new object[]
            {
                parser,
                false, // isDefault, TODO: do we need a way to set this?
            };
        }

        var argument = (Argument)Activator.CreateInstance(argType, argConsParams)!;

        if (!NoName)
        {
            argument.Name = Name ?? propName.Kebaberize();
        }

        argument.Description = Description;
        argument.HelpName = HelpName;
        argument.IsHidden = IsHidden;

        if (LegalFileNamesOnly)
            argument.LegalFileNamesOnly();
        else if (LegalFIlePathsOnly)
            argument.LegalFilePathsOnly();
        else if (ExistingOnly)
        {
            if (argument is Argument<FileInfo> afi) afi.ExistingOnly();
            else if (argument is Argument<DirectoryInfo> adi) adi.ExistingOnly();
            else if (argument is Argument<FileSystemInfo> afsi) afsi.ExistingOnly();
            else
                throw new NotImplementedException("TODO: need to implement generic ext method call");
        }

        if (MinValues != null || MaxValues != null)
        {
            argument.Arity = new ArgumentArity(MinValues ?? 0, MaxValues ?? int.MaxValue);
        }
        else if (MinMaxValues != null)
        {
            argument.Arity = MinMaxValues.Value switch
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
            argument.SetDefaultValueFactory(() => defaultMeth.Invoke(null, null));
        }

        return argument;
    }
}
