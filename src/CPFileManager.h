#pragma once

#include "IFileManager.h"
#include "pk2/PK2Reader.h"

#include <memory>
#include <string>
#include <unordered_map>
#include <vector>

class CPFileManager : public IFileManager {
private:
    struct MountedPk2 {
        std::string logicalName;
        std::string physicalPath;
        std::unique_ptr<PK2Reader> reader;
    };

    struct OpenFileInfo {
        int id;
        bool isPk2File;
        HANDLE hFile;
        char filename[260];
        std::vector<unsigned char> data;
        size_t cursor;
        FILETIME creationTime;
        FILETIME lastWriteTime;
        OpenFileInfo()
            : id(0), isPk2File(false), hFile(INVALID_HANDLE_VALUE), cursor(0) {
            filename[0] = 0;
            ZeroMemory(&creationTime, sizeof(creationTime));
            ZeroMemory(&lastWriteTime, sizeof(lastWriteTime));
        }
    };

    struct SearchState {
        std::vector<search_result_t> results;
        size_t index;
        SearchState() : index(0) {}
    };

    char root_dir[260];
    char current_dir[260];
    __int32 bIsOpen;
    char pad_0x0210[0xC];
    HMODULE mainModuleHandle;
    char pad_0x0220[0x1C];
    unsigned char bSomething;
    char pad_0x023D[0x2B];
    error_handler_t error_handler;
    HWND hwnd;
    char pad_0x0270[0x1D0];

    int bListMoreFiles;
    int nextHandleId;
    std::vector<MountedPk2> mountedPk2s;
    std::unordered_map<int, OpenFileInfo> openFiles;
    std::string physicalBaseDir;

    std::string NormalizePath(const char* path) const;
    std::string CombineVirtualPath(const char* filename) const;
    std::string GetBaseDirectoryFromContainerArg(const char* filename) const;
    std::vector<std::string> GetSearchCandidates(const char* filename) const;
    std::string ExtractLogicalPk2Name(const std::string& normalizedPath) const;
    std::vector<size_t> GetPreferredPk2Order(const char* filename) const;
    bool MountPk2(const std::string& logicalName, const std::string& physicalPath, const char* password);
    bool EnsurePk2MountedByLogicalName(const std::string& logicalName, const char* password);
    bool EnsureDefaultPk2sMounted(const char* password);
    bool FindEntryAcrossPk2s(const char* filename, size_t& pk2Index, PK2Entry& entry, std::string& resolvedPath);
    bool EnumerateCurrentDirectory(const char* pattern, std::vector<search_result_t>& results);
    std::string ResolvePhysicalPath(const char* filename) const;
    int GetNextHandleId();

public:
    virtual int Mode(void);
    virtual int ConfigSet(int, int);
    virtual int ConfigGet(int, int);

    virtual int CreateContainer(const char *filename,  const char *password);
    virtual int OpenContainer(const char *filename, const char* password, int mode);
    virtual int CloseContainer(void);
    virtual int IsOpen(void);

    virtual int CloseAllFiles(void);
    virtual HMODULE MainModuleHandle(void);
    virtual int Function_9(int);

    virtual int Open(CJArchiveFm *fm, const char *filename, int access, int unknown);
    virtual int Open(const char *filename, int access, int unknown);

    virtual int Function_12(void);
    virtual int Function_13(void);
    virtual int Create(CJArchiveFm * fm, const char * filename, int unknown);
    virtual int Create(const char* filename, int unknown);

    virtual int Delete(const char *filename);
    virtual int Close(int index);

    virtual int Read(int hFile, char* lpBuffer, int nNumberOfBytesToRead, unsigned long *lpNumberOfBytesRead);
    virtual int Write(int hFile, const char* lpBuffer, int nNumberOfBytesToWrite, unsigned long *lpNumberOfBytesWritten);

    virtual char* CmdLinePath(void);
    virtual char* CmdLineExe(void);

    virtual __int64* GetDirectoryPosition(__int64* pPosition) override;
    virtual bool SetDirectoryPosition(__int64 position) override;

    virtual int DirectoryCreate(const char* name);
    virtual int DirectoryRemove(const char* name);

    virtual bool ResetDirectory(void);
    virtual bool ChangeDirectory(const char* dirname);
    virtual int GetDirectoryName(size_t buffersize, char* Dst);

    virtual int SetVirtualPath(const char *path);
    virtual int GetVirtualPath(char* dest);

    virtual search_handle_t* FindFirstFile(search_handle_t* pSearchHandle, const char* pSearchPattern, search_result_t* pSearchResult);
    virtual bool FindNextFile(search_handle_t* pSearchHandle, search_result_t* pSearchResult);
    virtual bool FindClose(search_handle_t* pSearchHandle);

    virtual int FileNameFromHandle(int hFile, char* dst, size_t count);
    virtual int GetFileSize(int hFile, LPDWORD lpFileSizeHigh);
    virtual BOOL GetFileTime(int hFile, LPFILETIME lpCreationTime, LPFILETIME lpLastWriteTime);
    virtual BOOL SetFileTime(int hFile, LPFILETIME lpCreationTime, LPFILETIME lpLastWriteTime);
    virtual int Seek(int hFile, LONG lDistanceToMove, DWORD dwMoveMethod);

    virtual HWND GetHwnd(void);
    virtual void SetHwnd(HWND);
    virtual void RegisterErrorHandler(error_handler_t callback);

    virtual int ImportDirectory(const char *srcdir, const char *dstdir, const char *directory_name, bool create_target_dir);
    virtual int ImportFile(const char *srcdir, const char *dstdir, const char *filename, bool create_target_dir);
    virtual int ExportDirectory(const char *srcdir, const char *dstdir, const char *directory_name, bool create_target_dir);
    virtual int ExportFile(const char *srcdir, const char *dstdir, const char *filename, bool create_target_dir);

    virtual int FileExists(char* name, int flags);

    virtual int UpdateCurrentDirectory(void);
    virtual int Function_50(int);

    virtual int Lock(int);
    virtual int Unlock();

    virtual ~CPFileManager() { };

    CPFileManager() {
        this->bIsOpen = 0;
        this->bListMoreFiles = 1;
        this->nextHandleId = 0;
        this->root_dir[0] = 0;
        this->current_dir[0] = 0;
        this->mainModuleHandle = GetModuleHandleA(0);
        this->physicalBaseDir.clear();
        this->bSomething = 0;
        this->error_handler = 0;
        this->hwnd = 0;
    }
};
