#include "IFileManager.h"
#include "debug.h"

extern "C" __declspec(dllexport) void __stdcall GFXDllReleaseObject(IFileManager* object) {
	debug(DEBUG_OBJECT, "GFXDllReleaseObject(%08x)\n", object);
	delete object;
}

