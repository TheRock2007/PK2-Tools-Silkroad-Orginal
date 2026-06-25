#include "IFileManager.h"
#include <iostream>
#include <iomanip>

int IFileManager::GetVersion(void) {
	std::cout << "FM::getVersion()";
	return FM_VERSION;
};

int IFileManager::CheckVersion(int version) {


	if (version != FM_VERSION) {
		std::cout << "FM::checkVersion(" << std::showbase << std::hex << version << ") = 0";

		char buffer[256];

		sprintf(buffer, "Dll Version(%x)\nNecessary Version(%x)", version, FM_VERSION);

		MessageBox(0, buffer, "Invalid Version(GFXFileManager.dll(Check Version))", MB_OK);

		return 0;
	}

	std::cout << "FM::checkVersion(" << std::showbase << std::hex << version << ") = 1";
	return 1;
};

int IFileManager::ShowDialog(DialogData *initstruct) {
	std::cout << "FM::ShowDialog(" << std::showbase << std::hex << initstruct << ") = 0";

	return 0;
};

int IFileManager::ForeachEntryInContainer(foreach_callback_t cb, const char *filter, void *userstate) {

	__int64 position;

	this->GetDirectoryPosition(&position);

	cb(CALLBACK_STATE_INIT, NULL, userstate);

	loop_container_content(cb, filter, userstate);

	this->SetDirectoryPosition(position);

	return 1;
}

void IFileManager::loop_container_content(foreach_callback_t cb, const char *filter, void *userstate) {

	search_handle_t searchHandle;
	search_result_t searchResult;

	// Loop over all dirs

	this->FindFirstFile(&searchHandle, filter, &searchResult);

	if (searchHandle.Success) {
		do {

			// Ignore everything thats not a directory or starts with a dot "."
			if (searchResult.Type == ENTRY_TYPE_FOLDER && searchResult.Name[0] != '.') {
				cb(CALLBACK_STATE_ENTER_DIR, &searchResult, userstate);

				if ( this->ChangeDirectory(searchResult.Name) ) {
					loop_container_content(cb, filter, userstate);

					this->ChangeDirectory("..");
				}

				cb(CALLBACK_STATE_LEAVE_DIR, NULL, userstate);
			}
		} while (this->FindNextFile(&searchHandle, &searchResult));

		this->FindClose(&searchHandle);
	}

	// Loop over all files
	this->FindFirstFile(&searchHandle, filter, &searchResult);

	if (searchHandle.Success) {
		do {

			// Filter out the files
			if (searchResult.Type == ENTRY_TYPE_FILE) {
				cb(CALLBACK_STATE_FILE, &searchResult, userstate);
			}
		} while (this->FindNextFile(&searchHandle, &searchResult));

		this->FindClose(&searchHandle);
	}
}
