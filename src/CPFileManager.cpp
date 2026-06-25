#include "CPFileManager.h"

#include <Windows.h>
#include <Shlwapi.h>
#pragma comment(lib, "shlwapi.lib")

#include <algorithm>
#include <cstring>
#include <list>
#include <set>
#include <string>
#include <vector>

#include "PK2PayloadCrypto.h"
#include "debug.h"
#include "commandine.h"

namespace {
    const char* kDefaultPk2Names[] = {
        "Media.pk2",
        "Data.pk2",
        "Map.pk2",
        "Music.pk2",
        "Particles.pk2"
    };

    static bool IsRootDots(const char* name) {
        return name && name[0] == '.' && (name[1] == 0 || (name[1] == '.' && name[2] == 0));
    }
}

std::string CPFileManager::NormalizePath(const char* path) const {
    std::string s = path ? path : "";
    std::replace(s.begin(), s.end(), '/', '\\');
    std::transform(s.begin(), s.end(), s.begin(), [](unsigned char c) { return static_cast<char>(tolower(c)); });
    while (!s.empty() && s.front() == '\\') {
        s.erase(s.begin());
    }
    while (!s.empty() && s.back() == '\\') {
        s.pop_back();
    }
    return s;
}

std::string CPFileManager::CombineVirtualPath(const char* filename) const {
    std::string dir = NormalizePath(current_dir);
    std::string name = NormalizePath(filename);
    if (dir.empty()) return name;
    if (name.empty()) return dir;
    return dir + "\\" + name;
}

std::string CPFileManager::GetBaseDirectoryFromContainerArg(const char* filename) const {
    char modulePath[MAX_PATH] = {0};
    GetModuleFileNameA(nullptr, modulePath, MAX_PATH);
    PathRemoveFileSpecA(modulePath);

    if (!filename || !*filename) {
        return modulePath;
    }

    char resolved[MAX_PATH] = {0};
    strcpy_s(resolved, sizeof(resolved), filename);

    if (PathIsRelativeA(resolved)) {
        char combined[MAX_PATH] = {0};
        PathCombineA(combined, modulePath, resolved);
        strcpy_s(resolved, sizeof(resolved), combined);
    }

    PathRemoveFileSpecA(resolved);
    return resolved;
}

std::string CPFileManager::ResolvePhysicalPath(const char* filename) const {
    char path[MAX_PATH] = {0};
    const std::string& baseDir = physicalBaseDir.empty() ? GetBaseDirectoryFromContainerArg(nullptr) : physicalBaseDir;

    std::string normalized = filename ? filename : "";
    std::replace(normalized.begin(), normalized.end(), '/', '\\');
    while (!normalized.empty() && normalized.front() == '\\') {
        normalized.erase(normalized.begin());
    }

    if (normalized.empty()) {
        return baseDir;
    }

    if (PathIsRelativeA(normalized.c_str())) {
        PathCombineA(path, baseDir.c_str(), normalized.c_str());
        return path;
    }

    return normalized;
}

std::vector<std::string> CPFileManager::GetSearchCandidates(const char* filename) const {
    std::vector<std::string> candidates;
    std::string requested = NormalizePath(filename);
    std::string combined = CombineVirtualPath(filename);

    if (!combined.empty()) candidates.push_back(combined);
    if (!requested.empty() && requested != combined) candidates.push_back(requested);

    std::vector<std::string> expanded;
    for (size_t i = 0; i < candidates.size(); ++i) {
        const std::string& c = candidates[i];
        expanded.push_back(c);
        size_t slash = c.find('\\');
        if (slash != std::string::npos) {
            std::string first = c.substr(0, slash);
            for (const char* name : kDefaultPk2Names) {
                std::string logical = NormalizePath(name);
                size_t dot = logical.rfind('.');
                if (dot != std::string::npos) logical.erase(dot);
                if (first == logical) {
                    std::string stripped = c.substr(slash + 1);
                    if (!stripped.empty()) expanded.push_back(stripped);
                    break;
                }
            }
        }
    }

    std::set<std::string> unique;
    std::vector<std::string> finalList;
    for (size_t i = 0; i < expanded.size(); ++i) {
        if (!expanded[i].empty() && unique.insert(expanded[i]).second) {
            finalList.push_back(expanded[i]);
        }
    }
    return finalList;
}


std::string CPFileManager::ExtractLogicalPk2Name(const std::string& normalizedPath) const {
    if (normalizedPath.empty()) return std::string();
    size_t slash = normalizedPath.find('\\');
    std::string first = (slash == std::string::npos) ? normalizedPath : normalizedPath.substr(0, slash);
    for (size_t i = 0; i < mountedPk2s.size(); ++i) {
        if (mountedPk2s[i].logicalName == first) return first;
    }
    return std::string();
}

std::vector<size_t> CPFileManager::GetPreferredPk2Order(const char* filename) const {
    std::vector<size_t> order;
    std::string preferred = ExtractLogicalPk2Name(NormalizePath(filename));
    if (preferred.empty()) preferred = ExtractLogicalPk2Name(NormalizePath(current_dir));
    if (preferred.empty()) preferred = ExtractLogicalPk2Name(NormalizePath(root_dir));

    if (!preferred.empty()) {
        for (size_t i = 0; i < mountedPk2s.size(); ++i) {
            if (mountedPk2s[i].logicalName == preferred) {
                order.push_back(i);
                break;
            }
        }
    }
    for (size_t i = 0; i < mountedPk2s.size(); ++i) {
        if (std::find(order.begin(), order.end(), i) == order.end()) order.push_back(i);
    }
    return order;
}

bool CPFileManager::MountPk2(const std::string& logicalName, const std::string& physicalPath, const char* password) {
    std::string normalizedLogical = NormalizePath(logicalName.c_str());
    size_t dot = normalizedLogical.rfind('.');
    if (dot != std::string::npos) normalizedLogical.erase(dot);

    for (size_t i = 0; i < mountedPk2s.size(); ++i) {
        if (mountedPk2s[i].logicalName == normalizedLogical) {
            return true;
        }
    }

    MountedPk2 mounted;
    mounted.logicalName = normalizedLogical;
    mounted.physicalPath = physicalPath;
    mounted.reader = std::make_unique<PK2Reader>();
    if (password && *password) {
        mounted.reader->SetDecryptionKey(const_cast<char*>(password), static_cast<uint8_t>(strlen(password)));
    }
    if (!mounted.reader->Open(physicalPath.c_str())) {
        debug(DEBUG_CONTAINER, "PK2 mount failed: %s (%s)\n", physicalPath.c_str(), mounted.reader->GetError().c_str());
        return false;
    }
    debug(DEBUG_CONTAINER, "PK2 mounted: logical=%s physical=%s\n", mounted.logicalName.c_str(), mounted.physicalPath.c_str());
    mountedPk2s.push_back(std::move(mounted));
    return true;
}

bool CPFileManager::EnsurePk2MountedByLogicalName(const std::string& logicalName, const char* password) {
    std::string normalized = NormalizePath(logicalName.c_str());
    if (normalized.empty()) return false;

    for (size_t i = 0; i < mountedPk2s.size(); ++i) {
        if (mountedPk2s[i].logicalName == normalized) return true;
    }

    for (const char* pk2Name : kDefaultPk2Names) {
        std::string probe = NormalizePath(pk2Name);
        size_t dot = probe.rfind('.');
        if (dot != std::string::npos) probe.erase(dot);
        if (probe == normalized) {
            char full[MAX_PATH] = {0};
            PathCombineA(full, physicalBaseDir.c_str(), pk2Name);
            return MountPk2(pk2Name, full, password && *password ? password : "169841");
        }
    }

    return false;
}

bool CPFileManager::EnsureDefaultPk2sMounted(const char* password) {
    bool mountedAny = false;
    for (const char* pk2Name : kDefaultPk2Names) {
        char full[MAX_PATH] = {0};
        PathCombineA(full, physicalBaseDir.c_str(), pk2Name);
        mountedAny = MountPk2(pk2Name, full, password && *password ? password : "169841") || mountedAny;
    }
    return mountedAny;
}

bool CPFileManager::FindEntryAcrossPk2s(const char* filename, size_t& pk2Index, PK2Entry& entry, std::string& resolvedPath) {
    std::vector<std::string> candidates = GetSearchCandidates(filename);
    std::vector<size_t> order = GetPreferredPk2Order(filename);
    for (size_t oi = 0; oi < order.size(); ++oi) {
        const size_t i = order[oi];
        for (size_t c = 0; c < candidates.size(); ++c) {
            std::string probePath = candidates[c];
            std::string first = ExtractLogicalPk2Name(probePath);
            if (!first.empty()) {
                size_t slash = probePath.find('\\');
                std::string stripped = (slash == std::string::npos) ? std::string() : probePath.substr(slash + 1);
                if (first != mountedPk2s[i].logicalName) {
                    continue;
                }
                probePath = stripped;
            }
            if (probePath.empty()) continue;
            PK2Entry probe = {};
            if (mountedPk2s[i].reader->GetEntry(probePath.c_str(), probe)) {
                pk2Index = i;
                entry = probe;
                resolvedPath = probePath;
                return true;
            }
        }
    }
    return false;
}

bool CPFileManager::EnumerateCurrentDirectory(const char* pattern, std::vector<search_result_t>& results) {
    std::set<std::string> uniqueNames;
    std::string directory = NormalizePath(current_dir);
    std::string spec = pattern ? pattern : "*.*";
    const char* localPattern = spec.c_str();
    const char* slash = strrchr(localPattern, '\\');
    if (!slash) slash = strrchr(localPattern, '/');
    if (slash) localPattern = slash + 1;
    if (!*localPattern) localPattern = "*.*";

    for (size_t i = 0; i < mountedPk2s.size(); ++i) {
        PK2Entry parent = {};
        std::list<PK2Entry> entries;
        bool ok = false;
        if (directory.empty()) {
            parent.type = 1;
            parent.position = 256;
            ok = mountedPk2s[i].reader->GetEntries(parent, entries);
        } else if (mountedPk2s[i].reader->GetEntry(directory.c_str(), parent)) {
            ok = mountedPk2s[i].reader->GetEntries(parent, entries);
        }
        if (!ok) continue;

        for (std::list<PK2Entry>::const_iterator it = entries.begin(); it != entries.end(); ++it) {
            if (IsRootDots(it->name)) continue;
            if (!PathMatchSpecA(it->name, localPattern)) continue;
            std::string lowered = NormalizePath(it->name);
            if (!uniqueNames.insert(lowered).second) continue;

            search_result_t r = {};
            strcpy_s(r.Name, sizeof(r.Name), it->name);
            r.Type = (it->type == 1) ? ENTRY_TYPE_FOLDER : ENTRY_TYPE_FILE;
            r.Size = static_cast<int>(it->size);
            strcpy_s(r.find_data.cFileName, sizeof(r.find_data.cFileName), it->name);
            r.find_data.dwFileAttributes = (it->type == 1) ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_ARCHIVE;
            ULARGE_INTEGER u;
            u.QuadPart = it->createTime;
            r.CreationTime.dwLowDateTime = u.LowPart;
            r.CreationTime.dwHighDateTime = u.HighPart;
            r.find_data.ftCreationTime = r.CreationTime;
            u.QuadPart = it->modifyTime;
            r.LastWriteTime.dwLowDateTime = u.LowPart;
            r.LastWriteTime.dwHighDateTime = u.HighPart;
            r.find_data.ftLastWriteTime = r.LastWriteTime;
            r.find_data.nFileSizeLow = it->size;
            results.push_back(r);
        }
    }

    return !results.empty();
}

int CPFileManager::GetNextHandleId() {
    do {
        ++nextHandleId;
        if (nextHandleId <= 0) nextHandleId = 1;
    } while (openFiles.find(nextHandleId) != openFiles.end());
    return nextHandleId;
}

int CPFileManager::Mode() {
    debug(DEBUG_OTHER, "WFM::get_mode() = 1\n");
    return 1;
}

int CPFileManager::ConfigSet(int a, int b) {
    if (a == 2) {
        bSomething = (b == 0) ? 0 : 1;
    }
    debug(DEBUG_UNKNOWN, "WFM::ConfigSet(%d , %d) = 0\n", a, b);
    return 0;
}

int CPFileManager::ConfigGet(int a, int b) {
    debug(DEBUG_UNKNOWN, "WFM::ConfigGet(%d , %d) = 0\n", a, b);
    return 0;
}

int CPFileManager::CreateContainer(const char *filename, const char *password) {
    debug(DEBUG_CONTAINER, "WFM::CreateContainer(\"%s\", \"%s\") = 0\n", filename ? filename : "", password ? password : "");
    return 0;
}

int CPFileManager::OpenContainer(const char *filename, const char* password, int mode) {
    debug(DEBUG_CONTAINER, "WFM::OpenContainer(\"%s\", \"%s\", 0x%08x)\n", filename ? filename : "", password ? password : "", mode);

    CloseContainer();

    std::string baseDir = GetBaseDirectoryFromContainerArg(filename);
    physicalBaseDir = baseDir;
    populate_cmdline();

    std::string requestedLogical;
    std::string requestedFileName;
    if (filename && *filename) {
        char fname[_MAX_FNAME] = {0};
        char ext[_MAX_EXT] = {0};
        _splitpath_s(filename, nullptr, 0, nullptr, 0, fname, sizeof(fname), ext, sizeof(ext));
        requestedLogical = NormalizePath(fname);
        requestedFileName = std::string(fname) + std::string(ext);
    }

    auto mountExact = [&](const char* pk2Name) {
        char full[MAX_PATH] = {0};
        PathCombineA(full, baseDir.c_str(), pk2Name);
        return MountPk2(pk2Name, full, password && *password ? password : "169841");
    };

    bool mountedAny = false;

    // Joymax opens one PK2 container at a time. Mounting all archives into one pool can
    // resolve a correct path from the wrong PK2 and break client startup with File Load Fail.
    if (!requestedFileName.empty()) {
        mountedAny = mountExact(requestedFileName.c_str());
        if (!mountedAny) {
            for (const char* pk2Name : kDefaultPk2Names) {
                std::string logical = NormalizePath(pk2Name);
                size_t dot = logical.rfind('.');
                if (dot != std::string::npos) logical.erase(dot);
                if (logical == requestedLogical) {
                    mountedAny = mountExact(pk2Name);
                    break;
                }
            }
        }
    }

    if (!mountedAny && requestedFileName.empty()) {
        for (const char* pk2Name : kDefaultPk2Names) {
            mountedAny = mountExact(pk2Name) || mountedAny;
        }
    }

    PK2PayloadCrypto_Initialize();
    strcpy_s(root_dir, sizeof(root_dir), requestedLogical.c_str());
    current_dir[0] = 0;
    bIsOpen = mountedAny ? 1 : 0;
    return bIsOpen;
}

int CPFileManager::CloseContainer() {
    CloseAllFiles();
    for (size_t i = 0; i < mountedPk2s.size(); ++i) {
        if (mountedPk2s[i].reader) mountedPk2s[i].reader->Close();
    }
    mountedPk2s.clear();
    bIsOpen = 0;
    root_dir[0] = 0;
    current_dir[0] = 0;
    debug(DEBUG_CONTAINER, "WFM::CloseContainer() = 1\n");
    return 1;
}

int CPFileManager::IsOpen() {
    debug(DEBUG_CONTAINER, "WFM::IsOpen() = %d\n", this->bIsOpen);
    return bIsOpen;
}

int CPFileManager::CloseAllFiles() {
    std::vector<int> ids;
    for (std::unordered_map<int, OpenFileInfo>::const_iterator it = openFiles.begin(); it != openFiles.end(); ++it) {
        ids.push_back(it->first);
    }
    for (size_t i = 0; i < ids.size(); ++i) {
        Close(ids[i]);
    }
    debug(DEBUG_UNKNOWN, "WFM::CloseAllFiles() = 1\n");
    return 1;
}

HMODULE CPFileManager::MainModuleHandle(void) {
    debug(DEBUG_OTHER, "WFM::MainModuleHandle() = %p\n", this->mainModuleHandle);
    return this->mainModuleHandle;
}

int CPFileManager::Function_9(int a) {
    debug(DEBUG_UNKNOWN, "WFM::Function_9(%d) = -1\n", a);
    return -1;
}

int CPFileManager::Open(CJArchiveFm *fm, const char *filename, int access, int unknown) {
    debug(DEBUG_FILE, "WFM::Open2(%p, \"%s\", 0x%08x, 0x%08x)\n", fm, filename ? filename : "", access, unknown);
    fm->field_15 = 1;
    fm->pFileManager = this;
    int handle = this->Open(filename, access, unknown);
    fm->hFile = handle;
    fm->is_write_mode = (access >> 30) & 1;
    fm->pCurrent = fm->is_write_mode ? fm->buffer : fm->pEnd;
    return fm->hFile == -1 ? 0 : 1;
}

int CPFileManager::Open(const char *filename, int access, int unknown) {
    (void)unknown;
    if (!filename || !*filename) return -1;
    if (access == GENERIC_WRITE || access == 0x40000000) {
        debug(DEBUG_FILE_NOTFOUND, "CPFileManager write not supported for PK2-backed files: %s\n", filename);
        return -1;
    }

    size_t pk2Index = 0;
    PK2Entry entry = {};
    std::string resolvedPath;
    if (!FindEntryAcrossPk2s(filename, pk2Index, entry, resolvedPath)) {
        std::string normalizedName = NormalizePath(filename);
        size_t slash = normalizedName.find('\\');
        if (slash != std::string::npos) {
            std::string first = normalizedName.substr(0, slash);
            if (EnsurePk2MountedByLogicalName(first, "169841")) {
                (void)FindEntryAcrossPk2s(filename, pk2Index, entry, resolvedPath);
            }
        }
        if (resolvedPath.empty()) {
            EnsureDefaultPk2sMounted("169841");
            (void)FindEntryAcrossPk2s(filename, pk2Index, entry, resolvedPath);
        }
    }

    if (resolvedPath.empty()) {
        std::string physicalPath = ResolvePhysicalPath(filename);
        DWORD attrs = GetFileAttributesA(physicalPath.c_str());
        if (attrs != INVALID_FILE_ATTRIBUTES && !(attrs & FILE_ATTRIBUTE_DIRECTORY)) {
            HANDLE hFile = CreateFileA(physicalPath.c_str(), access == GENERIC_WRITE ? GENERIC_WRITE : GENERIC_READ, FILE_SHARE_READ, nullptr, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, nullptr);
            if (hFile != INVALID_HANDLE_VALUE) {
                int id = GetNextHandleId();
                OpenFileInfo info;
                info.id = id;
                info.isPk2File = false;
                info.hFile = hFile;
                strcpy_s(info.filename, sizeof(info.filename), physicalPath.c_str());
                ::GetFileTime(hFile, &info.creationTime, nullptr, &info.lastWriteTime);
                openFiles[id] = std::move(info);
                debug(DEBUG_FILE, "WFM::Open(\"%s\", 0x%x, 0x%x) = %d [fs=%s]\n", filename, access, unknown, id, physicalPath.c_str());
                return id;
            }
        }
        debug(DEBUG_FILE_NOTFOUND, "PK2/FS file not found: current='%s' name='%s'\n", current_dir, filename);
        return -1;
    }

    std::vector<uint8_t> buffer;
    if (!mountedPk2s[pk2Index].reader->ExtractToMemory(entry, buffer)) {
        debug(DEBUG_FILE_NOTFOUND, "PK2 extract failed: %s\n", resolvedPath.c_str());
        return -1;
    }

    if (!buffer.empty() && mountedPk2s[pk2Index].reader->HasPayloadEncryption() && PK2PayloadCrypto_ShouldDecrypt(resolvedPath)) {
        PK2PayloadCrypto_DecryptBufferForFile(static_cast<uint64_t>(entry.position), entry.size, buffer.data(), buffer.size());
    }

    int id = GetNextHandleId();
    OpenFileInfo info;
    info.id = id;
    info.isPk2File = true;
    info.data.assign(buffer.begin(), buffer.end());
    info.cursor = 0;
    strcpy_s(info.filename, sizeof(info.filename), resolvedPath.c_str());
    ULARGE_INTEGER u;
    u.QuadPart = entry.createTime;
    info.creationTime.dwLowDateTime = u.LowPart;
    info.creationTime.dwHighDateTime = u.HighPart;
    u.QuadPart = entry.modifyTime;
    info.lastWriteTime.dwLowDateTime = u.LowPart;
    info.lastWriteTime.dwHighDateTime = u.HighPart;
    openFiles[id] = std::move(info);

    debug(DEBUG_FILE, "WFM::Open(\"%s\", 0x%x, 0x%x) = %d [pk2=%s]\n", filename, access, unknown, id, mountedPk2s[pk2Index].physicalPath.c_str());
    return id;
}

int CPFileManager::Function_12(void)  {
    debug(DEBUG_UNKNOWN, "WFM::Function_12() = -1\n");
    return -1;
}

int CPFileManager::Function_13(void)  {
    debug(DEBUG_UNKNOWN, "WFM::Function_13() = 0\n");
    return 0;
}

int CPFileManager::Create(CJArchiveFm * fm, const char * filename, int unknown)  {
    debug(DEBUG_UNKNOWN, "WFM::Create(%p, %s, %d) = 0\n", fm, filename ? filename : "", unknown);
    return 0;
}

int CPFileManager::Create(const char* filename, int unknown) {
    debug(DEBUG_UNKNOWN, "WFM::Create(\"%s\", %08x) = 0\n", filename ? filename : "", unknown);
    return -1;
}

int CPFileManager::Delete(const char *filename) {
    debug(DEBUG_FILE, "WFM::Delete(\"%s\") = 0\n", filename ? filename : "");
    return 0;
}

int CPFileManager::Close(int hFile) {
    std::unordered_map<int, OpenFileInfo>::iterator it = openFiles.find(hFile);
    if (it == openFiles.end()) return 0;
    if (!it->second.isPk2File && it->second.hFile != INVALID_HANDLE_VALUE) {
        CloseHandle(it->second.hFile);
    }
    openFiles.erase(it);
    debug(DEBUG_FILE, "WFM::Close(%08x) = 1\n", hFile);
    return 1;
}

int CPFileManager::Read(int hFile, char* lpBuffer, int nNumberOfBytesToRead, unsigned long* lpNumberOfBytesRead) {
    std::unordered_map<int, OpenFileInfo>::iterator it = openFiles.find(hFile);
    if (it == openFiles.end()) return 0;
    if (lpNumberOfBytesRead) *lpNumberOfBytesRead = 0;
    if (nNumberOfBytesToRead <= 0) return 1;

    if (!it->second.isPk2File) {
        DWORD bytesRead = 0;
        BOOL ok = ReadFile(it->second.hFile, lpBuffer, static_cast<DWORD>(nNumberOfBytesToRead), &bytesRead, nullptr);
        if (lpNumberOfBytesRead) *lpNumberOfBytesRead = bytesRead;
        return ok ? 1 : 0;
    }

    size_t remaining = 0;
    if (it->second.cursor < it->second.data.size()) {
        remaining = it->second.data.size() - it->second.cursor;
    }
    size_t toRead = std::min<size_t>(static_cast<size_t>(nNumberOfBytesToRead), remaining);
    if (toRead > 0) {
        memcpy(lpBuffer, it->second.data.data() + it->second.cursor, toRead);
        it->second.cursor += toRead;
    }
    if (lpNumberOfBytesRead) *lpNumberOfBytesRead = static_cast<unsigned long>(toRead);

    debug(DEBUG_IO, "WFM::Read(%08x, %p, %d, %p) = 1\n", hFile, lpBuffer, nNumberOfBytesToRead, lpNumberOfBytesRead);
    return 1;
}

int CPFileManager::Write(int hFile, const char* lpBuffer, int nNumberOfBytesToWrite, unsigned long *lpNumberOfBytesWritten) {
    (void)hFile; (void)lpBuffer; (void)nNumberOfBytesToWrite;
    if (lpNumberOfBytesWritten) *lpNumberOfBytesWritten = 0;
    debug(DEBUG_IO, "WFM::Write(%08x, %p, %d, %p) = 0\n", hFile, lpBuffer, nNumberOfBytesToWrite, lpNumberOfBytesWritten);
    return 0;
}

char* CPFileManager::CmdLinePath(void){
    char* path = get_cmdline_path();
    debug(DEBUG_OTHER, "WFM::CmdLinePath() = \"%s\"\n", path ? path : "");
    return path;
}

char* CPFileManager::CmdLineExe(void) {
    char* exe = get_cmdline_exe();
    debug(DEBUG_OTHER, "WFM::CmdLineExe() = \"%s\"\n", exe ? exe : "");
    return exe;
}

__int64 * CPFileManager::GetDirectoryPosition(__int64 * pPosition) {
    *pPosition = 0;
    debug(DEBUG_OTHER, "WFM::GetDirectoryPosition(%p)\n", pPosition);
    return pPosition;
}

bool CPFileManager::SetDirectoryPosition(__int64 position) {
    debug(DEBUG_OTHER, "WFM::SetDirectoryPosition(0x%08llx) = 0\n", position);
    return false;
}

int CPFileManager::DirectoryCreate(const char* name) {
    debug(DEBUG_DIRECTORY, "WFM::DirectoryCreate(\"%s\") = 0\n", name ? name : "");
    return 0;
}

int CPFileManager::DirectoryRemove(const char* name) {
    debug(DEBUG_DIRECTORY, "WFM::DirectoryRemove(\"%s\") = 0\n", name ? name : "");
    return 0;
}

bool CPFileManager::ResetDirectory(void) {
    current_dir[0] = 0;
    debug(DEBUG_DIRECTORY, "WFM::ResetDirectory() = 1\n");
    return true;
}

bool CPFileManager::ChangeDirectory(const char* dirname) {
    if (!dirname || !*dirname) return true;
    std::string next = CombineVirtualPath(dirname);
    if (!next.empty()) {
        next = NormalizePath(next.c_str());
    }
    strcpy_s(current_dir, sizeof(current_dir), next.c_str());
    debug(DEBUG_DIRECTORY, "WFM::ChangeDirectory(\"%s\") = 1 [now=%s]\n", dirname, current_dir);
    return true;
}

int CPFileManager::GetDirectoryName(size_t buffersize, char* Dst) {
    strcpy_s(Dst, buffersize, this->current_dir);
    int len = static_cast<int>(strlen(Dst));
    debug(DEBUG_DIRECTORY, "WFM::GetDirectoryName(%zu, %p) = %d\n", buffersize, Dst, len);
    return len;
}

int CPFileManager::SetVirtualPath(const char *path) {
    std::string normalized = NormalizePath(path);
    strcpy_s(this->root_dir, sizeof(this->root_dir), normalized.c_str());
    strcpy_s(this->current_dir, sizeof(this->current_dir), normalized.c_str());
    debug(DEBUG_DIRECTORY, "WFM::SetVirtualPath(%s) = 1\n", path ? path : "");
    return 1;
}

int CPFileManager::GetVirtualPath(char* dest) {
    strcpy_s(dest, sizeof(this->current_dir), this->current_dir);
    debug(DEBUG_DIRECTORY, "WFM::GetVirtualPath(%p) = 1\n", dest);
    return 1;
}

search_handle_t* CPFileManager::FindFirstFile(search_handle_t* pSearchHandle, const char* pSearchPattern, search_result_t* pSearchResult) {
    debug(DEBUG_SEARCH, "WFM::FindFirstFile(%p, \"%s\", %p)\n", pSearchHandle, pSearchPattern ? pSearchPattern : "", pSearchResult);
    SearchState* state = new SearchState();
    EnumerateCurrentDirectory(pSearchPattern, state->results);
    pSearchHandle->hFind = reinterpret_cast<HANDLE>(state);
    if (state->results.empty()) {
        pSearchHandle->Success = 0;
        return pSearchHandle;
    }
    *pSearchResult = state->results[0];
    state->index = 1;
    pSearchHandle->Success = 1;
    return pSearchHandle;
}

bool CPFileManager::FindNextFile(search_handle_t* pSearchHandle, search_result_t* pSearchResult) {
    debug(DEBUG_SEARCH, "WFM::FindNextFile(%p, %p)\n", pSearchHandle, pSearchResult);
    SearchState* state = reinterpret_cast<SearchState*>(pSearchHandle->hFind);
    if (!state || state->index >= state->results.size()) {
        pSearchHandle->Success = 0;
        return false;
    }
    *pSearchResult = state->results[state->index++];
    pSearchHandle->Success = 1;
    return true;
}

bool CPFileManager::FindClose(search_handle_t* search) {
    debug(DEBUG_SEARCH, "WFM::FindClose(%p)\n", search);
    SearchState* state = reinterpret_cast<SearchState*>(search->hFind);
    delete state;
    search->hFind = 0;
    search->Success = 0;
    return true;
}

int CPFileManager::FileNameFromHandle(int hFile, char* dst, size_t count) {
    std::unordered_map<int, OpenFileInfo>::iterator it = openFiles.find(hFile);
    if (it == openFiles.end()) return 0;
    if (strlen(it->second.filename) + 1 > count) return 0;
    strcpy_s(dst, count, it->second.filename);
    return 1;
}

int CPFileManager::GetFileSize(int hFile, LPDWORD lpFileSizeHigh) {
    std::unordered_map<int, OpenFileInfo>::iterator it = openFiles.find(hFile);
    if (it == openFiles.end()) return 0;
    if (!it->second.isPk2File) {
        return ::GetFileSize(it->second.hFile, lpFileSizeHigh);
    }
    if (lpFileSizeHigh) *lpFileSizeHigh = 0;
    return static_cast<int>(it->second.data.size());
}

BOOL CPFileManager::GetFileTime(int hFile, LPFILETIME lpCreationTime, LPFILETIME lpLastWriteTime) {
    std::unordered_map<int, OpenFileInfo>::iterator it = openFiles.find(hFile);
    if (it == openFiles.end()) return FALSE;
    if (!it->second.isPk2File) {
        return ::GetFileTime(it->second.hFile, lpCreationTime, nullptr, lpLastWriteTime);
    }
    if (lpCreationTime) *lpCreationTime = it->second.creationTime;
    if (lpLastWriteTime) *lpLastWriteTime = it->second.lastWriteTime;
    return TRUE;
}

BOOL CPFileManager::SetFileTime(int hFile, LPFILETIME lpCreationTime, LPFILETIME lpLastWriteTime) {
    std::unordered_map<int, OpenFileInfo>::iterator it = openFiles.find(hFile);
    if (it == openFiles.end()) return FALSE;
    if (!it->second.isPk2File) {
        return ::SetFileTime(it->second.hFile, lpCreationTime, nullptr, lpLastWriteTime);
    }
    if (lpCreationTime) it->second.creationTime = *lpCreationTime;
    if (lpLastWriteTime) it->second.lastWriteTime = *lpLastWriteTime;
    return TRUE;
}

int CPFileManager::Seek(int hFile, LONG lDistanceToMove, DWORD dwMoveMethod) {
    std::unordered_map<int, OpenFileInfo>::iterator it = openFiles.find(hFile);
    if (it == openFiles.end()) return -1;
    if (!it->second.isPk2File) {
        return SetFilePointer(it->second.hFile, lDistanceToMove, nullptr, dwMoveMethod);
    }
    long long base = 0;
    if (dwMoveMethod == FILE_CURRENT) base = static_cast<long long>(it->second.cursor);
    else if (dwMoveMethod == FILE_END) base = static_cast<long long>(it->second.data.size());
    long long next = base + lDistanceToMove;
    if (next < 0) next = 0;
    if (static_cast<size_t>(next) > it->second.data.size()) next = static_cast<long long>(it->second.data.size());
    it->second.cursor = static_cast<size_t>(next);
    return static_cast<int>(it->second.cursor);
}

HWND CPFileManager::GetHwnd(void) {
    debug(DEBUG_OTHER, "WFM::GetHwnd() = %p\n", this->hwnd);
    return this->hwnd;
}

void CPFileManager::SetHwnd(HWND nhwnd) {
    debug(DEBUG_OTHER, "WFM::SetHwnd(%p)\n", nhwnd);
    this->hwnd = nhwnd;
}

void CPFileManager::RegisterErrorHandler(error_handler_t callback) {
    debug(DEBUG_OTHER, "WFM::RegisterErrorHandler(%p)\n", callback);
    this->error_handler = callback;
}

int CPFileManager::ImportDirectory(const char *srcdir, const char *dstdir, const char *directory_name, bool create_target_dir) {
    debug(DEBUG_UNKNOWN, "WFM::ImportDirectory(\"%s\", \"%s\", \"%s\", %d) = 0\n", srcdir ? srcdir : "", dstdir ? dstdir : "", directory_name ? directory_name : "", create_target_dir ? 1 : 0);
    return 0;
}

int CPFileManager::ImportFile(const char *srcdir, const char *dstdir, const char *filename, bool create_target_dir) {
    debug(DEBUG_FILE, "WFM::ImportFile(\"%s\", \"%s\", \"%s\", %d) = 0\n", srcdir ? srcdir : "", dstdir ? dstdir : "", filename ? filename : "", create_target_dir ? 1 : 0);
    return 0;
}

int CPFileManager::ExportDirectory(const char *srcdir, const char *dstdir, const char *directory_name, bool create_target_dir) {
    debug(DEBUG_UNKNOWN, "WFM::ExportDirectory(\"%s\", \"%s\", \"%s\", %d) = 0\n", srcdir ? srcdir : "", dstdir ? dstdir : "", directory_name ? directory_name : "", create_target_dir ? 1 : 0);
    return 0;
}

int CPFileManager::ExportFile(const char *srcdir, const char *dstdir, const char *filename, bool create_target_dir) {
    debug(DEBUG_UNKNOWN, "WFM::ExportFile(\"%s\", \"%s\", \"%s\", %d) = 0\n", srcdir ? srcdir : "", dstdir ? dstdir : "", filename ? filename : "", create_target_dir ? 1 : 0);
    return 0;
}

int CPFileManager::FileExists(char* name, int flags) {
    (void)flags;
    size_t pk2Index = 0;
    PK2Entry entry = {};
    std::string resolvedPath;
    int ret = FindEntryAcrossPk2s(name, pk2Index, entry, resolvedPath) ? 0 : -1;
    if (ret != 0) {
        std::string physicalPath = ResolvePhysicalPath(name);
        DWORD attrs = GetFileAttributesA(physicalPath.c_str());
        if (attrs != INVALID_FILE_ATTRIBUTES && !(attrs & FILE_ATTRIBUTE_DIRECTORY)) ret = 0;
    }
    debug(DEBUG_FILE, "WFM::FileExists(\"%s\", %08x) = %d\n", name ? name : "", flags, ret);
    return ret;
}

int CPFileManager::UpdateCurrentDirectory(void) {
    debug(DEBUG_UNKNOWN, "WFM::UpdateCurrentDirectory() = 0\n");
    return 0;
}

int CPFileManager::Function_50(int a) {
    debug(DEBUG_UNKNOWN, "WFM::Function_50(0x%08x) = 0\n", a);
    return 0;
}

int CPFileManager::Lock(int a) {
    debug(DEBUG_UNKNOWN, "WFM::Lock(0x%08x) = 0\n", a);
    return 0;
}

int CPFileManager::Unlock() {
    debug(DEBUG_UNKNOWN, "WFM::Unlock() = 0\n");
    return 0;
}
