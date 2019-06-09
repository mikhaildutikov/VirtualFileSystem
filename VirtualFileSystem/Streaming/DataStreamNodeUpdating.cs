using System;
using VirtualFileSystem.DiskStructuresManagement;

namespace VirtualFileSystem.Streaming
{
    internal class DataStreamNodeUpdating : DataStreamReadableWritable
    {
        private readonly DataStreamReadableWritable _dataStream;
        private readonly Node _streamOwningNode;
        private readonly IFileSystemNodeStorage _fileSystemNodeStorage;

        public DataStreamNodeUpdating(DataStreamReadableWritable dataStream, Node streamOwningNode, IFileSystemNodeStorage fileSystemNodeStorage)
        {
            if (dataStream == null) throw new ArgumentNullException("dataStream");
            if (streamOwningNode == null) throw new ArgumentNullException("streamOwningNode");
            if (fileSystemNodeStorage == null) throw new ArgumentNullException("fileSystemNodeStorage");

            _dataStream = dataStream;
            _streamOwningNode = streamOwningNode;
            _fileSystemNodeStorage = fileSystemNodeStorage;
        }

        public override void SetPosition(int newPosition)
        {
            _dataStream.SetPosition(newPosition);
        }

        /// <summary>
        /// Устанавливает длину потока (беря себе или освобождая соответствующий объем дискового пространства).
        /// </summary>
        /// <param name="newLength">Новая длина потока</param>
        /// <exception cref="InsufficientSpaceException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public override void SetLength(int newLength)
        {
            _dataStream.SetLength(newLength);

            this.PersistOwningNode();
        }

        /// <summary>
        /// Производит усечение потока (зануляет его длину).
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public override void Truncate()
        {
            _dataStream.Truncate();

            this.PersistOwningNode();
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
            return _dataStream.Read(bufferToReadBytesInto, offset, count);
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
            var oldLength = _dataStream.Length;

            _dataStream.Write(bytesToWrite, arrayOffset, count);

            var newLength = _dataStream.Length;

            if (newLength != oldLength)
            {
                this.PersistOwningNode();
            }
        }

        private void PersistOwningNode()
        {
            FileNode streamOwningNodeAsFileNode = _streamOwningNode as FileNode; // Note: можно избежать приведения типов на 100%, используя паттерн Visitor, к примеру.
            
            if (streamOwningNodeAsFileNode != null)
            {
                streamOwningNodeAsFileNode.LastModificationTimeUtc = DateTime.UtcNow;
            }

            _fileSystemNodeStorage.WriteNode(_streamOwningNode);
        }

        public override int Length
        {
            get { return _dataStream.Length; }
        }

        public override int Position
        {
            get { return _dataStream.Position; }
        }

        public override void Dispose()
        {
            _dataStream.Dispose();
        }

        public override void MoveToEnd()
        {
            _dataStream.MoveToEnd();
        }
    }
}