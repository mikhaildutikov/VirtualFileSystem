using System;
using System.Collections.Generic;
using System.Linq;
using VirtualFileSystem.Toolbox.Extensions;

namespace VirtualFileSystem
{
    internal class PathValidator : IPathValidator
    {
        private readonly string _rootPath;
        private readonly IEnumerable<char> _illegalCharactersForPath;
        private readonly IFileSystemArtifactNamesValidator _nameValidator;
        private readonly char _directorySeparatorChar;

        public static readonly PathValidator Default = new PathValidator(VirtualFileSystem.Root,
                                                                Constants.IllegalCharactersForPaths,
                                                                FileSystemArtifactNamesValidator.Default,
                                                                VirtualFileSystem.DirectorySeparatorChar);

        public PathValidator(string rootPath, IEnumerable<char> illegalCharactersForPath, IFileSystemArtifactNamesValidator nameValidator, char directorySeparatorChar)
        {
            if (illegalCharactersForPath == null) throw new ArgumentNullException("illegalCharactersForPath");
            if (nameValidator == null) throw new ArgumentNullException("nameValidator");
            
            if (String.IsNullOrEmpty(rootPath))
            {
                throw new ArgumentNullException("rootPath");
            }

            if (illegalCharactersForPath.ContainsLetters())
            {
                throw new ArgumentException("В текущей версии не поддерживаются буквы в качестве элементов множества, определяющего символы, которые можно использовать в именах файлов, папок, путях файловой системы");
            }

            if (illegalCharactersForPath.Any(character => character.Equals(directorySeparatorChar)))
            {
                throw new ArgumentException("Набор элементов, составляющих недопустимые символы для задания путей, не может содержать символ разделения папок(\"{0}\")".FormatWith(directorySeparatorChar));
            }

            Nullable<char> illegalChar;

            if (rootPath.ContainsAny(illegalCharactersForPath, out illegalChar))
            {
                throw new ArgumentException("Путь к корневой папке содержит как минимум один из запрещенных для путей символов, этот: \"{0}\"".FormatWith(illegalChar.Value));
            }

            _rootPath = rootPath;
            _illegalCharactersForPath = illegalCharactersForPath;
            _nameValidator = nameValidator;
            _directorySeparatorChar = directorySeparatorChar;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pathToValidate"></param>
        /// <exception cref="InvalidPathException"></exception>
        public void Validate(string pathToValidate)
        {
            if (String.IsNullOrEmpty(pathToValidate))
            {
                throw new InvalidPathException("Путь задан неверно - не задан вообще или представляет собой пустую строку");
            }

            Nullable<char> charFound;

            if (pathToValidate.ContainsAny(_illegalCharactersForPath, out charFound))
            {
                throw new InvalidPathException("Путь задан неверно - он содержит недопустимый символ \"{0}\"".FormatWith(charFound.Value));
            }

            // TODO: вынести. Логику такого рода (стратегия сравнения case sensitive или нет и проч.) лучше настраивать в одном месте.
            if (String.Equals(_rootPath, pathToValidate, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (pathToValidate.EndsWith(new string(new char[]{_directorySeparatorChar})))
            {
                throw new InvalidPathException("Путь (\"{0}\") задан неверно - он не должен заканчиваться знаком разделения папок (\"{1}\"), путь всегда указывает на папку или на файл.".FormatWith(pathToValidate, _directorySeparatorChar));
            }
            
            ValidatePathComponents(pathToValidate);
        }

        private void ValidatePathComponents(string pathToValidate)
        {
            string[] pathComponents = pathToValidate.Split(new char[] {_directorySeparatorChar}, StringSplitOptions.None);

            if ((_rootPath.Length == 1) && (_rootPath[0] == _directorySeparatorChar))
            {
                if (!String.IsNullOrEmpty(pathComponents[0]))
                {
                    throw CreateMissingRootException(pathToValidate);
                }
            }
            else if (!String.Equals(pathComponents[0], _rootPath, StringComparison.OrdinalIgnoreCase))
            {
                throw CreateMissingRootException(pathToValidate);
            }

            for (int i = 1; i < pathComponents.Length; i++)
            {
                string pathComponent = pathComponents[i];
                
                try
                {
                    _nameValidator.Validate(pathComponent);
                }
                catch (InvalidNameException exception)
                {
                    throw new InvalidPathException("Путь (\"{0}\") содержит невалидные составляющие".FormatWith(pathToValidate), exception);
                }
            }
        }

        private InvalidPathException CreateMissingRootException(string pathToValidate)
        {
            return new InvalidPathException(
                "Путь (\"{0}\") задан неверно. Любой путь, указывающий на что-либо в файловой системе, должен начинаться с указателя на корневую папку диска (обозначается \"{1}\")".FormatWith(pathToValidate, _rootPath));
        }
    }
}