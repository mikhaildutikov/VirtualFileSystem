using System;

namespace VirtualFileSystem
{
    /// <summary>
    /// Адресуемый объект - то есть такой, у которого есть адрес. У любого объекта файловой системы
    /// есть адрес - путь к нему.
    /// </summary>
    public abstract class Addressable : IAddressableObjectVisitorAcceptor
    {
        /// <summary>
        /// Путь, указывающий на объект.
        /// </summary>
        public string FullPath { get; private set; }

        /// <summary>
        /// Имя объекта.
        /// </summary>
        public string Name { get; private set; }

        protected Addressable(string fullPath, string name)
        {
            if (String.IsNullOrEmpty(fullPath)) throw new ArgumentNullException("fullPath");
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            FullPath = fullPath;
            Name = name;
        }

        /// <summary>
        /// Принимает посетителя (Visitor, GoF).
        /// </summary>
        /// <param name="visitor"></param>
        public abstract void Accept(IAddressableObjectVisitor visitor);
    }
}