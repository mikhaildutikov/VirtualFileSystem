namespace VirtualFileSystem.Toolbox
{
    internal class AddressingSystemBlockSizes
    {
        public AddressingSystemBlockSizes(int doubleIndirectBlockSize, int lastSingleIndirectBlockSize)
        {
            MethodArgumentValidator.ThrowIfNegative(doubleIndirectBlockSize, "doubleIndirectBlockSize");
            MethodArgumentValidator.ThrowIfNegative(lastSingleIndirectBlockSize, "lastSingleIndirectBlockSize");

            DoubleIndirectBlockSize = doubleIndirectBlockSize;
            LastSingleIndirectBlockSize = lastSingleIndirectBlockSize;
        }

        public int DoubleIndirectBlockSize { get; private set; }
        public int LastSingleIndirectBlockSize { get; private set; }
    }
}