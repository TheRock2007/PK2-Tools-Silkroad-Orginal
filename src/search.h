#pragma once

#include <Windows.h>

enum ENTRY_TYPE : char {
	ENTRY_TYPE_FOLDER = 1,
	ENTRY_TYPE_FILE = 2
};

struct search_result_t
{
	_FILETIME CreationTime;
	_FILETIME LastWriteTime;
	long long EntryOffset; // position of SPackObj
	long long DataOffset; // position of SPackBlock for directories, position of data for files.
	int Size;
	ENTRY_TYPE Type;
	char Name[89];
	WIN32_FIND_DATAA find_data;
};

struct search_token_t
{
public:
	char szPattern[260];
	char szName[260];
	char szExtension[260];
	bool bool0; // helps to know which fields to compare against
	bool bool1; // helps to know which fields to compare against
};

struct search_handle_t 
{
public:
	bool Success;
	bool YieldEverything; // search will stop at every entry (only works on FindNextFile)
	bool HasReachedEndOfBlock;
	long long NextDirectoryOffset;
	long long NextBlockOffset;
	long long NextEntryOffset;
	char szPattern[89];
	search_token_t Token;
	char field_387;
	HANDLE hFind;
	int field_38C;
};