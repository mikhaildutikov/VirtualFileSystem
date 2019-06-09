using System;

namespace VirtualFileSystem.Tests.TestFactories
{
    internal static class VirtualFileSystemFactory
    {
        public static VirtualFileSystem CreateDefaultFileSystem()
        {
            var collaborators = TestCollaboratorsFactory.CreateAllCollaborators();

            collaborators.VirtualFileSystem.DeleteFile(collaborators.FileNodeFake.FullPath);

            return collaborators.VirtualFileSystem;
        }

        public static VirtualFileSystem CreateASystemWithSeveralFilesUnderGivenFolder(string folderPath, int numberOfFilesToPutThere)
        {
            VirtualFileSystem fileSystem = CreateDefaultFileSystem();

            var newFolder = fileSystem.CreateFolder(folderPath);

            for (int i = 0; i < numberOfFilesToPutThere; i++)
            {
                fileSystem.CreateFile(fileSystem.PathBuilder.CombinePaths(newFolder.FullPath, Guid.NewGuid().ToString("N")));
            }

            return fileSystem;
        }
    }
}