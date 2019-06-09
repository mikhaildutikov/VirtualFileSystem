using System;
using System.Threading;
using VirtualFileSystem.Addressing;
using VirtualFileSystem.Exceptions.Irrecoverable;
using VirtualFileSystem.Locking;
using VirtualFileSystem.Streaming;
using VirtualFileSystem.Toolbox;
using VirtualFileSystem.Toolbox.Extensions;

namespace VirtualFileSystem
{
    /// <summary>
    /// Note: thread-safe.
    /// Note: большой класс, отрефакторить.
    /// </summary>
    internal class DataStream : DataStreamReadableWritable
    {
        private const int ZeroingBufferSize = 65536;
        private readonly IDataStreamStructureBuilder _streamStructureBuilder;
        private readonly IFileSystemLockReleaseManager _lockingManager;
        private IDoubleIndirectDataStreamEnumerator _diskBlockEnumerator;
        private readonly object _stateChangeCriticalSection = new object();
        private readonly Guid _idOfLockBeingHeld;
        private readonly int _blockSize;
        private bool _disposed;

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ArgumentException"></exception>
        internal DataStream(
            IDataStreamStructureBuilder streamStructureBuilder,
            int blockSize,
            IFileSystemLockReleaseManager lockingManager,
            Guid idOfLockBeingHeld)
        {
            try
            {
                if (streamStructureBuilder == null) throw new ArgumentNullException("streamStructureBuilder");
                if (lockingManager == null) throw new ArgumentNullException("lockingManager");
                MethodArgumentValidator.ThrowIfIsDefault(idOfLockBeingHeld, "idOfLockBeingHeld");
                MethodArgumentValidator.ThrowIfNegative(blockSize, "blockSize");

                _streamStructureBuilder = streamStructureBuilder;
                _blockSize = blockSize;
                _lockingManager = lockingManager;
                _idOfLockBeingHeld = idOfLockBeingHeld;
                _diskBlockEnumerator = streamStructureBuilder.CreateEnumerator();
            }
            catch (Exception)
            {
                GC.SuppressFinalize(this);
                
                throw;
            }
        }

        /// <summary>
        /// Устанавливает указатель текущего положения в потоке в новое место.
        /// Примечание: за границу потока установить указатель нельзя (допускается лишь устанавливать ее за последним записанным/считанным байтом).
        /// </summary>
        /// <param name="newPosition">Новая позиция указателя текущего положения.</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public override void SetPosition(int newPosition)
        {
            lock (_stateChangeCriticalSection)
            {
                this.ThrowIfDisposed();

                MethodArgumentValidator.ThrowIfNegative(newPosition, "newPosition");

                if (newPosition > this.Length)
                {
                    throw new ArgumentOutOfRangeException("newPosition", "Невозможно установить позицию за пределами потока данных.");
                }

                if (newPosition == this.Length)
                {
                    this.MoveToEnd();
                    return;
                }

                int blockIndex = newPosition / _blockSize;

                _diskBlockEnumerator.SetPosition(blockIndex);
                _diskBlockEnumerator.Current.Position = newPosition % _blockSize;
            }
        }

        /// <summary>
        /// Note: предполагает, что исполняется в критическом регионе (на критической секции _stateChangeCriticalSection).
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("Поток данных для чтения и записи (\"{0}\")".FormatWith(this.GetType().FullName));
            }
        }

        /// <summary>
        /// Устанавливает длину потока (беря себе или освобождая соответствующий объем дискового пространства).
        /// </summary>
        /// <param name="newLength">Новая длина потока</param>
        /// <exception cref="InsufficientSpaceException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public override void SetLength(int newLength)
        {
            lock (_stateChangeCriticalSection)
            {
                this.ThrowIfDisposed();

                MethodArgumentValidator.ThrowIfNegative(newLength, "newLength");

                if (newLength < _streamStructureBuilder.CurrentSize)
                {
                    this.Shrink(newLength);
                    return;
                }

                if (newLength == this.Length)
                {
                    return;
                }

                int position = this.Position;

                this.MoveToEnd();

                int numberOfZeroesToAdd = this.Length == 0
                                              ? newLength
                                              : newLength - (_streamStructureBuilder.CurrentSize - 1);

                WriteZeroes(numberOfZeroesToAdd);

                this.SetPosition(position);
            }
        }

        private void WriteZeroes(int numberOfZeroesToAdd)
        {
            int numberOfZeroesWritten = 0;

            var zeroes = new byte[ZeroingBufferSize];

            while ((numberOfZeroesToAdd - numberOfZeroesWritten) > ZeroingBufferSize)
            {
                this.Write(zeroes, 0, zeroes.Length);

                numberOfZeroesWritten += zeroes.Length;
            }

            int zeroesLeft = numberOfZeroesToAdd - numberOfZeroesWritten;

            if (zeroesLeft > 0)
            {
                zeroes = new byte[zeroesLeft];
                this.Write(zeroes, 0, zeroes.Length);
            }
        }

        private void Shrink(int newLength)
        {
            _streamStructureBuilder.SetSize(newLength);

            _diskBlockEnumerator = _streamStructureBuilder.CreateEnumerator();

            this.MoveToEnd();
        }

        /// <summary>
        /// Производит усечение потока (зануляет его длину).
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public override void Truncate()
        {
            this.SetLength(0);
        }

        /// <summary>
        /// Перемещает указатель текущего положения в потоке в самый конец потока (за последним записанным/считанным байтом).
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public override void MoveToEnd()
        {
            lock (_stateChangeCriticalSection)
            {
                this.ThrowIfDisposed();

                if (_diskBlockEnumerator.IsEmpty)
                {
                    return;
                }

                _diskBlockEnumerator.SetPosition(_diskBlockEnumerator.Count - 1);
                _diskBlockEnumerator.Current.Position = _diskBlockEnumerator.Current.OccupiedSpaceInBytes;
            }
        }

        /// <summary>
        /// Читает байты из потока в заданный массив, начиная с текущей позиции в потоке.
        /// </summary>
        /// <param name="bufferToReadBytesInto">Байтовый массив, в который следует читать данные.</param>
        /// <param name="offset">Стартовая позиция в массиве <paramref name="bufferToReadBytesInto"/>, куда надо записывать данные из потока.</param>
        /// <param name="count">Число байт, которые следует считать из потока.</param>
        /// <returns>Число байт, которые удалось прочитать (за пределами потока читать нельзя, потому это число может оказаться меньше чем <paramref name="count"/>).</returns>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public override int Read(byte[] bufferToReadBytesInto, int offset, int count)
        {
            lock (_stateChangeCriticalSection)
            {
                this.ThrowIfDisposed();

                MethodArgumentValidator.ThrowIfNegative(offset, "offset");
                MethodArgumentValidator.ThrowIfNegative(count, "count");
                if (bufferToReadBytesInto == null) throw new ArgumentNullException("bufferToReadBytesInto");
                if ((offset + count) > bufferToReadBytesInto.Length)
                {
                    throw new ArgumentException("Не удастся прочитать {0} байт, начиная с позиции {1} в массиве. В массиве содержится следующее число элементов: {2}".FormatWith(count, offset, bufferToReadBytesInto.Length));
                }

                int numberOfBytesReadable = this.Length - this.Position;

                if (count > numberOfBytesReadable)
                {
                    count = numberOfBytesReadable;
                }

                int positionInArray = offset;
                int positionInArrayAfterAllHasBeenRead = offset + count;
                int numberOfBytesRead = 0;

                while (positionInArray != positionInArrayAfterAllHasBeenRead)
                {
                    if (_diskBlockEnumerator.Current.IsAtEndOfReadableData)
                    {
                        if (!_diskBlockEnumerator.MoveNext())
                        {
                            throw new InvalidOperationException("Не удалось прочитать заданное количество байт: попытка чтения за границами потока");
                        }
                    }

                    int numberOfBytesToRead = Math.Min(
                        _diskBlockEnumerator.Current.OccupiedSpaceInBytes - _diskBlockEnumerator.Current.Position,
                        positionInArrayAfterAllHasBeenRead - positionInArray);

                    if (numberOfBytesToRead == 0)
                    {
                        throw new InvalidOperationException();
                    }

                    int position = _diskBlockEnumerator.Current.Position;

                    byte[] bytesRead = _diskBlockEnumerator.Current.ReadAll();

                    Array.Copy(bytesRead, position, bufferToReadBytesInto, positionInArray, numberOfBytesToRead); //zero instead of position                    

                    positionInArray += numberOfBytesToRead;
                    numberOfBytesRead += numberOfBytesToRead;
                    _diskBlockEnumerator.Current.Position += numberOfBytesToRead;
                }

                return numberOfBytesRead;
            }
        }

        /// <summary>
        /// Записывает в поток указанное число байт из массива. Если поток недостаточного размера, он увеличивается.
        /// Запись производится, начиная с текущей позиции в потоке.
        /// </summary>
        /// <param name="bytesToWrite">Массив байт, данные из которого следует записать в поток.</param>
        /// <param name="arrayOffset">Указывает начальную позицию в массиве (<paramref name="bytesToWrite"/>), с которой нужно брать байты для записи.</param>
        /// <param name="count">Количество байт, которые, начиная с <paramref name="arrayOffset"/>, следует записать в поток.</param>
        /// <exception cref="InsufficientSpaceException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="MaximumFileSizeReachedException"></exception>
        public override void Write(byte[] bytesToWrite, int arrayOffset, int count)
        {
            if (bytesToWrite == null)
            {
                throw new ArgumentNullException("bytesToWrite");
            }

            // Note: этому методу, подозреваю, сильно не хватило тестов.

            MethodArgumentValidator.ThrowIfNegative(arrayOffset, "arrayOffset");
            MethodArgumentValidator.ThrowIfNegative(count, "count");

            if ((arrayOffset + count) > bytesToWrite.Length)
            {
                throw new ArgumentException("Не удастся прочитать {0} байт, начиная с позиции {1} в массиве. В массиве содержится следующее число элементов: {2}".FormatWith(count, arrayOffset, bytesToWrite.Length));
            }

            if (count == 0)
            {
                return;
            }

            Monitor.Enter(_stateChangeCriticalSection);

            try
            {
                this.ThrowIfDisposed();

                int numberOfBytesAvailable = this.Length - this.Position; // есть еще зажатые в выделенном блоке, ими мы занимаемся ниже.

                int numberOfBytesNeeded = count - numberOfBytesAvailable;

                bool canAvoidNewBlocksAcquisition =
                    _diskBlockEnumerator.Current.NumberOfBytesCanBeWrittenAtCurrentPosition >= numberOfBytesNeeded;

                if ((numberOfBytesNeeded > 0) && !canAvoidNewBlocksAcquisition)
                {
                    int oldBlockIndex = _diskBlockEnumerator.Position;
                    int oldInsideBlockPosition = _diskBlockEnumerator.Current.Position;

                    _streamStructureBuilder.SetSize(_streamStructureBuilder.CurrentSize + numberOfBytesNeeded);

                    _diskBlockEnumerator = _streamStructureBuilder.CreateEnumerator();
                    _diskBlockEnumerator.SetPosition(oldBlockIndex);

                    if (!_diskBlockEnumerator.Current.IsNull)
                    {
                        _diskBlockEnumerator.Current.Position = oldInsideBlockPosition;
                    }
                }

                WriteBytesKnowingStreamLengthIsRight(bytesToWrite, arrayOffset, count);
            }
            catch (MaximumFileSizeReachedException)
            {
                throw new MaximumFileSizeReachedException("Не удаcтся записать {0} байт в файл: максимальный размер файла в текущей версии системы - {1} байт, текущий размер файла - {2} байт.".FormatWith(count, _streamStructureBuilder.MaximumSize, this.Length));
            }
            catch (NoFreeBlocksException)
            {
                throw new InsufficientSpaceException("Операция записи отменена: недостаточно места на диске для записи в поток следующего числа байт -- {0}".FormatWith(count));
            }
            finally
            {
                Monitor.Exit(_stateChangeCriticalSection);
            }
        }

        private void WriteBytesKnowingStreamLengthIsRight(byte[] bytesToWrite, int arrayOffset, int count)
        {
            int positionInArray = arrayOffset;
            int positionAfterWritingItAll = count + arrayOffset;

            while (positionInArray != positionAfterWritingItAll)
            {
                if (!_diskBlockEnumerator.Current.CanAcceptBytesAtCurrentPosition)
                {
                    if (!_diskBlockEnumerator.MoveNext())
                    {
                        throw new InconsistentDataDetectedException();
                    }
                }

                int numberOfBytesToWrite =
                    Math.Min
                        (
                            _diskBlockEnumerator.Current.NumberOfBytesCanBeWrittenAtCurrentPosition,
                            positionAfterWritingItAll - positionInArray
                        );

                if (numberOfBytesToWrite == 0)
                {
                    throw new InconsistentDataDetectedException();
                }

                _diskBlockEnumerator.Current.WriteBytes(bytesToWrite, positionInArray, numberOfBytesToWrite);

                positionInArray += numberOfBytesToWrite;
            }
        }

        /// <summary>
        /// Длина потока данных, количество байт в потоке.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public override int Length
        {
            get
            {
                lock (_stateChangeCriticalSection)
                {
                    this.ThrowIfDisposed();

                    return _streamStructureBuilder.CurrentSize; 
                }
            }
        }

        /// <summary>
        /// Текущая позиция указателя в потоке (всегда - после последнего записанного/прочитанного байта).
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public override int Position
        {
            get
            {
                lock (_stateChangeCriticalSection)
                {
                    this.ThrowIfDisposed();

                    if (_diskBlockEnumerator.IsEmpty)
                    {
                        return 0;
                    }

                    return _diskBlockEnumerator.Position*_blockSize + _diskBlockEnumerator.Current.Position;
                }
            }
        }

        #region IDisposable Members

        /// <summary>
        /// Освобождает ресурсы потока (разблокирует файл в файловой системе).
        /// </summary>
        public override void Dispose()
        {
            this.Dispose(false);
        }

        private void Dispose(bool calledFromFinalizer)
        {
            lock (_stateChangeCriticalSection)
            {
                if (_disposed)
                {
                    return;
                }

                _lockingManager.ReleaseLock(_idOfLockBeingHeld);
                _disposed = true;

                if (!calledFromFinalizer)
                {
                    GC.SuppressFinalize(this);
                }
            }
        }

        #endregion

        ~DataStream()
        {
            this.Dispose(true);
        }
    }
}