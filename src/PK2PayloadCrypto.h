#pragma once
#include <cstddef>
#include <cstdint>
#include <string>
#include <vector>

bool PK2PayloadCrypto_ShouldDecrypt(const std::string& path);
void PK2PayloadCrypto_DecryptBuffer(char* buffer, int length);
void PK2PayloadCrypto_Initialize();

bool PK2PayloadCrypto_HasMarker(const uint8_t* reserved, size_t reservedLength);
void PK2PayloadCrypto_WriteMarker(uint8_t* reserved, size_t reservedLength);
void PK2PayloadCrypto_ClearMarker(uint8_t* reserved, size_t reservedLength);
void PK2PayloadCrypto_CryptBuffer(uint64_t filePosition, uint32_t fileSize, uint64_t streamOffset, void* buffer, size_t length);
void PK2PayloadCrypto_DecryptBufferForFile(uint64_t filePosition, uint32_t fileSize, void* buffer, size_t length);

bool PK2PayloadCrypto_TryGetLooseEncryptedPayloadSize(const std::string& path, uint32_t& payloadSize);
size_t PK2PayloadCrypto_LooseFooterSize();

bool PK2PayloadCrypto_TryDecryptLooseBuffer(std::vector<uint8_t>& buffer);
