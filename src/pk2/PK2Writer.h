#pragma once

#ifndef PK2WRITER_H_
#define PK2WRITER_H_

#include "shared_types.h"
#include "PK2.h"
#include "blowfish.h"
#include "PK2Reader.h"
#include <string>
#include <vector>
#include <filesystem>

struct PK2BuildProgress
{
	const char * currentPath;
	uint32_t currentFile;
	uint32_t totalFiles;
	uint64_t currentBytes;
	uint64_t totalBytes;
};

typedef bool (*PK2BuildProgressCallback)(const PK2BuildProgress & progress, void * userdata);

class PK2Writer
{
private:
	Blowfish m_blowfish;
	std::string m_asciiKey;
	std::string m_baseKey;
	std::string m_error;
	bool m_encrypt;
	bool m_encryptPayloads;
	PK2BuildProgressCallback m_progressCallback;
	void * m_progressUserdata;

public:
	PK2Writer();

	void SetEncryptionKey(char * ascii_key = (char*)"169841", uint8_t ascii_key_length = 6, char * base_key = (char*)"\x03\xF8\xE4\x44\x88\x99\x3F\x64\xFE\x35", uint8_t base_key_length = 10);
	void SetEncryptEntries(bool enabled);
	void SetEncryptPayloads(bool enabled);
	void SetProgressCallback(PK2BuildProgressCallback callback, void * userdata);
	bool GetEncryptEntries() const;
	bool GetEncryptPayloads() const;
	std::string GetError() const;

	bool BuildFromFolder(const std::filesystem::path & inputFolder, const std::filesystem::path & outputPk2);
	bool ExtractAll(const std::filesystem::path & inputPk2, const std::filesystem::path & outputFolder);
	bool ListFiles(const std::filesystem::path & inputPk2, std::vector<std::string> & files);
	bool ExtractSelected(const std::filesystem::path & inputPk2, const std::filesystem::path & outputFolder, const std::vector<std::string> & selectedFiles);
	bool ImportFiles(const std::filesystem::path & inputPk2,
		const std::vector<std::filesystem::path> & files,
		const std::string & internalFolder,
		const std::filesystem::path & outputPk2,
		bool overwriteOutput);
	bool ImportFolder(const std::filesystem::path & inputPk2,
		const std::filesystem::path & sourceFolder,
		const std::filesystem::path & outputPk2,
		bool overwriteOutput);
	bool ImportFolder(const std::filesystem::path & inputPk2,
		const std::filesystem::path & sourceFolder,
		const std::string & internalFolder,
		const std::filesystem::path & outputPk2,
		bool overwriteOutput);
	bool CryptPayloadsInPlace(const std::filesystem::path & inputPk2, bool encryptPayloads);
};

#endif
