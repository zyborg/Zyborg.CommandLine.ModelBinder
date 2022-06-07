namespace System.CommandLine.ModelBinder;

internal static class DbgLog
{
    //private static int _indent = 0;

    public static void Indent()
    {
        //_indent++;
    }
    public static void Outdent()
    {
        //_indent--;
    }

    public static void Log(string fmt, params object[] args)
    {
        //Console.Error.WriteLine($"{new string(' ', _indent * 2)}{string.Format(fmt, args)}");
    }

    public static void LogIndent(string fmt, params object[] args)
    {
        //Log(fmt, args);
        //Indent();
    }
}
