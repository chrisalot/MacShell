// Copyright (c) MacShell. All rights reserved.

using System;
using Microsoft.UI.Xaml;
using MacShell.App;

namespace MacShell.App;

/// <summary>
/// Program entry point.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Main entry point.
    /// </summary>
    [STAThread]
    private static void Main(string[] args)
    {
        try
        {
            // Initialize WinUI 3 - single instance check is handled in App.OnLaunched
            Application.Start(_ => new App());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fatal error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}