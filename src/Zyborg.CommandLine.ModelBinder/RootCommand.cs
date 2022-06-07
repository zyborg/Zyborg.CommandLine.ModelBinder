using System.CommandLine.Invocation;
using System.Reflection;

namespace System.CommandLine.ModelBinder;

public class RootCommand<TModel> : RootCommand where TModel : new()
{
    /// <summary>
    /// A static method named the same as an Argument or Option property
    /// plus this suffix, and taking an argument of <see cref="Parsing.ArgumentResult"/>
    /// and returning the same type as the associated property will be
    /// registered as a custom parser for the associated Arugment or Option.
    /// </summary>
    public const string CustomParserMethodSuffix = "_Parser";

    /// <summary>
    /// A static method named the same as an Argument or Option property
    /// plus this suffix, and returning the same type as the associated
    /// property will be registered as a default value factory for the
    /// associated Argument or Option
    /// </summary>
    public const string DefaultValueMethodSuffix = "_Default";

    private static readonly Type[] ParseMethodParameterTypes =
        new[] { typeof(Parsing.ArgumentResult) };

    public TModel? Model { get; set; }

    public static RootCommand<TModel> Build(RootCommandConfiguration? rcc = null)
    {
        var type = typeof(TModel);
        var root = new RootCommand<TModel>();
        var conf = rcc ?? new();
        type.GetCustomAttribute<CommandAttribute>()?.ConfigureCommand(root);

        Apply(root, type,
            (context, model) =>
            {
                DbgLog.Log("root modelBuilder");

                context.BindingContext.AddService(typeof(RootCommandConfiguration),
                    services => conf);

                root.Model = (TModel)model;
            });

        return root;
    }

    private static void Apply(Command command, Type commandModelType,
        Action<InvocationContext, object>? parentModelBuilder = null,
        Action<InvocationContext, object>? parentModelBinder = null)
    {
        var modelBuilder = AssembleValueBinders(command, commandModelType,
            parentModelBuilder, parentModelBinder);

        var invokeMeth = commandModelType.GetMethod("Invoke");

        var handler = (InvocationContext context) =>
        {
            DbgLog.LogIndent($"handler for {commandModelType.FullName}");

            var model = modelBuilder.Invoke(context);

            parentModelBinder?.Invoke(context, model);

            var cons = (IConsole)context.BindingContext.GetService(typeof(IConsole))!;
            var conf = (RootCommandConfiguration)context.BindingContext.GetService(
                typeof(RootCommandConfiguration))!;

            // Resolve the service resolver, default
            // to just CLI context service provider
            var serviceResolver = (Type t) => context.BindingContext.GetService(t);
            if (conf.Services != null)
            {
                // First check in custom service provider,
                // then fallback to CLI context service provider
                serviceResolver = (Type t) => conf.Services.GetService(t)
                    ?? context.BindingContext.GetService(t);
            }

            if (invokeMeth != null)
            {
                var args = Array.Empty<object?>();
                var methParams = invokeMeth.GetParameters();
                if (methParams?.Length > 0)
                {
                    var argList = new List<object?>();
                    foreach (var mp in methParams)
                    {
                        var argVal = serviceResolver(mp.ParameterType);
                        if (argVal == null && conf.FailOnUnresolvedServices)
                        {
                            throw new NotSupportedException(
                                $"unable to resolve service for invocation parameter of type [{mp.ParameterType}]");
                        }
                        DbgLog.Log($" * Arg Val {mp.ParameterType.FullName} = {argVal}");
                        argList.Add(argVal);
                    }
                    args = argList.ToArray();
                }

                return invokeMeth.Invoke(model, args);
            }
            else if (conf.FailOnMissingInvoke)
            {
                // TODO:  until we can force display of Help, this is a temporary kludge
                throw new MissingMethodException("resolved CLI command is missing invocation method");
            }


            return null;
        };

        if (invokeMeth?.ReturnType is Type invokeMethType
            && typeof(Task).IsAssignableFrom(invokeMethType))
        {
            command.SetHandler(context => (Task)handler.Invoke(context));
        }
        else
        {
            command.SetHandler(context => handler.Invoke(context));
        }
    }

    //root.AddAlias("alias");
    //root.AddArgument(new Argument<int>());
    //root.AddCommand(new Command("name"));
    //root.AddOption(new Option<int>("name"));
    //root.AddValidator(cr => { });
    //root.AddGlobalOption(new Option<int>("name"));

    //Command x;

    private static Func<InvocationContext, object> AssembleValueBinders(
        Command command, Type commandModelType,
        Action<InvocationContext, object>? parentModelBuilder,
        Action<InvocationContext, object>? parentModelBinder)
    {
        var binders = new List<Action<InvocationContext, object>>();

        var modelBuilder = (InvocationContext context) =>
        {
            DbgLog.LogIndent($"modelBuilder for {commandModelType.FullName}");

            object commandModel = Activator.CreateInstance(commandModelType)!;

            foreach (var b in binders)
            {
                b.Invoke(context, commandModel);
            }

            parentModelBuilder?.Invoke(context, commandModel);

            // Add a service resolver to the context for this instance of the
            // command model, to be accessible to child command invocations
            DbgLog.Log($"Adding Service for {commandModelType.FullName} : {commandModel}");
            context.BindingContext.AddService(commandModelType, services => commandModel);

            DbgLog.Outdent();

            return commandModel;
        };

        // Process Arg, Opt and Subcommand properties
        foreach (var prop in commandModelType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var cmdAttr = prop.GetCustomAttribute<CommandAttribute>();
            if (cmdAttr != null)
            {
                var cmd = cmdAttr.ToCommand(prop.Name);
                command.Add(cmd);
                Apply(cmd, prop.PropertyType,
                    (context, childModel) =>
                    {
                        DbgLog.LogIndent($"parent modelBuilder for {commandModelType.FullName} on {prop.Name}");

                        binders.Add((context, model) =>
                        {
                            DbgLog.LogIndent($"parent modelBuilder for {commandModelType.FullName} on {prop.Name} property binder");

                            prop.SetValue(model, childModel);

                            DbgLog.Outdent();
                        });

                        DbgLog.Outdent();
                    },
                    (context, model) =>
                    {
                        DbgLog.LogIndent($"parent modelBinder for {commandModelType.FullName} on {prop.Name}");

                        modelBuilder(context);
                        parentModelBinder?.Invoke(context, model);

                        //var modelType = model.GetType();
                        //DbgLog.Log($"Adding Service for {modelType.FullName} : {model}");

                        //context.BindingContext.AddService(modelType, services => model);

                        DbgLog.Outdent();
                    });
                continue;
            }

            var optAttr = prop.GetCustomAttribute<OptionAttribute>();
            if (optAttr != null)
            {
                // Search for other optional associated methods
                var parserMeth = commandModelType.GetMethod(prop.Name + CustomParserMethodSuffix,
                    BindingFlags.Static | BindingFlags.Public, null, ParseMethodParameterTypes, null);
                var defaultMeth = commandModelType.GetMethod(prop.Name + DefaultValueMethodSuffix,
                    BindingFlags.Static | BindingFlags.Public);

                var opt = optAttr.ToOption(prop.PropertyType, prop.Name,
                    parserMeth, defaultMeth);
                if (optAttr.Global)
                    command.AddGlobalOption(opt);
                else
                    command.Add(opt);
                binders.Add((context, model) => prop.SetValue(model, context.ParseResult.GetValueForOption(opt)));
                continue;
            }

            var argAttr = prop.GetCustomAttribute<ArgumentAttribute>();
            if (argAttr != null)
            {
                // Search for other optional associated methods
                var parserMeth = commandModelType.GetMethod(prop.Name + CustomParserMethodSuffix,
                    BindingFlags.Static | BindingFlags.Public, null, ParseMethodParameterTypes, null);
                var defaultMeth = commandModelType.GetMethod(prop.Name + DefaultValueMethodSuffix,
                    BindingFlags.Static | BindingFlags.Public);

                var arg = argAttr.ToArgument(prop.PropertyType, prop.Name,
                    parserMeth, defaultMeth);
                binders.Add((context, model) => prop.SetValue(model, context.ParseResult.GetValueForArgument(arg)));
                command.Add(arg);
                continue;
            }

            // TODO: any other special attributes we should look for?
        }

        return modelBuilder;
    }
}
