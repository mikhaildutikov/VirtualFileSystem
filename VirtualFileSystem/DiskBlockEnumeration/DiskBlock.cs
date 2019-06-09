using System;
using VirtualFileSystem.Disk;
using VirtualFileSystem.Toolbox;
using VirtualFileSystem.Toolbox.Extensions;

namespace VirtualFileSystem.DiskBlockEnumeration
{
    internal class DiskBlock : IDiskBlock
    {
        private readonly IVirtualDisk _disk;
        private readonly int _blockIndex;
        private int _position;

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public DiskBlock(IVirtualDisk disk, int index, int occupiedSpaceInBytes, int position)
        {
            if (disk == null)
            {
                throw new ArgumentNullException("disk");
            }

            MethodArgumentValidator.ThrowIfNegative(index, "index");
            MethodArgumentValidator.ThrowIfNegative(occupiedSpaceInBytes, "occupiedSpaceInBytes");
            MethodArgumentValidator.ThrowIfNegative(position, "position");

            if (index >= disk.NumberOfBlocks)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            if (occupiedSpaceInBytes > disk.BlockSizeInBytes)
            {
                throw new ArgumentOutOfRangeException("occupiedSpaceInBytes");
            }

            if (position > disk.BlockSizeInBytes)
            {
                throw new ArgumentOutOfRangeException("position");
            }

            _disk = disk;
            _blockIndex = index;
            this.OccupiedSpaceInBytes = occupiedSpaceInBytes;
            this.SizeInBytes = _disk.BlockSizeInBytes;
            _position = position;
        }

        public int OccupiedSpaceInBytes
        {
            get;
            private set;
        }

        public int FreeSpaceInBytes
        {
            get
            {
                return this.SizeInBytes - this.OccupiedSpaceInBytes;
            }
        }

        public int NumberOfBytesCanBeWrittenAtCurrentPosition
        {
            get
            {
                if (this.Position > (this.SizeInBytes - 1))
                {
                    return 0;
                }

                return (this.SizeInBytes - this.Position);
            }
        }

        public bool CanAcceptBytesAtCurrentPosition
        {
            get
            {
                return (this.Position < (this.SizeInBytes));
            }
        }

        public int SizeInBytes
        {
            get;
            private set;
        }

        /// <summary>
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public int Position
        {
            get
            {
                return _position;
            }
            set
            {
                MethodArgumentValidator.ThrowIfNegative(value, "value");

                if (value > SizeInBytes)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                _position = value;
            }
        }

        public byte[] ReadAll()
        {
            byte[] bytesRead = _disk.ReadBytesFromBlock(BlockIndex, 0, this.OccupiedSpaceInBytes);

            return bytesRead;
        }

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public void WriteBytes(byte[] array, int startingPosition, int length)
        {
            MethodArgumentValidator.ThrowIfNull(array, "array");
            MethodArgumentValidator.ThrowIfNegative(startingPosition, "startingPosition");
            MethodArgumentValidator.ThrowIfNegative(length, "length");

            if ((startingPosition + length) > array.Length)
            {
                throw new ArgumentException("В массиве, начиная с позиции {0}, не найдется следующего числа байт для записи: {1}".FormatWith(startingPosition, length));
            }

            if (this.IsAtEndOfBlock)
            {
                throw new InvalidOperationException("Запись за пределами границ блока не разрешена");
            }

            int positionAfterWriting = this.Position + length;

            if (positionAfterWriting > this.SizeInBytes)
            {
                throw new ArgumentException("Запись невозможна: попытка записи за пределами массива");
            }

            _disk.WriteBytesToBlock(BlockIndex, array, startingPosition, _position, length);

            if ((positionAfterWriting > this.OccupiedSpaceInBytes) && (this.OccupiedSpaceInBytes < this.SizeInBytes))
            {
                this.OccupiedSpaceInBytes += (positionAfterWriting - this.OccupiedSpaceInBytes);
            }

            this.Position = positionAfterWriting;
        }

        public bool IsNull
        {
            get { return false; }
        }

        public bool IsAtEndOfReadableData
        {
            get
            {
                return (this.Position == this.OccupiedSpaceInBytes);
            }
        }

        public bool IsAtEndOfBlock
        {
            get
            {
                return (this.Position == this.SizeInBytes);
            }
        }

        public int BlockIndex
        {
            get { return _blockIndex; }
        }
    }
}