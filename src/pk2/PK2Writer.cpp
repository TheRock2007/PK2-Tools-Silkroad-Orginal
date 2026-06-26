#include "PK2Writer.h"
#include "shared_io.h"
#include "../PK2PayloadCrypto.h"
#include <windows.h>
#include <cstdio>
#include <cstring>
#include <list>
#include <memory>
#include <algorithm>
#include <system_error>
#include <cctype>
#include <set>

namespace fs = std::filesystem;

namespace
{
	struct Node
	{
		std::string name;
		bool isDirectory = false;
		fs::path sourcePath;
		uint64_t fileOffset = 0;
		uint32_t fileSize = 0;
		uint64_t dirOffset = 0;
		uint32_t dirBlockCount = 0;
		uint64_t accessTime = 0;
		uint64_t createTime = 0;
		uint64_t modifyTime = 0;
		uint64_t subtreeFileCount = 0;
		uint64_t subtreeByteCount = 0;
		Node * parent = nullptr;
		std::vector<std::unique_ptr<Node>> children;
	};

	uint64_t FileTimeNow()
	{
		FILETIME ft;
		GetSystemTimeAsFileTime(&ft);
		ULARGE_INTEGER v;
		v.LowPart = ft.dwLowDateTime;
		v.HighPart = ft.dwHighDateTime;
		return v.QuadPart;
	}

	uint64_t FileTimeFromPath(const fs::path & path)
	{
		WIN32_FILE_ATTRIBUTE_DATA data;
		if(GetFileAttributesExA(path.string().c_str(), GetFileExInfoStandard, &data))
		{
			ULARGE_INTEGER v;
			v.LowPart = data.ftLastWriteTime.dwLowDateTime;
			v.HighPart = data.ftLastWriteTime.dwHighDateTime;
			return v.QuadPart;
		}
		return FileTimeNow();
	}

	std::string ToLowerAscii(std::string value)
	{
		std::transform(value.begin(), value.end(), value.begin(), [](unsigned char ch) { return static_cast<char>(tolower(ch)); });
		return value;
	}

	void InitializePk2BlowfishKey(Blowfish & blowfish, const char * ascii_key, uint8_t ascii_key_length)
	{
		static const char base_key[] = "\x03\xF8\xE4\x44\x88\x99\x3F\x64\xFE\x35";
		if(ascii_key_length > 56)
		{
			ascii_key_length = 56;
		}
		uint8_t bf_key[56] = {0};
		uint8_t a_key[56] = {0};
		uint8_t b_key[56] = {0};
		memcpy(a_key, ascii_key, ascii_key_length);
		memcpy(b_key, base_key, sizeof(base_key) - 1);
		for(int x = 0; x < ascii_key_length; ++x)
		{
			bf_key[x] = a_key[x] ^ b_key[x];
		}
		blowfish.Initialize(bf_key, ascii_key_length);
	}


	bool ReportBuildProgress(PK2BuildProgressCallback callback,
		void * userdata,
		const std::string & currentPath,
		uint32_t currentFile,
		uint32_t totalFiles,
		uint64_t currentBytes,
		uint64_t totalBytes)
	{
		if(!callback)
		{
			return true;
		}

		PK2BuildProgress progress{};
		progress.currentPath = currentPath.c_str();
		progress.currentFile = currentFile;
		progress.totalFiles = totalFiles;
		progress.currentBytes = currentBytes;
		progress.totalBytes = totalBytes;
		return callback(progress, userdata);
	}

	void RecalculateTreeTotals(Node & dir)
	{
		dir.subtreeFileCount = 0;
		dir.subtreeByteCount = 0;
		for(auto & child : dir.children)
		{
			if(child->isDirectory)
			{
				RecalculateTreeTotals(*child);
				dir.subtreeFileCount += child->subtreeFileCount;
				dir.subtreeByteCount += child->subtreeByteCount;
			}
			else
			{
				dir.subtreeFileCount += 1;
				dir.subtreeByteCount += child->fileSize;
			}
		}
	}

	bool CopyFileToStream(FILE * out,
		Node & node,
		const std::string & internalPath,
		bool encryptPayload,
		PK2BuildProgressCallback progressCallback,
		void * progressUserdata,
		uint32_t currentFile,
		uint32_t totalFiles,
		uint64_t & totalBytesWritten,
		uint64_t totalBytes,
		std::string & error)
	{
		FILE * in = nullptr;
		fopen_s(&in, node.sourcePath.string().c_str(), "rb");
		if(in == nullptr)
		{
			error = "Could not open source file: " + node.sourcePath.string();
			return false;
		}

		uint32_t looseEncryptedPayloadSize = 0;
		const bool sourceAlreadyEncrypted =
			PK2PayloadCrypto_TryGetLooseEncryptedPayloadSize(node.sourcePath.string(), looseEncryptedPayloadSize) &&
			looseEncryptedPayloadSize == node.fileSize;
		const bool encryptThisPayload = encryptPayload && PK2PayloadCrypto_ShouldDecrypt(internalPath);

		std::vector<uint8_t> buffer(1024 * 1024);
		size_t readCount = 0;
		uint64_t streamOffset = 0;
		uint64_t remaining = node.fileSize;
		if(!ReportBuildProgress(progressCallback, progressUserdata, internalPath, currentFile, totalFiles, totalBytesWritten, totalBytes))
		{
			error = "Build cancelled.";
			fclose(in);
			return false;
		}

		while(remaining > 0 && (readCount = fread(buffer.data(), 1, static_cast<size_t>(std::min<uint64_t>(buffer.size(), remaining)), in)) > 0)
		{
			if(sourceAlreadyEncrypted)
			{
				// Loose encrypted files are encrypted with offset 0.  Rebase them
				// to the final PK2 payload offset, or decrypt them if the target
				// archive should store plain payloads.
				PK2PayloadCrypto_CryptBuffer(0, node.fileSize, streamOffset, buffer.data(), readCount);
				if(encryptThisPayload)
				{
					PK2PayloadCrypto_CryptBuffer(node.fileOffset, node.fileSize, streamOffset, buffer.data(), readCount);
				}
			}
			else if(encryptThisPayload)
			{
				PK2PayloadCrypto_CryptBuffer(node.fileOffset, node.fileSize, streamOffset, buffer.data(), readCount);
			}
			if(fwrite(buffer.data(), 1, readCount, out) != readCount)
			{
				fclose(in);
				error = "Could not write file data for: " + node.sourcePath.string();
				return false;
			}
			streamOffset += static_cast<uint64_t>(readCount);
			remaining -= static_cast<uint64_t>(readCount);
			totalBytesWritten += static_cast<uint64_t>(readCount);
			if(!ReportBuildProgress(progressCallback, progressUserdata, internalPath, currentFile, totalFiles, totalBytesWritten, totalBytes))
			{
				error = "Build cancelled.";
				fclose(in);
				return false;
			}
		}

		fclose(in);
		return true;
	}

	bool BuildTree(const fs::path & folder,
		Node & parent,
		PK2BuildProgressCallback progressCallback,
		void * progressUserdata,
		uint32_t & scannedFiles,
		uint64_t & scannedBytes,
		std::string & error)
	{
		if(!ReportBuildProgress(progressCallback, progressUserdata, std::string("Scanning folder: ") + folder.string(), scannedFiles, 0, 0, 0))
		{
			error = "Build cancelled.";
			return false;
		}

		std::error_code ec;
		std::vector<fs::directory_entry> entries;
		for(const auto & entry : fs::directory_iterator(folder, ec))
		{
			if(ec)
			{
				error = "Could not enumerate folder: " + folder.string();
				return false;
			}
			entries.push_back(entry);
		}

		std::sort(entries.begin(), entries.end(), [](const fs::directory_entry & a, const fs::directory_entry & b)
		{
			if(a.is_directory() != b.is_directory())
			{
				return a.is_directory() > b.is_directory();
			}
			return ToLowerAscii(a.path().filename().string()) < ToLowerAscii(b.path().filename().string());
		});

		for(const auto & entry : entries)
		{
			auto child = std::make_unique<Node>();
			child->name = entry.path().filename().string();
			child->parent = &parent;
			child->sourcePath = entry.path();
			child->isDirectory = entry.is_directory();
			child->modifyTime = FileTimeFromPath(entry.path());
			child->createTime = child->modifyTime;
			child->accessTime = child->modifyTime;
			if(child->name.size() > 80)
			{
				error = "Entry name too long for PK2: " + child->name;
				return false;
			}
			if(child->isDirectory)
			{
				if(!BuildTree(entry.path(), *child, progressCallback, progressUserdata, scannedFiles, scannedBytes, error))
				{
					return false;
				}
			}
			else
			{
				uintmax_t size = entry.file_size(ec);
				if(ec)
				{
					error = "Could not get file size for: " + entry.path().string();
					return false;
				}

				uint32_t looseEncryptedPayloadSize = 0;
				if(PK2PayloadCrypto_TryGetLooseEncryptedPayloadSize(entry.path().string(), looseEncryptedPayloadSize))
				{
					child->fileSize = looseEncryptedPayloadSize;
				}
				else
				{
					if(size > 0xFFFFFFFFULL)
					{
						error = "File too large for PK2 format: " + entry.path().string();
						return false;
					}
					child->fileSize = static_cast<uint32_t>(size);
				}

				++scannedFiles;
				scannedBytes += child->fileSize;
				if(!ReportBuildProgress(progressCallback, progressUserdata, std::string("Scanning file: ") + entry.path().string(), scannedFiles, 0, scannedBytes, 0))
				{
					error = "Build cancelled.";
					return false;
				}
			}
			parent.children.push_back(std::move(child));
		}
		return true;
	}

	void ReserveDirectoryOffsets(FILE * out, Node & dir)
	{
		size_t entryCount = dir.children.size() + 2; // . and ..
		dir.dirBlockCount = static_cast<uint32_t>((entryCount + 19) / 20);
		dir.dirOffset = static_cast<uint64_t>(file_tell(out));

		PK2EntryBlock blank{};
		for(uint32_t i = 0; i < dir.dirBlockCount; ++i)
		{
			fwrite(&blank, 1, sizeof(blank), out);
		}

		for(auto & child : dir.children)
		{
			if(child->isDirectory)
			{
				ReserveDirectoryOffsets(out, *child);
			}
		}
	}

	bool WriteFilePayloads(FILE * out,
		Node & dir,
		const std::string & basePath,
		bool encryptPayload,
		PK2BuildProgressCallback progressCallback,
		void * progressUserdata,
		uint32_t totalFiles,
		uint32_t & currentFile,
		uint64_t & totalBytesWritten,
		uint64_t totalBytes,
		std::string & error)
	{
		for(auto & child : dir.children)
		{
			std::string childPath = basePath.empty() ? child->name : (basePath + "\\" + child->name);
			if(child->isDirectory)
			{
				if(!WriteFilePayloads(out, *child, childPath, encryptPayload, progressCallback, progressUserdata, totalFiles, currentFile, totalBytesWritten, totalBytes, error))
				{
					return false;
				}
			}
			else
			{
				child->fileOffset = static_cast<uint64_t>(file_tell(out));
				++currentFile;
				if(!CopyFileToStream(out, *child, childPath, encryptPayload, progressCallback, progressUserdata, currentFile, totalFiles, totalBytesWritten, totalBytes, error))
				{
					return false;
				}
			}
		}
		return true;
	}

	void FillEntry(PK2Entry & e, uint8_t type, const std::string & name, uint64_t position, uint32_t size, uint64_t atime, uint64_t ctime, uint64_t mtime)
	{
		memset(&e, 0, sizeof(e));
		e.type = type;
		memcpy(e.name, name.c_str(), std::min<size_t>(name.size(), 80));
		e.accessTime = atime;
		e.createTime = ctime;
		e.modifyTime = mtime;
		e.position = static_cast<int64_t>(position);
		e.size = size;
		e.nextChain = 0;
		e.padding[0] = 0;
		e.padding[1] = 0;
	}

	void GatherEntries(Node & dir, std::vector<PK2Entry> & outEntries)
	{
		uint64_t now = dir.modifyTime ? dir.modifyTime : FileTimeNow();
		PK2Entry self{};
		FillEntry(self, 1, ".", dir.dirOffset, 0, now, now, now);
		outEntries.push_back(self);

		PK2Entry parent{};
		uint64_t parentOffset = dir.parent ? dir.parent->dirOffset : dir.dirOffset;
		FillEntry(parent, 1, "..", parentOffset, 0, now, now, now);
		outEntries.push_back(parent);

		for(auto & child : dir.children)
		{
			PK2Entry entry{};
			if(child->isDirectory)
			{
				FillEntry(entry, 1, child->name, child->dirOffset, 0, child->accessTime, child->createTime, child->modifyTime);
			}
			else
			{
				FillEntry(entry, 2, child->name, child->fileOffset, child->fileSize, child->accessTime, child->createTime, child->modifyTime);
			}
			outEntries.push_back(entry);
		}
	}

	bool WriteDirectoryEntries(FILE * out, Node & dir, Blowfish & blowfish, bool encrypt, std::string & error)
	{
		std::vector<PK2Entry> entries;
		GatherEntries(dir, entries);

		for(uint32_t blockIndex = 0; blockIndex < dir.dirBlockCount; ++blockIndex)
		{
			PK2EntryBlock block{};
			for(int slot = 0; slot < 20; ++slot)
			{
				size_t globalIndex = static_cast<size_t>(blockIndex) * 20 + static_cast<size_t>(slot);
				if(globalIndex < entries.size())
				{
					block.entries[slot] = entries[globalIndex];
				}
			}

			if(blockIndex + 1 < dir.dirBlockCount)
			{
				block.entries[19].nextChain = static_cast<int64_t>(dir.dirOffset + static_cast<uint64_t>(blockIndex + 1) * sizeof(PK2EntryBlock));
			}

			if(file_seek(out, static_cast<int64_t>(dir.dirOffset + static_cast<uint64_t>(blockIndex) * sizeof(PK2EntryBlock)), SEEK_SET) != 0)
			{
				error = "Could not seek while writing directory entries.";
				return false;
			}

			if(encrypt)
			{
				for(int i = 0; i < 20; ++i)
				{
					PK2Entry encoded{};
					blowfish.Encode(&block.entries[i], sizeof(PK2Entry), &encoded, sizeof(PK2Entry));
					if(fwrite(&encoded, 1, sizeof(PK2Entry), out) != sizeof(PK2Entry))
					{
						error = "Could not write encrypted directory entry.";
						return false;
					}
				}
			}
			else
			{
				if(fwrite(&block, 1, sizeof(block), out) != sizeof(block))
				{
					error = "Could not write directory block.";
					return false;
				}
			}
		}

		for(auto & child : dir.children)
		{
			if(child->isDirectory)
			{
				if(!WriteDirectoryEntries(out, *child, blowfish, encrypt, error))
				{
					return false;
				}
			}
		}

		return true;
	}

	std::string NormalizeInternalFolder(std::string folder)
	{
		std::replace(folder.begin(), folder.end(), '/', '\\');
		while(!folder.empty() && (folder.front() == '\\' || folder.front() == '/'))
		{
			folder.erase(folder.begin());
		}
		while(!folder.empty() && (folder.back() == '\\' || folder.back() == '/'))
		{
			folder.pop_back();
		}
		return folder;
	}

	bool EnsureDirectory(const fs::path & path, std::string & error)
	{
		if(path.empty())
		{
			return true;
		}
		std::error_code ec;
		fs::create_directories(path, ec);
		if(ec)
		{
			error = "Could not create folder: " + path.string();
			return false;
		}
		return true;
	}

	std::string NormalizeRelativePath(std::string value)
	{
		std::replace(value.begin(), value.end(), '/', '\\');
		while(!value.empty() && (value.front() == '\\' || value.front() == '/'))
		{
			value.erase(value.begin());
		}
		return value;
	}

	std::vector<std::string> SplitInternalPath(const std::string & value)
	{
		std::vector<std::string> parts;
		std::string current;
		for(char ch : value)
		{
			if(ch == '\\' || ch == '/')
			{
				if(!current.empty())
				{
					parts.push_back(current);
					current.clear();
				}
			}
			else
			{
				current.push_back(ch);
			}
		}
		if(!current.empty())
		{
			parts.push_back(current);
		}
		return parts;
	}

	bool IsSamePath(const fs::path & a, const fs::path & b)
	{
		std::error_code ec;
		fs::path aa = fs::absolute(a, ec).lexically_normal();
		if(ec) aa = a;
		ec.clear();
		fs::path bb = fs::absolute(b, ec).lexically_normal();
		if(ec) bb = b;
		return ToLowerAscii(aa.string()) == ToLowerAscii(bb.string());
	}

	struct DirectPk2Context
	{
		FILE * file = nullptr;
		PK2Header header{};
		int64_t fileSize = 0;
		bool payloadEncrypted = false;
		bool headerNeedsRewrite = false;
	};

	struct DirectEntryRef
	{
		PK2Entry entry{};
		int64_t blockOffset = 0;
		int slot = 0;
	};

	bool LooksLikeValidDecodedEntry(const PK2Entry & entry)
	{
		if(entry.type == 0)
		{
			return true;
		}
		if(entry.type != 1 && entry.type != 2)
		{
			return false;
		}
		if(entry.padding[0] != 0 || entry.padding[1] != 0)
		{
			return false;
		}
		bool hasNull = false;
		for(size_t i = 0; i < sizeof(entry.name); ++i)
		{
			unsigned char ch = static_cast<unsigned char>(entry.name[i]);
			if(ch == 0)
			{
				hasNull = true;
				break;
			}
			if(ch < 32)
			{
				return false;
			}
		}
		return hasNull;
	}

	bool DirectProbeRootBlockForKey(FILE * file, Blowfish & blowfish)
	{
		if(file_seek(file, static_cast<int64_t>(sizeof(PK2Header)), SEEK_SET) != 0)
		{
			return false;
		}
		PK2EntryBlock block{};
		if(fread(&block, 1, sizeof(block), file) != sizeof(block))
		{
			return false;
		}
		bool hasUsableEntry = false;
		for(int i = 0; i < 20; ++i)
		{
			PK2Entry decoded{};
			blowfish.Decode(&block.entries[i], sizeof(PK2Entry), &decoded, sizeof(PK2Entry));
			if(!LooksLikeValidDecodedEntry(decoded))
			{
				return false;
			}
			if(decoded.type == 1 || decoded.type == 2)
			{
				hasUsableEntry = true;
			}
		}
		return hasUsableEntry;
	}

	bool DirectProbePlainRootBlock(FILE * file)
	{
		if(file_seek(file, static_cast<int64_t>(sizeof(PK2Header)), SEEK_SET) != 0)
		{
			return false;
		}
		PK2EntryBlock block{};
		if(fread(&block, 1, sizeof(block), file) != sizeof(block))
		{
			return false;
		}
		bool hasUsableEntry = false;
		for(int i = 0; i < 20; ++i)
		{
			if(!LooksLikeValidDecodedEntry(block.entries[i]))
			{
				return false;
			}
			if(block.entries[i].type == 1 || block.entries[i].type == 2)
			{
				hasUsableEntry = true;
			}
		}
		return hasUsableEntry;
	}

	bool DirectHeaderOrRootAcceptsKey(DirectPk2Context & ctx, Blowfish & blowfish)
	{
		uint8_t verify[16] = {0};
		blowfish.Encode("Joymax Pak File", 16, verify, 16);
		if(memcmp(verify, ctx.header.verify, 3) == 0)
		{
			return true;
		}
		return DirectProbeRootBlockForKey(ctx.file, blowfish);
	}

	bool DirectTryDecodeHeaderWithKey(Blowfish & blowfish, const PK2Header & rawHeader, PK2Header & decodedHeader)
	{
		PK2Header decoded{};
		blowfish.Decode(&rawHeader, sizeof(PK2Header), &decoded, sizeof(PK2Header));
		if(decoded.version != 0x01000002)
		{
			return false;
		}
		decoded.encryption = 1;
		decodedHeader = decoded;
		return true;
	}

	bool DirectOpenPk2(const fs::path & pk2, Blowfish & blowfish, DirectPk2Context & ctx, std::string & error)
	{
		fopen_s(&ctx.file, pk2.string().c_str(), "rb+");
		if(!ctx.file)
		{
			error = "Could not open PK2 for fast update: " + pk2.string();
			return false;
		}

		if(fread(&ctx.header, 1, sizeof(ctx.header), ctx.file) != sizeof(ctx.header))
		{
			error = "Could not read PK2 header.";
			fclose(ctx.file);
			ctx.file = nullptr;
			return false;
		}
		if(ctx.header.version != 0x01000002)
		{
			PK2Header rawHeader = ctx.header;
			PK2Header decodedHeader{};
			bool decoded = DirectTryDecodeHeaderWithKey(blowfish, rawHeader, decodedHeader);
			if(!decoded)
			{
				InitializePk2BlowfishKey(blowfish, "169841", 6);
				decoded = DirectTryDecodeHeaderWithKey(blowfish, rawHeader, decodedHeader);
			}
			if(!decoded)
			{
				InitializePk2BlowfishKey(blowfish, "\x32\x30\x30\x39\xC4\xEA", 6);
				decoded = DirectTryDecodeHeaderWithKey(blowfish, rawHeader, decodedHeader);
			}
			if(!decoded)
			{
				error = "Invalid PK2 header.";
				fclose(ctx.file);
				ctx.file = nullptr;
				return false;
			}
			ctx.header = decodedHeader;
			ctx.headerNeedsRewrite = true;
		}

		if(ctx.header.encryption)
		{
			if(!DirectHeaderOrRootAcceptsKey(ctx, blowfish))
			{
				InitializePk2BlowfishKey(blowfish, "169841", 6);
			}
			if(!DirectHeaderOrRootAcceptsKey(ctx, blowfish))
			{
				InitializePk2BlowfishKey(blowfish, "\x32\x30\x30\x39\xC4\xEA", 6);
			}
			if(!DirectHeaderOrRootAcceptsKey(ctx, blowfish))
			{
				error = "Invalid Blowfish key.";
				fclose(ctx.file);
				ctx.file = nullptr;
				return false;
			}
		}
		else if(!DirectProbePlainRootBlock(ctx.file))
		{
			InitializePk2BlowfishKey(blowfish, "169841", 6);
			if(DirectProbeRootBlockForKey(ctx.file, blowfish))
			{
				ctx.header.encryption = 1;
				ctx.headerNeedsRewrite = true;
			}
			else
			{
				InitializePk2BlowfishKey(blowfish, "\x32\x30\x30\x39\xC4\xEA", 6);
				if(DirectProbeRootBlockForKey(ctx.file, blowfish))
				{
					ctx.header.encryption = 1;
					ctx.headerNeedsRewrite = true;
				}
				else
				{
					error = "Invalid PK2 directory header.";
					fclose(ctx.file);
					ctx.file = nullptr;
					return false;
				}
			}
		}

		if(ctx.header.encryption)
		{
			uint8_t verify[16] = {0};
			blowfish.Encode("Joymax Pak File", 16, verify, 16);
			memset(verify + 3, 0, 13);
			if(memcmp(ctx.header.verify, verify, 16) != 0)
			{
				memcpy(ctx.header.verify, verify, 16);
				ctx.headerNeedsRewrite = true;
			}
		}

		if(file_seek(ctx.file, 0, SEEK_END) != 0)
		{
			error = "Could not seek to the end of the PK2 file.";
			fclose(ctx.file);
			ctx.file = nullptr;
			return false;
		}
		ctx.fileSize = file_tell(ctx.file);
		ctx.payloadEncrypted = PK2PayloadCrypto_HasMarker(ctx.header.reserved, sizeof(ctx.header.reserved));

		if(ctx.headerNeedsRewrite)
		{
			// Legacy builds encrypted/damaged the PK2 header itself, or left the
			// standard encryption flag/verify bytes inconsistent.  Normalize it
			// back to the standard Joymax header layout so normal tools/launchers can
			// enumerate and update the archive.  Directory entries remain protected by
			// the standard PK2 Blowfish flag, while payload encryption is tracked only
			// through the reserved marker.
			if(file_seek(ctx.file, 0, SEEK_SET) != 0 || fwrite(&ctx.header, 1, sizeof(ctx.header), ctx.file) != sizeof(ctx.header))
			{
				error = "Could not normalize the PK2 header.";
				fclose(ctx.file);
				ctx.file = nullptr;
				return false;
			}
			fflush(ctx.file);
		}

		return true;
	}

	void DirectClosePk2(DirectPk2Context & ctx)
	{
		if(ctx.file)
		{
			fclose(ctx.file);
			ctx.file = nullptr;
		}
	}

	bool DirectReadBlock(DirectPk2Context & ctx, Blowfish & blowfish, int64_t offset, PK2EntryBlock & block, std::string & error)
	{
		if(offset < static_cast<int64_t>(sizeof(PK2Header)))
		{
			error = "Invalid PK2 directory offset.";
			return false;
		}
		if(file_seek(ctx.file, offset, SEEK_SET) != 0)
		{
			error = "Could not seek to PK2 directory block.";
			return false;
		}
		if(fread(&block, 1, sizeof(block), ctx.file) != sizeof(block))
		{
			error = "Could not read PK2 directory block.";
			return false;
		}
		if(ctx.header.encryption)
		{
			for(int i = 0; i < 20; ++i)
			{
				PK2Entry decoded{};
				blowfish.Decode(&block.entries[i], sizeof(PK2Entry), &decoded, sizeof(PK2Entry));
				block.entries[i] = decoded;
			}
		}
		return true;
	}

	bool DirectWriteBlock(DirectPk2Context & ctx, Blowfish & blowfish, int64_t offset, const PK2EntryBlock & block, std::string & error)
	{
		if(file_seek(ctx.file, offset, SEEK_SET) != 0)
		{
			error = "Could not seek while writing PK2 directory block.";
			return false;
		}
		if(ctx.header.encryption)
		{
			for(int i = 0; i < 20; ++i)
			{
				PK2Entry encoded{};
				blowfish.Encode(&block.entries[i], sizeof(PK2Entry), &encoded, sizeof(PK2Entry));
				if(fwrite(&encoded, 1, sizeof(PK2Entry), ctx.file) != sizeof(PK2Entry))
				{
					error = "Could not write encrypted PK2 directory entry.";
					return false;
				}
			}
		}
		else if(fwrite(&block, 1, sizeof(block), ctx.file) != sizeof(block))
		{
			error = "Could not write PK2 directory block.";
			return false;
		}
		fflush(ctx.file);
		return true;
	}

	bool DirectWriteEntry(DirectPk2Context & ctx, Blowfish & blowfish, const DirectEntryRef & ref, const PK2Entry & entry, std::string & error)
	{
		PK2EntryBlock block{};
		if(!DirectReadBlock(ctx, blowfish, ref.blockOffset, block, error))
		{
			return false;
		}
		block.entries[ref.slot] = entry;
		return DirectWriteBlock(ctx, blowfish, ref.blockOffset, block, error);
	}

	bool DirectFindEntry(DirectPk2Context & ctx, Blowfish & blowfish, int64_t folderOffset, const std::string & name, DirectEntryRef & ref, bool & found, std::string & error)
	{
		found = false;
		std::set<int64_t> visited;
		int64_t current = folderOffset;
		while(current != 0)
		{
			if(!visited.insert(current).second)
			{
				error = "Detected a cyclic PK2 directory chain.";
				return false;
			}
			PK2EntryBlock block{};
			if(!DirectReadBlock(ctx, blowfish, current, block, error))
			{
				return false;
			}
			for(int slot = 0; slot < 20; ++slot)
			{
				PK2Entry & entry = block.entries[slot];
				if((entry.type == 1 || entry.type == 2) && _stricmp(entry.name, name.c_str()) == 0)
				{
					ref.entry = entry;
					ref.blockOffset = current;
					ref.slot = slot;
					found = true;
					return true;
				}
			}
			current = block.entries[19].nextChain;
		}
		return true;
	}

	bool DirectAppendBlock(DirectPk2Context & ctx, Blowfish & blowfish, const PK2EntryBlock & block, int64_t & newOffset, std::string & error)
	{
		if(file_seek(ctx.file, 0, SEEK_END) != 0)
		{
			error = "Could not seek to append a PK2 directory block.";
			return false;
		}
		newOffset = file_tell(ctx.file);
		if(!DirectWriteBlock(ctx, blowfish, newOffset, block, error))
		{
			return false;
		}
		ctx.fileSize = std::max<int64_t>(ctx.fileSize, newOffset + static_cast<int64_t>(sizeof(PK2EntryBlock)));
		return true;
	}

	bool DirectFindFreeSlot(DirectPk2Context & ctx, Blowfish & blowfish, int64_t folderOffset, DirectEntryRef & ref, std::string & error)
	{
		std::set<int64_t> visited;
		int64_t current = folderOffset;
		int64_t last = folderOffset;
		PK2EntryBlock lastBlock{};
		while(current != 0)
		{
			if(!visited.insert(current).second)
			{
				error = "Detected a cyclic PK2 directory chain.";
				return false;
			}
			PK2EntryBlock block{};
			if(!DirectReadBlock(ctx, blowfish, current, block, error))
			{
				return false;
			}
			for(int slot = 0; slot < 20; ++slot)
			{
				if(block.entries[slot].type == 0 && !(slot == 19 && block.entries[slot].nextChain != 0))
				{
					ref.blockOffset = current;
					ref.slot = slot;
					ref.entry = block.entries[slot];
					return true;
				}
			}
			last = current;
			lastBlock = block;
			current = block.entries[19].nextChain;
		}

		PK2EntryBlock blank{};
		int64_t newOffset = 0;
		if(!DirectAppendBlock(ctx, blowfish, blank, newOffset, error))
		{
			return false;
		}
		lastBlock.entries[19].nextChain = newOffset;
		if(!DirectWriteBlock(ctx, blowfish, last, lastBlock, error))
		{
			return false;
		}
		ref.blockOffset = newOffset;
		ref.slot = 0;
		ref.entry = PK2Entry{};
		return true;
	}

	bool DirectCreateDirectory(DirectPk2Context & ctx, Blowfish & blowfish, int64_t parentOffset, const std::string & name, int64_t & dirOffset, std::string & error)
	{
		if(name.empty() || name == "." || name == ".." || name.size() > 80)
		{
			error = "Invalid PK2 folder name: " + name;
			return false;
		}

		uint64_t now = FileTimeNow();
		PK2EntryBlock dirBlock{};
		if(file_seek(ctx.file, 0, SEEK_END) != 0)
		{
			error = "Could not seek to append a PK2 folder.";
			return false;
		}
		dirOffset = file_tell(ctx.file);
		FillEntry(dirBlock.entries[0], 1, ".", static_cast<uint64_t>(dirOffset), 0, now, now, now);
		FillEntry(dirBlock.entries[1], 1, "..", static_cast<uint64_t>(parentOffset), 0, now, now, now);
		if(!DirectAppendBlock(ctx, blowfish, dirBlock, dirOffset, error))
		{
			return false;
		}

		DirectEntryRef freeSlot{};
		if(!DirectFindFreeSlot(ctx, blowfish, parentOffset, freeSlot, error))
		{
			return false;
		}
		PK2Entry folderEntry{};
		FillEntry(folderEntry, 1, name, static_cast<uint64_t>(dirOffset), 0, now, now, now);
		return DirectWriteEntry(ctx, blowfish, freeSlot, folderEntry, error);
	}

	bool DirectEnsureFolder(DirectPk2Context & ctx, Blowfish & blowfish, const std::vector<std::string> & folders, int64_t & folderOffset, std::string & error)
	{
		folderOffset = sizeof(PK2Header);
		for(const std::string & folder : folders)
		{
			if(folder.empty())
			{
				continue;
			}
			DirectEntryRef ref{};
			bool found = false;
			if(!DirectFindEntry(ctx, blowfish, folderOffset, folder, ref, found, error))
			{
				return false;
			}
			if(found)
			{
				if(ref.entry.type != 1)
				{
					error = "A file already exists where a folder is needed: " + folder;
					return false;
				}
				folderOffset = ref.entry.position;
				continue;
			}

			int64_t createdOffset = 0;
			if(!DirectCreateDirectory(ctx, blowfish, folderOffset, folder, createdOffset, error))
			{
				return false;
			}
			folderOffset = createdOffset;
		}
		return true;
	}

	bool DirectAppendFilePayload(DirectPk2Context & ctx, const fs::path & sourceFile, const std::string & internalPath, uint64_t & fileOffset, uint32_t & fileSize, std::string & error)
	{
		std::error_code ec;
		uintmax_t size = fs::file_size(sourceFile, ec);
		if(ec)
		{
			error = "Could not get source file size: " + sourceFile.string();
			return false;
		}
		uint32_t looseEncryptedPayloadSize = 0;
		const bool sourceAlreadyEncrypted = PK2PayloadCrypto_TryGetLooseEncryptedPayloadSize(sourceFile.string(), looseEncryptedPayloadSize);
		if(sourceAlreadyEncrypted)
		{
			fileSize = looseEncryptedPayloadSize;
		}
		else
		{
			if(size > 0xFFFFFFFFULL)
			{
				error = "File too large for PK2 format: " + sourceFile.string();
				return false;
			}
			fileSize = static_cast<uint32_t>(size);
		}

		FILE * in = nullptr;
		fopen_s(&in, sourceFile.string().c_str(), "rb");
		if(!in)
		{
			error = "Could not open source file: " + sourceFile.string();
			return false;
		}

		if(file_seek(ctx.file, 0, SEEK_END) != 0)
		{
			fclose(in);
			error = "Could not seek to append PK2 payload.";
			return false;
		}
		fileOffset = static_cast<uint64_t>(file_tell(ctx.file));
		const bool encryptThisPayload = ctx.payloadEncrypted && PK2PayloadCrypto_ShouldDecrypt(internalPath);

		std::vector<uint8_t> buffer(1024 * 1024);
		uint64_t streamOffset = 0;
		uint64_t remaining = fileSize;
		while(remaining > 0)
		{
			size_t readCount = fread(buffer.data(), 1, static_cast<size_t>(std::min<uint64_t>(buffer.size(), remaining)), in);
			if(readCount == 0)
			{
				break;
			}
			if(sourceAlreadyEncrypted)
			{
				// Source bytes are a loose encrypted payload with offset 0.  Convert
				// them to plain first, then optionally encrypt them again for the
				// destination PK2 payload offset.
				PK2PayloadCrypto_CryptBuffer(0, fileSize, streamOffset, buffer.data(), readCount);
				if(encryptThisPayload)
				{
					PK2PayloadCrypto_CryptBuffer(fileOffset, fileSize, streamOffset, buffer.data(), readCount);
				}
			}
			else if(encryptThisPayload)
			{
				PK2PayloadCrypto_CryptBuffer(fileOffset, fileSize, streamOffset, buffer.data(), readCount);
			}
			if(fwrite(buffer.data(), 1, readCount, ctx.file) != readCount)
			{
				fclose(in);
				error = "Could not append file payload to PK2.";
				return false;
			}
			streamOffset += static_cast<uint64_t>(readCount);
			remaining -= static_cast<uint64_t>(readCount);
		}
		fclose(in);
		fflush(ctx.file);
		ctx.fileSize = std::max<int64_t>(ctx.fileSize, static_cast<int64_t>(fileOffset + fileSize));
		return true;
	}

	bool DirectUpsertFile(DirectPk2Context & ctx, Blowfish & blowfish, const fs::path & sourceFile, const std::string & internalFolder, std::string & error)
	{
		if(!fs::exists(sourceFile) || !fs::is_regular_file(sourceFile))
		{
			error = "Invalid source file: " + sourceFile.string();
			return false;
		}

		std::string fileName = sourceFile.filename().string();
		if(fileName.empty() || fileName.size() > 80)
		{
			error = "Invalid PK2 file name: " + fileName;
			return false;
		}

		std::vector<std::string> folders = SplitInternalPath(NormalizeInternalFolder(internalFolder));
		int64_t folderOffset = 0;
		if(!DirectEnsureFolder(ctx, blowfish, folders, folderOffset, error))
		{
			return false;
		}

		DirectEntryRef ref{};
		bool found = false;
		if(!DirectFindEntry(ctx, blowfish, folderOffset, fileName, ref, found, error))
		{
			return false;
		}
		if(found && ref.entry.type != 2)
		{
			error = "A folder already exists with the same name: " + fileName;
			return false;
		}

		std::error_code ec;
		uintmax_t sourceSize = fs::file_size(sourceFile, ec);
		if(ec)
		{
			error = "Could not get source file size: " + sourceFile.string();
			return false;
		}
		uint32_t looseEncryptedPayloadSize = 0;
		uint32_t effectiveSourceSize = 0;
		if(PK2PayloadCrypto_TryGetLooseEncryptedPayloadSize(sourceFile.string(), looseEncryptedPayloadSize))
		{
			effectiveSourceSize = looseEncryptedPayloadSize;
		}
		else
		{
			if(sourceSize > 0xFFFFFFFFULL)
			{
				error = "File too large for PK2 format: " + sourceFile.string();
				return false;
			}
			effectiveSourceSize = static_cast<uint32_t>(sourceSize);
		}

		uint64_t fileTime = FileTimeFromPath(sourceFile);
		if(found && ref.entry.size == effectiveSourceSize && ref.entry.modifyTime == fileTime)
		{
			return true;
		}

		if(!found && !DirectFindFreeSlot(ctx, blowfish, folderOffset, ref, error))
		{
			return false;
		}

		uint64_t fileOffset = 0;
		uint32_t fileSize = 0;
		std::string internalPath = NormalizeInternalFolder(internalFolder);
		if(!internalPath.empty())
		{
			internalPath += "\\";
		}
		internalPath += fileName;
		if(!DirectAppendFilePayload(ctx, sourceFile, internalPath, fileOffset, fileSize, error))
		{
			return false;
		}

		PK2Entry updated{};
		FillEntry(updated, 2, fileName, fileOffset, fileSize, fileTime, fileTime, fileTime);
		return DirectWriteEntry(ctx, blowfish, ref, updated, error);
	}

	bool DirectImportFilesInPlace(Blowfish & blowfish, const fs::path & pk2, const std::vector<fs::path> & files, const std::string & internalFolder, std::string & error)
	{
		DirectPk2Context ctx{};
		if(!DirectOpenPk2(pk2, blowfish, ctx, error))
		{
			return false;
		}
		for(const fs::path & file : files)
		{
			if(!DirectUpsertFile(ctx, blowfish, file, internalFolder, error))
			{
				DirectClosePk2(ctx);
				return false;
			}
		}
		DirectClosePk2(ctx);
		return true;
	}

	bool DirectImportFolderInPlace(Blowfish & blowfish, const fs::path & pk2, const fs::path & sourceFolder, const std::string & internalFolder, std::string & error)
	{
		DirectPk2Context ctx{};
		if(!DirectOpenPk2(pk2, blowfish, ctx, error))
		{
			return false;
		}

		std::error_code ec;
		std::string normalizedBase = NormalizeInternalFolder(internalFolder);
		std::vector<std::string> baseFolders = SplitInternalPath(normalizedBase);
		int64_t baseOffset = 0;
		if(!DirectEnsureFolder(ctx, blowfish, baseFolders, baseOffset, error))
		{
			DirectClosePk2(ctx);
			return false;
		}

		for(auto it = fs::recursive_directory_iterator(sourceFolder, ec); it != fs::recursive_directory_iterator(); it.increment(ec))
		{
			if(ec)
			{
				error = "Could not read source folder tree.";
				DirectClosePk2(ctx);
				return false;
			}

			fs::path relative = fs::relative(it->path(), sourceFolder, ec);
			if(ec)
			{
				error = "Could not calculate relative path for: " + it->path().string();
				DirectClosePk2(ctx);
				return false;
			}

			std::string relativeFolder = relative.parent_path().string();
			std::string targetFolder = normalizedBase;
			if(!relativeFolder.empty())
			{
				if(!targetFolder.empty())
				{
					targetFolder += "\\";
				}
				targetFolder += relativeFolder;
			}

			if(it->is_directory())
			{
				std::string directoryTarget = normalizedBase;
				std::string rel = relative.string();
				if(!rel.empty())
				{
					if(!directoryTarget.empty())
					{
						directoryTarget += "\\";
					}
					directoryTarget += rel;
				}
				int64_t ignoredOffset = 0;
				if(!DirectEnsureFolder(ctx, blowfish, SplitInternalPath(NormalizeInternalFolder(directoryTarget)), ignoredOffset, error))
				{
					DirectClosePk2(ctx);
					return false;
				}
			}
			else if(it->is_regular_file())
			{
				if(!DirectUpsertFile(ctx, blowfish, it->path(), targetFolder, error))
				{
					DirectClosePk2(ctx);
					return false;
				}
			}
		}

		DirectClosePk2(ctx);
		return true;
	}

	struct ListedFile
	{
		std::string relativePath;
		PK2Entry entry;
	};

	struct CollectFilesContext
	{
		std::vector<ListedFile> files;
	};

	bool CollectFilesCallback(PK2Reader * reader, const std::string & path, PK2EntryBlock & block, void * userdata)
	{
		(void)reader;
		CollectFilesContext * ctx = static_cast<CollectFilesContext*>(userdata);
		for(int i = 0; i < 20; ++i)
		{
			PK2Entry & entry = block.entries[i];
			if(entry.type != 2)
			{
				continue;
			}

			std::string relativePath = path;
			if(!relativePath.empty())
			{
				relativePath += "\\";
			}
			relativePath += entry.name;
			ctx->files.push_back({NormalizeRelativePath(relativePath), entry});
		}
		return true;
	}

	bool CollectFilesLikeExtractorRecursive(PK2Reader & reader, PK2Entry & folder, const std::string & path, CollectFilesContext & ctx, std::string & error)
	{
		std::list<PK2Entry> entries;
		if(!reader.GetEntries(folder, entries))
		{
			error = reader.GetError();
			return false;
		}

		for(PK2Entry & entry : entries)
		{
			if(strcmp(entry.name, ".") == 0 || strcmp(entry.name, "..") == 0)
			{
				continue;
			}

			std::string relativePath = path.empty() ? std::string(entry.name) : (path + "\\" + entry.name);
			if(entry.type == 2)
			{
				ctx.files.push_back({NormalizeRelativePath(relativePath), entry});
			}
			else if(entry.type == 1)
			{
				if(!CollectFilesLikeExtractorRecursive(reader, entry, relativePath, ctx, error))
				{
					return false;
				}
			}
		}

		return true;
	}

	bool CollectFilesLikeExtractor(PK2Reader & reader, CollectFilesContext & ctx, std::string & error)
	{
		PK2Entry root{};
		root.type = 1;
		root.position = sizeof(PK2Header);
		return CollectFilesLikeExtractorRecursive(reader, root, std::string(), ctx, error);
	}

	struct ExtractContext
	{
		fs::path baseFolder;
		std::string error;
	};

	bool ExtractCallback(PK2Reader * reader, const std::string & path, PK2EntryBlock & block, void * userdata)
	{
		ExtractContext * ctx = static_cast<ExtractContext*>(userdata);
		for(int i = 0; i < 20; ++i)
		{
			PK2Entry & entry = block.entries[i];
			if(entry.type != 1 && entry.type != 2)
			{
				continue;
			}
			if(strcmp(entry.name, ".") == 0 || strcmp(entry.name, "..") == 0)
			{
				continue;
			}
			fs::path outPath = ctx->baseFolder;
			if(!path.empty())
			{
				outPath /= fs::path(path);
			}
			outPath /= entry.name;

			if(entry.type == 1)
			{
				if(!EnsureDirectory(outPath, ctx->error))
				{
					return false;
				}
			}
			else
			{
				if(!EnsureDirectory(outPath.parent_path(), ctx->error))
				{
					return false;
				}
				std::vector<uint8_t> buffer;
				if(!reader->ExtractToMemory(entry, buffer))
				{
					ctx->error = reader->GetError();
					return false;
				}
				std::string internalPath = path.empty() ? std::string(entry.name) : (path + "\\" + entry.name);
				if(reader->HasPayloadEncryption() && !buffer.empty() && PK2PayloadCrypto_ShouldDecrypt(internalPath))
				{
					PK2PayloadCrypto_DecryptBufferForFile(static_cast<uint64_t>(entry.position), entry.size, buffer.data(), buffer.size());
				}
				FILE * out = nullptr;
				fopen_s(&out, outPath.string().c_str(), "wb");
				if(out == nullptr)
				{
					ctx->error = "Could not create file: " + outPath.string();
					return false;
				}
				if(!buffer.empty() && fwrite(buffer.data(), 1, buffer.size(), out) != buffer.size())
				{
					fclose(out);
					ctx->error = "Could not write file: " + outPath.string();
					return false;
				}
				fclose(out);
			}
		}
		return true;
	}
}

PK2Writer::PK2Writer()
	: m_encrypt(true), m_encryptPayloads(true), m_progressCallback(nullptr), m_progressUserdata(nullptr)
{
	SetEncryptionKey();
}

void PK2Writer::SetEncryptionKey(char * ascii_key, uint8_t ascii_key_length, char * base_key, uint8_t base_key_length)
{
	if(ascii_key_length > 56)
	{
		ascii_key_length = 56;
	}
	m_asciiKey.assign(ascii_key, ascii_key + ascii_key_length);
	m_baseKey.assign(base_key, base_key + base_key_length);
	uint8_t bf_key[56] = { 0 };
	uint8_t a_key[56] = { 0 };
	uint8_t b_key[56] = { 0 };
	memcpy(a_key, ascii_key, ascii_key_length);
	memcpy(b_key, base_key, base_key_length);
	for(int x = 0; x < ascii_key_length; ++x)
	{
		bf_key[x] = a_key[x] ^ b_key[x];
	}
	m_blowfish.Initialize(bf_key, ascii_key_length);
}

void PK2Writer::SetEncryptEntries(bool enabled)
{
	m_encrypt = enabled;
}

void PK2Writer::SetEncryptPayloads(bool enabled)
{
	m_encryptPayloads = enabled;
}

void PK2Writer::SetProgressCallback(PK2BuildProgressCallback callback, void * userdata)
{
	m_progressCallback = callback;
	m_progressUserdata = userdata;
}

bool PK2Writer::GetEncryptEntries() const
{
	return m_encrypt;
}

bool PK2Writer::GetEncryptPayloads() const
{
	return m_encryptPayloads;
}

std::string PK2Writer::GetError() const
{
	return m_error;
}

bool PK2Writer::BuildFromFolder(const fs::path & inputFolder, const fs::path & outputPk2)
{
	m_error.clear();
	if(!fs::exists(inputFolder) || !fs::is_directory(inputFolder))
	{
		m_error = "Input folder does not exist.";
		return false;
	}

	Node root;
	root.name.clear();
	root.isDirectory = true;
	root.sourcePath = inputFolder;
	root.parent = nullptr;
	root.modifyTime = FileTimeNow();
	root.createTime = root.modifyTime;
	root.accessTime = root.modifyTime;

	uint32_t scannedFiles = 0;
	uint64_t scannedBytes = 0;
	if(!BuildTree(inputFolder, root, m_progressCallback, m_progressUserdata, scannedFiles, scannedBytes, m_error))
	{
		return false;
	}
	if(!ReportBuildProgress(m_progressCallback, m_progressUserdata, "Preparing PK2 directory layout...", scannedFiles, scannedFiles, 0, 0))
	{
		m_error = "Build cancelled.";
		return false;
	}
	RecalculateTreeTotals(root);

	FILE * out = nullptr;
	fopen_s(&out, outputPk2.string().c_str(), "wb+");
	if(out == nullptr)
	{
		m_error = "Could not create output PK2 file.";
		return false;
	}

	PK2Header header{};
	memcpy(header.name, "JoyMax File Manager!\n", 21);
	header.version = 0x01000002;
	header.encryption = m_encrypt ? 1 : 0;
	memset(header.reserved, 0, sizeof(header.reserved));
	if(m_encryptPayloads)
	{
		PK2PayloadCrypto_WriteMarker(header.reserved, sizeof(header.reserved));
	}
	if(m_encrypt)
	{
		uint8_t verify[16] = {0};
		m_blowfish.Encode("Joymax Pak File", 16, verify, 16);
		memset(verify + 3, 0, 13);
		memcpy(header.verify, verify, 16);
		root.dirOffset = sizeof(PK2Header);
	}

	if(fwrite(&header, 1, sizeof(header), out) != sizeof(header))
	{
		fclose(out);
		m_error = "Could not write PK2 header.";
		return false;
	}

	ReserveDirectoryOffsets(out, root);
	uint32_t currentFile = 0;
	uint64_t totalBytesWritten = 0;
	if(!WriteFilePayloads(out,
		root,
		std::string(),
		m_encryptPayloads,
		m_progressCallback,
		m_progressUserdata,
		static_cast<uint32_t>(root.subtreeFileCount),
		currentFile,
		totalBytesWritten,
		root.subtreeByteCount,
		m_error))
	{
		fclose(out);
		return false;
	}
	if(!ReportBuildProgress(m_progressCallback, m_progressUserdata, std::string(), currentFile, static_cast<uint32_t>(root.subtreeFileCount), totalBytesWritten, root.subtreeByteCount))
	{
		fclose(out);
		m_error = "Build cancelled.";
		return false;
	}
	if(!WriteDirectoryEntries(out, root, m_blowfish, m_encrypt, m_error))
	{
		fclose(out);
		return false;
	}

	fclose(out);
	return true;
}

bool PK2Writer::ExtractAll(const fs::path & inputPk2, const fs::path & outputFolder)
{
	m_error.clear();
	PK2Reader reader;
	if(!m_asciiKey.empty())
	{
		reader.SetDecryptionKey((char*)m_asciiKey.data(), static_cast<uint8_t>(std::min<size_t>(m_asciiKey.size(), 56)));
	}
	if(!reader.Open(inputPk2.string().c_str()))
	{
		m_error = reader.GetError();
		return false;
	}

	ExtractContext ctx;
	ctx.baseFolder = outputFolder;
	if(!EnsureDirectory(outputFolder, ctx.error))
	{
		m_error = ctx.error;
		reader.Close();
		return false;
	}

	reader.ForEachEntryDo(&ExtractCallback, &ctx);
	if(!ctx.error.empty())
	{
		m_error = ctx.error;
		reader.Close();
		return false;
	}

	reader.Close();
	return true;
}

bool PK2Writer::ListFiles(const fs::path & inputPk2, std::vector<std::string> & files)
{
	m_error.clear();
	files.clear();

	PK2Reader reader;
	if(!m_asciiKey.empty())
	{
		reader.SetDecryptionKey((char*)m_asciiKey.data(), static_cast<uint8_t>(std::min<size_t>(m_asciiKey.size(), 56)));
	}
	if(!reader.Open(inputPk2.string().c_str()))
	{
		m_error = reader.GetError();
		return false;
	}

	CollectFilesContext ctx;
	if(!CollectFilesLikeExtractor(reader, ctx, m_error))
	{
		reader.Close();
		return false;
	}

	reader.Close();
	std::sort(ctx.files.begin(), ctx.files.end(), [](const ListedFile & a, const ListedFile & b)
	{
		return ToLowerAscii(a.relativePath) < ToLowerAscii(b.relativePath);
	});
	for(const auto & item : ctx.files)
	{
		files.push_back(item.relativePath);
	}
	return true;
}


bool PK2Writer::CryptPayloadsInPlace(const fs::path & inputPk2, bool encryptPayloads)
{
	m_error.clear();

	PK2Reader reader;
	if(!m_asciiKey.empty())
	{
		reader.SetDecryptionKey((char*)m_asciiKey.data(), static_cast<uint8_t>(std::min<size_t>(m_asciiKey.size(), 56)));
	}
	bool opened = reader.Open(inputPk2.string().c_str());
	if(!opened)
	{
		std::string openError = reader.GetError();
		if(openError == "Invalid Blowfish key.")
		{
			reader.SetDecryptionKey((char*)"\x32\x30\x30\x39\xC4\xEA");
			opened = reader.Open(inputPk2.string().c_str());
		}
		if(!opened)
		{
			m_error = openError.empty() ? reader.GetError() : openError;
			return false;
		}
	}

	const bool currentlyEncrypted = reader.HasPayloadEncryption();
	PK2Header decodedHeader = reader.GetHeader();
	CollectFilesContext ctx;
	if(!CollectFilesLikeExtractor(reader, ctx, m_error))
	{
		reader.Close();
		return false;
	}
	reader.Close();

	FILE * file = nullptr;
	fopen_s(&file, inputPk2.string().c_str(), "rb+");
	if(!file)
	{
		m_error = "Could not open PK2 for in-place payload encryption: " + inputPk2.string();
		return false;
	}

	PK2Header header{};
	if(fread(&header, 1, sizeof(header), file) != sizeof(header))
	{
		fclose(file);
		m_error = "Could not read PK2 header.";
		return false;
	}

	const bool legacyEncryptedHeader = header.version != 0x01000002 && decodedHeader.version == 0x01000002;
	if(legacyEncryptedHeader)
	{
		// Convert old custom/encrypted headers back to a normal Joymax PK2 header.
		// This is the important compatibility step: external PK2 tools and the
		// launcher/update importer can read the archive structure normally, while
		// GFXFileManager still decrypts protected internal file payloads.
		header = decodedHeader;
	}

	const bool payloadStateChanged = currentlyEncrypted != encryptPayloads;
	if(payloadStateChanged)
	{
		uint64_t totalBytes = 0;
		for(const auto & item : ctx.files)
		{
			totalBytes += item.entry.size;
		}

		std::vector<uint8_t> buffer(1024 * 1024);
		uint64_t totalDone = 0;
		uint32_t currentFile = 0;
		for(const auto & item : ctx.files)
		{
			++currentFile;
			const PK2Entry & entry = item.entry;
			if(entry.type != 2 || entry.size == 0 || !PK2PayloadCrypto_ShouldDecrypt(item.relativePath))
			{
				continue;
			}
			if(entry.position < static_cast<int64_t>(sizeof(PK2Header)))
			{
				fclose(file);
				m_error = "Invalid PK2 payload offset for: " + item.relativePath;
				return false;
			}

			uint64_t streamOffset = 0;
			uint64_t remaining = entry.size;
			while(remaining > 0)
			{
				const size_t chunk = static_cast<size_t>(std::min<uint64_t>(buffer.size(), remaining));
				if(file_seek(file, entry.position + static_cast<int64_t>(streamOffset), SEEK_SET) != 0)
				{
					fclose(file);
					m_error = "Could not seek to PK2 payload: " + item.relativePath;
					return false;
				}
				if(fread(buffer.data(), 1, chunk, file) != chunk)
				{
					fclose(file);
					m_error = "Could not read PK2 payload: " + item.relativePath;
					return false;
				}
				PK2PayloadCrypto_CryptBuffer(static_cast<uint64_t>(entry.position), entry.size, streamOffset, buffer.data(), chunk);
				if(file_seek(file, entry.position + static_cast<int64_t>(streamOffset), SEEK_SET) != 0)
				{
					fclose(file);
					m_error = "Could not seek while writing PK2 payload: " + item.relativePath;
					return false;
				}
				if(fwrite(buffer.data(), 1, chunk, file) != chunk)
				{
					fclose(file);
					m_error = "Could not write PK2 payload: " + item.relativePath;
					return false;
				}
				streamOffset += chunk;
				remaining -= chunk;
				totalDone += chunk;
				if(!ReportBuildProgress(m_progressCallback, m_progressUserdata, item.relativePath, currentFile, static_cast<uint32_t>(ctx.files.size()), totalDone, totalBytes))
				{
					fclose(file);
					m_error = "PK2 payload operation cancelled.";
					return false;
				}
			}
		}
	}
	else if(!legacyEncryptedHeader)
	{
		// Already in the requested payload state and the header is already standard.
		fclose(file);
		return true;
	}

	if(encryptPayloads)
	{
		PK2PayloadCrypto_WriteMarker(header.reserved, sizeof(header.reserved));
	}
	else
	{
		PK2PayloadCrypto_ClearMarker(header.reserved, sizeof(header.reserved));
	}

	if(file_seek(file, 0, SEEK_SET) != 0 || fwrite(&header, 1, sizeof(header), file) != sizeof(header))
	{
		fclose(file);
		m_error = "Could not update PK2 header marker.";
		return false;
	}

	fflush(file);
	fclose(file);
	return true;
}

bool PK2Writer::ExtractSelected(const fs::path & inputPk2, const fs::path & outputFolder, const std::vector<std::string> & selectedFiles)
{
	m_error.clear();
	if(selectedFiles.empty())
	{
		m_error = "No internal PK2 files were selected.";
		return false;
	}

	PK2Reader reader;
	if(!m_asciiKey.empty())
	{
		reader.SetDecryptionKey((char*)m_asciiKey.data(), static_cast<uint8_t>(std::min<size_t>(m_asciiKey.size(), 56)));
	}
	if(!reader.Open(inputPk2.string().c_str()))
	{
		m_error = reader.GetError();
		return false;
	}

	if(!EnsureDirectory(outputFolder, m_error))
	{
		reader.Close();
		return false;
	}

	CollectFilesContext ctx;
	if(!CollectFilesLikeExtractor(reader, ctx, m_error))
	{
		reader.Close();
		return false;
	}

	std::map<std::string, PK2Entry> lookup;
	for(const auto & item : ctx.files)
	{
		lookup[ToLowerAscii(item.relativePath)] = item.entry;
	}

	for(const auto & requested : selectedFiles)
	{
		std::string normalized = NormalizeRelativePath(requested);
		auto it = lookup.find(ToLowerAscii(normalized));
		if(it == lookup.end())
		{
			m_error = "Could not find PK2 entry: " + normalized;
			reader.Close();
			return false;
		}

		fs::path outPath = outputFolder / fs::path(normalized);
		if(!EnsureDirectory(outPath.parent_path(), m_error))
		{
			reader.Close();
			return false;
		}

		std::vector<uint8_t> buffer;
		PK2Entry entry = it->second;
		if(!reader.ExtractToMemory(entry, buffer))
		{
			m_error = reader.GetError();
			reader.Close();
			return false;
		}
		if(reader.HasPayloadEncryption() && !buffer.empty() && PK2PayloadCrypto_ShouldDecrypt(normalized))
		{
			PK2PayloadCrypto_DecryptBufferForFile(static_cast<uint64_t>(entry.position), entry.size, buffer.data(), buffer.size());
		}

		FILE * out = nullptr;
		fopen_s(&out, outPath.string().c_str(), "wb");
		if(out == nullptr)
		{
			m_error = "Could not create output file: " + outPath.string();
			reader.Close();
			return false;
		}
		if(!buffer.empty() && fwrite(buffer.data(), 1, buffer.size(), out) != buffer.size())
		{
			fclose(out);
			m_error = "Could not write output file: " + outPath.string();
			reader.Close();
			return false;
		}
		fclose(out);
	}

	reader.Close();
	return true;
}

bool PK2Writer::ImportFiles(const fs::path & inputPk2,
	const std::vector<fs::path> & files,
	const std::string & internalFolder,
	const fs::path & outputPk2,
	bool overwriteOutput)
{
	m_error.clear();
	if(files.empty())
	{
		m_error = "No files selected for import.";
		return false;
	}

	if(overwriteOutput && IsSamePath(inputPk2, outputPk2))
	{
		return DirectImportFilesInPlace(m_blowfish, inputPk2, files, internalFolder, m_error);
	}

	char tempPathBuffer[MAX_PATH] = {0};
	GetTempPathA(MAX_PATH, tempPathBuffer);
	char tempDirBuffer[MAX_PATH] = {0};
	GetTempFileNameA(tempPathBuffer, "pk2", 0, tempDirBuffer);
	DeleteFileA(tempDirBuffer);
	CreateDirectoryA(tempDirBuffer, nullptr);
	fs::path tempFolder = tempDirBuffer;

	if(!ExtractAll(inputPk2, tempFolder))
	{
		fs::remove_all(tempFolder);
		return false;
	}

	std::string normalized = NormalizeInternalFolder(internalFolder);
	fs::path destinationBase = tempFolder;
	if(!normalized.empty())
	{
		destinationBase /= fs::path(normalized);
	}
	if(!EnsureDirectory(destinationBase, m_error))
	{
		fs::remove_all(tempFolder);
		return false;
	}

	for(const auto & file : files)
	{
		if(!fs::exists(file) || !fs::is_regular_file(file))
		{
			m_error = "Invalid source file: " + file.string();
			fs::remove_all(tempFolder);
			return false;
		}
		fs::path destination = destinationBase / file.filename();
		std::error_code ec;
		fs::copy_file(file, destination, fs::copy_options::overwrite_existing, ec);
		if(ec)
		{
			m_error = "Could not copy file into extracted PK2 tree: " + file.string();
			fs::remove_all(tempFolder);
			return false;
		}
	}

	fs::path finalOutput = outputPk2;
	fs::path tempOutput = overwriteOutput ? (outputPk2.string() + ".tmpbuild") : outputPk2;
	if(!BuildFromFolder(tempFolder, tempOutput))
	{
		fs::remove_all(tempFolder);
		return false;
	}

	if(overwriteOutput)
	{
		std::error_code ec;
		fs::rename(tempOutput, finalOutput, ec);
		if(ec)
		{
			fs::remove(finalOutput, ec);
			fs::rename(tempOutput, finalOutput, ec);
			if(ec)
			{
				m_error = "Could not replace output PK2 file.";
				fs::remove_all(tempFolder);
				return false;
			}
		}
	}

	fs::remove_all(tempFolder);
	return true;
}


bool PK2Writer::ImportFolder(const fs::path & inputPk2,
	const fs::path & sourceFolder,
	const fs::path & outputPk2,
	bool overwriteOutput)
{
	return ImportFolder(inputPk2, sourceFolder, std::string(), outputPk2, overwriteOutput);
}


bool PK2Writer::ImportFolder(const fs::path & inputPk2,
	const fs::path & sourceFolder,
	const std::string & internalFolder,
	const fs::path & outputPk2,
	bool overwriteOutput)
{
	m_error.clear();
	if(!fs::exists(sourceFolder) || !fs::is_directory(sourceFolder))
	{
		m_error = "Selected source folder does not exist.";
		return false;
	}

	if(overwriteOutput && IsSamePath(inputPk2, outputPk2))
	{
		return DirectImportFolderInPlace(m_blowfish, inputPk2, sourceFolder, internalFolder, m_error);
	}

	char tempPathBuffer[MAX_PATH] = {0};
	GetTempPathA(MAX_PATH, tempPathBuffer);
	char tempDirBuffer[MAX_PATH] = {0};
	GetTempFileNameA(tempPathBuffer, "pk2", 0, tempDirBuffer);
	DeleteFileA(tempDirBuffer);
	CreateDirectoryA(tempDirBuffer, nullptr);
	fs::path tempFolder = tempDirBuffer;

	if(!ExtractAll(inputPk2, tempFolder))
	{
		fs::remove_all(tempFolder);
		return false;
	}

	std::string normalized = NormalizeInternalFolder(internalFolder);
	fs::path destinationRoot = tempFolder;
	if(!normalized.empty())
	{
		destinationRoot /= fs::path(normalized);
	}
	if(!EnsureDirectory(destinationRoot, m_error))
	{
		fs::remove_all(tempFolder);
		return false;
	}

	std::error_code ec;
	for(auto it = fs::recursive_directory_iterator(sourceFolder, ec); it != fs::recursive_directory_iterator(); it.increment(ec))
	{
		if(ec)
		{
			m_error = "Could not read source folder tree.";
			fs::remove_all(tempFolder);
			return false;
		}

		const fs::directory_entry & item = *it;
		fs::path relative = fs::relative(item.path(), sourceFolder, ec);
		if(ec)
		{
			m_error = "Could not calculate relative path for: " + item.path().string();
			fs::remove_all(tempFolder);
			return false;
		}

		fs::path destination = destinationRoot / relative;
		if(item.is_directory())
		{
			if(!EnsureDirectory(destination, m_error))
			{
				fs::remove_all(tempFolder);
				return false;
			}
		}
		else if(item.is_regular_file())
		{
			if(!EnsureDirectory(destination.parent_path(), m_error))
			{
				fs::remove_all(tempFolder);
				return false;
			}
			fs::copy_file(item.path(), destination, fs::copy_options::overwrite_existing, ec);
			if(ec)
			{
				m_error = "Could not copy file into extracted PK2 tree: " + item.path().string();
				fs::remove_all(tempFolder);
				return false;
			}
		}
	}

	fs::path finalOutput = outputPk2;
	fs::path tempOutput = overwriteOutput ? (outputPk2.string() + ".tmpbuild") : outputPk2;
	if(!BuildFromFolder(tempFolder, tempOutput))
	{
		fs::remove_all(tempFolder);
		return false;
	}

	if(overwriteOutput)
	{
		std::error_code renameError;
		fs::rename(tempOutput, finalOutput, renameError);
		if(renameError)
		{
			fs::remove(finalOutput, renameError);
			fs::rename(tempOutput, finalOutput, renameError);
			if(renameError)
			{
				m_error = "Could not replace output PK2 file.";
				fs::remove_all(tempFolder);
				return false;
			}
		}
	}

	fs::remove_all(tempFolder);
	return true;
}
