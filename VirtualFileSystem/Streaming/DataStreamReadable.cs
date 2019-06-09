using System;

// ReSharper disable CheckNamespace
namespace VirtualFileSystem
// ReSharper restore CheckNamespace
{
    /// <summary>
    /// Поток данных, допускающий только чтение.
    /// </summary>
    public abstract class DataStreamReadable : IDisposable
    {
        /// <summary>
        /// Устанавливает указатель текущего положения в потоке в новое место.
        /// Примечание: за границу потока установить указатель нельзя (допускается лишь устанавливать ее за последним записанным/считанным байтом).
        /// </summary>
        /// <param name="newPosition">Новая позиция указателя текущего положения.</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public abstract void SetPosition(int newPosition);

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
        public abstract int Read(byte[] bufferToReadBytesInto, int offset, int count);

        /// <summary>
        /// Длина потока данных, количество байт в потоке.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public abstract int Length { get; }

        /// <summary>
        /// Текущая позиция указателя в потоке (всегда - после последнего записанного/прочитанного байта).
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public abstract int Position { get; }

        /// <summary>
        /// Освобождает ресурсы потока (разблокирует файл в файловой системе).
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// Перемещает указатель текущего положения в потоке в самый конец потока (за последним записанным/считанным байтом).
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public abstract void MoveToEnd();
    }
}