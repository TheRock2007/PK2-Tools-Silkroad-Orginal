#include "PK2Reader.h"
#include <stdio.h>
#include <stdlib.h>
#include <algorithm>
#include <set>
#include <cctype>
#include "shared_io.h"
#include "../PK2PayloadCrypto.h"


int MakePathSlashWindows_1(int ch)
{
	return ch == '/' ? '\\' : ch;
}

//-----------------------------------------------------------------------------

// I use different variations of this code depending on the program, so it's not going
// to be in a common file.
std::list<std::string> TokenizeString_1(const std::string& str, const std::string& delim)
{
	// http://www.gamedev.net/community/forums/topic.asp?topic_id=381544#TokenizeString
	using namespace std;
	list<string> tokens;
	size_t p0 = 0, p1 = string::npos;
	while(p0 != string::npos)
	{
		p1 = str.find_first_of(delim, p0);
		if(p1 != p0)
		{
			string token = str.substr(p0, p1 - p0);
			tokens.push_back(token);
		}
		p0 = str.find_first_not_of(delim, p1);
	}
	return tokens;
}

//-----------------------------------------------------------------------------

void PK2Reader::Cache(std::string & base_name, PK2Entry & e)
{
	m_cache[base_name] = e;
}

//-----------------------------------------------------------------------------

PK2Reader::PK2Reader()
{
	m_root_offset = 0;
	m_file_size = 0;
	m_file = 0;
	memset(&m_header, 0, sizeof(PK2Header));
	SetDecryptionKey();
}

//-----------------------------------------------------------------------------

PK2Reader::~PK2Reader()
{
	Close();
}

//-----------------------------------------------------------------------------

size_t PK2Reader::GetCacheSize()
{
	return m_cache.size();
}

//-----------------------------------------------------------------------------

void PK2Reader::ClearCache()
{
	m_cache.clear();
}

//-----------------------------------------------------------------------------

std::string PK2Reader::GetError()
{
	std::string e = m_error.str();
	m_error.str("");
	return e;
}

//-----------------------------------------------------------------------------

void PK2Reader::SetDecryptionKey(char * ascii_key, uint8_t ascii_key_length, char * base_key, uint8_t base_key_length)
{
	if(ascii_key_length > 56)
	{
		ascii_key_length = 56;
	}

	uint8_t bf_key[56] = { 0x00 };

	uint8_t a_key[56] = { 0x00 };
	memcpy(a_key, ascii_key, ascii_key_length);

	uint8_t b_key[56] = { 0x00 };
	memcpy(b_key, base_key, base_key_length);

	for(int x = 0; x < ascii_key_length; ++x)
	{
		bf_key[x] = a_key[x] ^ b_key[x];
	}

	m_blowfish.Initialize(bf_key, ascii_key_length);
}


//-----------------------------------------------------------------------------

bool PK2Reader::IsSafeOffset(int64_t offset, size_t bytes) const
{
	if(offset < 0)
	{
		return false;
	}
	if(offset < m_root_offset)
	{
		return false;
	}
	if(offset > m_file_size)
	{
		return false;
	}
	if(bytes > 0)
	{
		if(static_cast<uint64_t>(offset) + static_cast<uint64_t>(bytes) > static_cast<uint64_t>(m_file_size))
		{
			return false;
		}
	}
	return true;
}

//-----------------------------------------------------------------------------

bool PK2Reader::ReadEntryBlockAt(int64_t offset, PK2EntryBlock & block)
{
	return ReadEntryBlockAt(offset, block, m_header.encryption != 0);
}

//-----------------------------------------------------------------------------

bool PK2Reader::ReadEntryBlockAt(int64_t offset, PK2EntryBlock & block, bool decryptEntries)
{
	if(!IsSafeOffset(offset, sizeof(PK2EntryBlock)))
	{
		m_error.str(""); m_error << "Invalid PK2 entry block offset.";
		return false;
	}

	if(file_seek(m_file, offset, SEEK_SET) != 0)
	{
		m_error.str(""); m_error << "Invalid seek index.";
		return false;
	}

	size_t read_count = fread(&block, 1, sizeof(PK2EntryBlock), m_file);
	if(read_count != sizeof(PK2EntryBlock))
	{
		m_error.str(""); m_error << "Could not read a PK2EntryBlock object";
		return false;
	}

	for(int x = 0; x < 20; ++x)
	{
		PK2Entry & e = block.entries[x];
		if(decryptEntries)
		{
			PK2Entry decoded{};
			m_blowfish.Decode(&e, sizeof(PK2Entry), &decoded, sizeof(PK2Entry));
			e = decoded;
		}
		if(e.type != 0 && (e.padding[0] != 0 || e.padding[1] != 0))
		{
			m_error.str(""); m_error << "The padding is not NULL. User seek error.";
			return false;
		}
	}

	return true;
}

//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------

bool PK2Reader::ValidateEntry(const PK2Entry & entry, bool allowNull, bool & isDotEntry)
{
	isDotEntry = false;
	if(allowNull && entry.type == 0)
	{
		return true;
	}
	if(entry.type != 1 && entry.type != 2)
	{
		m_error.str(""); m_error << "Unsupported PK2 entry type.";
		return false;
	}

	bool nullFound = false;
	for(size_t i = 0; i < sizeof(entry.name); ++i)
	{
		unsigned char ch = static_cast<unsigned char>(entry.name[i]);
		if(ch == 0)
		{
			nullFound = true;
			break;
		}
		if(ch < 32)
		{
			m_error.str(""); m_error << "Invalid characters in PK2 entry name.";
			return false;
		}
	}
	if(!nullFound)
	{
		m_error.str(""); m_error << "Unterminated PK2 entry name.";
		return false;
	}

	if(strcmp(entry.name, ".") == 0 || strcmp(entry.name, "..") == 0)
	{
		isDotEntry = true;
		return true;
	}

	if(entry.position < 0)
	{
		m_error.str(""); m_error << "Negative PK2 entry position.";
		return false;
	}
	if(entry.type == 1)
	{
		if(!IsSafeOffset(entry.position, sizeof(PK2EntryBlock)))
		{
			m_error.str(""); m_error << "Invalid folder entry position.";
			return false;
		}
	}
	else
	{
		if(entry.size > 0 && !IsSafeOffset(entry.position, entry.size))
		{
			m_error.str(""); m_error << "Invalid file entry payload range.";
			return false;
		}
	}
	if(entry.nextChain != 0 && !IsSafeOffset(entry.nextChain, sizeof(PK2EntryBlock)))
	{
		m_error.str(""); m_error << "Invalid nextChain pointer.";
		return false;
	}
	return true;
}

//-----------------------------------------------------------------------------

bool PK2Reader::ProbeEntryBlockAt(int64_t offset, bool decryptEntries)
{
	std::string oldError = m_error.str();
	PK2EntryBlock block{};
	if(!ReadEntryBlockAt(offset, block, decryptEntries))
	{
		m_error.str(oldError);
		return false;
	}

	bool hasUsableEntry = false;
	for(int i = 0; i < 20; ++i)
	{
		bool isDotEntry = false;
		if(!ValidateEntry(block.entries[i], true, isDotEntry))
		{
			m_error.str(oldError);
			return false;
		}
		if(block.entries[i].type == 1 || block.entries[i].type == 2)
		{
			hasUsableEntry = true;
		}
	}

	m_error.str(oldError);
	return hasUsableEntry;
}

//-----------------------------------------------------------------------------

bool PK2Reader::VerifyCurrentKeyByHeaderOrRoot()
{
	if(m_header.encryption == 0)
	{
		return true;
	}

	uint8_t verify[16] = {0};
	m_blowfish.Encode("Joymax Pak File", 16, verify, 16);

	// Silkroad PK2 files only rely on the first three bytes of this value.
	// Several private clients leave the remaining bytes dirty, so comparing all
	// sixteen bytes rejects valid archives even though the directory blocks can
	// be decoded correctly.
	if(memcmp(verify, m_header.verify, 3) == 0)
	{
		return true;
	}

	// Some patched clients keep a non-standard verify field.  In that case use
	// the encrypted root block itself as the proof that the current Blowfish key
	// is correct.
	return ProbeEntryBlockAt(m_root_offset, true);
}

//-----------------------------------------------------------------------------

bool PK2Reader::TryOpenWithKnownKeyFallbacks()
{
	if(m_header.encryption == 0)
	{
		if(ProbeEntryBlockAt(m_root_offset, false))
		{
			return true;
		}

		// Header flag is wrong or stripped, but directory entries are encrypted.
		// Try the known Silkroad keys and mark this instance as encrypted if one
		// decodes the root directory safely.
		SetDecryptionKey((char*)"169841", 6);
		if(ProbeEntryBlockAt(m_root_offset, true))
		{
			m_header.encryption = 1;
			return true;
		}

		SetDecryptionKey((char*)"\x32\x30\x30\x39\xC4\xEA", 6);
		if(ProbeEntryBlockAt(m_root_offset, true))
		{
			m_header.encryption = 1;
			return true;
		}

		m_error.str(""); m_error << "Invalid PK2 directory header.";
		return false;
	}

	if(VerifyCurrentKeyByHeaderOrRoot())
	{
		return true;
	}

	SetDecryptionKey((char*)"169841", 6);
	if(VerifyCurrentKeyByHeaderOrRoot())
	{
		return true;
	}

	SetDecryptionKey((char*)"\x32\x30\x30\x39\xC4\xEA", 6);
	if(VerifyCurrentKeyByHeaderOrRoot())
	{
		return true;
	}

	m_error.str(""); m_error << "Invalid Blowfish key.";
	return false;
}

//-----------------------------------------------------------------------------

void PK2Reader::Close()
{
	if(m_file)
	{
		fclose(m_file);
		m_file = 0;
	}

	m_cache.clear();
	m_root_offset = 0;
	m_file_size = 0;
	m_error.str("");
	memset(&m_header, 0, sizeof(PK2Header));
}

//-----------------------------------------------------------------------------

bool PK2Reader::HasPayloadEncryption() const
{
	return PK2PayloadCrypto_HasMarker(m_header.reserved, sizeof(m_header.reserved));
}

//-----------------------------------------------------------------------------

PK2Header PK2Reader::GetHeader() const
{
	return m_header;
}

//-----------------------------------------------------------------------------

bool PK2Reader::Open(const char * filename)
{
	size_t read_count = 0;

	if(m_file != 0)
	{
		m_error.str(""); m_error << "There is already a PK2 opened.";
		return false;
	}

	fopen_s(&m_file, filename, "rb");
	if(m_file == 0)
	{
		m_error.str(""); m_error << "Could not open the file \"" << filename << "\".";
		return false;
	}

	read_count = fread(&m_header, 1, sizeof(PK2Header), m_file);
	if(read_count != sizeof(PK2Header))
	{
		fclose(m_file);
		m_file = 0;
		m_error.str(""); m_error << "Could not read in the PK2Header.";
		return false;
	}

	if(m_header.version != 0x01000002)
	{
		PK2Header rawHeader = m_header;
		auto tryDecodeHeaderWithCurrentKey = [&]() -> bool
		{
			PK2Header decoded{};
			m_blowfish.Decode(&rawHeader, sizeof(PK2Header), &decoded, sizeof(PK2Header));
			if(decoded.version == 0x01000002)
			{
				m_header = decoded;
				m_header.encryption = 1;
				return true;
			}
			return false;
		};

		bool decodedHeader = tryDecodeHeaderWithCurrentKey();
		if(!decodedHeader)
		{
			SetDecryptionKey((char*)"169841", 6);
			decodedHeader = tryDecodeHeaderWithCurrentKey();
		}
		if(!decodedHeader)
		{
			SetDecryptionKey((char*)"\x32\x30\x30\x39\xC4\xEA", 6);
			decodedHeader = tryDecodeHeaderWithCurrentKey();
		}
		if(!decodedHeader)
		{
			fclose(m_file);
			m_file = 0;
			m_error.str(""); m_error << "Invalid PK2 version.";
			return false;
		}
	}

	if(file_seek(m_file, 0, SEEK_END) != 0)
	{
		fclose(m_file);
		m_file = 0;
		m_error.str(""); m_error << "Could not seek to the end of the PK2 file.";
		return false;
	}
	m_file_size = file_tell(m_file);
	m_root_offset = sizeof(PK2Header);

	if(!IsSafeOffset(m_root_offset, sizeof(PK2EntryBlock)))
	{
		fclose(m_file);
		m_file = 0;
		m_error.str(""); m_error << "The PK2 file is truncated or invalid.";
		return false;
	}

	if(!TryOpenWithKnownKeyFallbacks())
	{
		std::string error = m_error.str();
		fclose(m_file);
		m_file = 0;
		m_error.str("");
		m_error << (error.empty() ? "Could not decode PK2 directory header." : error);
		return false;
	}

	return true;
}

//-----------------------------------------------------------------------------

bool PK2Reader::GetEntries(PK2Entry & parent, std::list<PK2Entry> & entries)
{
	if(m_file == 0)
	{
		m_error.str(""); m_error << "There is no PK2 loaded yet.";
		return false;
	}

	if(parent.type == 0 && parent.position == 0)
	{
		parent.type = 1;
		parent.position = m_root_offset;
	}

	if(parent.type != 1)
	{
		m_error.str(""); m_error << "Invalid entry type. Only folders are allowed.";
		return false;
	}

	int64_t currentOffset = parent.position;
	std::set<int64_t> visited;

	while(true)
	{
		if(!visited.insert(currentOffset).second)
		{
			m_error.str(""); m_error << "Detected a cyclic PK2 directory chain.";
			return false;
		}

		PK2EntryBlock block;
		if(!ReadEntryBlockAt(currentOffset, block))
		{
			return false;
		}

		for(int x = 0; x < 20; ++x)
		{
			PK2Entry & e = block.entries[x];
			bool isDotEntry = false;
			if(!ValidateEntry(e, true, isDotEntry))
			{
				return false;
			}
			if(e.type == 1 || e.type == 2)
			{
				entries.push_back(e);
			}
		}

		if(block.entries[19].nextChain)
		{
			currentOffset = block.entries[19].nextChain;
		}
		else
		{
			break;
		}
	}

	return true;
}

//-----------------------------------------------------------------------------

bool PK2Reader::GetEntry(const char * pathname, PK2Entry & entry)
{
	if(m_file == 0)
	{
		m_error.str(""); m_error << "There is no PK2 loaded yet.";
		return false;
	}

	std::string base_name = pathname ? pathname : "";
	std::transform(base_name.begin(), base_name.end(), base_name.begin(), tolower);
	std::transform(base_name.begin(), base_name.end(), base_name.begin(), MakePathSlashWindows_1);

	while(!base_name.empty() && base_name.front() == '\\')
	{
		base_name.erase(base_name.begin());
	}
	while(!base_name.empty() && base_name.back() == '\\')
	{
		base_name.pop_back();
	}

	if(base_name.empty() || base_name == ".")
	{
		entry = PK2Entry{};
		entry.type = 1;
		entry.position = m_root_offset;
		Cache(base_name, entry);
		return true;
	}

	std::list<std::string> tokens = TokenizeString_1(base_name, "\\");

	// Check the cache first so we can save some time on frequent accesses
	std::map<std::string, PK2Entry>::iterator itr = m_cache.find(base_name);
	if(itr != m_cache.end())
	{
		entry = itr->second;
		return true;
	}

	PK2EntryBlock block;
	size_t read_count = 0;
	std::string name;

	if(entry.position == 0)
	{
		if(file_seek(m_file, m_root_offset, SEEK_SET) != 0)
		{
			m_error.str(""); m_error << "Invalid seek index.";
			return false;
		}
	}
	else
	{
		if(file_seek(m_file, entry.position, SEEK_SET) != 0)
		{
			m_error.str(""); m_error << "Invalid seek index.";
			return false;
		}
	}

	while(!tokens.empty())
	{
		std::string path = tokens.front();
		tokens.pop_front();

		read_count = fread(&block, 1, sizeof(PK2EntryBlock), m_file);
		if(read_count != sizeof(PK2EntryBlock))
		{
			m_error.str(""); m_error << "Could not read a PK2EntryBlock object";
			return false;
		}

		bool cycle = false;

		for(int x = 0; x < 20; ++x)
		{
			PK2Entry & e = block.entries[x];

			// I opt to decode entries as we process them rather than before hand to save 'some' processing
			// from extra entries we don't have to search.
			if(m_header.encryption)
			{
				m_blowfish.Decode(&e, sizeof(PK2Entry), &e, sizeof(PK2Entry));
			}

			// Protect against possible user seeking errors
			if(e.padding[0] != 0 || e.padding[1] != 0)
			{
				m_error.str(""); m_error << "The padding is not NULL. User seek error.";
				return false;
			}

			if(e.type == 0)
			{
				continue;
			}

			// Incurs some overhead in the long run, but the convenience it gives of not knowing exact
			// case is well worth it!
			name = e.name;
			std::transform(name.begin(), name.end(), name.begin(), tolower);

			if(name == path)
			{
				// We are at the end of the list of paths to find
				if(tokens.empty())
				{
					entry = e;
					Cache(base_name, e);
					return true;
				}
				else
				{
					// We want to make sure we only search folders, otherwise
					// bugs could result.
					if(e.type == 1)
					{
						if(file_seek(m_file, e.position, SEEK_SET) != 0)
						{
							m_error.str(""); m_error << "Invalid seek index.";
							return false;
						}
						cycle = true;
						break;
					}

					m_error.str(""); m_error << "Invalid entry, files cannot have children!";

					// Invalid entry (files can't have children!)
					return false;
				}
			}
		}

		// We found a path entry, continue down the list
		if(cycle)
		{
			continue;
		}

		// More entries to search in the current directory
		if(block.entries[19].nextChain)
		{
			if(file_seek(m_file, block.entries[19].nextChain, SEEK_SET) != 0)
			{
				m_error.str(""); m_error << "Invalid seek index for nextChain.";
				return false;
			}
			tokens.push_front(path);
			continue;
		}

		// If we get here, what we looking for does not exist
		break;
	}

	m_error.str(""); m_error << "The entry does not exist";

	return false;
}

//-----------------------------------------------------------------------------

bool PK2Reader::ForEachEntryDo(bool (* UserFunc)(PK2Reader *, const std::string &, PK2EntryBlock &, void *), void * userdata)
{
	if(m_file == 0)
	{
		m_error.str(""); m_error << "There is no PK2 loaded yet.";
		return false;
	}

	std::list<PK2Entry> folders;
	std::list<std::string> paths;
	std::set<int64_t> visitedOffsets;

	PK2Entry root{};
	root.type = 1;
	root.position = m_root_offset;
	folders.push_back(root);
	paths.push_back(std::string());

	while(!folders.empty())
	{
		PK2Entry folder = folders.front();
		folders.pop_front();
		std::string path = paths.front();
		paths.pop_front();

		if(!visitedOffsets.insert(folder.position).second)
		{
			continue;
		}

		int64_t currentOffset = folder.position;
		std::set<int64_t> localChainVisited;

		while(true)
		{
			if(!localChainVisited.insert(currentOffset).second)
			{
				m_error.str(""); m_error << "Detected a cyclic PK2 directory chain.";
				return false;
			}

			PK2EntryBlock block;
			if(!ReadEntryBlockAt(currentOffset, block))
			{
				return false;
			}

			for(int x = 0; x < 20; ++x)
			{
				PK2Entry & e = block.entries[x];
				bool isDotEntry = false;
				if(!ValidateEntry(e, true, isDotEntry))
				{
					return false;
				}
				if(e.type == 1 && !isDotEntry)
				{
					std::string cpath = path.empty() ? std::string(e.name) : (path + "\\" + e.name);
					folders.push_back(e);
					paths.push_back(cpath);
				}
			}

			if((*UserFunc)(this, path, block, userdata) == false)
			{
				return true;
			}

			if(block.entries[19].nextChain)
			{
				currentOffset = block.entries[19].nextChain;
			}
			else
			{
				break;
			}
		}
	}

	return true;
}

//-----------------------------------------------------------------------------

bool PK2Reader::ExtractToMemory(PK2Entry & entry, std::vector<uint8_t> & buffer)
{
	if(entry.type != 2)
	{
		m_error.str(""); m_error << "The entry is not a file.";
		return false;
	}
	if(entry.position < 0 || (entry.size > 0 && !IsSafeOffset(entry.position, entry.size)))
	{
		m_error.str(""); m_error << "Invalid file entry payload range.";
		return false;
	}
	buffer.resize(entry.size);
	if(buffer.empty())
	{
		return true;
	}
	file_seek(m_file, entry.position, SEEK_SET);
	size_t read_count = fread(&buffer[0], 1, entry.size, m_file);
	if(read_count != entry.size)
	{
		buffer.clear();
		m_error.str(""); m_error << "Could read all of the file data.";
		return false;
	}
	return true;
}

//-----------------------------------------------------------------------------
