using System;
using System.IO;

namespace PK2Encryptor;

public enum ToolEngineKind
{
    SilkroadOriginal
}

public sealed class ProjectProfile
{
    public ProjectProfile(ToolEngineKind kind, string code, string displayName, string runtimeFolder, string description)
    {
        Kind = kind;
        Code = code;
        DisplayName = displayName;
        RuntimeFolder = runtimeFolder;
        Description = description;
    }

    public ToolEngineKind Kind { get; }
    public string Code { get; }
    public string DisplayName { get; }
    public string RuntimeFolder { get; }
    public string Description { get; }

    public string RuntimeDirectory => Path.Combine(AppContext.BaseDirectory, RuntimeFolder);
    public string GfxFileManagerPath => Path.Combine(RuntimeDirectory, "GFXFileManager.dll");

    public override string ToString() => DisplayName;
}

internal static class ProjectProfiles
{
    public static readonly ProjectProfile SilkroadOriginal = new(
        ToolEngineKind.SilkroadOriginal,
        "silkroad-original",
        "Silkroad-Orginal",
        "Silkroad-Orginal",
        "Default Silkroad PK2 workflow using one original GFXFileManager engine.");

    public static readonly ProjectProfile[] All = { SilkroadOriginal };

    public static ProjectProfile FromCode(string? code) => SilkroadOriginal;
}
