#define NOMINMAX
#include <windows.h>

#include <algorithm>
#include <cstdio>
#include <cctype>
#include <cstring>
#include <filesystem>
#include <fstream>
#include <list>
#include <map>
#include <memory>
#include <sstream>
#include <string>
#include <vector>

#include "pk2/PK2Writer.h"
#include "pk2/PK2Reader.h"
#include "pk2/shared_io.h"
#include "PK2PayloadCrypto.h"

namespace fs = std::filesystem;

extern "C"
{
    typedef int (__stdcall *PK2ToolsProgressCallback)(int percent, const wchar_t * status, void * userdata);
}

namespace
{
    struct NativeProgressContext
    {
        PK2ToolsProgressCallback callback = nullptr;
        void * userdata = nullptr;
        std::wstring operation;
        int progressBase = 0;
        int progressSpan = 1000;
    };

    std::wstring WidenUtf8(const std::string & value)
    {
        if(value.empty())
        {
            return std::wstring();
        }
        int needed = MultiByteToWideChar(CP_UTF8, 0, value.c_str(), -1, nullptr, 0);
        UINT codePage = CP_UTF8;
        if(needed <= 0)
        {
            codePage = CP_ACP;
            needed = MultiByteToWideChar(codePage, 0, value.c_str(), -1, nullptr, 0);
        }
        if(needed <= 0)
        {
            return std::wstring(value.begin(), value.end());
        }
        std::wstring result(static_cast<size_t>(needed), L'\0');
        MultiByteToWideChar(codePage, 0, value.c_str(), -1, result.data(), needed);
        if(!result.empty() && result.back() == L'\0')
        {
            result.pop_back();
        }
        return result;
    }

    std::string NarrowPath(const fs::path & path)
    {
        return path.string();
    }
    std::string NarrowWide(const wchar_t * value)
    {
        if(!value || !value[0])
        {
            return std::string();
        }
        int needed = WideCharToMultiByte(CP_UTF8, 0, value, -1, nullptr, 0, nullptr, nullptr);
        if(needed <= 0)
        {
            return std::string();
        }
        std::string result(static_cast<size_t>(needed), '\0');
        WideCharToMultiByte(CP_UTF8, 0, value, -1, result.data(), needed, nullptr, nullptr);
        if(!result.empty() && result.back() == '\0')
        {
            result.pop_back();
        }
        return result;
    }

    std::string NormalizeBlowfishKey(const wchar_t * blowfishKey)
    {
        std::string key = NarrowWide(blowfishKey);
        while(!key.empty() && isspace(static_cast<unsigned char>(key.front()))) key.erase(key.begin());
        while(!key.empty() && isspace(static_cast<unsigned char>(key.back()))) key.pop_back();
        if(key.empty()) key = "169841";
        if(key.size() > 56) key.resize(56);
        return key;
    }


    void CopyError(const std::string & message, wchar_t * errorBuffer, int errorChars)
    {
        if(!errorBuffer || errorChars <= 0)
        {
            return;
        }
        std::wstring wide = WidenUtf8(message);
        if(wide.empty() && !message.empty())
        {
            wide.assign(message.begin(), message.end());
        }
        const int copyChars = std::min<int>(errorChars - 1, static_cast<int>(wide.size()));
        if(copyChars > 0)
        {
            memcpy(errorBuffer, wide.c_str(), static_cast<size_t>(copyChars) * sizeof(wchar_t));
        }
        errorBuffer[copyChars] = 0;
    }

    bool SendProgress(NativeProgressContext * ctx, int percent, const std::wstring & status)
    {
        if(!ctx || !ctx->callback)
        {
            return true;
        }
        percent = std::max(0, std::min(1000, percent));
        int scaled = ctx->progressBase + static_cast<int>((static_cast<int64_t>(percent) * ctx->progressSpan) / 1000);
        scaled = std::max(0, std::min(1000, scaled));
        return ctx->callback(scaled, status.c_str(), ctx->userdata) != 0;
    }

    bool WriterProgressBridge(const PK2BuildProgress & progress, void * userdata)
    {
        auto * ctx = static_cast<NativeProgressContext*>(userdata);
        int percent = 0;
        if(progress.totalBytes > 0)
        {
            percent = static_cast<int>((progress.currentBytes * 1000ULL) / progress.totalBytes);
        }
        else if(progress.totalFiles > 0)
        {
            percent = static_cast<int>((static_cast<uint64_t>(progress.currentFile) * 1000ULL) / progress.totalFiles);
        }

        std::ostringstream ss;
        if(ctx && !ctx->operation.empty())
        {
            std::wstring widePrefix = ctx->operation;
            std::string prefix(widePrefix.begin(), widePrefix.end());
            ss << prefix;
        }
        else
        {
            ss << "Processing";
        }
        if(progress.totalFiles > 0)
        {
            ss << " - file " << progress.currentFile << "/" << progress.totalFiles;
        }
        if(progress.currentPath && progress.currentPath[0])
        {
            ss << ": " << progress.currentPath;
        }
        return SendProgress(ctx, percent, WidenUtf8(ss.str()));
    }

    std::string ToLowerAscii(std::string value)
    {
        std::transform(value.begin(), value.end(), value.begin(), [](unsigned char ch) { return static_cast<char>(tolower(ch)); });
        return value;
    }

    std::string NormalizeRelativePath(std::string value)
    {
        std::replace(value.begin(), value.end(), '/', '\\');
        while(!value.empty() && (value.front() == '\\' || value.front() == '/'))
        {
            value.erase(value.begin());
        }
        return value;
    }

    bool EnsureDirectory(const fs::path & path, std::string & error)
    {
        if(path.empty())
        {
            return true;
        }
        std::error_code ec;
        fs::create_directories(path, ec);
        if(ec)
        {
            error = "Could not create folder: " + NarrowPath(path);
            return false;
        }
        return true;
    }

    bool BuildOutputPath(const fs::path & outputFolder, const std::string & relativePath, fs::path & outputPath, std::string & error)
    {
        const std::string normalized = NormalizeRelativePath(relativePath);
        if(normalized.empty())
        {
            error = "Empty PK2 output path.";
            return false;
        }
        fs::path relative = fs::path(normalized);
        if(relative.is_absolute())
        {
            error = "Unsafe absolute PK2 path: " + normalized;
            return false;
        }
        for(const auto & part : relative)
        {
            if(part == "." || part == "..")
            {
                error = "Unsafe PK2 path component: " + normalized;
                return false;
            }
        }
        outputPath = outputFolder / relative;
        return true;
    }

    struct EntryItem
    {
        std::string relativePath;
        PK2Entry entry{};
    };

    bool HasMagic(const std::vector<uint8_t> & buffer, const char * magic, size_t magicLength)
    {
        return buffer.size() >= magicLength && memcmp(buffer.data(), magic, magicLength) == 0;
    }

    bool EndsWith(const std::string & value, const char * suffix)
    {
        const size_t suffixLength = strlen(suffix);
        return value.size() >= suffixLength && value.compare(value.size() - suffixLength, suffixLength, suffix) == 0;
    }

    bool LooksLikePlainPayload(const std::string & path, const std::vector<uint8_t> & buffer)
    {
        if(buffer.empty())
        {
            return true;
        }
        if(HasMagic(buffer, "DDS ", 4) ||
            HasMagic(buffer, "JMX", 3) ||
            HasMagic(buffer, "RIFF", 4) ||
            HasMagic(buffer, "OggS", 4) ||
            HasMagic(buffer, "PK\003\004", 4) ||
            HasMagic(buffer, "\x89PNG", 4) ||
            HasMagic(buffer, "\xFF\xD8\xFF", 3) ||
            HasMagic(buffer, "BM", 2) ||
            HasMagic(buffer, "MZ", 2))
        {
            return true;
        }

        const std::string lowerPath = ToLowerAscii(path);
        const bool textLikeExtension =
            EndsWith(lowerPath, ".txt") || EndsWith(lowerPath, ".ini") || EndsWith(lowerPath, ".xml") ||
            EndsWith(lowerPath, ".cfg") || EndsWith(lowerPath, ".csv") || EndsWith(lowerPath, ".lst") ||
            EndsWith(lowerPath, ".lua") || EndsWith(lowerPath, ".json") || EndsWith(lowerPath, ".bsr") ||
            EndsWith(lowerPath, ".bms") || EndsWith(lowerPath, ".ddj");

        const size_t sample = std::min<size_t>(buffer.size(), 4096);
        size_t printable = 0;
        size_t control = 0;
        size_t zero = 0;
        for(size_t i = 0; i < sample; ++i)
        {
            const uint8_t ch = buffer[i];
            if(ch == 0)
            {
                ++zero;
            }
            if((ch >= 32 && ch <= 126) || ch == '\r' || ch == '\n' || ch == '\t')
            {
                ++printable;
            }
            else if(ch < 32)
            {
                ++control;
            }
        }

        const double printableRatio = sample == 0 ? 1.0 : static_cast<double>(printable) / static_cast<double>(sample);
        const double controlRatio = sample == 0 ? 0.0 : static_cast<double>(control) / static_cast<double>(sample);
        const double zeroRatio = sample == 0 ? 0.0 : static_cast<double>(zero) / static_cast<double>(sample);
        if(textLikeExtension)
        {
            return printableRatio >= 0.60 && zeroRatio < 0.20;
        }
        return printableRatio >= 0.85 && controlRatio < 0.08 && zeroRatio < 0.03;
    }

    bool TryDecryptLikelyEncryptedPayload(const EntryItem & item, std::vector<uint8_t> & buffer)
    {
        if(buffer.empty() || LooksLikePlainPayload(item.relativePath, buffer))
        {
            return false;
        }

        std::vector<uint8_t> candidate = buffer;
        PK2PayloadCrypto_CryptBuffer(static_cast<uint64_t>(item.entry.position), item.entry.size, 0, candidate.data(), candidate.size());
        PK2PayloadCrypto_TryDecryptLooseBuffer(candidate);
        if(LooksLikePlainPayload(item.relativePath, candidate))
        {
            buffer.swap(candidate);
            return true;
        }
        return false;
    }

    bool OpenReaderWithFallback(PK2Reader & reader, const fs::path & pk2File, const std::string & blowfishKey, std::string & error)
    {
        std::string key = blowfishKey.empty() ? "169841" : blowfishKey;
        reader.SetDecryptionKey((char*)key.data(), static_cast<uint8_t>(std::min<size_t>(key.size(), 56)));
        bool opened = reader.Open(NarrowPath(pk2File).c_str());
        if(!opened)
        {
            std::string openError = reader.GetError();
            if(openError == "Invalid Blowfish key.")
            {
                reader.SetDecryptionKey((char*)"\x32\x30\x30\x39\xC4\xEA");
                opened = reader.Open(NarrowPath(pk2File).c_str());
            }
            if(!opened)
            {
                error = openError.empty() ? reader.GetError() : openError;
                return false;
            }
        }
        return true;
    }

    bool CollectEntriesRecursive(PK2Reader & reader, PK2Entry & folder, const std::string & path, std::vector<EntryItem> & files, std::string & error)
    {
        std::list<PK2Entry> entries;
        if(!reader.GetEntries(folder, entries))
        {
            error = reader.GetError();
            return false;
        }

        for(PK2Entry & entry : entries)
        {
            if(strcmp(entry.name, ".") == 0 || strcmp(entry.name, "..") == 0)
            {
                continue;
            }

            std::string relativePath = path.empty() ? std::string(entry.name) : (path + "\\" + entry.name);
            if(entry.type == 2)
            {
                files.push_back({NormalizeRelativePath(relativePath), entry});
            }
            else if(entry.type == 1)
            {
                if(!CollectEntriesRecursive(reader, entry, relativePath, files, error))
                {
                    return false;
                }
            }
        }
        return true;
    }

    bool CollectEntries(PK2Reader & reader, std::vector<EntryItem> & files, std::string & error)
    {
        files.clear();
        PK2Entry root{};
        root.type = 1;
        root.position = sizeof(PK2Header);
        if(!CollectEntriesRecursive(reader, root, std::string(), files, error))
        {
            return false;
        }
        std::sort(files.begin(), files.end(), [](const EntryItem & a, const EntryItem & b)
        {
            return ToLowerAscii(a.relativePath) < ToLowerAscii(b.relativePath);
        });
        return true;
    }

    bool WriteBufferToFile(const fs::path & outputPath, const std::vector<uint8_t> & buffer, std::string & error)
    {
        if(!EnsureDirectory(outputPath.parent_path(), error))
        {
            return false;
        }
        FILE * out = nullptr;
        fopen_s(&out, NarrowPath(outputPath).c_str(), "wb");
        if(!out)
        {
            error = "Could not create output file: " + NarrowPath(outputPath);
            return false;
        }
        const size_t written = buffer.empty() ? 0 : fwrite(buffer.data(), 1, buffer.size(), out);
        fclose(out);
        if(written != buffer.size())
        {
            DeleteFileA(NarrowPath(outputPath).c_str());
            error = "Could not write output file: " + NarrowPath(outputPath);
            return false;
        }
        return true;
    }

    bool ExtractEntry(PK2Reader & reader,
        const fs::path & pk2File,
        const EntryItem & item,
        const fs::path & outputFolder,
        bool rawOutput,
        NativeProgressContext * progressContext,
        uint64_t & doneBytes,
        uint64_t totalBytes,
        std::string & error)
    {
        fs::path outputPath;
        if(!BuildOutputPath(outputFolder, item.relativePath, outputPath, error))
        {
            return false;
        }

        PK2Entry entry = item.entry;
        if(entry.type != 2)
        {
            error = "The PK2 entry is not a file: " + item.relativePath;
            return false;
        }
        if(entry.position < static_cast<int64_t>(sizeof(PK2Header)))
        {
            error = "Invalid PK2 payload offset for: " + item.relativePath;
            return false;
        }

        std::vector<uint8_t> buffer;
        buffer.resize(entry.size);
        if(entry.size > 0)
        {
            FILE * in = nullptr;
            fopen_s(&in, NarrowPath(pk2File).c_str(), "rb");
            if(!in)
            {
                error = "Could not open PK2 file for extraction: " + NarrowPath(pk2File);
                return false;
            }
            if(file_seek(in, entry.position, SEEK_SET) != 0)
            {
                fclose(in);
                error = "Could not seek to PK2 payload: " + item.relativePath;
                return false;
            }

            uint64_t remaining = entry.size;
            uint64_t offset = 0;
            while(remaining > 0)
            {
                const size_t chunk = static_cast<size_t>(std::min<uint64_t>(1024ULL * 1024ULL, remaining));
                const size_t readCount = fread(buffer.data() + static_cast<size_t>(offset), 1, chunk, in);
                if(readCount != chunk)
                {
                    fclose(in);
                    buffer.clear();
                    error = "Could not read all file data: " + item.relativePath;
                    return false;
                }

                offset += readCount;
                remaining -= readCount;
                doneBytes += readCount;

                const int percent = totalBytes > 0
                    ? static_cast<int>((doneBytes * 1000ULL) / totalBytes)
                    : 0;
                if(!SendProgress(progressContext, percent, L"Extracting " + WidenUtf8(item.relativePath)))
                {
                    fclose(in);
                    error = "Extraction cancelled.";
                    return false;
                }
            }
            fclose(in);
        }
        else
        {
            const int percent = totalBytes > 0
                ? static_cast<int>((doneBytes * 1000ULL) / totalBytes)
                : 0;
            if(!SendProgress(progressContext, percent, L"Extracting " + WidenUtf8(item.relativePath)))
            {
                error = "Extraction cancelled.";
                return false;
            }
        }

        if(!rawOutput && !buffer.empty())
        {
            if(reader.HasPayloadEncryption() && PK2PayloadCrypto_ShouldDecrypt(item.relativePath))
            {
                PK2PayloadCrypto_DecryptBufferForFile(static_cast<uint64_t>(entry.position), entry.size, buffer.data(), buffer.size());
            }
            if(!PK2PayloadCrypto_TryDecryptLooseBuffer(buffer))
            {
                TryDecryptLikelyEncryptedPayload(item, buffer);
            }
        }

        return WriteBufferToFile(outputPath, buffer, error);
    }

    bool ReadSelectionFile(const fs::path & selectionFile, std::vector<std::string> & selectedFiles, std::string & error)
    {
        selectedFiles.clear();
        std::ifstream in(selectionFile, std::ios::binary);
        if(!in)
        {
            error = "Could not open selected file list: " + NarrowPath(selectionFile);
            return false;
        }

        std::string line;
        while(std::getline(in, line))
        {
            while(!line.empty() && (line.back() == '\r' || line.back() == '\n'))
            {
                line.pop_back();
            }
            if(line.size() >= 3 && static_cast<unsigned char>(line[0]) == 0xEF && static_cast<unsigned char>(line[1]) == 0xBB && static_cast<unsigned char>(line[2]) == 0xBF)
            {
                line.erase(0, 3);
            }
            if(!line.empty())
            {
                selectedFiles.push_back(NormalizeRelativePath(line));
            }
        }
        return true;
    }
}

extern "C" __declspec(dllexport) int __stdcall PK2Tools_ListPk2W(const wchar_t * pk2File, const wchar_t * listFile, const wchar_t * blowfishKey, wchar_t * errorBuffer, int errorChars)
{
    if(!pk2File || !listFile)
    {
        CopyError("Missing PK2 or list file path.", errorBuffer, errorChars);
        return 2;
    }

    std::string error;
    PK2Reader reader;
    if(!OpenReaderWithFallback(reader, fs::path(pk2File), NormalizeBlowfishKey(blowfishKey), error))
    {
        CopyError(error, errorBuffer, errorChars);
        return 3;
    }

    const bool payloadEncrypted = reader.HasPayloadEncryption();
    std::vector<EntryItem> files;
    if(!CollectEntries(reader, files, error))
    {
        reader.Close();
        CopyError(error.empty() ? reader.GetError() : error, errorBuffer, errorChars);
        return 4;
    }
    reader.Close();

    FILE * out = nullptr;
    _wfopen_s(&out, listFile, L"wb");
    if(!out)
    {
        CopyError("Could not create PK2 listing output file.", errorBuffer, errorChars);
        return 5;
    }
    for(const auto & item : files)
    {
        std::string state = payloadEncrypted ? "Encrypted payload" : "Plain payload";
        std::string line = item.relativePath + "\t" + std::to_string(item.entry.size) + "\t" + state + "\r\n";
        fwrite(line.data(), 1, line.size(), out);
    }
    fclose(out);
    return 0;
}

extern "C" __declspec(dllexport) int __stdcall PK2Tools_BuildPk2W(const wchar_t * sourceFolder, const wchar_t * outputPk2, int encryptEntries, int encryptPayloads, const wchar_t * blowfishKey, PK2ToolsProgressCallback callback, void * userdata, wchar_t * errorBuffer, int errorChars)
{
    if(!sourceFolder || !outputPk2)
    {
        CopyError("Missing source folder or output PK2 path.", errorBuffer, errorChars);
        return 2;
    }

    std::string error;
    fs::path output = fs::path(outputPk2);
    if(!EnsureDirectory(output.parent_path(), error))
    {
        CopyError(error, errorBuffer, errorChars);
        return 3;
    }

    NativeProgressContext ctx;
    ctx.callback = callback;
    ctx.userdata = userdata;
    ctx.operation = L"Building PK2";
    SendProgress(&ctx, 0, L"Preparing PK2 build...");

    PK2Writer writer;
    std::string key = NormalizeBlowfishKey(blowfishKey);
    writer.SetEncryptionKey((char*)key.data(), static_cast<uint8_t>(key.size()));
    writer.SetEncryptEntries(encryptEntries != 0);
    writer.SetEncryptPayloads(encryptPayloads != 0);
    writer.SetProgressCallback(&WriterProgressBridge, &ctx);
    if(!writer.BuildFromFolder(fs::path(sourceFolder), output))
    {
        CopyError(writer.GetError(), errorBuffer, errorChars);
        return 4;
    }

    SendProgress(&ctx, 1000, L"PK2 build completed.");
    return 0;
}

extern "C" __declspec(dllexport) int __stdcall PK2Tools_CryptPk2W(const wchar_t * pk2File, int encryptPayloads, const wchar_t * blowfishKey, PK2ToolsProgressCallback callback, void * userdata, wchar_t * errorBuffer, int errorChars)
{
    if(!pk2File)
    {
        CopyError("Missing PK2 file path.", errorBuffer, errorChars);
        return 2;
    }

    NativeProgressContext ctx;
    ctx.callback = callback;
    ctx.userdata = userdata;
    ctx.operation = encryptPayloads ? L"Encrypting PK2 payloads" : L"Decrypting PK2 payloads";
    SendProgress(&ctx, 0, ctx.operation);

    PK2Writer writer;
    std::string key = NormalizeBlowfishKey(blowfishKey);
    writer.SetEncryptionKey((char*)key.data(), static_cast<uint8_t>(key.size()));
    writer.SetProgressCallback(&WriterProgressBridge, &ctx);
    if(!writer.CryptPayloadsInPlace(fs::path(pk2File), encryptPayloads != 0))
    {
        CopyError(writer.GetError(), errorBuffer, errorChars);
        return 3;
    }

    SendProgress(&ctx, 1000, encryptPayloads ? L"PK2 internal payload encryption completed." : L"PK2 internal payload decryption completed.");
    return 0;
}

extern "C" __declspec(dllexport) int __stdcall PK2Tools_ImportFolderW(const wchar_t * pk2File, const wchar_t * sourceFolder, const wchar_t * internalFolder, int encryptPayloads, const wchar_t * blowfishKey, PK2ToolsProgressCallback callback, void * userdata, wchar_t * errorBuffer, int errorChars)
{
    if(!pk2File || !sourceFolder)
    {
        CopyError("Missing PK2 file or import source folder.", errorBuffer, errorChars);
        return 2;
    }

    NativeProgressContext ctx;
    ctx.callback = callback;
    ctx.userdata = userdata;
    ctx.operation = encryptPayloads ? L"Preparing encrypted import" : L"Preparing plain import";
    ctx.progressBase = 0;
    ctx.progressSpan = 420;
    SendProgress(&ctx, 0, ctx.operation);

    PK2Writer writer;
    std::string key = NormalizeBlowfishKey(blowfishKey);
    writer.SetEncryptionKey((char*)key.data(), static_cast<uint8_t>(key.size()));
    writer.SetProgressCallback(&WriterProgressBridge, &ctx);

    // Normalize the whole archive first. This keeps old entries and newly
    // imported entries in the same payload state, which avoids mixed PK2s.
    if(!writer.CryptPayloadsInPlace(fs::path(pk2File), encryptPayloads != 0))
    {
        CopyError(writer.GetError(), errorBuffer, errorChars);
        return 3;
    }

    ctx.operation = L"Injecting folder into PK2";
    ctx.progressBase = 420;
    ctx.progressSpan = 580;
    SendProgress(&ctx, 0, L"Injecting folder into PK2...");
    std::string internal = internalFolder ? fs::path(internalFolder).string() : std::string();
    if(!writer.ImportFolder(fs::path(pk2File), fs::path(sourceFolder), internal, fs::path(pk2File), true))
    {
        CopyError(writer.GetError(), errorBuffer, errorChars);
        return 4;
    }

    SendProgress(&ctx, 1000, encryptPayloads ? L"Import completed with encrypted internal payloads." : L"Import completed with plain internal payloads.");
    return 0;
}

extern "C" __declspec(dllexport) int __stdcall PK2Tools_ExtractPk2W(const wchar_t * pk2File, const wchar_t * outputFolder, const wchar_t * selectionFile, int extractAll, int rawOutput, const wchar_t * blowfishKey, PK2ToolsProgressCallback callback, void * userdata, wchar_t * errorBuffer, int errorChars)
{
    if(!pk2File || !outputFolder)
    {
        CopyError("Missing PK2 file or output folder.", errorBuffer, errorChars);
        return 2;
    }

    std::string error;
    std::vector<std::string> selectedFiles;
    if(!extractAll)
    {
        if(!selectionFile || !ReadSelectionFile(fs::path(selectionFile), selectedFiles, error))
        {
            CopyError(error.empty() ? "Missing selected file list." : error, errorBuffer, errorChars);
            return 3;
        }
        if(selectedFiles.empty())
        {
            CopyError("No internal PK2 files were selected.", errorBuffer, errorChars);
            return 3;
        }
    }

    PK2Reader reader;
    if(!OpenReaderWithFallback(reader, fs::path(pk2File), NormalizeBlowfishKey(blowfishKey), error))
    {
        CopyError(error, errorBuffer, errorChars);
        return 4;
    }

    std::vector<EntryItem> files;
    if(!CollectEntries(reader, files, error))
    {
        reader.Close();
        CopyError(error.empty() ? reader.GetError() : error, errorBuffer, errorChars);
        return 5;
    }

    if(!extractAll)
    {
        std::map<std::string, EntryItem> lookup;
        for(const auto & item : files)
        {
            lookup[ToLowerAscii(item.relativePath)] = item;
        }

        std::vector<EntryItem> filtered;
        for(const auto & selected : selectedFiles)
        {
            auto it = lookup.find(ToLowerAscii(NormalizeRelativePath(selected)));
            if(it == lookup.end())
            {
                reader.Close();
                CopyError("Could not find PK2 entry: " + selected, errorBuffer, errorChars);
                return 6;
            }
            filtered.push_back(it->second);
        }
        files.swap(filtered);
    }

    NativeProgressContext ctx;
    ctx.callback = callback;
    ctx.userdata = userdata;
    ctx.operation = rawOutput ? L"Extracting raw PK2 payloads" : L"Extracting plain PK2 files";

    uint64_t totalBytes = 0;
    for(const auto & item : files)
    {
        totalBytes += item.entry.size;
    }

    uint64_t doneBytes = 0;
    for(size_t i = 0; i < files.size(); ++i)
    {
        const auto & item = files[i];
        int percent = totalBytes > 0 ? static_cast<int>((doneBytes * 1000ULL) / totalBytes) : static_cast<int>((i * 1000ULL) / std::max<size_t>(files.size(), 1));
        std::wstring status = L"Extracting " + WidenUtf8(item.relativePath);
        if(!SendProgress(&ctx, percent, status))
        {
            reader.Close();
            CopyError("Extraction cancelled.", errorBuffer, errorChars);
            return 8;
        }
        if(!ExtractEntry(reader, fs::path(pk2File), item, fs::path(outputFolder), rawOutput != 0, &ctx, doneBytes, totalBytes, error))
        {
            reader.Close();
            CopyError(error, errorBuffer, errorChars);
            return 7;
        }
    }

    reader.Close();
    SendProgress(&ctx, 1000, L"PK2 extraction completed.");
    return 0;
}
