using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace PK2Encryptor;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => ShowFatalStartupError(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if(e.ExceptionObject is Exception ex)
            {
                ShowFatalStartupError(ex);
            }
        };

        try
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm(ProjectProfiles.SilkroadOriginal));
        }
        catch(Exception ex)
        {
            ShowFatalStartupError(ex);
        }
    }
    private static void ShowFatalStartupError(Exception ex)
    {
        try
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PK2Tools");
            Directory.CreateDirectory(folder);
            var logFile = Path.Combine(folder, "startup-error.log");
            File.WriteAllText(logFile, ex.ToString());
            MessageBox.Show($"PK2 Tools could not start.\r\n\r\n{ex.Message}\r\n\r\nA log was written to:\r\n{logFile}", "PK2 Tools", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch
        {
            MessageBox.Show(ex.ToString(), "PK2 Tools", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
