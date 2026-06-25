#pragma once

#include "IFileManager.h"
#include "GFXInfo.h"
#include "pk2/PK2Reader.h"

#include <unordered_map>
#include <vector>
#include <memory>

class CWFileManager : public IFileManager {
private:

	struct OpenFileInfo {
		int field_0;
		bool isPk2File;
		HANDLE hFile;
		char filename[260];
		std::vector<unsigned char> data;
		size_t cursor;
		FILETIME creationTime;
		FILETIME lastWriteTime;
		OpenFileInfo() : field_0(0), isPk2File(false), hFile(INVALID_HANDLE_VALUE), cursor(0) {
			filename[0] = 0;
			ZeroMemory(&creationTime, sizeof(creationTime));
			ZeroMemory(&lastWriteTime, sizeof(lastWriteTime));
		}
	};

	struct PK2SearchState {
		std::vector<search_result_t> results;
		size_t index;
		PK2SearchState() : index(0) {}
	};

	char initial_path[260]; // 0x4
	char current_dir[260]; // 0x108
	gfxinfo_t *container_info; // 0x20C
		
	// consider switching this to unordered_map (wasnt present in VC80)
	std::unordered_map<int, OpenFileInfo> open_files; // 0x210

	HMODULE hMainModule; // 0x21C

	int opened_files_ident; // 0x238
	char disallow_uppercase_filename; // 0x23C

	error_handler_t error_handler; // 0x268
	HWND hwnd; // 0x026C
	bool use_pk2_container;
	char container_filename[MAX_PATH];
	char container_root_prefix[64];
	char container_password[128];
	std::unique_ptr<PK2Reader> pk2_reader;

public:
	virtual int Mode();
	virtual int ConfigSet(int a2, int a3);
	virtual int ConfigGet(int a2, int a3);


	//
	// Container
	//

	virtual int CreateContainer(const char *filename,  const char *password);
	virtual int OpenContainer(const char *filename, const char* password, int mode);
	virtual int CloseContainer(void);

	virtual int IsOpen(void); //

	virtual int CloseAllFiles(void); //Similar in both implementations

	virtual HMODULE MainModuleHandle(void);

	virtual int Function_9(int);

	//
	// Files
	//

	virtual int Open(const char *filename, int access, int length);
	virtual int Open(CJArchiveFm* fm, const char *filename, int access, int length);

	virtual int Function_12(void); //return -1
	virtual int Function_13(void); //return 0

	virtual int Create(const char* filename, int length); //
	virtual int Create(CJArchiveFm * fm, const char * filename, int length); //

	virtual int Delete (const char *filename);
	virtual int Close(int hFile); //

	virtual int Read(int hFile, char* lpBuffer, int nNumberOfBytesToWrite, unsigned long *lpNumberOfBytesWritten);
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
	virtual int GetVirtualPath(char *dest);


	virtual search_handle_t* FindFirstFile(search_handle_t* pSearchHandle, const char* pSearchPattern, search_result_t* pSearchResult);
	virtual bool FindNextFile(search_handle_t* pSearchHandle, search_result_t* pSearchResult);
	virtual bool FindClose(search_handle_t* pSearchHandle);

	//
	// File Information
	//

	virtual int FileNameFromHandle(int hFile, char* dst, size_t count);
	virtual int GetFileSize(int hFile, LPDWORD lpFileSizeHigh); //
	virtual BOOL GetFileTime(int hFile, LPFILETIME lpCreationTime, LPFILETIME lpLastWriteTime); //
	virtual BOOL SetFileTime(int hFile, LPFILETIME lpCreationTime, LPFILETIME lpLastWriteTime); //

	virtual int Seek(int hFile, LONG lDistanceToMove, DWORD dwMoveMethod); //


	virtual HWND GetHwnd(void);
	virtual void SetHwnd(HWND);
	virtual void RegisterErrorHandler(error_handler_t callback);


	virtual int ImportDirectory(const char *srcdir, const char *dstdir, const char *directory_name, bool create_target_dir);
	virtual int ImportFile(const char *srcdir, const char *dstdir, const char *filename, bool create_target_dir);
	virtual int ExportDirectory(const char *srcdir, const char *dstdir, const char *directory_name, bool create_target_dir);
	virtual int ExportFile(const char *srcdir, const char *dstdir, const char *filename, bool create_target_dir); // create_target_dir is unused

	virtual int FileExists(char* name, int flags); //


	virtual int UpdateCurrentDirectory(void); //
	virtual int Function_50(int); // returns zero in both impl.


	virtual int Lock(int); //
	virtual int Unlock(); //

public:
	CWFileManager();

private:
	int GetNextFreeIndex();
	bool CreateDirectoryRecursive(const char* filename);
	bool ChangeDirTo(const char*);
	std::string NormalizePath(const char* path) const;
	std::string BuildPhysicalPath(const char* filename) const;
	std::string BuildPk2Path(const char* filename) const;
	bool PopulatePk2SearchResults(const char* pattern, std::vector<search_result_t>& outResults);
	bool WildcardMatch(const char* pattern, const char* candidate) const;

};
