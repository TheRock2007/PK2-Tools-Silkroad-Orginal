#include "commandine.h"

#include <Windows.h>
#include <stdlib.h>

char g_cmdLinePath[260];
char g_cmdLineExe[260];

void populate_cmdline() {

	char *raw = GetCommandLineA();

	char *start;

	if (raw[0] == '"') {
		start = &raw[1]; // skip first character
	} else {
		start = &raw[0]; // complex expression to see the difference to above
	}

	char drive[3];
	char dir[256];
	char filename[256];
	char ext[256];


	_splitpath_s(start, drive, dir, filename, ext);

	if (strlen(ext) >= 2 && ext[strlen(ext) - 1] == '"') {
		ext[strlen(ext) - 1] = 0;
	}

	_makepath_s(g_cmdLinePath, sizeof(g_cmdLinePath), drive, dir, 0, 0);
	_makepath_s(g_cmdLineExe, sizeof(g_cmdLineExe), 0, 0, filename, ext);
}

char *get_cmdline_path() {
	return g_cmdLinePath;
}

char *get_cmdline_exe() {
	return g_cmdLineExe;
}
