#include "CWFileManager.h"
#include "GFXInfo.h"
#include <limits>
#include <string>
#include <cstring>
#include <algorithm>
#include <vector>
#include <Shlwapi.h>
#pragma comment(lib, "shlwapi.lib")

#include "commandine.h"
#include "PK2PayloadCrypto.h"

// Shitty implemented internal funcs 
int check_file_exists(char *lpFileName, int arg);
int check_file_attributes(char *lpFileName, int arg);

int PopulateResultFromFindData(search_result_t *pSearchResult);



CWFileManager::CWFileManager()
	: opened_files_ident(0), use_pk2_container(false)
{
	initial_path[0] = 0;
	current_dir[0] = 0;
	container_filename[0] = 0;
	container_root_prefix[0] = 0;
	container_password[0] = 0;
	container_info = 0;
	hMainModule = GetModuleHandleA(0);
	disallow_uppercase_filename = 0;
	error_handler = 0;
	hwnd = 0;
	populate_cmdline();
}

std::string CWFileManager::NormalizePath(const char* path) const {
	std::string s = path ? path : "";
	std::replace(s.begin(), s.end(), '/', '\\');
	while (!s.empty() && s[0] == '\\') s.erase(s.begin());
	std::transform(s.begin(), s.end(), s.begin(), [](unsigned char c) { return (char)tolower(c); });
	return s;
}

std::string CWFileManager::BuildPhysicalPath(const char* filename) const {
	std::string p = current_dir;
	p += filename ? filename : "";
	return p;
}

std::string CWFileManager::BuildPk2Path(const char* filename) const {
	std::string rel = NormalizePath(current_dir);
	rel += NormalizePath(filename);
	while (!rel.empty() && rel[0] == '\\') rel.erase(0, 1);
	if (!container_root_prefix[0]) return rel;
	std::string prefix = NormalizePath(container_root_prefix);
	if (rel.rfind(prefix + "\\", 0) == 0 || rel == prefix)
		return rel;
	if (rel.empty()) return prefix;
	return prefix + "\\" + rel;
}

bool CWFileManager::WildcardMatch(const char* pattern, const char* candidate) const {
	return PathMatchSpecA(candidate, pattern);
}

int CWFileManager::Mode() {
	return 2;
}

int CWFileManager::ConfigSet(int key, int value) {
	if (key == 2) 
		this->disallow_uppercase_filename = value != 0;

	return 0;
}

int CWFileManager::ConfigGet(int a2, int a3) {
	return 0;
}

int CWFileManager::CreateContainer(const char *filename, const char *password) {
	return OpenContainer(filename, password, 0);
}

int CWFileManager::OpenContainer(const char *filename, const char* password, int mode) {
	(void)mode;
	if (container_info) {
		CloseContainer();
	}
	this->container_info = open_container_info_write(filename ? filename : "");
	use_pk2_container = false;
	if (filename) strcpy_s(container_filename, sizeof(container_filename), filename);
	if (password) strcpy_s(container_password, sizeof(container_password), password);
	container_root_prefix[0] = 0;
	pk2_reader.reset();
	if (filename) {
		const char* ext = strrchr(filename, '.');
		if (ext && _stricmp(ext, ".pk2") == 0) {
			use_pk2_container = true;
			pk2_reader = std::make_unique<PK2Reader>();
			if (password && *password) {
				pk2_reader->SetDecryptionKey((char*)password, (uint8_t)strlen(password));
			}
			if (!pk2_reader->Open(filename)) {
				std::string err = pk2_reader->GetError();
				SHOW_ERROR(err.c_str(), "Open PK2 failed");
				pk2_reader.reset();
				use_pk2_container = false;
				return 0;
			}
			char fname[_MAX_FNAME] = {0};
			_splitpath_s(filename, 0, 0, 0, 0, fname, sizeof(fname), 0, 0);
			for (size_t i = 0; fname[i]; ++i) fname[i] = (char)tolower((unsigned char)fname[i]);
			strcpy_s(container_root_prefix, sizeof(container_root_prefix), fname);
			PK2PayloadCrypto_Initialize();
		}
	}
	return 1;
}

int CWFileManager::IsOpen(void) {
	if (use_pk2_container) {
		return pk2_reader ? 1 : 0;
	}
	return 1;
}

int CWFileManager::CloseAllFiles(void) {
	std::vector<int> ids;
	for (auto it = open_files.begin(); it != open_files.end(); ++it) ids.push_back(it->first);
	for (size_t i = 0; i < ids.size(); ++i) this->Close(ids[i]);
	return 1;
}

HMODULE CWFileManager::MainModuleHandle(void) {
	return this->hMainModule;
}

int CWFileManager::Function_9(int a2) {
	// harcoded, always -1
	return -1;
}

int CWFileManager::Delete(const char *filename) {
	return 0; // Deleting files is not supported by original CWFileManager!
}

int CWFileManager::Open(CJArchiveFm* fm, const char *filename, int access, int length) {
	fm->field_15 = 1;
	fm->pFileManager = this;
	fm->hFile = Open(filename, access, length);
	fm->is_write_mode = access == GENERIC_WRITE;

	if (fm->is_write_mode) {
		fm->pCurrent = fm->buffer;
	} else {
		fm->pCurrent = fm->pEnd;
	}

	return fm->hFile != -1;
}

int CWFileManager::Open(const char *filename, int dwDesiredAccess, int length) {
	(void)length;
	if (use_pk2_container) {
		if (dwDesiredAccess == GENERIC_WRITE) {
			SHOW_ERROR("Write mode is not supported for PK2 containers", "PK2");
			return -1;
		}
		if (!pk2_reader) return -1;
		std::string pk2Path = BuildPk2Path(filename);
		PK2Entry entry = {};
		if (!pk2_reader->GetEntry(pk2Path.c_str(), entry)) {
			std::string normalized = NormalizePath(current_dir) + NormalizePath(filename);
			if (normalized != pk2Path && pk2_reader->GetEntry(normalized.c_str(), entry)) {
				pk2Path = normalized;
			} else {
				char error_buffer[512];
				sprintf_s(error_buffer, "FM PK2 File(%s)\n", pk2Path.c_str());
				OutputDebugStringA(error_buffer);
				return -1;
			}
		}
		std::vector<uint8_t> buffer;
		if (!pk2_reader->ExtractToMemory(entry, buffer)) return -1;
		if (!buffer.empty() && pk2_reader->HasPayloadEncryption() && PK2PayloadCrypto_ShouldDecrypt(pk2Path)) {
			PK2PayloadCrypto_DecryptBufferForFile(static_cast<uint64_t>(entry.position), entry.size, buffer.data(), buffer.size());
		}
		int findex = GetNextFreeIndex();
		auto &finfo = this->open_files[findex];
		finfo.isPk2File = true;
		finfo.hFile = INVALID_HANDLE_VALUE;
		finfo.data = std::move(buffer);
		finfo.cursor = 0;
		strcpy_s(finfo.filename, sizeof(finfo.filename), pk2Path.c_str());
		ULARGE_INTEGER u;
		u.QuadPart = entry.createTime;
		finfo.creationTime.dwLowDateTime = u.LowPart;
		finfo.creationTime.dwHighDateTime = u.HighPart;
		u.QuadPart = entry.modifyTime;
		finfo.lastWriteTime.dwLowDateTime = u.LowPart;
		finfo.lastWriteTime.dwHighDateTime = u.HighPart;
		return findex;
	}

	char full_filename[520];
	strcpy_s(full_filename, sizeof(full_filename), this->current_dir);
	strcat_s(full_filename, sizeof(full_filename), filename);

	if (this->disallow_uppercase_filename) {
		for (const char *f = filename; *f; ++f) {
			if (*f >= 'A' && *f <= 'Z') {
				char output[260];
				sprintf_s(output, "FM File(%s)\n", full_filename);
				OutputDebugStringA(output);
				return -1;
			}
		}
	}

	for (char *f = full_filename; *f; ++f) {
		if (*f == '\\' && *(f + 1) == '\\') {
			char output[260];
			sprintf_s(output, "FM File(%s)\n", full_filename);
			OutputDebugStringA(output);
			return -1;
		}
	}

	DWORD dwShareMode = 0;
	DWORD dwCreationDistribution = (dwDesiredAccess == GENERIC_WRITE) ? CREATE_ALWAYS : OPEN_EXISTING;
	if (dwDesiredAccess == GENERIC_READ) dwShareMode = FILE_SHARE_READ;
	HANDLE hFile = CreateFileA(full_filename, dwDesiredAccess, dwShareMode, 0, dwCreationDistribution, FILE_ATTRIBUTE_ARCHIVE, 0);
	if (hFile == INVALID_HANDLE_VALUE) {
		char error_buffer[256];
		sprintf_s(error_buffer, "FM File(%s)\n", full_filename);
		OutputDebugStringA(error_buffer);
		return -1;
	}

	int findex = GetNextFreeIndex();
	auto &finfo = this->open_files[findex];
	finfo.isPk2File = false;
	finfo.hFile = hFile;
	strcpy_s(finfo.filename, sizeof(finfo.filename), filename);
	return findex;
}

int CWFileManager::GetNextFreeIndex() {


	std::unordered_map<int, OpenFileInfo>::iterator it;

	// Note the potential lockup once you hit INT_MAX+1 open files
	// @TODO: Doublecheck and if true, prevent this shit
	do {
		if (++this->opened_files_ident >= INT_MAX)
			this->opened_files_ident = 1;

		it = open_files.find(this->opened_files_ident);

	} while (it != open_files.end());

	OpenFileInfo info;
	info.field_0 = this->opened_files_ident; // guessed

	// Copy this thing into the map
	// Also guessed, not sure what this looked like before.
	this->open_files[this->opened_files_ident] = info;

	return this->opened_files_ident;
}

int CWFileManager::Function_12(void) {
	return -1;
}

int CWFileManager::Function_13(void) {
	return 0;
}

int CWFileManager::Create(const char* filename, int length) {
	if (use_pk2_container) {
		SHOW_ERROR("Create is not supported for PK2 containers", "PK2");
		return -1;
	}
	if (this->CreateDirectoryRecursive(filename)) {
		return this->Open(filename, GENERIC_WRITE, length);
	}
	return -1;
}

int CWFileManager::Create(CJArchiveFm * fm, const char * filename, int length) {
	fm->field_15 = 1;
	fm->is_write_mode = 1;
	fm->pFileManager = this;

	fm->hFile = this->Create(filename, length);;

	if (fm->is_write_mode) {
		fm->pCurrent = fm->buffer;
	} else {
		fm->pCurrent = fm->pEnd;
	}

	return fm->hFile != -1;
}

// This method has a bug, see #16
bool CWFileManager::CreateDirectoryRecursive(const char* filename) {
	char buffer[512] = {0};
	char *token;

	char previous_dir[512] = {0};

	GetCurrentDirectory(sizeof(previous_dir), previous_dir);

	// Original path for finding the last path separator
	std::string original(filename ? filename : "");
	std::string::size_type pos = original.find_last_of('\\');

	if (pos == std::string::npos || pos >= sizeof(buffer)) {
		return true;
	}

	// Copy path without filename to new buffer and terminate it
	memcpy(buffer, original.c_str(), pos);
	buffer[pos] = '\0';

	// Start tokenizing
	token = strtok(buffer, "\\");

	// Growing path variable
	std::string fullpath;

	while (token != NULL) {
		// Add current folder to path
		fullpath += token;

		// Create entire path
		CreateDirectory(fullpath.c_str(), 0);

		// Append separator
		fullpath += "\\";

		// Get next token
		token = strtok(NULL, "\\");
	}

	// Check if directories were created
	int result = 1;
	if (!SetCurrentDirectory(fullpath.c_str())) {
		result = 0;
	}

	// Reset directory to previous
	SetCurrentDirectory(previous_dir);

	return result;
}

int CWFileManager::Close(int hFile) {
	auto file = this->open_files.find(hFile);
	if (file == this->open_files.end()) {
		SHOW_ERROR("File Handle is invalid", "Error during Close");
		return 0;
	}
	BOOL result = TRUE;
	if (!file->second.isPk2File && file->second.hFile != INVALID_HANDLE_VALUE) {
		result = ::CloseHandle(file->second.hFile);
	}
	this->open_files.erase(file);
	if (this->container_info) this->container_info->number_of_open_files--;
	return result;
}


int CWFileManager::Read(int hFile, char* lpBuffer, int nNumberOfBytesToRead, unsigned long *lpNumberOfBytesRead) {
	auto file = this->open_files.find(hFile);
	if (file == this->open_files.end()) {
		SHOW_ERROR("File Handle is invalid", "Error during Read");
		return 0;
	}
	if (file->second.isPk2File) {
		if (lpNumberOfBytesRead) *lpNumberOfBytesRead = 0;
		if (nNumberOfBytesToRead <= 0) return 1;
		size_t remaining = file->second.data.size() - file->second.cursor;
		size_t toRead = std::min<size_t>((size_t)nNumberOfBytesToRead, remaining);
		if (toRead > 0) {
			memcpy(lpBuffer, file->second.data.data() + file->second.cursor, toRead);
			file->second.cursor += toRead;
		}
		if (lpNumberOfBytesRead) *lpNumberOfBytesRead = (unsigned long)toRead;
		if (this->container_info) this->container_info->number_of_bytes_processed_total += (int)toRead;
		return 1;
	}
	BOOL result = ::ReadFile(file->second.hFile, lpBuffer, nNumberOfBytesToRead, lpNumberOfBytesRead, 0);
	if (this->container_info && lpNumberOfBytesRead) this->container_info->number_of_bytes_processed_total += *lpNumberOfBytesRead;
	return result;
}

int CWFileManager::Write(int hFile, const char* lpBuffer, int nNumberOfBytesToWrite, unsigned long *lpNumberOfBytesWritten) {
	auto file = this->open_files.find(hFile);
	if (file == this->open_files.end()) {
		SHOW_ERROR("File Handle is invalid", "Error during Write");
		return 0;
	}
	if (file->second.isPk2File) {
		SHOW_ERROR("Write is not supported for PK2 containers", "PK2");
		if (lpNumberOfBytesWritten) *lpNumberOfBytesWritten = 0;
		return 0;
	}
	BOOL result = ::WriteFile(file->second.hFile, lpBuffer, nNumberOfBytesToWrite, lpNumberOfBytesWritten, 0);
	if (this->container_info && lpNumberOfBytesWritten) this->container_info->number_of_bytes_processed_total += *lpNumberOfBytesWritten;
	return result;
}


char* CWFileManager::CmdLinePath(void) {
	return get_cmdline_path();
}

char* CWFileManager::CmdLineExe(void) {
	return get_cmdline_exe();
}

__int64 * CWFileManager::GetDirectoryPosition(__int64 * pPosition)
{
	*pPosition = 0;
	return pPosition;
}

bool CWFileManager::SetDirectoryPosition(__int64 position)
{
	return false;
}


//
// File Information
//

int CWFileManager::FileNameFromHandle(int hFile, char* dst, size_t count) {
	auto file = this->open_files.find(hFile);
	if (file == this->open_files.end()) {
		SHOW_ERROR("File Handle is invalid", "Error during FileNameFromHandle");
		return 0;
	}
	size_t len = strlen(file->second.filename);
	if (len >= count) return 0;
	strcpy_s(dst, count, file->second.filename);
	return 1;
}

int CWFileManager::GetFileSize(int hFile, LPDWORD lpFileSizeHigh) {
	auto file = this->open_files.find(hFile);
	if (file == this->open_files.end()) { SHOW_ERROR("File Handle is invalid", "Error during GetFileSize"); return 0; }
	if (file->second.isPk2File) { if (lpFileSizeHigh) *lpFileSizeHigh = 0; return (DWORD)file->second.data.size(); }
	return ::GetFileSize(file->second.hFile, lpFileSizeHigh);
}

BOOL CWFileManager::GetFileTime(int hFile, LPFILETIME lpCreationTime, LPFILETIME lpLastWriteTime) {
	auto file = this->open_files.find(hFile);
	if (file == this->open_files.end()) { SHOW_ERROR("File Handle is invalid", "Error during GetFileTime"); return 0; }
	if (file->second.isPk2File) { if (lpCreationTime) *lpCreationTime = file->second.creationTime; if (lpLastWriteTime) *lpLastWriteTime = file->second.lastWriteTime; return 1; }
	return ::GetFileTime(file->second.hFile, lpCreationTime, 0, lpLastWriteTime);
}

BOOL CWFileManager::SetFileTime(int hFile, LPFILETIME lpCreationTime, LPFILETIME lpLastWriteTime) {
	auto file = this->open_files.find(hFile);
	if (file == this->open_files.end()) { SHOW_ERROR("File Handle is invalid", "Error during SetFileTime"); return 0; }
	if (file->second.isPk2File) { if (lpCreationTime) file->second.creationTime = *lpCreationTime; if (lpLastWriteTime) file->second.lastWriteTime = *lpLastWriteTime; return 1; }
	return ::SetFileTime(file->second.hFile, lpCreationTime, 0, lpLastWriteTime);
}

int CWFileManager::Seek(int hFile, LONG lDistanceToMove, DWORD dwMoveMethod) {
	auto file = this->open_files.find(hFile);
	if (file == this->open_files.end()) { SHOW_ERROR("File Handle is invalid", "Error during Seek"); return 0; }
	if (file->second.isPk2File) {
		long long base = 0;
		if (dwMoveMethod == FILE_CURRENT) base = (long long)file->second.cursor;
		else if (dwMoveMethod == FILE_END) base = (long long)file->second.data.size();
		long long next = base + lDistanceToMove;
		if (next < 0) next = 0;
		if ((size_t)next > file->second.data.size()) next = (long long)file->second.data.size();
		file->second.cursor = (size_t)next;
		return (int)file->second.cursor;
	}
	return ::SetFilePointer(file->second.hFile, lDistanceToMove, 0, dwMoveMethod);
}

int CWFileManager::Lock(int a) {
	return 0;
}

int CWFileManager::Unlock() {
	return 0;
}

int CWFileManager::Function_50(int a) {
	return 0;
}

int CWFileManager::UpdateCurrentDirectory() {
	return 0;
}

int CWFileManager::FileExists(char *filename, int a3)
{
	if (use_pk2_container && pk2_reader) {
		PK2Entry entry = {};
		std::string pk2Path = BuildPk2Path(filename);
		if (pk2_reader->GetEntry(pk2Path.c_str(), entry)) return 0;
		std::string alt = NormalizePath(current_dir) + NormalizePath(filename);
		if (alt != pk2Path && pk2_reader->GetEntry(alt.c_str(), entry)) return 0;
		return -1;
	}
	char fullpath[520];
	strcpy_s(fullpath, 0x208u, this->current_dir);
	strcat_s(fullpath, 0x208u, filename);
	return check_file_exists(fullpath, a3);
}

int check_file_exists(char *lpFileName, int arg) {
	return -(check_file_attributes(lpFileName, arg) != 0);
}

int check_file_attributes(char *lpFileName, int arg) {
	DWORD attrib;
	DWORD err;

	if ( !lpFileName || arg & 0xFFFFFFF9 )
	{
		*__doserrno() = 0;
		*_errno() = 22;
		// _invalid_parameter(0, 0, 0, 0, 0);
		return 22;
	}
	attrib = GetFileAttributesA(lpFileName);
	if ( attrib == INVALID_FILE_ATTRIBUTES )
	{
		err = GetLastError();
		// _dosmaperr(err);
		return *_errno();
	}
	if ( !(attrib & FILE_ATTRIBUTE_DIRECTORY) && (attrib & 1) && (arg & 2) )
	{
		*__doserrno() = 5;
		*_errno() = 13;
		return *_errno();
	}
	return 0;
}

int CWFileManager::ImportDirectory(const char *srcdir, const char *dstdir, const char *directory_name, bool create_target_dir) {
	return 0;
}

int CWFileManager::ImportFile(const char *srcdir, const char *dstdir, const char *filename, bool create_target_dir){
	return 0;
}

int CWFileManager::ExportDirectory(const char *srcdir, const char *dstdir, const char *directory_name, bool create_target_dir){
	return 0;
}

int CWFileManager::ExportFile(const char *srcdir, const char *dstdir, const char *filename, bool create_target_dir){
	return 0;
}

void CWFileManager::RegisterErrorHandler(error_handler_t callback) {
	this->error_handler = callback;
}

HWND CWFileManager::GetHwnd(void) {
	return this->hwnd;
}

void CWFileManager::SetHwnd(HWND hwnd) {
	this->hwnd = hwnd;
}


int PopulateResultFromFindData(search_result_t *pSearchResult)
{
	strcpy_s(pSearchResult->Name, 89u, pSearchResult->find_data.cFileName);

	pSearchResult->CreationTime.dwLowDateTime = pSearchResult->find_data.ftCreationTime.dwLowDateTime;
	pSearchResult->CreationTime.dwHighDateTime = pSearchResult->find_data.ftCreationTime.dwHighDateTime;

	pSearchResult->LastWriteTime.dwLowDateTime = pSearchResult->find_data.ftLastWriteTime.dwLowDateTime;
	pSearchResult->LastWriteTime.dwHighDateTime = pSearchResult->find_data.ftLastWriteTime.dwHighDateTime;

	pSearchResult->Size =  pSearchResult->find_data.nFileSizeLow;
	pSearchResult->Type = (ENTRY_TYPE)(((pSearchResult->find_data.dwFileAttributes & 0x10) != 16) + 1);
	return 0;
}


bool CWFileManager::PopulatePk2SearchResults(const char* pattern, std::vector<search_result_t>& outResults)
{
	if (!pk2_reader) return false;
	std::string dir = NormalizePath(current_dir);
	if (!dir.empty() && dir.back() == '\\') dir.pop_back();
	std::string fullDir = BuildPk2Path("");
	if (!fullDir.empty() && fullDir.back() == '\\') fullDir.pop_back();
	PK2Entry parent = {};
	if (!fullDir.empty() && !pk2_reader->GetEntry(fullDir.c_str(), parent)) {
		// allow root fallback
		parent.type = 1;
		parent.position = 256;
	} else if (fullDir.empty()) {
		parent.type = 1;
		parent.position = 256;
	}
	std::list<PK2Entry> entries;
	if (!pk2_reader->GetEntries(parent, entries)) return false;
	for (std::list<PK2Entry>::iterator it = entries.begin(); it != entries.end(); ++it) {
		if (it->name[0] == '.' && (it->name[1] == 0 || (it->name[1] == '.' && it->name[2] == 0))) continue;
		if (!WildcardMatch(pattern, it->name)) continue;
		search_result_t r = {};
		strcpy_s(r.Name, sizeof(r.Name), it->name);
		r.Size = (int)it->size;
		r.Type = it->type == 1 ? ENTRY_TYPE_FOLDER : ENTRY_TYPE_FILE;
		ULARGE_INTEGER u;
		u.QuadPart = it->createTime; r.CreationTime.dwLowDateTime = u.LowPart; r.CreationTime.dwHighDateTime = u.HighPart;
		u.QuadPart = it->modifyTime; r.LastWriteTime.dwLowDateTime = u.LowPart; r.LastWriteTime.dwHighDateTime = u.HighPart;
		strcpy_s(r.find_data.cFileName, sizeof(r.find_data.cFileName), it->name);
		r.find_data.dwFileAttributes = (it->type == 1) ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_ARCHIVE;
		r.find_data.ftCreationTime = r.CreationTime;
		r.find_data.ftLastWriteTime = r.LastWriteTime;
		r.find_data.nFileSizeLow = it->size;
		outResults.push_back(r);
	}
	return true;
}

search_handle_t* CWFileManager::FindFirstFile(search_handle_t *pSearchHandle, const char *lpFileName, search_result_t *pSearchResult)
{
	if (use_pk2_container) {
		PK2SearchState* state = new PK2SearchState();
		PopulatePk2SearchResults(lpFileName, state->results);
		if (state->results.empty()) {
			pSearchHandle->Success = 0;
			pSearchHandle->hFind = (HANDLE)state;
			return pSearchHandle;
		}
		*pSearchResult = state->results[0];
		state->index = 1;
		pSearchHandle->Success = 1;
		pSearchHandle->hFind = (HANDLE)state;
		return pSearchHandle;
	}
	HANDLE hFind = ::FindFirstFileA(lpFileName, &pSearchResult->find_data);
	pSearchHandle->hFind = hFind;
	if (hFind == INVALID_HANDLE_VALUE) {
		pSearchHandle->Success = 0;
	} else {
		pSearchHandle->Success = 1;
		PopulateResultFromFindData(pSearchResult);
	}
	return pSearchHandle;
}

bool CWFileManager::FindNextFile(search_handle_t *pSearchHandle, search_result_t *pSearchResult)
{
	if (use_pk2_container) {
		PK2SearchState* state = (PK2SearchState*)pSearchHandle->hFind;
		if (!state || state->index >= state->results.size()) { pSearchHandle->Success = 0; return false; }
		*pSearchResult = state->results[state->index++];
		return true;
	}
	if (::FindNextFileA(pSearchHandle->hFind, &(pSearchResult->find_data))) {
		PopulateResultFromFindData(pSearchResult);
		return true;
	}
	pSearchHandle->Success = 0;
	return false;
}

bool CWFileManager::FindClose(search_handle_t *pSearchHandle)
{
	if (use_pk2_container) {
		PK2SearchState* state = (PK2SearchState*)pSearchHandle->hFind;
		delete state;
		pSearchHandle->hFind = 0;
		return true;
	}
	::FindClose(pSearchHandle->hFind);
	return true;
}

int CWFileManager::GetVirtualPath(char *Dst)
{
	strcpy_s(Dst, sizeof(this->initial_path), this->initial_path);
	return 1;
}

int CWFileManager::SetVirtualPath(const char *Src)
{
	strcpy_s(this->initial_path, sizeof(this->initial_path), Src ? Src : "");
	strcpy_s(this->current_dir, sizeof(this->current_dir), Src ? Src : "");
	return 1;
}


int CWFileManager::GetDirectoryName(rsize_t SizeInBytes, char *outname)
{
	strcpy_s(outname, SizeInBytes, this->current_dir);
	return (int)strlen(outname);
}

bool CWFileManager::ChangeDirTo(const char *lpPathName) {
	if (!lpPathName || !*lpPathName) return true;
	if (use_pk2_container) {
		std::string dir = NormalizePath(current_dir);
		dir += NormalizePath(lpPathName);
		if (!dir.empty() && dir.back() != '\\') dir += '\\';
		strcpy_s(current_dir, sizeof(current_dir), dir.c_str());
		return true;
	}
	bool ret = SetCurrentDirectoryA(lpPathName);
	GetCurrentDirectoryA(sizeof(this->current_dir), this->current_dir);
	if (this->current_dir[0] && this->current_dir[strlen(this->current_dir)-1] != '\\') strcat_s(this->current_dir, sizeof(this->current_dir), "\\");
	if (!ret) SHOW_ERROR("Change folder failed", "ChangeDirTo");
	return ret;
}

bool CWFileManager::ChangeDirectory(const char *lpPathName)
{
	if (!lpPathName || !*lpPathName) return true;
	if (use_pk2_container) return ChangeDirTo(lpPathName);
	SetCurrentDirectoryA(this->current_dir);
	return ChangeDirTo(lpPathName);
}

bool CWFileManager::ResetDirectory()
{
	if (use_pk2_container) { strcpy_s(this->current_dir, sizeof(this->current_dir), this->initial_path); return true; }
	return ChangeDirTo(this->initial_path);
}


BOOL CWFileManager::DirectoryCreate(const CHAR *lpPathName)
{
	if (use_pk2_container) return FALSE;
	SetCurrentDirectoryA(this->current_dir);
	return CreateDirectoryA(lpPathName, 0);
}

BOOL CWFileManager::DirectoryRemove(LPCSTR lpPathName)
{
	if (use_pk2_container) return FALSE;
	SetCurrentDirectoryA(this->current_dir);
	return RemoveDirectoryA(lpPathName);
}

int CWFileManager::CloseContainer()
{
	CloseAllFiles();
	if (pk2_reader) { pk2_reader->Close(); pk2_reader.reset(); }
	use_pk2_container = false;
	if (this->container_info) open_container_info_delete(this->container_info);
	this->container_info = 0;
	return 1;
}
