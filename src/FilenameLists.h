#pragma once
#include <list>

// Type definitions for clarity and consistency
typedef std::list<const char*> FileNameList;
typedef FileNameList::iterator FileNameListIterator;

// External declarations of the global lists
extern FileNameList g_lstNonEncFileNames;
extern FileNameList g_lstEncTxtFileNames;
extern FileNameList g_lstNonEncFolderNames;

// Initialize functions for each list type
void InitializeNonEncryptedFiles();
void InitializeEncryptedFiles();