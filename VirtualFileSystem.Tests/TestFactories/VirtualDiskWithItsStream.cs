using System;
using System.IO;
using VirtualFileSystem.Disk;

namespace VirtualFileSystem.Tests.TestFactories
{
    internal class VirtualDiskWithItsStream
    {
        public VirtualDiskWithItsStream(VirtualDisk virtualDisk, Stream diskBackingStream)
        {
            if (virtualDisk == null)
            {
                throw new ArgumentNullException("virtualDisk");
            }

            if (diskBackingStream == null)
            {
                throw new ArgumentNullException("diskBackingStream");
            }

            Disk = virtualDisk;
            BackingStream = diskBackingStream;
        }

        public VirtualDisk Disk
        {
            get;
            private set;
        }

        public Stream BackingStream
        {
            get;
            private set;
        }
    }
}