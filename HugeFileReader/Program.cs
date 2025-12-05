//  <@$&< copyright begin >&$@> 24fe144c2255e2f7ccb65514965434a807ae8998c9c4d01902a628f980431c98:20241017.A:2025:12:5:9:40
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// Copyright © 2024-2025 Stewart A. Nutter - All Rights Reserved.
// No warranty is implied or given.
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// <@$&< copyright end >&$@>

using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading;
using System.Windows.Forms;

namespace HugeFileReader;

[SupportedOSPlatform("windows6.1")]
static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main (string[] args)
    {
        // Handle UI thread exceptions
        Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);

        // Handle non-UI thread exceptions
        AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

        // Set unhandled exception mode
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        Application.EnableVisualStyles ();
        Application.SetCompatibleTextRenderingDefault (false);

        Application.Run (new HFViewer (args.Length > 0 ? args[0] : null));
    }

    private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
    {
        if (Debugger.IsAttached) Debugger.Break();
        MessageBox.Show($"An unhandled UI thread exception occurred:\n{e.Exception.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            if (Debugger.IsAttached) Debugger.Break();
            MessageBox.Show($"An unhandled non-UI thread exception occurred:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        else
        {
            if (Debugger.IsAttached) Debugger.Break();
            MessageBox.Show("An unhandled non-UI thread exception occurred.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

}
