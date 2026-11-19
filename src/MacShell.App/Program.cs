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
        // Ensure single instance
        using var mutex = new Mutex(true, "Global\\MacShell_SingleInstance", out bool createdNew);

        if (!createdNew)
        {
            // Another instance is running - try to activate it
            ActivateExistingInstance();
            return;
        }

        try
        {
            // Initialize WinUI 3
            Application.Start(_ => new App());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fatal error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static void ActivateExistingInstance()
    {
        // Try to find and activate the existing window
        // This is a simplified version - in production you'd use FindWindow
        System.Diagnostics.Debug.WriteLine("MacShell is already running");
    }
}