// See https://aka.ms/new-console-template for more information
using System.CommandLine;
using System.CommandLine.ModelBinder;
using System.CommandLine.Parsing;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

// Sample:
// dotnet tool update dotnet-suggest --verbosity quiet --global


var sp = new ServiceCollection()
    .AddSingleton(new Dictionary<string, string>
    {
        ["var1"] = "Variable #1",
        ["var2"] = "Second Variable",
        ["var3"] = "Variable III",
    }).BuildServiceProvider();

var root = RootCommand<Model>.Build(new()
{
    //FailOnMissingInvoke = true,
    FailOnUnresolvedServices = true,
    Services = sp,
});
var exit = root.Invoke(args);

if (exit != 0)
    return exit;

Console.WriteLine(JsonSerializer.Serialize(root.Model, Model.JSOpts));

return exit;



public class Model
{
    public static readonly JsonSerializerOptions JSOpts = new JsonSerializerOptions()
    {
        WriteIndented = true,
    };

    [Option(Global = true)]
    public bool Global { get; set; }

    [Argument]
    public string? Name { get; set; }
    public static string? Name_Default() =>
        "DefaultFOO";
    public static string? Name_Parser(ArgumentResult ar) =>
        $"[{string.Join(":", ar.Tokens)}]";

    [Command]
    public ToolCommand? Tool { get; set; }

    [Command(Aliases = new string[] { "nil", "null" })]
    public NoopCommand? Noop { get; set; }

    public void Invoke(System.CommandLine.Invocation.InvocationContext context, IConsole cons)
    {
        cons.WriteLine("Invoking Root Command!");
        //context.ExitCode = 99;
    }

    public class ToolCommand
    {
        [Command]
        public UpdateCommand? Update { get; set; }

        [Option(AllowMultipleArguemtnsPerToken = true)]
        public IEnumerable<string>? Names { get; set; }

        [Option]
        public IEnumerable<string>? Places { get; set; }

        public void Invoke(Dictionary<string, string> vars)
        {
            Console.WriteLine("Invoking Tool Subcommand!");
            Console.WriteLine("Got Vars:");
            Console.WriteLine(JsonSerializer.Serialize(vars, JSOpts));
        }

        public class UpdateCommand
        {
            [Option]
            public string? Verbosity { get; set; }

            public async Task Invoke(Model root, ToolCommand tool)
            {
                Console.WriteLine("Invoking Update Subcommand!");
                await Task.Delay(1000);
                Console.WriteLine("  * root.Global: " + JsonSerializer.Serialize(root.Global, JSOpts));
                await Task.Delay(1000);
                Console.WriteLine("  * tool.Names: " + JsonSerializer.Serialize(tool.Names, JSOpts));
                Console.WriteLine("  * tool.Places: " + JsonSerializer.Serialize(tool.Places, JSOpts));
            }
        }
    }

    public class NoopCommand
    {

    }
}
