#pragma once

#include <Windows.h>

struct gfxinfo_t
{
  char in_use;
  int index;
  int number_of_open_files;
  char filename[256];
  int field_10C;
  int field_110;
  int field_114;
  unsigned long long int number_of_bytes_processed_total;
  
  SYSTEMTIME timestamp;
  int pid;
  int field_134;
};

gfxinfo_t *open_container_info_write(const char *filename);
gfxinfo_t *__cdecl open_container_info_delete(gfxinfo_t *info);

