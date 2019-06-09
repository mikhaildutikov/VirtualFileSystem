using VirtualFileSystem.DiskBlockEnumeration;
using VirtualFileSystem.Toolbox;

namespace VirtualFileSystem.Addressing
{
    internal interface IDoubleIndirectDataStreamEnumerator : IEnumeratorAddressable<IDiskBlock>
    {
        bool IsEmpty { get; }
    }
}