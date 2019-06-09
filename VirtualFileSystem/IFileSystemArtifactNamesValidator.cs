namespace VirtualFileSystem
{
    internal interface IFileSystemArtifactNamesValidator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="nameToValidate"></param>
        /// <exception cref="InvalidNameException"></exception>
        void Validate(string nameToValidate);
    }
}