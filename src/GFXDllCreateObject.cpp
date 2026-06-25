#include <Windows.h>
#include "CPFileManager.h"
#include "CWFileManager.h"
#include "LoggingFileManager.h"

#include "debug.h"


#define TARGET_VERSION 0x1007
#define MODE_ARCHIVE 1
#define MODE_FILESYSTEM 2



extern "C" __declspec(dllexport) int __stdcall  GFXDllCreateObject(int mode, IFileManager** object, int version) {
	char message[100];

	debug(DEBUG_OBJECT, "GFXDllCreateObject(%08x, %08x, %04x)\n", mode, object, version);

	if (version != TARGET_VERSION) {
		sprintf(message, "Dll Version(%x)\nNecessary Version (%x)", version, TARGET_VERSION);
		MessageBox(0, message, "Invalid Version(GFXFileManager.dll)", MB_OK|MB_APPLMODAL);
		return 0;
	}

	if (mode == MODE_ARCHIVE) {
		*object = new LoggingFileManager<CPFileManager>();
	} else if (mode == MODE_FILESYSTEM) {
		*object = new LoggingFileManager<CWFileManager>();
	} else {
		*object = 0;
	}

	return 0;
}
