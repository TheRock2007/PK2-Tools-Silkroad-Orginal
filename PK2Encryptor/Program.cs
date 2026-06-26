using System;
using System.IO;
using System.Text.Json;
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
            Application.Run(new MainForm());
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
            var languageCode = ReadSavedLanguageCode();
            var message = string.Equals(languageCode, "ar", StringComparison.OrdinalIgnoreCase)
                ? $"تعذر تشغيل PK2 Tools.\r\n\r\n{ex.Message}\r\n\r\nتم حفظ سجل الخطأ في:\r\n{logFile}"
                : $"PK2 Tools could not start.\r\n\r\n{ex.Message}\r\n\r\nA log was written to:\r\n{logFile}";
            MessageBox.Show(message, "PK2 Tools", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch
        {
            MessageBox.Show(ex.ToString(), "PK2 Tools", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string ReadSavedLanguageCode()
    {
        try
        {
            var settingsPath = Path.Combine(AppContext.BaseDirectory, "Setting.json");
            if(!File.Exists(settingsPath))
            {
                return "en";
            }

            using var document = JsonDocument.Parse(File.ReadAllText(settingsPath));
            return document.RootElement.TryGetProperty("LanguageCode", out var language)
                ? language.GetString() ?? "en"
                : "en";
        }
        catch
        {
            return "en";
        }
    }
}
