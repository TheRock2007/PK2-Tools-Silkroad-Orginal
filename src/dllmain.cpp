#include <Windows.h>
#include <stdio.h>

#include "debug.h"

HMODULE hInstance;
extern FILE *dbgfile;

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ulReason, LPVOID lpReserved) {
	(void)lpReserved;
	if (ulReason == DLL_PROCESS_ATTACH) {
		hInstance = hModule;
		dbgfile = nullptr;
		DisableThreadLibraryCalls(hModule);
	} else if (ulReason == DLL_PROCESS_DETACH) {
		dbgfile = nullptr;
	}
	return TRUE;
}
