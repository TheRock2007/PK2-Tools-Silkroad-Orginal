#pragma once

#include "IFileManager.h"
#include "debug.h"

template <class T>
class LoggingFileManager : public IFileManager {

private:
	T fm;

public:

	int Mode();
	int ConfigSet(int,int);
	int ConfigGet(int,int);

	int CreateContainer(const char *filename,  const char *password);
	int OpenContainer(const char *filename, const char* password, int mode);
	int CloseContainer();
	int IsOpen();

	int CloseAllFiles(void);


	HMODULE MainModuleHandle(void);


	int Function_9(int);
	int Open(const char *filename, int access, int unknown);
	int Open(CJArchiveFm* fm, const char *filename, int access, int unknown);

	
	int Function_12(void);
	int Function_13(void);

	int Create(const char* filename, int unknown);
	int Create(CJArchiveFm * fm, const char * filename, int unknown);
	

	int Delete(const char *filename);
	int Close(int hFile);

	int Read(int hFile, char* lpBuffer, int nNumberOfBytesToRead, unsigned long *lpNumberOfBytesRead);
	int Write(int hFile, const char* lpBuffer, int nNumberOfBytesToWrite, unsigned long *lpNumberOfBytesWritten);


	char *CmdLinePath(void);
	char *CmdLineExe(void);

	virtual __int64* GetDirectoryPosition(__int64* pPosition) override;
	virtual bool SetDirectoryPosition(__int64 position) override;


	int DirectoryCreate(const char* name);
	int DirectoryRemove(const char* name);


	bool ResetDirectory(void);
	bool ChangeDirectory(const char* dirname);
	int GetDirectoryName(size_t buffersize, char* Dst);

	
	int SetVirtualPath(const char *path);
	int GetVirtualPath(char *dest);

	search_handle_t* FindFirstFile(search_handle_t* pSearchHandle, const char* pattern, search_result_t* pSearchEntry);
	bool FindNextFile(search_handle_t* pSearchHandle, search_result_t* pSearchEntry);
	bool FindClose(search_handle_t* pSearchHandle);



	int FileNameFromHandle(int hFile, char* dst, size_t count);
	int GetFileSize(int hFile, LPDWORD lpFileSizeHigh);
	BOOL GetFileTime(int hFile, LPFILETIME lpCreationTime, LPFILETIME lpLastWriteTime);
	BOOL SetFileTime(int hFile, LPFILETIME lpCreationTime, LPFILETIME lpLastWriteTime);
	int Seek(int hFile, LONG lDistanceToMove, DWORD dwMoveMethod);

	HWND GetHwnd(void);
	void SetHwnd(HWND);

	void RegisterErrorHandler(error_handler_t callback);

	int ImportDirectory(const char *srcdir, const char *dstdir, const char *directory_name, bool create_target_dir);
	int ImportFile(const char *srcdir, const char *dstdir, const char *filename, bool create_target_dir);
	int ExportDirectory(const char *srcdir, const char *dstdir, const char *directory_name, bool create_target_dir);
	int ExportFile(const char *srcdir, const char *dstdir, const char *filename, bool create_target_dir);

	int FileExists(char* name, int flags);

	int UpdateCurrentDirectory(void);
	 int Function_50(int);

	int Lock(int);
	int Unlock();

};

template<class T>
int LoggingFileManager<T>::Mode() {
	debug(DEBUG_OTHER, __FUNCTION__"Mode()");
	return fm.Mode();
}
template<class T>

int LoggingFileManager<T>::ConfigSet(int a1, int a2) {
	debug(DEBUG_OTHER, __FUNCTION__"(%d, %d)", a1, a2);
	return fm.ConfigSet(a1, a2);
}
template<class T>
int LoggingFileManager<T>::ConfigGet(int a1, int a2) {
	debug(DEBUG_OTHER, __FUNCTION__"(%d, %d)", a1, a2);
	return fm.ConfigGet(a1, a2);
}

template<class T>
int LoggingFileManager<T>::CreateContainer(const char *filename,  const char *password) {
	debug(DEBUG_CONTAINER, __FUNCTION__"(\"%s\", \"%s\")", filename, password);
	return fm.CreateContainer(filename, password);
}

template<class T>
int LoggingFileManager<T>::OpenContainer(const char *filename, const char* password, int mode) {
	debug(DEBUG_CONTAINER, __FUNCTION__"(\"%s\", \"%s\", 0x%08x)", filename, password, mode);
	return fm.OpenContainer(filename, password, mode);
}

template<class T>
int LoggingFileManager<T>::CloseContainer() {
	debug(DEBUG_CONTAINER, __FUNCTION__"()");
	return fm.CloseContainer();
}

template<class T>
int LoggingFileManager<T>::IsOpen() {
	debug(DEBUG_CONTAINER, __FUNCTION__"()");
	return fm.IsOpen();
}

template<class T>
int LoggingFileManager<T>::CloseAllFiles() {
	debug(DEBUG_UNKNOWN, __FUNCTION__"()");
	return fm.CloseAllFiles();
}

template<class T>
HMODULE LoggingFileManager<T>::MainModuleHandle(void) {
	debug(DEBUG_OTHER, __FUNCTION__"()");
	return fm.MainModuleHandle();
}

template<class T>
int LoggingFileManager<T>::Function_9(int a) {
	debug(DEBUG_UNKNOWN, __FUNCTION__"(%d)", a);
	return fm.Function_9(a);
}

template<class T>
int LoggingFileManager<T>::Open(const char *filename, int access, int unknown) {
	debug(DEBUG_FILE, __FUNCTION__"(\"%s\", 0x%08x, 0x%08x)", filename, access, unknown);
	return fm.Open(filename, access, unknown);
}

template<class T>
int LoggingFileManager<T>::Open(CJArchiveFm *pfm, const char *filename, int access, int unknown) {
  debug(DEBUG_FILE, __FUNCTION__"(0x%08x, \"%s\", 0x%08x, 0x%08x)", pfm, filename, access, unknown);
  return fm.Open(pfm, filename, access, unknown);
}


template<class T>
int LoggingFileManager<T>::Function_12(void)  {
	debug(DEBUG_UNKNOWN, __FUNCTION__"()");
	return fm.Function_12();
}


template<class T>
int LoggingFileManager<T>::Function_13(void)  {
	debug(DEBUG_UNKNOWN, __FUNCTION__"()");
	return fm.Function_13();
}


template<class T>
int LoggingFileManager<T>::Create(CJArchiveFm *pfm, const char *filename, int unknown)  {
	debug(DEBUG_UNKNOWN, __FUNCTION__"(%d, %d, %d)", pfm, filename, unknown);
	return fm.Create(pfm, filename, unknown);
}

template<class T>
int LoggingFileManager<T>::Create(const char *filename, int unknown) {

	debug(DEBUG_UNKNOWN,__FUNCTION__ "(\"%s\", %08x)", filename, unknown);
	return fm.Create(filename, unknown);
}

template<class T>
int LoggingFileManager<T>::Delete(const char *filename) {

	debug(DEBUG_FILE, __FUNCTION__"(\"%s\")", filename);
	return fm.Delete(filename);
}

template<class T>
int LoggingFileManager<T>::Close(int hFile) {

	debug(DEBUG_FILE, __FUNCTION__"(%08x)", hFile);
	return fm.Close(hFile);
}



template<class T>
int LoggingFileManager<T>::Read(int hFile, char *lpBuffer, int nNumberOfBytesToRead, unsigned long *lpNumberOfBytesRead) {

	debug(DEBUG_IO, __FUNCTION__"(%p, %p, %d, %p)", hFile, lpBuffer, nNumberOfBytesToRead, lpNumberOfBytesRead);

	return fm.Read(hFile, lpBuffer, nNumberOfBytesToRead, lpNumberOfBytesRead);
}

template<class T>
int LoggingFileManager<T>::Write(int hFile, const char *lpBuffer, int nNumberOfBytesToWrite, unsigned long *lpNumberOfBytesWritten) {
	debug(DEBUG_IO, __FUNCTION__"(%p, %p, %d, %p)", hFile, lpBuffer, nNumberOfBytesToWrite, lpNumberOfBytesWritten);

	return fm.Write(hFile, lpBuffer, nNumberOfBytesToWrite, lpNumberOfBytesWritten);
}

template<class T>
char *LoggingFileManager<T>::CmdLinePath(void){
	debug(DEBUG_OTHER,__FUNCTION__"() = \"Silkroad.exe\"");
	return fm.CmdLinePath();
}

template<class T>
char *LoggingFileManager<T>::CmdLineExe(void) {
	debug(DEBUG_OTHER, __FUNCTION__"() = \"/22 0 0\"");
	return fm.CmdLineExe();
}


template<class T>
__int64 *LoggingFileManager<T>::GetDirectoryPosition(__int64 *pPosition) {

	debug(DEBUG_OTHER, __FUNCTION__"(0x%x)", pPosition);
	return fm.GetDirectoryPosition(pPosition);
}

template<class T>
bool LoggingFileManager<T>::SetDirectoryPosition(__int64 position) {
	debug(DEBUG_OTHER, __FUNCTION__"(0x%08x)", position);
	return fm.SetDirectoryPosition(position);
}


template<class T>
int LoggingFileManager<T>::DirectoryCreate(const char* name) {
	debug(DEBUG_DIRECTORY, __FUNCTION__"(\"%s\")", name);
	return fm.DirectoryCreate(name);
}

template<class T>
int LoggingFileManager<T>::DirectoryRemove(const char* name) {
	debug(DEBUG_DIRECTORY, __FUNCTION__"(\"%s\")", name);
	return fm.DirectoryRemove(name);
}

template<class T>
bool LoggingFileManager<T>::ResetDirectory(void) {
	debug(DEBUG_DIRECTORY, __FUNCTION__"()");
	return fm.ResetDirectory();
}


template<class T>
bool LoggingFileManager<T>::ChangeDirectory(const char* dirname) {
	debug(DEBUG_DIRECTORY, __FUNCTION__"(\"%s\")", dirname);
	return fm.ChangeDirectory(dirname);
}

template<class T>
int LoggingFileManager<T>::GetDirectoryName(size_t buffersize, char* Dst) {
	debug(DEBUG_DIRECTORY, __FUNCTION__"(%d, %08x)", buffersize, Dst);
	return fm.GetDirectoryName(buffersize, Dst);
}


template<class T>
int LoggingFileManager<T>::SetVirtualPath(const char *path) {
	debug(DEBUG_DIRECTORY, __FUNCTION__"(%08x)", path);
	return fm.SetVirtualPath(path);
}

template<class T>
int LoggingFileManager<T>::GetVirtualPath(char* dest) {
	debug(DEBUG_DIRECTORY, __FUNCTION__"(%08x)", dest);
	return fm.GetVirtualPath(dest);
}

template<class T>
search_handle_t* LoggingFileManager<T>::FindFirstFile(search_handle_t* pSearchHandle, const char* pSearchPatern, search_result_t* pSearchResult) {
	debug(DEBUG_SEARCH, __FUNCTION__"(%08x, \"%s\", %08x)", pSearchHandle, pSearchPatern, pSearchResult);
	return fm.FindFirstFileA(pSearchHandle, pSearchPatern, pSearchResult);
}

template<class T>
bool LoggingFileManager<T>::FindNextFile(search_handle_t* pSearchHandle, search_result_t* pSearchResult) {
	debug(DEBUG_SEARCH, __FUNCTION__"(%08x, %08x)", pSearchHandle, pSearchResult);
	return fm.FindNextFileA(pSearchHandle, pSearchResult);
}

template<class T>
bool LoggingFileManager<T>::FindClose(search_handle_t* pSearchHandle) {
	debug(DEBUG_SEARCH, __FUNCTION__"(%08x)", pSearchHandle);
	return fm.FindClose(pSearchHandle);
}

template<class T>
int LoggingFileManager<T>::FileNameFromHandle(int hFile, char* dst, size_t count) {
	debug(DEBUG_FILE, __FUNCTION__"(%08x, %08x, %08x)", hFile, dst, count);
	return fm.FileNameFromHandle(hFile, dst, count);
}

template<class T>
int LoggingFileManager<T>::GetFileSize(int hFile, LPDWORD lpFileSizeHigh) {
	debug(DEBUG_FILE, __FUNCTION__"(%d, %08x)", hFile, lpFileSizeHigh);
	return fm.GetFileSize(hFile, lpFileSizeHigh);
}


template<class T>
BOOL LoggingFileManager<T>::GetFileTime(int hFile, LPFILETIME lpCreationTime, LPFILETIME lpLastWriteTime) {
	debug(DEBUG_FILE, __FUNCTION__"(%08x, %08x, %08x)", hFile, lpCreationTime, lpLastWriteTime);
	return fm.GetFileTime(hFile, lpCreationTime, lpLastWriteTime);
}

template<class T>
BOOL LoggingFileManager<T>::SetFileTime(int hFile, LPFILETIME lpCreationTime, LPFILETIME lpLastWriteTime) {
	debug(DEBUG_FILE, __FUNCTION__"(%08x, %08x, %08x)", hFile, lpCreationTime, lpLastWriteTime);
	return fm.SetFileTime(hFile, lpCreationTime, lpLastWriteTime);
}

template<class T>
int LoggingFileManager<T>::Seek(int hFile, LONG lDistanceToMove, DWORD dwMoveMethod) {
	debug(DEBUG_IO, __FUNCTION__"(%08x, %d, %d)", hFile, lDistanceToMove, dwMoveMethod);
	return fm.Seek(hFile, lDistanceToMove, dwMoveMethod);
}


template<class T>
HWND LoggingFileManager<T>::GetHwnd(void) {
	debug(DEBUG_OTHER, __FUNCTION__"()");
	return fm.GetHwnd();
}

template<class T>
void LoggingFileManager<T>::SetHwnd(HWND nhwnd) {
	debug(DEBUG_OTHER, __FUNCTION__"(0x%08x)", nhwnd);
	return fm.SetHwnd(nhwnd);
}


template<class T>
void LoggingFileManager<T>::RegisterErrorHandler(error_handler_t callback) {
	debug(DEBUG_OTHER, __FUNCTION__"(0x%08x)", callback);
	return fm.RegisterErrorHandler(callback);
}


template<class T>
int LoggingFileManager<T>::ImportDirectory(const char *srcdir, const char *dstdir, const char *directory_name, bool create_target_dir) {

	debug(DEBUG_UNKNOWN, __FUNCTION__"(\"%s\", \"%s\", \"%s\", 0x%08x)", srcdir, dstdir, directory_name, create_target_dir);
	return fm.ImportDirectory(srcdir, dstdir, directory_name, create_target_dir);
}

template<class T>
int LoggingFileManager<T>::ImportFile(const char *srcdir, const char *dstdir, const char *filename, bool create_target_dir) {

	debug(DEBUG_FILE, __FUNCTION__"(\"%s\", \"%s\", \"%s\", 0x%08x)", srcdir, dstdir, filename, create_target_dir);
	return fm.ImportFile(srcdir, dstdir, filename, create_target_dir);
}

template<class T>
int LoggingFileManager<T>::ExportDirectory(const char *srcdir, const char *dstdir, const char *directory_name, bool create_target_dir) {

	debug(DEBUG_UNKNOWN, __FUNCTION__"(\"%s\", \"%s\", \"%s\", 0x%08x)",srcdir, dstdir, directory_name, create_target_dir);
	return fm.ExportDirectory(srcdir, dstdir, directory_name, create_target_dir);
}

template<class T>
int LoggingFileManager<T>::ExportFile(const char *srcdir, const char *dstdir, const char *filename, bool create_target_dir) {

	debug(DEBUG_UNKNOWN, __FUNCTION__"(\"%s\", \"%s\", \"%s\", 0x%08x)", srcdir, dstdir, filename, create_target_dir);
	return fm.ExportFile(srcdir, dstdir, filename, create_target_dir);
}


template<class T>
int LoggingFileManager<T>::FileExists(char* name, int flags) {
	debug(DEBUG_FILE, __FUNCTION__"(\"%s\", %08x)", name, flags);
	return fm.FileExists(name, flags);
}


template<class T>
int LoggingFileManager<T>::UpdateCurrentDirectory(void) {
	debug(DEBUG_UNKNOWN, __FUNCTION__"()");
	return fm.UpdateCurrentDirectory();
}

template<class T>
int LoggingFileManager<T>::Function_50(int a) {
	debug(DEBUG_UNKNOWN, __FUNCTION__"(0x%08x)", a);
	return fm.Function_50(a);
}

template<class T>
int LoggingFileManager<T>::Lock(int a) {
	debug(DEBUG_UNKNOWN, __FUNCTION__"(0x%08x)", a);
	return fm.Lock(a);
}

template<class T>
int LoggingFileManager<T>::Unlock() {
	debug(DEBUG_UNKNOWN, __FUNCTION__"()");
	return fm.Unlock();
}
