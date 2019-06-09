namespace VirtualFileSystem
{
    internal interface IPathValidator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pathToValidate"></param>
        /// <exception cref="InvalidPathException"></exception>
        void Validate(string pathToValidate);
    }
}