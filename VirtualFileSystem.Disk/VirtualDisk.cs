using System;
using System.IO;
using System.Text;
using VirtualFileSystem.Toolbox;
using VirtualFileSystem.Toolbox.Extensions;

namespace VirtualFileSystem.Disk
{
    /// <summary>
    /// TODO:
    /// 1) Выделить код для создания диска (в фабрику) - сейчас это не делаю
    /// 2) Is endianness an issue? - could be for some files - the file system must care for endianness of its own structures. (почти на 100% поддерживается - с Free Space Bitmap трудности)
    /// 3) Extract Formatting Code.
    /// </summary>
    internal class VirtualDisk : IVirtualDisk
    {
        private const int MinimumNumberOfBlocksOnDisk = 50;
        internal const int MaximumDiskSizeInBytes = 1073741824; //Note: 1Гб. Никакой магии, просто с дисками большего размера я приложение не гонял.
        internal const int OnlySupportedBlockSize = 2048;

        public const string DiskManufacturerId = "33817eb3af9f4cb4948b3afebca78db9";
        public static readonly Version DiskFirmwareVersion = new Version(1, 0, 0, 0);
        public static readonly Encoding DataEncoding = Encoding.UTF8;

        private const int SizeOfBufferToUseWhenZeroingDisk = 65536;

        private readonly int _firstBlockOffset;
        private readonly Stream _backingStream;
        private bool _disposed;
        private readonly object _diskAccessCriticalSection = new object();

        internal VirtualDisk(Stream backingStream)
        {
            if (backingStream == null)
            {
                throw new ArgumentNullException("backingStream");
            }

            new StreamValidator().Validate(backingStream);

            backingStream.Position = 0;

            try
            {
                // Всегда - Little Endian
                var binaryReader = new BinaryReader(backingStream, VirtualDisk.DataEncoding);

                string diskManufacturerId = binaryReader.ReadString();
                string versionAsString = binaryReader.ReadString();

                try
                {
                    var firmwareVersion = new Version(versionAsString);

                    if (!DiskFirmwareVersion.Equals(firmwareVersion))
                    {
                        throw new VirtualDiskCreationFailedException("Виртуальный диск с прошивкой версии {0} не поддерживается.".FormatWith(firmwareVersion));
                    }
                }
                catch (ArgumentException)
                {
                    throw new VirtualDiskCreationFailedException("Виртуальный диск не распознан: его заголовок имеет неверную структуру.");
                }
                catch (FormatException)
                {
                    throw new VirtualDiskCreationFailedException("Виртуальный диск не распознан: его заголовок имеет неверную структуру.");
                }
                catch (OverflowException)
                {
                    throw new VirtualDiskCreationFailedException("Виртуальный диск не распознан: его заголовок имеет неверную структуру.");
                }

                int blockSizeInBytes = binaryReader.ReadInt32();
                int maximumDiskSize = binaryReader.ReadInt32();
                int firstBlockOffset = binaryReader.ReadInt32();

                if (!String.Equals(diskManufacturerId, DiskManufacturerId, StringComparison.Ordinal))
                {
                    throw new VirtualDiskCreationFailedException("Виртуальный диск не распознан.");
                }

                _backingStream = backingStream;
                _firstBlockOffset = firstBlockOffset;
                BlockSizeInBytes = blockSizeInBytes;
                DiskSizeInBytes = maximumDiskSize;
                NumberOfBlocks = (maximumDiskSize - firstBlockOffset) / blockSizeInBytes;   
            }
            catch (EndOfStreamException exception)
            {
                throw new VirtualDiskCreationFailedException("Виртуальный диск не распознан: его заголовок имеет неверную структуру.", exception);
            }
            catch (IOException exception)
            {
                throw new VirtualDiskCreationFailedException("Виртуальный диск не распознан: его заголовок имеет неверную структуру.", exception);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="backingStream"></param>
        /// <param name="blockSizeInBytes"></param>
        /// <param name="maximumDiskSizeInBytes"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="VirtualDiskCreationFailedException"></exception>
        public static VirtualDisk CreateFormattingTheStream(Stream backingStream, int blockSizeInBytes, int maximumDiskSizeInBytes)
        {
            if (backingStream == null) throw new ArgumentNullException("backingStream");

            new StreamValidator().Validate(backingStream);
            MakeSureArgumentsMakeSense(blockSizeInBytes, maximumDiskSizeInBytes);

            int firstBlockOffset;

            try
            {
                firstBlockOffset = WriteDiskHeaderTruncatingTheStream(backingStream, blockSizeInBytes, maximumDiskSizeInBytes);
                ZeroOutTheDisk(backingStream, maximumDiskSizeInBytes);
            }
            catch (IOException exception)
            {
                throw CreateGenericDiskCreationFailure(exception);
            }

            return new VirtualDisk(backingStream);
        }

        private static void ZeroOutTheDisk(Stream backingStream, int maximumDiskSizeInBytes)
        {
            long numberOfBytesToZeroOut = maximumDiskSizeInBytes - backingStream.Length;

            var zeroes = new byte[SizeOfBufferToUseWhenZeroingDisk];
            backingStream.Seek(0, SeekOrigin.End);

            while (numberOfBytesToZeroOut != 0)
            {
                if (numberOfBytesToZeroOut < SizeOfBufferToUseWhenZeroingDisk)
                {
                    zeroes = new byte[numberOfBytesToZeroOut];
                }

                backingStream.Write(zeroes, 0, zeroes.Length);
                numberOfBytesToZeroOut -= zeroes.Length;
            }
        }

        private static int WriteDiskHeaderTruncatingTheStream(Stream backingStream, int blockSizeInBytes, int maximumDiskSizeInBytes)
        {
            backingStream.SetLength(0);
            backingStream.Position = 0;

            // Всегда - Little Endian
            var binaryWriter = new BinaryWriter(backingStream, VirtualDisk.DataEncoding);

            binaryWriter.Write(VirtualDisk.DiskManufacturerId);
            binaryWriter.Write(VirtualDisk.DiskFirmwareVersion.ToString());
            binaryWriter.Write(blockSizeInBytes);
            binaryWriter.Write(maximumDiskSizeInBytes);

            binaryWriter.Flush();

            int totalHeaderLength = (int)backingStream.Length + sizeof(Int32); // +4 - нужно еще записать смещение первого блока относительно начала потока.

            int numberOfWholeBlocksOccupiedByHeader =
                SpaceRequirementsCalculator.GetNumberOfChunksNeededToStoreData(totalHeaderLength, blockSizeInBytes);

            int firstBlockOffset = numberOfWholeBlocksOccupiedByHeader * blockSizeInBytes;

            binaryWriter.Write(numberOfWholeBlocksOccupiedByHeader * blockSizeInBytes);

            binaryWriter.BaseStream.SetLength(numberOfWholeBlocksOccupiedByHeader * blockSizeInBytes);

            binaryWriter.Flush();

            return firstBlockOffset;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="backingStream"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="VirtualDiskCreationFailedException"></exception>
        public static VirtualDisk CreateFromStream(Stream backingStream)
        {
            new StreamValidator().Validate(backingStream);

            backingStream.Position = 0;

            try
            {
                // Всегда - Little Endian
                var binaryReader = new BinaryReader(backingStream, VirtualDisk.DataEncoding);

                string diskManufacturerId = binaryReader.ReadString();
                string versionAsString = binaryReader.ReadString();

                try
                {
                    var firmwareVersion = new Version(versionAsString);

                    if (!DiskFirmwareVersion.Equals(firmwareVersion))
                    {
                        throw new VirtualDiskCreationFailedException("Виртуальный диск с прошивкой версии {0} не поддерживается.".FormatWith(firmwareVersion));
                    }
                }
                catch (ArgumentException)
                {
                    throw new VirtualDiskCreationFailedException("Виртуальный диск не распознан: его заголовок имеет неверную структуру.");
                }
                catch (FormatException)
                {
                    throw new VirtualDiskCreationFailedException("Виртуальный диск не распознан: его заголовок имеет неверную структуру.");
                }
                catch (OverflowException)
                {
                    throw new VirtualDiskCreationFailedException("Виртуальный диск не распознан: его заголовок имеет неверную структуру.");
                }

                int blockSizeInBytes = binaryReader.ReadInt32();
                int maximumDiskSize = binaryReader.ReadInt32();
                int firstBlockOffset = binaryReader.ReadInt32();

                if (!String.Equals(diskManufacturerId, DiskManufacturerId, StringComparison.Ordinal))
                {
                    throw new VirtualDiskCreationFailedException("Виртуальный диск не распознан.");
                }

                return new VirtualDisk(backingStream);
            }
            catch (EndOfStreamException exception)
            {
                throw new VirtualDiskCreationFailedException("Виртуальный диск не распознан: его заголовок имеет неверную структуру.", exception);
            }
            catch (IOException exception)
            {
                throw new VirtualDiskCreationFailedException("Виртуальный диск не распознан: его заголовок имеет неверную структуру.", exception);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        /// <exception cref="VirtualDiskCreationFailedException"></exception>
        private static Exception CreateGenericDiskCreationFailure(Exception exception)
        {
            return new VirtualDiskCreationFailedException("Не удалось создать виртуальный диск: {0}".FormatWith(exception.Message), exception);
        }

        private static void MakeSureArgumentsMakeSense(int blockSizeInBytes, int maximumDiskSizeInBytes)
        {
            if (blockSizeInBytes <= 0)
            {
                throw new ArgumentOutOfRangeException("blockSizeInBytes");
            }

            if (blockSizeInBytes != OnlySupportedBlockSize)
            {
                throw new ArgumentOutOfRangeException("blockSizeInBytes", "Размер дискового блока в этой версии должен составлять {0} байт.".FormatWith(OnlySupportedBlockSize));
            }

            if (maximumDiskSizeInBytes % blockSizeInBytes != 0)
            {
                throw new ArgumentException("Максимальный размер диска должен быть кратен размеру дискового блока.", "maximumDiskSizeInBytes");
            }

            if (maximumDiskSizeInBytes <= 0)
            {
                throw new ArgumentOutOfRangeException("maximumDiskSizeInBytes");
            }

            if (maximumDiskSizeInBytes > MaximumDiskSizeInBytes)
            {
                throw new ArgumentException(
                    "В текущей версии диски размером больше {0} байт не поддерживаются.".FormatWith(
                        MaximumDiskSizeInBytes));
            }

            int numberOfBlocks = maximumDiskSizeInBytes / blockSizeInBytes;

            if (numberOfBlocks < MinimumNumberOfBlocksOnDisk)
            {
                throw new ArgumentOutOfRangeException("maximumDiskSizeInBytes", "Диск должен позволять хранить как минимум следующее число блоков: {0} (ограничения текущей версии).".FormatWith(MinimumNumberOfBlocksOnDisk));
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockIndex"></param>
        /// <param name="bytesToWrite"></param>
        /// <param name="arrayOffset"></param>
        /// <param name="blockOffset"></param>
        /// <param name="numberOfBytesToWrite"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public void WriteBytesToBlock(int blockIndex, byte[] bytesToWrite, int arrayOffset, int blockOffset, int numberOfBytesToWrite)
        {
            lock (_diskAccessCriticalSection)
            {
                ThrowIfDisposed();
                MethodArgumentValidator.ThrowIfNull(bytesToWrite, "bytesToWrite");
                MethodArgumentValidator.ThrowIfNegative(blockOffset, "blockOffset");
                MethodArgumentValidator.ThrowIfNegative(numberOfBytesToWrite, "numberOfBytesToWrite");
                MethodArgumentValidator.ThrowIfNegative(arrayOffset, "arrayOffset");

                this.MakeSureBlockIndexArgumentIsSane(blockIndex, "blockIndex");

                if (blockOffset >= this.BlockSizeInBytes)
                {
                    throw new ArgumentOutOfRangeException("blockOffset");
                }

                if (arrayOffset > bytesToWrite.Length)
                {
                    throw new ArgumentOutOfRangeException("arrayOffset");
                }

                if (numberOfBytesToWrite > this.BlockSizeInBytes)
                {
                    throw new ArgumentOutOfRangeException("numberOfBytesToWrite");
                }

                if ((blockOffset + numberOfBytesToWrite) > this.BlockSizeInBytes)
                {
                    throw new ArgumentOutOfRangeException("blockOffset, numberOfBytesToWrite");
                }

                if (bytesToWrite.Length < numberOfBytesToWrite)
                {
                    throw new ArgumentOutOfRangeException("numberOfBytesToWrite", "Массиве данных слишком мал, чтобы из него можно было считать следующее число байт: {0}.".FormatWith(numberOfBytesToWrite));
                }

                this.SeekToBlockStartWithInBlockOffset(blockIndex, blockOffset);

                _backingStream.Write(bytesToWrite, arrayOffset, numberOfBytesToWrite);
            }
        }

        public int WriteBytesContinuoslyStartingFromBlock(byte[] bytes, int startingBlockIndex, int arrayOffset, int blockOffset)
        {
            // TODO: check args, revisit
            lock (_diskAccessCriticalSection)
            {
                ThrowIfDisposed();

                int numberOfBytesToWrite = bytes.Length - arrayOffset;
                int availableSpace = ((this.NumberOfBlocks - startingBlockIndex) * this.BlockSizeInBytes) + (this.BlockSizeInBytes - blockOffset);

                if (numberOfBytesToWrite > availableSpace)
                {
                    // InsufficientSpaceException()
                    throw new ArgumentException();
                }

                var distribution = ItemDistributor.Distribute(numberOfBytesToWrite, this.BlockSizeInBytes - blockOffset, this.BlockSizeInBytes);

                int currentArrayOffset = 0;

                foreach (BucketDistribution bucketDistribution in distribution)
                {
                    this.WriteBytesToBlock(
                        startingBlockIndex + bucketDistribution.BucketIndex,
                        bytes,
                        currentArrayOffset,
                        bucketDistribution.IndexOfFirstItemTheBucketGot,
                        bucketDistribution.NumberOfItemsDistributed);

                    currentArrayOffset += bucketDistribution.NumberOfItemsDistributed;
                }

                return distribution.Count;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="indexOfBlockToWriteTo"></param>
        /// <param name="bytesToWrite"></param>
        /// <param name="startingPosition"></param>
        /// <param name="numberOfBytesToWrite"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ArgumentException"></exception>
        private void WriteBytesToBlock(int indexOfBlockToWriteTo, byte[] bytesToWrite, int startingPosition, int numberOfBytesToWrite)
        {
            MethodArgumentValidator.ThrowIfNull(bytesToWrite, "bytesToWrite");
            MethodArgumentValidator.ThrowIfNegative(indexOfBlockToWriteTo, "indexOfBlockToWriteTo");
            MethodArgumentValidator.ThrowIfNegative(startingPosition, "startingPosition");
            MethodArgumentValidator.ThrowIfNegative(numberOfBytesToWrite, "numberOfBytesToWrite");

            if ((startingPosition + numberOfBytesToWrite) > bytesToWrite.Length)
            {
                throw new ArgumentException("Не получится записать {0} байтов, начиная с позиции {1}. В массиве всего следующее число байт: {2}.".FormatWith(numberOfBytesToWrite, startingPosition, bytesToWrite.Length));
            }

            if (bytesToWrite.Length > BlockSizeInBytes)
            {
                throw new ArgumentException("Невозможно записать данные в блок: в блок можно записать, максимум, {0} байт.".FormatWith(BlockSizeInBytes), "bytesToWrite");
            }

            this.MakeSureBlockIndexArgumentIsSane(indexOfBlockToWriteTo, "indexOfBlockToWriteTo");

            this.SeekToBlockStart(indexOfBlockToWriteTo);

            _backingStream.Write(bytesToWrite, startingPosition, numberOfBytesToWrite);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="indexOfBlockToWriteTo"></param>
        /// <param name="bytesToWrite"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public void WriteBytesToBlock(int indexOfBlockToWriteTo, byte[] bytesToWrite)
        {
            lock (_diskAccessCriticalSection)
            {
                ThrowIfDisposed();

                this.WriteBytesToBlock(indexOfBlockToWriteTo, bytesToWrite, 0, bytesToWrite.Length);
            }
        }

        private void SeekToBlockStart(int indexOfBlockToSeekTo)
        {
            _backingStream.Position = _firstBlockOffset + (indexOfBlockToSeekTo * BlockSizeInBytes);
        }

        private void SeekToBlockStartWithInBlockOffset(int indexOfBlockToSeekTo, int offsetInsideTheBlock)
        {
            SeekToBlockStart(indexOfBlockToSeekTo);
            _backingStream.Position += offsetInsideTheBlock;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockIndex"></param>
        /// <param name="blockIndexArgumentName"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void MakeSureBlockIndexArgumentIsSane(int blockIndex, string blockIndexArgumentName)
        {
            if ((blockIndex >= NumberOfBlocks) || (blockIndex < 0))
            {
                throw new ArgumentOutOfRangeException(
                    blockIndexArgumentName,
                    "Невозможно записать данные в блок номер {0}. (У диска следующее число блоков -- {1})".FormatWith(blockIndex, NumberOfBlocks));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="indexOfBlockToReadFrom"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public byte[] ReadAllBytesFromBlock(int indexOfBlockToReadFrom)
        {
            lock (_diskAccessCriticalSection)
            {
                ThrowIfDisposed();
                this.MakeSureBlockIndexArgumentIsSane(indexOfBlockToReadFrom, "indexOfBlockToReadFrom");

                this.SeekToBlockStart(indexOfBlockToReadFrom);

                var bytesRead = new byte[BlockSizeInBytes];

                _backingStream.Read(bytesRead, 0, BlockSizeInBytes);

                return bytesRead;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="indexOfBlockToReadFrom"></param>
        /// <param name="startingPosition"></param>
        /// <param name="numberOfBytesToRead"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public byte[] ReadBytesFromBlock(int indexOfBlockToReadFrom, int startingPosition, int numberOfBytesToRead)
        {
            lock (_diskAccessCriticalSection)
            {
                ThrowIfDisposed();
                MethodArgumentValidator.ThrowIfNegative(indexOfBlockToReadFrom, "indexOfBlockToReadFrom");
                MethodArgumentValidator.ThrowIfNegative(numberOfBytesToRead, "numberOfBytesToRead");

                if ((startingPosition + numberOfBytesToRead) > BlockSizeInBytes)
                {
                    throw new ArgumentException("Не удалось прочитать данные: попытка чтения за границами блока.", "startingPosition, numberOfBytesToRead");
                }

                this.MakeSureBlockIndexArgumentIsSane(indexOfBlockToReadFrom, "indexOfBlockToReadFrom");

                SeekToBlockStartWithInBlockOffset(indexOfBlockToReadFrom, startingPosition);

                var bytesRead = new byte[numberOfBytesToRead];

                _backingStream.Read(bytesRead, 0, numberOfBytesToRead);

                return bytesRead;
            }
        }

        public int BlockSizeInBytes { get; private set; }
        public long DiskSizeInBytes { get; private set; }
        public int NumberOfBlocks { get; private set; }

        public void Dispose()
        {
            lock (_diskAccessCriticalSection)
            {
                if (!_disposed)
                {
                    _backingStream.Dispose();
                    _disposed = true;
                }
            }
        }
    }
}