using System;

// ReSharper disable CheckNamespace
namespace VirtualFileSystem
// ReSharper restore CheckNamespace
{
    /// <summary>
    /// Поток данных, в который можно писать данные, из которого можно читать данные.
    /// </summary>
    public abstract class DataStreamReadableWritable : DataStreamReadable
    {
        /// <summary>
        /// Устанавливает длину потока (беря себе или освобождая соответствующий объем дискового пространства).
        /// </summary>
        /// <param name="newLength">Новая длина потока</param>
        /// <exception cref="InsufficientSpaceException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public abstract void SetLength(int newLength);

        /// <summary>
        /// Производит усечение потока (зануляет его длину).
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public abstract void Truncate();

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
        public abstract void Write(byte[] bytesToWrite, int arrayOffset, int count);
    }
}