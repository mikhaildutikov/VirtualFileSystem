using System;
using System.IO;
using VirtualFileSystem.Disk;
using VirtualFileSystem.DiskStructuresManagement;

namespace VirtualFileSystem.Tests.TestFactories
{
    /// <summary>
    /// Note: не устраняю здесь дублирование.
    /// </summary>
    internal static class TestCollaboratorsFactory
    {
        public static StructureBuilderTestCollaborators CreateCollaboratorsForTestingDataStreamStructureBuilder()
        {
            var collaborators = CreateAllCollaborators();

            var fileNode = collaborators.NodeResolver.ResolveFileNodeByPath(@"\hey");

            return new StructureBuilderTestCollaborators(fileNode, collaborators.Disk);
        }

        public static VirtualFileSystem CreateFileSystemFromExistingStream(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            VirtualDisk disk = VirtualDisk.CreateFromStream(stream);

            var diskStructuresManager = new FileSystemNodeStorage(disk);

            FileSystemHeader header = diskStructuresManager.ReadFileSystemHeader(VirtualDiskFormatter.FileSystemHeaderBlockIndex);

            var nameValidator = FileSystemArtifactNamesValidator.Default;

            var pathValidator = PathValidator.Default;

            var pathBuilder = PathBuilder.Default;

            var nodeResolver = new NodeResolver(disk, diskStructuresManager, StringComparer.OrdinalIgnoreCase, header.RootBlockOffset, VirtualFileSystem.Root, VirtualFileSystem.DirectorySeparatorChar, pathValidator, pathBuilder);

            return VirtualFileSystem.CreateFromDisk(disk, StringComparer.OrdinalIgnoreCase, nodeResolver, pathBuilder, nameValidator, pathValidator);
        }

        public static VirtualFileSystem CreateFileSystem(out Stream fileSystemStream)
        {
            var allCollaborators = CreateAllCollaborators();

            fileSystemStream = allCollaborators.Stream;

            allCollaborators.VirtualFileSystem.DeleteFile(allCollaborators.FileNodeFake.FullPath);

            return allCollaborators.VirtualFileSystem;
        }

        internal static AllCollaborators CreateAllCollaborators()
        {
            return CreateAllCollaborators(5000, true);
        }

        internal static AllCollaborators CreateAllCollaborators(int numberOfBlocks, bool createTestFile)
        {
            var stream = new MemoryStream();
                // new FileStream(@"e:\" + Guid.NewGuid().ToString("N"), FileMode.Create);

            var formatter = new VirtualDiskFormatter();

            VirtualDisk disk = VirtualDisk.CreateFormattingTheStream(stream, VirtualDisk.OnlySupportedBlockSize, VirtualDisk.OnlySupportedBlockSize * numberOfBlocks);

            var diskStructuresManager = new FileSystemNodeStorage(disk);

            formatter.Format(disk, diskStructuresManager);

            FileSystemHeader header = diskStructuresManager.ReadFileSystemHeader(VirtualDiskFormatter.FileSystemHeaderBlockIndex);

            var nameValidator = FileSystemArtifactNamesValidator.Default;

            var pathValidator = PathValidator.Default;

            var pathBuilder = PathBuilder.Default;

            var nodeResolver = new NodeResolver(disk, diskStructuresManager, StringComparer.OrdinalIgnoreCase, header.RootBlockOffset, VirtualFileSystem.Root, VirtualFileSystem.DirectorySeparatorChar, pathValidator, pathBuilder);

            var virtualFileSystem = VirtualFileSystem.CreateFromDisk(disk, StringComparer.OrdinalIgnoreCase, nodeResolver, pathBuilder, nameValidator, pathValidator);

            FileInfo fileNodeFake = null;

            if (createTestFile)
            {
                fileNodeFake = virtualFileSystem.CreateFile(@"\hey");
            }

            return new AllCollaborators(disk, diskStructuresManager, nameValidator, pathValidator, nodeResolver, virtualFileSystem, fileNodeFake, stream);
        }

        public static DataStreamTestCollaboratorSet CreateCollaboratorsForTestingDataStreams()
        {
            var collaborators = CreateAllCollaborators();

            return new DataStreamTestCollaboratorSet(collaborators.Disk, collaborators.DiskStructuresManager, collaborators.FileNodeFake, collaborators.VirtualFileSystem);
        }

        public static DataStreamTestCollaboratorSet CreateCollaboratorsForTestingDataStreamsOneGigabyteDrive()
        {
            var stream = new FileStream(@"e:\tests.vhd", FileMode.Create);

            var formatter = new VirtualDiskFormatter();

            VirtualDisk disk = VirtualDisk.CreateFormattingTheStream(stream, 2048, 2048 * 500000);

            var diskStructuresManager = new FileSystemNodeStorage(disk);

            formatter.Format(disk, diskStructuresManager);

            FileSystemHeader header = diskStructuresManager.ReadFileSystemHeader(VirtualDiskFormatter.FileSystemHeaderBlockIndex);

            var nameValidator = FileSystemArtifactNamesValidator.Default;

            var pathValidator = PathValidator.Default;

            var pathBuilder = PathBuilder.Default;

            var nodeResolver = new NodeResolver(disk, diskStructuresManager, StringComparer.OrdinalIgnoreCase, header.RootBlockOffset, VirtualFileSystem.Root, VirtualFileSystem.DirectorySeparatorChar, pathValidator, pathBuilder);

            var virtualFileSystem = VirtualFileSystem.CreateFromDisk(disk, StringComparer.OrdinalIgnoreCase, nodeResolver, pathBuilder, nameValidator, pathValidator);

            var fileNodeFake = virtualFileSystem.CreateFile(@"\hey");

            return new DataStreamTestCollaboratorSet(disk, diskStructuresManager, fileNodeFake, virtualFileSystem);
        }

        public static DataStreamReadableWritable CreateEmptyDataStream()
        {
            var testCollaborators = TestCollaboratorsFactory.CreateCollaboratorsForTestingDataStreams();

            return testCollaborators.VirtualFileSystem.OpenFileForWriting(testCollaborators.FileInfo.FullPath);
        }
    }
}