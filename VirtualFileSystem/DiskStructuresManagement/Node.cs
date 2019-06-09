using System;
using System.Runtime.Serialization;
using VirtualFileSystem.Toolbox;

namespace VirtualFileSystem.DiskStructuresManagement
{
    [Serializable]
    internal abstract class Node
    {
        private string _name;
        private readonly DateTime _creationTimeUtc;
        private Guid _version;
        private readonly Int32 _diskBlockIndex;
        private readonly Guid _id;

        [NonSerialized]
        private FileSystemArtifactNamesValidator _nameValidator;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id"></param>
        /// <param name="diskBlockIndex"></param>
        /// <param name="version"></param>
        /// <param name="creationTimeUtc"></param>
        /// <exception cref="InvalidNameException"></exception>
        protected Node(string name, Guid id, int diskBlockIndex, Guid version, DateTime creationTimeUtc)
        {
            MethodArgumentValidator.ThrowIfIsDefault<Guid>(version, "version");
            MethodArgumentValidator.ThrowIfIsDefault<Guid>(id, "id");
            MethodArgumentValidator.ThrowIfIsDefault<DateTime>(creationTimeUtc, "creationTimeUtc");
            MethodArgumentValidator.ThrowIfDateIsNonUtc(creationTimeUtc, "creationTimeUtc");

            this.InitializeValidator();

            _nameValidator.Validate(name);

            _creationTimeUtc = creationTimeUtc;
            _diskBlockIndex = diskBlockIndex;
            _id = id;
            _version = version;
            _name = name;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="InvalidNameException"></exception>
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _nameValidator.Validate(value);

                _name = value;
            }
        }

        public DateTime CreationTimeUtc
        {
            get
            {
                return _creationTimeUtc;
            }
        }

        public Guid Id
        {
            get
            {
                return _id;
            }
        }

        public Guid Version
        {
            get
            {
                return _version;
            }
            protected set
            {
                _version = value;
            }
        }

        public int DiskBlockIndex
        {
            get { return _diskBlockIndex; }
        }

        [OnDeserialized]
        private void OnAfterDeserialization(StreamingContext context)
        {
            this.InitializeValidator();
        }

        private void InitializeValidator()
        {
            _nameValidator = new FileSystemArtifactNamesValidator(Constants.IllegalCharactersForNames, Constants.FileAndFolderMaximumNameLength); //Note: легко видеть, что обычно я передаю collaborators извне, для повышения тестируемости и уменьшения связности классов. Этот же конструируется на месте - не успеваю от этого избавиться.
        }
    }
}