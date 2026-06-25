#include "GFXInfo.h"

gfxinfo_t g_gfx_infos[500] = {0};
int open_container_counter = 0;

gfxinfo_t *open_container_info_write(const char *filename)
{
	int cindex;
	gfxinfo_t *cur_info;

	++open_container_counter;

	cindex = 0;
	cur_info = g_gfx_infos;

	while ( cur_info->in_use )
	{
		++cur_info;
		++cindex;

		// Check if we're overflowing
		if ( cur_info >= &g_gfx_infos[500] )
			return 0;
	}

	g_gfx_infos[cindex].index = cindex;
	g_gfx_infos[cindex].in_use = 1;
	g_gfx_infos[cindex].field_110 = 0;
	g_gfx_infos[cindex].field_114 = 0;
	g_gfx_infos[cindex].number_of_bytes_processed_total = 0;

	strcpy_s(g_gfx_infos[cindex].filename, 0x100u, filename);

	GetLocalTime(&g_gfx_infos[cindex].timestamp);

	g_gfx_infos[cindex].pid = GetCurrentProcessId();

	return &g_gfx_infos[cindex];
}


gfxinfo_t *__cdecl open_container_info_delete(gfxinfo_t *info)
{
	--open_container_counter;
	if ( info )
		info->in_use = 0;
	return info;
}


extern "C" __declspec(dllexport) int __stdcall GFXFMInfo(gfxinfo_t *pInfo, int index) {

	if ( g_gfx_infos[index].in_use )
	{
		memcpy(pInfo, &g_gfx_infos[index], sizeof(gfxinfo_t));
	}
	else
	{
		memset(pInfo, 0, sizeof(gfxinfo_t));
	}

	return 0;
}
