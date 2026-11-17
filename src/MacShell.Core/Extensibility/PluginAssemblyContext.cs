using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace MacShell.Core.Extensibility;

/// <summary>
/// Custom AssemblyLoadContext for isolating widget assemblies.
/// Allows unloading and prevents conflicts between widgets.
/// </summary>
public sealed class PluginAssemblyContext : AssemblyLoadContext
{
    private readonly string _pluginId;
    private readonly string _pluginPath;
    private readonly AssemblyDependencyResolver _resolver;
    private readonly HashSet<string> _loadedAssemblies = new(StringComparer.OrdinalIgnoreCase);

    public PluginAssemblyContext(string pluginId, string pluginPath, string pluginFolder)
        : base(name: pluginId, isCollectible: true)
    {
        _pluginId = pluginId;
        _pluginPath = pluginPath;
        _resolver = new AssemblyDependencyResolver(pluginFolder);
    }

    /// <summary>
    /// Resolves assembly dependencies from the plugin folder.
    /// </summary>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }
        return null;
    }

    /// <summary>
    /// Resolves unmanaged DLL dependencies.
    /// </summary>
    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }
        return base.LoadUnmanagedDll(unmanagedDllName);
    }

    /// <summary>
    /// Loads the main plugin assembly.
    /// </summary>
    public Assembly LoadPluginAssembly()
    {
        return LoadFromAssemblyPath(_pluginPath);
    }

    /// <summary>
    /// Gets all assemblies loaded in this context.
    /// </summary>
    public IReadOnlyCollection<Assembly> GetLoadedAssemblies()
    {
        return Assemblies.ToList();
    }
}