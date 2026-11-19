using System;
using System.IO;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace MacShell.UI.Helpers;

/// <summary>
/// Helper class to load XAML at runtime instead of compile-time compilation.
/// This bypasses the XAML compiler (WMC9999 error) by parsing XAML files at runtime.
/// </summary>
public static class RuntimeXamlLoader
{
    /// <summary>
    /// Loads a XAML file from the application's output directory and returns the root element.
    /// </summary>
    /// <typeparam name="T">The expected type of the root element.</typeparam>
    /// <param name="relativePath">Relative path to the XAML file from the assembly location.</param>
    /// <returns>The loaded XAML element.</returns>
    public static T Load<T>(string relativePath) where T : class
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyLocation = assembly.Location;
        var assemblyDir = Path.GetDirectoryName(assemblyLocation) ?? "";
        var fullPath = Path.Combine(assemblyDir, relativePath);

        if (!File.Exists(fullPath))
        {
            // Try alternative locations (for widget assemblies)
            var codeBase = assembly.Location;
            var codeBaseDir = Path.GetDirectoryName(codeBase) ?? "";
            fullPath = Path.Combine(codeBaseDir, relativePath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"XAML file not found: {relativePath}. Tried: {assemblyDir} and {codeBaseDir}");
            }
        }

        var xamlContent = File.ReadAllText(fullPath);

        // Parse the XAML
        var element = (T)XamlReader.Load(xamlContent);
        return element;
    }

    /// <summary>
    /// Loads a ResourceDictionary from a XAML file at runtime.
    /// </summary>
    /// <param name="relativePath">Relative path to the XAML file from the assembly location.</param>
    /// <returns>The loaded ResourceDictionary.</returns>
    public static ResourceDictionary LoadResourceDictionary(string relativePath)
    {
        return Load<ResourceDictionary>(relativePath);
    }

    /// <summary>
    /// Loads a Window from a XAML file at runtime.
    /// </summary>
    /// <param name="relativePath">Relative path to the XAML file from the assembly location.</param>
    /// <returns>The loaded Window.</returns>
    public static Window LoadWindow(string relativePath)
    {
        return Load<Window>(relativePath);
    }

    /// <summary>
    /// Loads a UserControl from a XAML file at runtime.
    /// </summary>
    /// <param name="relativePath">Relative path to the XAML file from the assembly location.</param>
    /// <returns>The loaded UserControl.</returns>
    public static UserControl LoadUserControl(string relativePath)
    {
        return Load<UserControl>(relativePath);
    }

    /// <summary>
    /// Loads a Page from a XAML file at runtime.
    /// </summary>
    /// <param name="relativePath">Relative path to the XAML file from the assembly location.</param>
    /// <returns>The loaded Page.</returns>
    public static Page LoadPage(string relativePath)
    {
        return Load<Page>(relativePath);
    }
}