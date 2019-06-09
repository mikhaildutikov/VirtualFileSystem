using System;
using System.Collections.Generic;
using VirtualFileSystem.Toolbox.Extensions;

namespace VirtualFileSystem
{
    internal class FileSystemArtifactNamesValidator : IFileSystemArtifactNamesValidator
    {
        public static FileSystemArtifactNamesValidator Default = new FileSystemArtifactNamesValidator(Constants.IllegalCharactersForNames, Constants.FileAndFolderMaximumNameLength);

        private readonly IEnumerable<char> _illegalCharactersForNames;
        private readonly uint _maximumCharacterCount;

        public FileSystemArtifactNamesValidator(IEnumerable<char> illegalCharactersForNames, uint maximumCharacterCount)
        {
            if (illegalCharactersForNames == null) throw new ArgumentNullException("illegalCharactersForNames");

            if (illegalCharactersForNames.ContainsLetters())
            {
                throw new ArgumentException("В текущей версии не поддерживаются буквы в качестве элементов множества, определяющего символы, которые можно использовать в именах файлов, папок, в путях файловой системы");
            }

            _illegalCharactersForNames = illegalCharactersForNames;
            _maximumCharacterCount = maximumCharacterCount;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nameToValidate"></param>
        /// <exception cref="InvalidNameException"></exception>
        public void Validate(string nameToValidate)
        {
            if (String.IsNullOrEmpty(nameToValidate))
            {
                throw new InvalidNameException("Имя (\"{0}\") задано неверно: оно должно быть непустой строкой".FormatWith(nameToValidate));
            }

            if (nameToValidate.Length > _maximumCharacterCount)
            {
                throw new InvalidNameException("Максимальная длина имени файла или папки не должна превышать {0} символов".FormatWith(_maximumCharacterCount));
            }

            Nullable<char> charFound;

            if (nameToValidate.ContainsAny(_illegalCharactersForNames, out charFound))
            {
                throw new InvalidNameException("Имя (\"{0}\") задано неверно: оно содержит недопустимый символ \"{1}\"".FormatWith(nameToValidate, charFound.Value));
            }
        }
    }
}