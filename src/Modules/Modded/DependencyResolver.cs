using ReplantedOnline.Utilities.MelonLoader;
using System.Reflection;

namespace ReplantedOnline.Modules.Modded;

/// <summary>
/// Provides dynamic resolution and loading of embedded assembly dependencies at runtime.
/// </summary>
internal static class DependencyResolver
{
    /// <summary>
    /// Comma separated list of assembly names that should be resolved as embedded resources.
    /// </summary>
    internal const string EMBEDDED_ASSEMBLIES = "DiscordRPC";

    /// <summary>
    /// Initializes the dependency resolver.
    /// </summary>
    internal static void Initialize()
    {
        AppDomain.CurrentDomain.AssemblyResolve += ResolveEmbeddedAssembly;
    }

    /// <summary>
    /// Resolves assembly loading requests by attempting to load embedded DLL resources.
    /// </summary>
    /// <param name="sender">The source of the assembly resolve event.</param>
    /// <param name="args">Contains the assembly name that needs to be resolved.</param>
    /// <returns>
    /// The loaded <see cref="Assembly"/> if the requested assembly matches an embedded resource;
    /// otherwise, returns <c>null</c>.
    /// </returns>
    private static Assembly ResolveEmbeddedAssembly(object sender, ResolveEventArgs args)
    {
        // Only resolve assemblies requested locally
        if (args.RequestingAssembly != ModInfo.Assembly) return null;

        AssemblyName assemblyName = new(args.Name);

        foreach (string dependency in EMBEDDED_ASSEMBLIES.Split(", "))
        {
            if (assemblyName.Name == dependency)
            {
                string resourceName = $"ReplantedOnline.Resources.EmbeddedAssemblies.{dependency}.dll";
                using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);

                if (stream == null)
                {
                    ReplantedOnlineMod.Logger.Error(typeof(DependencyResolver), $"Failed to find embedded resource: {resourceName}");
                    return null;
                }

                try
                {
                    byte[] buffer = new byte[stream.Length];
                    stream.Read(buffer, 0, buffer.Length);
                    return Assembly.Load(buffer);
                }
                catch (Exception ex)
                {
                    ReplantedOnlineMod.Logger.Error(typeof(DependencyResolver), ex.ToString());
                    return null;
                }
            }
        }

        return null;
    }
}