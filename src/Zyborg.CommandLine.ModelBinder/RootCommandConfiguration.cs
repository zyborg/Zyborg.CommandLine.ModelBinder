namespace System.CommandLine.ModelBinder;

public class RootCommandConfiguration
{
    /// <summary>
    /// If true then an error will result when the resolved CLI
    /// Command class is missing any appropriate Invoke method.
    /// </summary>
    public bool FailOnMissingInvoke { get; set; }

    /// <summary>
    /// If true then an error will result when the parameters
    /// to the resolved Command class invocation method can
    /// not all be resolved from the effective service provider(s).
    /// </summary>
    public bool FailOnUnresolvedServices { get; set; }

    /// <summary>
    /// Optional custom service provider that will be used to
    /// resolve services to the Command class invocation method
    /// parameters.  This will be the primary and the CLI context
    /// will be used as a fallback if not resolved by this one.
    /// </summary>
    public IServiceProvider? Services { get; set; }
}
