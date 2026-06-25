#include "debug.h"
#include <stdio.h>
#include <stdarg.h>

int m_group = DEBUG_OFF;
FILE *dbgfile = nullptr;
int lines = 0;

int debug(debug_group group, const char *format, ...) {
	(void)group;
	(void)format;
	return 0;
}
