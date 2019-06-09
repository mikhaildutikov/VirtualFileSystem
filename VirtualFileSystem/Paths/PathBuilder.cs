using System;
using System.Collections.Generic;
using VirtualFileSystem.Toolbox;
using VirtualFileSystem.Toolbox.Extensions;

namespace VirtualFileSystem
{
    /// <summary>
    /// Класс, предоставляющий очень скромный функционал для работы с путями в файловой системе.
    /// </summary>
    public class PathBuilder
    {
        private readonly char _directorySeparator;
        private readonly IPathValidator _pathValidator;
        private readonly string _rootPath;
        private readonly IEqualityComparer<string> _nameComparer;

        internal static readonly PathBuilder Default = new PathBuilder(VirtualFileSystem.DirectorySeparatorChar, PathValidator.Default, VirtualFileSystem.Root, StringComparer.OrdinalIgnoreCase);

        internal PathBuilder(
            char directorySeparator,
            IPathValidator pathValidator,
            string rootPath,
            IEqualityComparer<string> nameComparer)
        {
            if (pathValidator == null) throw new ArgumentNullException("pathValidator");
            if (nameComparer == null) throw new ArgumentNullException("nameComparer");

            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty(rootPath, "rootPath");

            _directorySeparator = directorySeparator;
            _pathValidator = pathValidator;
            _rootPath = rootPath;
            _nameComparer = nameComparer;
        }

        private bool EndsWithPathSeparatorChar(string path)
        {
            return path[path.Length - 1].Equals(_directorySeparator);
        }

        /// <summary>
        /// Объединяет две составляющих пути в одно целое.
        /// </summary>
        /// <param name="path1">Первая составляющая пути. Должна указывать на папку (корень файловой системы - тоже папка), не заканчиваться символом разделения папок.</param>
        /// <param name="path2">Вторая составляющая пути. Должна представлять собой относительный путь (без указания на корень) к файлу или папке, не заканчиваться символом разделения папок.</param>
        /// <returns>Путь, полученный конкатенацией <paramref name="path1"/> и <paramref name="path2"/>.</returns>
        /// <exception cref="ArgumentException">Если хотя бы один из переднных аргументов невалиден.</exception>
        public string CombinePaths(string path1, string path2)
        {
            try
            {
                _pathValidator.Validate(path1);
            }
            catch (InvalidPathException exception)
            {
                throw new ArgumentException("Не удалось объединить пути. Путь \"{0}\" невалиден. Далее - подробные сведения об ошибке.{1}{2}".FormatWith(path1, Environment.NewLine, exception.Message), "path1");
            }

            if (String.IsNullOrEmpty(path2))
            {
                return path1;
            }

            // Note: и здесь возможны оптимизации
            string combinedPath;

            if (this.EndsWithPathSeparatorChar(path1)) // в итоге после всех тех проверок, достаточно выяснить, не корень ли path1.
            {
                combinedPath = path1 + path2;
            }
            else
            {
                combinedPath = path1 + _directorySeparator + path2;
            }

            try
            {
                _pathValidator.Validate(combinedPath);
            }
            catch (InvalidPathException exception)
            {
                throw new ArgumentException("Не удастся объединить пути. Результат их объединения будет содержать следующую ошибку валидации. \"{0}\"".FormatWith(exception.Message));
            }
            
            return combinedPath;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        internal string GetFileOrFolderName(string path)
        {
            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty(path, "fullPath");

            if (_nameComparer.Equals(_rootPath, path))
            {
                return path;
            }

            if (path.IndexOf(_directorySeparator) < 0)
            {
                return path;
            }

            return path.Remove(0, path.LastIndexOf(_directorySeparator) + 1);
        }


        /// <summary>
        /// Делает из абсолютного пути к папке <paramref name="fullPathToTurnIntoRelative"/>, ее путь относительно папки <paramref name="fullPathForFolderToGetRelativePathAgainst"/>.
        /// </summary>
        /// <param name="fullPathForFolderToGetRelativePathAgainst"></param>
        /// <param name="fullPathToTurnIntoRelative"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        internal string GetRelativePath(string fullPathForFolderToGetRelativePathAgainst, string fullPathToTurnIntoRelative)
        {
            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty("fullPathForFolderToGetRelativePathAgainst", fullPathForFolderToGetRelativePathAgainst);
            MethodArgumentValidator.ThrowIfStringIsNullOrEmpty("fullPathToTurnIntoRelative", fullPathToTurnIntoRelative);

            // Note: ordinal ignore case - выносить надо отсюда. Такие вещи надо настраивать в каком-то одном месте.
            if (!fullPathToTurnIntoRelative.StartsWith(fullPathForFolderToGetRelativePathAgainst, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Чтобы получить путь одной папки относительно другой, необходимо, чтобы та папка, относительно которой считается путь, была родительской папкой (прямо или косвенно) для той, которой считается путь.");
            }

            return
                fullPathToTurnIntoRelative
                .Substring(fullPathForFolderToGetRelativePathAgainst.Length, fullPathToTurnIntoRelative.Length - fullPathForFolderToGetRelativePathAgainst.Length)
                .TrimStart(new[] { _directorySeparator });
        }

        internal bool PointsToRoot(string path)
        {
            return _nameComparer.Equals(_rootPath, path);
        }
    }
}