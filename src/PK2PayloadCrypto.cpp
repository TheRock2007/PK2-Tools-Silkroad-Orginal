#include "PK2PayloadCrypto.h"

#include <algorithm>
#include <cstdio>
#include <cstring>
#include <limits>
#include <vector>

namespace
{
    const uint8_t kPayloadMarker[16] = {
        'G', 'F', 'X', 'P', 'K', '2', 'P', 'A',
        'Y', 'L', 'O', 'A', 'D', '0', '0', '1'
    };

    const uint8_t kLooseFileMarker[16] = {
        'G', 'F', 'X', 'P', 'K', '2', 'L', 'O',
        'O', 'S', 'E', 'E', 'N', 'C', '0', '1'
    };

    constexpr size_t kLooseFooterSize = 32;

    const uint8_t kPayloadKey[32] = {
        0x7A, 0xD4, 0x19, 0xC8, 0x2E, 0x71, 0x5B, 0xA6,
        0x93, 0x0F, 0xE2, 0x44, 0xBC, 0x38, 0x6D, 0x10,
        0xF7, 0x5C, 0xA1, 0x29, 0xD0, 0x84, 0xEE, 0x63,
        0x16, 0x9B, 0x37, 0xC5, 0x48, 0xFA, 0x02, 0xB1
    };

    uint32_t Load32LE(const uint8_t* p)
    {
        return static_cast<uint32_t>(p[0]) |
            (static_cast<uint32_t>(p[1]) << 8) |
            (static_cast<uint32_t>(p[2]) << 16) |
            (static_cast<uint32_t>(p[3]) << 24);
    }

    uint64_t Load64LE(const uint8_t* p)
    {
        return static_cast<uint64_t>(p[0]) |
            (static_cast<uint64_t>(p[1]) << 8) |
            (static_cast<uint64_t>(p[2]) << 16) |
            (static_cast<uint64_t>(p[3]) << 24) |
            (static_cast<uint64_t>(p[4]) << 32) |
            (static_cast<uint64_t>(p[5]) << 40) |
            (static_cast<uint64_t>(p[6]) << 48) |
            (static_cast<uint64_t>(p[7]) << 56);
    }

    void Store32LE(uint8_t* p, uint32_t v)
    {
        p[0] = static_cast<uint8_t>(v);
        p[1] = static_cast<uint8_t>(v >> 8);
        p[2] = static_cast<uint8_t>(v >> 16);
        p[3] = static_cast<uint8_t>(v >> 24);
    }

    uint32_t Rotl32(uint32_t v, int bits)
    {
        return (v << bits) | (v >> (32 - bits));
    }

    void QuarterRound(uint32_t& a, uint32_t& b, uint32_t& c, uint32_t& d)
    {
        a += b; d ^= a; d = Rotl32(d, 16);
        c += d; b ^= c; b = Rotl32(b, 12);
        a += b; d ^= a; d = Rotl32(d, 8);
        c += d; b ^= c; b = Rotl32(b, 7);
    }

    void BuildNonce(uint64_t filePosition, uint32_t fileSize, uint32_t nonce[3])
    {
        nonce[0] = 0x32504B47u ^ static_cast<uint32_t>(filePosition & 0xFFFFFFFFULL);
        nonce[1] = 0x31454647u ^ static_cast<uint32_t>((filePosition >> 32) & 0xFFFFFFFFULL);
        nonce[2] = 0xA7C35D91u ^ fileSize;
    }

    void ChaCha20Block(uint64_t filePosition, uint32_t fileSize, uint32_t counter, uint8_t output[64])
    {
        uint32_t nonce[3];
        BuildNonce(filePosition, fileSize, nonce);

        uint32_t state[16] = {
            0x61707865u, 0x3320646Eu, 0x79622D32u, 0x6B206574u,
            Load32LE(kPayloadKey + 0), Load32LE(kPayloadKey + 4),
            Load32LE(kPayloadKey + 8), Load32LE(kPayloadKey + 12),
            Load32LE(kPayloadKey + 16), Load32LE(kPayloadKey + 20),
            Load32LE(kPayloadKey + 24), Load32LE(kPayloadKey + 28),
            counter, nonce[0], nonce[1], nonce[2]
        };

        uint32_t working[16];
        memcpy(working, state, sizeof(working));

        for(int i = 0; i < 10; ++i)
        {
            QuarterRound(working[0], working[4], working[8], working[12]);
            QuarterRound(working[1], working[5], working[9], working[13]);
            QuarterRound(working[2], working[6], working[10], working[14]);
            QuarterRound(working[3], working[7], working[11], working[15]);
            QuarterRound(working[0], working[5], working[10], working[15]);
            QuarterRound(working[1], working[6], working[11], working[12]);
            QuarterRound(working[2], working[7], working[8], working[13]);
            QuarterRound(working[3], working[4], working[9], working[14]);
        }

        for(int i = 0; i < 16; ++i)
        {
            Store32LE(output + (i * 4), working[i] + state[i]);
        }
    }
}

void PK2PayloadCrypto_Initialize()
{
}

bool PK2PayloadCrypto_ShouldDecrypt(const std::string& path)
{
    (void)path;
    return true;
}

void PK2PayloadCrypto_DecryptBuffer(char* buffer, int length)
{
    if(!buffer || length <= 0)
    {
        return;
    }
    PK2PayloadCrypto_CryptBuffer(0, static_cast<uint32_t>(length), 0, buffer, static_cast<size_t>(length));
}

bool PK2PayloadCrypto_HasMarker(const uint8_t* reserved, size_t reservedLength)
{
    if(!reserved || reservedLength < sizeof(kPayloadMarker))
    {
        return false;
    }
    return memcmp(reserved, kPayloadMarker, sizeof(kPayloadMarker)) == 0;
}

void PK2PayloadCrypto_WriteMarker(uint8_t* reserved, size_t reservedLength)
{
    if(!reserved || reservedLength < sizeof(kPayloadMarker))
    {
        return;
    }
    memcpy(reserved, kPayloadMarker, sizeof(kPayloadMarker));
    if(reservedLength > sizeof(kPayloadMarker))
    {
        reserved[sizeof(kPayloadMarker)] = 1;
    }
}

void PK2PayloadCrypto_ClearMarker(uint8_t* reserved, size_t reservedLength)
{
    if(!reserved || reservedLength == 0)
    {
        return;
    }
    const size_t clearBytes = std::min(reservedLength, static_cast<size_t>(32));
    memset(reserved, 0, clearBytes);
}

void PK2PayloadCrypto_CryptBuffer(uint64_t filePosition, uint32_t fileSize, uint64_t streamOffset, void* buffer, size_t length)
{
    if(!buffer || length == 0)
    {
        return;
    }

    uint8_t* bytes = static_cast<uint8_t*>(buffer);
    uint64_t blockCounter = streamOffset / 64;
    size_t blockOffset = static_cast<size_t>(streamOffset % 64);
    uint8_t keystream[64];

    while(length > 0)
    {
        ChaCha20Block(filePosition, fileSize, static_cast<uint32_t>(blockCounter), keystream);
        const size_t take = std::min(length, static_cast<size_t>(64 - blockOffset));
        for(size_t i = 0; i < take; ++i)
        {
            bytes[i] ^= keystream[blockOffset + i];
        }
        bytes += take;
        length -= take;
        ++blockCounter;
        blockOffset = 0;
    }
}

void PK2PayloadCrypto_DecryptBufferForFile(uint64_t filePosition, uint32_t fileSize, void* buffer, size_t length)
{
    PK2PayloadCrypto_CryptBuffer(filePosition, fileSize, 0, buffer, length);
}


bool PK2PayloadCrypto_TryDecryptLooseBuffer(std::vector<uint8_t>& buffer)
{
    if(buffer.size() < kLooseFooterSize)
    {
        return false;
    }

    const size_t footerOffset = buffer.size() - kLooseFooterSize;
    const uint8_t* footer = buffer.data() + footerOffset;
    if(memcmp(footer, kLooseFileMarker, sizeof(kLooseFileMarker)) != 0)
    {
        return false;
    }

    const uint64_t originalSize = Load64LE(footer + 16);
    const uint32_t version = Load32LE(footer + 24);
    if(version != 1 || originalSize > std::numeric_limits<uint32_t>::max())
    {
        return false;
    }
    if(originalSize + kLooseFooterSize != static_cast<uint64_t>(buffer.size()))
    {
        return false;
    }

    if(originalSize > 0)
    {
        PK2PayloadCrypto_CryptBuffer(0, static_cast<uint32_t>(originalSize), 0, buffer.data(), static_cast<size_t>(originalSize));
    }
    buffer.resize(static_cast<size_t>(originalSize));
    return true;
}

bool PK2PayloadCrypto_TryGetLooseEncryptedPayloadSize(const std::string& path, uint32_t& payloadSize)
{
    payloadSize = 0;

    FILE* file = nullptr;
    fopen_s(&file, path.c_str(), "rb");
    if(!file)
    {
        return false;
    }

    if(_fseeki64(file, 0, SEEK_END) != 0)
    {
        fclose(file);
        return false;
    }
    const int64_t physicalSize = _ftelli64(file);
    if(physicalSize < static_cast<int64_t>(kLooseFooterSize))
    {
        fclose(file);
        return false;
    }

    if(_fseeki64(file, physicalSize - static_cast<int64_t>(kLooseFooterSize), SEEK_SET) != 0)
    {
        fclose(file);
        return false;
    }

    uint8_t footer[kLooseFooterSize] = {0};
    if(fread(footer, 1, sizeof(footer), file) != sizeof(footer))
    {
        fclose(file);
        return false;
    }
    fclose(file);

    if(memcmp(footer, kLooseFileMarker, sizeof(kLooseFileMarker)) != 0)
    {
        return false;
    }

    const uint64_t originalSize = Load64LE(footer + 16);
    const uint32_t version = Load32LE(footer + 24);
    if(version != 1 || originalSize > std::numeric_limits<uint32_t>::max())
    {
        return false;
    }
    if(originalSize + kLooseFooterSize != static_cast<uint64_t>(physicalSize))
    {
        return false;
    }

    payloadSize = static_cast<uint32_t>(originalSize);
    return true;
}

size_t PK2PayloadCrypto_LooseFooterSize()
{
    return kLooseFooterSize;
}
