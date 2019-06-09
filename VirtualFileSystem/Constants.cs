using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace VirtualFileSystem
{
    internal static class Constants
    {
        public const uint FileAndFolderMaximumNameLength = 255;
        public const int NumberOfBitsInByte = 8;
        public const int BytesToStoreChunkOfDataLength = sizeof(Int32);
        public const int BlockReferenceSizeInBytes = sizeof (Int32);
        public static readonly ReadOnlyCollection<char> IllegalCharactersForNames = new[] {'\t', '\n', '\r', VirtualFileSystem.DirectorySeparatorChar, '/', ';', '?', '*' }.ToList().AsReadOnly();
        public static readonly ReadOnlyCollection<char> IllegalCharactersForPaths = new[] {'\t', '\n', '\r', ';', '?', '*' }.ToList().AsReadOnly();
    }
}