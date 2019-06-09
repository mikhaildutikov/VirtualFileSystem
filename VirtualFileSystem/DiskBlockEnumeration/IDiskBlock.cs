namespace VirtualFileSystem.DiskBlockEnumeration
{
    internal interface IDiskBlock
    {
        int OccupiedSpaceInBytes { get; }
        int FreeSpaceInBytes { get; }
        int NumberOfBytesCanBeWrittenAtCurrentPosition { get; }
        bool CanAcceptBytesAtCurrentPosition { get; }
        int SizeInBytes { get; }
        int Position { get; set; }
        byte[] ReadAll();
        void WriteBytes(byte[] array, int startingPosition, int length);
        bool IsNull { get; }
        bool IsAtEndOfBlock { get; }
        bool IsAtEndOfReadableData { get; }
        int BlockIndex { get; }
    }
}