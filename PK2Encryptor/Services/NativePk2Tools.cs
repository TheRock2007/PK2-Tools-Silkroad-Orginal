using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PK2Encryptor;

internal sealed class NativePk2Progress
{
    public NativePk2Progress(int percent, string status)
    {
        Percent = Math.Clamp(percent, 0, 1000);
        Status = status;
    }

    public int Percent { get; }
    public string Status { get; }
}

internal static class NativePk2Tools
{
    private const string DllName = "GFXFileManager.dll";
    private const int ErrorCapacity = 8192;

    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    private delegate int NativeProgressCallback(int percent, IntPtr status, IntPtr userdata);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int PK2Tools_ListPk2W(string pk2File, string listFile, string blowfishKey, StringBuilder errorBuffer, int errorChars);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int PK2Tools_BuildPk2W(string sourceFolder, string outputPk2, int encryptEntries, int encryptPayloads, string blowfishKey, NativeProgressCallback callback, IntPtr userdata, StringBuilder errorBuffer, int errorChars);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int PK2Tools_CryptPk2W(string pk2File, int encryptPayloads, string blowfishKey, NativeProgressCallback callback, IntPtr userdata, StringBuilder errorBuffer, int errorChars);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int PK2Tools_ImportFolderW(string pk2File, string sourceFolder, string internalFolder, int encryptPayloads, string blowfishKey, NativeProgressCallback callback, IntPtr userdata, StringBuilder errorBuffer, int errorChars);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int PK2Tools_ExtractPk2W(string pk2File, string outputFolder, string? selectionFile, int extractAll, int rawOutput, string blowfishKey, NativeProgressCallback callback, IntPtr userdata, StringBuilder errorBuffer, int errorChars);

    public static Task<List<Pk2PreviewEntry>> ReadPk2EntriesAsync(string pk2File, string blowfishKey, CancellationToken token)
    {
        return Task.Run(() =>
        {
            token.ThrowIfCancellationRequested();
            var listFile = Path.Combine(Path.GetTempPath(), "pk2tools_list_" + Guid.NewGuid().ToString("N") + ".tsv");
            try
            {
                var error = CreateErrorBuffer();
                var result = PK2Tools_ListPk2W(pk2File, listFile, NormalizeKey(blowfishKey), error, error.Capacity);
                if(result != 0)
                {
                    throw CreateNativeException("list PK2", result, error);
                }
                token.ThrowIfCancellationRequested();
                return ReadListingFile(listFile);
            }
            finally
            {
                TryDelete(listFile);
            }
        }, token);
    }

    public static Task BuildPk2Async(string sourceFolder, string outputPk2, bool encryptEntries, bool encryptPayloads, string blowfishKey, IProgress<NativePk2Progress> progress, CancellationToken token)
    {
        return RunWithProgressAsync("build PK2", progress, token, (callback, error) =>
            PK2Tools_BuildPk2W(sourceFolder, outputPk2, encryptEntries ? 1 : 0, encryptPayloads ? 1 : 0, NormalizeKey(blowfishKey), callback, IntPtr.Zero, error, error.Capacity));
    }

    public static Task CryptPk2Async(string pk2File, bool encryptPayloads, string blowfishKey, IProgress<NativePk2Progress> progress, CancellationToken token)
    {
        return RunWithProgressAsync(encryptPayloads ? "encrypt PK2" : "decrypt PK2", progress, token, (callback, error) =>
            PK2Tools_CryptPk2W(pk2File, encryptPayloads ? 1 : 0, NormalizeKey(blowfishKey), callback, IntPtr.Zero, error, error.Capacity));
    }

    public static Task ImportFolderAsync(string pk2File, string sourceFolder, string internalFolder, bool encryptPayloads, string blowfishKey, IProgress<NativePk2Progress> progress, CancellationToken token)
    {
        return RunWithProgressAsync("import folder", progress, token, (callback, error) =>
            PK2Tools_ImportFolderW(pk2File, sourceFolder, internalFolder, encryptPayloads ? 1 : 0, NormalizeKey(blowfishKey), callback, IntPtr.Zero, error, error.Capacity));
    }

    public static Task ExtractPk2Async(string pk2File, string outputFolder, string? selectionFile, bool extractAll, bool rawOutput, string blowfishKey, IProgress<NativePk2Progress> progress, CancellationToken token)
    {
        return RunWithProgressAsync("extract PK2", progress, token, (callback, error) =>
            PK2Tools_ExtractPk2W(pk2File, outputFolder, selectionFile, extractAll ? 1 : 0, rawOutput ? 1 : 0, NormalizeKey(blowfishKey), callback, IntPtr.Zero, error, error.Capacity));
    }

    private static string NormalizeKey(string? key) => string.IsNullOrWhiteSpace(key) ? "169841" : key.Trim();

    private static Task RunWithProgressAsync(string operationName, IProgress<NativePk2Progress> progress, CancellationToken token, Func<NativeProgressCallback, StringBuilder, int> action)
    {
        return Task.Run(() =>
        {
            token.ThrowIfCancellationRequested();
            var error = CreateErrorBuffer();
            var progressClock = Stopwatch.StartNew();
            var lastReportedPercent = -1;
            var lastReportedStatus = string.Empty;
            var lastReportedMilliseconds = -500L;

            NativeProgressCallback callback = (percent, statusPointer, _) =>
            {
                var status = Marshal.PtrToStringUni(statusPointer) ?? operationName;
                var safePercent = Math.Clamp(percent, 0, 1000);
                var now = progressClock.ElapsedMilliseconds;
                var isBoundary = safePercent <= 0 || safePercent >= 1000;
                var percentMoved = Math.Abs(safePercent - lastReportedPercent) >= 4;
                var enoughTimePassed = now - lastReportedMilliseconds >= 75;
                var statusChanged = !string.Equals(status, lastReportedStatus, StringComparison.Ordinal);
                var stale = now - lastReportedMilliseconds >= 250;

                if(isBoundary || stale || (percentMoved && enoughTimePassed) || (statusChanged && enoughTimePassed))
                {
                    lastReportedPercent = safePercent;
                    lastReportedStatus = status;
                    lastReportedMilliseconds = now;
                    progress.Report(new NativePk2Progress(safePercent, status));
                }

                return token.IsCancellationRequested ? 0 : 1;
            };

            var result = action(callback, error);
            GC.KeepAlive(callback);
            if(token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();
            }
            if(result != 0)
            {
                throw CreateNativeException(operationName, result, error);
            }
        }, token);
    }

    private static StringBuilder CreateErrorBuffer() => new(ErrorCapacity);

    private static InvalidOperationException CreateNativeException(string operationName, int result, StringBuilder error)
    {
        var text = error.ToString().Trim();
        if(string.IsNullOrWhiteSpace(text))
        {
            text = $"Native PK2 engine returned code {result} while trying to {operationName}.";
        }
        return new InvalidOperationException(text);
    }

    private static List<Pk2PreviewEntry> ReadListingFile(string listFile)
    {
        var result = new List<Pk2PreviewEntry>();
        if(!File.Exists(listFile))
        {
            return result;
        }

        foreach(var line in File.ReadLines(listFile, Encoding.UTF8))
        {
            if(string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split('\t');
            if(parts.Length < 2)
            {
                continue;
            }

            long.TryParse(parts[1], out var size);
            var state = parts.Length >= 3 ? parts[2] : "Ready";
            result.Add(new Pk2PreviewEntry(parts[0], Math.Max(0, size), state));
        }

        return result;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if(File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
        }
    }
}
