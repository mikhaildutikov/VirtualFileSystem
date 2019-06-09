using System.IO;
using VirtualFileSystem.Disk;

namespace VirtualFileSystem.Tests.TestFactories
{
    internal static class VirtualDiskTestFactory
    {
        public static int DefaultDiskBlockSize = VirtualDisk.OnlySupportedBlockSize;
        public static int DefaultDiskSize = DefaultDiskBlockSize * 300;

        public static VirtualDiskWithItsStream ConstructDefaultTestDiskWithStream()
        {
            var diskBackingStream = new MemoryStream();

            //diskBackingStream =
            //    System.IO.File.Open(Path.Combine(TestContext.TestDeploymentDir, Guid.NewGuid().ToString("N") + ".hdd"), FileMode.CreateNew, FileAccess.ReadWrite);

            VirtualDisk virtualDisk = VirtualDisk.CreateFormattingTheStream(diskBackingStream, DefaultDiskBlockSize, DefaultDiskSize);

            return new VirtualDiskWithItsStream(virtualDisk, diskBackingStream);
        }

        public static VirtualDisk ConstructDefaultTestDisk()
        {
            return ConstructDefaultTestDiskWithStream().Disk;
        }
    }
}