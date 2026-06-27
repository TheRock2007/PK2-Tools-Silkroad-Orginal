using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PK2Encryptor;



internal enum ExplorerItemKind
{
    Up,
    LocalDirectory,
    LocalFile,
    Pk2Directory,
    Pk2File
}

internal sealed class ExplorerItemTag
{
    public ExplorerItemTag(ExplorerItemKind kind, string path)
    {
        Kind = kind;
        Path = path;
    }

    public ExplorerItemKind Kind { get; }
    public string Path { get; }
}

internal sealed class Pk2PreviewEntry
{
    public Pk2PreviewEntry(string path, long size, string state)
    {
        Path = path;
        Size = size;
        State = state;
    }

    public string Path { get; }
    public long Size { get; }
    public string State { get; }
}

public sealed partial class MainForm : Form
{
    #region Models and loose payload crypto

    private enum CryptoFileResult
    {
        Processed,
        AlreadyEncrypted,
        NotEncrypted,
        SkippedPlainType
    }


    private sealed class BuildPk2Job
    {
        public BuildPk2Job(string name, string sourceFolder, string outputFile, bool exists)
        {
            Name = name;
            SourceFolder = sourceFolder;
            OutputFile = outputFile;
            Exists = exists;
        }

        public string Name { get; }
        public string SourceFolder { get; }
        public string OutputFile { get; }
        public bool Exists { get; }
    }


    private static class LoosePayloadCrypto
    {
        private const int FooterSize = 32;
        private static readonly byte[] LooseMagic = Encoding.ASCII.GetBytes("GFXPK2LOOSEENC01");

        private static readonly byte[] PayloadKey =
        {
            0x7A, 0xD4, 0x19, 0xC8, 0x2E, 0x71, 0x5B, 0xA6,
            0x93, 0x0F, 0xE2, 0x44, 0xBC, 0x38, 0x6D, 0x10,
            0xF7, 0x5C, 0xA1, 0x29, 0xD0, 0x84, 0xEE, 0x63,
            0x16, 0x9B, 0x37, 0xC5, 0x48, 0xFA, 0x02, 0xB1
        };

        private static bool IsPlainExcludedFile(string path)
        {
            try
            {
                var normalized = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).ToLowerInvariant();
                var parts = normalized.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
                if(parts.Length == 0)
                {
                    return false;
                }

                var fileName = parts[^1];
                var mediaIndex = Array.LastIndexOf(parts, "media");
                var relativeParts = mediaIndex >= 0 ? parts.Skip(mediaIndex + 1).ToArray() : parts;
                var mediaRelative = string.Join('\\', relativeParts);

                if(mediaRelative.StartsWith("config\\", StringComparison.OrdinalIgnoreCase) ||
                   mediaRelative.StartsWith("external\\", StringComparison.OrdinalIgnoreCase) ||
                   mediaRelative.StartsWith("fonts\\", StringComparison.OrdinalIgnoreCase) ||
                   normalized.Contains("\\media\\config\\", StringComparison.OrdinalIgnoreCase) ||
                   normalized.Contains("\\media\\external\\", StringComparison.OrdinalIgnoreCase) ||
                   normalized.Contains("\\media\\fonts\\", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if(mediaRelative is "type.txt" or "gateport.txt" or "divisioninfo.txt" or "sv.t" ||
                   fileName is "type.txt" or "gateport.txt" or "divisioninfo.txt" or "sv.t")
                {
                    return true;
                }

                const string textDataPrefix = "server_dep\\silkroad\\textdata\\";
                if(mediaRelative.StartsWith(textDataPrefix, StringComparison.OrdinalIgnoreCase) ||
                   normalized.Contains("\\server_dep\\silkroad\\textdata\\", StringComparison.OrdinalIgnoreCase))
                {
                    return IsPlainTextDataFile(fileName);
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsPlainTextDataFile(string fileName)
        {
            string[] plainTextDataFiles =
            {
                "textdata_equip&skill.txt",
                "textdata_object.txt",
                "textdataname.txt",
                "textevent.txt",
                "texthelp.txt",
                "textquest.txt",
                "textquest_otherstring.txt",
                "textquest_queststring.txt",
                "textquest_speech&name.txt",
                "textuisystem.txt",
                "textzonename.txt"
            };

            foreach(var baseFile in plainTextDataFiles)
            {
                if(string.Equals(fileName, baseFile, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                var stem = Path.GetFileNameWithoutExtension(baseFile);
                if(fileName.StartsWith(stem + "_", StringComparison.OrdinalIgnoreCase) &&
                   fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) &&
                   fileName.Length > stem.Length + 5)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsEncrypted(string path, out ulong originalSize)
        {
            originalSize = 0;
            try
            {
                var info = new FileInfo(path);
                if(!info.Exists || info.Length < FooterSize)
                {
                    return false;
                }

                var footer = new byte[FooterSize];
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                stream.Position = stream.Length - FooterSize;
                if(stream.Read(footer, 0, footer.Length) != footer.Length)
                {
                    return false;
                }

                for(var i = 0; i < LooseMagic.Length; ++i)
                {
                    if(footer[i] != LooseMagic[i])
                    {
                        return false;
                    }
                }

                originalSize = BitConverter.ToUInt64(footer, 16);
                var version = BitConverter.ToUInt32(footer, 24);
                if(version != 1)
                {
                    return false;
                }
                return originalSize + FooterSize == (ulong)stream.Length;
            }
            catch
            {
                originalSize = 0;
                return false;
            }
        }

        public static CryptoFileResult EncryptFileInPlace(string path, IProgress<long> progress)
        {
            if(IsPlainExcludedFile(path))
            {
                progress.Report(GetLength(path));
                return CryptoFileResult.SkippedPlainType;
            }

            if(IsEncrypted(path, out _))
            {
                progress.Report(GetLength(path));
                return CryptoFileResult.AlreadyEncrypted;
            }

            var info = new FileInfo(path);
            if(!info.Exists)
            {
                throw new FileNotFoundException("File was not found.", path);
            }
            if(info.Length > uint.MaxValue)
            {
                throw new InvalidOperationException("PK2 payload encryption supports files up to 4 GB.");
            }

            var fileSize = (uint)info.Length;
            CryptPayloadRegion(path, fileSize, progress);
            AppendFooter(path, fileSize);
            progress.Report((long)fileSize + FooterSize);
            return CryptoFileResult.Processed;
        }

        public static CryptoFileResult DecryptFileInPlace(string path, IProgress<long> progress)
        {
            if(!IsEncrypted(path, out var originalSize))
            {
                progress.Report(GetLength(path));
                return CryptoFileResult.NotEncrypted;
            }
            if(originalSize > uint.MaxValue)
            {
                throw new InvalidOperationException("PK2 payload encryption supports files up to 4 GB.");
            }

            var fileSize = (uint)originalSize;
            CryptPayloadRegion(path, fileSize, progress);
            using(var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None))
            {
                stream.SetLength((long)originalSize);
                stream.Flush(true);
            }
            progress.Report((long)originalSize);
            return CryptoFileResult.Processed;
        }

        private static long GetLength(string path)
        {
            try
            {
                return new FileInfo(path).Length;
            }
            catch
            {
                return 0;
            }
        }

        private static void AppendFooter(string path, uint originalSize)
        {
            var footer = new byte[FooterSize];
            Buffer.BlockCopy(LooseMagic, 0, footer, 0, LooseMagic.Length);
            Buffer.BlockCopy(BitConverter.GetBytes((ulong)originalSize), 0, footer, 16, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(1u), 0, footer, 24, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(0u), 0, footer, 28, 4);

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None);
            stream.Position = originalSize;
            stream.Write(footer, 0, footer.Length);
            stream.Flush(true);
        }

        private static void CryptPayloadRegion(string path, uint fileSize, IProgress<long> progress)
        {
            var buffer = new byte[1024 * 1024];
            using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None, buffer.Length, FileOptions.SequentialScan);
            ulong streamOffset = 0;
            while(streamOffset < fileSize)
            {
                stream.Position = (long)streamOffset;
                var read = stream.Read(buffer, 0, (int)Math.Min((ulong)buffer.Length, (ulong)fileSize - streamOffset));
                if(read <= 0)
                {
                    break;
                }

                CryptBuffer(fileSize, streamOffset, buffer, read);
                stream.Position = (long)streamOffset;
                stream.Write(buffer, 0, read);
                streamOffset += (ulong)read;
                progress.Report((long)streamOffset);
            }
            stream.Flush(true);
        }

        private static void CryptBuffer(uint fileSize, ulong streamOffset, byte[] buffer, int length)
        {
            if(length <= 0)
            {
                return;
            }

            var blockCounter = streamOffset / 64;
            var blockOffset = (int)(streamOffset % 64);
            var keystream = new byte[64];
            var bufferOffset = 0;

            while(length > 0)
            {
                ChaCha20Block(fileSize, (uint)blockCounter, keystream);
                var take = Math.Min(length, 64 - blockOffset);
                for(var i = 0; i < take; ++i)
                {
                    buffer[bufferOffset + i] ^= keystream[blockOffset + i];
                }
                bufferOffset += take;
                length -= take;
                blockCounter++;
                blockOffset = 0;
            }
        }

        private static void ChaCha20Block(uint fileSize, uint counter, byte[] output)
        {
            var nonce0 = 0x32504B47u;
            var nonce1 = 0x31454647u;
            var nonce2 = 0xA7C35D91u ^ fileSize;

            uint[] state =
            {
                0x61707865u, 0x3320646Eu, 0x79622D32u, 0x6B206574u,
                Load32(PayloadKey, 0), Load32(PayloadKey, 4), Load32(PayloadKey, 8), Load32(PayloadKey, 12),
                Load32(PayloadKey, 16), Load32(PayloadKey, 20), Load32(PayloadKey, 24), Load32(PayloadKey, 28),
                counter, nonce0, nonce1, nonce2
            };

            var working = new uint[16];
            Array.Copy(state, working, state.Length);

            for(var i = 0; i < 10; ++i)
            {
                QuarterRound(ref working[0], ref working[4], ref working[8], ref working[12]);
                QuarterRound(ref working[1], ref working[5], ref working[9], ref working[13]);
                QuarterRound(ref working[2], ref working[6], ref working[10], ref working[14]);
                QuarterRound(ref working[3], ref working[7], ref working[11], ref working[15]);
                QuarterRound(ref working[0], ref working[5], ref working[10], ref working[15]);
                QuarterRound(ref working[1], ref working[6], ref working[11], ref working[12]);
                QuarterRound(ref working[2], ref working[7], ref working[8], ref working[13]);
                QuarterRound(ref working[3], ref working[4], ref working[9], ref working[14]);
            }

            for(var i = 0; i < 16; ++i)
            {
                Store32(output, i * 4, unchecked(working[i] + state[i]));
            }
        }

        private static uint Load32(byte[] source, int offset)
        {
            return (uint)source[offset]
                | ((uint)source[offset + 1] << 8)
                | ((uint)source[offset + 2] << 16)
                | ((uint)source[offset + 3] << 24);
        }

        private static void Store32(byte[] destination, int offset, uint value)
        {
            destination[offset] = (byte)value;
            destination[offset + 1] = (byte)(value >> 8);
            destination[offset + 2] = (byte)(value >> 16);
            destination[offset + 3] = (byte)(value >> 24);
        }

        private static uint Rotl32(uint value, int bits)
        {
            return (value << bits) | (value >> (32 - bits));
        }

        private static void QuarterRound(ref uint a, ref uint b, ref uint c, ref uint d)
        {
            unchecked
            {
                a += b; d ^= a; d = Rotl32(d, 16);
                c += d; b ^= c; b = Rotl32(b, 12);
                a += b; d ^= a; d = Rotl32(d, 8);
                c += d; b ^= c; b = Rotl32(b, 7);
            }
        }
    }

    #endregion
}
