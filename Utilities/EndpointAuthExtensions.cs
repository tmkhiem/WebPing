namespace WebPing.Utilities;

/// <summary>
/// Extension methods for marking endpoints as requiring authentication
/// </summary>
public static class EndpointAuthExtensions
{
    /// <summary>
    /// Marks an endpoint as requiring authentication.
    /// Routes marked with this will have authentication enforced by the middleware.
    /// </summary>
    public static RouteHandlerBuilder RequireAuth(this RouteHandlerBuilder builder)
    {
        return builder.WithMetadata(new RequireAuthAttribute());
    }
}

/// <summary>
/// Attribute to mark endpoints as requiring authentication
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequireAuthAttribute : Attribute
{
}
